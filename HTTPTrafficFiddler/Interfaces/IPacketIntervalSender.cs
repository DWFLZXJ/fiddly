using SharpPcap.WinPcap;

namespace HTTPTrafficFiddler.Interfaces
{
    interface IPacketIntervalSender
    {
        SendQueue GetPacketQueue();
        SendQueue GetPacketQueueOnClose();
    }
}
