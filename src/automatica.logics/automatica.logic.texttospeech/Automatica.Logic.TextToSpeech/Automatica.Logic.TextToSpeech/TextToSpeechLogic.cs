using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Automatica.Logic.TextToSpeech
{
    public class TextToSpeechLogic : Automatica.Core.Logic.Logic
    {
       
        private string _url;
        private RuleInterfaceInstance _output;
        private RuleInterfaceInstance _text;
        private RuleInterfaceInstance _voice;

        private string _textValue;
        private Voices? _voiceValue;

        public TextToSpeechLogic(ILogicContext context) : base(context)
        {
        
            
        }

        protected override async Task<bool> Start(RuleInstance instance,
            CancellationToken token = new CancellationToken())
        {

            _output = instance.RuleInterfaceInstance.First(a => a.This2RuleInterfaceTemplateNavigation.Key == "output");
            _text = instance.RuleInterfaceInstance.First(a => a.This2RuleInterfaceTemplateNavigation.Key == "text");
            _voice = instance.RuleInterfaceInstance.First(a => a.This2RuleInterfaceTemplateNavigation.Key == "voice");


            _textValue = _text.ValueString;
            _voiceValue = (Voices)Convert.ToInt32(_voice.ValueString);

            _ = Task.Run(LoadSynthesizer, token);

            return true;
        }

        private async Task LoadSynthesizer()
        {
            try
            {   
                if (!_voiceValue.HasValue || String.IsNullOrEmpty(_textValue))
                {
                    Context.Logger.LogWarning($"Voice or text is null or empty, cannot synthesize text {_voice} and {_textValue}");
                }
                var voice = PropertyHelper.GetNameAttributeFromEnumValue(_voiceValue);
                await Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_output), new DispatchValue(_output.ObjId, DispatchableType.RuleInstance, String.Empty, DateTime.Now, DispatchValueSource.Read));
                _url = await Context.CloudApi.Synthesize(Context.RuleInstance.ObjId, _textValue, voice.TranslationKey, "");
                await Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_output), new DispatchValue(_output.ObjId, DispatchableType.RuleInstance, _url, DateTime.Now, DispatchValueSource.Read));
            }
            catch (Exception e)
            {
                Context.Logger.LogError(e, $"Could not synthesize text....{e}");
            }
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (source.Id == instance.ObjId)
            {
                return;
            }
            if (instance.ObjId == _text.ObjId)
            {
                _textValue = value.ToString();
            }
            else if (instance.ObjId == _voice.ObjId)
            {
                _voiceValue = (Voices)Convert.ToInt32(value.ToString());
            }


            _ = Task.Run(LoadSynthesizer);
            base.ParameterValueChanged(instance, source, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.ObjId == _text.ObjId)
            {
                _textValue = value.ToString();
            }
            return SingleOutputChanged(new LogicOutputChanged(_output, _url));
        }
    }
}
