import { Component, OnDestroy, OnInit, ChangeDetectionStrategy } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { LogHubService } from "src/app/base/communication/hubs/log-hub.service";
import { LogsService } from "src/app/services/logs.service";
import { LogLevel } from "@microsoft/signalr";
import ArrayStore from "devextreme/data/array_store";
import CustomStore from "devextreme/data/custom_store";
import { saveAs } from "file-saver";

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

// keep the live log bounded, oldest entries are dropped in place so the grid binding stays the same array
const MAX_LOG_ENTRIES = 5000;

function pushCapped<T>(list: T[], entry: T) {
  list.push(entry);
  if (list.length > MAX_LOG_ENTRIES) {
    list.splice(0, list.length - MAX_LOG_ENTRIES);
  }
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
    styleUrls: ["./logs.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
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
    appService.setAppTitle("UI.LOGS.TITLE");

    const logFacility = { name: this.allFacilityName, logs: [] };
    this.logFacilities.set(this.allFacilityName, logFacility);
    this.logFacilitiesArray.push(logFacility);
    this.selectedFacility = logFacility;

    const levels = [LogLevel.Trace, LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical, LogLevel.None];
    for (const level of levels) {
      this.logLevels.push({ key: level, name: this.levelName(level) });
    }

    this.logLevelsDataSource = new ArrayStore({
      data: this.logLevels,
      key: 'key'
    });
  }



  /** live logging is off until the user starts it */
  toggleLive() {
    this.stop = !this.stop;
  }

  get liveCount(): number {
    return this.logFacilities.get(this.allFacilityName)?.logs.length ?? 0;
  }

  facilityLabel(f: LogFacility) {
    if (f.name === this.allFacilityName) {
      return this.translate.translate("UI.LOGS.ALL_FACILITIES");
    }
    return f.name;
  }

  isAllFacility(f: LogFacility) {
    return f?.name === this.allFacilityName;
  }

  clearSelected() {
    if (this.selectedFacility) {
      this.selectedFacility.logs.splice(0, this.selectedFacility.logs.length);
    }
  }

  levelName(level: number): string {
    switch (level) {
      case LogLevel.Trace: return this.translate.translate("UI.LOGS.LEVEL_TRACE");
      case LogLevel.Debug: return this.translate.translate("UI.LOGS.LEVEL_DEBUG");
      case LogLevel.Information: return this.translate.translate("UI.LOGS.LEVEL_INFO");
      case LogLevel.Warning: return this.translate.translate("UI.LOGS.LEVEL_WARNING");
      case LogLevel.Error: return this.translate.translate("UI.LOGS.LEVEL_ERROR");
      case LogLevel.Critical: return this.translate.translate("UI.LOGS.LEVEL_CRITICAL");
      case LogLevel.None: return this.translate.translate("UI.LOGS.LEVEL_NONE");
    }
    return String(level);
  }

  levelTone(level: number): string {
    switch (level) {
      case LogLevel.Information: return "accent";
      case LogLevel.Warning: return "warn";
      case LogLevel.Error:
      case LogLevel.Critical: return "bad";
    }
    return "";
  }

  async ngOnInit() {
    const loadingDone = this.appService.beginLoading();
    // level names follow the language
    this.registerObservable(this.translate.onChange(), () => {
      for (const l of this.logLevels) {
        l.name = this.levelName(l.key);
      }
    });
    try {
      await this.load();

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
        pushCapped(this.logFacilities.get(facility).logs, { facility: facility, log: log, timestamp: new Date() });
        // the combined list keeps the real source so it can be shown next to the message
        pushCapped(this.logFacilities.get(this.allFacilityName).logs, { facility: facility, log: log, timestamp: new Date() });
      });

    } catch (error) {
      this.handleError(error);
    } finally {
      loadingDone();
    }
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
    if ($event.addedItems && $event.addedItems.length > 0) {
      this.selectedFacility = $event.addedItems[0];
    }
  }

  async onSelectionChanged($event) {
    var logFile = $event.selectedRowsData[0] as LogFile;

    if (!logFile?.isFile) {
      return;
    }
    this.isLoading = true;

    try {
      this.selectedLogFile = logFile;
      var log = String(await this.logsService.getLogFile(logFile.path!) ?? "");

      let blob = new Blob([log], { type: "text/plain" });

      let pwa = window.open('', '_blank');
      if (!pwa || pwa.closed || typeof pwa.closed == 'undefined' || !pwa.document) {
        // pop-up blocked or no windows (app): download the file instead
        saveAs(blob, logFile.name);
        return;
      }

      let url = window.URL.createObjectURL(blob);

      var iframe = pwa.document.createElement('iframe');
      var title = pwa.document.createElement('title');
      title.appendChild(pwa.document.createTextNode(logFile.name));

      iframe.src = url;
      iframe.width = '100%';
      iframe.height = '100%';
      iframe.style.border = 'none';

      pwa.document.head.appendChild(title);
      pwa.document.body.appendChild(iframe);
      pwa.document.body.style.margin = "0";
    } catch (error) {
      this.handleError(error);
    } finally {
      this.isLoading = false;
    }
  }

  logLevelEnumName = (data) => {
    return this.levelName(data.logLevel);
  }

  ngOnDestroy(): void {
    super.baseOnDestroy();
  }
}
