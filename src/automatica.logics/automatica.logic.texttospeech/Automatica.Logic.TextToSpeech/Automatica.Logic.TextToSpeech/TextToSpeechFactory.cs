using System;
using Automatica.Core.Base.Templates;
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
        public override string LogicName => "Messenger";

        public override Guid LogicGuid => new Guid("e846b61a-7a64-4c69-be31-1f44cb60ed8d");

        public override Version LogicVersion => new Version(0, 1, 0, 1);

        public override bool InDevelopmentMode => true;



        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new MessengerLogic(context);
        }

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MESSENGER.CLOUD_EMAIL.NAME", "MESSENGER.CLOUD_EMAIL.DESCRIPTION",
                "messenger-cloud-email", "MESSENGER.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(new Guid("6837b80d-ef8a-40ce-b713-4a0eb13aaffc"), "T", "MESSENGER.CLOUD_EMAIL.TRIGGER.DESCRIPTION",
                LogicGuid, LogicInterfaceDirection.Input, 0, 1);

         
        }
    }
}
