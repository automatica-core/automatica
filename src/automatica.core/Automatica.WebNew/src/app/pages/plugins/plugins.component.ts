import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";
import { UpdateHubService } from "src/app/base/communication/hubs/update-hub.service";
import { PluginType, PluginState, Version, PluginsService } from "src/app/services/plugins.service";
import { L10nTranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { CustomMenuItem } from "src/app/base/model/custom-menu-item";
import { Router } from "@angular/router";
import { DesignTimeDataService } from "src/app/services/design-time-data.service";
import { TranslationConfigService } from "src/app/services/translation-config.service";
import { HubConnectionService } from "src/app/base/communication/hubs/hub-connection.service";


function versionCompare(v1: Version, v2: Version) {
  if (v1.major !== v2.major) {
    if (v1.major > v2.major) {
      return 1;
    } else {
    } return -1;
  }

  if (v1.minor !== v2.minor) {
    if (v1.minor > v2.minor) {
      return 1;
    } else {
      return -1;
    }
  }

  if (v1.build !== v2.build) {
    if (v1.build > v2.build) {
      return 1;
    } else {
      return -1;
    }
  }

  if (v1.revision !== v2.revision) {
    if (v1.revision > v2.revision) {
      return 1;
    } else {
      return -1;
    }
  }

  return 0;
}

function versionToString(v: Version) {
  return `${v.major}.${v.minor}.${v.build}.${v.revision}`;
}

function versionParse(v: string): Version {
  const split = v.split(".");

  if (split.length === 0) {
    return void 0;
  }
  if (split.length === 1) {
    return {
      major: parseInt(split[0], 10),
      minor: 0,
      build: 0,
      revision: 0
    };
  }
  if (split.length === 2) {
    return {
      major: parseInt(split[0], 10),
      minor: parseInt(split[1], 10),
      build: 0,
      revision: 0
    };
  }
  if (split.length === 3) {
    return {
      major: parseInt(split[0], 10),
      minor: parseInt(split[1], 10),
      build: parseInt(split[2], 10),
      revision: 0
    };
  }
  if (split.length === 4) {
    return {
      major: parseInt(split[0], 10),
      minor: parseInt(split[1], 10),
      build: parseInt(split[2], 10),
      revision: parseInt(split[3], 10)
    };
  }
}



export type PluginListState = "error" | "working" | "update" | "done" | "current" | "available";

const STATE_ORDER: PluginListState[] = ["error", "working", "update", "done", "current", "available"];

export class PluginStateInstance {
  private _downloadMaxFormatted: string;
  private _downloadCurrentFormatted: string;
  private _downloadCurrent: number = 0;
  private _downloadMax: number;

  private _error: string;

  private _downloaded: boolean;
  private cloudVersion: Version;
  private corePluginVersion: Version;

  constructor(public instance: PluginState) {
    if (instance.cloudPlugin) {
      this.cloudVersion = versionParse(instance.cloudPlugin.version);
    }
    if (instance.loadedPlugin) {
      this.corePluginVersion = versionParse(instance.loadedPlugin.version);
    }
  }

  get objId() {
    return this.instance.cloudPlugin.pluginGuid;
  }

  get name() {
    return this.instance.cloudPlugin.name;
  }

  get version() {
    return this.instance.cloudPlugin.version;
  }

  get type() {
    return this.instance.cloudPlugin.pluginType === PluginType.Driver ? "Driver" : "Logic";
  }

  get isDriver(): boolean {
    return this.instance.cloudPlugin.pluginType === PluginType.Driver;
  }

  /** short name without the package prefix and factory suffix, e.g. "P3.Driver.Knx.DriverFactory" -> "Knx" */
  get displayName(): string {
    const name = this.instance.cloudPlugin.name || this.instance.cloudPlugin.componentName || "";
    const short = name
      .replace(/^(P3|Automatica(\.Core)?)\.(Drivers?|Logics?)\./i, "")
      .replace(/\.(DriverFactory|LogicFactory|Factory|Driver|Logic)$/i, "");
    return short || name;
  }

  get isWorking(): boolean {
    return !!this.downloadMax && !this.downloaded && !this.error;
  }

  get state(): PluginListState {
    if (this.error) {
      return "error";
    }
    if (this.isWorking) {
      return "working";
    }
    if (this.downloaded) {
      return "done";
    }
    if (this.isInstalled) {
      return this.cloudIsNewer ? "update" : "current";
    }
    return "available";
  }

  get progressPercent(): number {
    if (!this.downloadMax) {
      return 0;
    }
    return Math.min(100, Math.round(this.downloadCurrent / this.downloadMax * 100));
  }

  get isInstalled(): boolean {
    if (this.instance.loadedPlugin) {
      return true;
    }
    return false;
  }

  get cloudIsNewer(): boolean {
    if (!this.isInstalled) {
      return true;
    }
    const compare = versionCompare(this.cloudVersion, this.corePluginVersion);
    return compare === 1;
  }

  get installedVersion() {
    if (this.isInstalled) {
      return versionToString(this.corePluginVersion);
    }
    return "";
  }


  public get downloadMaxFormatted(): string {
    return this._downloadMaxFormatted;
  }
  public set downloadMaxFormatted(v: string) {
    this._downloadMaxFormatted = v;
  }


  public get downloadCurrentFormatted(): string {
    return this._downloadCurrentFormatted;
  }
  public set downloadCurrentFormatted(v: string) {
    this._downloadCurrentFormatted = v;
  }


  public get downloadCurrent(): number {
    return this._downloadCurrent;
  }
  public set downloadCurrent(v: number) {
    if (v <= this._downloadCurrent) {
      return;
    }

    this._downloadCurrent = v;
    this.downloadCurrentFormatted = UpdateHubService.formatBytes(v);
  }


  public get downloadMax(): number {
    return this._downloadMax;
  }
  public set downloadMax(v: number) {
    this._downloadMax = v;
    this.downloadMaxFormatted = UpdateHubService.formatBytes(v);
  }

  public get error(): string {
    return this._error;
  }
  public set error(v: string) {
    this._error = v;
  }

  public get downloaded(): boolean {
    return this._downloaded;
  }
  public set downloaded(v: boolean
  ) {
    this._downloaded = v;
  }


}

@Component({
    selector: "app-plugins",
    templateUrl: "./plugins.component.html",
    styleUrls: ["./plugins.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class PluginsComponent extends BaseComponent implements OnInit, OnDestroy {
  menuItems: CustomMenuItem[] = [];
  menuMore: CustomMenuItem = {
    id: "more",
    label: "",
    icon: "overflow",
    items: []
  }
  menuInstall: CustomMenuItem = {
    id: "install",
    label: "",
    icon: "download",
    items: undefined,
    command: (event) => { this.installAll(); }
  }
  menuInstallUpdate: CustomMenuItem = {
    id: "install-update",
    label: "",
    icon: "download",
    items: undefined,
    command: (event) => { this.installUpdateAll(); }
  }

  plugins: PluginStateInstance[] = [];
  pluginsMap = new Map<string, PluginStateInstance>();

  search = "";
  typeFilter: "all" | "driver" | "logic" = "all";
  loadError: string = void 0;
  loaded = false;

  constructor(
    private pluginsService: PluginsService,
    private updateHubService: UpdateHubService,
    notify: NotifyService,
    private translationService: L10nTranslationService,
    private designTimeDataService: DesignTimeDataService,
    private translationConfigService: TranslationConfigService,
    private hubConnectionService: HubConnectionService,
    private router: Router,
    appService: AppService) {
    super(notify, translationService, appService);

    appService.setAppTitle("PLUGINS.NAME");
  }

  get updateCount() {
    return this.plugins.filter(a => a.state === "update").length;
  }

  get availableCount() {
    return this.plugins.filter(a => a.state === "available").length;
  }

  get installedCount() {
    return this.plugins.filter(a => a.isInstalled).length;
  }

  countOf(type: "all" | "driver" | "logic") {
    if (type === "all") {
      return this.plugins.length;
    }
    return this.plugins.filter(a => a.isDriver === (type === "driver")).length;
  }

  /** problems and updates first, then installed, then the rest; filtered by search and type */
  get visiblePlugins(): PluginStateInstance[] {
    const q = (this.search || "").trim().toLowerCase();
    return this.plugins
      .filter(a => this.typeFilter === "all" || a.isDriver === (this.typeFilter === "driver"))
      .filter(a => !q || a.displayName.toLowerCase().includes(q) || (a.name || "").toLowerCase().includes(q))
      .sort((a, b) => {
        const s = STATE_ORDER.indexOf(a.state) - STATE_ORDER.indexOf(b.state);
        if (s !== 0) {
          return s;
        }
        return a.displayName.localeCompare(b.displayName);
      });
  }

  async load() {
    try {
      this.isLoading = true;
      this.loadError = void 0;
      this.plugins = [];
      this.pluginsMap.clear();
      const plugins: PluginState[] = await this.pluginsService.getPlugins();

      for (const p of plugins) {
        try {
          const instance = new PluginStateInstance(p);
          this.plugins.push(instance);
          this.pluginsMap.set(p.cloudPlugin.pluginGuid, instance);
        } catch (error) {
          console.log(p, error);
        }
      }

    } catch (error) {
      this.loadError = String(this.parseErrorString(error) ?? "");
      this.handleError(error);
    }

    this.loaded = true;
    this.isLoading = false;
  }

  async ngOnInit() {
    const loadingDone = this.appService.beginLoading();
    this.hubConnectionService.init();
    this.registerObservable(this.translationService.onChange(), () => {
      this.menuInstall.label = this.translationService.translate("UI.PLUGINS.INSTALL_ALL");
      this.menuInstallUpdate.label = this.translationService.translate("UI.PLUGINS.INSTALL_UPDATE_ALL");
      // new array so the menu picks up the translated labels
      this.menuMore.items = [this.menuInstall, this.menuInstallUpdate];
      this.menuItems = [this.menuMore];
    });

    this.menuMore.items = [this.menuInstall, this.menuInstallUpdate];
    this.menuItems = [this.menuMore];

    this.baseOnInit();

    try {
      await this.load();

      super.registerEvent(this.updateHubService.PluginDownloadProgressChanged, (a) => {
        const instance = this.pluginsMap.get(a[0][0]);
        if (!instance) {
          return;
        }

        instance.downloadMax = a[0][2];
        instance.downloadCurrent = a[0][1];
      });

      super.registerEvent(this.updateHubService.PluginFailed, (a) => {
        const instance = this.pluginsMap.get(a[0][0]);
        if (instance) {
          instance.error = a[0][1] || this.translationService.translate("UI.PLUGINS.FAILED_UNKNOWN");
        }
      });
      super.registerEvent(this.updateHubService.PluginFinished, async (a) => {
        const instance = this.pluginsMap.get(a[0][0]);
        if (!instance) {
          return;
        }
        instance.downloaded = true;
        instance.downloadMax = 100;
        instance.downloadCurrent = instance.downloadMax;

      });
      super.registerEvent(this.updateHubService.PluginLoaded, async (a) => {
        await this.translationConfigService.init();
      });
    } catch (error) {
      super.handleError(error);
    } finally {
      loadingDone();
    }
  }

  /** the one action that fits the plugin's state: install, update or try again */
  async runAction(data: PluginStateInstance) {
    const state = data.state;
    if (state === "error") {
      data.error = void 0;
      if (data.isInstalled) {
        await this.update(void 0, data);
      } else {
        await this.install(void 0, data);
      }
      return;
    }
    if (state === "update") {
      await this.update(void 0, data);
    } else if (state === "available") {
      await this.install(void 0, data);
    }
  }

  openLogs() {
    this.router.navigate(["/admin/logs"]);
  }

  // shows the progress row right away, the hub fills in the real numbers
  private markStarted(data: PluginStateInstance) {
    data.downloadMax = data.downloadMax || 1;
  }

  async install($event, data: PluginStateInstance) {
    this.markStarted(data);
    try {
      await this.updateHubService.installPlugin(data.instance.cloudPlugin);
    } catch (error) {
      data.error = String(this.parseErrorString(error) ?? "");
    }
    this.designTimeDataService.clearLogicTemplate();
  }

  async update($event, data: PluginStateInstance) {
    this.markStarted(data);
    try {
      await this.updateHubService.updatePlugin(data.instance.cloudPlugin);
    } catch (error) {
      data.error = String(this.parseErrorString(error) ?? "");
    }
    this.designTimeDataService.clearLogicTemplate();
  }

  async updateAll() {
    const items = this.plugins.filter(a => a.cloudIsNewer && a.isInstalled);
    const data = this.preparePluginList(items);
    await this.runPluginBatch(() => this.updateHubService.updateAllPlugins(data));
  }

  async installAll() {
    const items = this.plugins.filter(a => !a.isInstalled);
    const data = this.preparePluginList(items);
    await this.runPluginBatch(() => this.updateHubService.installAllPlugins(data));
  }

  async installUpdateAll() {
    const updateItems = this.plugins.filter(a => a.cloudIsNewer && a.isInstalled);
    const installItems = this.plugins.filter(a => !a.isInstalled);
    const items = [...updateItems, ...installItems];
    const data = this.preparePluginList(items);
    await this.runPluginBatch(() => this.updateHubService.installUpdateAllPlugins(data));
  }

  /**
   * Plugins in use are updated without a restart where possible. The server only restarts if one could not be updated in place:
   * then the call does not return (the connection drops) and the overlay stays until the server is started again.
   */
  private async runPluginBatch(call: () => Promise<any>) {
    this.appService.isStarting = true;
    try {
      await call();
      this.appService.isStarting = false;
    } catch (error) {
      console.log(error);
    }
    this.designTimeDataService.clearLogicTemplate();
  }

  preparePluginList(plugins: PluginStateInstance[]) {
    const data = [];
    for (const x of plugins) {
      data.push(x.instance.cloudPlugin);
    }
    return data;
  }

  itemClick($event) {
    const item: CustomMenuItem = $event.itemData;

    if (item && item.command) {
      item.command($event);
    }
  }

  ngOnDestroy() {
    super.baseOnDestroy();
    this.translationConfigService.init();
  }
}
