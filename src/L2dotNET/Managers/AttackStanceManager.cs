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

        private static Dictionary<L2Player, long> _players;

        public static void Initialize()
        {
            _players = new Dictionary<L2Player, long>();

            Task.Factory.StartNew(CheckStance);
        }

        public static void SetAttackStance(L2Player player)
        {
            lock (_players)
            {
                if (!_players.ContainsKey(player))
                {
                    _players.Add(player, DateTime.UtcNow.AddMilliseconds(ATTACK_STANCE_DURATION_MS).Ticks);
                    return;
                }

                _players[player] = DateTime.UtcNow.AddMilliseconds(ATTACK_STANCE_DURATION_MS).Ticks;
            }
        }

        private static async void CheckStance()
        {
            while (true)
            {
                List<L2Player> expiredPlayers;
                long currentTime = DateTime.UtcNow.Ticks;

                lock (_players)
                {
                    expiredPlayers = _players.Where(x => x.Value < currentTime).Select(x => x.Key).ToList();
                    expiredPlayers.ForEach(player => _players.Remove(player));
                }

                expiredPlayers.ForEach(player => player.CharAttack.StopAutoAttack());

                await Task.Delay(3000);
            }
        }
    }
}
