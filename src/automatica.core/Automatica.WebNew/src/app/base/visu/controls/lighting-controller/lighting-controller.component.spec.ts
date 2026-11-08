import { LightingControllerComponent } from "./lighting-controller.component";
import {
  LightingCircuitType, LightingCommands, MOOD_ALL_OFF, MOOD_ALL_ON, MOOD_MANUAL, cleanName, parseLightingState, scaledColor
} from "./lighting-controller.model";
import { createNotifySpy, createTranslateSpy } from "src/app/testing/test-helpers";

const STATE = JSON.stringify({
  mood: 2,
  moods: [{ id: 1, name: "Reading" }, { id: 2, name: "Cosy" }],
  circuits: [
    { index: 1, name: "Ceiling", type: 2, level: 60, color: "#FFFFFF", kelvin: 4000, on: true },
    { index: 2, name: "", type: 1, level: 0, color: "#FFFFFF", kelvin: 4000, on: false },
    { index: 3, name: "Strip", type: 3, level: 50, color: "#FF8800", kelvin: 4000, on: true },
    { index: 4, name: "Desk", type: 4, level: 80, color: "#FFFFFF", kelvin: 3000, on: true }
  ],
  motion: false,
  simulation: true
});

describe("lighting controller model", () => {
  it("parses the visuState output", () => {
    const state = parseLightingState(STATE);
    expect(state.mood).toBe(2);
    expect(state.moods.map(m => m.name)).toEqual(["Reading", "Cosy"]);
    expect(state.circuits.length).toBe(4);
    expect(state.circuits[3]).toEqual({ index: 4, name: "Desk", type: LightingCircuitType.TunableWhite, level: 80, color: "#FFFFFF", kelvin: 3000, on: true });
    expect(state.simulation).toBeTrue();
  });

  it("returns the empty state for broken values and drops broken entries", () => {
    expect(parseLightingState("{nope").circuits).toEqual([]);
    expect(parseLightingState(null).mood).toBe(MOOD_ALL_OFF);
    expect(parseLightingState(42).moods).toEqual([]);

    const state = parseLightingState({
      mood: "x",
      moods: [null, { id: "a" }, { id: 3 }],
      circuits: [{ index: 1, type: 0 }, { index: 2, type: 9 }, { index: 3, type: 2, level: 250, color: "red", kelvin: 99999 }]
    });
    expect(state.mood).toBe(MOOD_ALL_OFF);
    expect(state.moods).toEqual([{ id: 3, name: "" }]);
    expect(state.circuits.length).toBe(1);
    expect(state.circuits[0].level).toBe(100);
    expect(state.circuits[0].color).toBe("#FFFFFF");
    expect(state.circuits[0].kelvin).toBe(6500);
    expect(state.circuits[0].on).toBeTrue();
  });

  it("scales a color with the brightness", () => {
    expect(scaledColor("#FF8800", 50)).toBe("#804400");
    expect(scaledColor("#FF8800", 0)).toBe("#000000");
    expect(scaledColor("broken", 100)).toBe("#FFFFFF");
  });

  it("builds the commands of the logic", () => {
    expect(LightingCommands.mood(3)).toBe("mood:3");
    expect(LightingCommands.setLevel(1, 59.6)).toBe("set:1:60");
    expect(LightingCommands.setSwitch(2, true)).toBe("set:2:on");
    expect(LightingCommands.setColor(3, "#00FF00")).toBe("set:3:#00FF00");
    expect(LightingCommands.setWhite(4, 80, 3200)).toBe("set:4:80@3200");
    expect(LightingCommands.saveNew("TV: evening")).toBe("save:new:TV  evening");
    expect(LightingCommands.rename(2, " Night ")).toBe("rename:2:Night");
    expect(LightingCommands.delete(2)).toBe("delete:2");
    expect(cleanName("a\nb")).toBe("a b");
  });
});

describe("LightingControllerComponent", () => {
  let component: LightingControllerComponent;
  let dataHub: { setValue: jasmine.Spy; getCurrentValue: jasmine.Spy };
  const rule = {
    ObjId: "rule",
    Interfaces: [
      { ObjId: "cmd", Template: { Key: "command" } },
      { ObjId: "state", Template: { Key: "visuState" } },
      { ObjId: "mood", Template: { Key: "mood" } }
    ]
  };

  function create(current?: any) {
    dataHub = {
      setValue: jasmine.createSpy("setValue"),
      getCurrentValue: jasmine.createSpy("getCurrentValue").and.callFake((id: string) => id === "state" && current !== undefined ? { value: current } : undefined)
    };
    const translate = createTranslateSpy({ "UI.VISU.LIGHTING_ALL_ON": "All on", "UI.VISU.LIGHTING_ALL_OFF": "All off", "UI.VISU.LIGHTING_MANUAL": "Manual" });
    translate.translate.and.callFake(((key: string, params?: any) =>
      key === "UI.VISU.LIGHTING_CIRCUIT" ? `Circuit ${params.index}` : ({ "UI.VISU.LIGHTING_ALL_ON": "All on", "UI.VISU.LIGHTING_ALL_OFF": "All off", "UI.VISU.LIGHTING_MANUAL": "Manual" } as any)[key] ?? key) as any);

    component = new LightingControllerComponent(dataHub as any, createNotifySpy(), translate, {} as any, {} as any, {} as any);
    component.item = { objectType: rule } as any;
    component.initInterfaces();
  }

  beforeEach(() => create());

  it("takes the last known state from the data hub", () => {
    create(STATE);
    expect(component.state.mood).toBe(2);
    expect(component.moodName(component.state.mood)).toBe("Cosy");
  });

  it("follows the visuState and mood outputs", () => {
    component.onRuleInstanceValueChanged("state", STATE);
    expect(component.state.circuits.length).toBe(4);
    expect(component.isOn).toBeTrue();
    expect(component.level).toBe(80);

    component.onRuleInstanceValueChanged("mood", 0);
    expect(component.state.mood).toBe(MOOD_ALL_OFF);

    component.onRuleInstanceValueChanged("other", "ignored");
    expect(component.state.mood).toBe(MOOD_ALL_OFF);
  });

  it("names built-in moods, user moods and circuits", () => {
    component.onRuleInstanceValueChanged("state", STATE);
    expect(component.moodName(MOOD_ALL_ON)).toBe("All on");
    expect(component.moodName(MOOD_ALL_OFF)).toBe("All off");
    expect(component.moodName(MOOD_MANUAL)).toBe("Manual");
    expect(component.moodName(1)).toBe("Reading");
    expect(component.moodName(42)).toBe("42");
    expect(component.circuitName(component.state.circuits[1])).toBe("Circuit 2");
    expect(component.circuitName(component.state.circuits[0])).toBe("Ceiling");
  });

  it("toggles the room from the tile", () => {
    component.toggle();
    expect(dataHub.setValue).toHaveBeenCalledWith("cmd", "on");

    component.onRuleInstanceValueChanged("state", STATE);
    component.toggle();
    expect(dataHub.setValue).toHaveBeenCalledWith("cmd", "off");
  });

  it("sends mood and circuit commands only for real changes", () => {
    component.onRuleInstanceValueChanged("state", STATE);
    const [ceiling, plain, strip, desk] = component.state.circuits;

    component.selectMood(1);
    expect(dataHub.setValue).toHaveBeenCalledWith("cmd", "mood:1");

    dataHub.setValue.calls.reset();
    component.levelChanged(ceiling, 60);      // the slider echoes the state it got
    component.switchCircuit(ceiling, true);  // same for the switch
    component.colorChanged(strip, "#804400"); // the shown (scaled) color
    component.kelvinChanged(desk, 3000);
    expect(dataHub.setValue).not.toHaveBeenCalled();

    component.levelChanged(ceiling, 30);
    component.switchCircuit(plain, true);
    component.colorChanged(strip, "#00ff00");
    component.levelChanged(desk, 40);
    component.kelvinChanged(desk, 5000);
    expect(dataHub.setValue.calls.allArgs()).toEqual([
      ["cmd", "set:1:30"], ["cmd", "set:2:on"], ["cmd", "set:3:#00FF00"], ["cmd", "set:4:40@3000"], ["cmd", "set:4:80@5000"]
    ]);
  });

  it("saves, overwrites, renames and deletes moods", () => {
    component.onRuleInstanceValueChanged("state", STATE);
    const reading = component.state.moods[0];

    component.newMoodName = "   ";
    component.saveNewMood();
    expect(dataHub.setValue).not.toHaveBeenCalled();

    component.newMoodName = " Dinner ";
    component.saveNewMood();
    expect(dataHub.setValue).toHaveBeenCalledWith("cmd", "save:new:Dinner");
    expect(component.newMoodName).toBe("");

    component.overwriteMood(reading);
    expect(dataHub.setValue).toHaveBeenCalledWith("cmd", "save:1:Reading");

    component.startRename(reading);
    expect(component.renamingId).toBe(1);
    component.renameText = "Reading";
    component.confirmRename(reading);   // unchanged: nothing sent
    expect(dataHub.setValue).not.toHaveBeenCalledWith("cmd", "rename:1:Reading");
    component.startRename(reading);
    component.renameText = "Books";
    component.confirmRename(reading);
    expect(dataHub.setValue).toHaveBeenCalledWith("cmd", "rename:1:Books");
    expect(component.renamingId).toBeUndefined();

    component.deleteMood(reading);
    expect(dataHub.setValue).toHaveBeenCalledWith("cmd", "delete:1");
  });

  it("sends nothing when the logic has no command input", () => {
    rule.Interfaces = rule.Interfaces.filter(a => a.Template.Key !== "command");
    try {
      create();
      component.toggle();
      component.selectMood(1);
      expect(dataHub.setValue).not.toHaveBeenCalled();
    } finally {
      rule.Interfaces.unshift({ ObjId: "cmd", Template: { Key: "command" } });
    }
  });
});
