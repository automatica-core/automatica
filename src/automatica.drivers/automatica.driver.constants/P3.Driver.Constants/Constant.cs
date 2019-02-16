﻿using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Driver;

[assembly: InternalsVisibleTo("P3.Driver.Constants.Tests")]

namespace P3.Driver.Constants
{
    public class Constant : DriverBase
    {
        // the timer is used to sent the value every 10 sec
        private readonly Timer _dispatchTimer = new Timer();
        private double? _value = null;

        internal double? Value => this._value;

        public Constant(IDriverContext ctx) : base(ctx)
        {
            _dispatchTimer.Interval = 10000;
            _dispatchTimer.Elapsed += _dispatchTimer_Elapsed;

        }

        // timer elapsed method
        private void _dispatchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_value.HasValue)
            {
                // dispatch the value
                DispatchValue(_value);
            }
        }

        public override bool Init()
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
                _value = Math.PI *2;
            }
            else
            {
                var prop = GetPropertyValueInt("const_value"); // get the value property
                _value = Convert.ToDouble(prop);
            }
            return base.Init();
        }

        // only called once
        public override Task<bool> Start()
        {
            _dispatchTimer.Start();

            // initially dispatch the value on satrt
            DispatchValue(_value);

            return base.Start();
        }
        
        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null; //we have no more children, therefore return null
        }
    }
}
