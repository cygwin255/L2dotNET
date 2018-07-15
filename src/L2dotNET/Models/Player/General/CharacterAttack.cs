using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using L2dotNET.DataContracts.Shared.Enums;
using L2dotNET.Managers;
using L2dotNET.Network.serverpackets;
using L2dotNET.Utility;
using NLog.Targets;

namespace L2dotNET.Models.Player.General
{
    public class CharacterAttack
    {
        public bool IsAttacking { get; private set; }
        public bool IsCasting { get; private set; }
        public bool IsInAttackStance { get; private set; }

        private readonly L2Character _character;
        private L2Character _target;

        private Task _autoAttackTask;

        public CharacterAttack(L2Character character)
        {
            _character = character;
        }

        public async void DoAttack(L2Character target) // TODO: Add skill
        {
            if (!CanAttack(target) || target == _target)
            {
                _character.SendActionFailedAsync();
                return;
            }

            if (IsAttacking)
            {
                await CancelAttack();
            }

            IsAttacking = true;

            _target = target;

            _autoAttackTask = Task.Factory.StartNew(PerformAutoAttack); // for bow and melee weapon
        }

        public async Task CancelAttack()
        {
            IsAttacking = false;
            await _autoAttackTask;
        }

        public bool CanAttack(L2Character target = null, bool moveIfTooFar = true)
        {
            if (target == null)
            {
                target = _target;
            }

            if (target == null || target.Dead) // TODO: add check for immune/abnormal/GM
            {
                return false;
            }

            if (_character is L2Player)
            {
                L2Player player = (L2Player) _character;

                if (player.Movement.IsSitting)
                {
                    return false;
                }
            }

            // TODO: Move to config
            _character.SendMessageAsync($"[attack]distance to target is {(int)_character.Movement.DistanceTo(target)}");
            if (_character.Movement.DistanceToSquared(target) > 50*50 + 10) // TODO : use Weapon range
            {
                if (moveIfTooFar)
                {
                    _character.SendMessageAsync("[attack]targer is too far, moving for it");
                    _character.Movement.MoveToAndHit(target, 50);
                }

                return false;
            }

            return true;
        }

        private async Task PerformAutoAttack()
        {
            // TODO: revalidate that on every attack
            int attackSpeed = (int) (470000 / _character.CharacterStat.PAttackSpeed); // TODO: calculate real attack speed
            bool dual = true; // is dual weapon, harcode for now

            while (IsAttacking && CanAttack())
            {
                Attack attackPacket = new Attack(_character, GenerateSimpleHit(dual));

                if (dual)
                {
                    attackPacket.Hits.Add(GenerateSimpleHit(dual));
                }

                await _character.BroadcastPacketAsync(attackPacket);

                StartAutoAttack();
                _target.CharAttack.StartAutoAttack();

                await Task.Delay(dual ? attackSpeed / 2 : attackSpeed - 5);

                if (!IsAttacking || !CanAttack())
                {
                    break;
                }

                PerformHit(attackPacket.Hits[0]);

                if (dual)
                {
                    await Task.Delay(attackSpeed / 2 - 5);

                    if (!IsAttacking || !CanAttack())
                    {
                        break;
                    }

                    PerformHit(attackPacket.Hits[1]);
                }
            }

            IsAttacking = false;
            _target = null;
        }

        private async void PerformHit(Hit hit)
        {
            if (hit.IsMiss)
            {
                await _character.SendPacketAsync(new SystemMessage(SystemMessageId.MissedTarget));

                if (_target is L2Player)
                {
                    await _target.SendPacketAsync(new SystemMessage(SystemMessageId.AvoidedS1Attack)
                        .AddName(_character));
                }
            }
            else
            {
                _target.CharStatus.ReduceHp(hit.Damage, _character);

                // start autoAttack

                await _character.SendPacketAsync(new SystemMessage(SystemMessageId.YouDidS1Dmg).AddNumber(hit.Damage));

                if (hit.IsCritical)
                {
                    await _character.SendPacketAsync(new SystemMessage(SystemMessageId.CriticalHit));
                }

                if (_target is L2Player)
                {
                    await _target.SendPacketAsync(new SystemMessage(SystemMessageId.C1HasReceivedS3DamageFromC2)
                        .AddName(_target)
                        .AddName(_character)
                        .AddNumber(hit.Damage));
                }
            }
        }

        private Hit GenerateSimpleHit(bool dual)
        {
            int damage = RandomThreadSafe.Instance.Next(5, 20) / (dual ? 2 : 1);
            bool isMiss = RandomThreadSafe.Instance.NextDouble() < 0.10d;
            bool isCritical = RandomThreadSafe.Instance.NextDouble() < 0.15d;
            int shield = 0;

            if (isCritical)
            {
                damage *= 2;
            }

            return new Hit(_target.ObjectId, damage, isMiss, isCritical, shield, false, 0);
        }

        public async void StartAutoAttack()
        {
            if (_character is L2Player)
            {
                if (!IsInAttackStance)
                {
                    await _character.BroadcastPacketAsync(new AutoAttackStart(_character.ObjectId));
                }

                AttackStanceManager.SetAttackStance((L2Player) _character);
                IsInAttackStance = true;
            }
        }

        public async void StopAutoAttack()
        {
            if (IsInAttackStance && _character is L2Player)
            {
                await _character.BroadcastPacketAsync(new AutoAttackStop(_character.ObjectId));
                IsInAttackStance = false;
            }
        }
    }
}
