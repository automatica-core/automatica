/**
 * Layout and flow maths of the energy flow tile (energy monitor logic of the metering plugin), kept free of Angular so
 * it can be tested on its own. All powers are kW as the logic sends them: grid + import / - export,
 * battery + charging / - discharging.
 */

export type EnergyNodeKind = "pv" | "grid" | "battery" | "house" | "consumer";

/** node -> hub ("in"), hub -> node ("out") or nothing flows */
export type EnergyFlowDirection = "in" | "out" | "none";

export interface EnergyValues {
  grid?: number;
  pv?: number;
  battery?: number;
  soc?: number;
  house?: number;
  consumers: (number | undefined)[];
  autarkyToday?: number;
  selfConsumptionToday?: number;
  pvToday?: number;
}

export interface EnergyConsumerConfig {
  name?: string;
  /** the logic got a value for this consumer (or a name is set) */
  present: boolean;
}

export interface EnergyFlowOptions {
  /** kW that draws the thickest and fastest line */
  peakPower: number;
  hasBattery: boolean;
  consumers: EnergyConsumerConfig[];
}

export interface EnergyPoint {
  x: number;
  y: number;
}

export interface EnergyNode {
  id: string;
  kind: EnergyNodeKind;
  /** consumer number, 1 based */
  index?: number;
  name?: string;
  position: EnergyPoint;
  power: number;
  direction: EnergyFlowDirection;
}

export interface EnergyLink {
  id: string;
  kind: EnergyNodeKind;
  /** drawn in flow direction, so the moving dots always run from the start to the end of the path */
  path: string;
  power: number;
  direction: EnergyFlowDirection;
  active: boolean;
  width: number;
  /** seconds for one dot spacing */
  duration: number;
}

export interface EnergyFlowLayout {
  nodes: EnergyNode[];
  links: EnergyLink[];
}

export const VIEW_WIDTH = 320;
export const VIEW_HEIGHT = 220;
export const HUB: EnergyPoint = { x: 160, y: 110 };
/** distance of two dots along a line, the stroke-dasharray period in the template */
export const DOT_SPACING = 14;
/** below this (kW, 5 W) nothing is drawn as flowing, meters jitter around 0 */
export const ACTIVE_THRESHOLD = 0.005;

const MIN_WIDTH = 1.5;
const MAX_WIDTH = 6;
const MIN_SPEED = 8;   // view units per second at (almost) no power
const MAX_SPEED = 56;  // at the peak power

const POSITIONS: { [key: string]: EnergyPoint } = {
  pv: { x: 160, y: 30 },
  grid: { x: 42, y: 110 },
  house: { x: 278, y: 110 },
  battery: { x: 160, y: 190 },
  consumer1: { x: 278, y: 190 },
  consumer2: { x: 278, y: 30 },
  consumer3: { x: 42, y: 190 }
};

function finite(value: number | undefined | null): number {
  const n = Number(value);
  return value === undefined || value === null || !isFinite(n) ? 0 : n;
}

/** share of the peak power, 0..1 */
export function load(power: number, peakPower: number): number {
  const peak = finite(peakPower) > 0 ? finite(peakPower) : 10;
  return Math.max(0, Math.min(1, Math.abs(finite(power)) / peak));
}

/** line width: square root, so small flows are still visible next to big ones */
export function strokeWidth(power: number, peakPower: number): number {
  return MIN_WIDTH + (MAX_WIDTH - MIN_WIDTH) * Math.sqrt(load(power, peakPower));
}

/** seconds the dots need for one spacing: more power, faster dots */
export function flowDuration(power: number, peakPower: number): number {
  const speed = MIN_SPEED + (MAX_SPEED - MIN_SPEED) * load(power, peakPower);
  return Math.round(DOT_SPACING / speed * 1000) / 1000;
}

/** direction relative to the hub for the signed power of a node */
export function flowDirection(kind: EnergyNodeKind, power: number): EnergyFlowDirection {
  const p = finite(power);
  if (Math.abs(p) < ACTIVE_THRESHOLD) {
    return "none";
  }
  switch (kind) {
    case "pv":
      return p > 0 ? "in" : "none";
    case "grid":
      // import comes in from the grid, export goes out
      return p > 0 ? "in" : "out";
    case "battery":
      // charging takes power from the hub, discharging gives it
      return p > 0 ? "out" : "in";
    default:
      return p > 0 ? "out" : "none";
  }
}

/** the line from a node to the hub; nodes not on an axis get a curve that enters the hub horizontally */
export function linkPath(from: EnergyPoint, direction: EnergyFlowDirection): string {
  const straight = from.x === HUB.x || from.y === HUB.y;
  const forward = straight
    ? `M ${from.x} ${from.y} L ${HUB.x} ${HUB.y}`
    : `M ${from.x} ${from.y} Q ${from.x} ${HUB.y} ${HUB.x} ${HUB.y}`;
  if (direction !== "out") {
    return forward;
  }
  return straight
    ? `M ${HUB.x} ${HUB.y} L ${from.x} ${from.y}`
    : `M ${HUB.x} ${HUB.y} Q ${from.x} ${HUB.y} ${from.x} ${from.y}`;
}

export function buildEnergyFlow(values: EnergyValues, options: EnergyFlowOptions): EnergyFlowLayout {
  const nodes: EnergyNode[] = [];
  const add = (id: string, kind: EnergyNodeKind, power: number, extra: Partial<EnergyNode> = {}) => {
    nodes.push({ id, kind, position: POSITIONS[id], power: finite(power), direction: flowDirection(kind, power), ...extra });
  };

  add("pv", "pv", values.pv);
  add("grid", "grid", values.grid);
  add("house", "house", values.house);
  if (options.hasBattery || Math.abs(finite(values.battery)) >= ACTIVE_THRESHOLD) {
    add("battery", "battery", values.battery);
  }
  for (let i = 0; i < 3; i++) {
    const consumer = options.consumers[i];
    const power = values.consumers[i];
    if (consumer?.present || consumer?.name || Math.abs(finite(power)) >= ACTIVE_THRESHOLD) {
      add(`consumer${i + 1}`, "consumer", power, { index: i + 1, name: consumer?.name || undefined });
    }
  }

  const links = nodes.map<EnergyLink>(node => ({
    id: node.id,
    kind: node.kind,
    path: linkPath(node.position, node.direction),
    power: Math.abs(node.power),
    direction: node.direction,
    active: node.direction !== "none",
    width: node.direction === "none" ? MIN_WIDTH : strokeWidth(node.power, options.peakPower),
    duration: flowDuration(node.power, options.peakPower)
  }));

  return { nodes, links };
}

/** 0.42 kW -> 420 W, 3.456 kW -> 3.46 kW, 12.3 kW -> 12.3 kW */
export function formatPower(kw: number | undefined, locale = "en"): { value: string; unit: string } {
  if (kw === undefined || kw === null || !isFinite(Number(kw))) {
    return { value: "–", unit: "" };
  }
  const abs = Math.abs(Number(kw));
  if (abs < 1) {
    return { value: Math.round(abs * 1000).toLocaleString(locale), unit: "W" };
  }
  const digits = abs < 10 ? 2 : 1;
  return { value: abs.toLocaleString(locale, { minimumFractionDigits: digits, maximumFractionDigits: digits }), unit: "kW" };
}

export function formatNumber(value: number | undefined, digits: number, locale = "en"): string {
  if (value === undefined || value === null || !isFinite(Number(value))) {
    return "–";
  }
  return Number(value).toLocaleString(locale, { minimumFractionDigits: digits, maximumFractionDigits: digits });
}
