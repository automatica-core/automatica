import { Component, OnDestroy, OnInit } from '@angular/core';
import { L10nTranslationService, toNumber } from 'angular-l10n';
import { DataHubService } from 'src/app/base/communication/hubs/data-hub.service';
import { RuleInterfaceInstance } from 'src/app/base/model/rule-interface-instance';
import { RuleInterfaceType } from 'src/app/base/model/rule-interface-template';
import { AppService } from 'src/app/services/app.service';
import { ConfigService } from 'src/app/services/config.service';
import { NotifyService } from 'src/app/services/notify.service';
import { LogicInstanceVisuService } from 'src/app/services/logic-visu.service';
import { BaseMobileRuleComponent } from '../../base-mobile-rule-component';

@Component({
  selector: 'app-media-player',
  templateUrl: './media-player.component.html',
  styleUrls: ['./media-player.component.scss']
})
export class MediaPlayerComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {


  private _isPlaying: boolean;
  public get isPlaying(): boolean {
    return this._isPlaying;
  }
  public set isPlaying(v: boolean) {
    if (this._isPlaying == v) {
      return;
    }
    this._isPlaying = v;
  }


  private _volume: number;
  public get volume(): number {
    return this._volume;
  }
  public set volume(v: number) {
    this._volume = v;

  }


  private _radioStation: string;
  public get radioStation(): string {
    return this._radioStation;
  }
  public set radioStation(v: string) {
    this._radioStation = v;

    this.dataHub.setValue(this.radioStationState.ObjId, v);
  }



  playPauseState: RuleInterfaceInstance;
  volumeState: RuleInterfaceInstance;
  radioStationState: RuleInterfaceInstance;

  titleState: RuleInterfaceInstance;
  creatorState: RuleInterfaceInstance;
  albumState: RuleInterfaceInstance;
  albumArtState: RuleInterfaceInstance;
  classState: RuleInterfaceInstance;
  durationState: RuleInterfaceInstance;
  relativeTimeState: RuleInterfaceInstance;

  title: string;
  creator: string;
  album: string;
  albumArt: string;
  class: string;
  duration: string;
  relativeTime: string;

  next: RuleInterfaceInstance;
  prev: RuleInterfaceInstance;

  
  nextOutput: RuleInterfaceInstance;
  prevOutput: RuleInterfaceInstance;
  volumeOutput: RuleInterfaceInstance;

  constructor(
    dataHubService: DataHubService,
    notify: NotifyService,
    translate: L10nTranslationService,
    configService: ConfigService,
    ruleInstanceVisuService: LogicInstanceVisuService,
    appService: AppService) {
    super(dataHubService, notify, translate, configService, ruleInstanceVisuService, appService);
  }


  ngOnDestroy(): void {

  }
  ngOnInit(): void {
    this.baseOnInit();
    super.mobileRuleInit();

    this.playPauseState = this.getInterfaceByTypeAndName(RuleInterfaceType.Input, "play_pause");
    this.volumeState = this.getInterfaceByTypeAndName(RuleInterfaceType.Input, "volume");
    this.radioStationState = this.getInterfaceByTypeAndName(RuleInterfaceType.Input, "radio_station");


    this.titleState = this.getInterfaceByTypeAndName(RuleInterfaceType.Input, "title");
    this.creatorState = this.getInterfaceByTypeAndName(RuleInterfaceType.Input, "creator");
    this.albumState = this.getInterfaceByTypeAndName(RuleInterfaceType.Input, "album");
    this.albumArtState = this.getInterfaceByTypeAndName(RuleInterfaceType.Input, "album_art_uri");
    this.classState = this.getInterfaceByTypeAndName(RuleInterfaceType.Input, "class");
    this.durationState = this.getInterfaceByTypeAndName(RuleInterfaceType.Input, "duration");
    this.relativeTimeState = this.getInterfaceByTypeAndName(RuleInterfaceType.Input, "relative_time");

    this.next = this.getInterfaceByTypeAndName(RuleInterfaceType.Input, "next");
    this.prev = this.getInterfaceByTypeAndName(RuleInterfaceType.Input, "prev");

    
    this.nextOutput = this.getInterfaceByTypeAndName(RuleInterfaceType.Output, "next");
    this.prevOutput = this.getInterfaceByTypeAndName(RuleInterfaceType.Output, "prev");
    this.volumeOutput = this.getInterfaceByTypeAndName(RuleInterfaceType.Output, "volume");

    super.registerEvent(this.dataHub.dispatchValue, async (args) => {
      const nodeId = args[1];

      this.onRuleInstanceValueChanged(nodeId, args[2].value);
    });

    this._isPlaying = this.dataHub.getCurrentValue(this.playPauseState.ObjId)?.value;
    this._volume = this.dataHub.getCurrentValue(this.volumeState.ObjId)?.value;
  }

  onRuleInstanceValueChanged(interfaceId, value) {

    if (interfaceId == this.playPauseState.ObjId) {
      this.isPlaying = value!!;
    } else if (interfaceId == this.volumeState.ObjId) {
      this.volume = toNumber(value);
    } else if (interfaceId == this.radioStationState.ObjId) {
      this.radioStation = value;
    } else if (this.titleState && interfaceId == this.titleState.ObjId) {
      if (!value?.includes("tunein.com")) {
        this.title = value;
      }
    } else if (this.creatorState && interfaceId == this.creatorState.ObjId) {
      this.creator = value;
    } else if (this.albumState && interfaceId == this.albumState.ObjId) {
      this.album = value;
    } else if (this.albumArtState && interfaceId == this.albumArtState.ObjId) {
      if (value?.startsWith("http") || value?.startsWith("https")) {
        this.albumArt = value;
      }
      else if(value?.startsWith("/Assets")) {
        this.albumArt = void 0;
      }
    } else if (this.classState && interfaceId == this.classState.ObjId) {
      this.class = value;
    } else if (this.durationState && interfaceId == this.durationState.ObjId) {
      this.duration = value;
    } else if (this.relativeTimeState && interfaceId == this.relativeTimeState.ObjId) {
      this.relativeTime = value;
    }
  }

  prevCick() {
    this.dataHub.setValue(this.prev.ObjId, true);
  }
  nextClick() {
    this.dataHub.setValue(this.next.ObjId, true);
  }

  playPauseClick($event) {
    this.isPlaying = !this.isPlaying;


    this.dataHub.setValue(this.playPauseState.ObjId, this.isPlaying);
  }

  onVolumeSliderChanged($event) {
    console.log($event);
    this.dataHub.setValue(this.volumeState.ObjId, $event.value);
  }


  format(value) {
    return `${value}%`;
  }
}
