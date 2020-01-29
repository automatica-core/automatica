import { Component, OnInit, OnDestroy } from "@angular/core";
import { TranslationService } from "angular-l10n";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { RuleInstanceVisuService } from "src/app/services/rule-visu.service";
import { NotifyService } from "src/app/services/notify.service";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { ConfigService } from "src/app/services/config.service";
import { AppService } from "src/app/services/app.service";


enum WindowState {
  Undefined,
  Open = 1,
  Closed = 2,
  Tilt = 3,
  Locked = 4,
  Unlocked = 5
}

interface WindowStateData {
  Id: string;
  Name: string;
  Value: WindowState;
}

@Component({
  selector: "lib-window-monitor",
  templateUrl: "./window-monitor.component.html",
  styleUrls: ["./window-monitor.component.scss"]
})
export class WindowMonitorComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

  data: WindowStateData[] = [];

  openCount = 0;
  tiltCount = 0;
  unlockedCount = 0;

  constructor(
    dataHubService: DataHubService,
    notify: NotifyService,
    translate: TranslationService,
    visu: RuleInstanceVisuService,
    configService: ConfigService,
    appService: AppService) {
    super(dataHubService, notify, translate, configService, visu, appService);
  }

  async ngOnInit() {
    await super.mobileRuleInit();



  }

  protected onRuleInstanceValueChanged(data: WindowStateData[]) {
    this.data = data;

    let openCount = 0;
    let tiltCount = 0;
    let unlockedCount = 0;
    for (const x of this.data) {

      switch (x.Value) {
        case WindowState.Open:
          openCount++;
          break;
        case WindowState.Tilt:
          tiltCount++;
          break;
        case WindowState.Unlocked:
          unlockedCount++;
          break;
      }
    }

    this.openCount = openCount;
    this.tiltCount = tiltCount;
    this.unlockedCount = unlockedCount;
  }


  public onItemResized() {

  }

  ngOnDestroy(): void {
    super.baseOnDestroy();
  }

}
