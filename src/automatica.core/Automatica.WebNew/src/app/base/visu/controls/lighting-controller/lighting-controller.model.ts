/** circuit types of the lighting controller logic (P3.Logic.Lightning CircuitType) */
export enum LightingCircuitType {
  Unused = 0,
  Switch = 1,
  Dimmer = 2,
  Rgb = 3,
  TunableWhite = 4
}

/** built-in mood numbers (Loxone numbering) */
export const MOOD_ALL_OFF = 0;
export const MOOD_ALL_ON = 99;
export const MOOD_MANUAL = -1;

export const MIN_KELVIN = 2700;
export const MAX_KELVIN = 6500;

export interface LightingMood {
  id: number;
  name: string;
}

export interface LightingCircuit {
  index: number;
  name: string;
  type: LightingCircuitType;
  level: number;
  color: string;
  kelvin: number;
  on: boolean;
}

export interface LightingVisuState {
  mood: number;
  moods: LightingMood[];
  circuits: LightingCircuit[];
  motion: boolean;
  simulation: boolean;
}

export function emptyLightingState(): LightingVisuState {
  return { mood: MOOD_ALL_OFF, moods: [], circuits: [], motion: false, simulation: false };
}

function toNumber(value: any, fallback: number): number {
  const n = typeof value === "number" ? value : Number(value);
  return Number.isFinite(n) ? n : fallback;
}

function toColor(value: any): string {
  return typeof value === "string" && /^#[0-9a-fA-F]{6}$/.test(value) ? value.toUpperCase() : "#FFFFFF";
}

/**
 * Parses the visuState output of the logic; anything broken becomes the empty state, broken entries are dropped
 */
export function parseLightingState(raw: any): LightingVisuState {
  let data = raw;
  if (typeof raw === "string") {
    try {
      data = JSON.parse(raw);
    } catch {
      return emptyLightingState();
    }
  }
  if (!data || typeof data !== "object") {
    return emptyLightingState();
  }

  const moods: LightingMood[] = (Array.isArray(data.moods) ? data.moods : [])
    .filter((m: any) => m && Number.isFinite(Number(m.id)))
    .map((m: any) => ({ id: Number(m.id), name: typeof m.name === "string" ? m.name : "" }));

  const circuits: LightingCircuit[] = (Array.isArray(data.circuits) ? data.circuits : [])
    .filter((c: any) => c && Number.isFinite(Number(c.index)) && Number(c.type) > 0 && Number(c.type) <= 4)
    .map((c: any) => {
      const level = Math.max(0, Math.min(100, toNumber(c.level, 0)));
      return {
        index: Number(c.index),
        name: typeof c.name === "string" ? c.name : "",
        type: Number(c.type) as LightingCircuitType,
        level,
        color: toColor(c.color),
        kelvin: Math.max(MIN_KELVIN, Math.min(MAX_KELVIN, toNumber(c.kelvin, 4000))),
        on: typeof c.on === "boolean" ? c.on : level > 0
      };
    });

  return {
    mood: toNumber(data.mood, MOOD_ALL_OFF),
    moods,
    circuits,
    motion: data.motion === true,
    simulation: data.simulation === true
  };
}

/** the color with the brightness applied, like the RGB output of the logic */
export function scaledColor(color: string, level: number): string {
  const m = /^#([0-9a-fA-F]{2})([0-9a-fA-F]{2})([0-9a-fA-F]{2})$/.exec(color ?? "");
  const f = Math.max(0, Math.min(100, level)) / 100;
  const parts = m ? [m[1], m[2], m[3]].map(h => parseInt(h, 16)) : [255, 255, 255];
  return "#" + parts.map(p => Math.round(p * f).toString(16).padStart(2, "0")).join("").toUpperCase();
}

/** commands of the logic's command input; names lose ':' because it separates the parts */
export const LightingCommands = {
  mood: (id: number) => `mood:${id}`,
  on: () => "on",
  off: () => "off",
  setLevel: (circuit: number, level: number) => `set:${circuit}:${Math.round(level)}`,
  setSwitch: (circuit: number, on: boolean) => `set:${circuit}:${on ? "on" : "off"}`,
  setColor: (circuit: number, color: string) => `set:${circuit}:${color}`,
  setWhite: (circuit: number, level: number, kelvin: number) => `set:${circuit}:${Math.round(level)}@${Math.round(kelvin)}`,
  saveNew: (name: string) => `save:new:${cleanName(name)}`,
  save: (id: number, name: string) => `save:${id}:${cleanName(name)}`,
  rename: (id: number, name: string) => `rename:${id}:${cleanName(name)}`,
  delete: (id: number) => `delete:${id}`
};

export function cleanName(name: string): string {
  return (name ?? "").replace(/[:\r\n]/g, " ").trim();
}
