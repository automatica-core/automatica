// Definition document of an automation (schema 1), see Automatica.Core.Automation/README.md.
// Property names and enum values are camelCase as on the wire, every block carries its "type".

export type DayOfWeekName = "monday" | "tuesday" | "wednesday" | "thursday" | "friday" | "saturday" | "sunday";
export const DAYS_OF_WEEK: DayOfWeekName[] = ["monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"];

export type SunEvent = "sunrise" | "sunset" | "dawn" | "dusk";
export const SUN_EVENTS: SunEvent[] = ["sunrise", "sunset", "dawn", "dusk"];

/** a JSON value of a datapoint (bool, number or string) */
export type AutomationValue = boolean | number | string | null;

export interface AutomationDefinition {
    schema: number;
    triggers: TriggerDefinition[];
    conditions: ConditionGroup;
    actions: ActionDefinition[];
}

// ---------- triggers ----------

export type TriggerType = "datapoint" | "time" | "sun" | "conditions" | "control" | "startup";
export const TRIGGER_TYPES: TriggerType[] = ["datapoint", "time", "sun", "conditions", "control", "startup"];

interface TriggerBase {
    /** free id, used by the "started by trigger" condition and the run history */
    id?: string;
}

export type DatapointChange = "any" | "to" | "above" | "below";
export const DATAPOINT_CHANGES: DatapointChange[] = ["any", "to", "above", "below"];

export interface DatapointTrigger extends TriggerBase {
    type: "datapoint";
    node?: string;
    change: DatapointChange;
    value?: AutomationValue;
    /** only fire when the previous value was this one */
    from?: AutomationValue;
    forSeconds: number;
}

export interface TimeTrigger extends TriggerBase {
    type: "time";
    /** "HH:mm" or "HH:mm:ss", local time */
    at?: string;
    /** empty = every day */
    days: DayOfWeekName[];
}

export interface SunTrigger extends TriggerBase {
    type: "sun";
    event: SunEvent;
    offsetMinutes: number;
    days: DayOfWeekName[];
}

export interface ConditionsTrigger extends TriggerBase {
    type: "conditions";
    /** fires when the group changes from not met to met */
    conditions: ConditionGroup;
}

export type ControlStateChange = "any" | "on" | "off";
export const CONTROL_STATE_CHANGES: ControlStateChange[] = ["any", "on", "off"];

export interface ControlTrigger extends TriggerBase {
    type: "control";
    control?: string;
    to: ControlStateChange;
}

export interface StartupTrigger extends TriggerBase {
    type: "startup";
}

export type TriggerDefinition = DatapointTrigger | TimeTrigger | SunTrigger | ConditionsTrigger | ControlTrigger | StartupTrigger;

// ---------- conditions ----------

export type ConditionType = "group" | "datapoint" | "time" | "sun" | "control" | "trigger" | "shading";
/** the leaf types, a group is added with its own button */
export const CONDITION_TYPES: ConditionType[] = ["datapoint", "time", "sun", "control", "shading", "trigger"];

export type GroupOperator = "all" | "any" | "not";
export const GROUP_OPERATORS: GroupOperator[] = ["all", "any", "not"];

export interface ConditionGroup {
    type: "group";
    operator: GroupOperator;
    conditions: ConditionDefinition[];
}

export type CompareOperator = "eq" | "ne" | "lt" | "le" | "gt" | "ge" | "between";
export const COMPARE_OPERATORS: CompareOperator[] = ["eq", "ne", "lt", "le", "gt", "ge", "between"];

export interface DatapointCondition {
    type: "datapoint";
    node?: string;
    operator: CompareOperator;
    value?: AutomationValue;
    /** upper bound for "between" (inclusive) */
    value2?: AutomationValue;
}

export interface TimeCondition {
    type: "time";
    /** "HH:mm", wraps midnight when after > before */
    after?: string;
    before?: string;
    days: DayOfWeekName[];
}

export interface SunCondition {
    type: "sun";
    after?: SunEvent | null;
    afterOffsetMinutes: number;
    before?: SunEvent | null;
    beforeOffsetMinutes: number;
}

export interface ControlCondition {
    type: "control";
    control?: string;
    on: boolean;
}

export interface TriggeredByCondition {
    type: "trigger";
    triggerId?: string;
}

/** the automatic shading of a blind is (or is not) shading it right now */
export interface ShadingCondition {
    type: "shading";
    control?: string;
    shading: boolean;
}

export type ConditionDefinition = ConditionGroup | DatapointCondition | TimeCondition | SunCondition | ControlCondition | TriggeredByCondition
    | ShadingCondition;

// ---------- actions ----------

export type ActionType = "setValue" | "control" | "groupControl" | "delay" | "waitUntil" | "notify" | "if" | "runAutomation" | "enableAutomation" | "shading"
    | "stop";
export const ACTION_TYPES: ActionType[] = ["setValue", "control", "groupControl", "delay", "waitUntil", "notify", "if", "runAutomation", "enableAutomation",
    "shading", "stop"];

interface ActionBase {
    /** a disabled action is skipped (default true) */
    enabled: boolean;
    /** a failing action is logged and the run continues (default false) */
    continueOnError: boolean;
}

export type SetValueMode = "value" | "toggle" | "increment" | "copy";
export const SET_VALUE_MODES: SetValueMode[] = ["value", "toggle", "increment", "copy"];

export interface SetValueAction extends ActionBase {
    type: "setValue";
    node?: string;
    mode: SetValueMode;
    value?: AutomationValue;
    step: number;
    source?: string | null;
}

export type ControlCommand = "on" | "off" | "toggle" | "dim" | "up" | "down" | "stop" | "position";
export const CONTROL_COMMANDS: ControlCommand[] = ["on", "off", "toggle", "dim", "up", "down", "stop", "position"];

export interface ControlAction extends ActionBase {
    type: "control";
    control?: string;
    command: ControlCommand;
    /** percent for dim and position */
    value?: number | null;
}

export interface GroupControlAction extends ActionBase {
    type: "groupControl";
    area?: string | null;
    includeSubAreas: boolean;
    category?: string | null;
    command: ControlCommand;
    value?: number | null;
}

export interface DelayAction extends ActionBase {
    type: "delay";
    seconds: number;
    /** optional, a random delay between seconds and this value */
    randomToSeconds?: number | null;
}

export interface WaitUntilAction extends ActionBase {
    type: "waitUntil";
    condition: ConditionDefinition;
    forSeconds: number;
    /** 0 = never */
    timeoutSeconds: number;
    continueOnTimeout: boolean;
}

export type NotifySeverity = "info" | "warning" | "error";
export const NOTIFY_SEVERITIES: NotifySeverity[] = ["info", "warning", "error"];

export interface NotifyAction extends ActionBase {
    type: "notify";
    subject?: string;
    body?: string;
    severity: NotifySeverity;
    email: string[];
}

export interface IfAction extends ActionBase {
    type: "if";
    condition: ConditionDefinition;
    then: ActionDefinition[];
    else: ActionDefinition[];
}

export interface RunAutomationAction extends ActionBase {
    type: "runAutomation";
    automation?: string;
    skipConditions: boolean;
}

export interface EnableAutomationAction extends ActionBase {
    type: "enableAutomation";
    automation?: string;
    enable: boolean;
}

/** switches the automatic shading of blinds on (resumes) or off (pauses), picked one by one or by area/category */
export interface ShadingAction extends ActionBase {
    type: "shading";
    controls: string[];
    area?: string | null;
    includeSubAreas: boolean;
    category?: string | null;
    enable: boolean;
}

export interface StopAction extends ActionBase {
    type: "stop";
    reason?: string;
}

export type ActionDefinition = SetValueAction | ControlAction | GroupControlAction | DelayAction | WaitUntilAction | NotifyAction
    | IfAction | RunAutomationAction | EnableAutomationAction | ShadingAction | StopAction;
