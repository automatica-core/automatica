import {
    ActionDefinition, AutomationDefinition, AutomationValue, ConditionDefinition, ControlCommand, DayOfWeekName, SunEvent, TriggerDefinition
} from "src/app/base/model/automations/automation-definition";
import { RunState, RunStep, ValidationProblem } from "src/app/base/model/automations/automation-dto";
import { AutomationLookups, ValueKind } from "./automation-lookups";
import { BlockKind, sortDays } from "./automation-blocks";

export type Translate = (key: string, params?: any) => string;

export const T = "UI.AUTOMATIONS.";

function esc(text: string): string {
    return String(text ?? "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;");
}

/**
 * Turns blocks into short sentences ("Motion hallway changes to On"). With html the names and values are bold
 * (escaped, for [innerHTML]), otherwise plain text.
 */
export class AutomationDescriber {

    constructor(private translate: Translate, private lookups: AutomationLookups, private language: () => string = () => "en") {
    }

    // ---------- dates ----------

    private fmt(options: Intl.DateTimeFormatOptions): Intl.DateTimeFormat {
        try {
            return new Intl.DateTimeFormat(this.language() || "en", options);
        } catch {
            return new Intl.DateTimeFormat("en", options);
        }
    }

    /** "21:48", "yesterday 16:16" or "12.10.2026 05:00" in local time */
    when(iso: string | null | undefined): string {
        if (!iso) {
            return "";
        }
        const d = new Date(iso);
        if (isNaN(d.getTime())) {
            return "";
        }
        const time = this.fmt({ hour: "2-digit", minute: "2-digit" }).format(d);
        const today = new Date();
        const startOfToday = new Date(today.getFullYear(), today.getMonth(), today.getDate()).getTime();
        const day = 24 * 3600 * 1000;
        if (d.getTime() >= startOfToday) {
            return this.t("TIME.TODAY", { time });
        }
        if (d.getTime() >= startOfToday - day) {
            return this.t("TIME.YESTERDAY", { time });
        }
        return this.fmt({ day: "2-digit", month: "2-digit", year: "numeric" }).format(d) + " " + time;
    }

    /** time of a step with seconds and milliseconds, "21:47:12.084" */
    clock(iso: string | null | undefined, millis = false): string {
        if (!iso) {
            return "";
        }
        const d = new Date(iso);
        if (isNaN(d.getTime())) {
            return "";
        }
        const pad = (n: number, l = 2) => String(n).padStart(l, "0");
        const text = `${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
        return millis ? `${text}.${pad(d.getMilliseconds(), 3)}` : text;
    }

    t(key: string, params?: any): string {
        return this.translate(T + key, params);
    }

    /** a translated text of a key below UI.AUTOMATIONS, or undefined if the key is missing */
    tOptional(key: string, params?: any): string | undefined {
        const text = this.translate(T + key, params);
        return text === T + key ? void 0 : text;
    }

    private s(key: string, params: { [name: string]: string }, html: boolean): string {
        const p: { [name: string]: string } = {};
        for (const k of Object.keys(params || {})) {
            p[k] = html ? `<b>${esc(params[k])}</b>` : params[k];
        }
        return this.t("S." + key, p);
    }

    // ---------- names and values ----------

    nodeLabel(id: string): string {
        if (!id) {
            return this.t("S.NO_NODE");
        }
        return this.lookups.nodeName(id) ?? this.t("S.UNKNOWN_NODE");
    }

    controlLabel(id: string): string {
        if (!id) {
            return this.t("S.NO_CONTROL");
        }
        return this.lookups.control(id)?.name ?? this.t("S.UNKNOWN_CONTROL");
    }

    automationLabel(id: string): string {
        if (!id) {
            return this.t("S.NO_RULE");
        }
        return this.lookups.automationName(id) ?? this.t("S.UNKNOWN_RULE");
    }

    areaLabel(id: string): string {
        return this.lookups.area(id)?.path ?? this.t("S.UNKNOWN_AREA");
    }

    categoryLabel(id: string): string {
        return this.lookups.categoryName(id) ?? this.t("S.UNKNOWN_CATEGORY");
    }

    value(value: any, kind: ValueKind = "unknown"): string {
        if (value === undefined || value === null || value === "") {
            return this.t("VALUE.UNSET");
        }
        if (typeof value === "boolean" || kind === "bool") {
            const b = value === true || value === "true" || value === 1;
            return this.t(b ? "VALUE.ON" : "VALUE.OFF");
        }
        if (kind === "windowState") {
            switch (+value) {
                case 0: return this.t("VALUE.OPEN");
                case 1: return this.t("VALUE.CLOSED");
                case 2: return this.t("VALUE.TILTED");
            }
        }
        if (typeof value === "number") {
            return String(value);
        }
        if (typeof value === "object") {
            return JSON.stringify(value);
        }
        return kind === "number" ? String(value) : `“${value}”`;
    }

    nodeValue(node: string, value: AutomationValue): string {
        return this.value(value, this.lookups.valueKind(node));
    }

    duration(seconds: number): string {
        const s = Math.max(0, Math.round(seconds || 0));
        const h = Math.floor(s / 3600);
        const m = Math.floor((s % 3600) / 60);
        const sec = s % 60;
        const parts: string[] = [];
        if (h) {
            parts.push(this.t("UNIT.H", { n: h }));
        }
        if (m) {
            parts.push(this.t("UNIT.MIN", { n: m }));
        }
        if (sec || parts.length === 0) {
            parts.push(this.t("UNIT.S", { n: sec }));
        }
        return parts.join(" ");
    }

    durationMs(ms: number): string {
        if (ms < 1000) {
            return this.t("UNIT.MS", { n: Math.round(ms) });
        }
        return this.duration(ms / 1000);
    }

    offset(minutes: number): string {
        const m = Math.round(minutes || 0);
        const sign = m < 0 ? "−" : "+";
        return sign + this.t("UNIT.MIN", { n: Math.abs(m) });
    }

    sunEvent(event: SunEvent | null | undefined): string {
        return event ? this.t("SUN." + event.toUpperCase()) : this.t("SUN.NONE");
    }

    sunWithOffset(event: SunEvent, offset: number): string {
        if (!offset) {
            return this.sunEvent(event);
        }
        return this.t("S.SUN_OFFSET", { event: this.sunEvent(event), offset: this.offset(offset) });
    }

    time(text: string): string {
        if (!text) {
            return this.t("S.NO_TIME");
        }
        // "07:00:00" → "07:00"
        return text.length > 5 && text.endsWith(":00") ? text.substring(0, 5) : text;
    }

    days(days: DayOfWeekName[]): string {
        const list = sortDays(days || []);
        if (list.length === 0 || list.length === 7) {
            return this.t("DAYS.EVERY_DAY");
        }
        const key = list.join(",");
        if (key === "monday,tuesday,wednesday,thursday,friday") {
            return this.t("DAYS.WEEKDAYS");
        }
        if (key === "saturday,sunday") {
            return this.t("DAYS.WEEKEND");
        }
        return list.map(d => this.t("DAY_SHORT." + d.toUpperCase())).join(", ");
    }

    private daysSuffix(days: DayOfWeekName[], html: boolean): string {
        if (!days || days.length === 0 || days.length === 7) {
            return "";
        }
        return " " + this.s("ON_DAYS", { days: this.days(days) }, html);
    }

    private join(parts: string[], word: "AND" | "OR" | "COMMA"): string {
        if (word === "COMMA") {
            return parts.join(", ");
        }
        return parts.join(` ${this.t("S." + word)} `);
    }

    // ---------- labels ----------

    typeLabel(kind: BlockKind, type: string): string {
        return this.t(`TYPE.${kind.toUpperCase()}.${type.toUpperCase()}`);
    }

    typeHint(kind: BlockKind, type: string): string {
        return this.tOptional(`TYPE.${kind.toUpperCase()}.${type.toUpperCase()}_HINT`) ?? "";
    }

    runState(state: RunState | null | undefined): string {
        return state ? this.t("RUN_STATE." + state.toUpperCase()) : "";
    }

    problem(p: ValidationProblem): string {
        const code = (p.code || "").toUpperCase();
        return this.tOptional("PROBLEM." + code) ?? this.t("PROBLEM.UNKNOWN", { code: p.code });
    }

    // ---------- blocks ----------

    trigger(trigger: TriggerDefinition, html = false): string {
        if (!trigger) {
            return "";
        }
        switch (trigger.type) {
            case "datapoint": {
                const node = this.nodeLabel(trigger.node);
                const value = this.nodeValue(trigger.node, trigger.value);
                let text: string;
                switch (trigger.change) {
                    case "to":
                        text = this.s("T_DP_TO", { node, value }, html);
                        break;
                    case "above":
                        text = this.s("T_DP_ABOVE", { node, value }, html);
                        break;
                    case "below":
                        text = this.s("T_DP_BELOW", { node, value }, html);
                        break;
                    default:
                        text = this.s("T_DP_ANY", { node }, html);
                }
                if (trigger.from !== undefined && trigger.from !== null) {
                    text += " " + this.s("FROM", { from: this.nodeValue(trigger.node, trigger.from) }, html);
                }
                if (trigger.forSeconds > 0) {
                    text += " " + this.s("FOR", { duration: this.duration(trigger.forSeconds) }, html);
                }
                return text;
            }
            case "time":
                return this.s("T_TIME", { time: this.time(trigger.at) }, html) + this.daysSuffix(trigger.days, html);
            case "sun": {
                const event = this.sunEvent(trigger.event);
                const off = trigger.offsetMinutes || 0;
                let text: string;
                if (off < 0) {
                    text = this.s("T_SUN_BEFORE", { event, offset: this.duration(-off * 60) }, html);
                } else if (off > 0) {
                    text = this.s("T_SUN_AFTER", { event, offset: this.duration(off * 60) }, html);
                } else {
                    text = this.s("T_SUN", { event }, html);
                }
                return text + this.daysSuffix(trigger.days, html);
            }
            case "conditions":
                return this.t("S.T_COND", { conditions: this.condition(trigger.conditions, html) });
            case "control": {
                const control = this.controlLabel(trigger.control);
                const key = trigger.to === "on" ? "T_CTRL_ON" : trigger.to === "off" ? "T_CTRL_OFF" : "T_CTRL_ANY";
                return this.s(key, { control }, html);
            }
            case "startup":
                return this.t("S.T_STARTUP");
            case "scene":
                // "die Szene ein Steuerelement" reads wrong, so no scene picked yet gets its own text
                return trigger.scene
                    ? this.s("T_SCENE", { scene: this.controlLabel(trigger.scene) }, html)
                    : this.t("S.T_SCENE_NONE");
        }
        return this.typeLabel("trigger", (trigger as any).type ?? "");
    }

    condition(condition: ConditionDefinition, html = false): string {
        if (!condition) {
            return this.t("S.C_GROUP_EMPTY");
        }
        switch (condition.type) {
            case "group": {
                const parts = (condition.conditions || []).map(c => {
                    const text = this.condition(c, html);
                    return c.type === "group" && (c.conditions?.length ?? 0) > 1 ? `(${text})` : text;
                });
                if (parts.length === 0) {
                    return condition.operator === "any" ? this.t("S.C_GROUP_NEVER") : this.t("S.C_GROUP_EMPTY");
                }
                if (condition.operator === "not") {
                    return this.t("S.C_GROUP_NOT", { list: this.join(parts, "OR") });
                }
                return this.join(parts, condition.operator === "any" ? "OR" : "AND");
            }
            case "datapoint": {
                const node = this.nodeLabel(condition.node);
                const value = this.nodeValue(condition.node, condition.value);
                const op = (condition.operator || "eq").toUpperCase();
                if (condition.operator === "between") {
                    return this.s("C_BETWEEN", { node, value, value2: this.nodeValue(condition.node, condition.value2) }, html);
                }
                return this.s("C_" + op, { node, value }, html);
            }
            case "time": {
                let text: string;
                if (condition.after && condition.before) {
                    text = this.s("C_TIME_BETWEEN", { after: this.time(condition.after), before: this.time(condition.before) }, html);
                } else if (condition.after) {
                    text = this.s("C_TIME_AFTER", { after: this.time(condition.after) }, html);
                } else if (condition.before) {
                    text = this.s("C_TIME_BEFORE", { before: this.time(condition.before) }, html);
                } else if (condition.days?.length) {
                    return this.s("C_DAYS", { days: this.days(condition.days) }, html);
                } else {
                    return this.t("S.C_TIME_ANY");
                }
                return text + this.daysSuffix(condition.days, html);
            }
            case "sun": {
                const after = condition.after ? this.sunWithOffset(condition.after, condition.afterOffsetMinutes) : void 0;
                const before = condition.before ? this.sunWithOffset(condition.before, condition.beforeOffsetMinutes) : void 0;
                if (after && before) {
                    return this.s("C_SUN_BETWEEN", { after, before }, html);
                }
                if (after) {
                    return this.s("C_SUN_AFTER", { after }, html);
                }
                if (before) {
                    return this.s("C_SUN_BEFORE", { before }, html);
                }
                return this.t("S.C_SUN_NONE");
            }
            case "control":
                return this.s(condition.on ? "C_CTRL_ON" : "C_CTRL_OFF", { control: this.controlLabel(condition.control) }, html);
            case "trigger":
                return this.s("C_TRIGGER", { id: condition.triggerId || this.t("VALUE.UNSET") }, html);
            case "shading":
                return this.s(condition.shading ? "C_SHADING_ON" : "C_SHADING_OFF", { control: this.controlLabel(condition.control) }, html);
        }
        return this.typeLabel("condition", (condition as any).type ?? "");
    }

    /** the target text of an area and/or category */
    private groupTarget(area: string | null | undefined, category: string | null | undefined): string {
        if (area && category) {
            return this.t("S.G_AREA_CAT", { area: this.areaLabel(area), category: this.categoryLabel(category) });
        }
        if (area) {
            return this.t("S.G_AREA", { area: this.areaLabel(area) });
        }
        if (category) {
            return this.t("S.G_CAT", { category: this.categoryLabel(category) });
        }
        return this.t("S.G_NONE");
    }

    controlCommand(command: ControlCommand, control: string, value: number | null | undefined, html: boolean): string {
        const percent = this.t("UNIT.PERCENT", { n: value ?? 0 });
        return this.s("A_CTRL_" + (command || "on").toUpperCase(), { control, value: percent }, html);
    }

    action(action: ActionDefinition, html = false): string {
        if (!action) {
            return "";
        }
        switch (action.type) {
            case "setValue": {
                const node = this.nodeLabel(action.node);
                switch (action.mode) {
                    case "toggle":
                        return this.s("A_TOGGLE", { node }, html);
                    case "increment": {
                        const step = action.step ?? 1;
                        return this.s("A_INCREMENT", { node, step: (step > 0 ? "+" : "") + step }, html);
                    }
                    case "copy":
                        return this.s("A_COPY", { node, source: this.nodeLabel(action.source) }, html);
                    default:
                        return this.s("A_SET", { node, value: this.nodeValue(action.node, action.value) }, html);
                }
            }
            case "control":
                return this.controlCommand(action.command, this.controlLabel(action.control), action.value, html);
            case "groupControl":
                return this.controlCommand(action.command, this.groupTarget(action.area, action.category), action.value, html);
            case "shading": {
                const targets = (action.controls || []).map(c => this.controlLabel(c));
                if (action.area || action.category || !targets.length) {
                    targets.push(this.groupTarget(action.area, action.category));
                }
                return this.s(action.enable ? "A_SHADING_ON" : "A_SHADING_OFF", { blinds: targets.join(", ") }, html);
            }
            case "delay":
                if (action.randomToSeconds !== undefined && action.randomToSeconds !== null && action.randomToSeconds > action.seconds) {
                    return this.s("A_DELAY_RANDOM", { from: this.duration(action.seconds), to: this.duration(action.randomToSeconds) }, html);
                }
                return this.s("A_DELAY", { duration: this.duration(action.seconds) }, html);
            case "waitUntil": {
                let text = this.t("S.A_WAIT", { condition: this.condition(action.condition, html) });
                if (action.forSeconds > 0) {
                    text += " " + this.s("FOR", { duration: this.duration(action.forSeconds) }, html);
                }
                if (action.timeoutSeconds > 0) {
                    text += " " + this.s(action.continueOnTimeout ? "TIMEOUT_CONTINUE" : "TIMEOUT_STOP",
                        { duration: this.duration(action.timeoutSeconds) }, html);
                }
                return text;
            }
            case "notify": {
                let text = this.s("A_NOTIFY", { subject: action.subject || this.t("VALUE.UNSET") }, html);
                if (action.email?.length) {
                    text += " " + this.s("A_NOTIFY_EMAIL", { to: action.email.join(", ") }, html);
                }
                return text;
            }
            case "mail":
                return this.s("A_MAIL", {
                    subject: action.subject || this.t("VALUE.UNSET"),
                    to: action.to?.length ? action.to.join(", ") : this.t("VALUE.UNSET")
                }, html);
            case "if":
                return this.t("S.A_IF", { condition: this.condition(action.condition, html) });
            case "runAutomation": {
                const text = this.s("A_RUN", { automation: this.automationLabel(action.automation) }, html);
                return action.skipConditions ? text + " " + this.t("S.SKIP_COND") : text;
            }
            case "enableAutomation":
                return this.s(action.enable ? "A_ENABLE" : "A_DISABLE", { automation: this.automationLabel(action.automation) }, html);
            case "stop":
                return action.reason ? this.s("A_STOP_REASON", { reason: action.reason }, html) : this.t("S.A_STOP");
        }
        return this.typeLabel("action", (action as any).type ?? "");
    }

    /** an action with its nested lists, for the rule sentence */
    private actionDeep(action: ActionDefinition, html: boolean): string {
        if (action.type !== "if") {
            return this.action(action, html);
        }
        let text = this.t("S.A_IF_FULL", {
            condition: this.condition(action.condition, html),
            then: this.actions(action.then, html)
        });
        if (action.else?.length) {
            text += ", " + this.t("S.A_IF_ELSE", { else: this.actions(action.else, html) });
        }
        return text;
    }

    actions(actions: ActionDefinition[], html = false): string {
        const list = (actions || []).filter(a => a.enabled !== false).map(a => this.actionDeep(a, html));
        return list.length ? this.join(list, "COMMA") : this.t("S.NOTHING");
    }

    rule(def: AutomationDefinition, html = false): string {
        if (!def) {
            return "";
        }
        const when = def.triggers?.length
            ? this.join(def.triggers.map(a => this.trigger(a, html)), "OR")
            : this.t("S.MANUAL");
        const then = this.actions(def.actions, html);
        if (!def.conditions?.conditions?.length) {
            return this.t("S.RULE_NO_IF", { when, then });
        }
        return this.t("S.RULE", { when, if: this.condition(def.conditions, html), then });
    }

    block(kind: BlockKind, block: any, html = false): string {
        switch (kind) {
            case "trigger":
                return this.trigger(block, html);
            case "condition":
                return this.condition(block, html);
            case "action":
                return this.action(block, html);
        }
    }

    /** the actual value of a run or test step, formatted for the datapoint of the block */
    stepValue(step: RunStep, block: any, raw: any): string {
        const node = block?.node;
        return node ? this.nodeValue(node, raw) : this.value(raw);
    }
}
