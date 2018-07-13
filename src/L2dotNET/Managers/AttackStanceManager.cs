using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using L2dotNET.Models.Player;

namespace L2dotNET.Managers
{
    public static class AttackStanceManager
    {
        private const int ATTACK_STANCE_DURATION_MS = 15000;

        private static Dictionary<L2Player, DateTime> _players;

        public static void Initialize()
        {
            _players = new Dictionary<L2Player, DateTime>();

            Task.Factory.StartNew(CheckStance);
        }

        public static void SetAttackStance(L2Player player)
        {
            lock (_players)
            {
                if (!_players.ContainsKey(player))
                {
                    _players.Add(player, DateTime.UtcNow.AddMilliseconds(ATTACK_STANCE_DURATION_MS));
                    return;
                }

                _players[player] = DateTime.UtcNow.AddMilliseconds(ATTACK_STANCE_DURATION_MS);
            }
        }

        private static async void CheckStance()
        {
            while (true)
            {
                List<L2Player> expiredPlayers;
                lock (_players)
                {
                    expiredPlayers = _players.Where(x => x.Value < DateTime.UtcNow).Select(x => x.Key).ToList();
                    expiredPlayers.ForEach(player => _players.Remove(player));
                }

                expiredPlayers.ForEach(player => player.CharAttack.StopAutoAttack());

                await Task.Delay(3000);
            }
        }
    }
}
