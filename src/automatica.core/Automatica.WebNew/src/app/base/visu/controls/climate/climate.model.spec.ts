import { acModeKey, centralDetails, centralKind, centralStateKey, centralValue, fanCoilModeKey, flag, unitActive, unitKind, unitStateKey } from "./climate.model";

describe("climate tile model", () => {
  it("tells AC units from fan coils", () => {
    expect(unitKind(["tg", "on", "tt"])).toBe("ac");
    expect(unitKind(["tc", "tt", "bm"])).toBe("fan-coil");
  });

  it("reads flags from logic values", () => {
    expect(flag({ a: true }, "a")).toBeTrue();
    expect(flag({ a: "1" }, "a")).toBeTrue();
    expect(flag({ a: false }, "a")).toBeFalse();
    expect(flag({}, "a")).toBeFalse();
  });

  it("names the unit modes", () => {
    expect(acModeKey(3)).toBe("UI.VISU.CLIMATE_MODE_COOL");
    expect(acModeKey(4)).toBe("UI.VISU.CLIMATE_MODE_DRY");
    expect(fanCoilModeKey(0)).toBe("UI.VISU.CLIMATE_OFF");
    expect(fanCoilModeKey(4)).toBe("UI.VISU.CLIMATE_MODE_FAN");
    expect(acModeKey(undefined)).toBe("");
  });

  it("shows the unit state", () => {
    expect(unitStateKey("ac", { status: true })).toBe("UI.VISU.CLIMATE_ON");
    expect(unitStateKey("ac", { status: false, paused: true })).toBe("UI.VISU.CLIMATE_PAUSED");
    expect(unitStateKey("fan-coil", { h: 100, s: true })).toBe("UI.VISU.CLIMATE_HEATING");
    expect(unitStateKey("fan-coil", { cool: 40, s: true })).toBe("UI.VISU.CLIMATE_COOLING");
    expect(unitStateKey("fan-coil", { s: false })).toBe("UI.VISU.CLIMATE_OFF");
    expect(unitActive("ac", { status: true })).toBeTrue();
    expect(unitActive("fan-coil", { s: false })).toBeFalse();
  });

  it("tells the central blocks apart", () => {
    expect(centralKind(["tt", "v", "o", "c"])).toBe("valve");
    expect(centralKind(["w1", "y", "hcm"])).toBe("hvac");
    expect(centralKind(["h", "c", "hl", "cl"])).toBe("heating-cooling");
    expect(centralKind(["h", "c", "avmode"])).toBe("fan-coil");
    expect(centralKind(["h", "c", "p1"])).toBe("ac");
  });

  it("shows the central state", () => {
    expect(centralStateKey("heating-cooling", { h: true })).toBe("UI.VISU.CLIMATE_HEATING");
    expect(centralStateKey("fan-coil", { h: true, c: true })).toBe("UI.VISU.CLIMATE_HEATING_COOLING");
    expect(centralStateKey("hvac", { hcm: -1 })).toBe("UI.VISU.CLIMATE_COOLING");
    expect(centralStateKey("ac", {})).toBe("UI.VISU.CLIMATE_OFF");
    // the valve's "c" is close, not cooling
    expect(centralStateKey("valve", { c: true })).toBe("UI.VISU.CLIMATE_CLOSING");
    expect(centralStateKey("valve", { o: true, error: true })).toBe("UI.VISU.CLIMATE_ERROR");
  });

  it("lists the central details", () => {
    expect(centralDetails("heating-cooling", { h2: true, fout: true, toa: 4.25 }).map(a => a.key))
      .toEqual(["UI.VISU.CLIMATE_STAGE2", "UI.VISU.CLIMATE_FAN", "UI.VISU.CLIMATE_OUTDOOR_AVERAGE"]);
    expect(centralDetails("ac", { p1: true, p2: false, p3: true })).toEqual([{ key: "UI.VISU.CLIMATE_UNITS_PAUSED", value: 2 }]);
    expect(centralDetails("fan-coil", { avmode: 3 })).toEqual([{ key: "UI.VISU.CLIMATE_AVAILABLE_BOTH" }]);
    expect(centralDetails("hvac", { e: true, hmd: true }).map(a => a.key)).toEqual(["UI.VISU.CLIMATE_EMERGENCY", "UI.VISU.CLIMATE_HUMIDIFIER"]);
  });

  it("shows valve position or source load", () => {
    expect(centralValue("valve", { v: 42 })).toBe(42);
    expect(centralValue("heating-cooling", { h: true, hl: 75.2, cl: 0 })).toBe(75.2);
    expect(centralValue("heating-cooling", { c: true, hl: 0, cl: 30 })).toBe(30);
    expect(centralValue("heating-cooling", { hl: 10 })).toBe(10);
    expect(centralValue("hvac", { hcm: 1 })).toBeUndefined();
  });
});
