import { AutomationDefinition } from "./automation-definition";

// DTOs of webapi/automations, see Automatica.Core.Automation/README.md. Dates are UTC ISO strings.

export type AutomationMode = "single" | "restart" | "queued" | "parallel";
export const AUTOMATION_MODES: AutomationMode[] = ["single", "restart", "queued", "parallel"];

export type RunState = "running" | "waiting" | "done" | "conditionsNotMet" | "failed" | "stopped";

export type ProblemSeverity = "error" | "warning";

export type ValidationCode = "noTriggers" | "noActions" | "nodeNotFound" | "nodeNotReadable" | "nodeNotWritable" | "controlNotFound"
    | "controlNotSupported" | "invalidTime" | "valueMissing" | "automationNotFound" | "areaNotFound" | "categoryNotFound"
    | "groupTargetMissing" | "negativeDuration" | "locationMissing" | "selfReference";

export interface ValidationProblem {
    /** into the definition, e.g. "triggers/0" or "actions/2/then/0"; empty = the whole rule */
    path: string;
    code: ValidationCode | string;
    severity: ProblemSeverity;
    detail?: string;
}

export interface AutomationDto {
    objId: string;
    name: string;
    description: string;
    isEnabled: boolean;
    mode: AutomationMode;
    maxRuns: number;
    this2AreaInstance: string | null;
    this2CategoryInstance: string | null;
    definition: AutomationDefinition;
    createdAt: string;
    modifiedAt: string;
    lastRunAt: string | null;
    lastRunState: RunState | null;
    lastError: string | null;
    activeRuns: number;
    problems: ValidationProblem[];
}

export type AutomationSaveDto = Pick<AutomationDto, "name" | "description" | "isEnabled" | "mode" | "maxRuns" | "this2AreaInstance"
    | "this2CategoryInstance" | "definition">;

export type RunStepKind = "trigger" | "condition" | "action" | "info" | "error";
export type RunStepState = "running" | "waiting" | "done" | "failed" | "skipped" | "timeout";

export interface RunStep {
    /** UTC */
    at: string;
    kind: RunStepKind;
    /** into the definition, see ValidationProblem.path */
    path: string;
    /** conditions */
    passed?: boolean;
    /** actual value (trigger value, compared value, written value) */
    value?: any;
    /** trigger: previous value */
    previous?: any;
    /** actions */
    durationMs?: number;
    state?: RunStepState;
    /** English detail, e.g. an error */
    message?: string;
}

export interface AutomationRunDto {
    objId: string;
    startedAt: string;
    finishedAt: string | null;
    state: RunState;
    triggerId: string | null;
    error: string | null;
    steps: RunStep[];
}

export interface TestResult {
    passed: boolean;
    steps: RunStep[];
}

export interface RunStartedDto {
    runId: string;
}

/** a block of a released automation, webapi/automations-visu */
export interface AutomationVisuBlockDto {
    path: string;
    kind: "trigger" | "condition" | "action";
    type: string;
    /** name of the datapoint or control, null for time and sun blocks */
    label: string | null;
    /** the fields users may change */
    fields: string[];
    /** the whole block (camelCase definition JSON) */
    block: any;
}

/** an automation released for the visualization */
export interface AutomationVisuDto {
    objId: string;
    name: string;
    description: string | null;
    isEnabled: boolean;
    this2AreaInstance: string | null;
    this2CategoryInstance: string | null;
    lastRunAt: string | null;
    lastRunState: RunState | null;
    /** the user may change it (visu and admin roles) */
    canEdit: boolean;
    blocks: AutomationVisuBlockDto[];
}

export interface AutomationVisuSaveDto {
    blocks: { path: string; values: { [field: string]: any } }[];
}
