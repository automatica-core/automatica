/**
 * webapi/security-visu: the state of a security logic (burglar, fire/water, emergency alarm, alarm chain, access controller)
 * as the logic builds it. Labels and events are translation keys of the logic plugin. Never contains codes or tags.
 */

export type SecuritySeverity = "ok" | "info" | "warning" | "alarm";

export interface SecurityAction {
    command: string;
    /** translation key */
    label: string;
    style?: "primary" | "danger" | "default";
    /** ask before sending (e.g. disarm, reset) */
    confirm?: boolean;
}

export interface SecurityDetail {
    /** translation key */
    label: string;
    value: string;
    /** the value is a translation key too */
    translate?: boolean;
}

export interface SecurityLogEntry {
    time: string;
    /** translation key */
    event: string;
    detail?: string;
}

export interface SecurityUser {
    name: string;
    groups: string[];
    hasCode: boolean;
    hasTag: boolean;
}

export interface SecurityState {
    status: string;
    /** translation key */
    statusText: string;
    severity: SecuritySeverity;
    icon?: string;
    /** ISO time a running delay (arming, entry delay...) ends */
    countdownUntil?: string;
    details?: SecurityDetail[];
    actions: SecurityAction[];
    log: SecurityLogEntry[];
    /** access controller, administrators only */
    users?: SecurityUser[];
    groups?: string[];
    /** a tag is being learned for this user until the given ISO time */
    learn?: { name: string; until: string };
    /** schedule lines the logic could not read */
    scheduleErrors?: string[];
    authMode?: number;
    minCodeLength?: number;
}

export interface SecurityControlInfo {
    id: string;
    name: string;
    kind: string;
    commands: string[];
    /** visu or administrator role: may send the commands */
    canCommand: boolean;
    isAdmin: boolean;
    state: SecurityState;
}
