using System;
using System.Threading.Tasks;
using L2dotNET.Models.Player;
using L2dotNET.Network.serverpackets;
using L2dotNET.World;

namespace L2dotNET.Network.clientpackets
{
    class Appearing : PacketBase
    {
        private readonly GameClient _client;

        public Appearing(IServiceProvider serviceProvider, Packet packet, GameClient client) : base(serviceProvider)
        {
            _client = client;
        }

        public override async Task RunImpl()
        {
            L2Player player = _client.CurrentPlayer;

            int x = player.X;
            int y = player.Y;

            if (player.Obsx != -1)
            {
                x = player.Obsx;
                y = player.Obsy;
            }

            player.SendPacketAsync(new UserInfo(player));
            //L2World.UpdateRegion(player);
            //player.ValidateVisibleObjects(x, y, false);
            //player.UpdateVisibleStatus();


            player.SendActionFailedAsync();
        }
    }
}