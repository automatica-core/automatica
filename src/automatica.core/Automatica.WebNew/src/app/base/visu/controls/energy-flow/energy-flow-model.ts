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
  /** view box height: {@link VIEW_HEIGHT}, or {@link VIEW_HEIGHT_TALL} when the bottom consumer row is used */
  height: number;
}

/** extra consumers of the energy monitor logic (consumer1..6) */
export const CONSUMER_COUNT = 6;

export const VIEW_WIDTH = 320;
export const VIEW_HEIGHT = 220;
/** with a fifth or sixth consumer the tile grows by a row below the battery */
export const VIEW_HEIGHT_TALL = 300;
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
  battery: { x: 160, y: 190 }
};

/**
 * Place of each consumer (index 0 = consumer1). Consumers 1-4 use the corners, so the tile keeps its compact size;
 * 5 and 6 add a row below the battery. That row sits in the columns between the outer nodes and the battery, so its
 * curves run past them instead of through them. Fixed per number, so a node never moves when another one appears.
 */
export const CONSUMER_SLOTS: EnergyPoint[] = [
  { x: 278, y: 190 },
  { x: 278, y: 30 },
  { x: 42, y: 190 },
  { x: 42, y: 30 },
  { x: 101, y: 270 },
  { x: 219, y: 270 }
];

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
  for (let i = 0; i < CONSUMER_COUNT; i++) {
    const consumer = options.consumers[i];
    const power = values.consumers[i];
    if (consumer?.present || consumer?.name || Math.abs(finite(power)) >= ACTIVE_THRESHOLD) {
      add(`consumer${i + 1}`, "consumer", power, { index: i + 1, name: consumer?.name || undefined, position: CONSUMER_SLOTS[i] });
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

  const height = nodes.some(n => n.position.y > VIEW_HEIGHT) ? VIEW_HEIGHT_TALL : VIEW_HEIGHT;
  return { nodes, links, height };
}

/** group number of the house in the circuit breakdown, 1-6 are the consumers */
export const HOUSE_GROUP = 0;

/** one measured circuit as GetDataForVisu of the logic sends it ("circuitGroups"), kW and kWh */
export interface EnergyCircuitData {
  circuit: number;
  name?: string | null;
  /** the circuit input got a value (a named circuit without one is listed too) */
  hasValue?: boolean;
  power?: number;
  today?: number;
  /** what the circuits below it measure */
  measured?: number;
  /** own power - measured, only for a circuit with a value and circuits below it */
  rest?: number | null;
  restToday?: number | null;
  /** the circuits that belong to this one */
  circuits?: EnergyCircuitData[];
}

/** the house (0) or a consumer (1-6) with its circuits and the part no circuit measures */
export interface EnergyCircuitGroupData {
  group: number;
  total?: number;
  measured?: number;
  rest?: number;
  restToday?: number;
  circuits?: EnergyCircuitData[];
}

export interface EnergyBreakdownRow {
  id: string;
  kind: "circuit" | "rest";
  /** circuit number, 1 based */
  circuit?: number;
  name?: string;
  power: number;
  today?: number;
  /** bar width in %: share of what it belongs to (or of the measured sum when the circuits read more than that) */
  share: number;
  /** a named circuit whose input has no value yet */
  noValue: boolean;
  /** 0 directly below the house or consumer, 1 below a circuit, ... */
  depth: number;
  /** other circuits belong to this one, they follow it in the rows */
  hasChildren: boolean;
  /** circuit numbers of the circuits it sits below, the closest last; empty directly below the house or consumer */
  ancestors: number[];
}

/** a row is shown while every circuit above it is opened */
export function isRowVisible(row: EnergyBreakdownRow, opened: ReadonlySet<number>): boolean {
  return row.ancestors.every(circuit => opened.has(circuit));
}

export interface EnergyBreakdownGroup {
  id: string;
  group: number;
  total: number;
  measured: number;
  rest: number;
  restToday?: number;
  /** % of the total the circuits measure, undefined without a total */
  coverage?: number;
  /** a meter without circuits of its own: the popup shows only its name and power */
  plain?: boolean;
  /** the circuit tree as rows: circuits by power, each followed by its own circuits and their rest; the group's rest last */
  rows: EnergyBreakdownRow[];
}

function optional(value: number | undefined | null): number | undefined {
  const n = Number(value);
  return value === undefined || value === null || !isFinite(n) ? undefined : n;
}

function validCircuits(circuits: EnergyCircuitData[] | null | undefined): EnergyCircuitData[] {
  return Array.isArray(circuits) ? circuits.filter(c => c && isFinite(Number(c.circuit))) : [];
}

function sumPower(circuits: EnergyCircuitData[]): number {
  return circuits.reduce((sum, c) => sum + Math.max(finite(c.power), 0), 0);
}

/** bar width in % of scale */
function shareOf(power: number, scale: number): number {
  return scale > 0 ? Math.max(0, Math.min(100, power / scale * 100)) : 0;
}

/**
 * Appends the circuits (most power first) with, right after each one, the circuits below it and their rest one level
 * deeper. scale is the power the bars are relative to: what the circuits belong to.
 */
function appendCircuitRows(circuits: EnergyCircuitData[], scale: number, ancestors: number[], rows: EnergyBreakdownRow[], seen: Set<number>) {
  const depth = ancestors.length;
  const sorted = circuits
    .filter(c => !seen.has(Number(c.circuit)))
    .map(c => ({ data: c, circuit: Number(c.circuit), power: Math.max(finite(c.power), 0) }))
    .sort((a, b) => b.power - a.power || a.circuit - b.circuit);

  for (const { data, circuit, power } of sorted) {
    // the logic never sends a circuit twice; this keeps a broken answer from looping
    if (seen.has(circuit)) {
      continue;
    }
    seen.add(circuit);
    const children = validCircuits(data.circuits).filter(c => !seen.has(Number(c.circuit)));
    const name = typeof data.name === "string" && data.name.trim() ? data.name.trim() : undefined;
    const noValue = data.hasValue === false;
    rows.push({
      id: `circuit${circuit}`, kind: "circuit", circuit, name, power, today: optional(data.today),
      share: noValue ? 0 : shareOf(power, scale), noValue, depth, hasChildren: children.length > 0, ancestors
    });

    if (children.length > 0) {
      const measured = optional(data.measured) ?? sumPower(children);
      const childScale = Math.max(noValue ? 0 : power, measured);
      const below = [...ancestors, circuit];
      appendCircuitRows(children, childScale, below, rows, seen);
      const rest = optional(data.rest);
      if (rest !== undefined && !noValue) {
        rows.push({
          id: `rest${circuit}`, kind: "rest", circuit, power: Math.max(rest, 0), today: optional(data.restToday),
          share: shareOf(Math.max(rest, 0), childScale), noValue: false, depth: depth + 1, hasChildren: false, ancestors: below
        });
      }
    }
  }
}

/**
 * Rows of the details popup from the "circuitGroups" of the logic: house first, then the consumers. Inside a group the
 * circuits with the most power first, each followed by the circuits that belong to it, and the rest last. The circuits
 * do not have to add up: what they leave is the rest, and when they read more than what they belong to the bars are
 * scaled to the measured sum instead.
 */
export function buildBreakdown(groups: EnergyCircuitGroupData[] | null | undefined): EnergyBreakdownGroup[] {
  if (!Array.isArray(groups)) {
    return [];
  }
  return groups
    .filter(g => g && isFinite(Number(g.group)) && Array.isArray(g.circuits) && g.circuits.length > 0)
    .map(g => {
      const group = Number(g.group);
      const circuits = validCircuits(g.circuits);
      const measured = optional(g.measured) ?? sumPower(circuits);
      const total = Math.max(finite(g.total), 0);
      const rest = Math.max(optional(g.rest) ?? total - measured, 0);
      const scale = Math.max(total, measured);

      const rows: EnergyBreakdownRow[] = [];
      appendCircuitRows(circuits, scale, [], rows, new Set<number>());
      rows.push({
        id: "rest", kind: "rest", power: rest, today: optional(g.restToday), share: shareOf(rest, scale), noValue: false,
        depth: 0, hasChildren: false, ancestors: []
      });

      return {
        id: `group${group}`, group, total, measured, rest, restToday: optional(g.restToday),
        coverage: total > 0 ? Math.min(measured / total * 100, 100) : undefined,
        rows
      };
    })
    .sort((a, b) => a.group - b.group);
}

/**
 * All groups of the details popup: the ones with circuits from {@link buildBreakdown}, plus the house and every present
 * consumer without circuits as a plain entry with its power, so the popup lists every meter. House first, then the
 * consumers by number.
 */
export function withPlainGroups(groups: EnergyBreakdownGroup[], house: number | undefined,
  consumers: { present: boolean; power?: number }[]): EnergyBreakdownGroup[] {
  const result = [...groups];
  const has = new Set(groups.map(g => g.group));
  const plain = (group: number, power: number | undefined) => {
    // NaN while the consumer has no value yet: shown as "–"
    const value = optional(power);
    const total = value === undefined ? NaN : Math.max(value, 0);
    result.push({ id: `group${group}`, group, total, measured: 0, rest: total, rows: [], plain: true });
  };
  if (!has.has(HOUSE_GROUP) && optional(house) !== undefined) {
    plain(HOUSE_GROUP, house);
  }
  consumers.forEach((consumer, i) => {
    const group = i + 1;
    if (!has.has(group) && (consumer.present || optional(consumer.power) !== undefined)) {
      plain(group, consumer.power);
    }
  });
  return result.sort((a, b) => a.group - b.group);
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
