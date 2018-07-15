namespace L2dotNET.Network.loginauth.send
{
    class AccountInGame : GameserverPacket
    {
        private readonly int _accountId;
        private readonly bool _status;

        public AccountInGame(int accountId, bool status)
        {
            _accountId = accountId;
            _status = status;
        }

        public override void Write()
        {
            WriteByte(0xA2);
            WriteInt(_accountId);
            WriteByte(_status ? (byte)1 : (byte)0);
        }
    }
}