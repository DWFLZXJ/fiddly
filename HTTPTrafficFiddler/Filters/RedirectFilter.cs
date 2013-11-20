using System;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

using PacketDotNet;

using HTTPTrafficFiddler.Classes;
using HTTPTrafficFiddler.Components;
using HTTPTrafficFiddler.Interfaces;

namespace HTTPTrafficFiddler.Filters
{
    public enum RedirectFilterType {
        URL, Keywords, Regex
    };

    public class RedirectFilter : IPacketFilter, INotifyPropertyChanged
    {
        public String Name { get; set; }        
        public String RedirectTarget { get; set; }        

        private RedirectFilterType _redirectType;
        private String _redirectString;
        private bool _enabled;

        private String[] splitKeywords;
        private Regex compiledRegex;

        public bool Enabled
        {
            get
            {
                return _enabled;
            }

            set
            {
                var packetFilters = PacketDispatcher.Instance.GetPacketFilters();

                if (packetFilters == null) return;

                lock (packetFilters)
                {
                    _enabled = value;
                }
            }
        }

        public RedirectFilterType RedirectType
        {
            get
            {
                return _redirectType;
            }
            set
            {
                _redirectType = value;

                // update RedirectString's internal helpers on type change (compiled regex etc.)
                if (RedirectString != null)
                {
                    RedirectString = RedirectString;
                }
            }
        }

        public String RedirectString {
            get
            {
                return _redirectString;
            }

            set
            {
                _redirectString = value;

                if (RedirectType == RedirectFilterType.Keywords)
                {
                    splitKeywords = _redirectString.Split(',');

                    // strip leading and trailing whitespace from keywords
                    for (int i = 0; i < splitKeywords.Length; i++)
                    {
                        splitKeywords[i] = splitKeywords[i].Trim();
                    }
                }
                else if (RedirectType == RedirectFilterType.Regex)
                {
                    compiledRegex = new Regex(_redirectString, RegexOptions.Compiled);
                }
            }
        }

        public String Description {
            get
            {
                return ToString();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged()
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Name"));
                PropertyChanged(this, new PropertyChangedEventArgs("Description"));
            }
        }

        public RedirectFilter()
        {
            RedirectType = RedirectFilterType.Keywords;
            RedirectString = String.Empty;
            RedirectTarget = String.Empty;

            Name = String.Empty;
            Enabled = false;            
        }

        public RedirectFilter(String name, bool enabled) : base()
        {
            Name = name;
            Enabled = enabled;
        }

        public bool FilterPacket(EthernetPacket ethernetPacket)
        {
            var iface = PacketDispatcher.Instance.GetCurrentInterface();

            if (ethernetPacket == null) return false;

            // perform basic checks to detect if we have a valid TCP packet
            if (ethernetPacket.Type != EthernetPacketType.IpV4) return false;

            var ipPacket = (IPv4Packet)ethernetPacket.PayloadPacket;

            if (ipPacket.NextHeader != IPProtocolType.TCP) return false;

            var tcpPacket = (TcpPacket)ipPacket.PayloadPacket;

            if (tcpPacket.DestinationPort != 80) return false;

            // get full TCP payload and try to parse it as a HTTP request
            var packetData = Encoding.UTF8.GetString(tcpPacket.PayloadData);
            var request = HttpRequest.TryParse(packetData);

            if (request == null) return false;

            // match current request against current filter's settings
            if (IsFilterMatch(request))
            {
                ethernetPacket = PrepareTCPResponse(ethernetPacket, GenerateHTTPRedirect(RedirectTarget));
                
                iface.SendPacket(ethernetPacket);

                DebugInformation.WriteLine(String.Format("[FILTER] \"{0}\" applied to client \"{1}\"", Name, ipPacket.DestinationAddress));

                return true;
            }

            return false;
        }

        private EthernetPacket PrepareTCPResponse(EthernetPacket ethernetPacket, String payload)
        {
            var ipPacket = (IPv4Packet)ethernetPacket.PayloadPacket;
            var tcpPacket = (TcpPacket)ipPacket.PayloadPacket;

            var sourceMAC = ethernetPacket.SourceHwAddress;
            var sourceIP = ipPacket.SourceAddress;

            ethernetPacket.SourceHwAddress = ethernetPacket.DestinationHwAddress;
            ethernetPacket.DestinationHwAddress = sourceMAC;

            ipPacket.SourceAddress = ipPacket.DestinationAddress;
            ipPacket.DestinationAddress = sourceIP;

            tcpPacket.DestinationPort = tcpPacket.SourcePort;
            tcpPacket.SourcePort = 80;

            uint seq = tcpPacket.SequenceNumber;

            tcpPacket.SequenceNumber = tcpPacket.AcknowledgmentNumber;
            tcpPacket.AcknowledgmentNumber = seq + (uint)tcpPacket.PayloadData.Length;

            // set TCP payload and update checksum
            byte[] payloadBytes = Encoding.ASCII.GetBytes(payload);
            tcpPacket.PayloadData = payloadBytes;
            tcpPacket.UpdateTCPChecksum();

            // update IPv4 header fields and update checksum 
            ipPacket.TotalLength = ipPacket.HeaderLength + tcpPacket.Bytes.Length;
            ipPacket.PayloadLength = (ushort)tcpPacket.Bytes.Length;
            ipPacket.UpdateIPChecksum();
            ipPacket.Checksum = ipPacket.CalculateIPChecksum();

            ethernetPacket.UpdateCalculatedValues();

            return ethernetPacket;
        }

        private bool IsFilterMatch(HttpRequest request)
        {
            var requestUrl = "http://" + request.Host + request.Path;

            if (requestUrl.Contains(RedirectTarget)) return false;

            if (RedirectType == RedirectFilterType.URL)
            {
                return RedirectString.Equals(requestUrl);
            }
            else if (RedirectType == RedirectFilterType.Keywords)
            {
                foreach (var keyword in splitKeywords)
                {
                    if (requestUrl.Contains(keyword)) return true;
                }
            }
            else if (RedirectType == RedirectFilterType.Regex)
            {
                if (compiledRegex.IsMatch(requestUrl)) return true;
            }

            return false;
        }

        private String GenerateHTTPRedirect(String url)
        {
            var redirect = new StringBuilder();

            redirect.Append("HTTP/1.1 302 Redirect\r\n");
            redirect.Append("Location: " + url + "\r\n");
            redirect.Append("Server: Apache/2.4.4 (Unix)\r\n");
            redirect.Append("Content-Length: 89\r\n\r\n");
            redirect.Append("<html><head><title>Document Moved</title></head><body><h1>Object moved</h1></body></html>");

            return redirect.ToString();
        }

        public void EditFilter()
        {
            var filterWindow = new RedirectFilterWindow(this);

            if ((bool)filterWindow.ShowDialog())
            {
                PacketDispatcher.Instance.ReplacePacketFilter(this, filterWindow.GetFilter());
            }

            NotifyPropertyChanged();
        }

        public override string ToString()
        {
            return String.Format("Redirect based on '{0}' to '{1}'.", RedirectType.ToString().ToLower(), RedirectTarget);
        }      
    }
}
