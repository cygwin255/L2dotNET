using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using L2dotNET.DataContracts.Shared.Enums;
using L2dotNET.Network.serverpackets;
using L2dotNET.Utility;

namespace L2dotNET.Models.Player.General
{
    public class PlayerMovement : CharacterMovement
    {
        public bool IsSitting { get; protected set; }
        protected long _movementUpdateTime;
        protected bool _isSittingInProgress;
        protected new L2Player _character => (L2Player) base._character;

        public PlayerMovement(L2Player character) : base(character)
        {
        }

        public override bool CanMove()
        {
            if (_character.Movement.IsSitting)
            {
                return false;
            }

            return base.CanMove();
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

        public async Task Sit()
        {
            if (_isSittingInProgress || !CanMove() || IsMoving)
            {
                await _character.SendActionFailedAsync();
                return;
            }

            _isSittingInProgress = true;
            IsSitting = true;

            await _character.BroadcastPacketAsync(new ChangeWaitType(_character, ChangeWaitTypeId.Sit));

            await Task.Delay(2500);

            _isSittingInProgress = false;
        }

        public async Task Stand()
        {
            if (!IsSitting || _isSittingInProgress)
            {
                await _character.SendActionFailedAsync();
                return;
            }

            _isSittingInProgress = true;
            await _character.BroadcastPacketAsync(new ChangeWaitType(_character, ChangeWaitTypeId.Stand));

            await Task.Delay(2500);

            _isSittingInProgress = false;
            IsSitting = false;
            /*_sitTime.Enabled = false;
            _isSitting = !_isSitting;

            if (_isSitting || (_chair == null))
                return;

            _chair.IsUsedAlready = false;
            _chair = null;*/
        }

        public async Task SitStandToggle()
        {
            if (IsSitting)
            {
                await Stand();
            }
            else
            {
                await Sit();
            }
        }
    }
}
