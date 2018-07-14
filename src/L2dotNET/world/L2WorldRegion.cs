using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using L2dotNET.Models;
using L2dotNET.Models.Player;
using L2dotNET.Models.Zones;
using L2dotNET.Utility;
using NLog;

namespace L2dotNET.World
{
    public class L2WorldRegion
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly object _activationLock = new object();
        private readonly LockedDictionary<int, L2Object> _objects = new LockedDictionary<int, L2Object>(3);
        private readonly LockedDictionary<int, L2Player> _players = new LockedDictionary<int, L2Player>(3);

        //private readonly List<L2ZoneType> _zones = new List<L2ZoneType>();

        public int X { get; }
        public int Y { get; }

        public bool IsActive { get; private set; }
        public int ObjectsCount => _objects.Count;
        public int PlayersCount => _players.Count;

        public L2WorldRegion(int x, int y)
        {
            X = x;
            Y = y;
        }

        public IEnumerable<L2Object> GetObjects()
        {
            return _objects.GetAll();
        }

        public IEnumerable<L2Player> GetPlayers()
        {
            return _players.GetAll();
        }

        public int GetObjectsCount()
        {
            return _objects.Count;
        }

        public int GetPlayersCount()
        {
            return _players.Count;
        }
 
        public async void Activate()
        {
            lock (_activationLock)
            {
                if (IsActive)
                {
                    return;
                }

                IsActive = true;
                // TODO: Add region activation logic
            }
        }

        public void Deactivate()
        {
            lock (_activationLock)
            {
                if (!IsActive)
                {
                    return;
                }

                IsActive = false;
                // TODO: Add region deactivation logic
            }
        }

        public void Add(L2Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (obj is L2Player)
            {
                _players.Add(obj.ObjectId, (L2Player) obj);

                foreach (L2WorldRegion region in GetClosestNeighbours().Where(x => !x.IsActive))
                {
                    region.Activate();
                }
            }
            else
            {
                _objects.Add(obj.ObjectId, obj);
            }
        }

        public void Remove(L2Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (obj is L2Player)
            {
                _players.Remove(obj.ObjectId);
            }
            else
            {
                _objects.Remove(obj.ObjectId);
            }
        }

        public IEnumerable<L2Object> GetAllNeighbourObjects()
        {
            return GetNeighbours().SelectMany(x => x.GetObjects());
        }

        public IEnumerable<L2Player> GetAllNeighbourPlayers(int? exclude = null)
        {
            IEnumerable<L2Player> players = GetNeighbours().Where(x => x != null).SelectMany(x => x.GetPlayers());

            if (exclude.HasValue)
            {
                return players.Where(x => x.ObjectId != exclude.Value);
            }

            return players;
        }

        public async Task BroadcastToNeighbours(Func<L2Player, Task> asyncAction, int? exclude = null)
        {
            await Task.WhenAll(GetAllNeighbourPlayers(exclude).Select(asyncAction));
        }

        public IEnumerable<L2WorldRegion> GetNeighbours()
        {
            return GetNeighboursEnumerable().Union(GetFarNeighboursEnumerable()).Where(x => x != null);
        }

        public IEnumerable<L2WorldRegion> GetClosestNeighbours()
        {
            return GetNeighboursEnumerable().Where(x => x != null);
        }

        private IEnumerable<L2WorldRegion> GetNeighboursEnumerable()
        {
            yield return this;

            yield return L2World.GetRegionByIndexes(X + 1, Y);
            yield return L2World.GetRegionByIndexes(X - 1, Y);
            yield return L2World.GetRegionByIndexes(X, Y + 1);
            yield return L2World.GetRegionByIndexes(X, Y - 1);

            yield return L2World.GetRegionByIndexes(X + 1, Y + 1);
            yield return L2World.GetRegionByIndexes(X - 1, Y + 1);
            yield return L2World.GetRegionByIndexes(X + 1, Y - 1);
            yield return L2World.GetRegionByIndexes(X - 1, Y - 1);
        }

        private IEnumerable<L2WorldRegion> GetFarNeighboursEnumerable()
        {
            yield return L2World.GetRegionByIndexes(X+2, Y);
            yield return L2World.GetRegionByIndexes(X+2, Y+1);
            yield return L2World.GetRegionByIndexes(X+2, Y-1);
            //yield return L2World.GetRegionByIndexes(_tileX+2, _tileY+2);
            //yield return L2World.GetRegionByIndexes(_tileX+2, _tileY-2);

            yield return L2World.GetRegionByIndexes(X - 2, Y);
            yield return L2World.GetRegionByIndexes(X - 2, Y + 1);
            yield return L2World.GetRegionByIndexes(X - 2, Y - 1);
            //yield return L2World.GetRegionByIndexes(_tileX - 2, _tileY + 2);
            //yield return L2World.GetRegionByIndexes(_tileX - 2, _tileY - 2);

            yield return L2World.GetRegionByIndexes(X, Y+2);
            yield return L2World.GetRegionByIndexes(X+1, Y+2);
            yield return L2World.GetRegionByIndexes(X-1, Y+2);

            yield return L2World.GetRegionByIndexes(X, Y-2);
            yield return L2World.GetRegionByIndexes(X+1, Y-2);
            yield return L2World.GetRegionByIndexes(X-1, Y-2);

        }
    }
}