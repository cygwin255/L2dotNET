namespace L2dotNET.Network.serverpackets
{
    class ServerClose : GameserverPacket
    {
        public override void Write()
        {
            WriteByte(0x26);
        }
    }
}