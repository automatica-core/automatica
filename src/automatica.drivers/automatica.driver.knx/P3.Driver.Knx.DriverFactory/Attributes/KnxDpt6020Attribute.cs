using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Knx.Falcon;
using Microsoft.Extensions.Logging;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    /// <summary>
    /// Child of a DPT 6.020 node: one of the status flags A-E (true = set) or the active mode (0, 1 or 2).
    /// </summary>
    public class KnxDpt6020ValueAttribute : DriverBase
    {
        private readonly KnxDpt6020Attribute _parent;

        /// <summary>
        /// 0-4 for the status flags A-E, <see cref="KnxDpt6020Attribute.ModeIndex"/> for the active mode
        /// </summary>
        public int Index { get; }

        public bool IsMode => Index == KnxDpt6020Attribute.ModeIndex;

        /// <summary>
        /// bool for a status flag, int for the active mode
        /// </summary>
        public object Value { get; set; }

        public KnxDpt6020ValueAttribute(IDriverContext driverContext, KnxDpt6020Attribute parent, int index) : base(driverContext)
        {
            _parent = parent;
            Index = index;
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            object newValue;
            if (IsMode)
            {
                var mode = Convert.ToInt32(value);
                if (mode is < 0 or > 2)
                {
                    DriverContext.Logger.LogWarning($"{_parent.GroupAddress}: mode {value} is out of range (0..2), ignore write");
                    return;
                }
                newValue = mode;
            }
            else
            {
                newValue = Convert.ToBoolean(value);
            }

            if (Equals(newValue, Value))
            {
                await writeContext.DispatchValue(newValue, token);
                return;
            }

            // the parent encodes the state of all children, so the new value must be set before writing
            var oldValue = Value;
            Value = newValue;

            try
            {
                await _parent.WriteInternal(newValue, writeContext, token); // dispatches the value on success
            }
            catch
            {
                Value = oldValue;
                throw;
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            if (DriverContext.NodeInstance.IsReadable)
            {
                await _parent.ReadFromBus();
            }

            return false;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }

    /// <summary>
    /// DPT 6.020 status with mode (KNX 3.7.2): AAAAA = status flags A-E in bit 7-3 (0 = set, 1 = clear), FFF = active mode in bit 2-0 (001 = mode 0, 010 = mode 1, 100 = mode 2).
    /// Falcon has no codec for it, the node encodes the octet itself.
    /// </summary>
    public class KnxDpt6020Attribute : KnxGroupAddress
    {
        internal const int ModeIndex = 5;
        internal const string KeyPrefix = "knx-dpt6.020.";
        private static readonly string[] ChildKeys = { "A", "B", "C", "D", "E", "ACTIVE_MODE" };

        private readonly List<KnxDpt6020ValueAttribute> _children = new List<KnxDpt6020ValueAttribute>();

        public override int ImplementationDptType => (int)P3.Knx.Core.Driver.DptType.Dpt6_020;

        public KnxDpt6020Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        internal static byte Encode(bool[] flags, int mode)
        {
            var data = 0;
            for (var i = 0; i < 5; i++)
            {
                if (!flags[i])
                {
                    data |= 1 << (7 - i); // 1 = clear
                }
            }

            return (byte)(data | (1 << mode));
        }

        /// <returns>the status flags A-E (true = set) and the active mode, null if the mode field is not valid</returns>
        internal static (bool[] Flags, int? Mode) Decode(byte data)
        {
            var flags = new bool[5];
            for (var i = 0; i < 5; i++)
            {
                flags[i] = (data & (1 << (7 - i))) == 0;
            }

            int? mode = (data & 0x07) switch
            {
                0x01 => 0,
                0x02 => 1,
                0x04 => 2,
                _ => null
            };

            return (flags, mode);
        }

        protected override object ConvertToDptValue(object value)
        {
            var flags = new bool[5];
            foreach (var child in _children.Where(a => !a.IsMode))
            {
                flags[child.Index] = child.Value as bool? ?? false;
            }

            var mode = _children.FirstOrDefault(a => a.IsMode)?.Value as int? ?? 0;
            return Encode(flags, mode);
        }

        protected override GroupValue ToGroupValue(object dptValue)
        {
            return new GroupValue(new[] { (byte)dptValue });
        }

        protected override object FromGroupValue(GroupValue groupValue)
        {
            var data = groupValue.Value;
            if (data == null || data.Length != 1)
            {
                throw new FormatException("DPT 6.020 expects 1 byte");
            }

            return Decode(data[0]);
        }

        protected override bool ValueRead(object value)
        {
            if (value is ValueTuple<bool[], int?> decoded)
            {
                foreach (var child in _children)
                {
                    object childValue = child.IsMode ? decoded.Item2 : decoded.Item1[child.Index];
                    if (childValue == null)
                    {
                        DriverContext.Logger.LogWarning($"{GroupAddress}: invalid mode field in DPT 6.020 value");
                        continue;
                    }

                    child.Value = childValue;
                    child.DispatchRead(childValue);
                }
            }

            return false; // the folder node itself has no value
        }

        internal Task WriteInternal(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            return Write(value, writeContext, token);
        }

        internal Task<bool> ReadFromBus()
        {
            return Driver.Read(GroupAddress);
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(false);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation.Key;
            var index = key.StartsWith(KeyPrefix, StringComparison.Ordinal) ? Array.IndexOf(ChildKeys, key.Substring(KeyPrefix.Length)) : -1;
            if (index < 0)
            {
                return null;
            }

            var att = new KnxDpt6020ValueAttribute(ctx, this, index);
            _children.Add(att);
            return att;
        }
    }
}
