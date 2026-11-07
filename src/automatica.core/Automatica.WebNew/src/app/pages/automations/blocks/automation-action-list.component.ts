import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from "@angular/core";
import {
    ACTION_TYPES, ActionDefinition, ActionType, CONTROL_COMMANDS, ControlCommand, NOTIFY_SEVERITIES, SET_VALUE_MODES
} from "src/app/base/model/automations/automation-definition";
import { AutomationEditorContext } from "../automation-editor-context";
import { newAction, newGroup } from "../automation-blocks";
import { OptionItem, options } from "../automation-options";

/** actions in order, with the add palette; "if" actions contain two more lists */
@Component({
    selector: "automation-action-list",
    template: `
    @for (a of actions; track a; let i = $index; let first = $first; let last = $last) {
      <automation-action-card [action]="a" [path]="basePath + '/' + i" [depth]="depth" [first]="first" [last]="last"
        (moveUp)="move(i, -1)" (moveDown)="move(i, 1)" (remove)="removeAt(i)"></automation-action-card>
    } @empty {
      <p class="empty">{{ emptyText | translateAsync }}</p>
    }
    <automation-add-menu kind="action" [types]="types" [label]="'UI.AUTOMATIONS.ADD_ACTION' | translateAsync"
      (add)="add($event)"></automation-add-menu>`,
    styleUrls: ["./automation-forms.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AutomationActionListComponent {
    /** the list is changed in place */
    @Input() actions: ActionDefinition[] = [];
    @Input() basePath: string;
    @Input() depth = 0;
    @Input() emptyText = "UI.AUTOMATIONS.NO_ACTIONS";

    readonly types = ACTION_TYPES;

    constructor(public ctx: AutomationEditorContext) {
    }

    add(type: string) {
        const a = newAction(type as ActionType);
        this.actions.push(a);
        this.ctx.setExpanded(a, true);
    }

    move(i: number, delta: number) {
        const j = i + delta;
        if (j < 0 || j >= this.actions.length) {
            return;
        }
        const [a] = this.actions.splice(i, 1);
        this.actions.splice(j, 0, a);
    }

    removeAt(i: number) {
        this.actions.splice(i, 1);
    }
}

const SWITCH_COMMANDS: ControlCommand[] = ["on", "off", "toggle"];
const DIMMER_COMMANDS: ControlCommand[] = ["on", "off", "toggle", "dim"];
const BLIND_COMMANDS: ControlCommand[] = ["up", "down", "stop", "position"];

@Component({
    selector: "automation-action-card",
    templateUrl: "./automation-action-card.component.html",
    styleUrls: ["./automation-forms.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AutomationActionCardComponent {
    /** an ActionDefinition, any because the template reads the properties of every action type */
    @Input() action: any;
    @Input() path: string;
    @Input() depth = 0;
    @Input() first = false;
    @Input() last = false;
    @Output() moveUp = new EventEmitter<void>();
    @Output() moveDown = new EventEmitter<void>();
    @Output() remove = new EventEmitter<void>();

    readonly modeItems = options("SET_MODE", SET_VALUE_MODES);
    readonly allCommands = options("CMD", CONTROL_COMMANDS);
    readonly severityItems = options("SEVERITY", NOTIFY_SEVERITIES);
    readonly enableItems = options("ENABLE_OPT", [true, false], v => v ? "ON" : "OFF");
    readonly shadingEnableItems = options("SHADING_OPT", [true, false], v => v ? "ON" : "OFF");
    readonly placeholders = "{automation} {trigger} {trigger.value} {value:<id>}";

    private readonly commandsByType: { [type: string]: OptionItem<ControlCommand>[] } = {
        switch: options("CMD", SWITCH_COMMANDS),
        dimmer: options("CMD", DIMMER_COMMANDS),
        blind: options("CMD", BLIND_COMMANDS)
    };

    constructor(public ctx: AutomationEditorContext) {
    }

    get enabled(): boolean {
        return this.action?.enabled !== false;
    }

    setEnabled(on: boolean) {
        this.action.enabled = on;
    }

    get sub(): string {
        let text = this.ctx.describer.typeLabel("action", this.action?.type ?? "");
        if (this.action?.type === "control") {
            const control = this.ctx.lookups.control(this.action.control);
            if (control) {
                text += " · " + (this.ctx.describer.tOptional("CONTROL_TYPE." + control.type.toUpperCase()) ?? control.type);
            }
        }
        if (this.action?.continueOnError) {
            text += " · " + this.ctx.describer.t("CARD.CONTINUES_ON_ERROR");
        }
        return text;
    }

    /** the commands the chosen control supports */
    get controlCommands(): OptionItem<ControlCommand>[] {
        const type = this.ctx.lookups.control(this.action?.control)?.type;
        return this.commandsByType[type] ?? this.allCommands;
    }

    get needsPercent(): boolean {
        return this.action?.command === "dim" || this.action?.command === "position";
    }

    controlChanged(id: string) {
        this.action.control = id;
        const allowed = this.controlCommands.map(a => a.value);
        if (!allowed.includes(this.action.command)) {
            this.action.command = allowed[0];
        }
    }

    commandChanged(command: ControlCommand) {
        this.action.command = command;
        if (command === "dim" || command === "position") {
            this.action.value = this.action.value ?? 50;
        } else {
            delete this.action.value;
        }
    }

    modeChanged(mode: string) {
        this.action.mode = mode;
        if (mode !== "value") {
            delete this.action.value;
        }
        if (mode !== "copy") {
            delete this.action.source;
        }
    }

    get hasRandom(): boolean {
        return this.action.randomToSeconds !== undefined && this.action.randomToSeconds !== null;
    }

    setRandom(on: boolean) {
        if (on === this.hasRandom) {
            return;
        }
        if (on) {
            this.action.randomToSeconds = Math.max(this.action.seconds || 0, 0) * 2 || 60;
        } else {
            this.action.randomToSeconds = null;
        }
    }

    get emailText(): string {
        return (this.action?.email || []).join(", ");
    }

    setEmailText(text: string) {
        this.action.email = (text || "").split(/[,;\s]+/).map(a => a.trim()).filter(a => !!a);
    }

    get conditionIsGroup(): boolean {
        return this.action?.condition?.type === "group";
    }

    /** removing a single (non-group) condition leaves an empty group */
    clearCondition() {
        this.action.condition = newGroup("all");
    }
}
