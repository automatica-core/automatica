import {
    ActionDefinition, ActionType, AutomationDefinition, ConditionDefinition, ConditionGroup, ConditionType, DayOfWeekName, TriggerDefinition, TriggerType
} from "src/app/base/model/automations/automation-definition";

// Factories, icons and path handling for the blocks of a definition. Paths: see Automatica.Core.Automation/README.md.

export type BlockKind = "trigger" | "condition" | "action";
export type Section = "when" | "if" | "then";

export function emptyDefinition(): AutomationDefinition {
    return { schema: 1, triggers: [], conditions: newGroup("all"), actions: [] };
}

export function newGroup(operator: "all" | "any" | "not" = "all"): ConditionGroup {
    return { type: "group", operator, conditions: [] };
}

/** a free id like "trigger1" that no other trigger uses */
export function nextTriggerId(triggers: TriggerDefinition[]): string {
    const used = new Set(triggers.map(a => a.id));
    let i = triggers.length + 1;
    while (used.has(`trigger${i}`)) {
        i++;
    }
    return `trigger${i}`;
}

export function newTrigger(type: TriggerType, existing: TriggerDefinition[]): TriggerDefinition {
    const id = nextTriggerId(existing);
    switch (type) {
        case "datapoint":
            return { type, id, change: "to", value: true, forSeconds: 0 };
        case "time":
            return { type, id, at: "07:00", days: [] };
        case "sun":
            return { type, id, event: "sunset", offsetMinutes: 0, days: [] };
        case "conditions":
            return { type, id, conditions: newGroup("all") };
        case "control":
            return { type, id, to: "on" };
        case "startup":
            return { type, id };
    }
}

export function newCondition(type: ConditionType): ConditionDefinition {
    switch (type) {
        case "group":
            return newGroup("any");
        case "datapoint":
            return { type, operator: "eq" };
        case "time":
            return { type, after: "22:00", before: "06:00", days: [] };
        case "sun":
            return { type, after: "sunset", afterOffsetMinutes: 0, before: "sunrise", beforeOffsetMinutes: 0 };
        case "control":
            return { type, on: true };
        case "trigger":
            return { type };
        case "shading":
            return { type, shading: true };
    }
}

export function newAction(type: ActionType): ActionDefinition {
    const base = { enabled: true, continueOnError: false };
    switch (type) {
        case "setValue":
            return { ...base, type, mode: "value", step: 1 };
        case "control":
            return { ...base, type, command: "on" };
        case "groupControl":
            return { ...base, type, includeSubAreas: true, command: "off" };
        case "delay":
            return { ...base, type, seconds: 60 };
        case "waitUntil":
            return { ...base, type, condition: newGroup("all"), forSeconds: 0, timeoutSeconds: 0, continueOnTimeout: true };
        case "notify":
            return { ...base, type, subject: "", body: "", severity: "info", email: [] };
        case "mail":
            return { ...base, type, to: [], subject: "", body: "" };
        case "if":
            return { ...base, type, condition: newGroup("all"), then: [], else: [] };
        case "runAutomation":
            return { ...base, type, skipConditions: false };
        case "enableAutomation":
            return { ...base, type, enable: true };
        case "shading":
            return { ...base, type, controls: [], includeSubAreas: true, enable: true };
        case "stop":
            return { ...base, type, reason: "" };
    }
}

const TRIGGER_ICONS: Record<TriggerType, string> = {
    datapoint: "bolt",
    time: "clock",
    sun: "sun",
    conditions: "circle-check",
    control: "toggle-on",
    startup: "power-off"
};

const CONDITION_ICONS: Record<ConditionType, string> = {
    group: "layer-group",
    datapoint: "gauge",
    time: "clock",
    sun: "sun",
    control: "toggle-on",
    trigger: "bolt",
    shading: "sun"
};

const ACTION_ICONS: Record<ActionType, string> = {
    setValue: "pen",
    control: "lightbulb",
    groupControl: "layer-group",
    delay: "hourglass-half",
    waitUntil: "stopwatch",
    notify: "bell",
    mail: "envelope",
    if: "code-branch",
    runAutomation: "play",
    enableAutomation: "power-off",
    shading: "sun",
    stop: "hand"
};

export function blockIcon(kind: BlockKind, type: string): string {
    switch (kind) {
        case "trigger":
            return TRIGGER_ICONS[type] ?? "bolt";
        case "condition":
            return CONDITION_ICONS[type] ?? "gauge";
        case "action":
            return ACTION_ICONS[type] ?? "play";
    }
}

export function sectionOf(kind: BlockKind): Section {
    return kind === "trigger" ? "when" : kind === "condition" ? "if" : "then";
}

export interface ResolvedBlock {
    kind: BlockKind;
    block: any;
}

/**
 * Resolves a run step or problem path ("triggers/0", "conditions/1/0", "actions/2/then/0", "actions/3/condition",
 * "triggers/0/conditions/1") into the block it points at. undefined if the path does not exist in the definition.
 */
export function resolvePath(def: AutomationDefinition, path: string): ResolvedBlock | undefined {
    if (!def || !path) {
        return void 0;
    }
    const segs = path.split("/").filter(a => a !== "");
    let cur: any;
    let kind: BlockKind;
    let i: number;

    switch (segs[0]) {
        case "triggers":
            cur = def.triggers?.[+segs[1]];
            kind = "trigger";
            i = 2;
            break;
        case "conditions":
            cur = def.conditions;
            kind = "condition";
            i = 1;
            break;
        case "actions":
            cur = def.actions?.[+segs[1]];
            kind = "action";
            i = 2;
            break;
        default:
            return void 0;
    }

    while (cur && i < segs.length) {
        const s = segs[i];
        if (s === "conditions" && kind === "trigger") {
            cur = cur.conditions;
            kind = "condition";
            i++;
        } else if (s === "condition" && kind === "action") {
            cur = cur.condition;
            kind = "condition";
            i++;
        } else if ((s === "then" || s === "else") && kind === "action") {
            cur = cur[s]?.[+segs[i + 1]];
            i += 2;
        } else if (/^\d+$/.test(s) && kind === "condition" && cur.type === "group") {
            cur = cur.conditions?.[+s];
            i++;
        } else {
            return void 0;
        }
    }
    return cur ? { kind, block: cur } : void 0;
}

/** the longest prefix of the path that points at a block, "" for the whole rule */
export function ownerPath(def: AutomationDefinition, path: string): string {
    const segs = (path || "").split("/").filter(a => a !== "");
    while (segs.length > 0) {
        const p = segs.join("/");
        if (resolvePath(def, p)) {
            return p;
        }
        segs.pop();
    }
    return "";
}

export function isUnder(path: string, parent: string): boolean {
    return path === parent || path.startsWith(parent + "/");
}

/** DOM id of the card of a path */
export function blockDomId(path: string): string {
    return "automation-block-" + path.replace(/\//g, "-");
}

export function sortDays(days: DayOfWeekName[]): DayOfWeekName[] {
    const order: DayOfWeekName[] = ["monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"];
    return [...days].sort((a, b) => order.indexOf(a) - order.indexOf(b));
}

/** fills lists and groups the editor needs but an older or hand-written document may lack (in place) */
export function normalizeDefinition(def: AutomationDefinition): AutomationDefinition {
    const group = (g: any): ConditionGroup => {
        if (!g) {
            return newGroup("all");
        }
        if (!g.type && Array.isArray(g.conditions)) {
            // a property typed ConditionGroup may come without the discriminator
            g.type = "group";
        }
        if (g.type === "group") {
            g.operator = g.operator || "all";
            g.conditions = (g.conditions || []).map(c => normalizeCondition(c));
        }
        return g;
    };
    const normalizeCondition = (c: any): any => {
        if (!c) {
            return c;
        }
        if (c.type === "group") {
            return group(c);
        }
        if (c.type === "time" && !c.days) {
            c.days = [];
        }
        return c;
    };
    const actions = (list: any[]): ActionDefinition[] => (list || []).map(a => {
        if (a.type === "waitUntil" || a.type === "if") {
            a.condition = a.condition ? normalizeCondition(a.condition) : newGroup("all");
        }
        if (a.type === "if") {
            a.then = actions(a.then);
            a.else = actions(a.else);
        }
        if (a.type === "notify" && !a.email) {
            a.email = [];
        }
        if (a.type === "mail" && !a.to) {
            a.to = [];
        }
        return a;
    });

    def.schema = def.schema ?? 1;
    def.triggers = (def.triggers || []).map((t: any) => {
        if ((t.type === "time" || t.type === "sun") && !t.days) {
            t.days = [];
        }
        if (t.type === "conditions") {
            t.conditions = group(t.conditions);
        }
        return t;
    });
    def.conditions = group(def.conditions);
    def.actions = actions(def.actions);
    return def;
}

/** a copy of the definition without the UI-only parts, ready to send */
export function cleanDefinition(def: AutomationDefinition): AutomationDefinition {
    return JSON.parse(JSON.stringify(def));
}
