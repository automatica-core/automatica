using System;
using System.Globalization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKitFactory.NodeInstances.Nodes
{
    /// <summary>
    /// Node for a characteristic of a <see cref="Accessories.HomeKitAccessoryType"/>, converts between the node data type and the HAP format.
    /// </summary>
    internal class CharacteristicNode : BaseNode
    {
        private readonly CharacteristicDefinition _definition;
        private readonly NodeDataType _dataType;

        public CharacteristicNode(IDriverContext driverContext, Characteristic characteristic, HomeKitDriver driver, CharacteristicDefinition definition,
            NodeDataType dataType) : base(driverContext, characteristic, driver)
        {
            _definition = definition;
            _dataType = dataType;
        }

        /// <summary>
        /// Value written by HomeKit
        /// </summary>
        internal override void SetValue(object value)
        {
            if (value == null)
            {
                return;
            }

            base.SetValue(ToNodeValue(value, _dataType));
        }

        /// <summary>
        /// Value written by Automatica
        /// </summary>
        protected override object ConvertValue(object value)
        {
            return _definition.ConvertValue(value);
        }

        protected override System.Threading.Tasks.Task Write(object value, IWriteContext writeContext, System.Threading.CancellationToken token = default)
        {
            try
            {
                return base.Write(value, writeContext, token);
            }
            catch (Exception e) when (e is FormatException or InvalidCastException or ArgumentOutOfRangeException or OverflowException)
            {
                DriverContext.Logger.LogWarning($"{Name}: value {value} is not valid for HomeKit characteristic {_definition.Type}: {e.Message}");
                return System.Threading.Tasks.Task.CompletedTask;
            }
        }

        internal static object ToNodeValue(object value, NodeDataType dataType)
        {
            switch (dataType)
            {
                case NodeDataType.Boolean:
                    return value is bool b ? b : Convert.ToDouble(value, CultureInfo.InvariantCulture) != 0;
                case NodeDataType.Integer:
                    return value is bool bi ? (bi ? 1 : 0) : Convert.ToInt32(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                case NodeDataType.Double:
                    return Convert.ToDouble(value, CultureInfo.InvariantCulture);
                default:
                    return value;
            }
        }
    }

    /// <summary>
    /// Stateless programmable switch: every written value is sent as event, reading the characteristic returns null afterwards.
    /// </summary>
    internal sealed class ProgrammableSwitchEventNode : CharacteristicNode
    {
        public ProgrammableSwitchEventNode(IDriverContext driverContext, Characteristic characteristic, HomeKitDriver driver, CharacteristicDefinition definition,
            NodeDataType dataType) : base(driverContext, characteristic, driver, definition, dataType)
        {
        }

        protected override async System.Threading.Tasks.Task Write(object value, IWriteContext writeContext, System.Threading.CancellationToken token = default)
        {
            await base.Write(value, writeContext, token);
            Characteristic.Value = null;
        }
    }
}
