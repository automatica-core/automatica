using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;

namespace P3.Logic.Lightning.FlashingLights
{
    internal class FlashingLightsLogic : Automatica.Core.Logic.Logic
    {
        private bool _currentState;
        private bool _resetState;

        private readonly RuleInterfaceInstance _output;
        
        private bool _taskRunning;
        private readonly long _repetitions;

        private readonly int _delay;

        private CancellationTokenSource _cancellationTokenSource;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public FlashingLightsLogic(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == FlashingLightsLogicFactory.Output);

           
            try
            {
                var delay = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                    a.This2RuleInterfaceTemplate == FlashingLightsLogicFactory.Delay);
                _delay = Convert.ToInt32(delay!.ValueInteger!.Value);
            }
            catch (Exception)
            {
                _delay = 1000;
            }

            try
            {
                _repetitions = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                    a.This2RuleInterfaceTemplate == FlashingLightsLogicFactory.RepeatCount)!.ValueInteger!.Value;
            }
            catch(Exception)
            {
                _repetitions = 1;
            }

        }

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = new CancellationToken())
        {
            _cancellationTokenSource = new CancellationTokenSource();
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = new CancellationToken())
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = null;
            return base.Stop(instance, token);
        }

        private async Task RunAction(CancellationToken token)
        {
            _taskRunning = true;
            token.ThrowIfCancellationRequested();
            await _semaphoreSlim.WaitAsync(token);
            try
            {
                var setState = !_resetState;

                for (var i = 0; i < _repetitions; i++)
                {
                    Context.Logger.LogInformation($"Set light state to {setState}");

                    await Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, setState).Instance,
                        setState);

                    setState = !setState;

                    await Task.Delay(_delay, token);
                }

                await Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, _resetState).Instance,
                    _resetState);
            }
            finally
            {
                _taskRunning = false;
                _semaphoreSlim.Release();
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == FlashingLightsLogicFactory.Trigger && value is true)
            {
                Context.Logger.LogDebug($"Starting blinking lights - reset light to state {_currentState} after finished....");
                _resetState = _currentState;
                if(!_taskRunning)
                    _ = RunAction(_cancellationTokenSource.Token).ConfigureAwait(false);
            }
            else if (instance.This2RuleInterfaceTemplate == FlashingLightsLogicFactory.State)
            {
                _currentState = (bool)value;
                Context.Logger.LogInformation($"Current state is {_currentState}");
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
