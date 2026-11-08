import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";
import { formatTemperature, modeKey, RoomTemperatureMode, stepTarget, toNumber, usesFixedSetpoint } from "./room-controller.model";

/**
 * Tile of the intelligent room controller logic (climate): current temperature, target, temperature mode and
 * +/- buttons. The buttons change the comfort temperature (input Tcomf), in the fixed setpoint modes the target (Tt).
 */
@Component({
    selector: "visu-room-controller",
    templateUrl: "./room-controller.component.html",
    styleUrls: ["./room-controller.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class RoomControllerComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

  private currentInput: RuleInterfaceInstance;
  private modeInput: RuleInterfaceInstance;
  private targetInput: RuleInterfaceInstance;
  private comfortInput: RuleInterfaceInstance;
  private targetOutput: RuleInterfaceInstance;
  private temperatureModeOutput: RuleInterfaceInstance;
  private heatingCoolingOutput: RuleInterfaceInstance;
  private comfortOutput: RuleInterfaceInstance;
  private heatingOutput: RuleInterfaceInstance;
  private coolingOutput: RuleInterfaceInstance;

  current: number | undefined;
  target: number | undefined;
  comfort: number | undefined;
  temperatureMode: number | undefined;
  heatingCoolingMode = 0;
  heating = 0;
  cooling = 0;
  private mode: number | undefined;

  busy = false;

  get currentText(): string {
    return formatTemperature(this.current);
  }

  get targetText(): string {
    return formatTemperature(this.target);
  }

  get modeText(): string {
    const key = modeKey(this.temperatureMode);
    return key ? this.translate.translate(key) : "";
  }

  get directionText(): string {
    if (this.heating > 0) {
      return this.translate.translate("UI.VISU.ROOM_HEATING");
    }
    if (this.cooling > 0) {
      return this.translate.translate("UI.VISU.ROOM_COOLING");
    }
    return "";
  }

  get subText(): string {
    const direction = this.directionText;
    const target = `${this.translate.translate("UI.VISU.ROOM_TARGET")} ${this.targetText} °C`;
    return direction ? `${target} · ${direction}` : target;
  }

  get active(): boolean {
    return this.heating > 0 || this.cooling > 0;
  }

  get roomIcon(): any {
    if (this.heating > 0) {
      return ["fad", "fire"];
    }
    if (this.cooling > 0) {
      return ["fad", "snowflake"];
    }
    return ["fad", "temperature-half"];
  }

  /** the buttons are disabled while the controller is off or the needed input is missing */
  get canAdjust(): boolean {
    return this.temperatureMode !== RoomTemperatureMode.Off && !!this.adjustInput && !this.busy;
  }

  private get adjustInput(): RuleInterfaceInstance | undefined {
    return usesFixedSetpoint(this.mode) ? this.targetInput : this.comfortInput;
  }

  async ngOnInit() {
    this.baseOnInit();
    super.mobileRuleInit();

    this.currentInput = this.getInterfaceByKey("tc");
    this.modeInput = this.getInterfaceByKey("mode");
    this.targetInput = this.getInterfaceByKey("tt");
    this.comfortInput = this.getInterfaceByKey("tcomf");
    this.targetOutput = this.getInterfaceByKey("target");
    this.temperatureModeOutput = this.getInterfaceByKey("os");
    this.heatingCoolingOutput = this.getInterfaceByKey("hcm");
    this.comfortOutput = this.getInterfaceByKey("tchout");
    this.heatingOutput = this.getInterfaceByKey("h");
    this.coolingOutput = this.getInterfaceByKey("cool");

    for (const interf of [this.currentInput, this.modeInput, this.targetOutput, this.temperatureModeOutput,
      this.heatingCoolingOutput, this.comfortOutput, this.heatingOutput, this.coolingOutput]) {
      const value = interf ? this.dataHub.getCurrentValue(interf.ObjId)?.value : void 0;
      if (interf && value !== undefined) {
        this.onRuleInstanceValueChanged(interf.ObjId, value);
      }
    }

    const data = await this.ruleInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId);
    if (data) {
      for (const [key, value] of Object.entries(data)) {
        this.onRuleInstanceValueChanged(key, value);
      }
    }
  }

  private is(interf: RuleInterfaceInstance | undefined, id: string): boolean {
    return !!interf && interf.ObjId === id;
  }

  onRuleInstanceValueChanged(interfaceId, value) {
    if (this.is(this.currentInput, interfaceId)) {
      this.current = toNumber(value);
      this.value = this.current;
    } else if (this.is(this.modeInput, interfaceId)) {
      this.mode = toNumber(value);
    } else if (this.is(this.targetOutput, interfaceId)) {
      this.target = toNumber(value);
    } else if (this.is(this.temperatureModeOutput, interfaceId)) {
      this.temperatureMode = toNumber(value);
    } else if (this.is(this.heatingCoolingOutput, interfaceId)) {
      this.heatingCoolingMode = toNumber(value) ?? 0;
    } else if (this.is(this.comfortOutput, interfaceId)) {
      this.comfort = toNumber(value);
    } else if (this.is(this.heatingOutput, interfaceId)) {
      this.heating = toNumber(value) ?? 0;
    } else if (this.is(this.coolingOutput, interfaceId)) {
      this.cooling = toNumber(value) ?? 0;
    }
  }

  async adjust(direction: 1 | -1) {
    const input = this.adjustInput;
    if (!input || !this.canAdjust) {
      return;
    }
    const fixed = usesFixedSetpoint(this.mode);
    const base = fixed ? this.target : (this.comfort ?? this.target);
    const next = stepTarget(base, direction);

    this.busy = true;
    try {
      await this.dataHub.setValue(input.ObjId, next);
      // shown at once, the logic confirms with its outputs
      if (fixed) {
        this.target = next;
      } else {
        this.comfort = next;
        if (this.temperatureMode === RoomTemperatureMode.Comfort && this.heatingCoolingMode >= 0) {
          this.target = next;
        }
      }
    } catch (error) {
      this.notifyService.notifyError(this.translate.translate("UI.VISU.ROOM_TARGET_FAILED"));
    } finally {
      this.busy = false;
    }
  }

  ngOnDestroy(): void {
    this.baseOnDestroy();
  }
}
