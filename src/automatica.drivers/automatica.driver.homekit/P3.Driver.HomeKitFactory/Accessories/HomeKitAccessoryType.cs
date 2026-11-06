using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using P3.Driver.HomeKit.Hap.Model;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.HomeKitFactory.Accessories
{
    /// <summary>
    /// Service the characteristic of a data point belongs to.
    /// </summary>
    internal enum HomeKitServiceTarget
    {
        Primary,
        Battery
    }

    /// <summary>
    /// A data point (child node) of a HomeKit accessory folder, mapped to one HAP characteristic.
    /// </summary>
    internal sealed class HomeKitDataPoint
    {
        private readonly Func<CharacteristicDefinition> _characteristic;

        public HomeKitDataPoint(string key, Guid templateGuid, string namePrefix, Func<CharacteristicDefinition> characteristic, NodeDataType dataType,
            bool mandatory, HomeKitServiceTarget target = HomeKitServiceTarget.Primary)
        {
            Key = key;
            TemplateGuid = templateGuid;
            NamePrefix = namePrefix;
            _characteristic = characteristic;
            DataType = dataType;
            Mandatory = mandatory;
            Target = target;
        }

        public string Key { get; }
        public Guid TemplateGuid { get; }
        public string NamePrefix { get; }
        public NodeDataType DataType { get; }

        /// <summary>
        /// Mandatory characteristics are always part of the service, their nodes are created by default.
        /// </summary>
        public bool Mandatory { get; }

        public HomeKitServiceTarget Target { get; }

        public string NameKey => $"{NamePrefix}.NAME";
        public string DescriptionKey => $"{NamePrefix}.DESCRIPTION";

        public CharacteristicDefinition Characteristic => _characteristic();

        /// <summary>
        /// HomeKit can write the characteristic, the value is dispatched to Automatica (readable node).
        /// </summary>
        public bool IsReadable => Characteristic.CanWrite;

        /// <summary>
        /// HomeKit shows the value of the characteristic, Automatica writes it (writeable node).
        /// </summary>
        public bool IsWriteable => Characteristic.CanRead;
    }

    /// <summary>
    /// A HomeKit accessory with one primary HAP service, created as a folder node with one child node per characteristic.
    /// </summary>
    internal sealed class HomeKitAccessoryType
    {
        private readonly Func<NodeInstance, IEnumerable<CharacteristicDefinition>> _hiddenCharacteristics;

        public HomeKitAccessoryType(string key, string namePrefix, Guid interfaceGuid, string serviceType, IReadOnlyList<HomeKitDataPoint> dataPoints,
            Func<NodeInstance, IEnumerable<CharacteristicDefinition>> hiddenCharacteristics = null)
        {
            Key = key;
            NamePrefix = namePrefix;
            InterfaceGuid = interfaceGuid;
            ServiceType = serviceType;
            DataPoints = dataPoints;
            _hiddenCharacteristics = hiddenCharacteristics;
        }

        /// <summary>
        /// Key of the folder node template
        /// </summary>
        public string Key { get; }

        public string NamePrefix { get; }
        public Guid InterfaceGuid { get; }
        public string ServiceType { get; }
        public IReadOnlyList<HomeKitDataPoint> DataPoints { get; }

        public string NameKey => $"{NamePrefix}.NAME";
        public string DescriptionKey => $"{NamePrefix}.DESCRIPTION";

        public HomeKitDataPoint GetDataPoint(string key) => DataPoints.SingleOrDefault(a => a.Key == key);

        /// <summary>
        /// Mandatory characteristics that are not mapped to a node (e.g. temperature display units)
        /// </summary>
        public IEnumerable<CharacteristicDefinition> HiddenCharacteristics(NodeInstance folder) =>
            _hiddenCharacteristics?.Invoke(folder) ?? Enumerable.Empty<CharacteristicDefinition>();

        public Accessory CreateAccessory(ulong aid, string name, string manufacturer, string serial, NodeInstance folder)
        {
            var mandatory = DataPoints
                .Where(a => a.Mandatory && a.Target == HomeKitServiceTarget.Primary)
                .Select(a => a.Characteristic)
                .Concat(HiddenCharacteristics(folder))
                .ToArray();

            return AccessoryFactory.CreateServiceAccessory(aid, name, manufacturer, serial, ServiceType, mandatory);
        }

        public static Characteristic GetCharacteristic(Accessory accessory, HomeKitDataPoint dataPoint)
        {
            var service = dataPoint.Target == HomeKitServiceTarget.Battery ? accessory.GetOrAddBatteryService() : accessory.Specific;
            return service.GetOrAddCharacteristic(dataPoint.Characteristic);
        }
    }
}
