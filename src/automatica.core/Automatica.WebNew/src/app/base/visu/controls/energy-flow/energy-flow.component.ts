import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import {
  buildEnergyFlow, EnergyConsumerConfig, EnergyFlowLayout, EnergyNode, EnergyValues, formatNumber, formatPower,
  HUB, VIEW_HEIGHT, VIEW_WIDTH
} from "./energy-flow-model";

/** output keys of the energy monitor logic that the tile shows */
const VALUE_KEYS = ["grid", "pv", "battery", "soc", "house", "consumer1", "consumer2", "consumer3",
  "autarkyToday", "selfConsumptionToday", "pvToday"] as const;
type ValueKey = typeof VALUE_KEYS[number];

const DEFAULT_PEAK_POWER = 10;

/**
 * Energy flow tile of the energy monitor logic (metering plugin): grid, PV, battery, house and up to three extra
 * consumers around a hub, with dots running in the direction of the flow (faster and thicker with more power).
 * Only shows values; it writes nothing, so everyone who may see the visu page may see it.
 */
@Component({
    selector: "visu-energy-flow",
    templateUrl: "./energy-flow.component.html",
    styleUrls: ["./energy-flow.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class EnergyFlowComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

  readonly viewBox = `0 0 ${VIEW_WIDTH} ${VIEW_HEIGHT}`;
  readonly hub = HUB;

  layout: EnergyFlowLayout = { nodes: [], links: [] };

  private readonly values: { [key in ValueKey]?: number } = {};
  /** interface id -> output key */
  private readonly keysById = new Map<string, ValueKey>();
  private hasBattery = false;
  private consumerPresent = [false, false, false];

  async ngOnInit() {
    this.baseOnInit();
    await this.mobileRuleInit();

    for (const key of VALUE_KEYS) {
      const id = this.getInterfaceByKey(key)?.ObjId;
      if (id) {
        this.keysById.set(id.toLowerCase(), key);
        const current = this.dataHub.getCurrentValue(id);
        if (current) {
          this.setValue(key, current.value);
        }
      }
    }
    this.rebuild();

    try {
      const data = await this.ruleInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId);
      this.applyVisuData(data);
    } catch (error) {
      // the tile still fills from the live values
      console.error("could not load the energy monitor values", error);
    }
  }

  async ngOnDestroy() {
    this.baseOnDestroy();
  }

  /** what GetDataForVisu of the logic returns: output values by interface id, parameters by key, presence flags */
  applyVisuData(data: { [key: string]: any } | null | undefined) {
    if (!data) {
      return;
    }
    for (const [key, value] of Object.entries(data)) {
      const valueKey = this.keysById.get(key.toLowerCase());
      if (valueKey) {
        this.setValue(valueKey, value);
      }
    }
    this.hasBattery = data["hasBattery"] === true;
    this.consumerPresent = [1, 2, 3].map(i => data[`hasConsumer${i}`] === true);
    this.rebuild();
  }

  onRuleInstanceValueChanged(ruleInterfaceId: string, value: any) {
    const key = this.keysById.get(String(ruleInterfaceId).toLowerCase());
    if (!key) {
      return;
    }
    this.setValue(key, value);
    if (key === "battery" || key === "soc") {
      this.hasBattery = true;
    }
    this.rebuild();
  }

  private setValue(key: ValueKey, value: any) {
    const n = value === null || value === undefined || value === "" ? NaN : Number(value);
    if (isFinite(n)) {
      this.values[key] = n;
    } else {
      delete this.values[key];
    }
  }

  private parameter(key: string): any {
    return this.getInterfaceByKey(key)?.Value;
  }

  get peakPower(): number {
    const peak = Number(this.parameter("peakPower"));
    return isFinite(peak) && peak > 0 ? peak : DEFAULT_PEAK_POWER;
  }

  private consumers(): EnergyConsumerConfig[] {
    return [1, 2, 3].map(i => {
      const name = this.parameter(`consumer${i}Name`);
      return { name: typeof name === "string" && name.trim() ? name.trim() : undefined, present: this.consumerPresent[i - 1] };
    });
  }

  private rebuild() {
    const values: EnergyValues = {
      grid: this.values.grid,
      pv: this.values.pv,
      battery: this.values.battery,
      soc: this.values.soc,
      house: this.values.house,
      consumers: [this.values.consumer1, this.values.consumer2, this.values.consumer3],
      autarkyToday: this.values.autarkyToday,
      selfConsumptionToday: this.values.selfConsumptionToday,
      pvToday: this.values.pvToday
    };
    this.layout = buildEnergyFlow(values, {
      peakPower: this.peakPower,
      hasBattery: this.hasBattery || this.values.soc !== undefined,
      consumers: this.consumers()
    });
  }

  /** animations run only outside the visu editor */
  get animate(): boolean {
    return !this.editMode;
  }

  private get locale(): string {
    return this.translate?.getLocale?.()?.language || "en";
  }

  left(node: EnergyNode): number {
    return node.position.x / VIEW_WIDTH * 100;
  }

  top(node: EnergyNode): number {
    return node.position.y / VIEW_HEIGHT * 100;
  }

  power(node: EnergyNode) {
    const value = node.kind === "battery" || node.kind === "grid" || node.kind === "pv" || node.kind === "house"
      ? this.values[node.kind]
      : this.values[`consumer${node.index}` as ValueKey];
    return formatPower(value, this.locale);
  }

  nodeIcon(node: EnergyNode): string[] {
    switch (node.kind) {
      case "pv":
        return ["fad", "solar-panel"];
      case "grid":
        return ["fad", "utility-pole"];
      case "house":
        return ["fad", "house"];
      case "battery": {
        const soc = this.values.soc;
        const level = soc === undefined ? "half" : soc >= 88 ? "full" : soc >= 63 ? "three-quarters" : soc >= 38 ? "half" : soc >= 13 ? "quarter" : "empty";
        return ["fad", `battery-${level}`];
      }
      default:
        return ["fad", "plug"];
    }
  }

  /** node name; grid and battery say which way the power goes */
  label(node: EnergyNode): string {
    const t = (key: string, params?: any) => this.translate.translate(key, params);
    switch (node.kind) {
      case "pv":
        return t("UI.VISU.ENERGY_PV");
      case "house":
        return t("UI.VISU.ENERGY_HOUSE");
      case "grid":
        return node.direction === "in" ? t("UI.VISU.ENERGY_IMPORT")
          : node.direction === "out" ? t("UI.VISU.ENERGY_EXPORT") : t("UI.VISU.ENERGY_GRID");
      case "battery":
        return node.direction === "out" ? t("UI.VISU.ENERGY_CHARGE")
          : node.direction === "in" ? t("UI.VISU.ENERGY_DISCHARGE") : t("UI.VISU.ENERGY_BATTERY");
      default:
        return node.name || t("UI.VISU.ENERGY_CONSUMER", { index: node.index });
    }
  }

  socText(node: EnergyNode): string | undefined {
    if (node.kind !== "battery" || this.values.soc === undefined) {
      return undefined;
    }
    return `${formatNumber(this.values.soc, 0, this.locale)} %`;
  }

  get autarkyText(): string {
    return `${formatNumber(this.values.autarkyToday, 0, this.locale)} %`;
  }

  get selfConsumptionText(): string {
    return `${formatNumber(this.values.selfConsumptionToday, 0, this.locale)} %`;
  }

  get pvTodayText(): string {
    return `${formatNumber(this.values.pvToday, 1, this.locale)} kWh`;
  }

  get hasStats(): boolean {
    return this.values.autarkyToday !== undefined || this.values.selfConsumptionToday !== undefined || this.values.pvToday !== undefined;
  }

  /** one sentence for screen readers: all nodes with value and direction */
  get summary(): string {
    return this.layout.nodes.map(n => {
      const p = this.power(n);
      return `${this.label(n)} ${p.value} ${p.unit}`.trim();
    }).join(", ");
  }

  trackById(_: number, item: { id: string }) {
    return item.id;
  }
}
