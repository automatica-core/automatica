import { Component, OnInit, OnDestroy } from "@angular/core";
import { UpdateHubService } from "src/app/base/communication/hubs/update-hub.service";
import { PluginType, PluginState, Version, PluginsService } from "src/app/services/plugins.service";
import { L10nTranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { CustomMenuItem } from "src/app/base/model/custom-menu-item";


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



class PluginStateInstance {
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
  styleUrls: ["./plugins.component.scss"]
})
export class PluginsComponent extends BaseComponent implements OnInit, OnDestroy {
  menuItems: CustomMenuItem[] = [];
  menuUpdate: CustomMenuItem = {
    id: "save",
    label: "Update all",
    icon: "fa-download",
    items: undefined,
    command: (event) => { this.updateAll(); }
  }
  menuInstall: CustomMenuItem = {
    id: "save",
    label: "Install all",
    icon: "fa-download",
    items: undefined,
    command: (event) => { this.installAll(); }
  }
  menuInstallUpdate: CustomMenuItem = {
    id: "restart",
    label: "Install update all",
    icon: "fa-download",
    items: undefined,
    command: (event) => { this.installUpdateAll(); }
  }
  menuReload: CustomMenuItem = {
    id: "reload",
    label: "Reload",
    icon: "fa-download",
    items: undefined,
    command: (event) => { this.load(); }
  }

  plugins: PluginStateInstance[] = [];
  pluginsMap = new Map<string, PluginStateInstance>();
  constructor(
    private pluginsService: PluginsService,
    private updateHubService: UpdateHubService,
    notify: NotifyService,
    translationService: L10nTranslationService,
    appService: AppService) {
    super(notify, translationService, appService);

    this.menuUpdate.label = translationService.translate("PLUGINS.UPDATE_ALL");
    this.menuInstall.label = translationService.translate("PLUGINS.INSTALL_ALL");
    this.menuInstallUpdate.label = translationService.translate("PLUGINS.INSTALL_UPDATE_ALL");
    this.menuReload.label = translationService.translate("PLUGINS.RELOAD");

    this.menuItems.push(this.menuReload);
    this.menuItems.push(this.menuUpdate);
    this.menuItems.push(this.menuInstall);
    this.menuItems.push(this.menuInstallUpdate);

    appService.setAppTitle("PLUGINS.NAME");
  }

  async load() {
    try {
      this.plugins = [];
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
      this.handleError(error);
    }
  }

  async ngOnInit() {

    this.baseOnInit();

    this.appService.isLoading = true;
    try {
      await this.load();

      super.registerEvent(this.updateHubService.PluginDownloadProgressChanged, (a) => {
        const instance = this.pluginsMap.get(a[0][0]);

        instance.downloadMax = a[0][2];
        instance.downloadCurrent = a[0][1];
      });

      super.registerEvent(this.updateHubService.PluginFailed, (a) => {
        const instance = this.pluginsMap.get(a[0][0]);
        instance.error = a[0][1];
      });
      super.registerEvent(this.updateHubService.PluginFinished, (a) => {
        const instance = this.pluginsMap.get(a[0][0]);
        instance.downloaded = true;
        instance.downloadMax = 100;
        instance.downloadCurrent = instance.downloadMax;
      });
    } catch (error) {
      super.handleError(error);
    }

    this.appService.isLoading = false;
  }

  async install($event, data: PluginStateInstance) {
    await this.updateHubService.installPlugin(data.instance.cloudPlugin);
  }
  async update($event, data: PluginStateInstance) {
    await this.updateHubService.updatePlugin(data.instance.cloudPlugin);
  }

  async updateAll() {
    const items = this.plugins.filter(a => a.cloudIsNewer && a.isInstalled);
    const data = this.preparePluginList(items);
    await this.updateHubService.updateAllPlugins(data);

    this.appService.isStartingChanged.emit(true);
  }

  async installAll() {
    const items = this.plugins.filter(a => !a.isInstalled);
    const data = this.preparePluginList(items);
    await this.updateHubService.installAllPlugins(data);

    this.appService.isStartingChanged.emit(true);
  }

  async installUpdateAll() {
    const updateItems = this.plugins.filter(a => a.cloudIsNewer && a.isInstalled);
    const installItems = this.plugins.filter(a => !a.isInstalled);
    const items = [...updateItems, ...installItems];
    const data = this.preparePluginList(items);
    await this.updateHubService.installUpdateAllPlugins(data);

    this.appService.isStartingChanged.emit(true);
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
  }
}
