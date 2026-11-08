import { toNumber } from "../room-controller/room-controller.model";

/** a value of a logic interface, by interface key */
export type ClimateValues = Record<string, any>;

export function num(values: ClimateValues, key: string): number | undefined {
  return toNumber(values[key]);
}

export function flag(values: ClimateValues, key: string): boolean {
  const v = values[key];
  return v === true || v === "true" || v === 1 || v === "1";
}

/** AC unit (has the toggle input "tg") or fan coil unit (plain or fresh air) */
export type UnitKind = "ac" | "fan-coil";

export function unitKind(keys: string[]): UnitKind {
  return keys.includes("tg") ? "ac" : "fan-coil";
}

/** AC unit mode (output "modeout"): 1 auto, 2 heat, 3 cool, 4 dry, 5 fan */
export function acModeKey(mode: number | undefined): string {
  switch (mode) {
    case 1: return "UI.VISU.CLIMATE_MODE_AUTO";
    case 2: return "UI.VISU.CLIMATE_MODE_HEAT";
    case 3: return "UI.VISU.CLIMATE_MODE_COOL";
    case 4: return "UI.VISU.CLIMATE_MODE_DRY";
    case 5: return "UI.VISU.CLIMATE_MODE_FAN";
    default: return "";
  }
}

/** fan coil operating mode (output "modeout"): 0 off, 1 auto, 2 heating, 3 cooling, 4 ventilation */
export function fanCoilModeKey(mode: number | undefined): string {
  switch (mode) {
    case 0: return "UI.VISU.CLIMATE_OFF";
    case 1: return "UI.VISU.CLIMATE_MODE_AUTO";
    case 2: return "UI.VISU.CLIMATE_MODE_HEAT";
    case 3: return "UI.VISU.CLIMATE_MODE_COOL";
    case 4: return "UI.VISU.CLIMATE_MODE_FAN";
    default: return "";
  }
}

/** state text of a climate unit tile */
export function unitStateKey(kind: UnitKind, values: ClimateValues): string {
  if (kind === "ac") {
    if (flag(values, "paused")) {
      return "UI.VISU.CLIMATE_PAUSED";
    }
    return flag(values, "status") ? "UI.VISU.CLIMATE_ON" : "UI.VISU.CLIMATE_OFF";
  }
  if ((num(values, "h") ?? 0) > 0) {
    return "UI.VISU.CLIMATE_HEATING";
  }
  if ((num(values, "cool") ?? 0) > 0) {
    return "UI.VISU.CLIMATE_COOLING";
  }
  return flag(values, "s") ? "UI.VISU.CLIMATE_ON" : "UI.VISU.CLIMATE_OFF";
}

export function unitActive(kind: UnitKind, values: ClimateValues): boolean {
  return kind === "ac" ? flag(values, "status") : flag(values, "s");
}

/** heating/cooling controller, HVAC controller, fan coil central, AC central or mixing valve */
export type CentralKind = "valve" | "hvac" | "heating-cooling" | "fan-coil" | "ac";

export function centralKind(keys: string[]): CentralKind {
  if (keys.includes("v")) {
    return "valve";
  }
  if (keys.includes("w1")) {
    return "hvac";
  }
  if (keys.includes("hl")) {
    return "heating-cooling";
  }
  if (keys.includes("avmode")) {
    return "fan-coil";
  }
  return "ac";
}

export function centralHeating(kind: CentralKind, values: ClimateValues): boolean {
  return kind === "hvac" ? num(values, "hcm") === 1 : kind !== "valve" && flag(values, "h");
}

export function centralCooling(kind: CentralKind, values: ClimateValues): boolean {
  return kind === "hvac" ? num(values, "hcm") === -1 : kind !== "valve" && flag(values, "c");
}

export function centralStateKey(kind: CentralKind, values: ClimateValues): string {
  if (kind === "valve") {
    if (flag(values, "error")) {
      return "UI.VISU.CLIMATE_ERROR";
    }
    if (flag(values, "o")) {
      return "UI.VISU.CLIMATE_OPENING";
    }
    return flag(values, "c") ? "UI.VISU.CLIMATE_CLOSING" : "";
  }
  const heating = centralHeating(kind, values);
  const cooling = centralCooling(kind, values);
  if (heating && cooling) {
    return "UI.VISU.CLIMATE_HEATING_COOLING";
  }
  if (heating) {
    return "UI.VISU.CLIMATE_HEATING";
  }
  return cooling ? "UI.VISU.CLIMATE_COOLING" : "UI.VISU.CLIMATE_OFF";
}

/** detail line items (translation key, optional number) of a central tile */
export function centralDetails(kind: CentralKind, values: ClimateValues): { key: string, value?: number }[] {
  const items: { key: string, value?: number }[] = [];
  switch (kind) {
    case "heating-cooling":
      if (flag(values, "h2") || flag(values, "c2")) {
        items.push({ key: "UI.VISU.CLIMATE_STAGE2" });
      }
      if (flag(values, "ahout")) {
        items.push({ key: "UI.VISU.CLIMATE_ADDITIONAL" });
      }
      if (flag(values, "fout")) {
        items.push({ key: "UI.VISU.CLIMATE_FAN" });
      }
      break;
    case "hvac":
      if (flag(values, "w2") || flag(values, "y2")) {
        items.push({ key: "UI.VISU.CLIMATE_STAGE2" });
      }
      if (flag(values, "e")) {
        items.push({ key: "UI.VISU.CLIMATE_EMERGENCY" });
      }
      if (flag(values, "hmd")) {
        items.push({ key: "UI.VISU.CLIMATE_HUMIDIFIER" });
      }
      if (flag(values, "g")) {
        items.push({ key: "UI.VISU.CLIMATE_FAN" });
      }
      break;
    case "ac": {
      const paused = Object.keys(values).filter(k => /^p\d+$/.test(k) && flag(values, k)).length;
      if (paused > 0) {
        items.push({ key: "UI.VISU.CLIMATE_UNITS_PAUSED", value: paused });
      }
      break;
    }
    case "fan-coil": {
      const available = num(values, "avmode");
      if (available !== undefined) {
        items.push({ key: ["UI.VISU.CLIMATE_AVAILABLE_NONE", "UI.VISU.CLIMATE_AVAILABLE_HEAT", "UI.VISU.CLIMATE_AVAILABLE_COOL",
          "UI.VISU.CLIMATE_AVAILABLE_BOTH"][Math.max(0, Math.min(3, available))] });
      }
      break;
    }
  }
  const outdoor = num(values, "toa");
  if (outdoor !== undefined) {
    items.push({ key: "UI.VISU.CLIMATE_OUTDOOR_AVERAGE", value: outdoor });
  }
  return items;
}

/** the large value of a central tile: valve position or the heating / cooling load, undefined when there is none */
export function centralValue(kind: CentralKind, values: ClimateValues): number | undefined {
  if (kind === "valve") {
    return num(values, "v");
  }
  if (kind === "heating-cooling") {
    // the cooling load while cooling, otherwise the heating load
    return flag(values, "c") ? num(values, "cl") : num(values, "hl");
  }
  return void 0;
}
