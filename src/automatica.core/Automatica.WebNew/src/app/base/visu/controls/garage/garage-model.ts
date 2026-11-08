/** state output of the garage / gate logic */
export enum GarageState {
  Closed = 0,
  Open = 1,
  PartiallyOpen = 2,
  Opening = 3,
  Closing = 4
}

/** type parameter of the garage / gate logic: how the visu draws it */
export enum GarageType {
  Garage = 0,
  SlidingGate = 1,
  SwingGate = 2
}

export interface GarageMotion {
  /** 0 = closed, 100 = open, at the time the values arrived */
  position: number;
  /** 1 = opening, -1 = closing, 0 = stands */
  motion: number;
  /** where the drive stops (partial open), 0 or 100 for a full run */
  target?: number;
  openSeconds: number;
  closeSeconds: number;
}

export const GARAGE_STATE_TEXTS: { [state: number]: string } = {
  [GarageState.Closed]: "UI.VISU.GARAGE.CLOSED",
  [GarageState.Open]: "UI.VISU.GARAGE.OPEN",
  [GarageState.PartiallyOpen]: "UI.VISU.GARAGE.PARTIALLY_OPEN",
  [GarageState.Opening]: "UI.VISU.GARAGE.OPENING",
  [GarageState.Closing]: "UI.VISU.GARAGE.CLOSING"
};

export function clampPercent(value: any): number {
  const v = Number(value);
  if (value === null || value === undefined || isNaN(v)) {
    return 0;
  }
  return Math.max(0, Math.min(100, v));
}

/**
 * The logic sends the position when the drive starts and stops; while it moves the visu runs the position on with the
 * travel times, up to the target.
 */
export function extrapolateGaragePosition(m: GarageMotion, elapsedMs: number): number {
  const position = clampPercent(m.position);
  if (!m.motion || elapsedMs <= 0) {
    return position;
  }
  const seconds = m.motion > 0 ? m.openSeconds : m.closeSeconds;
  if (!(seconds > 0)) {
    return position;
  }
  const step = (100 / seconds) * (elapsedMs / 1000);
  const target = typeof m.target === "number" && !isNaN(m.target) ? clampPercent(m.target) : (m.motion > 0 ? 100 : 0);
  return m.motion > 0
    ? Math.min(position + step, Math.max(target, position))
    : Math.max(position - step, Math.min(target, position));
}

/** the state of a position when the drive stands (the logic sends it, this is the fallback before the first values) */
export function stateOfPosition(position: number): GarageState {
  if (position <= 0.5) {
    return GarageState.Closed;
  }
  return position >= 99.5 ? GarageState.Open : GarageState.PartiallyOpen;
}
