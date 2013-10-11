using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

using PacketDotNet;

using SharpPcap.LibPcap;
using SharpPcap.WinPcap;

using HTTPTrafficFiddler.Components;
using HTTPTrafficFiddler.Extensions;

namespace HTTPTrafficFiddler.Classes
{
    class NetworkInterface
    {
        public String Name;
        public String HardwareName;

        public IPAddress IPv4Address;
        public IPAddress IPv4Netmask;
        public IPAddress IPv4Gateway;

        public PhysicalAddress HardwareAddress;

        public bool HasIPv4Gateway;
        public int CIDR;

        public WinPcapDevice PcapDevice;
        public PcapInterface PcapInterface;

        /// <summary>
        /// Default NetworkInterface constructor - initializes properties to their default values.
        /// </summary>
        public NetworkInterface()
        {
            Name = "";
            HardwareName = "";

            IPv4Address = new IPAddress(0);
            IPv4Netmask = new IPAddress(0);
            IPv4Gateway = new IPAddress(0);

            HasIPv4Gateway = false;
            CIDR = 0;
        }

        /// <summary>
        /// NetworkInterface constructor accepting a "WinPcapDevice" used for setting appropriate values.
        /// </summary>
        /// <param name="device">WinPcapDevice to be used for this interface</param>
        public NetworkInterface(WinPcapDevice device) : this()
        {
            PcapDevice = device;
            PcapInterface = PcapDevice.Interface;

            Name = device.Interface.FriendlyName;
            HardwareName = device.Interface.Description;

            ParseIPv4Addresses();
        }

        /// <summary>
        /// Sends an ethernetPacket via internal "WinPcapDevice".
        /// </summary>
        public void SendPacket(EthernetPacket packet)
        {
            if (!PcapDevice.Opened) return;

            PcapDevice.SendPacket(packet.Bytes);
        }

        /// <summary>
        /// Transmits a send queue via internal "WinPcapDevice" and disposes it at the end.
        /// </summary>
        public void SendPacketQueue(SendQueue packetQueue)
        {
            if (!PcapDevice.Opened) return;

            PcapDevice.SendQueue(packetQueue, SendQueueTransmitModes.Normal);

            packetQueue.Dispose();
        }

        /// <summary>
        /// Checks if an IPv4 packet is originaly coming from/to this network interface. 
        /// </summary>
        public bool IsLocalIPv4Packet(EthernetPacket packet)
        {
            if (packet.Type != EthernetPacketType.IpV4) return false;

            var IPv4Packet = (IPv4Packet)packet.PayloadPacket;

            if (IPv4Packet == null) return false;

            // match multicast packets
            if (IPv4Packet.DestinationAddress.IsIPv4Multicast()) return true; 

            // match unicast packets
            if (IPv4Packet.DestinationAddress.Equals(IPv4Address) || IPv4Packet.SourceAddress.Equals(IPv4Address)) return true;

            return false;
        }

        /// <summary>
        /// Sets a static ARP entry for current gateway's IPv4 address. (Windows Vista+)
        /// </summary>
        /// <param name="macAddress">Gateway's MAC address</param>
        public void SetStaticARPGateway(PhysicalAddress macAddress)
        {
            SetStaticARP(IPv4Gateway, macAddress);
        }

        /// <summary>
        /// Sets a static ARP entry for specified IPv4/MAC pair. (Windows Vista+)
        /// </summary>
        /// <param name="ipAddress">ARP entry IPv4 address</param>
        /// <param name="macAddress">ARP entry MAC address</param>
        public void SetStaticARP(IPAddress ipAddress, PhysicalAddress macAddress)
        {
            var osVersion = Environment.OSVersion;

            DeleteStaticARP(ipAddress);

            // Windows Vista+ only
            if (osVersion.Version.Major < 6) return;

            var command = String.Format("netsh interface ip add neighbors \"{0}\" {1} {2}", Name, ipAddress, macAddress.ToFormattedString());      
            SystemInterface.ShellExecute(command);
        }

        /// <summary>
        /// Deletes a static ARP entry for specified IPv4 address. (Windows Vista+)
        /// </summary>
        /// <param name="ipAddress">ARP entry IPv4 address</param>
        public void DeleteStaticARP(IPAddress ipAddress)
        {
            var osVersion = Environment.OSVersion;

            // Windows Vista+ only
            if (osVersion.Version.Major < 6) return;

            var command = String.Format("netsh interface ip delete neighbors \"{0}\" {1}", Name, ipAddress);            
            SystemInterface.ShellExecute(command);
        }

        /// <summary>
        /// Parses IPv4 addresses from internal "WinPcapDevice". 
        /// </summary>
        private void ParseIPv4Addresses()
        {
            if (PcapInterface == null) return;

            if (PcapInterface.Addresses == null) return;

            // set first available IPv4 address as the default interface IPv4
            foreach (var address in PcapInterface.Addresses)
            {
                if (address.Addr == null) continue;

                if (address.Addr.type != Sockaddr.AddressTypes.AF_INET_AF_INET6) continue;
                if (address.Addr.ipAddress.AddressFamily != AddressFamily.InterNetwork) continue;

                IPv4Address = address.Addr.ipAddress;
                IPv4Netmask = address.Netmask.ipAddress;
                CIDR = 32 - IPv4Netmask.GetTrailingZeroes();
                break;
            }

            // set default gateway
            if (PcapInterface.GatewayAddress != null && PcapInterface.GatewayAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] gatewayBytes = PcapInterface.GatewayAddress.GetAddressBytes();
                IPv4Gateway = new IPAddress(gatewayBytes);

                HasIPv4Gateway = true;
            }

            // set hardware address
            foreach (var address in PcapInterface.Addresses)
            {
                if (address.Addr.type != Sockaddr.AddressTypes.HARDWARE) continue;

                HardwareAddress = address.Addr.hardwareAddress;
                break;
            }
        }

        public override String ToString()
        {
            if (HasIPv4Gateway)
            {
                return String.Format("{0} | IPv4: {1}/{2}, GW: {3}", Name, IPv4Address, CIDR, IPv4Gateway);
            }
            else
            {
                return String.Format("{0} | IPv4: {1}/{2} (undefined gateway)", Name, IPv4Address, CIDR);
            }
        }
    }
}
