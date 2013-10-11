using System;
using System.Collections.Generic;
using System.Net;

using HTTPTrafficFiddler.Extensions;

namespace HTTPTrafficFiddler.Classes
{
    class IPv4AddressRange
    {
        /// <summary>
        /// Number of IPv4 addresses inside the address range.
        /// </summary>
        public int AddressCount;

        private uint startAddress;
        private uint endAddress;

        /// <summary>
        /// Create a new IPv4 address range from an existing "NetworkInterface".
        /// </summary>
        /// <param name="iface">NetworkInterface instance to use in creating a new IPv4 address range</param>
        public static IPv4AddressRange CreateFromInterface(NetworkInterface iface)
        {
            var range = new IPv4AddressRange();

            range.AddressCount = (int)Math.Pow(2, 32 - iface.CIDR) - 2;

            uint address = iface.IPv4Address.ToUint();
            uint netmask = iface.IPv4Netmask.ToUint();

            uint network = address & netmask;

            range.startAddress = network + 1;
            range.endAddress = network + (uint)range.AddressCount;

            return range;
        }

        /// <summary>
        /// Returns an enumerable collection of IPv4 addresses inside the address range.
        /// </summary>
        public IEnumerable<IPAddress> GetAddresses()
        {
            for (uint a = startAddress; a <= endAddress; a++)
            {
                byte[] bytes = BitConverter.GetBytes(a);
                Array.Reverse(bytes);

                yield return new IPAddress(bytes);
            }
        }
    }
}
