﻿using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("P3.Driver.MachineFlags.Tests")]

namespace P3.Driver.MachineFlags.Attributes
{
    public class DoubleBinaryFlag : DriverNoneAttributeBase
    {

        private double? _value;
        public DoubleBinaryFlag(IDriverContext ctx) : base(ctx)
        {

        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            try
            {
                var dValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (_value == dValue)
                {
                    return Task.CompletedTask;
                }
                _value = dValue;

                DriverContext.Logger.LogDebug($"WriteValue {dValue}");

                writeContext.DispatchValue(dValue, token);
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
