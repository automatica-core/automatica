import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, inject } from "@angular/core";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";
import { ShadingService } from "src/app/services/shading.service";
import { VisuBlindState } from "src/app/base/model/shading/shading";

/** how often the tile asks for the state of the automatic shading */
const SHADING_REFRESH_MS = 60000;

@Component({
    selector: "visu-shutter",
    templateUrl: "./shutter.component.html",
    styleUrls: ["./shutter.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ShutterComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

  private directionOutput: RuleInterfaceInstance;
  private moveOutput: RuleInterfaceInstance;
  private stopOutput: RuleInterfaceInstance;
  private absolutePositionOutput: RuleInterfaceInstance;

  private directionInput: RuleInterfaceInstance;
  private moveInput: RuleInterfaceInstance;
  private stopInput: RuleInterfaceInstance;
  private absolutePositionInput: RuleInterfaceInstance;
  upInput: RuleInterfaceInstance;
  downInput: RuleInterfaceInstance;
  private isMovingOutput: RuleInterfaceInstance;

  private absolutePosition: number;
  private isMoving: boolean;
  displayValue: string;

  shutterIcon: any;

  private readonly shading = inject(ShadingService);
  /** the automatic shading of this blind, undefined when it is not configured (the switch is hidden) */
  shadingState: VisuBlindState | undefined;
  shadingBusy = false;

  /** 0 = open, 100 = closed */
  public get positionWidth(): number {
    const v = Number(this.value);
    return isNaN(v) ? 0 : Math.max(0, Math.min(100, v));
  }

  async ngOnInit() {
    this.baseOnInit();

    super.mobileRuleInit();
    this.directionOutput = this.getInterfaceByKey("directionOutput");
    this.moveOutput = this.getInterfaceByKey("moveOutput");
    this.stopOutput = this.getInterfaceByKey("stopOutput");
    this.absolutePositionOutput = this.getInterfaceByKey("absolutePercentageOutput");


    this.directionInput = this.getInterfaceByKey("direction");
    this.moveInput = this.getInterfaceByKey("ruleInputMove");
    this.stopInput = this.getInterfaceByKey("stop");
    this.absolutePositionInput = this.getInterfaceByKey("absolutePercentage");
    this.upInput = this.getInterfaceByKey("up");
    this.downInput = this.getInterfaceByKey("down");
    this.isMovingOutput = this.getInterfaceByKey("ruleIsMoving");

    this.isMoving = this.dataHub.getCurrentValue(this.isMovingOutput?.ObjId)?.value;
    this.value = this.dataHub.getCurrentValue(this.absolutePositionInput.ObjId)?.value;
    this.updateIcon();

    const data = (await this.ruleInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId));
    const map = new Map(Object.entries(data));

    map.forEach((value, key) => {
      this.onRuleInstanceValueChanged(key, value);
    });

    await this.loadShading();
    this.registerInterval(() => this.loadShading(), SHADING_REFRESH_MS);
  }

  private async loadShading(maxAgeMs = SHADING_REFRESH_MS / 2) {
    const id = this.ruleInstance?.ObjId;
    if (!id) {
      return;
    }
    const states = await this.shading.visuBlinds(maxAgeMs);
    this.shadingState = states.get(id.toLowerCase());
  }

  /** "auto" = the automatic is in charge, "manual" = paused by hand until the sun changes, "off" = switched off */
  get shadingClass(): string {
    if (!this.shadingState?.automatic) {
      return "off";
    }
    return this.shadingState.mode === "manual" ? "manual" : "auto";
  }

  get shadingHint(): string {
    const state = this.shadingState;
    if (!state) {
      return "";
    }
    const action = this.translate.translate(!state.automatic ? "UI.VISU.SHADING_TURN_ON"
      : state.mode === "manual" ? "UI.VISU.SHADING_RESUME" : "UI.VISU.SHADING_TURN_OFF");
    return state.reason ? `${action} (${state.reason})` : action;
  }

  /** switches the automatic on or off, a manual override is ended (resumed) first */
  async toggleShading() {
    const state = this.shadingState;
    if (!state || this.shadingBusy) {
      return;
    }
    this.shadingBusy = true;
    try {
      if (state.automatic && state.mode === "manual") {
        await this.shading.resumeVisu(state.controlId);
      } else {
        await this.shading.setVisuAutomatic(state.controlId, !state.automatic);
      }
      await this.loadShading(0);
    } catch (error) {
      this.notifyService.notifyError(this.translate.translate("UI.VISU.SHADING_FAILED"));
    } finally {
      this.shadingBusy = false;
    }
  }

  private updateIcon() {
    if (this.value >= 50 && this.value <= 100) {
      this.shutterIcon = ["fad", "blinds"];
    }
    else {
      this.shutterIcon = ["fad", "blinds-raised"];
    }
  }

  onRuleInstanceValueChanged(interfaceId, value) {
    if (this.absolutePositionOutput.ObjId == interfaceId || this.absolutePositionInput.ObjId == interfaceId) {
      if (value >= 100) {
        value = 100;
      }
      this.absolutePosition = value;
      this.value = value;
      this.displayValue = Math.round(value) + " %";

      this.updateIcon();
    }

    else if (this.isMovingOutput.ObjId == interfaceId) {
      this.isMoving = value;
    }
  }

  async moveUp($event) {
    await this.dataHub.setValue(this.upInput.ObjId, true);
  }

  async moveDown($event) {
    await this.dataHub.setValue(this.downInput.ObjId, true);
  }

  ngOnDestroy(): void {
    this.baseOnDestroy();
  }
}
