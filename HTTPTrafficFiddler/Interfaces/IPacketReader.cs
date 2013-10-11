using PacketDotNet;

namespace HTTPTrafficFiddler.Interfaces
{
    interface IPacketReader
    {
        void ReadPacket(EthernetPacket ethernetPacket);
    }
}
