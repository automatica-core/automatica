import { Component, OnInit, OnDestroy } from "@angular/core";
import { TelegramMonitorInstance } from "src/app/base/model/telegram-monitor/telegram-monitor-instance";
import { TelegramHubService } from "src/app/base/communication/hubs/telegram-monitor-hub.service";
import { TranslationService } from "angular-l10n";
import { TelegramMessage } from "src/app/base/model/telegram-monitor/telegram-message";
import { NotifyService } from "src/app/services/notify.service";
import { TelegramMonitorService } from "src/app/services/telegram-monitor.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";

@Component({
  selector: "app-telegram-monitor",
  templateUrl: "./telegram-monitor.component.html",
  styleUrls: ["./telegram-monitor.component.scss"]
})
export class TelegramMonitorComponent extends BaseComponent implements OnInit, OnDestroy {

  private _selectedMonitorInstance: TelegramMonitorInstance;

  public monitorInstances: TelegramMonitorInstance[] = [];

  constructor(private telegramHub: TelegramHubService,
    notify: NotifyService,
    translate: TranslationService,
    private telegramMonitorService: TelegramMonitorService,
    appService: AppService) {
    super(notify, translate);

    appService.setAppTitle("TELEGRAM_MONITOR.NAME");
  }


  public get selectedMonitorInstance(): TelegramMonitorInstance {
    return this._selectedMonitorInstance;
  }
  public set selectedMonitorInstance(v: TelegramMonitorInstance) {
    this._selectedMonitorInstance = v;
  }


  async ngOnInit() {

    this.monitorInstances = await this.telegramMonitorService.getMonitorInstances();
    if (this.monitorInstances.length > 0) {
      this.selectedMonitorInstance = this.monitorInstances[0];
    }

    super.registerEvent(this.telegramHub.onTelegram, (t) => {
      const data = t[0];

      const telegramMessage = new TelegramMessage();
      TelegramMessage.fromJson(data, telegramMessage);

      const busMon = this.getMonitorInstance(telegramMessage.BusId);

      if (busMon) {
        busMon.Messages.push(telegramMessage);
      }
    });
  }

  async onTabSelectionChanged($event) {
    const item: TelegramMonitorInstance = $event.addedItems[0];
  }

  calculateDirectionValue($event: TelegramMessage) {
    if ($event.Direction === 0) {
      return "INPUT <<";
    }
    return "OUTPUT <<";
  }

  private getMonitorInstance(id: string) {
    const items = this.monitorInstances.filter(a => a.Id === id);

    if (items && items.length > 0) {
      return items[0];
    }
    return void 0;
  }


  ngOnDestroy() {
    this.baseOnDestroy();
  }
}
