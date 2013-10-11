using System;

using PacketDotNet;

namespace HTTPTrafficFiddler.Interfaces
{
    public interface IPacketFilter
    {
        String Name
        {
            get;
            set;
        }

        String Description
        {
            get;
        }

        bool Enabled
        {
            get;
            set;
        }

        void EditFilter();

        bool FilterPacket(EthernetPacket ethernetPacket);        
    }
}
