import { IconPrefix, IconProp } from "@fortawesome/fontawesome-svg-core";
import { FaIconLibrary } from "@fortawesome/angular-fontawesome";

const PREFIXES: IconPrefix[] = ["fas", "far", "fad"];

/**
 * Icon of a status rule as the user typed it: "fire", "fad fire", "fad:fire" or "fa-solid fa-fire".
 * Unknown icons give the fallback, so a typo never breaks the tile.
 */
export function parseIcon(library: FaIconLibrary, text: string | null | undefined, fallback: IconProp): IconProp {
  const raw = (text ?? "").toString().trim().toLowerCase();
  if (!raw) {
    return fallback;
  }

  const parts = raw.split(/[\s:]+/).map(a => a.replace(/^fa-/, "")).filter(a => !!a);
  let prefix: IconPrefix | undefined;
  let name = parts[parts.length - 1];
  if (parts.length > 1) {
    const p = parts[0];
    prefix = (p === "solid" ? "fas" : p === "regular" ? "far" : p === "duotone" ? "fad" : p) as IconPrefix;
  }

  for (const candidate of prefix ? [prefix] : PREFIXES) {
    if (library.getIconDefinition(candidate, name as any)) {
      return [candidate, name as any];
    }
  }
  return fallback;
}

/** a color the user picked (#rgb, #rrggbb, rgb(), named), undefined for anything else */
export function safeColor(color: string | null | undefined): string | undefined {
  const value = (color ?? "").toString().trim();
  if (!value) {
    return undefined;
  }
  return /^(#[0-9a-f]{3,8}|rgba?\([\d\s.,%]+\)|hsla?\([\d\s.,%deg]+\)|[a-z]{3,20})$/i.test(value) ? value : undefined;
}

/** "14:05" today, "Mon 14:05" in the last week, "03.11. 14:05" (locale order) before */
export function formatTime(value: string | Date | null | undefined, locale: string, now: Date = new Date()): string {
  if (!value) {
    return "";
  }
  const date = value instanceof Date ? value : new Date(value);
  if (isNaN(date.getTime())) {
    return "";
  }
  const time = new Intl.DateTimeFormat(locale, { hour: "2-digit", minute: "2-digit" }).format(date);
  const sameDay = date.toDateString() === now.toDateString();
  if (sameDay) {
    return time;
  }
  const days = (now.getTime() - date.getTime()) / 86400000;
  if (days >= 0 && days < 6) {
    return `${new Intl.DateTimeFormat(locale, { weekday: "short" }).format(date)} ${time}`;
  }
  return `${new Intl.DateTimeFormat(locale, { day: "2-digit", month: "2-digit" }).format(date)} ${time}`;
}

/** full date and time for the title / screen readers */
export function formatDateTime(value: string | Date | null | undefined, locale: string): string {
  if (!value) {
    return "";
  }
  const date = value instanceof Date ? value : new Date(value);
  if (isNaN(date.getTime())) {
    return "";
  }
  return new Intl.DateTimeFormat(locale, { dateStyle: "medium", timeStyle: "medium" }).format(date);
}

/**
 * Calls the action once after a burst of calls (several outputs of a logic change at once)
 */
export class Debounce {
  private timer: any;

  constructor(private readonly ms: number) { }

  run(action: () => void) {
    clearTimeout(this.timer);
    this.timer = setTimeout(action, this.ms);
  }

  cancel() {
    clearTimeout(this.timer);
  }
}
