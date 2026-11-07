import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from "@angular/core";
import { AutomationValue, DAYS_OF_WEEK, DayOfWeekName } from "src/app/base/model/automations/automation-definition";
import { AutomationEditorContext, NodePickMode } from "../automation-editor-context";
import { BlockKind, blockIcon } from "../automation-blocks";
import { options } from "../automation-options";
import { ValueKind } from "../automation-lookups";

/** shows the chosen datapoint and opens the datapoint picker */
@Component({
    selector: "automation-node-field",
    template: `
    <button type="button" class="pick" [class.missing]="!!node && !name" (click)="pick()"
      [attr.aria-label]="'UI.AUTOMATIONS.FIELD.DATAPOINT' | translateAsync">
      <fa-icon [icon]="['fas', 'circle-nodes']"></fa-icon>
      <span class="txt">
        @if (name) {
          <span class="nm">{{ name }}</span>
          @if (path) { <span class="path">{{ path }}</span> }
        } @else if (node) {
          <span class="nm">{{ 'UI.AUTOMATIONS.S.UNKNOWN_NODE' | translateAsync }}</span>
          <span class="path mono">{{ node }}</span>
        } @else {
          <span class="nm placeholder">{{ 'UI.AUTOMATIONS.FIELD.PICK_DATAPOINT' | translateAsync }}</span>
        }
      </span>
      <fa-icon [icon]="['fas', 'ellipsis']"></fa-icon>
    </button>`,
    styleUrls: ["./automation-fields.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AutomationNodeFieldComponent {
    @Input() node: string;
    @Input() mode: NodePickMode = "read";
    @Output() nodeChange = new EventEmitter<string>();

    constructor(private ctx: AutomationEditorContext) {
    }

    get name(): string | undefined {
        return this.ctx.lookups.nodeName(this.node);
    }

    get path(): string {
        return this.ctx.lookups.nodePath(this.node);
    }

    async pick() {
        const id = await this.ctx.pickNode(this.mode, this.node);
        if (id && id !== this.node) {
            this.node = id;
            this.nodeChange.emit(id);
        }
    }
}

/** a value that fits the data type of the datapoint (bool, number, window state or text) */
@Component({
    selector: "automation-value-input",
    template: `
    @switch (kind) {
      @case ('bool') {
        <dx-select-box [items]="boolItems" valueExpr="value" [displayExpr]="ctx.optionText" [value]="boolValue"
          (onValueChanged)="set($event.value)"></dx-select-box>
      }
      @case ('windowState') {
        <dx-select-box [items]="windowItems" valueExpr="value" [displayExpr]="ctx.optionText" [value]="numberValue"
          (onValueChanged)="set($event.value)"></dx-select-box>
      }
      @case ('number') {
        <dx-number-box [value]="numberValue" [showSpinButtons]="true" (onValueChanged)="set($event.value)"></dx-number-box>
      }
      @case ('string') {
        <dx-text-box [value]="textValue" valueChangeEvent="input" (onValueChanged)="set($event.value)"></dx-text-box>
      }
      @default {
        <dx-text-box [value]="textValue" valueChangeEvent="change" (onValueChanged)="setParsed($event.value)"
          [placeholder]="'UI.AUTOMATIONS.FIELD.VALUE_ANY' | translateAsync"></dx-text-box>
      }
    }`,
    styleUrls: ["./automation-fields.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AutomationValueInputComponent {
    @Input() node: string;
    @Input() value: AutomationValue;
    @Output() valueChange = new EventEmitter<AutomationValue>();

    readonly boolItems = options("VALUE", [true, false], v => v ? "ON" : "OFF");
    readonly windowItems = options("VALUE", [0, 1, 2], v => ["OPEN", "CLOSED", "TILTED"][v]);

    constructor(public ctx: AutomationEditorContext) {
    }

    get kind(): ValueKind {
        return this.ctx.lookups.valueKind(this.node);
    }

    get boolValue(): boolean | undefined {
        if (this.value === undefined || this.value === null) {
            return void 0;
        }
        return this.value === true || this.value === "true" || this.value === 1;
    }

    get numberValue(): number | undefined {
        if (this.value === undefined || this.value === null || this.value === "") {
            return void 0;
        }
        const n = typeof this.value === "number" ? this.value : Number(this.value);
        return isNaN(n) ? void 0 : n;
    }

    get textValue(): string {
        return this.value === undefined || this.value === null ? "" : String(this.value);
    }

    set(v: AutomationValue) {
        if (v === this.value) {
            return;
        }
        this.value = v;
        this.valueChange.emit(v);
    }

    /** text of a datapoint without a known type: true/false and numbers keep their JSON type */
    setParsed(text: string) {
        const t = (text ?? "").trim();
        let v: AutomationValue;
        if (t === "") {
            v = null;
        } else if (t.toLowerCase() === "true") {
            v = true;
        } else if (t.toLowerCase() === "false") {
            v = false;
        } else if (!isNaN(Number(t))) {
            v = Number(t);
        } else {
            v = text;
        }
        this.set(v);
    }
}

/** weekday toggles, none selected = every day */
@Component({
    selector: "automation-days-picker",
    template: `
    <div class="days" role="group" [attr.aria-label]="'UI.AUTOMATIONS.FIELD.DAYS' | translateAsync">
      @for (d of allDays; track d) {
        <button type="button" [class.on]="has(d)" [attr.aria-pressed]="has(d)" (click)="toggle(d)"
          [title]="'UI.AUTOMATIONS.DAY.' + d.toUpperCase() | translateAsync">
          {{ 'UI.AUTOMATIONS.DAY_SHORT.' + d.toUpperCase() | translateAsync }}
        </button>
      }
    </div>
    @if (!days?.length) {
      <span class="days-hint">{{ 'UI.AUTOMATIONS.DAYS.EVERY_DAY' | translateAsync }}</span>
    }`,
    styleUrls: ["./automation-fields.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AutomationDaysPickerComponent {
    @Input() days: DayOfWeekName[] = [];
    @Output() daysChange = new EventEmitter<DayOfWeekName[]>();

    readonly allDays = DAYS_OF_WEEK;

    has(d: DayOfWeekName): boolean {
        return (this.days || []).includes(d);
    }

    toggle(d: DayOfWeekName) {
        const list = this.has(d) ? this.days.filter(a => a !== d) : [...(this.days || []), d];
        this.days = DAYS_OF_WEEK.filter(a => list.includes(a));
        this.daysChange.emit(this.days);
    }
}

/** local time of day as "HH:mm" */
@Component({
    selector: "automation-time-input",
    template: `
    <dx-date-box type="time" displayFormat="HH:mm" [value]="date" [showClearButton]="clearable"
      [useMaskBehavior]="true" (onValueChanged)="changed($event.value)"></dx-date-box>`,
    styleUrls: ["./automation-fields.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AutomationTimeInputComponent implements OnChanges {
    @Input() value: string;
    @Input() clearable = false;
    @Output() valueChange = new EventEmitter<string>();

    date: Date | null = null;

    ngOnChanges(changes: SimpleChanges) {
        if (changes.value && AutomationTimeInputComponent.format(this.date) !== this.value) {
            this.date = AutomationTimeInputComponent.parse(this.value);
        }
    }

    changed(v: Date | null) {
        const text = AutomationTimeInputComponent.format(v);
        this.date = v;
        if (text !== this.value) {
            this.value = text;
            this.valueChange.emit(text);
        }
    }

    static parse(text: string): Date | null {
        const m = /^(\d{1,2}):(\d{2})(?::(\d{2}))?$/.exec(text || "");
        if (!m) {
            return null;
        }
        const d = new Date(2000, 0, 1, +m[1], +m[2], m[3] ? +m[3] : 0);
        return d;
    }

    static format(d: Date | null): string | undefined {
        if (!d || isNaN(d.getTime())) {
            return void 0;
        }
        const pad = (n: number) => String(n).padStart(2, "0");
        const base = `${pad(d.getHours())}:${pad(d.getMinutes())}`;
        return d.getSeconds() ? `${base}:${pad(d.getSeconds())}` : base;
    }
}

type DurationUnit = 1 | 60 | 3600;

/** a duration in seconds, entered in seconds, minutes or hours */
@Component({
    selector: "automation-duration-input",
    template: `
    <div class="duration">
      <dx-number-box [value]="amount" [min]="0" [showSpinButtons]="true" [showClearButton]="nullable"
        (onValueChanged)="amountChanged($event.value)"></dx-number-box>
      <dx-select-box [items]="units" valueExpr="value" [displayExpr]="ctx.optionText" [value]="unit"
        (onValueChanged)="unitChanged($event.value)"></dx-select-box>
    </div>`,
    styleUrls: ["./automation-fields.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AutomationDurationInputComponent implements OnChanges {
    @Input() seconds: number | null;
    /** an empty box means null (otherwise 0) */
    @Input() nullable = false;
    @Output() secondsChange = new EventEmitter<number | null>();

    readonly units = options<DurationUnit>("UNIT_NAME", [1, 60, 3600], v => v === 1 ? "S" : v === 60 ? "MIN" : "H");

    unit: DurationUnit = 1;
    amount: number | null = 0;

    constructor(public ctx: AutomationEditorContext) {
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.seconds && this.current() !== this.seconds) {
            const s = this.seconds;
            if (s === null || s === undefined) {
                this.amount = null;
            } else {
                this.unit = s !== 0 && s % 3600 === 0 ? 3600 : s !== 0 && s % 60 === 0 ? 60 : (s === 0 ? this.unit : 1);
                this.amount = s / this.unit;
            }
        }
    }

    private current(): number | null {
        if (this.amount === null || this.amount === undefined) {
            return this.nullable ? null : 0;
        }
        return Math.round(this.amount * this.unit);
    }

    amountChanged(v: number | null) {
        this.amount = v;
        this.emit();
    }

    unitChanged(v: DurationUnit) {
        this.unit = v || 1;
        this.emit();
    }

    private emit() {
        const s = this.current();
        if (s !== this.seconds) {
            this.seconds = s;
            this.secondsChange.emit(s);
        }
    }
}

export interface AddMenuItem {
    type: string;
}

/** "+ Add …" that opens a palette of block types */
@Component({
    selector: "automation-add-menu",
    template: `
    <button type="button" class="add-btn" [attr.aria-expanded]="open" (click)="open = !open">
      <fa-icon [icon]="['fas', open ? 'xmark' : 'plus']"></fa-icon>{{ label }}
    </button>
    @if (open) {
      <div class="palette" role="menu" [attr.aria-label]="label">
        <div class="grid">
          @for (t of types; track t) {
            <button type="button" role="menuitem" (click)="pick(t)">
              <b><fa-icon [icon]="['fas', icon(t)]" [fixedWidth]="true"></fa-icon>{{ ctx.describer.typeLabel(kind, t) }}</b>
              <span>{{ ctx.describer.typeHint(kind, t) }}</span>
            </button>
          }
        </div>
      </div>
    }`,
    styleUrls: ["./automation-fields.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AutomationAddMenuComponent {
    @Input() label: string;
    @Input() kind: BlockKind;
    @Input() types: string[] = [];
    @Output() add = new EventEmitter<string>();

    open = false;

    constructor(public ctx: AutomationEditorContext) {
    }

    icon(t: string): string {
        return blockIcon(this.kind, t);
    }

    pick(t: string) {
        this.open = false;
        this.add.emit(t);
    }
}
