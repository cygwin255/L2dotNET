using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using L2dotNET.Models;
using L2dotNET.Models.Player;
using L2dotNET.Models.Zones;
using NLog;

namespace L2dotNET.World
{
    public static class L2World
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // Geodata min/max tiles
        public const int TileXMin = 16;
        public const int TileXMax = 26;
        public const int TileYMin = 10;
        public const int TileYMax = 25;

        // Map dimensions
        public const int TileSize = 32768;

        public const int WorldXMin = (TileXMin - 20) * TileSize;
        public const int WorldXMax = (TileXMax - 19) * TileSize;
        public const int WorldYMin = (TileYMin - 18) * TileSize;
        public const int WorldYMax = (TileYMax - 17) * TileSize;

        // Regions and offsets
        private const int RegionSize = 2048;
        private static readonly int RegionsX = (WorldXMax - WorldXMin) / RegionSize;
        private static readonly int RegionsY = (WorldYMax - WorldYMin) / RegionSize;
        private static readonly int RegionXOffset = Math.Abs(WorldXMin / RegionSize);
        private static readonly int RegionYOffset = Math.Abs(WorldYMin / RegionSize);

        private static ConcurrentDictionary<int, L2Object> _objects;
        private static ConcurrentDictionary<int, L2Player> _players;

        private static readonly Lazy<L2WorldRegion>[,] _worldRegions = new Lazy<L2WorldRegion>[RegionsX + 1, RegionsY + 1];

        public static void Initialize()
        {
            _objects = new ConcurrentDictionary<int, L2Object>();
            _players = new ConcurrentDictionary<int, L2Player>();

            Func<L2WorldRegion> LazyInitFunc(int i, int j) => () => new L2WorldRegion(i, j);

            for (int i = 0; i <= RegionsX; i++)
                for (int j = 0; j <= RegionsY; j++)
                    _worldRegions[i, j] = new Lazy<L2WorldRegion>(LazyInitFunc(i, j));

            Task.Factory.StartNew(DeactivateUnusedRegions);

            Log.Info($"WorldRegion grid ({RegionsX+1}x{RegionsY+1}) is now setted up. Total {(RegionsX + 1) * (RegionsY + 1)}");
        }

        public static void AddObject(L2Object obj)
        {
            if (_objects.TryAdd(obj.ObjectId, obj))
            {
                UpdateRegion(obj);
            }
        }

        public static void RemoveObject(L2Object obj)
        {
            L2Object o;
            _objects.TryRemove(obj.ObjectId, out o);
            o.Region.Remove(o);
            o.Region = null;
        }

        public static List<L2Object> GetObjects()
        {
            return _objects.Select(x => x.Value).ToList();
        }

        public static L2Object GetObject(int objectId)
        {
            L2Object obj;
            _objects.TryGetValue(objectId, out obj);

            return obj;
        }

        public static void AddPlayer(L2Player player)
        {
            _players.TryAdd(player.ObjectId, player);
        }

        public static void RemovePlayer(L2Player player)
        {
            L2Player o;
            _players.TryRemove(player.ObjectId, out o);
        }

        public static List<L2Player> GetPlayers()
        {
            return _players.Select(x => x.Value).ToList();
        }

        public static L2Player GetPlayer(int objectId)
        {
            L2Player player;
            _players.TryGetValue(objectId, out player);

            return player;
        }

        public static int GetRegionX(int regionX)
        {
            return (regionX - RegionXOffset) * RegionSize;
        }

        public static int GetRegionY(int regionY)
        {
            return (regionY - RegionYOffset) * RegionSize;
        }

        private static bool ValidRegion(int x, int y)
        {
            return (x >= 0) && (x <= RegionsX) && (y >= 0) && (y <= RegionsY);
        }

        public static L2WorldRegion GetRegion(L2Object obj)
        {
            return GetRegion(obj.X, obj.Y);
        }

        public static L2WorldRegion GetRegion(int x, int y)
        {
            int i = (x - WorldXMin) / RegionSize;
            int j = (y - WorldYMin) / RegionSize;

            return GetRegionByIndexes(i, j);
        }

        public static L2WorldRegion GetRegionByIndexes(int i, int j)
        {
            if (!ValidRegion(i, j))
            {
                return null;
            }

            return _worldRegions[i, j].Value;
        }

        public static Lazy<L2WorldRegion>[,] GetWorldRegions()
        {
            return _worldRegions;
        }

        public static async void UpdateRegion(L2Object obj)
        {
            L2WorldRegion activeRegion = GetRegion(obj);
            L2WorldRegion lastRegion = obj.Region;

            bool isPlayer = obj is L2Player;
            bool isNewObject = obj.Region == null;

            if (obj.Region == activeRegion)
            {
                return;
            }

            obj.Region?.Remove(obj);
            activeRegion.Add(obj);
            obj.Region = activeRegion;

            IEnumerable<L2WorldRegion> regionsDiff;

            if (isNewObject && lastRegion != null)
            {
                regionsDiff = activeRegion.GetNeighbours()
                    .Where(x => !lastRegion.GetNeighbours().Contains(x))
                    .ToList();
            }
            else
            {
                regionsDiff = activeRegion.GetNeighbours().ToList();
            }

            foreach (L2Player p in regionsDiff.SelectMany(x => x.GetPlayers()))
            {
                if (isPlayer)
                {
                    await p.BroadcastUserInfoToObjectAsync(obj);
                }

                await obj.BroadcastUserInfoToObjectAsync(p);
            }

            if (isPlayer)
            {
                foreach (L2Object o in regionsDiff.SelectMany(x => x.GetObjects()))
                {
                    await o.BroadcastUserInfoToObjectAsync(obj);
                    await Task.Delay(25); // TODO: Move to config
                }
            }
        }

        private static async void DeactivateUnusedRegions()
        {
            bool[,] lastUsedRegions = null;

            while (true)
            {
                bool[,] usedRegions = new bool[RegionsX + 1, RegionsY + 1];

                for (int i = 0; i <= RegionsX; i++)
                    for (int j = 0; j <= RegionsY; j++)
                    {
                        Lazy<L2WorldRegion> region = _worldRegions[i, j];

                        if (!region.IsValueCreated || region.Value.PlayersCount == 0)
                        {
                            continue;
                        }

                        foreach (L2WorldRegion r in region.Value.GetNeighbours())
                        {
                            usedRegions[r.X, r.Y] = true;
                        }
                    }

                if (lastUsedRegions == null)
                {
                    lastUsedRegions = usedRegions;
                }
                else
                {
                    for (int i = 0; i <= RegionsX; i++)
                        for (int j = 0; j <= RegionsY; j++)
                            if (!usedRegions[i, j] && !lastUsedRegions[i, j] 
                                && _worldRegions[i, j].IsValueCreated && _worldRegions[i, j].Value.IsActive)
                            {
                                _worldRegions[i, j].Value.Deactivate();
                                Log.Debug($"Deactivating region ({i},{j})");
                            }
                }

                await Task.Delay(60 * 1000); // TODO: Move to config
            }
        }

        public static async Task KickPlayer(L2Player player)
        {
            await player.SendMessageAsync("You have been kicked.");
            await player.Gameclient.Disconnect();
        }
    }
}