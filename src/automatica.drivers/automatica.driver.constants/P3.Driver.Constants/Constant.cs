using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Driver;
using Timer = System.Timers.Timer;

[assembly: InternalsVisibleTo("P3.Driver.Constants.Tests")]

namespace P3.Driver.Constants
{
    public class Constant : DriverBase
    {
        // the timer is used to sent the value every 10 sec
        private readonly Timer _dispatchTimer = new Timer();
        private object _value = null;


        public Constant(IDriverContext ctx) : base(ctx)
        {
            _dispatchTimer.Interval = 10000;

        }

        public object Value => _value;

        public override Task<bool> Read(CancellationToken token = default)
        {
            DispatchValue(_value);
            return Task.FromResult(true);
        }

        // timer elapsed method
        private void _dispatchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_value != null)
            {
                // dispatch the value
                DispatchValue(_value);
            }
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            if (DriverContext.NodeInstance.This2NodeTemplateNavigation.Key == "const_pi")
            {
                _value = Math.PI;
            }
            else if (DriverContext.NodeInstance.This2NodeTemplateNavigation.Key == "const_halfpi")
            {
                _value = Math.PI / 2;
            }
            else if (DriverContext.NodeInstance.This2NodeTemplateNavigation.Key == "const_doublepi")
            {
                _value = Math.PI * 2;
            }
            else if (DriverContext.NodeInstance.This2NodeTemplateNavigation.Key == "const_true")
            {
                _value = true;
            }
            else if (DriverContext.NodeInstance.This2NodeTemplateNavigation.Key == "const_false")
            {
                _value = false;
            }
            else if (DriverContext.NodeInstance.This2NodeTemplateNavigation.Key == "const_string")
            {
                var prop = GetPropertyValueString("const_value"); // get the value property
                _value = prop;
            }
            else
            {
                var prop = GetPropertyValueInt("const_value"); // get the value property
                _value = Convert.ToDouble(prop);
            }
            return base.Init(token);
        }

        // only called once
        public override Task<bool> Start(CancellationToken token = default)
        {
            _dispatchTimer.Start();
            _dispatchTimer.Elapsed += _dispatchTimer_Elapsed;

            // initially dispatch the value on start
            DispatchValue(_value);

            return base.Start(token);
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            _dispatchTimer.Elapsed -= _dispatchTimer_Elapsed;
            _dispatchTimer.Stop();

            _dispatchTimer.Dispose();

            return base.Stop(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null; //we have no more children, therefore return null
        }
    }
}
