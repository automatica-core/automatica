import { Component, OnDestroy, OnInit } from '@angular/core';
import { L10nTranslationService } from 'angular-l10n';
import { DataHubService } from 'src/app/base/communication/hubs/data-hub.service';
import { AppService } from 'src/app/services/app.service';
import { ConfigService } from 'src/app/services/config.service';
import { NotifyService } from 'src/app/services/notify.service';
import { RuleInstanceVisuService } from 'src/app/services/rule-visu.service';
import { BaseMobileRuleComponent } from '../../base-mobile-rule-component';

@Component({
  selector: 'app-media-player',
  templateUrl: './media-player.component.html',
  styleUrls: ['./media-player.component.scss']
})
export class MediaPlayerComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {
  
  
  private _isPlaying : boolean;
  public get isPlaying() : boolean {
    return this._isPlaying;
  }
  public set isPlaying(v : boolean) {
    this._isPlaying = v;
  }
  

  constructor(
    dataHubService: DataHubService,
    notify: NotifyService,
    translate: L10nTranslationService,
    configService: ConfigService,
    ruleInstanceVisuService: RuleInstanceVisuService,
    appService: AppService) {
    super(dataHubService, notify, translate, configService, ruleInstanceVisuService, appService);
  }

  
  protected onRuleInstanceValueChanged(ruleInterfaceId: any, value: any) {
   
  }
  ngOnDestroy(): void {
   
  }
  ngOnInit(): void {
  }


  playPauseClick($event) {
    this.isPlaying = !this.isPlaying;
  }
}
