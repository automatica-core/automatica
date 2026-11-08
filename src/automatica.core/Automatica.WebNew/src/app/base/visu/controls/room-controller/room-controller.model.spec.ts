import { formatTemperature, modeKey, RoomTemperatureMode, stepTarget, toNumber, usesFixedSetpoint } from "./room-controller.model";

describe("room controller tile model", () => {
  it("steps the target on the 0.5 K grid", () => {
    expect(stepTarget(22, 1)).toBe(22.5);
    expect(stepTarget(22, -1)).toBe(21.5);
    expect(stepTarget(21.3, 1)).toBe(22);
    expect(stepTarget(undefined, 1)).toBe(22.5);
  });

  it("keeps the target within the limits", () => {
    expect(stepTarget(35, 1)).toBe(35);
    expect(stepTarget(5, -1)).toBe(5);
  });

  it("knows the fixed setpoint modes", () => {
    expect(usesFixedSetpoint(undefined)).toBeFalse();
    expect(usesFixedSetpoint(0)).toBeFalse();
    expect(usesFixedSetpoint(2)).toBeFalse();
    expect(usesFixedSetpoint(3)).toBeTrue();
    expect(usesFixedSetpoint(5)).toBeTrue();
  });

  it("translates every temperature mode", () => {
    expect(modeKey(RoomTemperatureMode.Off)).toBe("UI.VISU.ROOM_MODE_OFF");
    expect(modeKey(RoomTemperatureMode.Eco)).toBe("UI.VISU.ROOM_MODE_ECO");
    expect(modeKey(RoomTemperatureMode.Comfort)).toBe("UI.VISU.ROOM_MODE_COMFORT");
    expect(modeKey(RoomTemperatureMode.BuildingProtection)).toBe("UI.VISU.ROOM_MODE_PROTECTION");
    expect(modeKey(RoomTemperatureMode.ManualTarget)).toBe("UI.VISU.ROOM_MODE_MANUAL");
    expect(modeKey(undefined)).toBe("");
  });

  it("parses and formats values", () => {
    expect(toNumber("21.5")).toBe(21.5);
    expect(toNumber(null)).toBeUndefined();
    expect(toNumber("x")).toBeUndefined();
    expect(formatTemperature(21)).toBe("21.0");
    expect(formatTemperature(undefined)).toBe("–");
  });
});
