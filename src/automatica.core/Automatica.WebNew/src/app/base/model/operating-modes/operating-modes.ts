/** the built-in modes, their names are translated (UI.OPERATING_MODES.MODE.<KEY>) */
export const BUILT_IN_MODES = ["public-holiday", "vacation", "school-holidays"];

export interface OperatingModeDefinition {
    key: string;
    /** empty for the built-in modes */
    name: string;
    /** higher wins */
    priority: number;
    builtIn?: boolean;
    /** manual override: true = on, false = off, null/undefined = the operating times */
    manual?: boolean | null;
}

/** an operating time: the mode is active from start to end (whole days, both included) */
export interface OperatingPeriod {
    id?: string;
    name: string;
    mode: string;
    /** yyyy-MM-dd */
    start: string;
    end: string;
    yearly: boolean;
    enabled: boolean;
}

export interface OperatingModeConfiguration {
    version?: number;
    country: string;
    region: string;
    disabledHolidays: string[];
    modes: OperatingModeDefinition[];
    periods: OperatingPeriod[];
}

export interface OperatingModeReason {
    /** holiday | period | manual */
    type: string;
    key: string;
    name: string;
}

export interface OperatingModeDay {
    date: string;
    modes: string[];
    reasons?: { [mode: string]: OperatingModeReason[] };
}

export interface HolidayRegion {
    code: string;
    name: string;
}

export interface HolidayCountry {
    code: string;
    name: string;
    regions: HolidayRegion[];
}

export interface Holiday {
    key: string;
    date: string;
    enabled?: boolean;
}

/** webapi/operating-modes */
export interface OperatingModeOverview {
    configuration: OperatingModeConfiguration;
    today: string;
    active: string[];
    days: OperatingModeDay[];
    holidays: Holiday[];
    countries: HolidayCountry[];
}

/** webapi/operating-modes-visu */
export interface OperatingModeVisuState {
    today: string;
    modes: OperatingModeDefinition[];
    active: string[];
    days: OperatingModeDay[];
}

/** the same key the server makes of a name ("Home office" -> "home-office") */
export function operatingModeSlug(value: string): string {
    if (!value) {
        return "";
    }
    let slug = value.trim().toLowerCase()
        .replace(/ä/g, "ae").replace(/ö/g, "oe").replace(/ü/g, "ue").replace(/ß/g, "ss");
    slug = slug.replace(/[^a-z0-9-]+/g, "-").replace(/^-+|-+$/g, "");
    return slug.length > 50 ? slug.substring(0, 50).replace(/-+$/g, "") : slug;
}

/** the display name of a mode: its own name, the translation of a built-in one, else the key */
export function operatingModeName(mode: OperatingModeDefinition | undefined, key: string, translate: (key: string) => string): string {
    if (mode?.name) {
        return mode.name;
    }
    const k = (mode?.key ?? key ?? "");
    if (BUILT_IN_MODES.includes(k)) {
        return translate("UI.OPERATING_MODES.MODE." + k.toUpperCase().replace(/-/g, "_"));
    }
    return k;
}
