using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.MediaController
{
    /// <summary>
    /// Media Controller: Universal remote: up to 8 modes (e.g. TV, radio) with power, volume, channel, number pad, navigation and playback commands (modelled on the Loxone function block of the same name)
    /// </summary>
    public class MediaControllerLogicFactory : LogicFactory
    {
        public static readonly Guid InputPowerToggle = new Guid("20f7d9e2-e259-59d6-8925-de2633e46c85");
        public static readonly Guid InputPowerOn = new Guid("f817c443-4658-5c1d-b708-8dfce3d24cd0");
        public static readonly Guid InputPowerOff = new Guid("e287e69c-3b7f-5aab-b1d6-17c62888f9c6");
        public static readonly Guid InputVolumeUp = new Guid("161bcf16-421b-5ac1-a52c-c70901df5d1f");
        public static readonly Guid InputVolumeDown = new Guid("fe2c25d9-fc7c-5003-a198-2f44c5561a96");
        public static readonly Guid InputVolume = new Guid("9eb5f2ee-d49f-5580-8726-46e42726452e");
        public static readonly Guid InputMute = new Guid("cda03d0e-c006-5dc0-87b7-36ac14a40977");
        public static readonly Guid InputChannelUp = new Guid("95a031c4-fb3b-5abf-9a39-6a52c65df218");
        public static readonly Guid InputChannelDown = new Guid("3577fcd1-7ca7-5743-9203-e9db01803729");
        public static readonly Guid InputChannel = new Guid("9e1ca508-4a85-5c5a-b1b9-250da2e16d93");
        public static readonly Guid InputDigit = new Guid("6fdd0807-5fc2-5634-a6b0-3297bcc62e89");
        public static readonly Guid InputMode = new Guid("71385425-de13-5bd4-b746-80bc4ad0fdbb");
        public static readonly Guid InputMode1 = new Guid("2170c14a-f428-5814-bdb7-098548c2356d");
        public static readonly Guid InputMode2 = new Guid("8541fc73-82dd-5c02-9f60-9807a668b544");
        public static readonly Guid InputMode3 = new Guid("51684f02-2ebc-5207-ba2b-c0e1b6e7f139");
        public static readonly Guid InputMode4 = new Guid("0a842aaa-12e5-5d65-b6a7-453559858ed5");
        public static readonly Guid InputMode5 = new Guid("1524ed7d-068e-55f5-8fce-c0b0b0678753");
        public static readonly Guid InputMode6 = new Guid("08284de6-4932-5c02-8d20-2b3524c5fad8");
        public static readonly Guid InputMode7 = new Guid("2d85cb62-7941-51f7-ba82-7ae306738306");
        public static readonly Guid InputMode8 = new Guid("2538f52a-16c9-5c0a-9d4c-68b458e2f8b0");
        public static readonly Guid InputPlayPause = new Guid("ffb8ac09-e1e3-5dc2-9a28-483b7159f983");
        public static readonly Guid InputNext = new Guid("38b4e661-380c-5347-acbb-f7bebb209645");
        public static readonly Guid InputPrevious = new Guid("4465ac5a-00d8-5b55-8238-090c946cf9a1");
        public static readonly Guid InputNavUp = new Guid("2dbda16e-06f0-51b1-b406-76e825f48435");
        public static readonly Guid InputNavDown = new Guid("72cade30-8138-53c8-b013-fb3ba8cd8f1d");
        public static readonly Guid InputNavLeft = new Guid("21e913a9-57dc-5966-ab82-759023314c8e");
        public static readonly Guid InputNavRight = new Guid("bd8ee8b3-892b-5a43-b4f6-302a80bebc47");
        public static readonly Guid InputNavOk = new Guid("325049d1-77c5-5cce-a69a-0ce979e6d72b");
        public static readonly Guid InputBack = new Guid("9e0e3cf1-c42e-565f-a6e8-367c622126d1");
        public static readonly Guid InputMenu = new Guid("a95d0a0d-ab6d-5c0b-a1b8-1cc934e30f1b");
        public static readonly Guid InputHome = new Guid("8dca40d2-bda0-558d-b1b3-bf92e71e0f48");
        public static readonly Guid InputLock = new Guid("e5692f16-f9d6-540a-a8cd-296b5fd6b62c");
        public static readonly Guid InputDisablePeriphery = new Guid("63600712-0c2b-5df1-8469-36f2aa1ffab2");
        public static readonly Guid OutputMode = new Guid("a0bb4a3b-1c4b-5f81-9b31-903f0df065ae");
        public static readonly Guid OutputPower = new Guid("f2e027d5-99c1-599f-b755-e4e0bc4b1082");
        public static readonly Guid OutputModeActive1 = new Guid("7c9adf3a-dacb-5b66-be58-3c261795ba35");
        public static readonly Guid OutputModeActive2 = new Guid("f90f4584-a4ec-584f-bcdb-fc68ee567027");
        public static readonly Guid OutputModeActive3 = new Guid("290259d0-84c6-5eb8-9196-2832211d4ae2");
        public static readonly Guid OutputModeActive4 = new Guid("65c12354-3f11-50bb-bc42-cad17d4c545f");
        public static readonly Guid OutputModeActive5 = new Guid("9fcaef2d-5846-52a4-af83-8f14286bf285");
        public static readonly Guid OutputModeActive6 = new Guid("bb09404b-6d3d-50a0-8f0d-86d216af6287");
        public static readonly Guid OutputModeActive7 = new Guid("78207dcf-fb6a-5c6b-87f0-ec051dd4af90");
        public static readonly Guid OutputModeActive8 = new Guid("68635736-33b1-57f8-8bea-aa9e514f5131");
        public static readonly Guid OutputVolumeUp = new Guid("10fc6eee-889e-53e7-9a94-1fd1717ecd75");
        public static readonly Guid OutputVolumeDown = new Guid("8374dd2c-0973-5274-b9aa-685fed7ec485");
        public static readonly Guid OutputVolume = new Guid("cc671f27-2fd3-5b6f-b325-e11a3502fbd7");
        public static readonly Guid OutputMute = new Guid("cf40d5af-1f73-5d4b-9159-163c082bc191");
        public static readonly Guid OutputChannelUp = new Guid("951e38f1-b38e-54b4-b5d5-f57e8f585ab8");
        public static readonly Guid OutputChannelDown = new Guid("fec80fc6-56a0-5bdd-8513-158979eddd87");
        public static readonly Guid OutputChannel = new Guid("fcf3e7ae-dbd6-5a2c-85a0-cf0b5de312c8");
        public static readonly Guid OutputPlayPause = new Guid("f1008ee6-58d6-538d-b558-90e71d06b87a");
        public static readonly Guid OutputNext = new Guid("daf7be86-8116-5740-b064-007c6a871ef7");
        public static readonly Guid OutputPrevious = new Guid("1ece615d-4d99-50e3-a224-f7aa42dfdd6f");
        public static readonly Guid OutputNavUp = new Guid("efe9ee22-15cf-5f46-b6cf-7cd96d215928");
        public static readonly Guid OutputNavDown = new Guid("3850f533-f481-57d9-b9ff-f5cbeac40a4a");
        public static readonly Guid OutputNavLeft = new Guid("d08d36df-db4d-5cf5-a2a3-2a94cab02742");
        public static readonly Guid OutputNavRight = new Guid("7276da64-5a25-5897-8a8b-4c377453e7de");
        public static readonly Guid OutputNavOk = new Guid("d436db8e-146b-5128-86c2-b9b30c7848a0");
        public static readonly Guid OutputBack = new Guid("2a9d3989-e145-5378-ada1-531908baf6af");
        public static readonly Guid OutputMenu = new Guid("3ed63cc5-7fec-53d9-9b84-d4db77817696");
        public static readonly Guid OutputHome = new Guid("6eb90c4c-9bc3-5b7b-bfb9-744f3b93a472");
        public static readonly Guid ParamModeName1 = new Guid("bf942d41-5355-5060-a88d-99a4961e8fbf");
        public static readonly Guid ParamModeName2 = new Guid("e501f224-d0fa-5034-9677-4cc817556601");
        public static readonly Guid ParamModeName3 = new Guid("5897bcda-2074-51da-b56e-93c3c69aef50");
        public static readonly Guid ParamModeName4 = new Guid("5609c662-3500-59c0-913a-551f9b791218");
        public static readonly Guid ParamModeName5 = new Guid("4aea5867-7df8-5c9b-a0c8-809f94e5c250");
        public static readonly Guid ParamModeName6 = new Guid("d03d5737-694e-5c1e-af44-eb0e53f1d03a");
        public static readonly Guid ParamModeName7 = new Guid("c7f28fa7-a912-5d3b-834a-e603bb90f14f");
        public static readonly Guid ParamModeName8 = new Guid("57c432ee-b5f2-5cf6-9fe5-098046d6ce75");
        public static readonly Guid ParamNumpadTimeout = new Guid("62dd610e-a53e-5506-a0de-8455c0e15a9c");
        public static readonly Guid OutputCommand = new Guid("c133eb41-50db-4a1e-b266-76157abd7214");

        public static readonly Guid[] InputModes = { InputMode1, InputMode2, InputMode3, InputMode4, InputMode5, InputMode6, InputMode7, InputMode8 };
        public static readonly Guid[] OutputModeActives = { OutputModeActive1, OutputModeActive2, OutputModeActive3, OutputModeActive4, OutputModeActive5, OutputModeActive6, OutputModeActive7, OutputModeActive8 };
        public static readonly Guid[] ParamModeNames = { ParamModeName1, ParamModeName2, ParamModeName3, ParamModeName4, ParamModeName5, ParamModeName6, ParamModeName7, ParamModeName8 };

        public override string LogicName => "P3.Logic.Operations.MediaController";
        public override Guid LogicGuid => new Guid("3ece4c89-cc8a-5413-ab1d-751908f3ec6e");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "OPERATIONS.MEDIA_CONTROLLER.NAME", "OPERATIONS.MEDIA_CONTROLLER.DESCRIPTION", "operations-media-controller", "OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputPowerToggle, "OPERATIONS.MEDIA_CONTROLLER.INPUT.POWER_TOGGLE.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.POWER_TOGGLE.DESCRIPTION", "powerToggle", LogicGuid, LogicInterfaceDirection.Input, 0, 1);
            factory.CreateLogicInterfaceTemplate(InputPowerOn, "OPERATIONS.MEDIA_CONTROLLER.INPUT.POWER_ON.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.POWER_ON.DESCRIPTION", "powerOn", LogicGuid, LogicInterfaceDirection.Input, 0, 2);
            factory.CreateLogicInterfaceTemplate(InputPowerOff, "OPERATIONS.MEDIA_CONTROLLER.INPUT.POWER_OFF.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.POWER_OFF.DESCRIPTION", "powerOff", LogicGuid, LogicInterfaceDirection.Input, 0, 3);
            factory.CreateLogicInterfaceTemplate(InputVolumeUp, "OPERATIONS.MEDIA_CONTROLLER.INPUT.VOLUME_UP.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.VOLUME_UP.DESCRIPTION", "volumeUp", LogicGuid, LogicInterfaceDirection.Input, 0, 4);
            factory.CreateLogicInterfaceTemplate(InputVolumeDown, "OPERATIONS.MEDIA_CONTROLLER.INPUT.VOLUME_DOWN.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.VOLUME_DOWN.DESCRIPTION", "volumeDown", LogicGuid, LogicInterfaceDirection.Input, 0, 5);
            factory.CreateLogicInterfaceTemplate(InputVolume, "OPERATIONS.MEDIA_CONTROLLER.INPUT.VOLUME.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.VOLUME.DESCRIPTION", "volume", LogicGuid, LogicInterfaceDirection.Input, 0, 6);
            factory.CreateLogicInterfaceTemplate(InputMute, "OPERATIONS.MEDIA_CONTROLLER.INPUT.MUTE.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.MUTE.DESCRIPTION", "mute", LogicGuid, LogicInterfaceDirection.Input, 0, 7);
            factory.CreateLogicInterfaceTemplate(InputChannelUp, "OPERATIONS.MEDIA_CONTROLLER.INPUT.CHANNEL_UP.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.CHANNEL_UP.DESCRIPTION", "channelUp", LogicGuid, LogicInterfaceDirection.Input, 0, 8);
            factory.CreateLogicInterfaceTemplate(InputChannelDown, "OPERATIONS.MEDIA_CONTROLLER.INPUT.CHANNEL_DOWN.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.CHANNEL_DOWN.DESCRIPTION", "channelDown", LogicGuid, LogicInterfaceDirection.Input, 0, 9);
            factory.CreateLogicInterfaceTemplate(InputChannel, "OPERATIONS.MEDIA_CONTROLLER.INPUT.CHANNEL.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.CHANNEL.DESCRIPTION", "channel", LogicGuid, LogicInterfaceDirection.Input, 0, 10);
            factory.CreateLogicInterfaceTemplate(InputDigit, "OPERATIONS.MEDIA_CONTROLLER.INPUT.DIGIT.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.DIGIT.DESCRIPTION", "digit", LogicGuid, LogicInterfaceDirection.Input, 0, 11);
            factory.CreateLogicInterfaceTemplate(InputMode, "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE.DESCRIPTION", "mode", LogicGuid, LogicInterfaceDirection.Input, 0, 12);
            factory.CreateLogicInterfaceTemplate(InputMode1, "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE1.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE1.DESCRIPTION", "mode1", LogicGuid, LogicInterfaceDirection.Input, 0, 13);
            factory.CreateLogicInterfaceTemplate(InputMode2, "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE2.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE2.DESCRIPTION", "mode2", LogicGuid, LogicInterfaceDirection.Input, 0, 14);
            factory.CreateLogicInterfaceTemplate(InputMode3, "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE3.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE3.DESCRIPTION", "mode3", LogicGuid, LogicInterfaceDirection.Input, 0, 15);
            factory.CreateLogicInterfaceTemplate(InputMode4, "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE4.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE4.DESCRIPTION", "mode4", LogicGuid, LogicInterfaceDirection.Input, 0, 16);
            factory.CreateLogicInterfaceTemplate(InputMode5, "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE5.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE5.DESCRIPTION", "mode5", LogicGuid, LogicInterfaceDirection.Input, 0, 17);
            factory.CreateLogicInterfaceTemplate(InputMode6, "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE6.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE6.DESCRIPTION", "mode6", LogicGuid, LogicInterfaceDirection.Input, 0, 18);
            factory.CreateLogicInterfaceTemplate(InputMode7, "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE7.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE7.DESCRIPTION", "mode7", LogicGuid, LogicInterfaceDirection.Input, 0, 19);
            factory.CreateLogicInterfaceTemplate(InputMode8, "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE8.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.MODE8.DESCRIPTION", "mode8", LogicGuid, LogicInterfaceDirection.Input, 0, 20);
            factory.CreateLogicInterfaceTemplate(InputPlayPause, "OPERATIONS.MEDIA_CONTROLLER.INPUT.PLAY_PAUSE.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.PLAY_PAUSE.DESCRIPTION", "playPause", LogicGuid, LogicInterfaceDirection.Input, 0, 21);
            factory.CreateLogicInterfaceTemplate(InputNext, "OPERATIONS.MEDIA_CONTROLLER.INPUT.NEXT.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.NEXT.DESCRIPTION", "next", LogicGuid, LogicInterfaceDirection.Input, 0, 22);
            factory.CreateLogicInterfaceTemplate(InputPrevious, "OPERATIONS.MEDIA_CONTROLLER.INPUT.PREVIOUS.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.PREVIOUS.DESCRIPTION", "previous", LogicGuid, LogicInterfaceDirection.Input, 0, 23);
            factory.CreateLogicInterfaceTemplate(InputNavUp, "OPERATIONS.MEDIA_CONTROLLER.INPUT.NAV_UP.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.NAV_UP.DESCRIPTION", "navUp", LogicGuid, LogicInterfaceDirection.Input, 0, 24);
            factory.CreateLogicInterfaceTemplate(InputNavDown, "OPERATIONS.MEDIA_CONTROLLER.INPUT.NAV_DOWN.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.NAV_DOWN.DESCRIPTION", "navDown", LogicGuid, LogicInterfaceDirection.Input, 0, 25);
            factory.CreateLogicInterfaceTemplate(InputNavLeft, "OPERATIONS.MEDIA_CONTROLLER.INPUT.NAV_LEFT.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.NAV_LEFT.DESCRIPTION", "navLeft", LogicGuid, LogicInterfaceDirection.Input, 0, 26);
            factory.CreateLogicInterfaceTemplate(InputNavRight, "OPERATIONS.MEDIA_CONTROLLER.INPUT.NAV_RIGHT.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.NAV_RIGHT.DESCRIPTION", "navRight", LogicGuid, LogicInterfaceDirection.Input, 0, 27);
            factory.CreateLogicInterfaceTemplate(InputNavOk, "OPERATIONS.MEDIA_CONTROLLER.INPUT.NAV_OK.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.NAV_OK.DESCRIPTION", "navOk", LogicGuid, LogicInterfaceDirection.Input, 0, 28);
            factory.CreateLogicInterfaceTemplate(InputBack, "OPERATIONS.MEDIA_CONTROLLER.INPUT.BACK.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.BACK.DESCRIPTION", "back", LogicGuid, LogicInterfaceDirection.Input, 0, 29);
            factory.CreateLogicInterfaceTemplate(InputMenu, "OPERATIONS.MEDIA_CONTROLLER.INPUT.MENU.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.MENU.DESCRIPTION", "menu", LogicGuid, LogicInterfaceDirection.Input, 0, 30);
            factory.CreateLogicInterfaceTemplate(InputHome, "OPERATIONS.MEDIA_CONTROLLER.INPUT.HOME.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.HOME.DESCRIPTION", "home", LogicGuid, LogicInterfaceDirection.Input, 0, 31);
            factory.CreateLogicInterfaceTemplate(InputLock, "OPERATIONS.MEDIA_CONTROLLER.INPUT.LOCK.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.LOCK.DESCRIPTION", "lock", LogicGuid, LogicInterfaceDirection.Input, 0, 32);
            factory.CreateLogicInterfaceTemplate(InputDisablePeriphery, "OPERATIONS.MEDIA_CONTROLLER.INPUT.DISABLE_PERIPHERY.NAME", "OPERATIONS.MEDIA_CONTROLLER.INPUT.DISABLE_PERIPHERY.DESCRIPTION", "disablePeriphery", LogicGuid, LogicInterfaceDirection.Input, 0, 33);
            factory.CreateLogicInterfaceTemplate(OutputMode, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE.DESCRIPTION", "modeOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateLogicInterfaceTemplate(OutputPower, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.POWER.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.POWER.DESCRIPTION", "powerOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 2);
            factory.CreateLogicInterfaceTemplate(OutputModeActive1, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE_ACTIVE1.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE_ACTIVE1.DESCRIPTION", "modeActive1", LogicGuid, LogicInterfaceDirection.Output, 0, 3);
            factory.CreateLogicInterfaceTemplate(OutputModeActive2, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE_ACTIVE2.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE_ACTIVE2.DESCRIPTION", "modeActive2", LogicGuid, LogicInterfaceDirection.Output, 0, 4);
            factory.CreateLogicInterfaceTemplate(OutputModeActive3, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE_ACTIVE3.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE_ACTIVE3.DESCRIPTION", "modeActive3", LogicGuid, LogicInterfaceDirection.Output, 0, 5);
            factory.CreateLogicInterfaceTemplate(OutputModeActive4, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE_ACTIVE4.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE_ACTIVE4.DESCRIPTION", "modeActive4", LogicGuid, LogicInterfaceDirection.Output, 0, 6);
            factory.CreateLogicInterfaceTemplate(OutputModeActive5, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE_ACTIVE5.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE_ACTIVE5.DESCRIPTION", "modeActive5", LogicGuid, LogicInterfaceDirection.Output, 0, 7);
            factory.CreateLogicInterfaceTemplate(OutputModeActive6, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE_ACTIVE6.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE_ACTIVE6.DESCRIPTION", "modeActive6", LogicGuid, LogicInterfaceDirection.Output, 0, 8);
            factory.CreateLogicInterfaceTemplate(OutputModeActive7, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE_ACTIVE7.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE_ACTIVE7.DESCRIPTION", "modeActive7", LogicGuid, LogicInterfaceDirection.Output, 0, 9);
            factory.CreateLogicInterfaceTemplate(OutputModeActive8, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE_ACTIVE8.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MODE_ACTIVE8.DESCRIPTION", "modeActive8", LogicGuid, LogicInterfaceDirection.Output, 0, 10);
            factory.CreateLogicInterfaceTemplate(OutputVolumeUp, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.VOLUME_UP.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.VOLUME_UP.DESCRIPTION", "volumeUpOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 11);
            factory.CreateLogicInterfaceTemplate(OutputVolumeDown, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.VOLUME_DOWN.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.VOLUME_DOWN.DESCRIPTION", "volumeDownOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 12);
            factory.CreateLogicInterfaceTemplate(OutputVolume, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.VOLUME.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.VOLUME.DESCRIPTION", "volumeOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 13);
            factory.CreateLogicInterfaceTemplate(OutputMute, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MUTE.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MUTE.DESCRIPTION", "muteOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 14);
            factory.CreateLogicInterfaceTemplate(OutputChannelUp, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.CHANNEL_UP.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.CHANNEL_UP.DESCRIPTION", "channelUpOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 15);
            factory.CreateLogicInterfaceTemplate(OutputChannelDown, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.CHANNEL_DOWN.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.CHANNEL_DOWN.DESCRIPTION", "channelDownOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 16);
            factory.CreateLogicInterfaceTemplate(OutputChannel, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.CHANNEL.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.CHANNEL.DESCRIPTION", "channelOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 17);
            factory.CreateLogicInterfaceTemplate(OutputPlayPause, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.PLAY_PAUSE.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.PLAY_PAUSE.DESCRIPTION", "playPauseOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 18);
            factory.CreateLogicInterfaceTemplate(OutputNext, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.NEXT.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.NEXT.DESCRIPTION", "nextOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 19);
            factory.CreateLogicInterfaceTemplate(OutputPrevious, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.PREVIOUS.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.PREVIOUS.DESCRIPTION", "previousOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 20);
            factory.CreateLogicInterfaceTemplate(OutputNavUp, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.NAV_UP.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.NAV_UP.DESCRIPTION", "navUpOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 21);
            factory.CreateLogicInterfaceTemplate(OutputNavDown, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.NAV_DOWN.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.NAV_DOWN.DESCRIPTION", "navDownOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 22);
            factory.CreateLogicInterfaceTemplate(OutputNavLeft, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.NAV_LEFT.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.NAV_LEFT.DESCRIPTION", "navLeftOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 23);
            factory.CreateLogicInterfaceTemplate(OutputNavRight, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.NAV_RIGHT.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.NAV_RIGHT.DESCRIPTION", "navRightOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 24);
            factory.CreateLogicInterfaceTemplate(OutputNavOk, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.NAV_OK.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.NAV_OK.DESCRIPTION", "navOkOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 25);
            factory.CreateLogicInterfaceTemplate(OutputBack, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.BACK.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.BACK.DESCRIPTION", "backOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 26);
            factory.CreateLogicInterfaceTemplate(OutputMenu, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MENU.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.MENU.DESCRIPTION", "menuOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 27);
            factory.CreateLogicInterfaceTemplate(OutputHome, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.HOME.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.HOME.DESCRIPTION", "homeOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 28);
            // for the "Media Controller Mode" blocks, one per device
            factory.CreateLogicInterfaceTemplate(OutputCommand, "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.COMMAND.NAME", "OPERATIONS.MEDIA_CONTROLLER.OUTPUT.COMMAND.DESCRIPTION", "commandOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 29);
            factory.CreateParameterLogicInterfaceTemplate(ParamModeName1, "OPERATIONS.MEDIA_CONTROLLER.PARAM.MODE_NAME1.NAME", "OPERATIONS.MEDIA_CONTROLLER.PARAM.MODE_NAME1.DESCRIPTION", "mode1Name", LogicGuid, 1, RuleInterfaceParameterDataType.Text, "TV", false);
            factory.CreateParameterLogicInterfaceTemplate(ParamModeName2, "OPERATIONS.MEDIA_CONTROLLER.PARAM.MODE_NAME2.NAME", "OPERATIONS.MEDIA_CONTROLLER.PARAM.MODE_NAME2.DESCRIPTION", "mode2Name", LogicGuid, 2, RuleInterfaceParameterDataType.Text, "Radio", false);
            factory.CreateParameterLogicInterfaceTemplate(ParamModeName3, "OPERATIONS.MEDIA_CONTROLLER.PARAM.MODE_NAME3.NAME", "OPERATIONS.MEDIA_CONTROLLER.PARAM.MODE_NAME3.DESCRIPTION", "mode3Name", LogicGuid, 3, RuleInterfaceParameterDataType.Text, "", false);
            factory.CreateParameterLogicInterfaceTemplate(ParamModeName4, "OPERATIONS.MEDIA_CONTROLLER.PARAM.MODE_NAME4.NAME", "OPERATIONS.MEDIA_CONTROLLER.PARAM.MODE_NAME4.DESCRIPTION", "mode4Name", LogicGuid, 4, RuleInterfaceParameterDataType.Text, "", false);
            factory.CreateParameterLogicInterfaceTemplate(ParamModeName5, "OPERATIONS.MEDIA_CONTROLLER.PARAM.MODE_NAME5.NAME", "OPERATIONS.MEDIA_CONTROLLER.PARAM.MODE_NAME5.DESCRIPTION", "mode5Name", LogicGuid, 5, RuleInterfaceParameterDataType.Text, "", false);
            factory.CreateParameterLogicInterfaceTemplate(ParamModeName6, "OPERATIONS.MEDIA_CONTROLLER.PARAM.MODE_NAME6.NAME", "OPERATIONS.MEDIA_CONTROLLER.PARAM.MODE_NAME6.DESCRIPTION", "mode6Name", LogicGuid, 6, RuleInterfaceParameterDataType.Text, "", false);
            factory.CreateParameterLogicInterfaceTemplate(ParamModeName7, "OPERATIONS.MEDIA_CONTROLLER.PARAM.MODE_NAME7.NAME", "OPERATIONS.MEDIA_CONTROLLER.PARAM.MODE_NAME7.DESCRIPTION", "mode7Name", LogicGuid, 7, RuleInterfaceParameterDataType.Text, "", false);
            factory.CreateParameterLogicInterfaceTemplate(ParamModeName8, "OPERATIONS.MEDIA_CONTROLLER.PARAM.MODE_NAME8.NAME", "OPERATIONS.MEDIA_CONTROLLER.PARAM.MODE_NAME8.DESCRIPTION", "mode8Name", LogicGuid, 8, RuleInterfaceParameterDataType.Text, "", false);
            factory.CreateParameterLogicInterfaceTemplate(ParamNumpadTimeout, "OPERATIONS.MEDIA_CONTROLLER.PARAM.NUMPAD_TIMEOUT.NAME", "OPERATIONS.MEDIA_CONTROLLER.PARAM.NUMPAD_TIMEOUT.DESCRIPTION", "numpadTimeout", LogicGuid, 9, RuleInterfaceParameterDataType.Integer, 3000, false);

            // the tile of this block is a core visu template (enum value 21); older cores without it keep the default logic tile,
            // so the plugin still builds and runs against the published core NuGet
            if (Enum.GetName(typeof(VisuMobileObjectTemplateTypes), 21) == "MediaController")
            {
                factory.ChangeDefaultVisuTemplate(LogicGuid, (VisuMobileObjectTemplateTypes)21);
            }
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new MediaControllerLogic(context);
        }
    }
}
