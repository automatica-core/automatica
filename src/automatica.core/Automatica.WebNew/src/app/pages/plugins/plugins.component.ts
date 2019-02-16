import { Component, OnInit, OnDestroy } from "@angular/core";
import { UpdateHubService } from "src/app/base/communication/hubs/update-hub.service";
import { PluginType, PluginState, Version, PluginsService } from "src/app/services/plugins.service";
import { TranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { CustomMenuItem } from "src/app/base/model/custom-menu-item";


function versionCompare(v1: Version, v2: Version) {
  if (v1.Major !== v2.Major) {
    if (v1.Major > v2.Major) {
      return 1;
    } else {
    } return -1;
  }

  if (v1.Minor !== v2.Minor) {
    if (v1.Minor > v2.Minor) {
      return 1;
    } else {
      return -1;
    }
  }

  if (v1.Build !== v2.Build) {
    if (v1.Build > v2.Build) {
      return 1;
    } else {
      return -1;
    }
  }

  if (v1.Revision !== v2.Revision) {
    if (v1.Revision > v2.Revision) {
      return 1;
    } else {
      return -1;
    }
  }

  return 0;
}

function versionToString(v: Version) {
  return `${v.Major}.${v.Minor}.${v.Build}.${v.Revision}`;
}

function versionParse(v: string): Version {
  const split = v.split(".");

  if (split.length === 0) {
    return void 0;
  }
  if (split.length === 1) {
    return {
      Major: parseInt(split[0], 10),
      Minor: 0,
      Build: 0,
      Revision: 0
    };
  }
  if (split.length === 2) {
    return {
      Major: parseInt(split[0], 10),
      Minor: parseInt(split[1], 10),
      Build: 0,
      Revision: 0
    };
  }
  if (split.length === 3) {
    return {
      Major: parseInt(split[0], 10),
      Minor: parseInt(split[1], 10),
      Build: parseInt(split[2], 10),
      Revision: 0
    };
  }
  if (split.length === 4) {
    return {
      Major: parseInt(split[0], 10),
      Minor: parseInt(split[1], 10),
      Build: parseInt(split[2], 10),
      Revision: parseInt(split[3], 10)
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
    this.cloudVersion = versionParse(instance.CloudPlugin.Version);
    if (instance.LoadedPlugin) {
      this.corePluginVersion = versionParse(instance.LoadedPlugin.Version);
    }
  }

  get objId() {
    return this.instance.CloudPlugin.PluginGuid;
  }

  get name() {
    return this.instance.CloudPlugin.Name;
  }

  get version() {
    return this.instance.CloudPlugin.Version;
  }

  get type() {
    return this.instance.CloudPlugin.PluginType === PluginType.Driver ? "Driver" : "Logic";
  }

  get isInstalled(): boolean {
    if (this.instance.LoadedPlugin) {
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
  menuRestart: CustomMenuItem = {
    id: "restart",
    label: "Restart",
    icon: "fa-download",
    items: undefined,
    command: (event) => { this.restart(); }
  }

  plugins: PluginStateInstance[] = [];
  pluginsMap = new Map<string, PluginStateInstance>();
  constructor(private pluginsService: PluginsService, private updateHubService: UpdateHubService,
    notify: NotifyService, translationService: TranslationService, private appService: AppService) {
    super(notify, translationService);

    this.menuUpdate.label = translationService.translate("PLUGINS.UPDATE_ALL");
    this.menuRestart.label = translationService.translate("COMMON.RESTART");

    this.menuItems.push(this.menuUpdate);
    this.menuItems.push(this.menuRestart);

    appService.setAppTitle("PLUGINS.NAME");
  }

  async ngOnInit() {

    this.appService.isLoading = true;
    try {
      const plugins: PluginState[] = await this.pluginsService.getPlugins();

      for (const p of plugins) {
        const instance = new PluginStateInstance(p);
        this.plugins.push(instance);
        this.pluginsMap.set(p.CloudPlugin.PluginGuid, instance);
      }

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
    await this.updateHubService.installPlugin(data.instance.CloudPlugin);
  }
  async update($event, data: PluginStateInstance) {
    await this.updateHubService.updatePlugin(data.instance.CloudPlugin);
  }

  async updateAll() {

    alert(this.translate.translate("PLUGINS.RESTART_AFTER_INSTALL"));

    const items = this.plugins.filter(a => a.cloudIsNewer && a.isInstalled);

    const data = [];
    for (const x of items) {
      data.push(x.instance.CloudPlugin);
    }
    await this.updateHubService.updateAllPlugins(data);
  }

  async restart() {
    await this.updateHubService.restart();
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
