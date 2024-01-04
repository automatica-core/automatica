using System;

namespace Automatica.Core.Base.TelegramMonitor
{

    /// <summary>
    /// Direction of a telegram
    /// </summary>
    public enum TelegramDirection
    {
        Input,
        Output
    }

    /// <summary>
    /// Interface for a <see cref="ITelegramMessage"/>
    /// </summary>
    public interface ITelegramMessage
    {
        /// <summary>
        /// ProviderInstance <see cref="Guid"/> for the message
        /// </summary>
        Guid BusId { get; set; }

        /// <summary>
        /// Direction of the telegram
        /// </summary>
        TelegramDirection Direction { get; }

        /// <summary>
        /// SourceAddress of the telegram
        /// </summary>
        string SourceAddress { get; }

        /// <summary>
        /// TargetAddress of the telegram
        /// </summary>
        string TargetAddress { get; }
        
        /// <summary>
        /// Hex encoded data of the telgram
        /// </summary>
        string Data { get; }

        /// <summary>
        /// Additional message of the telgram (eg. for debugging)
        /// </summary>
        string AdditionalMessageString { get; }
    }
}
