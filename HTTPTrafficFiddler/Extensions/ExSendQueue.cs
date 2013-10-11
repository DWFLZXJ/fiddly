using SharpPcap.WinPcap;

namespace HTTPTrafficFiddler.Extensions
{
    static class ExSendQueue
    {
        /// <summary>
        /// A variation to original "Transmit" method accepting "null" queues.
        /// </summary>
        public static void TransmitAll(this SendQueue sendQueue, WinPcapDevice device)
        {
            if (sendQueue == null) return;

            sendQueue.Transmit(device, SendQueueTransmitModes.Normal);
            sendQueue.Dispose();
        }
    }
}
