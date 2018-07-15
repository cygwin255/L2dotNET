using L2dotNET.Models;

namespace L2dotNET.Network.serverpackets
{
    class CharMoveToLocation : GameserverPacket
    {
        private readonly L2Character _character;

        public CharMoveToLocation(L2Character character)
        {
            _character = character;
        }

        public override void Write()
        {
            WriteByte(0x01);

            WriteInt(_character.ObjectId);

            WriteInt(_character.Movement.DestinationX);
            WriteInt(_character.Movement.DestinationY);
            WriteInt(_character.Movement.DestinationZ);

            WriteInt(_character.X);
            WriteInt(_character.Y);
            WriteInt(_character.Z);
        }
    }
}