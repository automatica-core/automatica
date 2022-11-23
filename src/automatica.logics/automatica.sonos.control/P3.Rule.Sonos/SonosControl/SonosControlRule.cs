using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Rule.Sonos.SonosControl;

public class SonosControlRule : Automatica.Core.Rule.Rule
{
    private readonly IRuleContext _context;

    //Inputs
    private readonly RuleInterfaceInstance _playPauseTrigger;
    private readonly RuleInterfaceInstance _playPauseInputState;
    private readonly RuleInterfaceInstance _volume;
    private readonly RuleInterfaceInstance _volumeIncrement;
    private readonly RuleInterfaceInstance _volumeDecrement;
    private readonly RuleInterfaceInstance _next;
    private readonly RuleInterfaceInstance _radioStationInput;

    //Params
    private readonly RuleInterfaceInstance _volumeOnPlay;
    private readonly RuleInterfaceInstance _radioStation;
    private readonly RuleInterfaceInstance _maxVolume;

    //Outputs
    private readonly RuleInterfaceInstance _playOutputStatus;
    private readonly RuleInterfaceInstance _pauseOutputStatus;
    private readonly RuleInterfaceInstance _volumeOutputStatus;
    private readonly RuleInterfaceInstance _radioStationOutputValue;
    private readonly RuleInterfaceInstance _nextOutput;


    private long _volumeOnPlayValue;
    private long _radioStationValue;
    private long _maxVolumeValue;

    private long _currentVolume;
    private bool _currentlyPlaying = false;

    public SonosControlRule(IRuleContext context) : base(context)
    {
        _context = context;

        //Inputs
        _playPauseTrigger = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlRuleFactory.PlayPauseTrigger);
        _playPauseInputState = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlRuleFactory.PauseTrigger);
        _volume = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlRuleFactory.Volume);
        _volumeIncrement = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlRuleFactory.VolumeIncrement);
        _volumeDecrement = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlRuleFactory.VolumeDecrement);
        _next = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlRuleFactory.Next);
        _radioStationInput = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlRuleFactory.RadioStationInput);

        //Params
        _volumeOnPlay = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlRuleFactory.VolumeOnPlay);
        _radioStation = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlRuleFactory.RadioStation);
        _maxVolume = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlRuleFactory.MaxVolume);


        //Outputs
        _playOutputStatus = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlRuleFactory.PlayOutputStatus);
        _pauseOutputStatus = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlRuleFactory.PauseOutputStatus);
        _volumeOutputStatus = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlRuleFactory.VolumeOutputStatus);
        _radioStationOutputValue = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlRuleFactory.RadioStationOutputValue);
        _nextOutput = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlRuleFactory.NextOutput);

    }

    protected override void ParamterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
    {
        if (instance.This2RuleInterfaceTemplate == _volumeOnPlay.This2RuleInterfaceTemplate)
        {
            _volumeOnPlayValue = Convert.ToInt64(value);
        }
        if (instance.This2RuleInterfaceTemplate == _radioStation.This2RuleInterfaceTemplate)
        {
            _radioStationValue = Convert.ToInt64(value);
        }
        if (instance.This2RuleInterfaceTemplate == _maxVolume.This2RuleInterfaceTemplate)
        {
            _maxVolumeValue = Convert.ToInt64(value);
        }

        base.ParamterValueChanged(instance, source, value);
    }

    public override Task<bool> Start()
    {
        _volumeOnPlayValue = Context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == _volumeOnPlay.This2RuleInterfaceTemplate)!.ValueInteger!.Value;

        _radioStationValue = Context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == _radioStation.This2RuleInterfaceTemplate)!.ValueInteger!.Value;
        
        _maxVolumeValue = Context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == _maxVolume.This2RuleInterfaceTemplate)!.ValueInteger!.Value;

        return base.Start();
    }


    protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
    {
        var ret = new List<IRuleOutputChanged>();

        if (instance.This2RuleInterfaceTemplate == _volume.This2RuleInterfaceTemplate)
        {
            _currentVolume = Convert.ToInt64(value);
        }

        if (instance.This2RuleInterfaceTemplate == _playPauseTrigger.This2RuleInterfaceTemplate)
        {
            var play = Convert.ToBoolean(value);

            if (play)
            {
                _currentVolume = _volumeOnPlayValue;
                _currentlyPlaying = true;

                ret.Add(new RuleOutputChanged(_playOutputStatus, true)); 
                ret.Add(new RuleOutputChanged(_volumeOutputStatus, _currentVolume));
                ret.Add(new RuleOutputChanged(_radioStationOutputValue, _radioStationValue));
                ret.Add(new RuleOutputChanged(_pauseOutputStatus, false));
            }
            else
            {
                ret.Add(new RuleOutputChanged(_playOutputStatus, false));
                ret.Add(new RuleOutputChanged(_pauseOutputStatus, true));
                ret.Add(new RuleOutputChanged(_radioStationOutputValue, null));
            }
        }
        if (instance.This2RuleInterfaceTemplate == _playPauseInputState.This2RuleInterfaceTemplate)
        {
            _currentlyPlaying = Convert.ToBoolean(value);
        }

        if (instance.This2RuleInterfaceTemplate == _volumeIncrement.This2RuleInterfaceTemplate)
        {
            if (_currentlyPlaying)
            {
                if (_currentVolume + 1 >= _maxVolumeValue)
                {
                    return ret;
                }

                _currentVolume++;
                ret.Add(new RuleOutputChanged(_volumeOutputStatus, _currentVolume));
            }
        }

        if (instance.This2RuleInterfaceTemplate == _volumeDecrement.This2RuleInterfaceTemplate)
        {
            if (_currentlyPlaying)
            {
                if (_currentVolume - 1 > 0)
                {
                    return ret;
                }

                _currentVolume--;
                ret.Add(new RuleOutputChanged(_volumeOutputStatus, _currentVolume));
            }
        }
        if (instance.This2RuleInterfaceTemplate == _volume.This2RuleInterfaceTemplate)
        {
            if (_currentlyPlaying)
            {
                var volume = Convert.ToInt64(value);
                if (volume >= _maxVolumeValue)
                {
                    return ret;
                }

                _currentVolume = volume;
                ret.Add(new RuleOutputChanged(_volumeOutputStatus, _currentVolume));
            }
        }

        if (instance.This2RuleInterfaceTemplate == _next.This2RuleInterfaceTemplate)
        {
            if (_currentlyPlaying)
            {
                Context.Dispatcher.DispatchValue(new RuleOutputChanged(_nextOutput, true).Instance, true);
                Thread.Sleep(100);
                Context.Dispatcher.DispatchValue(new RuleOutputChanged(_nextOutput, false).Instance, false);
            }
        }

        if (instance.This2RuleInterfaceTemplate == _radioStationInput.This2RuleInterfaceTemplate)
        {
            ret.Add(new RuleOutputChanged(_radioStationOutputValue, _radioStationValue));
        }

        return ret;
    }
}