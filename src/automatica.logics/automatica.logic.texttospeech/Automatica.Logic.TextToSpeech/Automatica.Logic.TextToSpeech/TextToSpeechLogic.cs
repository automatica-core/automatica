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
        private RuleInterfaceInstance _duration;
        private RuleInterfaceInstance _text;
        private RuleInterfaceInstance _voice;

        private string _textValue;
        private Voices? _voiceValue;
        private TimeSpan? _durationValue;
        private RuleInterfaceInstance _trigger;

        public TextToSpeechLogic(ILogicContext context) : base(context)
        {
        
            
        }

        protected override async Task<bool> Start(RuleInstance instance,
            CancellationToken token = new CancellationToken())
        {
            _output = instance.RuleInterfaceInstance.First(a => a.This2RuleInterfaceTemplateNavigation.Key == "output");
            _duration = instance.RuleInterfaceInstance.FirstOrDefault(a => a.This2RuleInterfaceTemplateNavigation.Key == "duration");
            _text = instance.RuleInterfaceInstance.First(a => a.This2RuleInterfaceTemplateNavigation.Key == "text");
            _voice = instance.RuleInterfaceInstance.First(a => a.This2RuleInterfaceTemplateNavigation.Key == "voice");

            _trigger = instance.RuleInterfaceInstance.FirstOrDefault(a => a.This2RuleInterfaceTemplateNavigation.Key == "trigger");

            _textValue = _text.ValueString;
            _voiceValue = (Voices)Convert.ToInt32(_voice.ValueString);

            _ = Task.Run(LoadSynthesizer, token);

            await Task.CompletedTask;

            return true;
        }

        private async Task LoadSynthesizer()
        {
            try
            {   
                if (!_voiceValue.HasValue || String.IsNullOrEmpty(_textValue))
                {
                    Context.Logger.LogWarning($"Voice or text is null or empty, cannot synthesize text {_voice} and {_textValue}");
                    return;
                }

                if (_output == null)
                {
                    Context.Logger.LogWarning($"Output field is null...cannot synthesize text");
                    return;
                }

                var voice = PropertyHelper.GetNameAttributeFromEnumValue(_voiceValue);
                await Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_output), new DispatchValue(_output.ObjId, DispatchableType.RuleInstance, String.Empty, DateTime.Now, DispatchValueSource.Read));
                if(_duration != null)
                    await Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_duration), new DispatchValue(_output.ObjId, DispatchableType.RuleInstance, String.Empty, DateTime.Now, DispatchValueSource.Read));

                var value = await Context.CloudApi.SynthesizeWithAudioDuration(Context.RuleInstance.ObjId, _textValue, voice.TranslationKey, "");
                _url = value.uri;
                _durationValue = value.audioDuration;

                await Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_output), new DispatchValue(_output.ObjId, DispatchableType.RuleInstance, _url, DateTime.Now, DispatchValueSource.Read));
                if (_duration != null)
                    await Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_duration), new DispatchValue(_output.ObjId, DispatchableType.RuleInstance, _durationValue, DateTime.Now, DispatchValueSource.Read));
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
            var ret = new List<ILogicOutputChanged>();
            if (_trigger != null && instance.ObjId == _trigger.ObjId)
            {
              
                if (String.IsNullOrEmpty(_textValue))
                {
                    LoadSynthesizer().RunSynchronously();
                }

                ret.Add(new LogicOutputChanged(_text, _textValue));
                if (_duration != null)
                {
                    ret.Add(new LogicOutputChanged(_duration, _durationValue));
                }
            }

            return ret;
        }
    }
}
