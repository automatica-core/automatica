using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Logic
{
    /// <summary>
    /// Base implementation of <see cref="ILogic"/>
    /// </summary>
    public abstract class Logic : ILogic
    {
        private readonly Dictionary<RuleInterfaceInstance, object> _valueDictionary = new();

        private readonly Dictionary<RuleInterfaceInstance, object> _inputValueDictionary = new();
        private readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="Logic"/> class.
        /// </summary>
        /// <param name="context">The <see cref="ILogicContext"/> to configure the class</param>
        protected Logic(ILogicContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Returns the given instance of the <see cref="ILogicContext"/>
        /// </summary>
        public ILogicContext Context { get; }

        public virtual bool IgnoreDuplicateValues => false;


        /// <summary>
        /// Returns UI specific data
        /// </summary>
        /// <returns></returns>
        public virtual object GetDataForVisu()
        {
            var dictionary = new Dictionary<string, object>();
            foreach (var param in Context.RuleInstance.RuleInterfaceInstance.Where(a =>
                         a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection ==
                         (long)Base.Templates.LogicInterfaceDirection.Param))
            {
                dictionary.Add(param.This2RuleInterfaceTemplateNavigation.Key, param.Value);
            }

            return dictionary;
        }

        public async Task<bool> Start(CancellationToken token = default)
        {
            await Start(Context.RuleInstance, token);

            foreach(var param in Context.RuleInstance.RuleInterfaceInstance.Where(a => a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection ==
                (long)Base.Templates.LogicInterfaceDirection.Param))
            {
                ParameterValueChanged(param, new LogicInterfaceInstanceDispatchable(param), param.Value);
            }

            return true;
        }

        protected virtual Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {

            return Task.FromResult(true);
        }

        public async Task<bool> Stop(CancellationToken token = default)
        {
            await Stop(Context.RuleInstance, token);
            return true;
        }

        public virtual Task Reload(CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        protected virtual Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            return Task.FromResult(true);
        }
        /// <summary>
        /// Will be called if a input parameter of the <see cref="ILogic"/> has changed
        /// </summary>
        /// <param name="instance">The <see cref="RuleInterfaceInstance"/> instance</param>
        /// <param name="value">The changed value</param>
        /// <returns></returns>
        public IList<ILogicOutputChanged> ValueChanged(RuleInterfaceInstance instance, object value)
        {
            return ValueChanged(instance, null, value);
        }

        /// <summary>
        ///  Will be called from <see cref="ValueChanged(RuleInterfaceInstance, object)"/>
        ///  Source will be set
        /// </summary>
        /// <param name="instance">The <see cref="RuleInterfaceInstance"/> instance</param>
        /// <param name="source">The source who dispatched the value (<see cref="IDispatchable"/>)</param>
        /// <param name="value">The changed value</param>
        /// <returns></returns>
        public IList<ILogicOutputChanged> ValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (_lock)
            {
              
                if (instance.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection ==
                    (long)Base.Templates.LogicInterfaceDirection.Param)
                {
                    ParameterValueChanged(instance, source, value);

                    Context.Logger.LogDebug($"RuleParameter changed {instance.This2RuleInstanceNavigation.Name} - {instance.This2RuleInterfaceTemplateNavigation.Name} (from {source?.GetType()}-{source?.Name}= value {value}");
                    return new List<ILogicOutputChanged>();
                }
                Context.Logger.LogDebug($"RuleInput changed {instance.This2RuleInstanceNavigation.Name} - {instance.This2RuleInterfaceTemplateNavigation.Name} (from {source?.GetType()}-{source?.Name}) value {value}");

                object prevValue = null;
                if (_inputValueDictionary.ContainsKey(instance))
                {
                    prevValue = _inputValueDictionary[instance];
                    _inputValueDictionary[instance] = value;
                }
                else
                {
                    _inputValueDictionary.Add(instance, value);
                }

                if (IgnoreDuplicateValues && _inputValueDictionary[instance] == prevValue)
                {
                    Context.Logger.LogDebug($"Input value did not change, ignore value change...");
                    return new List<ILogicOutputChanged>();
                }

                var values = InputValueChanged(instance, source, value);

                try
                {
                    foreach (var ruleOutValue in values)
                    {
                        Context.Logger.LogDebug($"RuleOutput changed {ruleOutValue.Instance.Name} value {ruleOutValue.Value}");

                        _valueDictionary[ruleOutValue.Instance.RuleInterfaceInstance] = ruleOutValue.Value;
                    }
                }
                catch (Exception)
                {
                    //Ignore for the current test
                }


                return values;
            }
        }


        /// <summary>
        /// Internal method to calculate new output values
        /// </summary>
        /// <param name="instance">The <see cref="RuleInterfaceInstance"/> instance</param>
        /// <param name="source">The source who dispatched the value (<see cref="IDispatchable"/>)</param>
        /// <param name="value">The changed value</param>
        /// <returns></returns>
        protected virtual IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            return new List<ILogicOutputChanged>();
        }

        /// <summary>
        /// Internal method to notify a parameter value has changed, if the parameter changed needs no notify a dispatch of a value, use the <see cref="Context.Dispatcher.DispatchValue"> DispatchValue</see>
        /// </summary>
        /// <param name="instance">The <see cref="RuleInterfaceInstance"/> instance</param>
        /// <param name="source">The source who dispatched the value (<see cref="IDispatchable"/>)</param>
        /// <param name="value">The changed value</param>
        /// <returns></returns>
        protected virtual void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {

        }


        /// <summary>
        /// Can be used to return only 1 output change
        /// </summary>
        /// <param name="value">The <see cref="ILogicOutputChanged"/> value</param>
        /// <returns></returns>
        protected IList<ILogicOutputChanged> SingleOutputChanged(ILogicOutputChanged value)
        {
            return new List<ILogicOutputChanged>
            {
                value
            };
        }
    }
}
