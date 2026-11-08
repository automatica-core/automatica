using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.MediaPlayer
{
    /// <summary>
    /// Media player: a music zone, speaker or TV of any driver as a control (<see cref="Automatica.Core.Control.Base.IMediaPlayer"/>),
    /// so the Matter and HomeKit bridges and voice assistants can play, pause and set the volume. The inputs take the state
    /// of the device, the outputs send the commands to it.
    /// </summary>
    public class MediaPlayerLogicFactory : LogicFactory
    {
        public static readonly Guid InputPlaying = new Guid("933bec82-0028-4e78-8867-a76936c2a155");
        public static readonly Guid InputVolume = new Guid("438e5e85-968c-4f3d-b6fa-28ac1a6030b4");
        public static readonly Guid InputMute = new Guid("e7fb30b8-1c77-4116-b70f-54c817ae4a26");
        public static readonly Guid InputTitle = new Guid("e5323da6-99a6-4537-895c-f89297a1fda8");
        public static readonly Guid InputArtist = new Guid("23a8a732-8e4a-45ea-8c66-fe4775578fa3");

        public static readonly Guid OutputPlay = new Guid("cad363ed-6055-4d46-9200-857a0cfa223b");
        public static readonly Guid OutputStop = new Guid("a781abe6-145b-47dd-90aa-7774c4de8ed6");
        public static readonly Guid OutputNext = new Guid("b79be797-ec90-431c-a38e-4649291ed462");
        public static readonly Guid OutputPrevious = new Guid("8059e8ac-1a77-41cd-9271-6859a26e35a9");
        public static readonly Guid OutputVolume = new Guid("bcd29af2-3e18-4dd5-b227-f2b72225afb7");
        public static readonly Guid OutputMute = new Guid("840901c2-783c-4f95-a3a0-f70da3c7b92e");

        public static readonly Guid ParamBridgeType = new Guid("2d39fb44-68fc-44fb-89dc-fc7bad454bf9");

        private const string I18N = "OPERATIONS.MEDIA_PLAYER";

        public override string LogicName => "P3.Logic.Operations.MediaPlayer";
        public override Guid LogicGuid => new Guid("9655e92c-0e60-4eb2-bf48-6c045590e948");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{I18N}.NAME", $"{I18N}.DESCRIPTION", "operations-media-player", "OPERATIONS.NAME", 100, 100);

            Input(factory, InputPlaying, "PLAYING", "playing", 1);
            Input(factory, InputVolume, "VOLUME", "volume", 2);
            Input(factory, InputMute, "MUTE", "mute", 3);
            Input(factory, InputTitle, "TITLE", "title", 4);
            Input(factory, InputArtist, "ARTIST", "artist", 5);

            Output(factory, OutputPlay, "PLAY", "playOutput", 1);
            Output(factory, OutputStop, "STOP", "stopOutput", 2);
            Output(factory, OutputNext, "NEXT", "nextOutput", 3);
            Output(factory, OutputPrevious, "PREVIOUS", "previousOutput", 4);
            Output(factory, OutputVolume, "VOLUME", "volumeOutput", 5);
            Output(factory, OutputMute, "MUTE", "muteOutput", 6);

            factory.CreateParameterLogicInterfaceTemplate(ParamBridgeType, $"{I18N}.PARAM.BRIDGE_TYPE.NAME", $"{I18N}.PARAM.BRIDGE_TYPE.DESCRIPTION",
                "bridgeType", LogicGuid, 1, RuleInterfaceParameterDataType.Enum, "0", false,
                $"ENUM(0,{I18N}.BRIDGE_TYPE.ON_OFF_VOLUME;1,{I18N}.BRIDGE_TYPE.SPEAKER)");
        }

        private void Input(ILogicTemplateFactory factory, Guid id, string key, string name, int sort) =>
            factory.CreateLogicInterfaceTemplate(id, $"{I18N}.INPUT.{key}.NAME", $"{I18N}.INPUT.{key}.DESCRIPTION", name, LogicGuid, LogicInterfaceDirection.Input, 0, sort);

        private void Output(ILogicTemplateFactory factory, Guid id, string key, string name, int sort) =>
            factory.CreateLogicInterfaceTemplate(id, $"{I18N}.OUTPUT.{key}.NAME", $"{I18N}.OUTPUT.{key}.DESCRIPTION", name, LogicGuid, LogicInterfaceDirection.Output, 0, sort);

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new MediaPlayerLogic(context);
        }
    }
}
