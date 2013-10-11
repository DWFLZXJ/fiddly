using System.Collections.Generic;
using SharpPcap.WinPcap;

using HTTPTrafficFiddler.Classes;

namespace HTTPTrafficFiddler.Components
{
    /// <summary>
    /// Responsible for reading available network interfaces.
    /// </summary>
    class NetworkInterfaceList
    {
        private static NetworkInterfaceList instance;

        public static NetworkInterfaceList Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new NetworkInterfaceList();
                }

                return instance;
            }
        }

        private List<NetworkInterface> networkInterfaces;

        /// <summary>
        /// Default (private) constructor - reads raw WinPcapDevices and creates a corresponding NetworkInterface list.
        /// </summary>
        private NetworkInterfaceList()
        {
            networkInterfaces = new List<NetworkInterface>();

            var deviceList = WinPcapDeviceList.Instance;
            
            foreach (var device in deviceList)
            {
                var iface = new NetworkInterface(device);                
                networkInterfaces.Add(iface);
            }
        }

        /// <summary>
        /// Returns a NetworkInterface list.
        /// </summary>
        public List<NetworkInterface> GetList()
        {
            return networkInterfaces;
        }

        /// <summary>
        /// Returns a most appropriate default interface based on some simple value checks.
        /// </summary>
        public NetworkInterface GetBestInterface()
        {
            if (networkInterfaces.Count == 0) return null;

            foreach (var iface in networkInterfaces)
            {
                if (iface.HasIPv4Gateway)
                {
                    return iface;
                }
            }

            return networkInterfaces[0];
        }
    }
}
