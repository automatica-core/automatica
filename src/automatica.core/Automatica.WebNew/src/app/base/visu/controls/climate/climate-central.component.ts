import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";
import { ClimateRuleComponent } from "./climate-rule-component";
import { centralCooling, centralDetails, centralHeating, CentralKind, centralKind, centralStateKey, centralValue, flag } from "./climate.model";

/**
 * Read-only tile of the central climate blocks: heating/cooling controller, HVAC controller, AC central, fan coil central
 * and mixing valve. Shows heating / cooling / off (valve: opening / closing, position), stages, load and the outdoor average.
 */
@Component({
    selector: "visu-climate-central",
    templateUrl: "./climate-central.component.html",
    styleUrls: ["./climate.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ClimateCentralComponent extends ClimateRuleComponent implements OnInit, OnDestroy {

  kind: CentralKind = "heating-cooling";
  stateText = "";
  subText = "";
  shownValue: number | undefined;
  active = false;

  get centralIcon(): any {
    if (this.kind === "valve") {
      return ["fad", "faucet"];
    }
    if (centralHeating(this.kind, this.values)) {
      return ["fad", "fire"];
    }
    if (centralCooling(this.kind, this.values)) {
      return ["fad", "snowflake"];
    }
    return ["fad", "temperature-half"];
  }

  get valueText(): string | undefined {
    return this.shownValue === undefined ? void 0 : String(Math.round(this.shownValue));
  }

  get level(): number | undefined {
    return this.kind === "valve" ? this.shownValue : void 0;
  }

  async ngOnInit() {
    this.kind = centralKind((this.ruleInstance?.Interfaces ?? []).map(a => a.Template?.Key));
    await this.climateInit();
  }

  protected onValuesChanged(): void {
    const state = centralStateKey(this.kind, this.values);
    this.stateText = state ? this.translate.translate(state) : "";
    this.shownValue = centralValue(this.kind, this.values);
    this.active = this.kind === "valve" ? !flag(this.values, "error") && (this.shownValue ?? 0) > 0
      : centralHeating(this.kind, this.values) || centralCooling(this.kind, this.values);
    this.subText = centralDetails(this.kind, this.values)
      .map(item => {
        const text = this.translate.translate(item.key);
        if (item.value === undefined) {
          return text;
        }
        return item.key === "UI.VISU.CLIMATE_OUTDOOR_AVERAGE" ? `${text} ${item.value.toFixed(1)} °C` : `${item.value} ${text}`;
      })
      .join(" · ");
  }

  ngOnDestroy(): void {
    this.baseOnDestroy();
  }
}
