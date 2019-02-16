﻿using System;
using P3.Driver.ZWaveAeon.CommandClasses;

namespace P3.Driver.ZWaveAeon.Devices.Vision
{
    public class ShockSensor : BatteryDevice
    {
        public event EventHandler<EventArgs> ShockDetected;
        public event EventHandler<EventArgs> ShockCancelled;
        public event EventHandler<EventArgs> TamperDetected;
        public event EventHandler<EventArgs> TamperCancelled;

        public ShockSensor(Node node)
            : base(node)
        {
            node.GetCommandClass<Basic>().Changed += Basic_Changed;
            node.GetCommandClass<Alarm>().Changed += Alarm_Changed;
        }

        private void Basic_Changed(object sender, ReportEventArgs<BasicReport> e)
        {
            if (e.Report.Value == 0x00)
            {
                OnShockCancelled(EventArgs.Empty);
                return;
            }
            if (e.Report.Value == 0xFF)
            {
                OnShockDetected(EventArgs.Empty);
                return;
            }
        }

        protected virtual void OnShockDetected(EventArgs e)
        {
            ShockDetected?.Invoke(this, e);
        }

        protected virtual void OnShockCancelled(EventArgs e)
        {
            ShockCancelled?.Invoke(this, e);
        }

        private void Alarm_Changed(object sender, ReportEventArgs<AlarmReport> e)
        {
            if (e.Report.Detail == AlarmDetailType.TamperingProductCoveringRemoved)
            {
                if (e.Report.Level == 0x00)
                {
                    OnTamperCancelled(EventArgs.Empty);
                    return;
                }
                if (e.Report.Level == 0xFF)
                {
                    OnTamperDetected(EventArgs.Empty);
                    return;
                }
            }
            if (e.Report.Type == AlarmType.Burglar)
            {
                if (e.Report.Level == 0x00)
                {
                    OnShockCancelled(EventArgs.Empty);
                    return;
                }
                if (e.Report.Level == 0xFF)
                {
                    OnShockDetected(EventArgs.Empty);
                    return;
                }
            }
        }

        protected virtual void OnTamperDetected(EventArgs e)
        {
            TamperDetected?.Invoke(this, e);
        }

        protected virtual void OnTamperCancelled(EventArgs e)
        {
            TamperCancelled?.Invoke(this, e);
        }
    }
}
