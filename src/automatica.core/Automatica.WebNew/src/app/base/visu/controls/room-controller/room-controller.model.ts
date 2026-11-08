/** temperature mode of the room controller logic (output "os"), same numbers as the server */
export enum RoomTemperatureMode {
  Off = -1,
  Eco = 0,
  Comfort = 1,
  BuildingProtection = 2,
  ManualTarget = 3
}

/** step of the +/- buttons in K */
export const ROOM_TARGET_STEP = 0.5;
export const ROOM_TARGET_MIN = 5;
export const ROOM_TARGET_MAX = 35;

/** the logic's "mode" input: 3..5 = fixed setpoint (the buttons change Tt instead of the comfort temperature) */
export function usesFixedSetpoint(mode: number | undefined): boolean {
  return mode !== undefined && mode !== null && mode >= 3;
}

/** next target after a +/- press, on the 0.5 K grid and within sane limits */
export function stepTarget(current: number | undefined, direction: 1 | -1, fallback = 22): number {
  const base = typeof current === "number" && isFinite(current) ? current : fallback;
  const next = Math.round((base + direction * ROOM_TARGET_STEP) / ROOM_TARGET_STEP) * ROOM_TARGET_STEP;
  return Math.min(ROOM_TARGET_MAX, Math.max(ROOM_TARGET_MIN, next));
}

/** translation key of a temperature mode */
export function modeKey(mode: number | undefined): string {
  switch (mode) {
    case RoomTemperatureMode.Off:
      return "UI.VISU.ROOM_MODE_OFF";
    case RoomTemperatureMode.Eco:
      return "UI.VISU.ROOM_MODE_ECO";
    case RoomTemperatureMode.BuildingProtection:
      return "UI.VISU.ROOM_MODE_PROTECTION";
    case RoomTemperatureMode.ManualTarget:
      return "UI.VISU.ROOM_MODE_MANUAL";
    case RoomTemperatureMode.Comfort:
      return "UI.VISU.ROOM_MODE_COMFORT";
    default:
      return "";
  }
}

export function toNumber(value: any): number | undefined {
  if (value === undefined || value === null || value === "") {
    return void 0;
  }
  const n = Number(value);
  return isFinite(n) ? n : void 0;
}

export function formatTemperature(value: number | undefined): string {
  return value === undefined ? "–" : value.toFixed(1);
}
