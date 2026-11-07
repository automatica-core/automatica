import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from "@angular/core";
import { CONTROL_STATE_CHANGES, DATAPOINT_CHANGES, SUN_EVENTS } from "src/app/base/model/automations/automation-definition";
import { AutomationEditorContext } from "../automation-editor-context";
import { options } from "../automation-options";

@Component({
    selector: "automation-trigger-card",
    templateUrl: "./automation-trigger-card.component.html",
    styleUrls: ["./automation-forms.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AutomationTriggerCardComponent {
    /** a TriggerDefinition, any because the template reads the properties of every trigger type */
    @Input() trigger: any;
    @Input() path: string;
    @Output() remove = new EventEmitter<void>();

    readonly changeItems = options("CHANGE", DATAPOINT_CHANGES);
    readonly controlToItems = options("CONTROL_TO", CONTROL_STATE_CHANGES);
    readonly sunItems = options("SUN", SUN_EVENTS);

    constructor(public ctx: AutomationEditorContext) {
    }

    get sub(): string {
        const type = this.ctx.describer.typeLabel("trigger", this.trigger?.type ?? "");
        return this.trigger?.id ? `${type} · ${this.trigger.id}` : type;
    }

    get hasFrom(): boolean {
        return this.trigger.from !== undefined && this.trigger.from !== null;
    }

    setHasFrom(on: boolean) {
        if (on === this.hasFrom) {
            return;
        }
        if (on) {
            const kind = this.ctx.lookups.valueKind(this.trigger.node);
            this.trigger.from = kind === "bool" ? false : kind === "number" || kind === "windowState" ? 0 : "";
        } else {
            delete this.trigger.from;
        }
    }

    changeChanged(change: string) {
        this.trigger.change = change;
        if (change === "any") {
            delete this.trigger.value;
        }
    }
}
