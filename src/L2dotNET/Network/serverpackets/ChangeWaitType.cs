using L2dotNET.DataContracts.Shared.Enums;
using L2dotNET.Models;

namespace L2dotNET.Network.serverpackets
{
    class ChangeWaitType : GameserverPacket
    {
        private readonly int _sId;
        private readonly int _type;
        private readonly int _x;
        private readonly int _y;
        private readonly int _z;

        public ChangeWaitType(L2Object player, ChangeWaitTypeId type)
        {
            _sId = player.ObjectId;
            _x = player.X;
            _y = player.Y;
            _z = player.Z;
            _type = (int)type;
        }

        public override void Write()
        {
            WriteByte(0x2f);
            WriteInt(_sId);
            WriteInt(_type);
            WriteInt(_x);
            WriteInt(_y);
            WriteInt(_z);
        }
    }
}