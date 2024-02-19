import { Component, OnDestroy, OnInit } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { LogHubService } from "src/app/base/communication/hubs/log-hub.service";
import { CustomMenuItem } from "src/app/base/model/custom-menu-item";
import { LogsService } from "src/app/services/logs.service";
import { LogLevel } from "@microsoft/signalr";
import ArrayStore from "devextreme/data/array_store";
import CustomStore from "devextreme/data/custom_store";

interface LogFacility {
  name: string;
  logs: LogEntry[];
}
interface Logger {
  facility: string;
  logLevel: LogLevel;
}

interface LogEntry {
  facility: string;
  log: string;
  timestamp: Date;
}

interface LogFile {
  name: string;
  path?: string;
  isFile: boolean;

  key: number;

  children: LogFile[];
}


@Component({
  selector: "app-logs",
  templateUrl: "./logs.component.html",
  styleUrls: ["./logs.component.scss"]
})
export class LogsComponent extends BaseComponent implements OnInit, OnDestroy {

  logFacilities: Map<string, LogFacility> = new Map<string, LogFacility>();

  logFacilitiesArray: LogFacility[] = [];
  logger: Logger[] = [];

  loggerDataSource: CustomStore;

  selectedFacility: LogFacility;
  selectedLogger: Logger;

  stop: boolean = true;

  logFileTree: LogFile[];

  logLevels: { key: number, name: string }[] = [];
  logLevelsDataSource;

  menuItems: CustomMenuItem[] = [];
  menuStartStop: CustomMenuItem = {
    id: "start",
    label: "Start",
    icon: "fa-start",
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

  menuRefresh: CustomMenuItem = {
    id: "refres",
    label: "Refresh",
    icon: "fa-refreshn",
    items: undefined,
    command: (event) => { this.load(); }
  }


  private allFacilityName: string = "all_log";
  selectedLogFile: LogFile;
  focusedRowKey: number;

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
    this.menuItems.push(this.menuRefresh);

    this.logLevels.push({ key: LogLevel.Trace, name: LogLevel[LogLevel.Trace] });
    this.logLevels.push({ key: LogLevel.Information, name: LogLevel[LogLevel.Information] });
    this.logLevels.push({ key: LogLevel.Debug, name: LogLevel[LogLevel.Debug] });
    this.logLevels.push({ key: LogLevel.Warning, name: LogLevel[LogLevel.Warning] });
    this.logLevels.push({ key: LogLevel.Error, name: LogLevel[LogLevel.Error] });
    this.logLevels.push({ key: LogLevel.Critical, name: LogLevel[LogLevel.Critical] });
    this.logLevels.push({ key: LogLevel.None, name: LogLevel[LogLevel.None] });

    this.logLevelsDataSource = new ArrayStore({
      data: this.logLevels,
      key: 'key'
    });
  }



  itemClick($event) {
    const item: CustomMenuItem = $event.itemData;

    if (item && item.command) {
      item.command($event);
    }
  }

  async ngOnInit() {
    this.appService.isLoading = true;
    await this.load();
    try {

      super.registerEvent(this.loggingHub.pushEventLog, (data) => {

        let facility: string = data[0];
        const log = data[1];

        facility = facility.replaceAll("||sep||", "/");

        if (!this.logFacilities.has(facility)) {
          const logFacility = { name: facility, logs: [] };

          this.logFacilities.set(facility, logFacility);
          this.logFacilitiesArray.push(logFacility);
        }
        if (this.stop) {
          return;
        }
        this.logFacilities.get(facility).logs.push({ facility: facility, log: log, timestamp: new Date() });
        this.logFacilities.get(this.allFacilityName).logs.push({ facility: this.allFacilityName, log: log, timestamp: new Date() });
      });

    } catch (error) {
      this.handleError(error);
    }

    this.appService.isLoading = false;
  }

  async load() {
    let logFiles = await this.logsService.getLogFiles();

    this.logFileTree = [this.generateTreeFromPaths(logFiles)];

    this.logger = JSON.parse(await this.logsService.getLogger());

    this.loggerDataSource = new CustomStore({

      load: async () => JSON.parse(await this.logsService.getLogger()),
      update: async (key, value) => {
        await this.logsService.setLogLevel(key, value.logLevel);
      },
      key: "facility"
    });

  }

  generateTreeFromPaths(paths: string[]): LogFile {
    let key = 0;
    const root: LogFile = { name: "root", isFile: false, children: [], path: "", key: key };


    const pathMap: Map<string, LogFile> = new Map();

    pathMap.set("root", root);

    for (const path of paths) {
      const segments = path.split("/");
      let parent = root;

      for (let i = 0; i < segments.length; i++) {
        const segment = segments[i];
        if (segment.trim() === "") {
          continue;
        }
        const fullPath = segments.slice(0, i + 1).join("/");

        if (!pathMap.has(fullPath)) {
          key++;
          const newNode: LogFile = {
            name: segment,
            isFile: i === segments.length - 1,
            path: i === segments.length - 1 ? fullPath : void 0,
            children: [],
            key: key
          };
          pathMap.set(fullPath, newNode);
          parent.children.push(newNode);
        }
        parent = pathMap.get(fullPath)!;
      }
    }

    return root;
  }

  onTabSelectionChanged($event) {
    this.selectedFacility = $event.addedItems[0];
  }

  async onSelectionChanged($event) {
    var logFile = $event.selectedRowsData[0] as LogFile;

    if (!logFile.isFile) {
      return;
    }
    this.isLoading = true;

    this.selectedLogFile = logFile;
    var log = (await this.logsService.getLogFile(logFile.path!)).toString();

    let blob = new Blob([log], { type: "text/plain" });
    let url = window.URL.createObjectURL(blob);

    var iframe = document.createElement('iframe');
    var title = document.createElement('title');
    title.appendChild(document.createTextNode(logFile.name));

    iframe.src = url;
    iframe.width = '100%';
    iframe.height = '100%';
    iframe.style.border = 'none';

    let pwa = window.open('', '_blank');
    pwa.document.head.appendChild(title);
    pwa.document.body.appendChild(iframe);
    pwa.document.body.style.margin = "0";
    if (!pwa || pwa.closed || typeof pwa.closed == 'undefined') {
      alert('Please disable your Pop-up blocker and try again.');
    }

    this.isLoading = false;
  }

  logLevelEnumName(data) {
    return LogLevel[data.logLevel];
  }

  ngOnDestroy(): void {
    super.baseOnDestroy();
  }
}
