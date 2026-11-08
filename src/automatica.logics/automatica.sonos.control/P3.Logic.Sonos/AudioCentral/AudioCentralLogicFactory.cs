using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Sonos.AudioCentral
{
    /// <summary>
    /// Audio Central: Controls several Sonos Control blocks together: on/off, volume (keeps the ratio between the players), favorites, doorbell and alarm sounds (modelled on the Loxone function block of the same name)
    /// </summary>
    public class AudioCentralLogicFactory : LogicFactory
    {
        public static readonly Guid InputToggle = new Guid("5fcb0095-e951-5d37-ac5b-f1cd51f776ac");
        public static readonly Guid InputOn = new Guid("9cd77605-9661-5185-9a64-d88c5df1ff47");
        public static readonly Guid InputOff = new Guid("429d3a1c-029b-5e55-bed2-ca2115e601ae");
        public static readonly Guid InputVolumeUp = new Guid("a27ae78d-9ba0-5a59-ad7b-5dd831ae4829");
        public static readonly Guid InputVolumeDown = new Guid("e32e9dcf-9404-586e-a7ee-afa90d61f543");
        public static readonly Guid InputVolume = new Guid("d46e1246-341b-5383-9fe0-7fc21b04b6f8");
        public static readonly Guid InputNextFavorite = new Guid("56fbf728-b4a9-5930-a637-58dfd98e56c5");
        public static readonly Guid InputPlay = new Guid("662a3cb6-6d15-5676-b7ae-1bab573db6fd");
        public static readonly Guid InputPause = new Guid("90e22b9f-b89a-5046-a10d-4f4fa91bf199");
        public static readonly Guid InputNext = new Guid("ef992835-a1e0-5c48-a024-02477be25af6");
        public static readonly Guid InputPrevious = new Guid("c546dbe8-5415-52e9-b74b-9792198768f0");
        public static readonly Guid InputBell = new Guid("ec15b052-2bd1-5964-b085-59c6a98df90f");
        public static readonly Guid InputAlarm = new Guid("2ad7b031-8f8d-5647-b385-74134e18047f");
        public static readonly Guid InputLock = new Guid("ba47cb27-27ce-592e-a95f-4538f0a70377");
        public static readonly Guid InputDisablePeriphery = new Guid("f81b5c25-0b7d-5b2f-90e5-bc3c07595a18");
        public static readonly Guid InputPlaying1 = new Guid("bede3644-ef0d-5e4c-9ca1-251b590b36c2");
        public static readonly Guid InputPlaying2 = new Guid("1f8c6eb7-71cc-5d9a-a413-57598df10542");
        public static readonly Guid InputPlaying3 = new Guid("6d3bbeca-cd96-537c-a3b4-2979920f2e5b");
        public static readonly Guid InputPlaying4 = new Guid("1dc014fc-0ade-557a-ac9a-64fb098aa1ed");
        public static readonly Guid InputPlaying5 = new Guid("283f6bd6-f6d1-5a70-bcb9-041ecc9cdf4f");
        public static readonly Guid InputPlaying6 = new Guid("b913f9d7-e426-5144-8b26-6f4d4254655c");
        public static readonly Guid InputPlaying7 = new Guid("3eb5bffd-79f1-525d-a969-e95df73903b0");
        public static readonly Guid InputPlaying8 = new Guid("5fff2786-8b72-52ec-9769-4153558a9fa7");
        public static readonly Guid InputPlayerVolume1 = new Guid("b05a48e9-d4df-543c-a8b6-7e7ecc528de0");
        public static readonly Guid InputPlayerVolume2 = new Guid("513f8a41-763c-59fa-bee8-567b1080640b");
        public static readonly Guid InputPlayerVolume3 = new Guid("c857bc5a-b5d9-5f17-8c6e-2fc4dc57c69a");
        public static readonly Guid InputPlayerVolume4 = new Guid("64e295da-05eb-5787-a750-cac96a7a274c");
        public static readonly Guid InputPlayerVolume5 = new Guid("b837501a-d9ad-546e-8c45-5abf3ae07667");
        public static readonly Guid InputPlayerVolume6 = new Guid("32f4851b-ff86-5c42-9fe5-662f94d1ec55");
        public static readonly Guid InputPlayerVolume7 = new Guid("2abe992d-ff84-5d5f-8470-a0499ebeedfc");
        public static readonly Guid InputPlayerVolume8 = new Guid("8514f08d-41eb-5b60-bb92-f7c3bf586e42");
        public static readonly Guid OutputPlay = new Guid("e204191a-7aea-5a5f-b1a8-949d78b74379");
        public static readonly Guid OutputVolume = new Guid("0b4d99e4-204c-5328-a1a3-523890475857");
        public static readonly Guid OutputVolumeUp = new Guid("df8fa570-0623-5679-97d3-318122dbcdc9");
        public static readonly Guid OutputVolumeDown = new Guid("5044eb0a-27ca-55fe-bedc-ad6415edfc23");
        public static readonly Guid OutputNext = new Guid("2814ef79-bdf4-5995-a6c4-06dfa8fdbf25");
        public static readonly Guid OutputPrevious = new Guid("4657972a-3104-585e-af60-e6685b773376");
        public static readonly Guid OutputRadioStation = new Guid("af6df0f3-83de-5dda-81e8-d1a197d0d73f");
        public static readonly Guid OutputSoundUrl = new Guid("ba7bef30-e285-53c6-993b-dcd3aceb66ee");
        public static readonly Guid OutputSoundTrigger = new Guid("5d0a2485-8ad1-59c5-bf4d-642fe049ce13");
        public static readonly Guid OutputPlayerVolume1 = new Guid("750b9e1e-fe81-516f-9229-437ba0b7d6ca");
        public static readonly Guid OutputPlayerVolume2 = new Guid("1d3ad71f-6ea9-5e1b-a2c3-a01299b1484d");
        public static readonly Guid OutputPlayerVolume3 = new Guid("01a2279b-b5d0-58b5-9fbd-8072d554c848");
        public static readonly Guid OutputPlayerVolume4 = new Guid("5ac6d91e-e25d-5c12-bb9b-9549e16ae56b");
        public static readonly Guid OutputPlayerVolume5 = new Guid("c10cb11c-c9f7-5f7f-89f5-7224ca433e5f");
        public static readonly Guid OutputPlayerVolume6 = new Guid("f1901961-05b7-5789-a53a-ddc624e28a5f");
        public static readonly Guid OutputPlayerVolume7 = new Guid("200e9d4a-3b36-55de-914d-91e4ba0af998");
        public static readonly Guid OutputPlayerVolume8 = new Guid("ce259985-6c40-5fef-83b7-8d767647532a");
        public static readonly Guid OutputActiveCount = new Guid("1dd94fca-2f2d-505d-9b54-8f4cfa92b85c");
        public static readonly Guid ParamFavorites = new Guid("75196a9d-fb7c-549f-9f64-cf478ff1a059");

        /// <summary>
        /// Ö3, FM4 and Ö1: station keys of the core's station catalog (older TuneIn ids such as s8007 keep working)
        /// </summary>
        public const string DefaultFavorites = "at-oe3;at-fm4;at-oe1";
        public static readonly Guid ParamBellUrl = new Guid("571afa9a-c4d4-5770-ab99-c2f0493f699b");
        public static readonly Guid ParamAlarmUrl = new Guid("58f9fb2a-20a9-5adc-9f70-8499f7eab24b");

        public static readonly Guid[] InputPlayings = { InputPlaying1, InputPlaying2, InputPlaying3, InputPlaying4, InputPlaying5, InputPlaying6, InputPlaying7, InputPlaying8 };
        public static readonly Guid[] InputPlayerVolumes = { InputPlayerVolume1, InputPlayerVolume2, InputPlayerVolume3, InputPlayerVolume4, InputPlayerVolume5, InputPlayerVolume6, InputPlayerVolume7, InputPlayerVolume8 };
        public static readonly Guid[] OutputPlayerVolumes = { OutputPlayerVolume1, OutputPlayerVolume2, OutputPlayerVolume3, OutputPlayerVolume4, OutputPlayerVolume5, OutputPlayerVolume6, OutputPlayerVolume7, OutputPlayerVolume8 };

        public override string LogicName => "SonosAudioCentral";
        public override Guid LogicGuid => new Guid("0e77c46d-998e-56db-9a98-95c64ee73950");
        public override Version LogicVersion => new Version(1, 1, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "SONOS_AUDIO_CENTRAL.NAME", "SONOS_AUDIO_CENTRAL.DESCRIPTION", "sonos.audio-central", "SONOS_CONTROL.GROUP.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputToggle, "SONOS_AUDIO_CENTRAL.INPUT.TOGGLE.NAME", "SONOS_AUDIO_CENTRAL.INPUT.TOGGLE.DESCRIPTION", "toggle", LogicGuid, LogicInterfaceDirection.Input, 0, 1);
            factory.CreateLogicInterfaceTemplate(InputOn, "SONOS_AUDIO_CENTRAL.INPUT.ON.NAME", "SONOS_AUDIO_CENTRAL.INPUT.ON.DESCRIPTION", "on", LogicGuid, LogicInterfaceDirection.Input, 0, 2);
            factory.CreateLogicInterfaceTemplate(InputOff, "SONOS_AUDIO_CENTRAL.INPUT.OFF.NAME", "SONOS_AUDIO_CENTRAL.INPUT.OFF.DESCRIPTION", "off", LogicGuid, LogicInterfaceDirection.Input, 0, 3);
            factory.CreateLogicInterfaceTemplate(InputVolumeUp, "SONOS_AUDIO_CENTRAL.INPUT.VOLUME_UP.NAME", "SONOS_AUDIO_CENTRAL.INPUT.VOLUME_UP.DESCRIPTION", "volumeUp", LogicGuid, LogicInterfaceDirection.Input, 0, 4);
            factory.CreateLogicInterfaceTemplate(InputVolumeDown, "SONOS_AUDIO_CENTRAL.INPUT.VOLUME_DOWN.NAME", "SONOS_AUDIO_CENTRAL.INPUT.VOLUME_DOWN.DESCRIPTION", "volumeDown", LogicGuid, LogicInterfaceDirection.Input, 0, 5);
            factory.CreateLogicInterfaceTemplate(InputVolume, "SONOS_AUDIO_CENTRAL.INPUT.VOLUME.NAME", "SONOS_AUDIO_CENTRAL.INPUT.VOLUME.DESCRIPTION", "volume", LogicGuid, LogicInterfaceDirection.Input, 0, 6);
            factory.CreateLogicInterfaceTemplate(InputNextFavorite, "SONOS_AUDIO_CENTRAL.INPUT.NEXT_FAVORITE.NAME", "SONOS_AUDIO_CENTRAL.INPUT.NEXT_FAVORITE.DESCRIPTION", "nextFavorite", LogicGuid, LogicInterfaceDirection.Input, 0, 7);
            factory.CreateLogicInterfaceTemplate(InputPlay, "SONOS_AUDIO_CENTRAL.INPUT.PLAY.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PLAY.DESCRIPTION", "play", LogicGuid, LogicInterfaceDirection.Input, 0, 8);
            factory.CreateLogicInterfaceTemplate(InputPause, "SONOS_AUDIO_CENTRAL.INPUT.PAUSE.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PAUSE.DESCRIPTION", "pause", LogicGuid, LogicInterfaceDirection.Input, 0, 9);
            factory.CreateLogicInterfaceTemplate(InputNext, "SONOS_AUDIO_CENTRAL.INPUT.NEXT.NAME", "SONOS_AUDIO_CENTRAL.INPUT.NEXT.DESCRIPTION", "next", LogicGuid, LogicInterfaceDirection.Input, 0, 10);
            factory.CreateLogicInterfaceTemplate(InputPrevious, "SONOS_AUDIO_CENTRAL.INPUT.PREVIOUS.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PREVIOUS.DESCRIPTION", "previous", LogicGuid, LogicInterfaceDirection.Input, 0, 11);
            factory.CreateLogicInterfaceTemplate(InputBell, "SONOS_AUDIO_CENTRAL.INPUT.BELL.NAME", "SONOS_AUDIO_CENTRAL.INPUT.BELL.DESCRIPTION", "bell", LogicGuid, LogicInterfaceDirection.Input, 0, 12);
            factory.CreateLogicInterfaceTemplate(InputAlarm, "SONOS_AUDIO_CENTRAL.INPUT.ALARM.NAME", "SONOS_AUDIO_CENTRAL.INPUT.ALARM.DESCRIPTION", "alarm", LogicGuid, LogicInterfaceDirection.Input, 0, 13);
            factory.CreateLogicInterfaceTemplate(InputLock, "SONOS_AUDIO_CENTRAL.INPUT.LOCK.NAME", "SONOS_AUDIO_CENTRAL.INPUT.LOCK.DESCRIPTION", "lock", LogicGuid, LogicInterfaceDirection.Input, 0, 14);
            factory.CreateLogicInterfaceTemplate(InputDisablePeriphery, "SONOS_AUDIO_CENTRAL.INPUT.DISABLE_PERIPHERY.NAME", "SONOS_AUDIO_CENTRAL.INPUT.DISABLE_PERIPHERY.DESCRIPTION", "disablePeriphery", LogicGuid, LogicInterfaceDirection.Input, 0, 15);
            factory.CreateLogicInterfaceTemplate(InputPlaying1, "SONOS_AUDIO_CENTRAL.INPUT.PLAYING1.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PLAYING1.DESCRIPTION", "playing1", LogicGuid, LogicInterfaceDirection.Input, 0, 16);
            factory.CreateLogicInterfaceTemplate(InputPlaying2, "SONOS_AUDIO_CENTRAL.INPUT.PLAYING2.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PLAYING2.DESCRIPTION", "playing2", LogicGuid, LogicInterfaceDirection.Input, 0, 17);
            factory.CreateLogicInterfaceTemplate(InputPlaying3, "SONOS_AUDIO_CENTRAL.INPUT.PLAYING3.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PLAYING3.DESCRIPTION", "playing3", LogicGuid, LogicInterfaceDirection.Input, 0, 18);
            factory.CreateLogicInterfaceTemplate(InputPlaying4, "SONOS_AUDIO_CENTRAL.INPUT.PLAYING4.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PLAYING4.DESCRIPTION", "playing4", LogicGuid, LogicInterfaceDirection.Input, 0, 19);
            factory.CreateLogicInterfaceTemplate(InputPlaying5, "SONOS_AUDIO_CENTRAL.INPUT.PLAYING5.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PLAYING5.DESCRIPTION", "playing5", LogicGuid, LogicInterfaceDirection.Input, 0, 20);
            factory.CreateLogicInterfaceTemplate(InputPlaying6, "SONOS_AUDIO_CENTRAL.INPUT.PLAYING6.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PLAYING6.DESCRIPTION", "playing6", LogicGuid, LogicInterfaceDirection.Input, 0, 21);
            factory.CreateLogicInterfaceTemplate(InputPlaying7, "SONOS_AUDIO_CENTRAL.INPUT.PLAYING7.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PLAYING7.DESCRIPTION", "playing7", LogicGuid, LogicInterfaceDirection.Input, 0, 22);
            factory.CreateLogicInterfaceTemplate(InputPlaying8, "SONOS_AUDIO_CENTRAL.INPUT.PLAYING8.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PLAYING8.DESCRIPTION", "playing8", LogicGuid, LogicInterfaceDirection.Input, 0, 23);
            factory.CreateLogicInterfaceTemplate(InputPlayerVolume1, "SONOS_AUDIO_CENTRAL.INPUT.PLAYER_VOLUME1.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PLAYER_VOLUME1.DESCRIPTION", "playerVolume1", LogicGuid, LogicInterfaceDirection.Input, 0, 24);
            factory.CreateLogicInterfaceTemplate(InputPlayerVolume2, "SONOS_AUDIO_CENTRAL.INPUT.PLAYER_VOLUME2.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PLAYER_VOLUME2.DESCRIPTION", "playerVolume2", LogicGuid, LogicInterfaceDirection.Input, 0, 25);
            factory.CreateLogicInterfaceTemplate(InputPlayerVolume3, "SONOS_AUDIO_CENTRAL.INPUT.PLAYER_VOLUME3.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PLAYER_VOLUME3.DESCRIPTION", "playerVolume3", LogicGuid, LogicInterfaceDirection.Input, 0, 26);
            factory.CreateLogicInterfaceTemplate(InputPlayerVolume4, "SONOS_AUDIO_CENTRAL.INPUT.PLAYER_VOLUME4.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PLAYER_VOLUME4.DESCRIPTION", "playerVolume4", LogicGuid, LogicInterfaceDirection.Input, 0, 27);
            factory.CreateLogicInterfaceTemplate(InputPlayerVolume5, "SONOS_AUDIO_CENTRAL.INPUT.PLAYER_VOLUME5.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PLAYER_VOLUME5.DESCRIPTION", "playerVolume5", LogicGuid, LogicInterfaceDirection.Input, 0, 28);
            factory.CreateLogicInterfaceTemplate(InputPlayerVolume6, "SONOS_AUDIO_CENTRAL.INPUT.PLAYER_VOLUME6.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PLAYER_VOLUME6.DESCRIPTION", "playerVolume6", LogicGuid, LogicInterfaceDirection.Input, 0, 29);
            factory.CreateLogicInterfaceTemplate(InputPlayerVolume7, "SONOS_AUDIO_CENTRAL.INPUT.PLAYER_VOLUME7.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PLAYER_VOLUME7.DESCRIPTION", "playerVolume7", LogicGuid, LogicInterfaceDirection.Input, 0, 30);
            factory.CreateLogicInterfaceTemplate(InputPlayerVolume8, "SONOS_AUDIO_CENTRAL.INPUT.PLAYER_VOLUME8.NAME", "SONOS_AUDIO_CENTRAL.INPUT.PLAYER_VOLUME8.DESCRIPTION", "playerVolume8", LogicGuid, LogicInterfaceDirection.Input, 0, 31);
            factory.CreateLogicInterfaceTemplate(OutputPlay, "SONOS_AUDIO_CENTRAL.OUTPUT.PLAY.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.PLAY.DESCRIPTION", "playOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateLogicInterfaceTemplate(OutputVolume, "SONOS_AUDIO_CENTRAL.OUTPUT.VOLUME.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.VOLUME.DESCRIPTION", "volumeOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 2);
            factory.CreateLogicInterfaceTemplate(OutputVolumeUp, "SONOS_AUDIO_CENTRAL.OUTPUT.VOLUME_UP.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.VOLUME_UP.DESCRIPTION", "volumeUpOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 3);
            factory.CreateLogicInterfaceTemplate(OutputVolumeDown, "SONOS_AUDIO_CENTRAL.OUTPUT.VOLUME_DOWN.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.VOLUME_DOWN.DESCRIPTION", "volumeDownOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 4);
            factory.CreateLogicInterfaceTemplate(OutputNext, "SONOS_AUDIO_CENTRAL.OUTPUT.NEXT.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.NEXT.DESCRIPTION", "nextOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 5);
            factory.CreateLogicInterfaceTemplate(OutputPrevious, "SONOS_AUDIO_CENTRAL.OUTPUT.PREVIOUS.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.PREVIOUS.DESCRIPTION", "previousOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 6);
            factory.CreateLogicInterfaceTemplate(OutputRadioStation, "SONOS_AUDIO_CENTRAL.OUTPUT.RADIO_STATION.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.RADIO_STATION.DESCRIPTION", "radioStationOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 7);
            factory.CreateLogicInterfaceTemplate(OutputSoundUrl, "SONOS_AUDIO_CENTRAL.OUTPUT.SOUND_URL.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.SOUND_URL.DESCRIPTION", "soundUrlOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 8);
            factory.CreateLogicInterfaceTemplate(OutputSoundTrigger, "SONOS_AUDIO_CENTRAL.OUTPUT.SOUND_TRIGGER.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.SOUND_TRIGGER.DESCRIPTION", "soundTriggerOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 9);
            factory.CreateLogicInterfaceTemplate(OutputPlayerVolume1, "SONOS_AUDIO_CENTRAL.OUTPUT.PLAYER_VOLUME1.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.PLAYER_VOLUME1.DESCRIPTION", "playerVolumeOutput1", LogicGuid, LogicInterfaceDirection.Output, 0, 10);
            factory.CreateLogicInterfaceTemplate(OutputPlayerVolume2, "SONOS_AUDIO_CENTRAL.OUTPUT.PLAYER_VOLUME2.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.PLAYER_VOLUME2.DESCRIPTION", "playerVolumeOutput2", LogicGuid, LogicInterfaceDirection.Output, 0, 11);
            factory.CreateLogicInterfaceTemplate(OutputPlayerVolume3, "SONOS_AUDIO_CENTRAL.OUTPUT.PLAYER_VOLUME3.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.PLAYER_VOLUME3.DESCRIPTION", "playerVolumeOutput3", LogicGuid, LogicInterfaceDirection.Output, 0, 12);
            factory.CreateLogicInterfaceTemplate(OutputPlayerVolume4, "SONOS_AUDIO_CENTRAL.OUTPUT.PLAYER_VOLUME4.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.PLAYER_VOLUME4.DESCRIPTION", "playerVolumeOutput4", LogicGuid, LogicInterfaceDirection.Output, 0, 13);
            factory.CreateLogicInterfaceTemplate(OutputPlayerVolume5, "SONOS_AUDIO_CENTRAL.OUTPUT.PLAYER_VOLUME5.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.PLAYER_VOLUME5.DESCRIPTION", "playerVolumeOutput5", LogicGuid, LogicInterfaceDirection.Output, 0, 14);
            factory.CreateLogicInterfaceTemplate(OutputPlayerVolume6, "SONOS_AUDIO_CENTRAL.OUTPUT.PLAYER_VOLUME6.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.PLAYER_VOLUME6.DESCRIPTION", "playerVolumeOutput6", LogicGuid, LogicInterfaceDirection.Output, 0, 15);
            factory.CreateLogicInterfaceTemplate(OutputPlayerVolume7, "SONOS_AUDIO_CENTRAL.OUTPUT.PLAYER_VOLUME7.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.PLAYER_VOLUME7.DESCRIPTION", "playerVolumeOutput7", LogicGuid, LogicInterfaceDirection.Output, 0, 16);
            factory.CreateLogicInterfaceTemplate(OutputPlayerVolume8, "SONOS_AUDIO_CENTRAL.OUTPUT.PLAYER_VOLUME8.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.PLAYER_VOLUME8.DESCRIPTION", "playerVolumeOutput8", LogicGuid, LogicInterfaceDirection.Output, 0, 17);
            factory.CreateLogicInterfaceTemplate(OutputActiveCount, "SONOS_AUDIO_CENTRAL.OUTPUT.ACTIVE_COUNT.NAME", "SONOS_AUDIO_CENTRAL.OUTPUT.ACTIVE_COUNT.DESCRIPTION", "activeCount", LogicGuid, LogicInterfaceDirection.Output, 0, 18);
            factory.CreateParameterLogicInterfaceTemplate(ParamFavorites, "SONOS_AUDIO_CENTRAL.PARAM.FAVORITES.NAME", "SONOS_AUDIO_CENTRAL.PARAM.FAVORITES.DESCRIPTION", "favorites", LogicGuid, 1, RuleInterfaceParameterDataType.RadioStations, DefaultFavorites, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamBellUrl, "SONOS_AUDIO_CENTRAL.PARAM.BELL_URL.NAME", "SONOS_AUDIO_CENTRAL.PARAM.BELL_URL.DESCRIPTION", "bellUrl", LogicGuid, 2, RuleInterfaceParameterDataType.Text, "", false);
            factory.CreateParameterLogicInterfaceTemplate(ParamAlarmUrl, "SONOS_AUDIO_CENTRAL.PARAM.ALARM_URL.NAME", "SONOS_AUDIO_CENTRAL.PARAM.ALARM_URL.DESCRIPTION", "alarmUrl", LogicGuid, 3, RuleInterfaceParameterDataType.Text, "", false);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new AudioCentralLogic(context);
        }
    }
}
