import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";
import {
  IRRIGATION_HISTORY_TEXTS, IrrigationHistoryItem, IrrigationZone, formatDuration, formatHistoryTime, irrigationHistory,
  irrigationZones, weekdayNames, zoneProgress
} from "./irrigation-model";

/** the countdown of the running zone */
const COUNTDOWN_TICK_MS = 1000;

/**
 * Tile of the irrigation logic: the running zone with the time left, start / stop on the tile; the popup lists the zones
 * (each can be watered on its own), the rain values that skip a run, the schedule and the last runs.
 */
@Component({
  selector: "visu-irrigation",
  templateUrl: "./irrigation.component.html",
  styleUrls: ["./irrigation.component.scss"],
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: false
})
export class IrrigationComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

  readonly historyTexts = IRRIGATION_HISTORY_TEXTS;

  startInput: RuleInterfaceInstance;
  stopInput: RuleInterfaceInstance;
  selectInput: RuleInterfaceInstance;

  private runningOutput: RuleInterfaceInstance;
  private activeOutput: RuleInterfaceInstance;
  private remainingOutput: RuleInterfaceInstance;
  private skippedOutput: RuleInterfaceInstance;

  running = false;
  activeZone = 0;
  skipped = false;
  locked = false;
  factor = 1;
  raining = false;
  rainSeconds = 0;
  maxRainSeconds = 0;
  rainForecast: number = undefined;
  maxRainForecast = 0;
  scheduleEnabled = false;
  scheduleTime: string;
  scheduleDays: string[] = [];
  zones: IrrigationZone[] = [];
  history: IrrigationHistoryItem[] = [];

  /** seconds left of the running zone, counted down here between the values of the logic */
  remaining = 0;
  private remainingAt = Date.now();
  private remainingFrom = 0;
  private names: any[] = [];
  private durations: any[] = [];
  private refreshing = false;

  async ngOnInit() {
    this.baseOnInit();
    await super.mobileRuleInit();

    this.startInput = this.getInterfaceByKey("start");
    this.stopInput = this.getInterfaceByKey("stop");
    this.selectInput = this.getInterfaceByKey("select");
    this.runningOutput = this.getInterfaceByKey("running");
    this.activeOutput = this.getInterfaceByKey("activeValve");
    this.remainingOutput = this.getInterfaceByKey("remaining");
    this.skippedOutput = this.getInterfaceByKey("skipped");

    await this.refresh();
    this.registerInterval(() => this.countDown(), COUNTDOWN_TICK_MS);
  }

  /** reads names, durations, rain, schedule and history from the logic */
  async refresh() {
    if (this.refreshing || !this.ruleInstance?.ObjId) {
      return;
    }
    this.refreshing = true;
    try {
      const data = await this.ruleInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId);
      if (data) {
        this.applyData(data);
      }
    } catch (error) {
      console.error("could not load the irrigation values", error);
    } finally {
      this.refreshing = false;
    }
  }

  applyData(data: any) {
    this.running = !!data.running;
    this.activeZone = Number(data.activeValve ?? 0) || 0;
    this.skipped = !!data.skipped;
    this.locked = !!data.locked;
    this.factor = Number(data.factor ?? 1) || 1;
    this.raining = !!data.raining;
    this.rainSeconds = Number(data.rainSeconds ?? 0) || 0;
    this.maxRainSeconds = Number(data.maxRainSeconds ?? 0) || 0;
    this.rainForecast = typeof data.rainForecast === "number" ? data.rainForecast : undefined;
    this.maxRainForecast = Number(data.maxRainForecast ?? 0) || 0;
    this.scheduleEnabled = !!data.scheduleEnabled;
    this.scheduleTime = data.scheduleTime ?? undefined;
    this.scheduleDays = weekdayNames(data.scheduleDays);
    this.names = data.zoneNames ?? [];
    this.durations = data.durations ?? [];
    this.zones = irrigationZones(this.names, this.durations, this.activeZone);
    this.history = irrigationHistory(data.history).slice(0, 10);
    this.setRemaining(Number(data.remaining ?? 0) || 0);
  }

  private setRemaining(seconds: number) {
    this.remainingFrom = seconds;
    this.remainingAt = Date.now();
    this.remaining = seconds;
  }

  private countDown() {
    if (!this.running || this.remainingFrom <= 0) {
      return;
    }
    this.remaining = Math.max(0, this.remainingFrom - (Date.now() - this.remainingAt) / 1000);
  }

  onRuleInstanceValueChanged(interfaceId, value) {
    if (this.remainingOutput?.ObjId === interfaceId) {
      this.setRemaining(Number(value) || 0);
    } else if (this.runningOutput?.ObjId === interfaceId || this.activeOutput?.ObjId === interfaceId
      || this.skippedOutput?.ObjId === interfaceId) {
      if (this.activeOutput?.ObjId === interfaceId) {
        this.activeZone = Number(value) || 0;
      } else if (this.runningOutput?.ObjId === interfaceId) {
        this.running = !!value;
      } else {
        this.skipped = !!value;
      }
      // a zone change: the history and the factor come with the logic's data
      this.refresh();
    }
  }

  get activeName(): string {
    return this.zones.find(a => a.zone === this.activeZone)?.name ?? `${this.activeZone}`;
  }

  get progress(): number {
    const zone = this.zones.find(a => a.zone === this.activeZone);
    return zone ? zoneProgress(this.remaining, zone.duration, this.factor) : 0;
  }

  get remainingText(): string {
    return formatDuration(this.remaining);
  }

  duration(seconds: number): string {
    return formatDuration(seconds);
  }

  /** the name of a zone (1..8), also of one without a watering time */
  zoneName(zone: number): string {
    const name = (this.names?.[zone - 1] ?? "").toString().trim();
    return name || `${zone}`;
  }

  /** date and time in the language of the app (its Angular locale is en-US) */
  time(at: Date): string {
    return formatHistoryTime(at, this.translate.getLocale()?.language);
  }

  async start() {
    await this.write(this.startInput, true);
  }

  async stop() {
    await this.write(this.stopInput, true);
  }

  /** waters one zone (select 1..8), or stops it when it runs */
  async runZone(zone: number) {
    await this.write(this.selectInput, this.running && this.activeZone === zone ? 0 : zone);
  }

  private async write(input: RuleInterfaceInstance, value: any) {
    if (!input || this.locked) {
      return;
    }
    await this.dataHub.setValue(input.ObjId, value);
  }

  ngOnDestroy(): void {
    this.baseOnDestroy();
  }
}
