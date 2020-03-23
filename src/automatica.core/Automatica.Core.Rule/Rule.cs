
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Rule
{
    /// <summary>
    /// Base implementation of <see cref="IRule"/>
    /// </summary>
    public abstract class Rule : IRule
    {
        private readonly Dictionary<RuleInterfaceInstance, object> _valueDictionary =
            new Dictionary<RuleInterfaceInstance, object>();
        private readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="Rule"/> class.
        /// </summary>
        /// <param name="context">The <see cref="IRuleContext"/> to configure the class</param>
        protected Rule(IRuleContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Returns the given instance of the <see cref="IRuleContext"/>
        /// </summary>
        public IRuleContext Context { get; }


        /// <summary>
        /// Returns UI specific data
        /// </summary>
        /// <returns></returns>
        public virtual object GetDataForVisu()
        {
            return null;
        }

        public virtual Task<bool> Start()
        {
            return Task.FromResult(true);
        }

        public virtual Task<bool> Stop()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Will be called if a input parameter of the <see cref="IRule"/> has changed
        /// </summary>
        /// <param name="instance">The <see cref="RuleInterfaceInstance"/> instance</param>
        /// <param name="value">The changed value</param>
        /// <returns></returns>
        public IList<IRuleOutputChanged> ValueChanged(RuleInterfaceInstance instance, object value)
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
        public IList<IRuleOutputChanged> ValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (_lock)
            {
              
                if (instance.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection ==
                    (long)Base.Templates.RuleInterfaceDirection.Param)
                {
                    ParamterValueChanged(instance, source, value);

                    Context.Logger.LogDebug($"RuleParameter changed {instance.This2RuleInstanceNavigation.Name} - {instance.This2RuleInterfaceTemplateNavigation.Name} from {source.GetType()}-{source.Name} value {value}");
                    return new List<IRuleOutputChanged>();
                }
                Context.Logger.LogDebug($"RuleInput changed {instance.This2RuleInstanceNavigation.Name} - {instance.This2RuleInterfaceTemplateNavigation.Name} from {source.GetType()}-{source.Name} value {value}");

                var values = InputValueChanged(instance, source, value);

                try
                {
                    foreach (var ruleOutValue in values)
                    {
                        Context.Logger.LogDebug($"RuleOutput changed {ruleOutValue.Instance.Name} value {ruleOutValue.Value}");

                        if (!_valueDictionary.ContainsKey(ruleOutValue.Instance.RuleInterfaceInstance))
                        {
                            _valueDictionary.Add(ruleOutValue.Instance.RuleInterfaceInstance, ruleOutValue.Value);
                        }
                        else
                        {
                            _valueDictionary[ruleOutValue.Instance.RuleInterfaceInstance] = ruleOutValue.Value;
                        }
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
        protected virtual IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            return new List<IRuleOutputChanged>();
        }

        /// <summary>
        /// Internal method to notify a parameter value has changed, if the parameter changed needs no notify a dispatch of a value, use the <see cref="Context.Dispatcher.DispatchValue"> DispatchValue</see>
        /// </summary>
        /// <param name="instance">The <see cref="RuleInterfaceInstance"/> instance</param>
        /// <param name="source">The source who dispatched the value (<see cref="IDispatchable"/>)</param>
        /// <param name="value">The changed value</param>
        /// <returns></returns>
        protected virtual void ParamterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {

        }


        /// <summary>
        /// Can be used to return only 1 output change
        /// </summary>
        /// <param name="value">The <see cref="IRuleOutputChanged"/> value</param>
        /// <returns></returns>
        protected IList<IRuleOutputChanged> SingleOutputChanged(IRuleOutputChanged value)
        {
            return new List<IRuleOutputChanged>
            {
                value
            };
        }
    }
}
