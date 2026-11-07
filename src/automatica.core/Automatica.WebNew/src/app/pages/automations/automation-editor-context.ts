import { Injectable } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { AutomationDefinition, TriggerDefinition } from "src/app/base/model/automations/automation-definition";
import { RunStep, ValidationProblem } from "src/app/base/model/automations/automation-dto";
import { AutomationLookups } from "./automation-lookups";
import { AutomationDescriber } from "./automation-describer";
import { blockDomId, emptyDefinition, isUnder, ownerPath } from "./automation-blocks";
import { OptionItem } from "./automation-options";

export type NodePickMode = "read" | "write";

/**
 * State shared by the editor and its block cards: the definition being edited, names, validation problems,
 * test results and which cards are open. Provided by AutomationEditorComponent.
 */
@Injectable()
export class AutomationEditorContext {

    definition: AutomationDefinition = emptyDefinition();

    readonly lookups = new AutomationLookups();
    readonly describer: AutomationDescriber;

    /** problems of the last validation, by the path of the card that shows them */
    private problemsByOwner = new Map<string, ValidationProblem[]>();
    private problemList: ValidationProblem[] = [];

    /** steps of the last "Test now", by path */
    private testSteps = new Map<string, RunStep>();

    private expanded = new WeakSet<object>();

    /** set by the editor, opens the datapoint picker */
    nodePicker: (mode: NodePickMode, current: string) => Promise<string | undefined> = () => Promise.resolve(void 0);

    /** displayExpr of the select boxes with OptionItem lists */
    readonly optionText = (item: OptionItem) => item ? this.translate.translate(item.key) : "";

    constructor(private translate: L10nTranslationService) {
        this.describer = new AutomationDescriber((key, params) => translate.translate(key, params), this.lookups,
            () => translate.getLocale()?.language);
    }

    // ---------- problems ----------

    get problems(): ValidationProblem[] {
        return this.problemList;
    }

    setProblems(problems: ValidationProblem[]) {
        this.problemList = problems || [];
        this.problemsByOwner = new Map();
        for (const p of this.problemList) {
            const owner = ownerPath(this.definition, p.path);
            const list = this.problemsByOwner.get(owner) ?? [];
            list.push(p);
            this.problemsByOwner.set(owner, list);
        }
    }

    /** problems shown on the card of this path */
    problemsAt(path: string): ValidationProblem[] {
        return this.problemsByOwner.get(path) ?? [];
    }

    /** worst severity on this card or inside it, for the header badge */
    severityUnder(path: string): "error" | "warning" | undefined {
        let result: "error" | "warning" | undefined;
        for (const [owner, list] of this.problemsByOwner) {
            if (!isUnder(owner, path)) {
                continue;
            }
            for (const p of list) {
                if (p.severity === "error") {
                    return "error";
                }
                result = "warning";
            }
        }
        return result;
    }

    countUnder(path: string): number {
        let count = 0;
        for (const [owner, list] of this.problemsByOwner) {
            if (isUnder(owner, path)) {
                count += list.length;
            }
        }
        return count;
    }

    // ---------- test ----------

    setTestSteps(steps: RunStep[] | undefined) {
        this.testSteps = new Map((steps || []).map(s => [s.path, s]));
    }

    testStep(path: string): RunStep | undefined {
        return this.testSteps.get(path);
    }

    get hasTestResult(): boolean {
        return this.testSteps.size > 0;
    }

    // ---------- cards ----------

    isExpanded(block: object): boolean {
        return !!block && this.expanded.has(block);
    }

    setExpanded(block: object, open: boolean) {
        if (!block) {
            return;
        }
        if (open) {
            this.expanded.add(block);
        } else {
            this.expanded.delete(block);
        }
    }

    toggle(block: object) {
        this.setExpanded(block, !this.isExpanded(block));
    }

    get triggers(): TriggerDefinition[] {
        return this.definition?.triggers ?? [];
    }

    private triggerIdCache: string[] = [];

    /** ids of the triggers (the same array while they do not change, for select boxes) */
    triggerIds(): string[] {
        const ids = this.triggers.map(a => a.id).filter(a => !!a);
        if (ids.join("\n") !== this.triggerIdCache.join("\n")) {
            this.triggerIdCache = ids;
        }
        return this.triggerIdCache;
    }

    pickNode(mode: NodePickMode, current: string): Promise<string | undefined> {
        return this.nodePicker(mode, current);
    }

    /** opens the card of the path and the cards around it, then scrolls to it */
    reveal(path: string) {
        const segs = (path || "").split("/").filter(a => a !== "");
        for (let i = 1; i <= segs.length; i++) {
            const p = segs.slice(0, i).join("/");
            const owner = ownerPath(this.definition, p);
            if (owner === p) {
                const block = this.resolve(p);
                if (block) {
                    this.setExpanded(block, true);
                }
            }
        }
        setTimeout(() => {
            const el = document.getElementById(blockDomId(ownerPath(this.definition, path)));
            el?.scrollIntoView({ behavior: "smooth", block: "center" });
        }, 50);
    }

    private resolve(path: string): object | undefined {
        // the root group is the section itself, it has no card
        if (path === "conditions") {
            return void 0;
        }
        const segs = path.split("/");
        let cur: any = this.definition;
        for (let i = 0; i < segs.length && cur; i++) {
            const s = segs[i];
            if (/^\d+$/.test(s)) {
                cur = Array.isArray(cur) ? cur[+s] : cur.conditions?.[+s];
            } else {
                cur = cur[s];
            }
        }
        return cur && typeof cur === "object" && !Array.isArray(cur) ? cur : void 0;
    }
}
