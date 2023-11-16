using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

public enum Voices
{
    [EnumName("en-US-JennyNeural")]
    EnglishJennyNeural = 0,

    [EnumName("en-US-GuyNeural")]
    EnglishGuyNeural = 1,

    [EnumName("de-DE-KatjaNeural")]
    GermanKatjaNeural = 2,

    [EnumName("de-DE-KasperNeural")]
    GermanKasperNeural = 3
}

namespace Automatica.Logic.TextToSpeech
{
    public class TextToSpeechFactory : LogicFactory
    {
        public override string LogicName => "TextToSpeech";

        public override Guid LogicGuid => new Guid("e846b61a-7a64-4c69-be31-1f44cb60ed8d");

        public override Version LogicVersion => new Version(0, 1, 1, 1);

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new TextToSpeechLogic(context);
        }

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TEXT_TO_SPEECH.SYNTHESIZE.NAME", "TEXT_TO_SPEECH.SYNTHESIZE.DESCRIPTION",
                "text-to-speech-synthesize", "TEXT_TO_SPEECH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(new Guid("6837b80d-ef8a-40ce-b713-4a0eb13aaffc"), "TEXT_TO_SPEECH.SYNTHESIZE.OUTPUT.NAME", "TEXT_TO_SPEECH.SYNTHESIZE.OUTPUT.DESCRIPTION", "output",
                LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateLogicInterfaceTemplate(new Guid("d759de1d-40b4-4e38-b505-2075fded6672"), "TEXT_TO_SPEECH.SYNTHESIZE.DURATION.NAME", "TEXT_TO_SPEECH.SYNTHESIZE.DURATION.DESCRIPTION", "duration",
                LogicGuid, LogicInterfaceDirection.Output, 0, 2);

            factory.CreateParameterLogicInterfaceTemplate(new Guid("90b44e5c-24d1-485f-a1fd-15b0f2a84820"), "TEXT_TO_SPEECH.SYNTHESIZE.TEXT.NAME", "TEXT_TO_SPEECH.SYNTHESIZE.TEXT.DESCRIPTION", "text", LogicGuid, 1,
                RuleInterfaceParameterDataType.Text, "", true);
            factory.CreateParameterLogicInterfaceTemplate(new Guid("bbcbf210-41b7-4b9f-ac93-ac4b275a32c1"), "TEXT_TO_SPEECH.SYNTHESIZE.VOICE.NAME", "TEXT_TO_SPEECH.SYNTHESIZE.VOICE.DESCRIPTION", "voice", LogicGuid, 1,
                RuleInterfaceParameterDataType.Enum, $"{(int)Voices.EnglishGuyNeural}", false, PropertyHelper.CreateEnumMetaString(typeof(Voices)));


        }
    }
}
