using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using L2dotNET.Network.serverpackets;
using L2dotNET.Utility;

namespace L2dotNET.Models.Player.General
{
    public class CharacterMovement : MovementBase
    {
        protected long _movementUpdateTime;
        
        public CharacterMovement(L2Character character) : base(character)
        {
        }

        public override Task MoveTo(int x, int y, int z, L2Character target = null, int radiusOffset = 0)
        {
            Task moveTask = base.MoveTo(x, y, z, target, radiusOffset);

            _movementUpdateTime = DateTime.UtcNow.Ticks;

            return moveTask;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void UpdatePosition(int x, int y, int z)
        {
            if (!IsMoving || !CanMove())
            {
                return;
            }

            bool slowDown = Utilz.DistanceSq(x, y, DestinationX, DestinationY) > Utilz.DistanceSq(_x, _y, DestinationX, DestinationY);

            int dx = x - _x;
            int dy = y - _y;

            double distance = Utilz.Length(dx, dy);
            long currentTime = DateTime.UtcNow.Ticks;

            // TODO: move to config
            const int maxSpeedUpPerSecondUnsync = 20;

            int distanceAllowedUnsync = (int)((slowDown ? _character.CharacterStat.MoveSpeed : maxSpeedUpPerSecondUnsync) 
                * (currentTime - _movementUpdateTime) / TimeSpan.TicksPerSecond);

            if (distance <= distanceAllowedUnsync)
            {
                _x = x;
                _y = y;
            }
            else
            {
                _x += (int) (dx / distance * distanceAllowedUnsync);
                _y += (int) (dy / distance * distanceAllowedUnsync);
            }

            Z = z;
            PerformMove(true);
            _movementUpdateTime = currentTime;
        }
    }
}
