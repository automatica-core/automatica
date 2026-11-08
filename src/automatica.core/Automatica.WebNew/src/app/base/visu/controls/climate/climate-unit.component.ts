import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";
import { ClimateRuleComponent } from "./climate-rule-component";
import { acModeKey, fanCoilModeKey, flag, num, unitActive, unitKind, UnitKind, unitStateKey } from "./climate.model";
import { formatTemperature, stepTarget } from "../room-controller/room-controller.model";

/**
 * Tile of the AC unit and fan coil unit blocks (climate): current temperature, target with -/+ (input Tt), state, mode and fan.
 * The AC unit gets a power button (pulse on the toggle input Tg), the fan coil a boost button (input Bm).
 */
@Component({
    selector: "visu-climate-unit",
    templateUrl: "./climate-unit.component.html",
    styleUrls: ["./climate.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ClimateUnitComponent extends ClimateRuleComponent implements OnInit, OnDestroy {

  kind: UnitKind = "fan-coil";
  current: number | undefined;
  target: number | undefined;
  stateText = "";
  subText = "";
  active = false;
  busy = false;

  get currentText(): string {
    return formatTemperature(this.current);
  }

  get targetText(): string {
    return formatTemperature(this.target);
  }

  get unitIcon(): any {
    if (this.kind === "fan-coil") {
      if ((num(this.values, "h") ?? 0) > 0) {
        return ["fad", "fire"];
      }
      if ((num(this.values, "cool") ?? 0) > 0) {
        return ["fad", "snowflake"];
      }
      return ["fad", "fan"];
    }
    return ["fad", "air-conditioner"];
  }

  get canAdjust(): boolean {
    return !!this.interfaceOf("tt") && !this.busy;
  }

  get boostOn(): boolean {
    return flag(this.values, "bm");
  }

  async ngOnInit() {
    this.kind = unitKind((this.ruleInstance?.Interfaces ?? []).map(a => a.Template?.Key));
    await this.climateInit();
  }

  protected onValuesChanged(): void {
    this.current = num(this.values, "tcout") ?? num(this.values, "tc");
    this.target = num(this.values, "ttout") ?? num(this.values, "tt");
    this.value = this.current;
    this.active = unitActive(this.kind, this.values);

    const parts: string[] = [];
    const state = unitStateKey(this.kind, this.values);
    this.stateText = state ? this.translate.translate(state) : "";

    const mode = num(this.values, "modeout");
    const modeKey = this.kind === "ac" ? acModeKey(mode) : fanCoilModeKey(mode);
    if (modeKey) {
      parts.push(this.translate.translate(modeKey));
    }
    const fan = num(this.values, "fanout");
    if (fan !== undefined) {
      const fanText = this.translate.translate("UI.VISU.CLIMATE_FAN");
      if (this.kind === "ac") {
        parts.push(fan === 0 ? `${fanText} ${this.translate.translate("UI.VISU.CLIMATE_MODE_AUTO")}` : `${fanText} ${fan}`);
      } else {
        parts.push(`${fanText} ${Math.round(fan)} %`);
      }
    }
    const co2 = this.kind === "fan-coil" ? num(this.values, "co2") : void 0;
    if (co2 !== undefined) {
      parts.push(`CO₂ ${Math.round(co2)} ppm`);
    }
    this.subText = parts.join(" · ");
  }

  async adjust(direction: 1 | -1) {
    if (!this.canAdjust) {
      return;
    }
    const next = stepTarget(this.target, direction);
    this.busy = true;
    try {
      if (await this.write("tt", next)) {
        this.target = next;
      }
    } finally {
      this.busy = false;
    }
  }

  /** AC unit: a pulse on the toggle input switches the unit on or off */
  async togglePower() {
    if (this.busy) {
      return;
    }
    this.busy = true;
    try {
      if (await this.write("tg", true)) {
        await this.write("tg", false);
      }
    } finally {
      this.busy = false;
    }
  }

  /** fan coil: boost (fan 100 %) on / off */
  async toggleBoost() {
    if (this.busy) {
      return;
    }
    this.busy = true;
    try {
      const next = !this.boostOn;
      if (await this.write("bm", next)) {
        this.values = { ...this.values, bm: next };
      }
    } finally {
      this.busy = false;
    }
  }

  ngOnDestroy(): void {
    this.baseOnDestroy();
  }
}
