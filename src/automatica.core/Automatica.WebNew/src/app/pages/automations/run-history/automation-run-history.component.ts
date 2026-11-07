import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { BaseComponent } from "src/app/base/base-component";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { AutomationsService } from "src/app/services/automations.service";
import { AutomationDefinition } from "src/app/base/model/automations/automation-definition";
import { AutomationRunDto, RunState, RunStep } from "src/app/base/model/automations/automation-dto";
import { AutomationEditorContext } from "../automation-editor-context";
import { ResolvedBlock, blockIcon, resolvePath, sectionOf } from "../automation-blocks";

const POLL_MS = 5000;

export interface StepView {
    step: RunStep;
    resolved: ResolvedBlock | undefined;
    section: string;
    icon: string;
    chip: string;
    sentence: string;
    details: string[];
    time: string;
}

/** the runs of a rule (refreshed every 5 s) and the steps of the selected run */
@Component({
    selector: "automation-run-history",
    templateUrl: "./automation-run-history.component.html",
    styleUrls: ["./automation-run-history.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AutomationRunHistoryComponent extends BaseComponent implements OnInit, OnChanges, OnDestroy {
    @Input() automationId: string;
    /** the saved definition, the step paths point into it */
    @Input() definition: AutomationDefinition;
    @Output() activeRunsChange = new EventEmitter<number>();

    runs: AutomationRunDto[] = [];
    selectedId: string | undefined;
    loaded = false;
    loadError = false;
    stopping = new Set<string>();

    private timer: any;
    private busy = false;
    private stepCache = new Map<string, { key: string; views: StepView[] }>();

    constructor(
        public ctx: AutomationEditorContext,
        private automations: AutomationsService,
        translate: L10nTranslationService,
        notify: NotifyService,
        appService: AppService) {
        super(notify, translate, appService);
    }

    ngOnInit() {
        this.refresh();
        this.timer = setInterval(() => this.refresh(), POLL_MS);
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.automationId && !changes.automationId.firstChange) {
            this.runs = [];
            this.selectedId = void 0;
            this.loaded = false;
            this.refresh();
        }
        if (changes.definition) {
            this.stepCache.clear();
        }
    }

    ngOnDestroy() {
        clearInterval(this.timer);
        super.baseOnDestroy();
    }

    async refresh() {
        if (this.busy || !this.automationId || this.destroyed) {
            return;
        }
        this.busy = true;
        try {
            const runs = await this.automations.getRuns(this.automationId);
            this.runs = runs;
            this.loadError = false;
            if (!this.selectedId || !runs.some(r => r.objId === this.selectedId)) {
                this.selectedId = runs[0]?.objId;
            }
            this.activeRunsChange.emit(runs.filter(r => this.isActive(r.state)).length);
        } catch (e) {
            // keep the last list, the next poll tries again
            this.loadError = true;
            console.warn("automations: loading the runs failed", e);
        } finally {
            this.loaded = true;
            this.busy = false;
        }
    }

    get selected(): AutomationRunDto | undefined {
        return this.runs.find(r => r.objId === this.selectedId);
    }

    select(run: AutomationRunDto) {
        this.selectedId = run.objId;
    }

    isActive(state: RunState): boolean {
        return state === "running" || state === "waiting";
    }

    stateClass(state: RunState): string {
        switch (state) {
            case "done":
                return "ok";
            case "failed":
                return "bad";
            case "running":
            case "waiting":
                return "accent";
            default:
                return "off";
        }
    }

    stateText(run: AutomationRunDto): string {
        const text = this.ctx.describer.runState(run.state);
        if (run.finishedAt && run.state === "done") {
            const ms = new Date(run.finishedAt).getTime() - new Date(run.startedAt).getTime();
            if (ms >= 0) {
                return `${text} · ${this.ctx.describer.durationMs(ms)}`;
            }
        }
        return text;
    }

    started(run: AutomationRunDto): string {
        return this.ctx.describer.when(run.startedAt);
    }

    startedClock(run: AutomationRunDto): string {
        return this.ctx.describer.clock(run.startedAt);
    }

    triggerText(run: AutomationRunDto): string {
        if (!run.triggerId) {
            return this.ctx.describer.t("HISTORY.MANUAL");
        }
        const trigger = this.definition?.triggers?.find(t => t.id === run.triggerId);
        return trigger ? this.ctx.describer.trigger(trigger, false) : run.triggerId;
    }

    async stop(run: AutomationRunDto) {
        this.stopping.add(run.objId);
        try {
            await this.automations.stopRun(this.automationId, run.objId);
            this.notifyService.notifyInfo(this.translate.translate("UI.AUTOMATIONS.HISTORY.STOPPED"));
            await this.refresh();
        } catch (error) {
            this.handleError(error);
        } finally {
            this.stopping.delete(run.objId);
        }
    }

    /** the steps of the selected run with their block sentences (cached while the run does not change) */
    steps(run: AutomationRunDto): StepView[] {
        const key = `${run.state}|${run.steps?.length ?? 0}|${run.finishedAt ?? ""}`;
        const cached = this.stepCache.get(run.objId);
        if (cached && cached.key === key) {
            return cached.views;
        }
        const views = (run.steps || []).map(s => this.view(s));
        this.stepCache.set(run.objId, { key, views });
        return views;
    }

    private view(step: RunStep): StepView {
        const d = this.ctx.describer;
        const resolved = step.path ? resolvePath(this.definition, step.path) : void 0;
        const kind = step.kind;

        let section: string;
        let icon: string;
        if (kind === "error") {
            section = "bad";
            icon = "triangle-exclamation";
        } else if (kind === "info") {
            section = "info";
            icon = "circle-info";
        } else {
            section = sectionOf(kind);
            icon = resolved ? blockIcon(resolved.kind, resolved.block?.type) : blockIcon(kind, "");
        }
        if (step.state === "failed" || (step.passed === false && kind !== "condition")) {
            section = "bad";
        }

        let chip = d.t("STEP.KIND." + kind.toUpperCase());
        if (kind === "condition" && step.passed !== undefined && step.passed !== null) {
            chip += step.passed ? " ✓" : " ✗";
        } else if (step.state) {
            chip += " · " + d.t("STEP.STATE." + step.state.toUpperCase());
        }

        let sentence: string;
        if (resolved) {
            sentence = d.block(resolved.kind, resolved.block, true);
        } else if (step.path) {
            sentence = d.t("STEP.UNKNOWN_BLOCK", { path: step.path });
        } else {
            sentence = kind === "error" ? d.t("STEP.KIND.ERROR") : d.t("PROBLEM.WHOLE_RULE");
        }

        const details: string[] = [];
        const block = resolved?.block;
        if (kind === "trigger" && step.previous !== undefined && step.value !== undefined) {
            details.push(d.t("STEP.CHANGED", { previous: d.stepValue(step, block, step.previous), value: d.stepValue(step, block, step.value) }));
        } else if (step.value !== undefined) {
            details.push(d.t("STEP.VALUE", { value: d.stepValue(step, block, step.value) }));
        }
        if (step.durationMs !== undefined && step.durationMs !== null) {
            details.push(d.t("STEP.TOOK", { duration: d.durationMs(step.durationMs) }));
        }
        if (step.message) {
            details.push(step.message);
        }

        return { step, resolved, section, icon, chip, sentence, details, time: d.clock(step.at, true) };
    }
}
