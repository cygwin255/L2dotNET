using System;
using System.Linq;
using System.Threading.Tasks;
using L2dotNET.Models.Player;
using L2dotNET.World;
using Microsoft.Extensions.DependencyInjection;

namespace L2dotNET.Network.loginauth.recv
{
    class LoginServKickAccount : PacketBase
    {
        private readonly AuthThread _authThread;
        private readonly int _accountId;

        public LoginServKickAccount(IServiceProvider serviceProvider, Packet p, AuthThread authThread) : base(serviceProvider)
        {
            _authThread = authThread;
            _accountId = p.ReadInt();
        }

        public override async Task RunImpl()
        {
            L2Player player = L2World.GetPlayers().FirstOrDefault(x => x.Account.AccountId == _accountId);

            if (player == null)
            {
                GameServer.ServiceProvider.GetService<ClientManager>().Disconnect(_accountId);
                return;
            }

            await L2World.KickPlayer(player);
        }
    }
}