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

  selectedFacility: LogFacility;

  stop: boolean = false;

  logFileTree: LogFile[];


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

  async load() {
    var logFiles = await this.logsService.getLogFiles();

    this.logFileTree = [this.generateTreeFromPaths(logFiles)];
  }

  generateTreeFromPaths(paths: string[]): LogFile {
    var key = 0;
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
    let pwa = window.open(url);
    if (!pwa || pwa.closed || typeof pwa.closed == 'undefined') {
      alert('Please disable your Pop-up blocker and try again.');
    }
    
    this.isLoading = false;
  }

  ngOnDestroy(): void {
    super.baseOnDestroy();
  }
}
