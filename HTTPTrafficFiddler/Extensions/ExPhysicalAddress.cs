using System;
using System.Linq;
using System.Net.NetworkInformation;

namespace HTTPTrafficFiddler.Extensions
{
    static class ExPhysicalAddress
    {
        /// <summary>
        /// Returns a string representation with hypens separating each byte (e.g. FF-FF-FF-FF-FF-FF).
        /// </summary>
        public static String ToFormattedString(this PhysicalAddress hardwareAddress)
        {
            var current = hardwareAddress.ToString();
            var parts = current.Select(x => x.ToString()).ToArray();

            return String.Format("{0}{1}-{2}{3}-{4}{5}-{6}{7}-{8}{9}-{10}{11}", parts);
        }
    }
}
