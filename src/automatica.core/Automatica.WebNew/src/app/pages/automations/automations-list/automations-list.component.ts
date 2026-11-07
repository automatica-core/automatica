import { ChangeDetectionStrategy, Component, OnDestroy, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { BaseComponent } from "src/app/base/base-component";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { AutomationsService } from "src/app/services/automations.service";
import { AutomationDto } from "src/app/base/model/automations/automation-dto";
import { AutomationLookups } from "../automation-lookups";
import { AutomationDescriber } from "../automation-describer";
import { AutomationLookupLoader } from "../automation-lookup-loader";
import { normalizeDefinition } from "../automation-blocks";
import { options } from "../automation-options";

export type RuleStatus = "ok" | "running" | "waiting" | "failed" | "off" | "problems";
type StatusFilter = "all" | "ok" | "active" | "failed" | "off" | "problems";

interface RuleRow {
    dto: AutomationDto;
    status: RuleStatus;
    hasErrors: boolean;
    summary: string;
    search: string;
    areaId: string;
    areaPath: string;
    lastRun: string;
}

interface RuleGroup {
    key: string;
    title: string;
    rows: RuleRow[];
}

const NO_AREA = "none";

@Component({
    selector: "app-automations-list",
    templateUrl: "./automations-list.component.html",
    styleUrls: ["./automations-list.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    providers: [AutomationLookupLoader],
    standalone: false
})
export class AutomationsListComponent extends BaseComponent implements OnInit, OnDestroy {

    readonly lookups = new AutomationLookups();
    readonly describer: AutomationDescriber;

    rows: RuleRow[] = [];
    loaded = false;
    loadError: string | undefined;

    search = "";
    areaFilter = "";
    statusFilter: StatusFilter = "all";

    areaItems: { id: string; name: string }[] = [];
    readonly statusItems = options<StatusFilter>("STATUS_FILTER", ["all", "ok", "active", "failed", "off", "problems"]);

    private toggling = new Set<string>();

    readonly optionText = (item: { key: string }) => item ? this.translate.translate(item.key) : "";

    constructor(
        private automations: AutomationsService,
        private loader: AutomationLookupLoader,
        private router: Router,
        translate: L10nTranslationService,
        notify: NotifyService,
        appService: AppService) {
        super(notify, translate, appService);
        appService.setAppTitle("UI.AUTOMATIONS.TITLE");
        this.describer = new AutomationDescriber((key, params) => translate.translate(key, params), this.lookups,
            () => translate.getLocale()?.language);
    }

    async ngOnInit() {
        super.baseOnInit();
        this.registerEvent(this.translate.onChange() as any, () => this.buildRows());
        await this.load();
    }

    async ngOnDestroy() {
        super.baseOnDestroy();
        await this.loader.release();
    }

    protected async load() {
        const done = this.appService.beginLoading();
        try {
            const list = await this.automations.getAll();
            for (const dto of list) {
                normalizeDefinition(dto.definition);
            }
            await this.loader.load(this.lookups, list);
            this.loadError = void 0;
            this.rawList = list;
            this.buildRows();
        } catch (error) {
            this.loadError = this.parseErrorString(error)?.toString() ?? "";
        } finally {
            this.loaded = true;
            done();
        }
    }

    private rawList: AutomationDto[] = [];

    private buildRows() {
        this.rows = this.rawList.map(dto => this.row(dto));
        const noArea = this.describer.t("NO_AREA");
        this.areaItems = [
            { id: "", name: this.describer.t("ALL_AREAS") },
            ...this.lookups.areas
                .filter(a => this.rows.some(r => r.areaId === a.id))
                .map(a => ({ id: a.id, name: a.path })),
            ...(this.rows.some(r => r.areaId === NO_AREA) ? [{ id: NO_AREA, name: noArea }] : [])
        ];
    }

    private row(dto: AutomationDto): RuleRow {
        const hasErrors = (dto.problems || []).some(p => p.severity === "error");
        const area = dto.this2AreaInstance ? this.lookups.area(dto.this2AreaInstance) : void 0;
        const plain = this.describer.rule(dto.definition, false);
        return {
            dto,
            status: AutomationsListComponent.status(dto, hasErrors),
            hasErrors,
            summary: this.describer.rule(dto.definition, true),
            search: [dto.name, dto.description, plain, area?.path].filter(a => !!a).join(" ").toLowerCase(),
            areaId: area ? area.id : NO_AREA,
            areaPath: area ? area.path : this.describer.t("NO_AREA"),
            lastRun: this.describer.when(dto.lastRunAt)
        };
    }

    static status(dto: AutomationDto, hasErrors: boolean): RuleStatus {
        if (!dto.isEnabled) {
            return "off";
        }
        if (hasErrors) {
            return "problems";
        }
        if (dto.activeRuns > 0 || dto.lastRunState === "running" || dto.lastRunState === "waiting") {
            return dto.lastRunState === "waiting" ? "waiting" : "running";
        }
        if (dto.lastRunState === "failed") {
            return "failed";
        }
        return "ok";
    }

    statusClass(status: RuleStatus): string {
        switch (status) {
            case "ok":
                return "ok";
            case "running":
            case "waiting":
                return "accent";
            case "failed":
                return "bad";
            case "problems":
                return "warn";
            default:
                return "off";
        }
    }

    private matchesStatus(row: RuleRow): boolean {
        switch (this.statusFilter) {
            case "all":
                return true;
            case "active":
                return row.status === "running" || row.status === "waiting";
            case "problems":
                return row.hasErrors || row.status === "problems";
            default:
                return row.status === this.statusFilter;
        }
    }

    get groups(): RuleGroup[] {
        const q = (this.search || "").trim().toLowerCase();
        const visible = this.rows.filter(r =>
            (!q || r.search.includes(q))
            && (!this.areaFilter || r.areaId === this.areaFilter)
            && this.matchesStatus(r));

        const map = new Map<string, RuleGroup>();
        for (const r of visible) {
            let g = map.get(r.areaId);
            if (!g) {
                g = { key: r.areaId, title: r.areaPath, rows: [] };
                map.set(r.areaId, g);
            }
            g.rows.push(r);
        }
        const list = [...map.values()];
        for (const g of list) {
            g.rows.sort((a, b) => (a.dto.name || "").localeCompare(b.dto.name || ""));
        }
        // rules without an area last
        return list.sort((a, b) => a.key === NO_AREA ? 1 : b.key === NO_AREA ? -1 : a.title.localeCompare(b.title));
    }

    trackGroup(_: number, g: RuleGroup) {
        return g.key;
    }

    open(row: RuleRow) {
        this.router.navigate(["/admin/automations", row.dto.objId]);
    }

    create() {
        this.router.navigate(["/admin/automations", "new"]);
    }

    isToggling(row: RuleRow): boolean {
        return this.toggling.has(row.dto.objId);
    }

    async setEnabled(row: RuleRow, enabled: boolean) {
        if (enabled === row.dto.isEnabled || this.toggling.has(row.dto.objId)) {
            return;
        }
        this.toggling.add(row.dto.objId);
        const previous = row.dto.isEnabled;
        row.dto.isEnabled = enabled;
        try {
            const result = await this.automations.setEnabled(row.dto.objId, enabled);
            if (result) {
                normalizeDefinition(result.definition);
                const i = this.rawList.findIndex(a => a.objId === result.objId);
                if (i >= 0) {
                    this.rawList[i] = result;
                }
            }
            this.notifyService.notifyInfo(this.translate.translate(enabled ? "UI.AUTOMATIONS.ENABLED_ON" : "UI.AUTOMATIONS.ENABLED_OFF"));
        } catch (error) {
            row.dto.isEnabled = previous;
            const problems = AutomationsService.problemsOfError(error);
            if (problems) {
                row.dto.problems = problems;
                this.notifyService.notifyWarning(this.translate.translate("UI.AUTOMATIONS.ENABLE_PROBLEMS"), 6000);
            } else {
                this.handleError(error);
            }
        } finally {
            this.toggling.delete(row.dto.objId);
            this.buildRows();
        }
    }
}
