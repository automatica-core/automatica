// Automatic shading, see Automatica.Core.Shading/README.md. Property names and enum values are camelCase as on the
// wire; the definitions are edited in place, so properties a newer server adds survive a round trip.

// ---------- signals ----------

export type SignalSourceType = "datapoint" | "calculated" | "irradiance" | "pv";

/** what a signal is used for, decides which source types it offers */
export type SignalKind = "sun" | "brightness" | "measured";

export const SOURCE_TYPES_BY_KIND: Record<SignalKind, SignalSourceType[]> = {
    sun: ["datapoint", "calculated"],
    brightness: ["datapoint", "irradiance", "pv", "calculated"],
    measured: ["datapoint"]
};

export interface DatapointSource {
    type: "datapoint";
    node?: string;
    factor: number;
    offset: number;
    staleAfterMinutes: number;
}

export interface CalculatedSource {
    type: "calculated";
    /** cloud cover in percent, brightness only */
    cloudCover?: Signal | null;
}

export interface IrradianceSource {
    type: "irradiance";
    node?: string;
    factor: number;
    offset: number;
    staleAfterMinutes: number;
}

export interface PvSource {
    type: "pv";
    node?: string;
    factor: number;
    peakPower: number;
    azimuth: number;
    tilt: number;
    performanceRatio: number;
    staleAfterMinutes: number;
}

export type SignalSource = DatapointSource | CalculatedSource | IrradianceSource | PvSource;

/** sources in priority order, the first one with a fresh value is used */
export interface Signal {
    sources: SignalSource[];
}

export function newSource(type: SignalSourceType): SignalSource {
    switch (type) {
        case "datapoint":
            return { type, factor: 1, offset: 0, staleAfterMinutes: 0 };
        case "calculated":
            return { type, cloudCover: { sources: [] } };
        case "irradiance":
            return { type, factor: 1, offset: 0, staleAfterMinutes: 15 };
        case "pv":
            return { type, factor: 1, peakPower: 10000, azimuth: 180, tilt: 30, performanceRatio: 0.85, staleAfterMinutes: 15 };
    }
}

// ---------- settings ----------

export type ManualOverrideMode = "untilChange" | "duration" | "untilEndOfDay";
export const MANUAL_OVERRIDE_MODES: ManualOverrideMode[] = ["untilChange", "duration", "untilEndOfDay"];

export interface SunThresholds {
    onLux: number;
    offLux: number;
    onDelayMinutes: number;
    offDelayMinutes: number;
}

export type WinterMode = "off" | "temperature" | "months";
export const WINTER_MODES: WinterMode[] = ["off", "temperature", "months"];

export interface WinterDefinition {
    mode: WinterMode;
    below: number;
    fromMonth: number;
    toMonth: number;
}

export interface ShadingSettings {
    schema?: number;
    isEnabled: boolean;
    sunAzimuth: Signal;
    sunElevation: Signal;
    brightness: Signal;
    thresholds: SunThresholds;
    wind: Signal;
    windLimit: number;
    windReleaseMinutes: number;
    rain: Signal;
    rainReleaseMinutes: number;
    outsideTemperature: Signal;
    frostBelow?: number | null;
    manualOverride: ManualOverrideMode;
    manualOverrideMinutes: number;
    minMoveIntervalMinutes: number;
    minPositionChange: number;
    minSlatChange: number;
    winter: WinterDefinition;
}

// ---------- facades ----------

export interface FacadeDefinition {
    schema?: number;
    brightness: Signal;
    thresholds?: SunThresholds | null;
    acceptanceAngle: number;
    minElevation: number;
    maxElevation: number;
    minOutsideTemperature?: number | null;
}

export interface ShadingFacade {
    objId?: string;
    name: string;
    azimuth: number;
    isEnabled: boolean;
    definition: FacadeDefinition;
}

// ---------- blinds ----------

export type BlindType = "roller" | "venetian";
export const BLIND_TYPES: BlindType[] = ["roller", "venetian"];

export interface SlatDefinition {
    trackSun: boolean;
    fixedAngle: number;
    width: number;
    spacing: number;
    extraAngle: number;
    minAngle: number;
    maxAngle: number;
    percentAtMinAngle: number;
    percentAtMaxAngle: number;
    openPercent: number;
}

export interface BlindDefinition {
    schema?: number;
    type: BlindType;
    azimuth?: number | null;
    acceptanceAngle?: number | null;
    minElevation?: number | null;
    maxElevation?: number | null;
    shadePosition: number;
    restorePosition: boolean;
    openPosition: number;
    slats: SlatDefinition;
    /** null: venetian blinds yes, roller blinds no */
    windProtection?: boolean | null;
    windLimit?: number | null;
    rainProtection: boolean;
    doorContact?: string | null;
    travelSeconds: number;
    winterGain: boolean;
    roomTemperature: Signal;
    maxRoomTemperature?: number | null;
}

export interface ShadingBlind {
    controlId: string;
    name: string;
    this2ShadingFacade?: string | null;
    isEnabled: boolean;
    hasSlats: boolean;
    isLoaded: boolean;
    isPaused: boolean;
    definition: BlindDefinition;
}

export interface AvailableBlind {
    controlId: string;
    name: string;
    hasSlats: boolean;
    isConfigured: boolean;
}

/** an error of the server validation (400 answer) */
export interface ShadingProblem {
    path: string;
    code: string;
    detail?: string;
}

// ---------- status ----------

export interface SunPosition {
    azimuth: number;
    elevation: number;
}

export interface SignalInfo {
    value?: number | null;
    source: string;
}

export type BlindMode = "off" | "paused" | "unavailable" | "wind" | "rain" | "frost" | "manual" | "doorOpen" | "shading" | "idle" | "winter";

export interface BlindStatus {
    controlId: string;
    name: string;
    facadeId?: string | null;
    type: BlindType;
    mode: BlindMode;
    reason: string;
    sunOnWindow: boolean;
    demand: boolean;
    position: number;
    slat?: number | null;
    targetPosition?: number | null;
    targetSlat?: number | null;
    manualUntil?: string | null;
    lastMoveAt?: string | null;
}

export interface FacadeStatus {
    id: string;
    name: string;
    azimuth: number;
    isEnabled: boolean;
    sunny: boolean;
    sunOnFacade: boolean;
    brightness?: SignalInfo | null;
    temperatureOk: boolean;
}

export interface ShadingStatus {
    isEnabled: boolean;
    hasLocation: boolean;
    evaluatedAt: string;
    sun?: SunPosition | null;
    sunSource?: string | null;
    wind?: SignalInfo | null;
    windAlarm: boolean;
    rain?: SignalInfo | null;
    rainAlarm: boolean;
    outsideTemperature?: SignalInfo | null;
    frost: boolean;
    winter: boolean;
    facades: FacadeStatus[];
    blinds: BlindStatus[];
}

export interface BlindPreview {
    controlId: string;
    name: string;
    sunOnWindow: boolean;
    shade: boolean;
    targetPosition?: number | null;
    targetSlat?: number | null;
}

export interface ShadingPreview {
    at: string;
    sun?: SunPosition | null;
    winter: boolean;
    facades: FacadeStatus[];
    blinds: BlindPreview[];
}

export interface SunPathPoint {
    at: string;
    azimuth: number;
    elevation: number;
}

/** the visualization's view of a blind */
export interface VisuBlindState {
    controlId: string;
    mode: BlindMode;
    reason: string;
    demand: boolean;
    manualUntil?: string | null;
    automatic: boolean;
}

// ---------- compass ----------

/** the 16 compass points, every 22.5° from north */
export const COMPASS_POINTS = ["N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"];

export function compassPoint(azimuth: number): string {
    const a = ((azimuth % 360) + 360) % 360;
    return COMPASS_POINTS[Math.round(a / 22.5) % 16];
}
