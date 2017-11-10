﻿using L2dotNET.model.player;

namespace L2dotNET.Network.serverpackets
{
    class PartySmallWindowUpdate : GameserverPacket
    {
        private readonly L2Player _member;

        public PartySmallWindowUpdate(L2Player member)
        {
            _member = member;
        }

        public override void Write()
        {
            WriteByte(0x52);
            WriteInt(_member.ObjId);
            WriteString(_member.Name);
            WriteInt(_member.CurCp);
            WriteInt(_member.MaxCp);
            WriteInt(_member.CurHp);
            WriteInt(_member.MaxHp);
            WriteInt(_member.CurMp);
            WriteInt(_member.MaxMp);
            WriteInt(_member.Level);
            WriteInt((int)_member.ActiveClass.ClassId.Id);
        }
    }
}