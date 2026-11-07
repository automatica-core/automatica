import { ChangeDetectionStrategy, Component, HostListener, OnDestroy, OnInit, inject } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { confirm } from "devextreme/ui/dialog";
import { BaseComponent } from "src/app/base/base-component";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { ViewportService } from "src/app/services/viewport.service";
import { ShadingService } from "src/app/services/shading.service";
import {
    AvailableBlind, BLIND_TYPES, BlindDefinition, BlindMode, BlindPreview, BlindStatus, COMPASS_POINTS, FacadeStatus, MANUAL_OVERRIDE_MODES,
    ShadingBlind, ShadingFacade, ShadingPreview, ShadingProblem, ShadingSettings, ShadingStatus, SunPathPoint, SunThresholds, WINTER_MODES,
    compassPoint
} from "src/app/base/model/shading/shading";
import { AutomationLookupLoader } from "../../automations/automation-lookup-loader";
import { AutomationLookups } from "../../automations/automation-lookups";
import { ShadingContext, shadingOptions } from "../shading-context";

type ShadingTab = "overview" | "facades" | "blinds" | "settings";

interface BlindEdit {
    isNew: boolean;
    controlId: string;
    name: string;
    hasSlats: boolean;
    isEnabled: boolean;
    /** "facade": direction of the facade, "own": the blind's own azimuth */
    direction: "facade" | "own";
    facade: string | null;
    definition: BlindDefinition;
}

interface BlindRow {
    blind: ShadingBlind;
    status?: BlindStatus;
    preview?: BlindPreview;
    facadeName: string;
    direction: string;
}

const STATUS_INTERVAL_MS = 30000;

function clone<T>(value: T): T {
    return value === undefined ? value : JSON.parse(JSON.stringify(value));
}

/** null for an empty number box */
function num(value: any): number | null {
    return value === null || value === undefined || value === "" || isNaN(+value) ? null : +value;
}

@Component({
    selector: "app-shading-page",
    templateUrl: "./shading-page.component.html",
    styleUrls: ["./shading-page.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    providers: [ShadingContext, AutomationLookupLoader],
    standalone: false
})
export class ShadingPageComponent extends BaseComponent implements OnInit, OnDestroy {

    readonly viewport = inject(ViewportService);

    tab: ShadingTab = "overview";
    loaded = false;
    loadError: string | undefined;

    settings: ShadingSettings | undefined;
    private savedSettingsJson = "";
    settingsProblems: ShadingProblem[] = [];
    savingSettings = false;

    facades: ShadingFacade[] = [];
    blinds: ShadingBlind[] = [];
    available: AvailableBlind[] = [];

    status: ShadingStatus | undefined;
    sunPath: SunPathPoint[] = [];

    /** minutes of the day shown by the preview, null: now */
    previewMinutes: number | null = null;
    preview: ShadingPreview | undefined;
    private previewTimer: any;
    private previewRequest = 0;

    compassFacades: any[] = [];
    blindRows: BlindRow[] = [];
    selectedFacade: string | null = null;

    // facade editor
    facadeEdit: ShadingFacade | undefined;
    facadeVisible = false;
    facadeProblems: ShadingProblem[] = [];
    facadeOwnThresholds = false;

    // blind editor
    blindEdit: BlindEdit | undefined;
    blindVisible = false;
    blindProblems: ShadingProblem[] = [];

    // add blind
    addVisible = false;
    addChoice: string | null = null;

    // datapoint picker
    pickerVisible = false;
    pickerSelection: any;
    private pickerResolve: (id: string | undefined) => void;

    private toggling = new Set<string>();

    readonly blindTypes = shadingOptions("BLIND_TYPE", BLIND_TYPES);
    readonly overrideModes = shadingOptions("OVERRIDE", MANUAL_OVERRIDE_MODES, v => v.replace(/[A-Z]/g, m => "_" + m).toUpperCase());
    readonly winterModes = shadingOptions("WINTER_MODE", WINTER_MODES);
    readonly windProtectionItems = shadingOptions("WIND_PROTECTION", ["default", "on", "off"]);
    readonly directionItems = shadingOptions("DIRECTION_MODE", ["facade", "own"]);
    readonly months = Array.from({ length: 12 }, (_, i) => ({ value: i + 1, key: `UI.SHADING.MONTH.M${i + 1}` }));
    readonly compassItems = COMPASS_POINTS.map((p, i) => ({ value: i * 22.5, key: `UI.SHADING.DIR.${p}` }));

    constructor(
        public ctx: ShadingContext,
        private shading: ShadingService,
        private loader: AutomationLookupLoader,
        translate: L10nTranslationService,
        notify: NotifyService,
        appService: AppService) {
        super(notify, translate, appService);
        appService.setAppTitle("UI.SHADING.TITLE");
        this.ctx.nodePicker = current => this.openPicker(current);
    }

    async ngOnInit() {
        super.baseOnInit();
        await this.load();
        this.registerInterval(() => this.loadStatus(), STATUS_INTERVAL_MS);
    }

    async ngOnDestroy() {
        clearTimeout(this.previewTimer);
        super.baseOnDestroy();
        await this.loader.release();
    }

    @HostListener("window:beforeunload", ["$event"])
    beforeUnload(event: BeforeUnloadEvent) {
        if (this.settingsDirty) {
            event.preventDefault();
        }
    }

    get lookups(): AutomationLookups {
        return this.ctx.lookups;
    }

    get settingsDirty(): boolean {
        return !!this.settings && JSON.stringify(this.settings) !== this.savedSettingsJson;
    }

    // ---------- loading ----------

    protected async load() {
        const done = this.appService.beginLoading();
        try {
            const [settings, facades, blinds, available] = await Promise.all([
                this.shading.getSettings(),
                this.shading.getFacades(),
                this.shading.getBlinds(),
                this.shading.getAvailableBlinds()
            ]);
            this.applySettings(settings);
            this.facades = facades.sort((a, b) => a.azimuth - b.azimuth);
            this.blinds = blinds.sort((a, b) => (a.name || "").localeCompare(b.name || ""));
            this.available = available;
            this.loadError = void 0;

            await Promise.all([this.loader.load(this.lookups), this.loadStatus(), this.loadSunPath()]);
        } catch (error) {
            this.loadError = this.parseErrorString(error)?.toString() ?? "";
        } finally {
            this.loaded = true;
            done();
        }
    }

    private applySettings(settings: ShadingSettings) {
        // the signal editor gives every calculated brightness a cloud cover signal, before the change check starts
        for (const source of settings?.brightness?.sources || []) {
            if (source.type === "calculated" && !source.cloudCover) {
                source.cloudCover = { sources: [] };
            }
        }
        this.settings = settings;
        this.savedSettingsJson = JSON.stringify(settings);
        this.settingsProblems = [];
    }

    async loadStatus() {
        try {
            this.status = await this.shading.getStatus();
        } catch (error) {
            console.warn("shading: status", error);
        }
        this.refreshViews();
    }

    private async loadSunPath() {
        try {
            this.sunPath = await this.shading.getSunPath(new Date(), 15);
        } catch {
            // no location: the compass shows no path
            this.sunPath = [];
        }
    }

    // ---------- derived views ----------

    private refreshViews() {
        const facadeStates = new Map<string, FacadeStatus>(((this.preview ?? this.status)?.facades || []).map(f => [f.id.toLowerCase(), f]));
        this.compassFacades = this.facades.map(f => {
            const state = facadeStates.get((f.objId || "").toLowerCase());
            return {
                id: f.objId,
                name: f.name,
                azimuth: f.azimuth,
                enabled: f.isEnabled,
                sunny: !!state?.sunny && !!state?.sunOnFacade,
                lit: !!state?.sunOnFacade,
                acceptance: f.definition?.acceptanceAngle ?? 80
            };
        });

        const statusMap = new Map((this.status?.blinds || []).map(b => [b.controlId.toLowerCase(), b]));
        const previewMap = new Map((this.preview?.blinds || []).map(b => [b.controlId.toLowerCase(), b]));
        this.blindRows = this.blinds.map(b => {
            const facade = this.facadeOf(b.this2ShadingFacade);
            const azimuth = b.definition?.azimuth ?? facade?.azimuth;
            return {
                blind: b,
                status: statusMap.get(b.controlId.toLowerCase()),
                preview: previewMap.get(b.controlId.toLowerCase()),
                facadeName: facade?.name ?? "",
                direction: azimuth === undefined || azimuth === null ? "" : this.directionLabel(azimuth)
            };
        });
    }

    facadeOf(id: string | null | undefined): ShadingFacade | undefined {
        if (!id) {
            return void 0;
        }
        return this.facades.find(f => (f.objId || "").toLowerCase() === id.toLowerCase());
    }

    facadeState(f: ShadingFacade): FacadeStatus | undefined {
        return ((this.preview ?? this.status)?.facades || []).find(a => a.id.toLowerCase() === (f.objId || "").toLowerCase());
    }

    directionLabel(azimuth: number): string {
        return `${this.translate.translate("UI.SHADING.DIR." + compassPoint(azimuth))} · ${Math.round(azimuth * 10) / 10}°`;
    }

    modeClass(mode: BlindMode | undefined): string {
        switch (mode) {
            case "shading":
                return "accent";
            case "winter":
                return "ok";
            case "manual":
            case "doorOpen":
                return "warn";
            case "wind":
            case "rain":
            case "frost":
            case "unavailable":
                return "bad";
            default:
                return "off";
        }
    }

    lux(value: number | null | undefined): string {
        if (value === null || value === undefined) {
            return "–";
        }
        return value >= 1000 ? `${(value / 1000).toFixed(value >= 10000 ? 0 : 1)} klx` : `${Math.round(value)} lx`;
    }

    number(value: number | null | undefined, unit: string, digits = 1): string {
        if (value === null || value === undefined) {
            return "–";
        }
        return `${(Math.round(value * Math.pow(10, digits)) / Math.pow(10, digits)).toLocaleString(this.translate.getLocale()?.language)} ${unit}`;
    }

    /** "calculated", "datapoint:<id>", "calculated+clouds:datapoint:<id>" as text */
    sourceLabel(source: string | null | undefined): string {
        if (!source) {
            return "";
        }
        return source.split(",").map(part => {
            const clouds = part.indexOf("+clouds:");
            if (clouds >= 0) {
                return this.ctx.t("SOURCE_SHORT.CALCULATED_CLOUDS", { clouds: this.sourceLabel(part.substring(clouds + 8)) });
            }
            const [type, id] = part.split(":");
            const name = id ? this.lookups.nodeName(id) : void 0;
            const typeText = this.ctx.t("SOURCE_SHORT." + (type || "").toUpperCase());
            return name ? `${typeText} · ${name}` : typeText;
        }).join(", ");
    }

    get previewTimeLabel(): string {
        if (this.previewMinutes === null) {
            return this.ctx.t("OVERVIEW.NOW");
        }
        const h = Math.floor(this.previewMinutes / 60);
        const m = this.previewMinutes % 60;
        return `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}`;
    }

    get shownSun() {
        return this.preview ? this.preview.sun : this.status?.sun;
    }

    get nowMinutes(): number {
        const d = new Date();
        return d.getHours() * 60 + d.getMinutes();
    }

    get counts() {
        const blinds = this.status?.blinds || [];
        return {
            shading: blinds.filter(b => b.mode === "shading").length,
            manual: blinds.filter(b => b.mode === "manual").length,
            alarm: blinds.filter(b => b.mode === "wind" || b.mode === "rain").length
        };
    }

    // ---------- preview ----------

    previewChanged(minutes: number) {
        this.previewMinutes = minutes;
        clearTimeout(this.previewTimer);
        this.previewTimer = setTimeout(() => this.loadPreview(), 150);
    }

    previewNow() {
        clearTimeout(this.previewTimer);
        this.previewMinutes = null;
        this.preview = void 0;
        this.refreshViews();
    }

    private async loadPreview() {
        if (this.previewMinutes === null) {
            return;
        }
        const at = new Date();
        at.setHours(0, 0, 0, 0);
        at.setMinutes(this.previewMinutes);
        const request = ++this.previewRequest;
        try {
            const preview = await this.shading.getPreview(at);
            if (request === this.previewRequest && this.previewMinutes !== null) {
                this.preview = preview;
                this.refreshViews();
            }
        } catch (error) {
            this.handleError(error);
        }
    }

    // ---------- settings ----------

    async setGlobalEnabled(enabled: boolean) {
        if (!this.settings || this.settings.isEnabled === enabled) {
            return;
        }
        if (this.settingsDirty) {
            // the switch saves only itself, the other changes stay unsaved
            const saved: ShadingSettings = JSON.parse(this.savedSettingsJson);
            saved.isEnabled = enabled;
            this.settings.isEnabled = enabled;
            await this.saveSettingsDocument(saved, false);
            return;
        }
        this.settings.isEnabled = enabled;
        await this.saveSettings();
    }

    async saveSettings() {
        if (!this.settings) {
            return;
        }
        await this.saveSettingsDocument(this.settings, true);
    }

    private async saveSettingsDocument(document: ShadingSettings, replace: boolean) {
        if (this.savingSettings) {
            return;
        }
        this.savingSettings = true;
        const done = this.appService.beginLoading();
        try {
            const result = await this.shading.saveSettings(document);
            if (replace) {
                this.applySettings(result);
            } else {
                const current = this.settings;
                this.savedSettingsJson = JSON.stringify(result);
                this.settings = current;
            }
            this.notifyService.notifySuccess(this.ctx.t("SAVED"));
            await this.loadStatus();
        } catch (error) {
            const problems = ShadingService.problemsOfError(error);
            if (problems) {
                this.settingsProblems = problems;
                this.tab = "settings";
                this.notifyService.notifyWarning(this.ctx.t("SAVE_PROBLEMS"), 6000);
            } else {
                this.handleError(error);
            }
        } finally {
            this.savingSettings = false;
            done();
        }
    }

    resetSettings() {
        if (this.savedSettingsJson) {
            this.applySettings(JSON.parse(this.savedSettingsJson));
        }
    }

    get hasFrostLimit(): boolean {
        return this.settings?.frostBelow !== null && this.settings?.frostBelow !== undefined;
    }

    setFrostLimit(on: boolean) {
        if (this.settings) {
            this.settings.frostBelow = on ? (this.settings.frostBelow ?? 0) : null;
        }
    }

    problemText(p: ShadingProblem): string {
        const key = "UI.SHADING.PROBLEM." + p.code;
        const text = this.translate.translate(key);
        return text && text !== key ? text : (p.detail || p.code);
    }

    // ---------- facades ----------

    newFacade() {
        this.facadeEdit = {
            name: "",
            azimuth: 180,
            isEnabled: true,
            definition: { brightness: { sources: [] }, thresholds: null, acceptanceAngle: 80, minElevation: 5, maxElevation: 90, minOutsideTemperature: null }
        };
        this.facadeOwnThresholds = false;
        this.facadeProblems = [];
        this.facadeVisible = true;
    }

    editFacade(f: ShadingFacade) {
        this.facadeEdit = clone(f);
        this.facadeEdit.definition.brightness = this.facadeEdit.definition.brightness || { sources: [] };
        this.facadeOwnThresholds = !!this.facadeEdit.definition.thresholds;
        this.facadeProblems = [];
        this.facadeVisible = true;
        this.selectedFacade = f.objId ?? null;
    }

    setFacadeThresholds(on: boolean) {
        this.facadeOwnThresholds = on;
        if (!this.facadeEdit) {
            return;
        }
        this.facadeEdit.definition.thresholds = on
            ? (this.facadeEdit.definition.thresholds ?? clone(this.settings?.thresholds) ?? { onLux: 20000, offLux: 12000, onDelayMinutes: 2, offDelayMinutes: 15 } as SunThresholds)
            : null;
    }

    get facadeHasMinTemperature(): boolean {
        const v = this.facadeEdit?.definition?.minOutsideTemperature;
        return v !== null && v !== undefined;
    }

    setFacadeMinTemperature(on: boolean) {
        if (this.facadeEdit) {
            this.facadeEdit.definition.minOutsideTemperature = on ? (this.facadeEdit.definition.minOutsideTemperature ?? 20) : null;
        }
    }

    async saveFacade() {
        const f = this.facadeEdit;
        if (!f) {
            return;
        }
        const done = this.appService.beginLoading();
        try {
            f.azimuth = num(f.azimuth) ?? 0;
            const saved = f.objId ? await this.shading.updateFacade(f) : await this.shading.createFacade(f);
            const index = this.facades.findIndex(a => a.objId === saved.objId);
            if (index >= 0) {
                this.facades[index] = saved;
            } else {
                this.facades.push(saved);
            }
            this.facades.sort((a, b) => a.azimuth - b.azimuth);
            this.facadeVisible = false;
            this.notifyService.notifySuccess(this.ctx.t("SAVED"));
            await this.loadStatus();
        } catch (error) {
            const problems = ShadingService.problemsOfError(error);
            if (problems) {
                this.facadeProblems = problems;
            } else {
                this.handleError(error);
            }
        } finally {
            done();
        }
    }

    async deleteFacade(f: ShadingFacade) {
        const used = this.blinds.filter(b => b.this2ShadingFacade?.toLowerCase() === (f.objId || "").toLowerCase()).length;
        const ok = await confirm(this.ctx.t(used ? "FACADES.DELETE_USED" : "FACADES.DELETE_CONFIRM", { name: f.name, count: used }),
            this.ctx.t("FACADES.DELETE"));
        if (!ok) {
            return;
        }
        const done = this.appService.beginLoading();
        try {
            await this.shading.deleteFacade(f.objId);
            this.facades = this.facades.filter(a => a.objId !== f.objId);
            this.blinds = await this.shading.getBlinds();
            this.facadeVisible = false;
            await this.loadStatus();
        } catch (error) {
            this.handleError(error);
        } finally {
            done();
        }
    }

    async toggleFacade(f: ShadingFacade, enabled: boolean) {
        if (f.isEnabled === enabled) {
            return;
        }
        try {
            const saved = await this.shading.updateFacade({ ...f, isEnabled: enabled });
            Object.assign(f, saved);
            await this.loadStatus();
        } catch (error) {
            this.handleError(error);
        }
    }

    // ---------- blinds ----------

    get unconfigured(): AvailableBlind[] {
        const configured = new Set(this.blinds.map(b => b.controlId.toLowerCase()));
        return this.available.filter(a => !configured.has(a.controlId.toLowerCase()));
    }

    async openAdd() {
        try {
            this.available = await this.shading.getAvailableBlinds();
        } catch (error) {
            this.handleError(error);
            return;
        }
        this.addChoice = this.unconfigured[0]?.controlId ?? null;
        this.addVisible = true;
    }

    startNewBlind() {
        const choice = this.available.find(a => a.controlId === this.addChoice);
        if (!choice) {
            return;
        }
        this.addVisible = false;
        const venetian = choice.hasSlats;
        this.blindEdit = {
            isNew: true,
            controlId: choice.controlId,
            name: choice.name,
            hasSlats: choice.hasSlats,
            isEnabled: true,
            direction: this.facades.length ? "facade" : "own",
            facade: this.facades[0]?.objId ?? null,
            definition: {
                type: venetian ? "venetian" : "roller",
                azimuth: this.facades.length ? null : 180,
                acceptanceAngle: null,
                minElevation: null,
                maxElevation: null,
                shadePosition: 100,
                restorePosition: true,
                openPosition: 0,
                slats: {
                    trackSun: true, fixedAngle: 45, width: 80, spacing: 72, extraAngle: 5, minAngle: 0, maxAngle: 90,
                    percentAtMinAngle: 0, percentAtMaxAngle: 100, openPercent: 0
                },
                windProtection: null,
                windLimit: null,
                rainProtection: false,
                doorContact: null,
                travelSeconds: 90,
                winterGain: true,
                roomTemperature: { sources: [] },
                maxRoomTemperature: null
            }
        };
        this.blindProblems = [];
        this.blindVisible = true;
    }

    editBlind(b: ShadingBlind) {
        const definition = clone(b.definition);
        definition.roomTemperature = definition.roomTemperature || { sources: [] };
        this.blindEdit = {
            isNew: false,
            controlId: b.controlId,
            name: b.name,
            hasSlats: b.hasSlats,
            isEnabled: b.isEnabled,
            direction: b.this2ShadingFacade ? "facade" : "own",
            facade: b.this2ShadingFacade ?? null,
            definition
        };
        this.blindProblems = [];
        this.blindVisible = true;
    }

    get blindWindProtection(): string {
        const v = this.blindEdit?.definition.windProtection;
        return v === true ? "on" : v === false ? "off" : "default";
    }

    set blindWindProtection(value: string) {
        if (this.blindEdit) {
            this.blindEdit.definition.windProtection = value === "on" ? true : value === "off" ? false : null;
        }
    }

    setBlindDirection(direction: "facade" | "own") {
        const edit = this.blindEdit;
        if (!edit) {
            return;
        }
        edit.direction = direction;
        if (direction === "own") {
            edit.definition.azimuth = edit.definition.azimuth ?? this.facadeOf(edit.facade)?.azimuth ?? 180;
        } else {
            edit.definition.azimuth = null;
            edit.facade = edit.facade ?? this.facades[0]?.objId ?? null;
        }
    }

    /** the optional angle overrides, null = from the facade */
    hasOverride(field: "acceptanceAngle" | "minElevation" | "maxElevation" | "windLimit" | "maxRoomTemperature"): boolean {
        const v = this.blindEdit?.definition?.[field];
        return v !== null && v !== undefined;
    }

    setOverride(field: "acceptanceAngle" | "minElevation" | "maxElevation" | "windLimit" | "maxRoomTemperature", on: boolean) {
        const def = this.blindEdit?.definition;
        if (!def) {
            return;
        }
        const facade = this.blindEdit.direction === "facade" ? this.facadeOf(this.blindEdit.facade)?.definition : void 0;
        const defaults = {
            acceptanceAngle: facade?.acceptanceAngle ?? 80,
            minElevation: facade?.minElevation ?? 5,
            maxElevation: facade?.maxElevation ?? 90,
            windLimit: this.settings?.windLimit ?? 10,
            maxRoomTemperature: 24
        };
        def[field] = on ? (def[field] ?? defaults[field]) : null;
    }

    async saveBlind() {
        const edit = this.blindEdit;
        if (!edit) {
            return;
        }
        const def = edit.definition;
        def.azimuth = edit.direction === "own" ? (num(def.azimuth) ?? 0) : null;
        const done = this.appService.beginLoading();
        try {
            const saved = await this.shading.saveBlind(edit.controlId, {
                this2ShadingFacade: edit.direction === "facade" ? edit.facade : null,
                isEnabled: edit.isEnabled,
                definition: def
            });
            const index = this.blinds.findIndex(a => a.controlId.toLowerCase() === saved.controlId.toLowerCase());
            if (index >= 0) {
                this.blinds[index] = saved;
            } else {
                this.blinds.push(saved);
                this.blinds.sort((a, b) => (a.name || "").localeCompare(b.name || ""));
            }
            this.blindVisible = false;
            this.notifyService.notifySuccess(this.ctx.t("SAVED"));
            await this.loadStatus();
        } catch (error) {
            const problems = ShadingService.problemsOfError(error);
            if (problems) {
                this.blindProblems = problems;
            } else {
                this.handleError(error);
            }
        } finally {
            done();
        }
    }

    async deleteBlind(b: ShadingBlind) {
        const ok = await confirm(this.ctx.t("BLINDS.DELETE_CONFIRM", { name: b.name }), this.ctx.t("BLINDS.DELETE"));
        if (!ok) {
            return;
        }
        const done = this.appService.beginLoading();
        try {
            await this.shading.deleteBlind(b.controlId);
            this.blinds = this.blinds.filter(a => a.controlId !== b.controlId);
            this.blindVisible = false;
            await this.loadStatus();
        } catch (error) {
            this.handleError(error);
        } finally {
            done();
        }
    }

    blindCount(f: ShadingFacade): number {
        const id = (f.objId || "").toLowerCase();
        return this.blinds.filter(b => (b.this2ShadingFacade || "").toLowerCase() === id).length;
    }

    isToggling(b: ShadingBlind): boolean {
        return this.toggling.has(b.controlId);
    }

    async setAutomatic(b: ShadingBlind, automatic: boolean) {
        if (b.isPaused === !automatic || this.toggling.has(b.controlId)) {
            return;
        }
        this.toggling.add(b.controlId);
        try {
            await this.shading.setAutomatic(b.controlId, automatic);
            b.isPaused = !automatic;
            await this.loadStatus();
        } catch (error) {
            this.handleError(error);
        } finally {
            this.toggling.delete(b.controlId);
        }
    }

    async resume(b: ShadingBlind) {
        try {
            await this.shading.resume(b.controlId);
            await this.loadStatus();
        } catch (error) {
            this.handleError(error);
        }
    }

    // ---------- datapoint picker ----------

    private openPicker(current: string): Promise<string | undefined> {
        this.pickerResolve?.(void 0);
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
            return this.ctx.t("PICKER.HINT");
        }
        if (!AutomationLookups.isDatapoint(node)) {
            return this.ctx.t("PICKER.NOT_VALUE");
        }
        if (node.IsReadable === false) {
            return this.ctx.t("PICKER.NOT_READABLE");
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
