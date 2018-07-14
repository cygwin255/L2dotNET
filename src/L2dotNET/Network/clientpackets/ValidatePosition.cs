using System;
using System.Threading.Tasks;
using L2dotNET.Models.Player;
using L2dotNET.Models.Zones;
using L2dotNET.World;
using NLog;

namespace L2dotNET.Network.clientpackets
{
    class ValidatePosition : PacketBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private const int Synctype = 1;

        private readonly GameClient _client;
        private readonly int _x;
        private readonly int _y;
        private readonly int _z;
        private readonly int _heading;
        private readonly int _data;

        public ValidatePosition(IServiceProvider serviceProvider, Packet packet, GameClient client) : base(serviceProvider)
        {
            _client = client;
            _x = packet.ReadInt();
            _y = packet.ReadInt();
            _z = packet.ReadInt();
            _heading = packet.ReadInt();
            _data = packet.ReadInt();
        }

        public override async Task RunImpl()
        {
            L2Player player = _client.CurrentPlayer;
            L2WorldRegion prevReg = player.Region;

            int realX = player.X;
            int realY = player.Y;
            int realZ = player.Z;

            int dx = _x - realX;
            int dy = _y - realY;
            int dz = _z - realZ;

            double diffSq = Math.Sqrt(dx * dx + dy * dy);
            player.SendMessageAsync($"diff: {(int) diffSq}");

            player.CharMovement.UpdatePosition(_x, _y, _z);

            if (diffSq > 600)
            {
                Log.Error($"User {player.ObjectId}:{player.Account.Login}:{player.Name} coord is unsync with server");
                // TODO: Add teleport back
            }

            L2World.UpdateRegion(player);

            //Log.Info($"Current client position: X:{_x}, Y:{_y}, Z:{_z}"); //debug
            player.BroadcastUserInfoAsync();
        }
    }
}