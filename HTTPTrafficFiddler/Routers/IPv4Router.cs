using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

using PacketDotNet;

using HTTPTrafficFiddler.Interfaces;
using HTTPTrafficFiddler.Components;
using HTTPTrafficFiddler.Extensions;

namespace HTTPTrafficFiddler.Routers
{
    class IPv4Router : IPacketRouter
    {
        private static IPv4Router instance;

        public static IPv4Router Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new IPv4Router();
                }

                return instance;
            }
        }

        private Dictionary<IPAddress, PhysicalAddress> routingTable;

        private IPv4Router()
        {
            routingTable = new Dictionary<IPAddress, PhysicalAddress>();
        }

        public Dictionary<IPAddress, PhysicalAddress> GetRoutingTable()
        {
            return routingTable;
        }

        public EthernetPacket RoutePacket(EthernetPacket ethernetPacket)
        {
            var iface = PacketDispatcher.Instance.GetCurrentInterface();

            if (!routingTable.ContainsKey(iface.IPv4Gateway)) return null;
            if (ethernetPacket.Type != EthernetPacketType.IpV4) return null;
            
            var packet = (IPv4Packet)ethernetPacket.PayloadPacket;            

            // change packets coming from gateway
            if (routingTable.ContainsKey(packet.DestinationAddress) && !packet.DestinationAddress.Equals(iface.IPv4Address)
                && !ethernetPacket.SourceHwAddress.Equals(iface.HardwareAddress))
            {
                ethernetPacket.DestinationHwAddress = routingTable[packet.DestinationAddress];
                ethernetPacket.SourceHwAddress = iface.HardwareAddress;

                return ethernetPacket;
            }

            // change packets coming from clients
            if (!packet.DestinationAddress.Equals(iface.IPv4Address) && !ethernetPacket.SourceHwAddress.Equals(iface.HardwareAddress))
            {
                ethernetPacket.DestinationHwAddress = routingTable[iface.IPv4Gateway];
                ethernetPacket.SourceHwAddress = iface.HardwareAddress;

                return ethernetPacket;
            }

            return null;
        }
    }
}
