import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnDestroy, Output } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { BaseComponent } from "src/app/base/base-component";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { AreaInstance } from "src/app/base/model/areas/area-instance";
import { NodeInstance } from "src/app/base/model/node-instance";
import { NodeTemplate } from "src/app/base/model/node-template";
import { AppService } from "src/app/services/app.service";
import { BaseService } from "src/app/services/base-service";
import { ConfigService } from "src/app/services/config.service";
import { NotifyService } from "src/app/services/notify.service";

export type LearnDeviceStatus = "new" | "known" | "adding" | "added" | "paired";

/** prefix of the values a driver reports about a sender, they are never copied to a node */
const LEARN_INFO = "learn-info-";

/** a driver reloads after a node was added, learn mode is enabled again on the new driver instance */
const RELEARN_DELAYS_MS = [4000, 12000];

export interface ExistingDevice {
  ObjId: string;
  DisplayName: string;
  address: string;
}

/**
 * A sender reported by learn mode: what the driver found out (profile, signal, known) and what the user enters.
 */
export class LearnedDevice {
  key: string;
  description: string;
  status: LearnDeviceStatus = "new";
  knownName: string;
  profile: string;
  telegram: string;
  rssi: number;
  replySent = false;
  templates: NodeTemplate[] = [];
  /** the address properties of the driver (e.g. enocean-serialnumber), copied to the node */
  properties: { [key: string]: string } = {};
  uniqueProperty: string;

  name = "";
  area: string;
  template: string;
  mode: "new" | "replace" = "new";
  replace: string;
  error: string;

  get profileDetected(): boolean {
    return !!this.profile;
  }

  /** signal strength in 0..4 bars: -90 dBm and below = 0, -50 dBm and above = 4 */
  get bars(): number {
    if (this.rssi === void 0 || this.rssi === null || isNaN(this.rssi)) {
      return 0;
    }
    return Math.max(0, Math.min(4, Math.round((this.rssi + 90) / 10)));
  }

  get canAdd(): boolean {
    if (this.status !== "new") {
      return false;
    }
    return this.mode === "replace" ? !!this.replace : !!this.template && !!this.name?.trim();
  }
}

/** reads a property instance as the hub sends it (PascalCase json or a model) */
function propertyKey(p: any): string {
  return p?.This2PropertyTemplateNavigation?.Key ?? p?.PropertyTemplate?.Key ?? p?.this2PropertyTemplateNavigation?.key;
}

function propertyValue(p: any): string {
  const value = p?.Value ?? p?.ValueString ?? p?.value ?? p?.valueString;
  return value === void 0 || value === null ? "" : String(value);
}

@Component({
  selector: "p3-learn-mode-dialog",
  templateUrl: "./learn-mode-dialog.component.html",
  styleUrls: ["./learn-mode-dialog.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false
})
export class LearnModeDialogComponent extends BaseComponent implements OnDestroy {

  /** the node learn mode runs on (e.g. EnOcean "Learned") */
  @Input() node: NodeInstance;

  /** all areas, flat */
  @Input() areaInstances: AreaInstance[] = [];

  /** a node was added or took over a device, the tree reloads */
  @Output() added = new EventEmitter<NodeInstance>();

  @Output() closed = new EventEmitter<void>();

  public visible = false;
  public devices: LearnedDevice[] = [];
  public existingDevices: ExistingDevice[] = [];

  private learnModeSub: any;
  private relearnTimers: any[] = [];

  constructor(
    private config: ConfigService,
    private dataHub: DataHubService,
    private changeDetection: ChangeDetectorRef,
    notify: NotifyService,
    translate: L10nTranslationService,
    appService: AppService) {
    super(notify, translate, appService);
  }

  async open(node: NodeInstance) {
    this.node = node;
    this.devices = [];
    this.existingDevices = this.collectExisting(node);
    this.visible = true;
    this.changeDetection.detectChanges();

    this.learnModeSub = super.registerEvent(this.dataHub.notifyLearnMode, (hubData) => this.onLearnNotification(hubData));
    try {
      await this.dataHub.enableLearnMode(node);
    } catch (error) {
      this.handleError(error);
    }
  }

  async onHiding() {
    this.clearRelearn();
    if (this.learnModeSub) {
      super.unregisterEvent(this.learnModeSub);
      this.learnModeSub = void 0;
    }
    if (this.node) {
      try {
        await this.dataHub.disableLearnMode(this.node);
      } catch (error) {
        console.error(error);
      }
    }
    this.visible = false;
    this.closed.emit();
  }

  ngOnDestroy() {
    this.clearRelearn();
    if (this.learnModeSub && this.node) {
      // closed without the popup closing, don't leave learn mode enabled on the server
      this.dataHub.disableLearnMode(this.node).catch((error) => console.error(error));
    }
    this.learnModeSub = void 0;
    super.baseOnDestroy();
  }

  /** hubData: name (key), description, node templates, property instances */
  onLearnNotification(hubData: any[]) {
    if (!hubData || hubData.length < 4) {
      return;
    }

    const key = hubData[0] as string;
    const info: { [key: string]: string } = {};
    const properties: { [key: string]: string } = {};
    for (const p of hubData[3] || []) {
      const k = propertyKey(p);
      if (!k) {
        continue;
      }
      if (k.startsWith(LEARN_INFO)) {
        info[k.substring(LEARN_INFO.length)] = propertyValue(p);
      } else {
        properties[k] = propertyValue(p);
      }
    }

    let device = this.devices.find(d => d.key === key);
    const status = info["status"];

    if (!device) {
      // a data telegram of a device that was not part of this session
      if (status === "receiving") {
        return;
      }
      device = new LearnedDevice();
      device.key = key;
      this.devices = [device, ...this.devices];
    }

    device.description = hubData[1];
    if (info["rssi"]) {
      device.rssi = +info["rssi"];
    }

    if (status === "receiving") {
      if (device.status === "added" || device.status === "adding" || device.status === "known") {
        device.status = "paired";
      }
    } else {
      device.profile = info["profile"] || device.profile;
      device.telegram = info["telegram"] || device.telegram;
      device.replySent = device.replySent || info["paired"] === "true";
      if (Object.keys(properties).length > 0) {
        device.properties = properties;
        device.uniqueProperty = Object.keys(properties)[0];
      }

      const templates = BaseService.getValidBaseModels<NodeTemplate>(hubData[2] || [], this.translate);
      if (templates.length > 0) {
        device.templates = templates;
        if (!device.template || !templates.find(t => t.ObjId === device.template)) {
          device.template = templates[0].ObjId;
        }
      }
      if (!device.name) {
        device.name = this.defaultName(device);
      }

      if (status === "known" && device.status === "new") {
        device.status = "known";
        device.knownName = info["known"];
      }
    }

    this.changeDetection.detectChanges();
  }

  templateChanged(device: LearnedDevice) {
    // the default name follows the profile until the user types one
    const names = device.templates.map(t => t.DisplayName);
    if (!device.name || names.includes(device.name)) {
      device.name = this.defaultName(device);
    }
  }

  async add(device: LearnedDevice) {
    if (!device.canAdd) {
      return;
    }

    device.error = void 0;
    device.status = "adding";
    this.changeDetection.detectChanges();

    try {
      const node = await this.config.addLearnedNode({
        Parent: this.node.ObjId,
        NodeTemplate: device.template,
        Name: device.name?.trim(),
        Area: device.area || void 0,
        Properties: device.properties,
        UniqueProperty: device.uniqueProperty,
        Replace: device.mode === "replace" ? device.replace : void 0
      });

      device.status = "added";
      this.notifyService.notifySuccess(this.translate.translate("LEARN_MODE.ADDED", { name: node?.Name ?? device.name }));
      this.added.emit(node);
      this.scheduleRelearn();
    } catch (error) {
      const conflict = error?.status === 409 ? (error.error ?? {}) : void 0;
      if (conflict) {
        device.status = "known";
        device.knownName = conflict.Name ?? conflict.name;
        device.error = this.translate.translate("LEARN_MODE.DUPLICATE", { name: device.knownName ?? "" });
      } else {
        device.status = "new";
        device.error = this.parseErrorString(error);
      }
    }
    this.changeDetection.detectChanges();
  }

  statusText(device: LearnedDevice): string {
    switch (device.status) {
      case "known":
        return this.translate.translate("LEARN_MODE.STATUS_KNOWN", { name: device.knownName ?? "" });
      case "adding":
        return this.translate.translate("LEARN_MODE.STATUS_ADDING");
      case "added":
        return this.translate.translate("LEARN_MODE.STATUS_ADDED");
      case "paired":
        return this.translate.translate("LEARN_MODE.STATUS_PAIRED");
      default:
        return this.translate.translate("LEARN_MODE.STATUS_NEW");
    }
  }

  trackDevice(_: number, device: LearnedDevice) {
    return device.key;
  }

  private defaultName(device: LearnedDevice): string {
    const template = device.templates.find(t => t.ObjId === device.template);
    return template?.DisplayName ?? device.key;
  }

  /** the devices below the learn node that carry an address property (any depth, old nested layouts as well) */
  private collectExisting(node: NodeInstance): ExistingDevice[] {
    const result: ExistingDevice[] = [];
    const visit = (n: NodeInstance) => {
      for (const child of n?.Children ?? []) {
        const address = (child.Properties ?? []).find(p => p.PropertyTemplate?.Key?.endsWith("serialnumber"));
        if (address) {
          result.push({ ObjId: child.ObjId, DisplayName: `${child.Name} (${address.Value ?? ""})`, address: String(address.Value ?? "") });
        }
        visit(child);
      }
    };
    visit(node);
    return result.sort((a, b) => a.DisplayName.localeCompare(b.DisplayName));
  }

  private scheduleRelearn() {
    this.clearRelearn();
    for (const delay of RELEARN_DELAYS_MS) {
      this.relearnTimers.push(setTimeout(() => {
        if (this.visible && this.node) {
          this.dataHub.enableLearnMode(this.node).catch((error) => console.error(error));
        }
      }, delay));
    }
  }

  private clearRelearn() {
    for (const timer of this.relearnTimers) {
      clearTimeout(timer);
    }
    this.relearnTimers = [];
  }
}
