using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Sonos.SonosControl;

public class SonosControlLogicFactory : LogicFactory
{
    public override Version LogicVersion => new Version(1, 0, 0, 0);

    public override bool InDevelopmentMode => true;

    public override string LogicName => "SonosControl";
    public override Guid LogicGuid => new Guid("550c0290-40e3-4d60-b016-3588d2a367fe");

    //Inputs
    public static readonly Guid PlayPauseTrigger = new Guid("93a1e9b7-77df-41ff-a02e-55722d3281ab");
    public static readonly Guid PlayDefaultTrigger = new Guid("10a12584-9d70-47f8-985b-6e8151e56bbe");
    public static readonly Guid PauseTrigger = new Guid("8cc610dd-235e-4f7c-accf-9c1624c0e35f");
    public static readonly Guid Volume = new Guid("473461b1-f28b-4159-a85b-1ec824a808a6");

    public static readonly Guid VolumeIncrement = new Guid("63fd6575-1c29-4537-b26f-f3d795bd4220");
    public static readonly Guid VolumeDecrement = new Guid("efdc7c97-f6da-4b64-a1f8-cb8ae576620d");
    public static readonly Guid Next = new Guid("1049fcc8-ecd8-43cd-be15-c351eadb75be");
    public static readonly Guid Previous = new Guid("b8e791ca-b290-4205-9f8f-365bb9b73501");
    public static readonly Guid RadioStationInput = new Guid("c24e12b1-79a1-4cc4-a926-b46bfa181625");


    public static readonly Guid TitleInput = new Guid("e2ea8f7b-c7f5-4b07-ac27-57e04ad59b2a");
    public static readonly Guid CreatorInput = new Guid("83b0a122-511c-478e-bcbd-4faf96afd747");
    public static readonly Guid AlbumInput = new Guid("e9fc6360-8c5e-4c34-afbe-37b3f265f95c");
    public static readonly Guid AlbumArtUriInput = new Guid("44cead6b-bbf4-4370-8dd4-89dc70034196");
    public static readonly Guid ClassInput = new Guid("91d716a7-9c7b-4dad-a4bb-01fa9c7e2534");

    public static readonly Guid DurationInput = new Guid("d799bf2f-a39f-47ca-9ab5-c28fe02fb7b9");
    public static readonly Guid RelativeTimeInput = new Guid("bb59da7b-826f-439d-88a5-eb630185c0aa");

    //Params
    public static readonly Guid VolumeOnPlay = new Guid("c1af8a31-094b-4411-9db3-1cca9ee73235");
    public static readonly Guid RadioStation = new Guid("01bf3fb1-cac8-43c4-95c9-c024fde1a2af");
    public static readonly Guid MaxVolume = new Guid("ac7e676e-d562-4f38-b66b-4581a44bd9ba");


    //Outputs
    public static readonly Guid PlayOutputStatus = new Guid("e182d7b3-3644-47c6-899f-4b1d1afe1348");
    public static readonly Guid PauseOutputStatus = new Guid("caf3ee10-a41e-4c60-97f7-b86eb71cfb20");
    public static readonly Guid VolumeOutputStatus = new Guid("fc9b4105-ca37-46a2-afa0-89c281d5b30a");
    public static readonly Guid RadioStationOutputValue = new Guid("9a12a267-338c-4a48-9d7a-5af4fe8189be");
    public static readonly Guid NextOutput = new Guid("2aa90a39-8410-4e26-b707-96d7a5f10342");
    public static readonly Guid PreviousOutput = new Guid("d3fa805c-94f7-49e9-a979-fb421339db00");

    public static readonly long DefaultVolume = 10;
    public static readonly string DefaultRadioStation = "s8007";


    public override void InitTemplates(ILogicTemplateFactory factory)
    {
        factory.CreateLogicTemplate(LogicGuid, "SONOS_CONTROL.NAME", "SONOS_CONTROL.DESCRIPTION", "sonos.control", "SONOS.NAME", 100, 100);

        factory.CreateParameterLogicInterfaceTemplate(RadioStation, "SONOS_CONTROL.RADIO_STATION.NAME",
            "SONOS_CONTROL.RADIO_STATION.DESCRIPTION", LogicGuid, 1, RuleInterfaceParameterDataType.Text, DefaultRadioStation);
        factory.CreateParameterLogicInterfaceTemplate(VolumeOnPlay, "SONOS_CONTROL.VOLUME_ON_START.NAME",
            "SONOS_CONTROL.VOLUME_ON_START.DESCRIPTION", LogicGuid, 2, RuleInterfaceParameterDataType.Integer, DefaultVolume);
        factory.CreateParameterLogicInterfaceTemplate(MaxVolume, "SONOS_CONTROL.MAX_VOLUME.NAME",
            "SONOS_CONTROL.MAX_VOLUME.DESCRIPTION", LogicGuid, 3, RuleInterfaceParameterDataType.Integer, 100);

        factory.CreateLogicInterfaceTemplate(PlayPauseTrigger, "SONOS_CONTROL.PLAY_PAUSE.NAME", "SONOS_CONTROL.PLAY_PAUSE.DESCRIPTION", "play_pause", LogicGuid, LogicInterfaceDirection.Input, 0, 0, RuleInterfaceType.Input);
        factory.CreateLogicInterfaceTemplate(PlayDefaultTrigger, "SONOS_CONTROL.PLAY_DEFAULT.NAME", "SONOS_CONTROL.PLAY_DEFAULT.DESCRIPTION", "play_default", LogicGuid, LogicInterfaceDirection.Input, 0, 1, RuleInterfaceType.Input);
        factory.CreateLogicInterfaceTemplate(PauseTrigger, "SONOS_CONTROL.PAUSE_INPUT_STATUS.NAME", "SONOS_CONTROL.PAUSE_INPUT_STATUS.DESCRIPTION", "pause", LogicGuid, LogicInterfaceDirection.Input, 0, 2, RuleInterfaceType.Input);
        factory.CreateLogicInterfaceTemplate(Volume, "SONOS_CONTROL.VOLUME.NAME", "SONOS_CONTROL.VOLUME.DESCRIPTION", "volume", LogicGuid, LogicInterfaceDirection.Input, 0, 3, RuleInterfaceType.Input);
        factory.CreateLogicInterfaceTemplate(VolumeIncrement, "SONOS_CONTROL.VOLUME_INCREMENT.NAME", "SONOS_CONTROL.VOLUME_INCREMENT.DESCRIPTION", "volume+", LogicGuid, LogicInterfaceDirection.Input, 0, 4, RuleInterfaceType.Input);
        factory.CreateLogicInterfaceTemplate(VolumeDecrement, "SONOS_CONTROL.VOLUME_DECREMENT.NAME", "SONOS_CONTROL.VOLUME_DECREMENT.DESCRIPTION", "volume-", LogicGuid, LogicInterfaceDirection.Input, 0, 5, RuleInterfaceType.Input);
        factory.CreateLogicInterfaceTemplate(Previous, "SONOS_CONTROL.PREV.NAME", "SONOS_CONTROL.PREV.DESCRIPTION", "prev", LogicGuid, LogicInterfaceDirection.Input, 0, 6, RuleInterfaceType.Input);
        factory.CreateLogicInterfaceTemplate(Next, "SONOS_CONTROL.NEXT.NAME", "SONOS_CONTROL.NEXT.DESCRIPTION", "next", LogicGuid, LogicInterfaceDirection.Input, 0, 7, RuleInterfaceType.Input);
        factory.CreateLogicInterfaceTemplate(RadioStationInput, "SONOS_CONTROL.RADIO_STATION.NAME", "SONOS_CONTROL.RADIO_STATION.DESCRIPTION", "radio_station", LogicGuid, LogicInterfaceDirection.Input, 0, 8, RuleInterfaceType.Input);


        //additional inputs for UI
        factory.CreateLogicInterfaceTemplate(TitleInput, "SONOS_CONTROL.STATE.TITLE.NAME", "SONOS_CONTROL.STATE.TITLE.DESCRIPTION", "title", LogicGuid, LogicInterfaceDirection.Input, 1, 9, RuleInterfaceType.Input);
        factory.CreateLogicInterfaceTemplate(CreatorInput, "SONOS_CONTROL.STATE.CREATOR.NAME", "SONOS_CONTROL.STATE.CREATOR.DESCRIPTION", "creator", LogicGuid, LogicInterfaceDirection.Input, 1, 10, RuleInterfaceType.Input);
        factory.CreateLogicInterfaceTemplate(AlbumInput, "SONOS_CONTROL.STATE.ALBUM.NAME", "SONOS_CONTROL.STATE.ALBUM.DESCRIPTION", "album", LogicGuid, LogicInterfaceDirection.Input, 1, 11, RuleInterfaceType.Input);
        factory.CreateLogicInterfaceTemplate(AlbumArtUriInput, "SONOS_CONTROL.STATE.ALBUM_ART_URI.NAME", "SONOS_CONTROL.STATE.ALBUM_ART_URI.DESCRIPTION", "album_art_uri", LogicGuid, LogicInterfaceDirection.Input, 1, 12, RuleInterfaceType.Input);
        factory.CreateLogicInterfaceTemplate(ClassInput, "SONOS_CONTROL.STATE.CLASS.NAME", "SONOS_CONTROL.STATE.CLASS.DESCRIPTION", "class", LogicGuid, LogicInterfaceDirection.Input, 1, 13, RuleInterfaceType.Input);
        factory.CreateLogicInterfaceTemplate(DurationInput, "SONOS_CONTROL.STATE.DURATION.NAME", "SONOS_CONTROL.STATE.DURATION.DESCRIPTION", "duration", LogicGuid, LogicInterfaceDirection.Input, 1, 14, RuleInterfaceType.Input);
        factory.CreateLogicInterfaceTemplate(RelativeTimeInput, "SONOS_CONTROL.STATE.RELATIVE_TIME.NAME", "SONOS_CONTROL.STATE.RELATIVE_TIME.DESCRIPTION", "relative_time", LogicGuid, LogicInterfaceDirection.Input, 1, 15, RuleInterfaceType.Input);


        factory.CreateLogicInterfaceTemplate(PlayOutputStatus, "SONOS_CONTROL.PLAY_OUTPUT_STATE.NAME", "SONOS_CONTROL.PLAY_OUTPUT_STATE.DESCRIPTION", "play", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Output);
        factory.CreateLogicInterfaceTemplate(PauseOutputStatus, "SONOS_CONTROL.PAUSE_OUTPUT_STATE.NAME", "SONOS_CONTROL.PAUSE_OUTPUT_STATE.DESCRIPTION", "pause", LogicGuid, LogicInterfaceDirection.Output, 0, 2, RuleInterfaceType.Output);
        factory.CreateLogicInterfaceTemplate(VolumeOutputStatus, "SONOS_CONTROL.VOLUME_OUTPUT_STATE.NAME", "SONOS_CONTROL.VOLUME_OUTPUT_STATE.DESCRIPTION", "volume", LogicGuid, LogicInterfaceDirection.Output, 0, 3, RuleInterfaceType.Output);
        factory.CreateLogicInterfaceTemplate(RadioStationOutputValue, "SONOS_CONTROL.RADIO_STATION_OUTPUT_VALUE.NAME", "SONOS_CONTROL.RADIO_STATION_OUTPUT_VALUE.DESCRIPTION", "radio_station", LogicGuid, LogicInterfaceDirection.Output, 0, 4, RuleInterfaceType.Output);
        factory.CreateLogicInterfaceTemplate(PreviousOutput, "SONOS_CONTROL.PREV.NAME", "SONOS_CONTROL.PREV.DESCRIPTION", "prev", LogicGuid, LogicInterfaceDirection.Output, 0, 5, RuleInterfaceType.Output);
        factory.CreateLogicInterfaceTemplate(NextOutput, "SONOS_CONTROL.NEXT.NAME", "SONOS_CONTROL.NEXT.DESCRIPTION", "next", LogicGuid, LogicInterfaceDirection.Output, 0, 6, RuleInterfaceType.Output);

        factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.MediaPlayer);

    }

    public override ILogic CreateLogicInstance(ILogicContext context)
    {
        return new SonosControlLogic(context);
    }
}