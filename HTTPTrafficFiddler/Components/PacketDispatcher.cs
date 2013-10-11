using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Threading;
using System.Diagnostics;

using PacketDotNet;

using SharpPcap;
using SharpPcap.WinPcap;

using HTTPTrafficFiddler.Readers;
using HTTPTrafficFiddler.Routers;
using HTTPTrafficFiddler.Senders;
using HTTPTrafficFiddler.Classes;
using HTTPTrafficFiddler.Interfaces;
using HTTPTrafficFiddler.Extensions;

namespace HTTPTrafficFiddler.Components
{
    class PacketDispatcher : IDisposable
    {
        public static PacketDispatcher Instance;

        public static void Create()
        {
            if (Instance == null)
            {
                Instance = new PacketDispatcher();
            }
        }

        private NetworkInterface iface;
        private String ifaceFilter;

        private bool started;

        private List<IPacketReader> packetReaders;
        private List<IPacketIntervalSender> packetSenders;
        private List<IPacketRouter> packetRouters;

        private ObservableCollection<IPacketFilter> packetFilters;
        
        private ConcurrentQueue<EthernetPacket> packetBuffer;

        private Thread networkDiscovery;
        private Thread intervalSender;
        private Thread packetProcessor;

        private ManualResetEvent packetsAvailable;

        private PacketDispatcher()
        {
            packetReaders = new List<IPacketReader>();
            packetSenders = new List<IPacketIntervalSender>();            
            packetRouters = new List<IPacketRouter>();

            packetFilters = new ObservableCollection<IPacketFilter>();

            packetReaders.Add(new ARPReader());
            packetSenders.Add(new ARPSender());
            packetRouters.Add(IPv4Router.Instance);

            packetBuffer = new ConcurrentQueue<EthernetPacket>();
            packetsAvailable = new ManualResetEvent(false);
        }

        public bool Bind(NetworkInterface networkInterface)
        {
            if (!networkInterface.HasIPv4Gateway)
            {
                return false;
            }

            iface = networkInterface;
            ifaceFilter = "ip || arp";

            return true;
        }

        public void AddPacketReader(IPacketReader reader)
        {
            packetReaders.Add(reader);
        }

        public void AddPacketSender(IPacketIntervalSender sender)
        {
            packetSenders.Add(sender);
        }

        public void AddPacketFilter(IPacketFilter filter)
        {
            lock (packetFilters)
            {
                packetFilters.Add(filter);
            }
        }

        public void RemovePacketFilter(IPacketFilter filter)
        {
            lock (packetFilters)
            {
                packetFilters.Remove(filter);
            }
        }

        public void ReplacePacketFilter(IPacketFilter currentFilter, IPacketFilter newFilter)
        {
            lock (packetFilters)
            {
                int index = packetFilters.IndexOf(currentFilter);

                packetFilters[index] = newFilter;
            }
        }

        public ObservableCollection<IPacketFilter> GetPacketFilters()
        {
            return packetFilters;
        }

        public NetworkInterface GetCurrentInterface()
        {
            return iface;
        }

        public void Start()
        {
            started = true;

            iface.PcapDevice.Open(OpenFlags.NoCaptureLocal | OpenFlags.Promiscuous, 1);
            iface.PcapDevice.Filter = ifaceFilter;
            iface.PcapDevice.StopCaptureTimeout = TimeSpan.FromMilliseconds(200);

            iface.PcapDevice.OnPacketArrival += OnPacketArrival;
            iface.PcapDevice.StartCapture();

            DebugInformation.WriteLine("Packet dispatcher (PD) started.");

            networkDiscovery = new Thread(new ThreadStart(NetworkDiscoveryProcessor));
            networkDiscovery.Name = "[PD] Network discovery";
            networkDiscovery.Start();

            packetProcessor = new Thread(new ThreadStart(PacketProcessor));
            packetProcessor.Name = "[PD] Packet processor";
            packetProcessor.Start();

            intervalSender = new Thread(new ThreadStart(IntervalSender));
            intervalSender.Name = "[PD] Packet interval sender";
            intervalSender.Start();
        }
        
        public void Stop()
        {
            if (!started) return;

            started = false;

            networkDiscovery.Join();
            packetProcessor.Join();
            intervalSender.Join();

            iface.PcapDevice.StopCapture();
            iface.PcapDevice.Close();

            DebugInformation.WriteLine("Packet dispatcher (PD) stopped.");            
        }

        public bool IsStarted()
        {
            return started;
        }

        private void OnPacketArrival(object sender, CaptureEventArgs e)
        {
            var packet = EthernetPacket.ParsePacket(LinkLayers.Ethernet, e.Packet.Data);

            if (packet == null) return;

            packetBuffer.Enqueue((EthernetPacket)packet);

            packetsAvailable.Set();
        }

        private void NetworkDiscoveryProcessor()
        {
            DebugInformation.WriteLine("[PD] Started sending network discovery packets.");

            NetworkDiscovery.SendPackets(iface);
        }

        private void PacketProcessor()
        {
            DebugInformation.WriteLine("[PD] Packet processor started.");

            var localPacketBuffer = new List<EthernetPacket>();
            var packetBytes = 0;

            while (started)
            {
                EthernetPacket rawPacket;                

                // wait if there haven't been any new packets put into queue
                packetsAvailable.WaitOne();

                // get packets from queue and add them to the local buffer
                while (packetBuffer.TryDequeue(out rawPacket) && rawPacket != null)
                {
                    localPacketBuffer.Add(rawPacket);
                   
                    packetBytes += rawPacket.BytesHighPerformance.BytesLength;

                    // add 16 bytes | TODO: find out why there are always 16 more bytes needed for each packet
                    packetBytes += 16;
                }

                packetsAvailable.Reset();
                
                var packetQueue = new SendQueue(packetBytes * packetRouters.Count);

                // loop through local packet buffer
                foreach (var packet in localPacketBuffer)
                {
                    // skip local packets
                    if (iface.IsLocalIPv4Packet(packet)) continue;

                    // send to packet readers
                    foreach (IPacketReader reader in packetReaders)
                    {
                        reader.ReadPacket(packet);
                    }

                    // send to packet filters, skip routing step for filtered packets
                    lock (packetFilters)
                    {
                        foreach (IPacketFilter filter in packetFilters)
                        {
                            if (filter.Enabled && filter.FilterPacket(packet)) continue;
                        }
                    }

                    // send to packet routers  
                    foreach (IPacketRouter router in packetRouters)
                    {
                        var routedPacket = router.RoutePacket(packet);

                        if (routedPacket != null)
                        {
                            packetQueue.Add(routedPacket.Bytes);
                        }
                    }
                }

                // send out all the routed packets
                packetQueue.TransmitAll(iface.PcapDevice);

                localPacketBuffer.Clear();
                packetBytes = 0;
            }

            DebugInformation.WriteLine("[PD] Packet processor stopped.");
        }

        private void IntervalSender()
        {
            DebugInformation.WriteLine("[PD] Interval sender started.");

            // initial sleep (give some time for responses to network discovery packets - ARP replies)
            Thread.Sleep(300 * (32 - iface.CIDR));

            var routingTable = IPv4Router.Instance.GetRoutingTable();

            // set static ARP entry for current gateway
            if (routingTable.ContainsKey(iface.IPv4Gateway))
            {
                DebugInformation.WriteLine("[ARP] set static entry for gateway - " + routingTable[iface.IPv4Gateway]);
                iface.SetStaticARPGateway(routingTable[iface.IPv4Gateway]);
            }
            // we don't have our gateway's MAC address - print an error
            else
            {
                DebugInformation.WriteLine("[ERR] unable to resolve gateway's MAC address");
                DebugInformation.WriteLine("[PD] Interval sender stopped.");

                StatusInformation.ChangeStatus("Status: traffic fiddler encountered an error (see debug information)!", false);
                
                return;
            }

            // start main loop for sending packets from "GetPacketQueue"
            while (started)
            {
                Thread.Sleep(1250);

                if (!started) break;

                Thread.Sleep(1250);

                if (!started) break;

                foreach (var sender in packetSenders)
                {
                    var sendQueue = sender.GetPacketQueue();

                    sendQueue.TransmitAll(iface.PcapDevice);
                }                
            }

            // send packets from "GetPacketQueueOnClose" for each sender (e.g. re-ARP poisoned targets) 
            foreach (var sender in packetSenders)
            {
                var sendQueue = sender.GetPacketQueueOnClose();

                sendQueue.TransmitAll(iface.PcapDevice);
            }   

            // delete static ARP entry for gateway's address
            iface.DeleteStaticARP(iface.IPv4Gateway);

            DebugInformation.WriteLine("[PD] Interval sender stopped.");
        }

        public void Dispose()
        {
            packetsAvailable.Dispose();
        }
    }
}
