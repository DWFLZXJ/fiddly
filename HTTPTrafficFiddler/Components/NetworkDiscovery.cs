using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

using PacketDotNet;
using SharpPcap.WinPcap;

using HTTPTrafficFiddler.Classes;
using NetworkInterface = HTTPTrafficFiddler.Classes.NetworkInterface;

namespace HTTPTrafficFiddler.Components
{
    static class NetworkDiscovery
    {
        // number of discovery packets (ARP requests) sent in one batch
        private static readonly int PacketsPerQueue = 32;

        private static readonly PhysicalAddress BroadcastMAC = PhysicalAddress.Parse("FFFFFFFFFFFF");
        
        public static void SendPackets(NetworkInterface iface)
        {
            var packetCount = 0;
            var range = IPv4AddressRange.CreateFromInterface(iface);
            var sendQueue = new SendQueue(64 * PacketsPerQueue);            

            foreach (var address in range.GetAddresses())
            {                
                sendQueue.Add(CreateARPRequest(iface.IPv4Address, iface.HardwareAddress, address).Bytes);
                packetCount++;

                if (packetCount == PacketsPerQueue)
                {
                    iface.SendPacketQueue(sendQueue);
                    
                    sendQueue = new SendQueue(64 * PacketsPerQueue);
                    packetCount = 0;

                    Thread.Sleep(10);
                }
            }
            
            // send any remaining packets
            if (packetCount != 0)
            {
                iface.SendPacketQueue(sendQueue);
            }
        }

        private static EthernetPacket CreateARPRequest(IPAddress sourceAddress, PhysicalAddress sourceMAC, IPAddress destinationAddress)
        {
            var ethernetPacket = new EthernetPacket(sourceMAC, BroadcastMAC, EthernetPacketType.Arp);
            var arpPacket = new ARPPacket(ARPOperation.Request, BroadcastMAC, destinationAddress, sourceMAC, sourceAddress);

            ethernetPacket.PayloadPacket = arpPacket;

            return ethernetPacket;
        }
    }
}
