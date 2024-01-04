using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.EF.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Model;

namespace Automatica.Core.Driver.Monitor
{ 
    /// <summary>
    /// Implements the <see cref="ITelegramMessage"/>
    /// </summary>
    public class TelegramMessage : TypedObject, ITelegramMessage
    {
        public TelegramMessage(Guid busId, TelegramDirection direction, string sourceAddress, string targetAddress, string hexData, string additionalMessageString)
        {
            BusId = busId;
            Direction = direction;
            SourceAddress = sourceAddress;
            TargetAddress = targetAddress;
            Data =  hexData;
            AdditionalMessageString = additionalMessageString;
            TimeStamp = DateTimeHelper.ProviderInstance.GetLocalNow().DateTime;
        }

        public Guid BusId { get; set; }
        public TelegramDirection Direction { get; }
        public string SourceAddress { get; }
        public string TargetAddress { get; }
        public string Data { get; }
        public string AdditionalMessageString { get; }

        public DateTime TimeStamp { get; }
    }

    /// <summary>
    /// <see cref="TelegramMonitorInstance"/>
    /// </summary>
    public class TelegramMonitorInstance : TypedObject
    {
        public TelegramMonitorInstance(Guid id, string name, string busType, string description)
        {
            Id = id;
            Name = name;
            BusType = busType;
            Description = description;
        }

        public Guid Id { get; }
        public string Name { get; }
        public string BusType { get; }
        public string Description { get; }
    }

    /// <summary>
    /// Used for sending data packets to the UI
    /// </summary>
    public interface ITelegramMonitor
    {
        /// <summary>
        /// Create a new instance of <see cref="ITelegramMonitorInstance"/>
        /// </summary>
        /// <param name="id">A unique <see cref="Guid"/> for the instance</param>
        /// <param name="name">The name shown in the UI</param>
        /// <param name="type">The type shwon in the UI</param>
        /// <param name="description">The description shown in the UI</param>
        /// <returns>New instance of <see cref="ITelegramMonitorInstance"/></returns>
        ITelegramMonitorInstance CreateTelegramMonitor(Guid id, string name, string type, string description);

        /// <summary>
        /// Creates a new instance of <see cref="ITelegramMonitorInstance"/>
        /// </summary>
        /// <param name="instance">ProviderInstance</param>
        /// <param name="type">The type shown in the UI</param>
        /// <returns></returns>
        ITelegramMonitorInstance CreateTelegramMonitor(NodeInstance instance, string type);

        /// <summary>
        /// Gets all instances
        /// </summary>
        /// <returns>Returns all <see cref="TelegramMonitorInstance"/> instances</returns>
        IList<TelegramMonitorInstance> GetMonitorInstances();

        /// <summary>
        /// Send a instance of <see cref="ITelegramMessage"/> to the UI
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task NotifyTelegram(ITelegramMessage message);

        /// <summary>
        /// Clears all <see cref="ITelegramMonitorInstance"/> instances
        /// </summary>
        void Clear();
    }
}
