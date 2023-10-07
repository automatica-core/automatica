﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;

namespace P3.Logic.Sonos.SonosControl;

public class SonosControlLogic : Automatica.Core.Logic.Logic
{
    private readonly ILogicContext _context;

    //Inputs
    private readonly RuleInterfaceInstance _playPauseTrigger;
    private readonly RuleInterfaceInstance _playDefaultTrigger;
    private readonly RuleInterfaceInstance _playPauseInputState;
    private readonly RuleInterfaceInstance _volume;
    private readonly RuleInterfaceInstance _volumeIncrement;
    private readonly RuleInterfaceInstance _volumeDecrement;
    private readonly RuleInterfaceInstance _next;
    private readonly RuleInterfaceInstance _prev;
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
    private readonly RuleInterfaceInstance _prevOutput;


    private long _volumeOnPlayValue;
    private string _radioStationValue;
    private long _maxVolumeValue;

    private long _currentVolume;
    private bool? _currentlyPlaying;
    private bool? _lastPlayValue;

    public SonosControlLogic(ILogicContext context) : base(context)
    {
        _context = context;

        context.Logger.LogInformation($"Starting {context.RuleInstance.ObjId} {context.RuleInstance.Name}");

        //Inputs
        _playPauseTrigger = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.PlayPauseTrigger);
        _playDefaultTrigger = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.PlayDefaultTrigger);
        _playPauseInputState = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.PauseTrigger);
        _volume = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.Volume);
        _volumeIncrement = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.VolumeIncrement);
        _volumeDecrement = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.VolumeDecrement);
        _next = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.Next);
        _prev = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.Previous);
        _radioStationInput = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.RadioStationInput);

        //Params
        _volumeOnPlay = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.VolumeOnPlay);
        _radioStation = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.RadioStation);
        _maxVolume = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.MaxVolume);


        //Outputs
        _playOutputStatus = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.PlayOutputStatus);
        _pauseOutputStatus = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.PauseOutputStatus);
        _volumeOutputStatus = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.VolumeOutputStatus);
        _radioStationOutputValue = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.RadioStationOutputValue);
        _nextOutput = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.NextOutput);
        _prevOutput = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == SonosControlLogicFactory.PreviousOutput);

    }

    protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
    {
        if (instance.This2RuleInterfaceTemplate == _volumeOnPlay.This2RuleInterfaceTemplate)
        {
            _volumeOnPlayValue = Convert.ToInt64(value);
        }
        if (instance.This2RuleInterfaceTemplate == _radioStation.This2RuleInterfaceTemplate)
        {
            _radioStationValue = Convert.ToString(value);
        }
        if (instance.This2RuleInterfaceTemplate == _maxVolume.This2RuleInterfaceTemplate)
        {
            _maxVolumeValue = Convert.ToInt64(value);
        }

        base.ParameterValueChanged(instance, source, value);
    }

    protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
    {
        _volumeOnPlayValue = Context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == _volumeOnPlay.This2RuleInterfaceTemplate)!.ValueInteger!.Value;

        _radioStationValue = Context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == _radioStation.This2RuleInterfaceTemplate)!.ValueString;
        
        _maxVolumeValue = Context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == _maxVolume.This2RuleInterfaceTemplate)!.ValueInteger!.Value;

        return base.Start(instance, token);
    }


    protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
    {
        var ret = new List<ILogicOutputChanged>();

        Context.Logger.LogInformation($"Input value changed.... {instance.ObjId} from {source.Name} ({source.Id}) with value {value}");

        if (instance.This2RuleInterfaceTemplate == _volume.This2RuleInterfaceTemplate)
        {
            var vol = Convert.ToInt64(value);
            if (_currentVolume != vol)
            {
                _currentVolume = vol;
                ret.Add(new LogicOutputChanged(_volumeOutputStatus, _currentVolume));
            }
        }

        if (instance.This2RuleInterfaceTemplate == _playPauseTrigger.This2RuleInterfaceTemplate)
        {
            var play = Convert.ToBoolean(value);

            if (_lastPlayValue == play)
                return ret;

            _lastPlayValue = play;

            if (play)
            {
                ret.Add(new LogicOutputChanged(_playOutputStatus, true));
                ret.Add(new LogicOutputChanged(_pauseOutputStatus, false));
                _currentlyPlaying = true;
            }
            else if (_currentlyPlaying.HasValue)
            {
                ret.Add(new LogicOutputChanged(_playOutputStatus, false));
                ret.Add(new LogicOutputChanged(_pauseOutputStatus, true));
                _currentlyPlaying = false;
            }
        }

        if (instance.This2RuleInterfaceTemplate == _playDefaultTrigger?.This2RuleInterfaceTemplate)
        {
            var play = Convert.ToBoolean(value);

            if(_lastPlayValue == play)
                return ret;

            _lastPlayValue = play;
            if (play)
            {
                _currentVolume = _volumeOnPlayValue;
                _currentlyPlaying = true;

                ret.Add(new LogicOutputChanged(_radioStationOutputValue, _radioStationValue));
                ret.Add(new LogicOutputChanged(_pauseOutputStatus, false));
                ret.Add(new LogicOutputChanged(_volumeOutputStatus, _currentVolume));
                ret.Add(new LogicOutputChanged(_playOutputStatus, true));
            }
            else if (_currentlyPlaying.HasValue)
            {
                _currentlyPlaying = false;
                ret.Add(new LogicOutputChanged(_pauseOutputStatus, true));
                ret.Add(new LogicOutputChanged(_playOutputStatus, false));
            }
        }
        if (instance.This2RuleInterfaceTemplate == _playPauseInputState.This2RuleInterfaceTemplate)
        {
            _currentlyPlaying = Convert.ToBoolean(value);
        }

        if (instance.This2RuleInterfaceTemplate == _volumeIncrement.This2RuleInterfaceTemplate)
        {
            if (_currentlyPlaying.HasValue && _currentlyPlaying.Value)
            {
                if (_currentVolume + 1 >= _maxVolumeValue)
                {
                    return ret;
                }

                _currentVolume++;
                ret.Add(new LogicOutputChanged(_volumeOutputStatus, _currentVolume));
            }
        }

        if (instance.This2RuleInterfaceTemplate == _volumeDecrement.This2RuleInterfaceTemplate)
        {
            if (_currentlyPlaying.HasValue && _currentlyPlaying.Value)
            {
                if (_currentVolume - 1 < 0)
                {
                    return ret;
                }

                _currentVolume--;
                ret.Add(new LogicOutputChanged(_volumeOutputStatus, _currentVolume));
            }
        }
        if (instance.This2RuleInterfaceTemplate == _volume.This2RuleInterfaceTemplate)
        {
            if (_currentlyPlaying.HasValue && _currentlyPlaying.Value)
            {
                var volume = Convert.ToInt64(value);
                if (_currentVolume == volume)
                {
                    return ret;
                }
                if (volume >= _maxVolumeValue)
                {
                    return ret;
                }

                _currentVolume = volume;
                ret.Add(new LogicOutputChanged(_volumeOutputStatus, _currentVolume));
            }
        }

        if (instance.This2RuleInterfaceTemplate == _next.This2RuleInterfaceTemplate)
        {
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_nextOutput, true).Instance, true);
            Thread.Sleep(100);
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_nextOutput, false).Instance, false);
        }

        if (instance.This2RuleInterfaceTemplate == _prev.This2RuleInterfaceTemplate)
        {
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_prevOutput, true).Instance, true);
            Thread.Sleep(100);
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_prevOutput, false).Instance, false);
        }

        if (instance.This2RuleInterfaceTemplate == _radioStationInput.This2RuleInterfaceTemplate)
        {
            ret.Add(new LogicOutputChanged(_radioStationOutputValue, _radioStationValue));
        }

        return ret;
    }
}