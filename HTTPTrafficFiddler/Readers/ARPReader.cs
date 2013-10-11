using System;

using PacketDotNet;

using HTTPTrafficFiddler.Components;
using HTTPTrafficFiddler.Routers;
using HTTPTrafficFiddler.Interfaces;

namespace HTTPTrafficFiddler.Readers
{
    class ARPReader : IPacketReader
    {
        public void ReadPacket(EthernetPacket ethernetPacket)
        {
            if (ethernetPacket.Type != EthernetPacketType.Arp) return;

            var packet = (ARPPacket)ethernetPacket.PayloadPacket;
            var routingTable = IPv4Router.Instance.GetRoutingTable();

            if (packet.Operation == ARPOperation.Response)
            {
                if (!routingTable.ContainsKey(packet.SenderProtocolAddress))
                {
                    routingTable.Add(packet.SenderProtocolAddress, ethernetPacket.SourceHwAddress);

                    DebugInformation.WriteLine(String.Format("[ARP] adding entry {0} - {1} to routing table",
                        packet.SenderProtocolAddress, ethernetPacket.SourceHwAddress));
                }
            }
        }
    }
}
