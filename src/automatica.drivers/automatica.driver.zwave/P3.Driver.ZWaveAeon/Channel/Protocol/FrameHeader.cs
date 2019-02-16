namespace P3.Driver.ZWaveAeon.Channel.Protocol
{
    enum FrameHeader : byte
    {
        /// <summary>
        /// Start of frame
        /// </summary>
        SOF = 0x01,
        /// <summary>
        /// Acknowledge
        /// </summary>
        ACK = 0x06,
        /// <summary>
        /// Negative acknowledge
        /// </summary>
        NAK = 0x15,
        /// <summary>
        /// Cancel
        /// </summary>
        CAN = 0x18
    }
}
