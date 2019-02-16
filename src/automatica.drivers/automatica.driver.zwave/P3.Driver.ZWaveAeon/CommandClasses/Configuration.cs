﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using P3.Driver.ZWaveAeon.Channel;

namespace P3.Driver.ZWaveAeon.CommandClasses
{
    public class Configuration : CommandClassBase
    {
        enum command : byte
        {
            Set = 0x04,
            Get = 0x05,
            Report = 0x06
        }

        public Configuration(Node node) : base(node, CommandClass.Configuration)
        {
        }

        public Task<ConfigurationReport> Get(byte parameter)
        {
            return Get(parameter, CancellationToken.None);
        }

        public async Task<ConfigurationReport> Get(byte parameter, CancellationToken cancellationToken)
        {
            var response = await Channel.Send(Node, new Command(Class, command.Get, parameter), command.Report, cancellationToken);
            return new ConfigurationReport(Node, response);
        }

        public Task Set(byte parameter, object value, byte size)
        {
            return Set(parameter, value, size, CancellationToken.None);
        }

        public async Task Set(byte parameter, object value, byte size, CancellationToken cancellationToken)
        {
            var int64 = Convert.ToInt64(value);
            await Set(parameter, int64, true, size, cancellationToken);
        }

        public Task Set(byte parameter, sbyte value)
        {
            return Set(parameter, value, CancellationToken.None);
        }

        public async Task Set(byte parameter, sbyte value, CancellationToken cancellationToken)
        {
            await Set(parameter, value, true, 0, cancellationToken);
        }

        public Task Set(byte parameter, byte value)
        {
            return Set(parameter, value, CancellationToken.None);
        }

        public async Task Set(byte parameter, byte value, CancellationToken cancellationToken)
        {
            await Set(parameter, value, false, 0, cancellationToken);
        }

        public Task Set(byte parameter, short value)
        {
            return Set(parameter, value, CancellationToken.None);
        }

        public async Task Set(byte parameter, short value, CancellationToken cancellationToken)
        {
            await Set(parameter, value, true, 0, cancellationToken);
        }

        public Task Set(byte parameter, ushort value)
        {
            return Set(parameter, value, CancellationToken.None);
        }

        public async Task Set(byte parameter, ushort value, CancellationToken cancellationToken)
        {
            await Set(parameter, value, false, 0, cancellationToken);
        }

        public Task Set(byte parameter, int value)
        {
            return Set(parameter, value, CancellationToken.None);
        }

        public async Task Set(byte parameter, int value, CancellationToken cancellationToken)
        {
            await Set(parameter, value, true, 0, cancellationToken);
        }

        public Task Set(byte parameter, uint value)
        {
            return Set(parameter, value, CancellationToken.None);
        }

        public async Task Set(byte parameter, uint value, CancellationToken cancellationToken)
        {
            await Set(parameter, value, false, 0, cancellationToken);
        }

        public Task Set(byte parameter, long value)
        {
            return Set(parameter, value, CancellationToken.None);
        }

        public async Task Set(byte parameter, long value, CancellationToken cancellationToken)
        {
            await Set(parameter, value, true, 0, cancellationToken);
        }

        public Task Set(byte parameter, ulong value)
        {
            return Set(parameter, value, CancellationToken.None);
        }

        public async Task Set(byte parameter, ulong value, CancellationToken cancellationToken)
        {
            await Set(parameter, (long)value, false, 0, cancellationToken);
        }

        private async Task Set(byte parameter, long value, bool signed, byte size, CancellationToken cancellationToken)
        {
            if (size == 0)
            {
                // extra roundtrip to get the correct size of the parameter
                var response = await Channel.Send(Node, new Command(Class, command.Get, parameter), command.Report, cancellationToken);
                size = response[1];
            }

            var values = default(byte[]);
            switch(size)
            {
                case 1:
                    values = signed ? PayloadConverter.GetBytes((sbyte)value) : PayloadConverter.GetBytes((byte)value);
                    break;
                case 2:
                    values = signed ? PayloadConverter.GetBytes((short)value) : PayloadConverter.GetBytes((ushort)value);
                    break;
                case 4:
                    values = signed ? PayloadConverter.GetBytes((int)value) : PayloadConverter.GetBytes((uint)value);
                    break;
                case 8:
                    values = signed ? PayloadConverter.GetBytes((long)value) : PayloadConverter.GetBytes((ulong)value);
                    break;
                default:
                    throw new NotSupportedException($"Size:{size} is not supported");
            }
            await Channel.Send(Node, new Command(Class, command.Set, new[] { parameter, (byte)values.Length }.Concat(values).ToArray()), cancellationToken);
        }
    }
}
