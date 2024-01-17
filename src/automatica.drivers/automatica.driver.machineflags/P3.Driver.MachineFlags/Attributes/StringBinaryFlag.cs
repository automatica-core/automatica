﻿using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("P3.Driver.MachineFlags.Tests")]

namespace P3.Driver.MachineFlags.Attributes
{
    public class StringBinaryFlag : DriverNoneAttributeBase
    {

        private string _value;
        public StringBinaryFlag(IDriverContext ctx) : base(ctx)
        {

        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            await readContext.DispatchValue(_value, token);
            return true;
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            try
            {
                var sValue = value.ToString();

                if (_value == sValue)
                {
                    return Task.CompletedTask;
                }

                _value = sValue;
                DriverContext.Logger.LogDebug($"{DriverContext.NodeInstance.ObjId} {DriverContext.NodeInstance.Name}: WriteValue {sValue}");

                writeContext.DispatchValue(sValue, token);

                DriverContext.Dispatcher.DispatchValue(this, sValue);
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, $"Could not convert value to bool {ex}");
            }


            return Task.CompletedTask;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            throw new InvalidOperationException();
        }
    }
}
