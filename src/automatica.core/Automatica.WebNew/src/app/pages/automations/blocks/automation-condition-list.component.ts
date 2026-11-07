import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from "@angular/core";
import { CONDITION_TYPES, COMPARE_OPERATORS, ConditionGroup, ConditionType, GROUP_OPERATORS, SUN_EVENTS } from "src/app/base/model/automations/automation-definition";
import { AutomationEditorContext } from "../automation-editor-context";
import { newCondition } from "../automation-blocks";
import { options } from "../automation-options";

/** the conditions of a group with the add buttons, nested groups only on the first level */
@Component({
    selector: "automation-condition-list",
    template: `
    <div class="sub-list sc-if" [class.flat]="flat">
      @if (showOperator) {
        <div class="sub-head">
          <dx-select-box class="op-select" [items]="opItems" valueExpr="value" [displayExpr]="ctx.optionText"
            [(value)]="group.operator"></dx-select-box>
          <span class="hint">{{ 'UI.AUTOMATIONS.GROUP_HINT.' + group.operator.toUpperCase() | translateAsync }}</span>
        </div>
      }
      @for (c of group.conditions; track c; let i = $index) {
        <automation-condition-card [condition]="c" [path]="basePath + '/' + i" [depth]="depth"
          (remove)="removeAt(i)"></automation-condition-card>
      } @empty {
        <p class="empty">{{ emptyText | translateAsync }}</p>
      }
      <div class="add-row">
        <automation-add-menu kind="condition" [types]="types" [label]="'UI.AUTOMATIONS.ADD_CONDITION' | translateAsync"
          (add)="add($event)"></automation-add-menu>
        @if (depth === 0) {
          <span class="group-add">
            <button type="button" class="add-btn" (click)="add('group')">
              <fa-icon [icon]="['fas', 'layer-group']"></fa-icon>{{ 'UI.AUTOMATIONS.ADD_GROUP' | translateAsync }}
            </button>
          </span>
        }
      </div>
    </div>`,
    styleUrls: ["./automation-forms.scss", "../fields/automation-fields.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AutomationConditionListComponent {
    @Input() group: ConditionGroup;
    @Input() basePath: string;
    @Input() depth = 0;
    /** shows the all/any/none select of the group (the root group has it in the section header) */
    @Input() showOperator = false;
    @Input() emptyText = "UI.AUTOMATIONS.NO_CONDITIONS_IN_GROUP";
    /** without the left border, for the root group inside its section */
    @Input() flat = false;

    readonly types = CONDITION_TYPES;
    readonly opItems = options("GROUP", GROUP_OPERATORS);

    constructor(public ctx: AutomationEditorContext) {
    }

    add(type: string) {
        const c = newCondition(type as ConditionType);
        if (c.type === "trigger") {
            c.triggerId = this.ctx.triggerIds()[0];
        }
        this.group.conditions = [...(this.group.conditions || []), c];
        this.ctx.setExpanded(c, true);
    }

    removeAt(i: number) {
        this.group.conditions = this.group.conditions.filter((_, idx) => idx !== i);
    }
}

@Component({
    selector: "automation-condition-card",
    templateUrl: "./automation-condition-card.component.html",
    styleUrls: ["./automation-forms.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AutomationConditionCardComponent {
    /** a ConditionDefinition, any because the template reads the properties of every condition type */
    @Input() condition: any;
    @Input() path: string;
    @Input() depth = 0;
    @Output() remove = new EventEmitter<void>();

    readonly opItems = options("GROUP", GROUP_OPERATORS);
    readonly compareItems = options("OP", COMPARE_OPERATORS);
    readonly sunItems = options("SUN", SUN_EVENTS);
    readonly onItems = options("VALUE", [true, false], v => v ? "ON" : "OFF");
    readonly shadingItems = options("SHADING_STATE", [true, false], v => v ? "ON" : "OFF");

    constructor(public ctx: AutomationEditorContext) {
    }

    get sub(): string {
        if (this.condition?.type === "group") {
            return this.ctx.describer.t("GROUP_LABEL." + (this.condition.operator || "all").toUpperCase());
        }
        return this.ctx.describer.typeLabel("condition", this.condition?.type ?? "");
    }

    operatorChanged(op: string) {
        this.condition.operator = op;
        if (op !== "between") {
            delete this.condition.value2;
        }
    }

    setSun(which: "after" | "before", event: string | null) {
        this.condition[which] = event || null;
    }
}
