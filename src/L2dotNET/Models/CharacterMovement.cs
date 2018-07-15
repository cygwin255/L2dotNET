using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using L2dotNET.Models.Player;
using L2dotNET.Network.serverpackets;
using L2dotNET.Utility;

namespace L2dotNET.Models
{
    public class CharacterMovement
    {
        public int X
        {
            get
            {
                PerformMove();
                return _x;
            }
            set
            {
                if (IsMoving)
                {
                    NotifyStopMove();
                }

                _x = value;
            }
        }

        public int Y
        {
            get
            {
                PerformMove();
                return _y;
            }
            set
            {
                if (IsMoving)
                {
                    NotifyStopMove();
                }

                _y = value;
            }
        }

        public int Z { get; set; }

        public int Heading { get; private set; }
        public bool IsMoving { get; private set; }
        public int DestinationX { get; private set; }
        public int DestinationY { get; private set; }
        public int DestinationZ { get; private set; }
        public int DestinationRadiusOffset { get; private set; }

        protected virtual L2Character _character { get; }
        protected long _movementLastTime;
        protected L2Character _attackTarget;
        protected int _x;
        protected int _y;

        public CharacterMovement(L2Character character)
        {
            _character = character;
            _attackTarget = null;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected void PerformMove(bool forceUpdate = false)
        {
            //await ValidateWaterZones();

            if (!IsMoving)
            {
                return;
            }

            if (!CanMove())
            {
                NotifyStopMove();
                return;
            }

            long currentTime = DateTime.UtcNow.Ticks;
            float elapsedSeconds = (currentTime - _movementLastTime) / (float) TimeSpan.TicksPerSecond;

            // TODO: move to config
            if (!forceUpdate && elapsedSeconds < 0.05f) // 50 ms, skip run if last run was less then 50ms ago
            {
                return;
            }

            _movementLastTime = currentTime;

            if (_attackTarget != null) // if we have target then update destination coordinates
            {
                DestinationX = _attackTarget.X;
                DestinationY = _attackTarget.Y;
                DestinationZ = _attackTarget.Z;
            }

            float distance = (float) Utilz.Length(DestinationX - _x, DestinationY - _y);
            _character.SendMessageAsync($"distance to dest is {distance}");
            // vector to destination with length = 1
            float vectorX = (DestinationX - _x) / distance;
            float vectorY = (DestinationY - _y) / distance;

            if (_attackTarget != null && DestinationRadiusOffset > 0)
            {
                DestinationX -= (int) (vectorX * DestinationRadiusOffset);
                DestinationY -= (int) (vectorY * DestinationRadiusOffset);
                distance = (float) Utilz.Length(DestinationX - _x, DestinationY - _y);
            }

            int dx = (int) (vectorX * _character.CharacterStat.MoveSpeed * elapsedSeconds);
            int dy = (int) (vectorY * _character.CharacterStat.MoveSpeed * elapsedSeconds);
            double ddistance = Utilz.Length(dx, dy);

            Heading = (int) (Math.Atan2(-vectorX, -vectorY) * 10430.378 + short.MaxValue);

            if (ddistance >= distance || distance < 20 || (_attackTarget != null && _character.CharAttack.CanAttack(_attackTarget, false)))
            {
                _x = DestinationX;
                _y = DestinationY;

                NotifyArrived();
                return;
            }

            _x += (int) (vectorX * _character.CharacterStat.MoveSpeed * elapsedSeconds);
            _y += (int) (vectorY * _character.CharacterStat.MoveSpeed * elapsedSeconds);
        }

        public double DistanceTo(int x, int y)
        {
            return Math.Sqrt(Math.Pow(x - X, 2) + Math.Pow(y - Y, 2));
        }

        public double DistanceToSquared(int x, int y)
        {
            return Math.Pow(x - X, 2) + Math.Pow(y - Y, 2);
        }

        public double DistanceTo(L2Object obj)
        {
            return DistanceTo(obj.X, obj.Y);
        }

        public double DistanceToSquared(L2Object obj)
        {
            return DistanceToSquared(obj.X, obj.Y);
        }

        public virtual bool CanMove()
        {
            if (_character.PBlockAct == 1)
            {
                return false;
            }

            if ((_character.AbnormalBitMaskEx & L2Character.AbnormalMaskExFreezing) == L2Character.AbnormalMaskExFreezing)
            {
                return false;
            }

            return true;
        }

        public void UpdatePosition()
        {
            PerformMove(true);
        }

        public virtual async Task MoveTo(int x, int y, int z, L2Character target = null, int radiusOffset = 0)
        {
            if (!CanMove())
            {
                await _character.SendActionFailedAsync();
                return;
            }

            if (_character.CharAttack.IsAttacking)
            {
                await _character.CharAttack.CancelAttack();
            }

            if (IsMoving)
            {
                PerformMove(true);
                await NotifyStopMove(false);
            }

            DestinationRadiusOffset = radiusOffset;
            _attackTarget = target;

            float dx = x - X;
            float dy = y - Y;
            // float dz = z - Z;
            double distance = DistanceTo(x, y);

            // TODO: move to config. 10 is an allowable error - no movement needed if dist less then that value.
            if (distance > 9900 || distance <= radiusOffset || distance < 10)
            {
                await _character.SendActionFailedAsync();
                return;
            }

            DestinationX = x;
            DestinationY = y;
            DestinationZ = z;

            Vector2 targetVector = new Vector2(dx, dy);
            targetVector /= targetVector.Length();

            Heading = (int) (Math.Atan2(-targetVector.X, -targetVector.Y) * 10430.378 + short.MaxValue);

            if (radiusOffset > 0)
            {
                DestinationX -= (int) (targetVector.X * radiusOffset);
                DestinationY -= (int) (targetVector.Y * radiusOffset);
            }

            _movementLastTime = DateTime.UtcNow.Ticks;
            IsMoving = true;
            await _character.BroadcastPacketAsync(new CharMoveToLocation(_character));
        }

        public virtual async Task MoveToAndHit(L2Character target, int radiusOffset = 150)
        {
            await MoveTo(target.X, target.Y, target.Z, target, radiusOffset);
            Task.Factory.StartNew(BroadcastDestinationChanged);
        }

        private async void BroadcastDestinationChanged()
        {
            int oldDestinationX = DestinationX;
            int oldDestinationY = DestinationY;
            int oldDestinationZ = DestinationZ;

            await Task.Delay(1000); // TODO: move to config

            while (IsMoving && _attackTarget != null)
            {
                if (oldDestinationX != DestinationX || oldDestinationY != DestinationY || oldDestinationZ != DestinationZ)
                {
                    // Send broadcast every 1s if destination is changed
                    _character.BroadcastPacketAsync(new CharMoveToLocation(_character));
                }

                oldDestinationX = DestinationX;
                oldDestinationY = DestinationY;
                oldDestinationZ = DestinationZ;

                // TODO: move to config
                await Task.Delay(1000);
                //PerformMove();
            }
        }

        public virtual async Task NotifyStopMove(bool broadcast = true)
        {
            if (IsMoving && broadcast)
            {
                await _character.BroadcastPacketAsync(new StopMove(_character));
            }

            IsMoving = false;
            _character.SendMessageAsync("[move]stopped movement!");
        }

        public virtual void NotifyArrived()
        {
            IsMoving = false;
            _character.SendMessageAsync("[move]arrived!");
            if (_attackTarget != null)
            {
                _character.SendMessageAsync("[move]starting attack!");
                _character.CharAttack.DoAttack(_attackTarget);
                _attackTarget = null;
            }
        }
    }
}
