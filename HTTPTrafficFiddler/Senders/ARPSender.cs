using System.Net;
using System.Net.NetworkInformation;

using PacketDotNet;
using SharpPcap.WinPcap;

using HTTPTrafficFiddler.Routers;
using HTTPTrafficFiddler.Interfaces;
using HTTPTrafficFiddler.Components;


namespace HTTPTrafficFiddler.Senders
{
    class ARPSender : IPacketIntervalSender
    {
        public SendQueue GetPacketQueue()
        {
            return GeneratePackets(false);
        }

        public SendQueue GetPacketQueueOnClose()
        {
            return GeneratePackets(true);
        }

        private SendQueue GeneratePackets(bool onClose)
        {
            var iface = PacketDispatcher.Instance.GetCurrentInterface();
            var routingTable = IPv4Router.Instance.GetRoutingTable();

            lock (routingTable)
            {
                if (routingTable.Count == 0) return null;

                var gatewayMAC = routingTable[iface.IPv4Gateway];
                var sendQueue = new SendQueue(64 * 4 * routingTable.Count);

                foreach (var entry in routingTable)
                {
                    // skip generating ARP replies for local gateway and local IP address entries
                    if (entry.Key.Equals(iface.IPv4Gateway) || entry.Key.Equals(iface.IPv4Address)) continue;

                    if (onClose)
                    {
                        // "friendly" ARP reply to gateway
                        sendQueue.Add(GenerateARPReply(entry.Key, entry.Value, iface.IPv4Gateway, gatewayMAC, iface.HardwareAddress).Bytes);

                        // "friendly" ARP reply to clients
                        sendQueue.Add(GenerateARPReply(iface.IPv4Gateway, gatewayMAC, entry.Key, entry.Value, iface.HardwareAddress).Bytes);
                    }
                    else
                    {
                        // fake ARP reply to gateway
                        sendQueue.Add(GenerateARPReply(entry.Key, iface.HardwareAddress, iface.IPv4Gateway, gatewayMAC).Bytes);

                        // fake ARP reply to clients
                        sendQueue.Add(GenerateARPReply(iface.IPv4Gateway, iface.HardwareAddress, entry.Key, entry.Value).Bytes);
                    }
                }

                return sendQueue;
            }
        }

        private EthernetPacket GenerateARPReply(IPAddress sourceAddress, PhysicalAddress sourceMAC, IPAddress targetAddress, PhysicalAddress targetMAC)
        {
            return GenerateARPReply(sourceAddress, sourceMAC, targetAddress, targetMAC, sourceMAC);
        }

        private EthernetPacket GenerateARPReply(IPAddress sourceAddress, PhysicalAddress sourceMAC, IPAddress targetAddress, PhysicalAddress targetMAC, PhysicalAddress ethernetSourceMAC)
        {
            var ethernetPacket = new EthernetPacket(ethernetSourceMAC, targetMAC, EthernetPacketType.Arp);
            var arpPacket = new ARPPacket(ARPOperation.Response, targetMAC, targetAddress, sourceMAC, sourceAddress);

            ethernetPacket.PayloadPacket = arpPacket;

            return ethernetPacket;
        }        
    }
}
