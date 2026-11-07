import { ChangeDetectionStrategy, Component, DoCheck, HostListener, OnDestroy, OnInit, inject } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { confirm } from "devextreme/ui/dialog";
import { Subscription } from "rxjs";
import { BaseComponent } from "src/app/base/base-component";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { ViewportService } from "src/app/services/viewport.service";
import { AutomationsService } from "src/app/services/automations.service";
import { AutomationDefinition, TRIGGER_TYPES, TriggerType } from "src/app/base/model/automations/automation-definition";
import {
    AUTOMATION_MODES, AutomationDto, AutomationMode, AutomationSaveDto, ValidationProblem
} from "src/app/base/model/automations/automation-dto";
import { AutomationEditorContext, NodePickMode } from "../automation-editor-context";
import { AutomationLookupLoader } from "../automation-lookup-loader";
import { AutomationLookups } from "../automation-lookups";
import { cleanDefinition, emptyDefinition, newTrigger, normalizeDefinition, ownerPath, resolvePath } from "../automation-blocks";
import { options } from "../automation-options";

type EditorTab = "rule" | "history";

@Component({
    selector: "app-automation-editor",
    templateUrl: "./automation-editor.component.html",
    styleUrls: ["./automation-editor.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    providers: [AutomationEditorContext, AutomationLookupLoader],
    standalone: false
})
export class AutomationEditorComponent extends BaseComponent implements OnInit, OnDestroy, DoCheck {

    readonly viewport = inject(ViewportService);

    id: string | undefined;
    loaded = false;
    notFound = false;
    tab: EditorTab = "rule";

    name = "";
    description = "";
    isEnabled = true;
    mode: AutomationMode = "single";
    maxRuns = 10;
    area: string | null = null;
    category: string | null = null;

    /** the rule as last loaded or saved, the run history resolves its paths against it */
    saved: AutomationDto | undefined;
    activeRuns = 0;

    saving = false;
    testing = false;
    validating = false;
    testPassed: boolean | undefined;
    testTriggerId: string | null = null;

    readonly triggerTypes = TRIGGER_TYPES;
    readonly modeItems = options("MODE", AUTOMATION_MODES);
    readonly runItems = [{ id: "skip", key: "UI.AUTOMATIONS.EDIT.RUN_SKIP" }];

    // datapoint picker
    pickerVisible = false;
    pickerMode: NodePickMode = "read";
    pickerSelection: any;
    private pickerResolve: (id: string | undefined) => void;

    private savedJson = "";
    private lastDefJson = "";
    private lastValidatedJson = "";
    private validateTimer: any;
    private paramSub: Subscription;

    constructor(
        public ctx: AutomationEditorContext,
        private automations: AutomationsService,
        private loader: AutomationLookupLoader,
        private route: ActivatedRoute,
        private router: Router,
        translate: L10nTranslationService,
        notify: NotifyService,
        appService: AppService) {
        super(notify, translate, appService);
        appService.setAppTitle("UI.AUTOMATIONS.TITLE");
        this.ctx.nodePicker = (mode, current) => this.openPicker(mode, current);
    }

    ngOnInit() {
        super.baseOnInit();
        this.paramSub = this.route.paramMap.subscribe(params => {
            const id = params.get("id");
            this.id = id && id !== "new" ? id : void 0;
            this.load();
        });
    }

    async ngOnDestroy() {
        clearTimeout(this.validateTimer);
        this.paramSub?.unsubscribe();
        super.baseOnDestroy();
        await this.loader.release();
    }

    @HostListener("window:beforeunload", ["$event"])
    beforeUnload(event: BeforeUnloadEvent) {
        if (this.dirty) {
            event.preventDefault();
        }
    }

    get lookups(): AutomationLookups {
        return this.ctx.lookups;
    }

    get def(): AutomationDefinition {
        return this.ctx.definition;
    }

    get isNew(): boolean {
        return !this.id;
    }

    get title(): string {
        return this.name?.trim() || this.ctx.describer.t(this.isNew ? "EDIT.NEW_TITLE" : "EDIT.UNNAMED");
    }

    get dirty(): boolean {
        return this.loaded && JSON.stringify(this.saveDto()) !== this.savedJson;
    }

    get modeHint(): string {
        return this.ctx.describer.t("MODE." + this.mode.toUpperCase() + "_HINT");
    }

    get usesMaxRuns(): boolean {
        return this.mode === "queued" || this.mode === "parallel";
    }

    get errorCount(): number {
        return this.ctx.problems.filter(p => p.severity === "error").length;
    }

    get ruleSentence(): string {
        return this.ctx.describer.rule(this.def, true);
    }

    protected async load() {
        const done = this.appService.beginLoading();
        this.loaded = false;
        this.notFound = false;
        try {
            let all: AutomationDto[] = [];
            try {
                all = await this.automations.getAll();
            } catch (e) {
                console.warn(e);
            }
            await this.loader.load(this.ctx.lookups, all);

            let dto: AutomationDto | undefined;
            if (this.id) {
                try {
                    dto = await this.automations.getById(this.id);
                } catch (e) {
                    if (e?.status === 404) {
                        this.notFound = true;
                        return;
                    }
                    throw e;
                }
            }
            this.apply(dto);
            this.loaded = true;
        } catch (error) {
            this.handleError(error);
        } finally {
            done();
        }
    }

    private apply(dto: AutomationDto | undefined) {
        this.saved = dto;
        this.name = dto?.name ?? "";
        this.description = dto?.description ?? "";
        this.isEnabled = dto?.isEnabled ?? true;
        this.mode = dto?.mode ?? "single";
        this.maxRuns = dto?.maxRuns || 10;
        this.area = dto?.this2AreaInstance ?? null;
        this.category = dto?.this2CategoryInstance ?? null;
        this.activeRuns = dto?.activeRuns ?? 0;

        this.ctx.definition = normalizeDefinition(dto ? cleanDefinition(dto.definition) : emptyDefinition());
        this.ctx.setProblems(dto?.problems ?? []);
        this.ctx.setTestSteps(void 0);
        this.testPassed = void 0;
        this.testTriggerId = this.ctx.triggerIds()[0] ?? null;

        // the saved definition is normalized too, so it compares equal
        if (this.saved) {
            this.saved = { ...this.saved, definition: cleanDefinition(this.ctx.definition) };
        }

        this.savedJson = JSON.stringify(this.saveDto());
        this.lastDefJson = JSON.stringify(this.def);
        this.lastValidatedJson = dto ? this.lastDefJson : "";

        if (!dto) {
            // a new rule starts with a trigger and an open card
            const trigger = newTrigger("datapoint", []);
            this.def.triggers.push(trigger);
            this.ctx.setExpanded(trigger, true);
            this.lastDefJson = JSON.stringify(this.def);
            this.scheduleValidate();
        }
    }

    saveDto(): AutomationSaveDto {
        return {
            name: this.name ?? "",
            description: this.description ?? "",
            isEnabled: this.isEnabled,
            mode: this.mode,
            maxRuns: this.maxRuns || 10,
            this2AreaInstance: this.area || null,
            this2CategoryInstance: this.category || null,
            definition: cleanDefinition(this.def)
        };
    }

    /** a change anywhere in the definition: validate again and drop the test result */
    ngDoCheck() {
        if (!this.loaded) {
            return;
        }
        const json = JSON.stringify(this.def);
        if (json !== this.lastDefJson) {
            this.lastDefJson = json;
            if (this.ctx.hasTestResult || this.testPassed !== undefined) {
                this.ctx.setTestSteps(void 0);
                this.testPassed = void 0;
            }
            if (this.testTriggerId && !this.ctx.triggerIds().includes(this.testTriggerId)) {
                this.testTriggerId = this.ctx.triggerIds()[0] ?? null;
            }
            this.scheduleValidate();
        }
    }

    private scheduleValidate() {
        clearTimeout(this.validateTimer);
        this.validateTimer = setTimeout(() => this.validate(), 600);
    }

    private async validate() {
        const json = JSON.stringify(this.def);
        if (json === this.lastValidatedJson) {
            return;
        }
        this.validating = true;
        try {
            const problems = await this.automations.validate(cleanDefinition(this.def));
            this.lastValidatedJson = json;
            // only if the definition did not change while the request was running
            if (JSON.stringify(this.def) === json) {
                this.ctx.setProblems(problems);
            }
        } catch (e) {
            console.warn("automations: validate failed", e);
        } finally {
            this.validating = false;
        }
    }

    // ---------- header actions ----------

    async save() {
        if (this.saving) {
            return;
        }
        this.saving = true;
        const done = this.appService.beginLoading();
        try {
            const dto = this.saveDto();
            const result = this.id ? await this.automations.update(this.id, dto) : await this.automations.create(dto);
            this.notifyService.notifySuccess(this.translate.translate("UI.AUTOMATIONS.EDIT.SAVED"));
            if (!this.id && result?.objId) {
                this.savedJson = JSON.stringify(dto);
                await this.router.navigate(["/admin/automations", result.objId], { replaceUrl: true });
                return;
            }
            const expanded = this.def;
            this.apply(result);
            this.restoreExpanded(expanded);
            this.loaded = true;
        } catch (error) {
            const problems = AutomationsService.problemsOfError(error);
            if (problems) {
                this.ctx.setProblems(problems);
                this.notifyService.notifyWarning(this.translate.translate("UI.AUTOMATIONS.EDIT.SAVE_PROBLEMS"), 6000);
            } else {
                this.handleError(error);
            }
        } finally {
            this.saving = false;
            done();
        }
    }

    /** keeps the open cards open after the definition was replaced by the saved one */
    private restoreExpanded(previous: AutomationDefinition) {
        const walk = (a: any, b: any) => {
            if (!a || !b || typeof a !== "object" || typeof b !== "object") {
                return;
            }
            if (!Array.isArray(a) && this.ctx.isExpanded(a)) {
                this.ctx.setExpanded(b, true);
            }
            for (const k of Object.keys(a)) {
                walk(a[k], b[k]);
            }
        };
        walk(previous, this.def);
    }

    async test() {
        if (this.testing) {
            return;
        }
        this.testing = true;
        try {
            const result = await this.automations.test(cleanDefinition(this.def), this.testTriggerId || null);
            this.ctx.setTestSteps(result?.steps ?? []);
            this.testPassed = result?.passed;
            // open the conditions so the ✓ / ✗ are visible
            for (const step of result?.steps ?? []) {
                if (step.kind === "condition" && step.path) {
                    const owner = ownerPath(this.def, step.path);
                    const parent = owner.split("/").slice(0, -1).join("/");
                    const block = parent && parent !== "conditions" ? resolvePath(this.def, parent)?.block : void 0;
                    if (block && block.type === "group") {
                        this.ctx.setExpanded(block, true);
                    }
                }
            }
        } catch (error) {
            this.handleError(error);
        } finally {
            this.testing = false;
        }
    }

    async run(skipConditions: boolean) {
        if (!this.id) {
            return;
        }
        try {
            await this.automations.run(this.id, skipConditions);
            this.notifyService.notifyInfo(this.translate.translate("UI.AUTOMATIONS.EDIT.RUN_STARTED"));
            this.tab = "history";
        } catch (error) {
            this.handleError(error);
        }
    }

    async remove() {
        if (!this.id) {
            await this.router.navigate(["/admin/automations"]);
            return;
        }
        const ok = await confirm(
            this.translate.translate("UI.AUTOMATIONS.EDIT.DELETE_CONFIRM", { name: this.title }),
            this.translate.translate("UI.AUTOMATIONS.EDIT.DELETE_TITLE"));
        if (!ok) {
            return;
        }
        try {
            await this.automations.delete(this.id);
            this.savedJson = JSON.stringify(this.saveDto());
            this.notifyService.notifySuccess(this.translate.translate("UI.AUTOMATIONS.EDIT.DELETED"));
            await this.router.navigate(["/admin/automations"]);
        } catch (error) {
            this.handleError(error);
        }
    }

    async back() {
        if (this.dirty) {
            const ok = await confirm(this.translate.translate("UI.AUTOMATIONS.EDIT.LEAVE_UNSAVED"),
                this.translate.translate("UI.PAGES.UNSAVED"));
            if (!ok) {
                return;
            }
        }
        await this.router.navigate(["/admin/automations"]);
    }

    // ---------- sections ----------

    addTrigger(type: string) {
        const trigger = newTrigger(type as TriggerType, this.def.triggers);
        this.def.triggers.push(trigger);
        this.ctx.setExpanded(trigger, true);
    }

    removeTrigger(i: number) {
        this.def.triggers.splice(i, 1);
    }

    setRootOperator(op: "all" | "any") {
        this.def.conditions.operator = op;
    }

    problemTarget(p: ValidationProblem): string {
        const owner = ownerPath(this.def, p.path);
        if (!owner) {
            return this.ctx.describer.t("PROBLEM.WHOLE_RULE");
        }
        const resolved = resolvePath(this.def, owner);
        return resolved ? this.ctx.describer.block(resolved.kind, resolved.block, false) : owner;
    }

    problemText(p: ValidationProblem): string {
        return this.ctx.describer.problem(p);
    }

    showProblem(p: ValidationProblem) {
        this.tab = "rule";
        if (p.path) {
            this.ctx.reveal(p.path);
        }
    }

    onActiveRunsChanged(count: number) {
        this.activeRuns = count;
    }

    // ---------- datapoint picker ----------

    private openPicker(mode: NodePickMode, current: string): Promise<string | undefined> {
        this.pickerResolve?.(void 0);
        this.pickerMode = mode;
        this.pickerSelection = current ? this.lookups.node(current) : void 0;
        this.pickerVisible = true;
        return new Promise(resolve => this.pickerResolve = resolve);
    }

    pickerSelect(node: any) {
        this.pickerSelection = node;
    }

    get pickerProblem(): string | undefined {
        const node = this.pickerSelection;
        if (!node) {
            return this.ctx.describer.t("PICKER.HINT_" + this.pickerMode.toUpperCase());
        }
        if (!AutomationLookups.isDatapoint(node)) {
            return this.ctx.describer.t("PICKER.NOT_VALUE");
        }
        if (this.pickerMode === "read" && node.IsReadable === false) {
            return this.ctx.describer.t("PICKER.NOT_READABLE");
        }
        if (this.pickerMode === "write" && node.IsWriteable === false) {
            return this.ctx.describer.t("PICKER.NOT_WRITABLE");
        }
        return void 0;
    }

    get pickerName(): string {
        return this.pickerSelection && AutomationLookups.isDatapoint(this.pickerSelection)
            ? (this.pickerSelection.DisplayName || this.pickerSelection.Name) : "";
    }

    closePicker(use: boolean) {
        const resolve = this.pickerResolve;
        this.pickerResolve = void 0;
        this.pickerVisible = false;
        resolve?.(use && !this.pickerProblem ? this.pickerSelection.ObjId : void 0);
    }
}
