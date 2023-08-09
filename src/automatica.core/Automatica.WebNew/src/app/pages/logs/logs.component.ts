import { Component, OnDestroy, OnInit } from "@angular/core";
import { CategoryService } from "src/app/services/categories.service";
import { L10nTranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { LogHubService } from "src/app/base/communication/hubs/log-hub.service";
import { CustomMenuItem } from "src/app/base/model/custom-menu-item";
import { LogsService } from "src/app/services/logs.service";

interface LogFacility {
  name: string;
  logs: LogEntry[];
}

interface LogEntry {
  facility: string;
  log: string;
  timestamp: Date;
}


@Component({
  selector: "app-logs",
  templateUrl: "./logs.component.html",
  styleUrls: ["./logs.component.scss"]
})
export class LogsComponent extends BaseComponent implements OnInit, OnDestroy {

  logFacilities: Map<string, LogFacility> = new Map<string, LogFacility>();

  logFacilitiesArray: LogFacility[] = [];

  selectedFacility: LogFacility;

  stop: boolean = false;

  menuItems: CustomMenuItem[] = [];
  menuStartStop: CustomMenuItem = {
    id: "stop",
    label: "Stop",
    icon: "fa-stop",
    items: undefined,
    command: (event) => {
      if (!this.stop) {
        this.menuStartStop.label = "Start";
        this.menuStartStop.icon = "fa-start";
      }
      else {
        this.menuStartStop.label = "Stop";
        this.menuStartStop.icon = "fa-stop";
      }
      this.stop = !this.stop;
    }
  }

  private allFacilityName: string = "all_log";

  constructor(
    translate: L10nTranslationService,
    notify: NotifyService,
    appService: AppService,
    private loggingHub: LogHubService,
    private logsService: LogsService) {
    super(notify, translate, appService);
    appService.setAppTitle("LOGS.NAME");

    const logFacility = { name: this.allFacilityName, logs: [] };
    this.logFacilities.set(this.allFacilityName, logFacility);
    this.logFacilitiesArray.push(logFacility);

    this.menuItems.push(this.menuStartStop);
  }
  itemClick($event) {
    const item: CustomMenuItem = $event.itemData;

    if (item && item.command) {
      item.command($event);
    }
  }

  async ngOnInit() {
    this.appService.isLoading = true;

    await this.logsService.getLogFiles();
    try {

      super.registerEvent(this.loggingHub.pushEventLog, (data) => {
        if (this.stop) {
          return;
        }
        var facility: string = data[0];
        const log = data[1];

        facility = facility.replaceAll("||sep||", "/");

        if (!this.logFacilities.has(facility)) {
          const logFacility = { name: facility, logs: [] };

          this.logFacilities.set(facility, logFacility);
          this.logFacilitiesArray.push(logFacility);
        }
        this.logFacilities.get(facility).logs.push({ facility: facility, log: log, timestamp: new Date() });
        this.logFacilities.get(this.allFacilityName).logs.push({ facility: this.allFacilityName, log: log, timestamp: new Date() });
      });

    } catch (error) {
      this.handleError(error);
    }

    this.appService.isLoading = false;
  }
  
  ngOnDestroy(): void {
    super.baseOnDestroy();
  }

}
