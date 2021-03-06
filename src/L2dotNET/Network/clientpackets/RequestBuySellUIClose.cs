﻿using System;
using System.Threading.Tasks;
using L2dotNET.Models.Player;

namespace L2dotNET.Network.clientpackets
{
    class RequestBuySellUiClose : PacketBase
    {
        private readonly GameClient _client;

        public RequestBuySellUiClose(IServiceProvider serviceProvider, Packet packet, GameClient client) : base(serviceProvider)
        {
            packet.MoveOffset(2);
            _client = client;
        }

        public override async Task RunImpl()
        {
            await Task.Run(() =>
            {
                L2Player player = _client.CurrentPlayer;

                player.SendItemList(true);
            });
        }
    }
}