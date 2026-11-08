import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";
import {
  LightingCircuit, LightingCircuitType, LightingCommands, LightingMood, LightingVisuState, MAX_KELVIN, MIN_KELVIN,
  MOOD_ALL_OFF, MOOD_ALL_ON, MOOD_MANUAL, emptyLightingState, parseLightingState, scaledColor
} from "./lighting-controller.model";

/**
 * Tile of the lighting controller logic (P3.Logic.Lightning): the tile switches the room on and off, the popup picks and
 * saves moods and sets the circuits. Everything goes through the logic's "command" input, the state comes from "visuState".
 */
@Component({
    selector: "visu-lighting-controller",
    templateUrl: "./lighting-controller.component.html",
    styleUrls: ["./lighting-controller.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class LightingControllerComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

  readonly CircuitType = LightingCircuitType;
  readonly MinKelvin = MIN_KELVIN;
  readonly MaxKelvin = MAX_KELVIN;
  readonly AllOn = MOOD_ALL_ON;
  readonly AllOff = MOOD_ALL_OFF;

  commandInput: RuleInterfaceInstance;
  stateOutput: RuleInterfaceInstance;
  moodOutput: RuleInterfaceInstance;

  state: LightingVisuState = emptyLightingState();

  newMoodName = "";
  /** id of the mood whose name is being edited */
  renamingId: number = undefined;
  renameText = "";

  get isOn(): boolean {
    return this.state.mood !== MOOD_ALL_OFF || this.state.circuits.some(c => c.on);
  }

  /** brightest circuit, for the level bar of the tile */
  get level(): number {
    return this.state.circuits.filter(c => c.on).reduce((max, c) => Math.max(max, c.level), 0);
  }

  get userMoods(): LightingMood[] {
    return this.state.moods;
  }

  async ngOnInit() {
    this.baseOnInit();
    super.mobileRuleInit();
    this.initInterfaces();

    try {
      const data = await this.ruleInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId);
      for (const [key, value] of Object.entries(data ?? {})) {
        this.onRuleInstanceValueChanged(key, value);
      }
    } catch (error) {
      console.error("could not load the state of the lighting controller", error);
    }
  }

  /** finds the logic interfaces by key and takes the last known state from the data hub */
  initInterfaces() {
    this.commandInput = this.getInterfaceByKey("command");
    this.stateOutput = this.getInterfaceByKey("visuState");
    this.moodOutput = this.getInterfaceByKey("mood");

    const current = this.stateOutput ? this.dataHub.getCurrentValue(this.stateOutput.ObjId)?.value : undefined;
    if (current !== undefined && current !== null) {
      this.state = parseLightingState(current);
    }
  }

  onRuleInstanceValueChanged(interfaceId, value) {
    if (this.stateOutput && this.stateOutput.ObjId === interfaceId) {
      this.state = parseLightingState(value);
    } else if (this.moodOutput && this.moodOutput.ObjId === interfaceId) {
      const mood = Number(value);
      if (Number.isFinite(mood)) {
        this.state = { ...this.state, mood };
      }
    }
  }

  /** text of a mood number: built-ins are translated, user moods show their name (or the number) */
  moodName(id: number): string {
    switch (id) {
      case MOOD_ALL_ON:
        return this.translate.translate("UI.VISU.LIGHTING_ALL_ON");
      case MOOD_ALL_OFF:
        return this.translate.translate("UI.VISU.LIGHTING_ALL_OFF");
      case MOOD_MANUAL:
        return this.translate.translate("UI.VISU.LIGHTING_MANUAL");
    }
    const mood = this.state.moods.find(m => m.id === id);
    return mood?.name ? mood.name : `${id}`;
  }

  circuitName(circuit: LightingCircuit): string {
    return circuit.name ? circuit.name : this.translate.translate("UI.VISU.LIGHTING_CIRCUIT", { index: circuit.index });
  }

  circuitSwatch(circuit: LightingCircuit): string {
    return scaledColor(circuit.color, Math.max(circuit.level, 35));
  }

  send(command: string) {
    if (!this.commandInput || !command) {
      return;
    }
    this.dataHub.setValue(this.commandInput.ObjId, command);
  }

  /** tapping the tile switches the room on or off */
  toggle() {
    this.send(this.isOn ? LightingCommands.off() : LightingCommands.on());
  }

  selectMood(id: number) {
    this.send(LightingCommands.mood(id));
  }

  switchCircuit(circuit: LightingCircuit, on: boolean) {
    if (circuit.on !== on) {
      this.send(LightingCommands.setSwitch(circuit.index, on));
    }
  }

  levelChanged(circuit: LightingCircuit, level: number) {
    if (level === undefined || level === null || Math.round(level) === Math.round(circuit.level)) {
      return;
    }
    if (circuit.type === LightingCircuitType.TunableWhite) {
      this.send(LightingCommands.setWhite(circuit.index, level, circuit.kelvin));
    } else {
      this.send(LightingCommands.setLevel(circuit.index, level));
    }
  }

  kelvinChanged(circuit: LightingCircuit, kelvin: number) {
    if (kelvin === undefined || kelvin === null || Math.round(kelvin) === Math.round(circuit.kelvin)) {
      return;
    }
    this.send(LightingCommands.setWhite(circuit.index, circuit.on ? circuit.level : 100, kelvin));
  }

  colorChanged(circuit: LightingCircuit, color: string) {
    if (!/^#[0-9a-fA-F]{6}$/.test(color ?? "") || color.toUpperCase() === scaledColor(circuit.color, circuit.level)) {
      return;
    }
    this.send(LightingCommands.setColor(circuit.index, color.toUpperCase()));
  }

  saveNewMood() {
    const name = this.newMoodName.trim();
    if (!name) {
      return;
    }
    this.send(LightingCommands.saveNew(name));
    this.newMoodName = "";
  }

  overwriteMood(mood: LightingMood) {
    this.send(LightingCommands.save(mood.id, mood.name));
  }

  startRename(mood: LightingMood) {
    this.renamingId = mood.id;
    this.renameText = mood.name;
  }

  cancelRename() {
    this.renamingId = undefined;
    this.renameText = "";
  }

  confirmRename(mood: LightingMood) {
    const name = this.renameText.trim();
    if (name && name !== mood.name) {
      this.send(LightingCommands.rename(mood.id, name));
    }
    this.cancelRename();
  }

  deleteMood(mood: LightingMood) {
    this.send(LightingCommands.delete(mood.id));
    if (this.renamingId === mood.id) {
      this.cancelRename();
    }
  }

  ngOnDestroy(): void {
    this.baseOnDestroy();
  }
}
