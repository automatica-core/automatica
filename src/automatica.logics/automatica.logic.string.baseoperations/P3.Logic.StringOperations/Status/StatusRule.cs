using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;
using P3.Logic.StringOperations.Common;

[assembly: InternalsVisibleTo("P3.Logic.StringOperations.Tests")]

namespace P3.Logic.StringOperations.Status
{
    /// <summary>
    /// Evaluates the rules top down on every input or rule change; the first true rule wins, none = the default status.
    /// Texts can contain the input values (&lt;v1&gt;, &lt;v2.1&gt;, ...).
    /// </summary>
    public class StatusRule : TextLogicBase
    {
        internal sealed record StatusResult(int Rule, string Text, double Value, string Icon, string Color);

        private readonly object[] _inputs = new object[StatusLogicFactory.Inputs.Length];
        private readonly StatusCondition[] _conditions = new StatusCondition[StatusLogicFactory.RuleCount];
        private readonly bool[] _parsed = new bool[StatusLogicFactory.RuleCount];
        private bool _anyInput;
        private StatusResult _last;

        public StatusRule(ILogicContext context) : base(context)
        {
        }

        internal StatusResult Current => _last ?? Evaluate();

        protected override void OnParameterChanged(Guid template, object value)
        {
            var rule = Array.IndexOf(StatusLogicFactory.ParamCondition, template);
            if (rule >= 0)
            {
                _conditions[rule] = Parse(rule, value?.ToString());
                _parsed[rule] = true;
            }

            // a changed rule or text applies at once, but only after the inputs have values (no burst of texts at startup)
            if (Started && _anyInput)
            {
                Dispatch(Update());
            }
        }

        private StatusCondition Parse(int rule, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            if (StatusCondition.TryParse(text, out var condition, out var error))
            {
                return condition;
            }
            Context.Logger.LogWarning($"Status {Context.RuleInstance?.Name}: rule {rule + 1} \"{text}\" is ignored, {error}");
            return null;
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var index = Array.IndexOf(StatusLogicFactory.Inputs, instance.This2RuleInterfaceTemplate);
            if (index < 0)
            {
                return NoChanges();
            }

            _inputs[index] = value;
            _anyInput = true;
            return Update();
        }

        /// <summary>
        /// The outputs that changed since the last evaluation (all of them the first time)
        /// </summary>
        private IList<ILogicOutputChanged> Update()
        {
            var result = Evaluate();
            var previous = _last;
            _last = result;

            var changes = new List<ILogicOutputChanged>();
            if (previous == null || previous.Text != result.Text)
            {
                Add(changes, StatusLogicFactory.OutputText, result.Text);
            }
            if (previous == null || !previous.Value.Equals(result.Value))
            {
                Add(changes, StatusLogicFactory.OutputValue, result.Value);
            }
            if (previous == null || previous.Rule != result.Rule)
            {
                Add(changes, StatusLogicFactory.OutputRule, result.Rule);
            }
            return changes;
        }

        private void Add(List<ILogicOutputChanged> changes, Guid output, object value)
        {
            var change = Changed(output, value);
            if (change != null)
            {
                changes.Add(change);
            }
        }

        internal StatusResult Evaluate()
        {
            for (var i = 0; i < StatusLogicFactory.RuleCount; i++)
            {
                if (!_parsed[i])
                {
                    _conditions[i] = Parse(i, Parameter(StatusLogicFactory.ParamCondition[i])?.ToString());
                    _parsed[i] = true;
                }

                var condition = _conditions[i];
                if (condition != null && condition.Evaluate(_inputs))
                {
                    return new StatusResult(i + 1,
                        Render(ParameterText(StatusLogicFactory.ParamText[i])),
                        ParameterDouble(StatusLogicFactory.ParamValue[i], i + 1),
                        ParameterText(StatusLogicFactory.ParamIcon[i]),
                        ParameterText(StatusLogicFactory.ParamColor[i]));
                }
            }

            return new StatusResult(0,
                Render(ParameterText(StatusLogicFactory.ParamDefaultText)),
                ParameterDouble(StatusLogicFactory.ParamDefaultValue, 0),
                ParameterText(StatusLogicFactory.ParamDefaultIcon),
                ParameterText(StatusLogicFactory.ParamDefaultColor));
        }

        private string Render(string template) => TextTemplate.Render(template, _inputs);

        public override object GetDataForVisu()
        {
            var data = base.GetDataForVisu() as IDictionary<string, object> ?? new Dictionary<string, object>();
            var current = Current;
            data["status"] = new Dictionary<string, object>
            {
                ["rule"] = current.Rule,
                ["text"] = current.Text,
                ["value"] = current.Value,
                ["icon"] = current.Icon,
                ["color"] = current.Color
            };
            return data;
        }
    }
}
