import { ChangeDetectionStrategy, Component, OnInit } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { AutomationVisuBlockDto, AutomationVisuDto } from "src/app/base/model/automations/automation-dto";
import { SUN_EVENTS } from "src/app/base/model/automations/automation-definition";
import { AutomationsService } from "src/app/services/automations.service";
import { NotifyService } from "src/app/services/notify.service";
import { AutomationEditorContext } from "src/app/pages/automations/automation-editor-context";
import { options } from "src/app/pages/automations/automation-options";

const T = "UI.AUTOMATIONS.";

export type VisuFieldKind = "time" | "timeOptional" | "days" | "sunEvent" | "sunEventOptional" | "minutes" | "duration" | "durationOptional"
    | "value" | "percent" | "number" | "mailTo";

/** how a value is entered: by the type of the value the admin set */
export type VisuValueKind = "bool" | "number" | "text";

export interface VisuField {
    name: string;
    kind: VisuFieldKind;
    label: string;
    valueKind?: VisuValueKind;
}

export interface VisuBlock {
    dto: AutomationVisuBlockDto;
    title: string;
    detail: string;
    fields: VisuField[];
    /** the values being edited */
    values: { [field: string]: any };
    saved: string;
}

export interface VisuAutomation {
    dto: AutomationVisuDto;
    blocks: VisuBlock[];
    /** state of the on/off switch */
    on: boolean;
    saving: boolean;
    switching: boolean;
}

/**
 * Automations the admins released for the visualization: switch them on or off and change the times, days,
 * thresholds and values of the blocks the admin marked (webapi/automations-visu). The structure stays in the admin
 * editor.
 */
@Component({
    selector: "app-automations-visu",
    templateUrl: "./automations-visu.component.html",
    styleUrls: ["./automations-visu.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    providers: [AutomationEditorContext],
    standalone: false
})
export class AutomationsVisuComponent implements OnInit {

    automations: VisuAutomation[] = [];
    loaded = false;
    failed = false;

    readonly sunEvents = options("SUN", SUN_EVENTS);
    readonly sunEventsOptional = options<string>("SUN", ["", ...SUN_EVENTS], v => v ? v.toUpperCase() : "NONE");

    constructor(private service: AutomationsService,
        private notify: NotifyService,
        private translate: L10nTranslationService,
        public ctx: AutomationEditorContext) {
    }

    async ngOnInit() {
        await this.load();
    }

    async load() {
        try {
            const list = await this.service.getVisu();
            this.automations = list.map(a => this.toItem(a));
            this.failed = false;
        } catch (e) {
            console.error(e);
            this.failed = true;
        }
        this.loaded = true;
    }

    private t(key: string, params?: any): string {
        return this.translate.translate(T + key, params);
    }

    private toItem(dto: AutomationVisuDto): VisuAutomation {
        return {
            dto,
            blocks: (dto.blocks || []).map(b => this.toBlock(b)).filter(b => b.fields.length > 0),
            on: dto.isEnabled,
            saving: false,
            switching: false
        };
    }

    private toBlock(dto: AutomationVisuBlockDto): VisuBlock {
        const block = dto.block || {};
        const values: { [field: string]: any } = {};
        for (const f of dto.fields || []) {
            values[f] = block[f] === undefined ? null : JSON.parse(JSON.stringify(block[f]));
        }
        const fields = (dto.fields || [])
            .filter(f => AutomationsVisuComponent.isVisible(dto, f))
            .map(f => this.field(dto, f));
        return {
            dto,
            title: this.t(`TYPE.${dto.kind.toUpperCase()}.${dto.type.toUpperCase()}`),
            detail: this.detail(dto),
            fields,
            values,
            saved: JSON.stringify(values)
        };
    }

    /** fields that mean nothing with the structure the admin chose are not shown (e.g. the step of "set to a value") */
    static isVisible(dto: AutomationVisuBlockDto, field: string): boolean {
        const b = dto.block || {};
        switch (`${dto.kind}/${dto.type}/${field}`) {
            case "trigger/datapoint/value":
                return !!b.change && b.change !== "any";
            case "condition/datapoint/value2":
                return b.operator === "between";
            case "action/setValue/value":
                return (b.mode || "value") === "value";
            case "action/setValue/step":
                return b.mode === "increment";
            case "action/control/value":
                return b.command === "dim" || b.command === "position";
        }
        return true;
    }

    private field(dto: AutomationVisuBlockDto, name: string): VisuField {
        const sunCondition = dto.kind === "condition" && dto.type === "sun";
        const timeCondition = dto.kind === "condition" && dto.type === "time";
        let kind: VisuFieldKind;
        let label: string;
        switch (name) {
            case "at":
                kind = "time";
                label = "FIELD.AT";
                break;
            case "after":
            case "before":
                kind = sunCondition ? "sunEventOptional" : timeCondition ? "timeOptional" : "time";
                label = name === "after" ? "FIELD.AFTER" : "FIELD.BEFORE";
                break;
            case "days":
                kind = "days";
                label = "FIELD.DAYS";
                break;
            case "event":
                kind = "sunEvent";
                label = "FIELD.EVENT";
                break;
            case "offsetMinutes":
            case "afterOffsetMinutes":
            case "beforeOffsetMinutes":
                kind = "minutes";
                label = "FIELD.OFFSET";
                break;
            case "forSeconds":
                kind = "duration";
                label = "FIELD.FOR";
                break;
            case "timeoutSeconds":
                kind = "duration";
                label = "FIELD.TIMEOUT";
                break;
            case "seconds":
                kind = "duration";
                label = dto.block?.randomToSeconds ? "FIELD.RANDOM_FROM" : "FIELD.DURATION";
                break;
            case "randomToSeconds":
                kind = "durationOptional";
                label = "FIELD.RANDOM_TO";
                break;
            case "step":
                kind = "number";
                label = "FIELD.STEP";
                break;
            case "to":
                kind = "mailTo";
                label = "FIELD.MAIL_TO";
                break;
            case "value":
                if (dto.kind === "action" && dto.type === "control") {
                    kind = "percent";
                    label = "FIELD.PERCENT";
                } else {
                    kind = "value";
                    label = "FIELD.VALUE";
                }
                break;
            case "value2":
                kind = "value";
                label = "FIELD.VALUE2";
                break;
            default:
                kind = "number";
                label = void 0;
        }
        const field: VisuField = { name, kind, label: label ? this.t(label) : name };
        if (kind === "value") {
            const v = dto.block?.[name] ?? dto.block?.value;
            field.valueKind = typeof v === "boolean" ? "bool" : typeof v === "number" ? "number" : "text";
        }
        return field;
    }

    /** the read-only part of the block: the datapoint or control and what is compared or sent */
    private detail(dto: AutomationVisuBlockDto): string {
        const b = dto.block || {};
        const parts: string[] = [];
        if (dto.label) {
            parts.push(dto.label);
        }
        switch (`${dto.kind}/${dto.type}`) {
            case "trigger/datapoint":
                parts.push(this.t(`CHANGE.${(b.change || "any").toUpperCase()}`));
                break;
            case "condition/datapoint":
                parts.push(this.t(`OP.${(b.operator || "eq").toUpperCase()}`));
                break;
            case "action/setValue":
                parts.push(this.t(`SET_MODE.${(b.mode || "value").toUpperCase()}`));
                break;
            case "action/control":
                if (b.command) {
                    parts.push(this.t(`CMD.${b.command.toUpperCase()}`));
                }
                break;
        }
        return parts.join(" · ");
    }

    // ---------- editing ----------

    isDirty(item: VisuAutomation): boolean {
        return item.blocks.some(b => JSON.stringify(b.values) !== b.saved);
    }

    textOf(value: any): string {
        return value === null || value === undefined ? "" : String(value);
    }

    /** a value typed as text: true/false and numbers become those types */
    setText(block: VisuBlock, field: string, text: string) {
        const s = (text ?? "").trim();
        let value: any = s;
        if (s === "") {
            value = null;
        } else if (s.toLowerCase() === "true" || s.toLowerCase() === "false") {
            value = s.toLowerCase() === "true";
        } else if (!isNaN(Number(s.replace(",", ".")))) {
            value = Number(s.replace(",", "."));
        }
        block.values[field] = value;
    }

    mailText(block: VisuBlock): string {
        return (block.values.to || []).join(", ");
    }

    setMailText(block: VisuBlock, text: string) {
        block.values.to = (text || "").split(/[;,]/).map(a => a.trim()).filter(a => a.length > 0);
    }

    sunValue(block: VisuBlock, field: string): string {
        return block.values[field] || "";
    }

    setSun(block: VisuBlock, field: string, value: string, optional: boolean) {
        if (value) {
            block.values[field] = value;
        } else if (optional) {
            block.values[field] = null;
        }
    }

    reset(item: VisuAutomation) {
        for (const b of item.blocks) {
            b.values = JSON.parse(b.saved);
        }
    }

    async save(item: VisuAutomation) {
        const blocks = item.blocks
            .filter(b => JSON.stringify(b.values) !== b.saved)
            .map(b => ({ path: b.dto.path, values: b.values }));
        if (!blocks.length) {
            return;
        }
        item.saving = true;
        try {
            const dto = await this.service.saveVisu(item.dto.objId, { blocks });
            this.replace(item, dto);
            this.notify.notifySuccess(this.t("VISU.SAVED"));
        } catch (error) {
            this.notify.notifyError(this.t("VISU.SAVE_REFUSED", { detail: this.errorText(error) }), 8000);
        } finally {
            item.saving = false;
        }
    }

    async setEnabled(item: VisuAutomation, enabled: boolean) {
        if (enabled === item.dto.isEnabled || item.switching) {
            return;
        }
        item.switching = true;
        try {
            const dto = await this.service.setVisuEnabled(item.dto.objId, enabled);
            item.dto.isEnabled = dto.isEnabled;
            item.dto.lastRunAt = dto.lastRunAt;
            item.dto.lastRunState = dto.lastRunState;
            item.on = dto.isEnabled;
        } catch (error) {
            // back to the state on the server
            item.on = item.dto.isEnabled;
            this.notify.notifyError(error?.status === 400 ? this.t("VISU.ENABLE_PROBLEMS") : this.t("VISU.FAILED"), 8000);
        } finally {
            item.switching = false;
        }
    }

    private replace(item: VisuAutomation, dto: AutomationVisuDto) {
        const fresh = this.toItem(dto);
        item.dto = fresh.dto;
        item.blocks = fresh.blocks;
        item.on = fresh.on;
    }

    private errorText(error: any): string {
        const body = error?.error;
        if (Array.isArray(body?.problems) && body.problems.length) {
            return body.problems.map((p: any) => {
                const key = `${T}PROBLEM.${String(p.code || "unknown").toUpperCase()}`;
                const text = this.translate.translate(key);
                return text && text !== key ? text : p.code;
            }).join(", ");
        }
        if (Array.isArray(body?.errors) && body.errors.length) {
            return body.errors.join(", ");
        }
        return this.t("VISU.FAILED");
    }

    runState(item: VisuAutomation): string {
        return item.dto.lastRunState ? this.t(`RUN_STATE.${item.dto.lastRunState.toUpperCase()}`) : this.t("NEVER_RUN");
    }
}
