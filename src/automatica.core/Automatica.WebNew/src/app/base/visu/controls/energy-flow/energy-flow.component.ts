import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, ViewChild } from "@angular/core";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { BaseControlComponent } from "../base-control/base-control.component";
import {
  buildBreakdown, buildEnergyFlow, CONSUMER_COUNT, EnergyBreakdownGroup, EnergyBreakdownRow, EnergyConsumerConfig, EnergyFlowLayout,
  EnergyNode, EnergyValues, formatNumber, formatPower, HOUSE_GROUP, HUB, isRowVisible, VIEW_HEIGHT, VIEW_WIDTH, withPlainGroups
} from "./energy-flow-model";

/** consumer numbers 1..6 */
const CONSUMER_NUMBERS = Array.from({ length: CONSUMER_COUNT }, (_, i) => i + 1);

/** how often the open details popup reloads the circuits (they come from GetDataForVisu, not as outputs) */
export const DETAILS_REFRESH_MS = 5000;

/** output keys of the energy monitor logic that the tile shows */
const VALUE_KEYS = ["grid", "pv", "battery", "soc", "house",
  "consumer1", "consumer2", "consumer3", "consumer4", "consumer5", "consumer6",
  "autarkyToday", "selfConsumptionToday", "pvToday"] as const;
type ValueKey = typeof VALUE_KEYS[number];

const DEFAULT_PEAK_POWER = 10;

/**
 * Energy flow tile of the energy monitor logic (metering plugin): grid, PV, battery, house and up to six extra
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

  readonly hub = HUB;

  layout: EnergyFlowLayout = { nodes: [], links: [], height: VIEW_HEIGHT };

  private readonly values: { [key in ValueKey]?: number } = {};
  /** interface id -> output key */
  private readonly keysById = new Map<string, ValueKey>();
  private hasBattery = false;
  private consumerPresent = CONSUMER_NUMBERS.map(() => false);

  @ViewChild(BaseControlComponent) baseControl: BaseControlComponent;

  /** measured circuits of the house and the consumers */
  circuitGroups: EnergyBreakdownGroup[] = [];
  /** what the details popup lists: the groups with circuits and every other meter with its power */
  detailsGroups: EnergyBreakdownGroup[] = [];
  private detailsTimer: any;
  /**
   * circuits whose own circuits are shown; all are closed at first. Kept by circuit number, so the refresh of the open
   * popup and closing and opening it again keep what the user opened.
   */
  readonly openedCircuits = new Set<number>();

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

    await this.loadVisuData();
  }

  async ngOnDestroy() {
    this.stopDetailsRefresh();
    this.baseOnDestroy();
  }

  private async loadVisuData() {
    try {
      const data = await this.ruleInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId);
      this.applyVisuData(data);
    } catch (error) {
      // the tile still fills from the live values
      console.error("could not load the energy monitor values", error);
    }
  }

  /** the logic has measured circuits: the tile opens a details popup with them */
  get hasDetails(): boolean {
    return this.circuitGroups.length > 0;
  }

  /** whether the tile offers the details: only with circuits and not in the visu editor */
  get canOpenDetails(): boolean {
    return this.hasDetails && !this.editMode;
  }

  openDetails($event?: Event) {
    if (!this.canOpenDetails) {
      return;
    }
    $event?.stopPropagation();
    this.baseControl?.openPopup();
  }

  /** the popup loads the circuits when it opens and again every few seconds while it is open */
  onDetailsShowing() {
    this.stopDetailsRefresh();
    this.loadVisuData();
    this.detailsTimer = setInterval(() => this.loadVisuData(), DETAILS_REFRESH_MS);
  }

  onDetailsHiding() {
    this.stopDetailsRefresh();
  }

  isRowVisible(row: EnergyBreakdownRow): boolean {
    return isRowVisible(row, this.openedCircuits);
  }

  isOpened(row: EnergyBreakdownRow): boolean {
    return this.openedCircuits.has(row.circuit);
  }

  /** opens or closes the circuits below a circuit */
  toggleCircuit(row: EnergyBreakdownRow, $event?: Event) {
    $event?.stopPropagation();
    if (!row.hasChildren || row.circuit === undefined) {
      return;
    }
    if (this.openedCircuits.has(row.circuit)) {
      this.openedCircuits.delete(row.circuit);
    } else {
      this.openedCircuits.add(row.circuit);
    }
  }

  private stopDetailsRefresh() {
    if (this.detailsTimer) {
      clearInterval(this.detailsTimer);
      this.detailsTimer = undefined;
    }
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
    this.consumerPresent = CONSUMER_NUMBERS.map(i => data[`hasConsumer${i}`] === true);
    this.circuitGroups = buildBreakdown(data["circuitGroups"]);
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
    return CONSUMER_NUMBERS.map(i => {
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
      consumers: CONSUMER_NUMBERS.map(i => this.values[`consumer${i}` as ValueKey]),
      autarkyToday: this.values.autarkyToday,
      selfConsumptionToday: this.values.selfConsumptionToday,
      pvToday: this.values.pvToday
    };
    const consumers = this.consumers();
    this.layout = buildEnergyFlow(values, {
      peakPower: this.peakPower,
      hasBattery: this.hasBattery || this.values.soc !== undefined,
      consumers
    });
    // the popup is there for the circuits; once it is, it lists the other meters as well
    this.detailsGroups = this.circuitGroups.length === 0 ? [] : withPlainGroups(this.circuitGroups, values.house,
      consumers.map((c, i) => ({ present: c.present, power: values.consumers[i] })));
  }

  /** animations run only outside the visu editor */
  get animate(): boolean {
    return !this.editMode;
  }

  private get locale(): string {
    return this.translate?.getLocale?.()?.language || "en";
  }

  /** grows with the bottom consumer row; the stage keeps the same ratio so nodes and lines stay on top of each other */
  get viewBox(): string {
    return `0 0 ${VIEW_WIDTH} ${this.layout.height}`;
  }

  get aspectRatio(): string {
    return `${VIEW_WIDTH} / ${this.layout.height}`;
  }

  left(node: EnergyNode): number {
    return node.position.x / VIEW_WIDTH * 100;
  }

  top(node: EnergyNode): number {
    return node.position.y / this.layout.height * 100;
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

  /** "House", or the consumer's name as on the tile */
  groupLabel(group: EnergyBreakdownGroup): string {
    if (group.group === HOUSE_GROUP) {
      return this.translate.translate("UI.VISU.ENERGY_HOUSE");
    }
    return this.consumers()[group.group - 1]?.name || this.translate.translate("UI.VISU.ENERGY_CONSUMER", { index: group.group });
  }

  groupIcon(group: EnergyBreakdownGroup): string[] {
    return group.group === HOUSE_GROUP ? ["fad", "house"] : ["fad", "plug"];
  }

  rowLabel(row: EnergyBreakdownRow): string {
    if (row.kind === "rest") {
      return this.translate.translate("UI.VISU.ENERGY_NOT_MEASURED");
    }
    return row.name || this.translate.translate("UI.VISU.ENERGY_CIRCUIT", { index: row.circuit });
  }

  powerText(kw: number | undefined): string {
    const p = formatPower(kw, this.locale);
    return `${p.value} ${p.unit}`.trim();
  }

  todayText(kwh: number | undefined): string {
    return `${formatNumber(kwh, 2, this.locale)} kWh`;
  }

  /** "85 % measured" under the group name */
  coverageText(group: EnergyBreakdownGroup): string | undefined {
    if (group.coverage === undefined) {
      return undefined;
    }
    return this.translate.translate("UI.VISU.ENERGY_MEASURED", { percent: formatNumber(group.coverage, 0, this.locale) });
  }

  trackById(_: number, item: { id: string }) {
    return item.id;
  }
}
