using System;
using System.Net;

namespace HTTPTrafficFiddler.Extensions
{
    static class ExIPAddress
    {
        /// <summary>
        /// Returns a number of trailing zeroes in an IPv4 address' binary representation.
        /// </summary>
        public static int GetTrailingZeroes(this IPAddress address)
        {
            uint mask = address.ToUint();
            int zeroes = 0;

            while (mask != 0 && (mask & 1) == 0)
            {
                zeroes++;
                mask = mask >> 1;
            }

            return zeroes;
        }

        /// <summary>
        /// Returns an unsigned 32-bit integer from IPv4 address' bytes.
        /// </summary>
        public static uint ToUint(this IPAddress address)
        {
            byte[] bytes = address.GetAddressBytes();
            Array.Reverse(bytes);

            return BitConverter.ToUInt32(bytes, 0);
        }

        /// <summary>
        /// Checks if IPv4 address is a multicast address.
        /// </summary>
        public static bool IsIPv4Multicast(this IPAddress address)
        {
            uint addressValue = address.ToUint();

            return (addressValue >> 28) == 224;
        }
    }
}
