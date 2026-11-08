export interface IrrigationZone {
  /** 1..8, the value of the select input */
  zone: number;
  name: string;
  /** seconds */
  duration: number;
}

export interface IrrigationHistoryItem {
  at: Date;
  /** start, manual, skip, zone, done, stop, select */
  kind: string;
  zone: number;
  factor: number;
  rainForecast: number;
}

/** 75 -> "1:15", 3725 -> "1:02:05" */
export function formatDuration(seconds: any): string {
  let s = Math.max(0, Math.round(Number(seconds) || 0));
  const h = Math.floor(s / 3600);
  s -= h * 3600;
  const m = Math.floor(s / 60);
  s -= m * 60;
  const ss = s.toString().padStart(2, "0");
  return h > 0 ? `${h}:${m.toString().padStart(2, "0")}:${ss}` : `${m}:${ss}`;
}

/** the zones with a duration, plus the one that runs now */
export function irrigationZones(names: any, durations: any, activeZone = 0): IrrigationZone[] {
  const n: any[] = Array.isArray(names) ? names : [];
  const d: any[] = Array.isArray(durations) ? durations : [];
  const zones: IrrigationZone[] = [];
  for (let i = 0; i < 8; i++) {
    const duration = Math.max(0, Number(d[i]) || 0);
    const name = (n[i] ?? "").toString().trim() || `${i + 1}`;
    if (duration > 0 || activeZone === i + 1) {
      zones.push({ zone: i + 1, name, duration });
    }
  }
  return zones;
}

/** 0..100 of the running zone, from the seconds left and the zone's time (times the rain factor) */
export function zoneProgress(remaining: number, duration: number, factor = 1): number {
  const total = duration * (factor > 0 ? factor : 1);
  if (!(total > 0)) {
    return 0;
  }
  return Math.max(0, Math.min(100, 100 * (1 - remaining / total)));
}

/** the history of the logic, newest first; accepts camelCase and PascalCase properties */
export function irrigationHistory(history: any): IrrigationHistoryItem[] {
  if (!Array.isArray(history)) {
    return [];
  }
  return history
    .map(a => ({
      at: new Date(a?.at ?? a?.At),
      kind: (a?.kind ?? a?.Kind ?? "").toString(),
      zone: Number(a?.zone ?? a?.Zone ?? 0),
      factor: Number(a?.factor ?? a?.Factor ?? 1),
      rainForecast: Number(a?.rainForecast ?? a?.RainForecast ?? 0)
    }))
    .filter(a => !isNaN(a.at.getTime()))
    .sort((a, b) => b.at.getTime() - a.at.getTime());
}

export const IRRIGATION_HISTORY_TEXTS: { [kind: string]: string } = {
  start: "UI.VISU.IRRIGATION.HISTORY_START",
  manual: "UI.VISU.IRRIGATION.HISTORY_MANUAL",
  skip: "UI.VISU.IRRIGATION.HISTORY_SKIP",
  zone: "UI.VISU.IRRIGATION.HISTORY_ZONE",
  done: "UI.VISU.IRRIGATION.HISTORY_DONE",
  stop: "UI.VISU.IRRIGATION.HISTORY_STOP",
  select: "UI.VISU.IRRIGATION.HISTORY_SELECT"
};

/** "08.11.26, 09:28" in the given language (the browser's when none) */
export function formatHistoryTime(at: Date, locale?: string): string {
  return at.toLocaleString(locale, { dateStyle: "short", timeStyle: "short" });
}

/** short weekday names of DayOfWeek values (0 = Sunday), Monday first */
export function weekdayNames(days: any, locale?: string): string[] {
  if (!Array.isArray(days)) {
    return [];
  }
  const set = new Set(days.map(a => Number(a)));
  const order = [1, 2, 3, 4, 5, 6, 0];
  // 2026-01-04 is a Sunday
  return order.filter(d => set.has(d))
    .map(d => new Date(2026, 0, 4 + d).toLocaleDateString(locale, { weekday: "short" }));
}
