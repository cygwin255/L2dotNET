using System.Linq;
using System.Threading.Tasks;
using L2dotNET.Models.Player;
using L2dotNET.Network.serverpackets;

namespace L2dotNET.Models.Vehicles
{
    public class L2Boat : L2Object
    {
        public bool OnRoute = false;

        public L2Boat(int objectId) : base(objectId)
        {
        }

        public override async Task BroadcastUserInfoAsync()
        {
            await Region.BroadcastToNeighbours(BroadcastUserInfoToObjectAsync);
        }

        public override async Task BroadcastUserInfoToObjectAsync(L2Object obj)
        {
            await obj.SendPacketAsync(new VehicleInfo(this));
        }
    }
}