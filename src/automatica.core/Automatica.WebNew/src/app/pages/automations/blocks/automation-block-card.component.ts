import { ChangeDetectionStrategy, Component, Input } from "@angular/core";
import { RunStep, ValidationProblem } from "src/app/base/model/automations/automation-dto";
import { userEditableFields } from "src/app/base/model/automations/automation-definition";
import { AutomationEditorContext } from "../automation-editor-context";
import { BlockKind, blockDomId, blockIcon, sectionOf } from "../automation-blocks";

/**
 * Frame of a block: the header reads as a sentence and opens the body. The caller puts the form inside and wraps it
 * in @if (ctx.isExpanded(block)), buttons for the header go into [card-actions].
 */
@Component({
    selector: "automation-block-card",
    templateUrl: "./automation-block-card.component.html",
    styleUrls: ["./automation-block-card.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AutomationBlockCardComponent {
    @Input() kind: BlockKind;
    @Input() block: any;
    @Input() path: string;
    /** override of the type icon */
    @Input() icon: string;
    /** the block is switched off (actions) */
    @Input() disabled = false;
    /** small line under the sentence, defaults to the type */
    @Input() sub: string;

    constructor(public ctx: AutomationEditorContext) {
    }

    get open(): boolean {
        return this.ctx.isExpanded(this.block);
    }

    get section(): string {
        return sectionOf(this.kind);
    }

    get domId(): string {
        return blockDomId(this.path);
    }

    get bodyId(): string {
        return this.domId + "-body";
    }

    get iconName(): string {
        return this.icon || blockIcon(this.kind, this.block?.type);
    }

    get sentence(): string {
        return this.ctx.describer.block(this.kind, this.block, true);
    }

    get subText(): string {
        return this.sub ?? this.ctx.describer.typeLabel(this.kind, this.block?.type ?? "");
    }

    get severity(): "error" | "warning" | undefined {
        return this.ctx.severityUnder(this.path);
    }

    get problemCount(): number {
        return this.ctx.countUnder(this.path);
    }

    get problems(): ValidationProblem[] {
        return this.ctx.problemsAt(this.path);
    }

    get test(): RunStep | undefined {
        const step = this.ctx.testStep(this.path);
        return step && step.passed !== undefined && step.passed !== null ? step : void 0;
    }

    get testValue(): string | undefined {
        const step = this.test;
        if (!step || step.value === undefined) {
            return void 0;
        }
        return this.ctx.describer.stepValue(step, this.block, step.value);
    }

    /** the automation is released for the visualization and this type of block can be handed to its users */
    get canRelease(): boolean {
        return !!this.ctx.definition?.userAccess && userEditableFields(this.kind, this.block?.type).length > 0;
    }

    get released(): boolean {
        return !!this.block?.userEditable;
    }

    /** marks the block: visualization users may change its values (the flag is left out of the JSON when off) */
    toggleRelease() {
        if (this.block.userEditable) {
            delete this.block.userEditable;
        } else {
            this.block.userEditable = true;
        }
    }

    problemText(p: ValidationProblem): string {
        return this.ctx.describer.problem(p);
    }
}
