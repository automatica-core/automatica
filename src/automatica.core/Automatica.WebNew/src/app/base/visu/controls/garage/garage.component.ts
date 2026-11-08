import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";
import {
  GarageMotion, GarageState, GarageType, GARAGE_STATE_TEXTS, clampPercent, extrapolateGaragePosition, stateOfPosition
} from "./garage-model";

/** how often the moving door is redrawn */
const MOTION_TICK_MS = 250;

/**
 * Tile of the garage / gate logic: the door (garage door, sliding or swing gate) with its position, open / stop / close
 * on the tile and partial open in the details popup. The visu writes the inputs of the logic like a push button.
 */
@Component({
  selector: "visu-garage",
  templateUrl: "./garage.component.html",
  styleUrls: ["./garage.component.scss"],
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: false
})
export class GarageComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

  openInput: RuleInterfaceInstance;
  closeInput: RuleInterfaceInstance;
  stopInput: RuleInterfaceInstance;
  partialInput: RuleInterfaceInstance;

  private positionOutput: RuleInterfaceInstance;
  private stateOutput: RuleInterfaceInstance;
  private lockedOutput: RuleInterfaceInstance;

  gateType: GarageType = GarageType.Garage;
  state: GarageState = GarageState.Closed;
  locked = false;
  partialPosition = 0;

  /** what the door shows now, runs on while the drive moves */
  position = 0;

  private motion: GarageMotion = { position: 0, motion: 0, openSeconds: 0, closeSeconds: 0 };
  private motionSince = Date.now();
  private refreshing = false;

  async ngOnInit() {
    this.baseOnInit();
    await super.mobileRuleInit();

    this.openInput = this.getInterfaceByKey("completeOpen");
    this.closeInput = this.getInterfaceByKey("completeClose");
    this.stopInput = this.getInterfaceByKey("stop");
    this.partialInput = this.getInterfaceByKey("partialOpen");
    this.positionOutput = this.getInterfaceByKey("position");
    this.stateOutput = this.getInterfaceByKey("state");
    this.lockedOutput = this.getInterfaceByKey("locked");

    await this.refresh();
    this.registerInterval(() => this.tick(), MOTION_TICK_MS);
  }

  /** reads the whole state (position, motion, travel times) from the logic */
  private async refresh() {
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
      console.error("could not load the garage values", error);
    } finally {
      this.refreshing = false;
    }
  }

  applyData(data: any) {
    this.gateType = Number(data.gateType ?? GarageType.Garage) as GarageType;
    this.locked = !!data.locked;
    this.partialPosition = clampPercent(data.partialPosition);
    this.motion = {
      position: clampPercent(data.position),
      motion: Number(data.motion ?? 0),
      target: data.target === null || data.target === undefined ? undefined : Number(data.target),
      openSeconds: Number(data.openSeconds ?? 0),
      closeSeconds: Number(data.closeSeconds ?? 0)
    };
    this.motionSince = Date.now();
    this.state = typeof data.state === "number" ? data.state : stateOfPosition(this.motion.position);
    this.position = this.motion.position;
  }

  private tick() {
    if (!this.motion.motion) {
      return;
    }
    this.position = extrapolateGaragePosition(this.motion, Date.now() - this.motionSince);
  }

  onRuleInstanceValueChanged(interfaceId, value) {
    if (this.stateOutput?.ObjId === interfaceId) {
      const state = Number(value);
      if (!isNaN(state)) {
        this.state = state;
        if (state !== GarageState.Opening && state !== GarageState.Closing) {
          this.motion = { ...this.motion, motion: 0 };
        }
      }
      // a start or stop: the motion and target come with the logic's data
      this.refresh();
    } else if (this.positionOutput?.ObjId === interfaceId) {
      this.motion = { ...this.motion, position: clampPercent(value) };
      this.motionSince = Date.now();
      this.position = this.motion.position;
    } else if (this.lockedOutput?.ObjId === interfaceId) {
      this.locked = !!value;
    }
  }

  get moving(): boolean {
    return this.state === GarageState.Opening || this.state === GarageState.Closing;
  }

  get stateText(): string {
    return GARAGE_STATE_TEXTS[this.state] ?? GARAGE_STATE_TEXTS[GarageState.Closed];
  }

  get positionText(): string {
    return `${Math.round(this.position)} %`;
  }

  get garageIcon(): any {
    return this.state === GarageState.Closed ? "warehouse" : "door-open";
  }

  get isGarage(): boolean {
    return this.gateType !== GarageType.SlidingGate && this.gateType !== GarageType.SwingGate;
  }

  get isSliding(): boolean {
    return this.gateType === GarageType.SlidingGate;
  }

  get isSwing(): boolean {
    return this.gateType === GarageType.SwingGate;
  }

  /** garage door: height of the closed part (0..55 in the drawing) */
  get doorHeight(): number {
    return 55 * (1 - this.position / 100);
  }

  /** sliding gate: how far it moved behind the right post */
  get slideOffset(): number {
    return 84 * this.position / 100;
  }

  /** swing gate: how far the wings are turned (1 = closed) */
  get wingScale(): number {
    return Math.max(0.12, 1 - 0.88 * this.position / 100);
  }

  async open() {
    await this.press(this.openInput);
  }

  async close() {
    await this.press(this.closeInput);
  }

  async stop() {
    await this.press(this.stopInput);
  }

  async partialOpen() {
    await this.press(this.partialInput);
  }

  private async press(input: RuleInterfaceInstance) {
    if (!input) {
      return;
    }
    await this.dataHub.setValue(input.ObjId, true);
  }

  ngOnDestroy(): void {
    this.baseOnDestroy();
  }
}
