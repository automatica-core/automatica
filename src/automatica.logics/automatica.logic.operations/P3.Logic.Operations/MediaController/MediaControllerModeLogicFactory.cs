using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.MediaController
{
    /// <summary>
    /// Media Controller Mode: one per device of a media controller. Linked to the command output of the controller, it passes on
    /// only the commands of its mode (power, volume, channel, playback, navigation), so a device needs no gating by mode
    /// </summary>
    public class MediaControllerModeLogicFactory : LogicFactory
    {
        public static readonly Guid InputCommand = new Guid("9a2a7a37-897a-45d0-aa41-00704126bc94");
        public static readonly Guid ParamMode = new Guid("8428f7b0-0ce0-4814-aab9-eaa42f9fc57a");
        public static readonly Guid OutputPower = new Guid("7d58adec-84aa-4b81-ae58-083aa826df1c");
        public static readonly Guid OutputVolumeUp = new Guid("1411da96-8420-45d1-bfe7-7825c6bed82d");
        public static readonly Guid OutputVolumeDown = new Guid("ca7d086b-f808-4aba-bb85-86d404e0857f");
        public static readonly Guid OutputVolume = new Guid("a7270960-135b-49cb-b850-001485c14501");
        public static readonly Guid OutputMute = new Guid("adf50884-8aed-4665-b3a0-9cd9594aa9a4");
        public static readonly Guid OutputChannelUp = new Guid("a4742077-c880-4f15-a595-1f6d120c3b54");
        public static readonly Guid OutputChannelDown = new Guid("d49d5bd4-bdd9-4511-98c0-ec931c11efc8");
        public static readonly Guid OutputChannel = new Guid("f12f27df-9e1f-4c8d-95e7-4e209284c8a4");
        public static readonly Guid OutputPlayPause = new Guid("83f34601-0857-4894-9721-918320caa316");
        public static readonly Guid OutputNext = new Guid("de8bd5fd-bd7d-496d-afa2-9321186788c4");
        public static readonly Guid OutputPrevious = new Guid("28bc9ebd-7c7f-4069-82fc-832dcc044b0a");
        public static readonly Guid OutputNavUp = new Guid("5147b482-a9ce-45bc-87a0-cfe7d8508b9b");
        public static readonly Guid OutputNavDown = new Guid("ea56b1d4-97d8-4f89-bc05-90425a570eb4");
        public static readonly Guid OutputNavLeft = new Guid("2fff93c9-261c-48a8-8536-c13b0f1876df");
        public static readonly Guid OutputNavRight = new Guid("7945b447-e41c-4d8d-8913-6d01d168de90");
        public static readonly Guid OutputNavOk = new Guid("55de7992-46c8-48b0-b930-6196e053d3a3");
        public static readonly Guid OutputBack = new Guid("b15c4841-de8f-416d-8158-a46a92c9f5b1");
        public static readonly Guid OutputMenu = new Guid("26e795a2-c0d7-464e-b7d8-2f221e052940");
        public static readonly Guid OutputHome = new Guid("f152eb6b-ee9e-49ec-820e-344c1b7c0aee");

        public override string LogicName => "P3.Logic.Operations.MediaControllerMode";
        public override Guid LogicGuid => new Guid("5653684c-2922-457b-a928-6cbad4830b6e");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        private const string Mc = "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.";

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "OPERATIONS.MEDIA_CONTROLLER_MODE.NAME", "OPERATIONS.MEDIA_CONTROLLER_MODE.DESCRIPTION", "operations-media-controller-mode", "OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputCommand, "OPERATIONS.MEDIA_CONTROLLER_MODE.INPUT.COMMAND.NAME", "OPERATIONS.MEDIA_CONTROLLER_MODE.INPUT.COMMAND.DESCRIPTION", "command", LogicGuid, LogicInterfaceDirection.Input, 0, 1);

            factory.CreateLogicInterfaceTemplate(OutputPower, "OPERATIONS.MEDIA_CONTROLLER_MODE.OUTPUT.POWER.NAME", "OPERATIONS.MEDIA_CONTROLLER_MODE.OUTPUT.POWER.DESCRIPTION", "powerOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            Output(factory, OutputVolumeUp, "VOLUME_UP", "volumeUpOutput", 2);
            Output(factory, OutputVolumeDown, "VOLUME_DOWN", "volumeDownOutput", 3);
            Output(factory, OutputVolume, "VOLUME", "volumeOutput", 4);
            Output(factory, OutputMute, "MUTE", "muteOutput", 5);
            Output(factory, OutputChannelUp, "CHANNEL_UP", "channelUpOutput", 6);
            Output(factory, OutputChannelDown, "CHANNEL_DOWN", "channelDownOutput", 7);
            Output(factory, OutputChannel, "CHANNEL", "channelOutput", 8);
            Output(factory, OutputPlayPause, "PLAY_PAUSE", "playPauseOutput", 9);
            Output(factory, OutputNext, "NEXT", "nextOutput", 10);
            Output(factory, OutputPrevious, "PREVIOUS", "previousOutput", 11);
            Output(factory, OutputNavUp, "NAV_UP", "navUpOutput", 12);
            Output(factory, OutputNavDown, "NAV_DOWN", "navDownOutput", 13);
            Output(factory, OutputNavLeft, "NAV_LEFT", "navLeftOutput", 14);
            Output(factory, OutputNavRight, "NAV_RIGHT", "navRightOutput", 15);
            Output(factory, OutputNavOk, "NAV_OK", "navOkOutput", 16);
            Output(factory, OutputBack, "BACK", "backOutput", 17);
            Output(factory, OutputMenu, "MENU", "menuOutput", 18);
            Output(factory, OutputHome, "HOME", "homeOutput", 19);

            factory.CreateParameterLogicInterfaceTemplate(ParamMode, "OPERATIONS.MEDIA_CONTROLLER_MODE.PARAM.MODE.NAME", "OPERATIONS.MEDIA_CONTROLLER_MODE.PARAM.MODE.DESCRIPTION", "mode", LogicGuid, 1, RuleInterfaceParameterDataType.Integer, 1, false);
        }

        // the outputs have the same names as the command outputs of the media controller
        private void Output(ILogicTemplateFactory factory, Guid id, string key, string name, int sortOrder)
        {
            factory.CreateLogicInterfaceTemplate(id, Mc + key + ".NAME", Mc + key + ".DESCRIPTION", name, LogicGuid, LogicInterfaceDirection.Output, 0, sortOrder);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new MediaControllerModeLogic(context);
        }
    }
}
