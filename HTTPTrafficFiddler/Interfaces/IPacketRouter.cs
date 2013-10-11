using PacketDotNet;

namespace HTTPTrafficFiddler.Interfaces
{
    interface IPacketRouter
    {
        EthernetPacket RoutePacket(EthernetPacket ethernetPacket);
    }
}
