import { ChangeDetectionStrategy, Component, HostListener, OnDestroy, OnInit } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { BaseComponent } from "src/app/base/base-component";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { OperatingModesService } from "src/app/services/operating-modes.service";
import {
    BUILT_IN_MODES, Holiday, HolidayCountry, OperatingModeConfiguration, OperatingModeDay, OperatingModeDefinition, OperatingModeOverview,
    OperatingPeriod, operatingModeName, operatingModeSlug
} from "src/app/base/model/operating-modes/operating-modes";

type ManualChoice = "auto" | "on" | "off";

interface UpcomingDay {
    date: string;
    label: string;
    today: boolean;
    modes: { key: string; name: string; why: string }[];
}

function clone<T>(value: T): T {
    return value === undefined ? value : JSON.parse(JSON.stringify(value));
}

function isoDate(date: Date): string {
    const pad = (n: number) => (n < 10 ? "0" : "") + n;
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/**
 * Operating modes (public holiday, vacation, school holidays, own modes) and operating times, like the Loxone
 * "Operating Modes". Logic blocks (alarm clock, pulse at, operating mode) and automations read the active modes.
 */
@Component({
    selector: "app-operating-modes-page",
    templateUrl: "./operating-modes-page.component.html",
    styleUrls: ["./operating-modes-page.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class OperatingModesPageComponent extends BaseComponent implements OnInit, OnDestroy {

    loaded = false;
    loadError: string | undefined;
    saving = false;

    overview: OperatingModeOverview | undefined;
    configuration: OperatingModeConfiguration | undefined;
    private savedJson = "";

    countries: HolidayCountry[] = [];
    holidays: Holiday[] = [];
    private holidayRequest = 0;

    upcoming: UpcomingDay[] = [];
    newModeName = "";

    readonly manualItems: { value: ManualChoice; key: string }[] = [
        { value: "auto", key: "UI.OPERATING_MODES.MANUAL.AUTO" },
        { value: "on", key: "UI.OPERATING_MODES.MANUAL.ON" },
        { value: "off", key: "UI.OPERATING_MODES.MANUAL.OFF" }
    ];

    constructor(
        private service: OperatingModesService,
        translate: L10nTranslationService,
        notify: NotifyService,
        appService: AppService) {
        super(notify, translate, appService);
        appService.setAppTitle("UI.OPERATING_MODES.TITLE");
    }

    async ngOnInit() {
        super.baseOnInit();
        await this.load();
    }

    ngOnDestroy() {
        super.baseOnDestroy();
    }

    @HostListener("window:beforeunload", ["$event"])
    beforeUnload(event: BeforeUnloadEvent) {
        if (this.dirty) {
            event.preventDefault();
        }
    }

    get dirty(): boolean {
        return !!this.configuration && JSON.stringify(this.configuration) !== this.savedJson;
    }

    get country(): HolidayCountry | undefined {
        return this.countries.find(a => a.code === this.configuration?.country);
    }

    get countryItems(): { code: string; name: string }[] {
        return [{ code: "", name: this.translate.translate("UI.OPERATING_MODES.HOLIDAYS.NONE") }, ...this.countries.map(a => ({ code: a.code, name: `${a.name} (${a.code})` }))];
    }

    get regionItems(): { code: string; name: string }[] {
        const regions = this.country?.regions ?? [];
        return [{ code: "", name: this.translate.translate("UI.OPERATING_MODES.HOLIDAYS.ALL_REGIONS") }, ...regions.map(a => ({ code: a.code, name: a.name }))];
    }

    get sortedModes(): OperatingModeDefinition[] {
        return [...(this.configuration?.modes ?? [])].sort((a, b) => b.priority - a.priority);
    }

    get modeItems(): { key: string; name: string }[] {
        return this.sortedModes.map(a => ({ key: a.key, name: this.modeName(a.key) }));
    }

    get activeToday(): UpcomingDay | undefined {
        return this.upcoming.find(a => a.today);
    }

    // ---------- loading ----------

    protected async load() {
        const done = this.appService.beginLoading();
        try {
            this.apply(await this.service.getOverview(31));
            this.loadError = void 0;
        } catch (error) {
            this.loadError = this.parseErrorString(error)?.toString() ?? "";
        } finally {
            this.loaded = true;
            done();
        }
    }

    private apply(overview: OperatingModeOverview) {
        this.overview = overview;
        this.countries = overview?.countries ?? [];
        this.configuration = clone(overview.configuration);
        this.configuration.disabledHolidays ??= [];
        this.configuration.periods ??= [];
        for (const mode of this.configuration.modes) {
            mode.manual ??= null;
        }
        for (const period of this.configuration.periods) {
            period.name ??= "";
        }
        this.savedJson = JSON.stringify(this.configuration);
        this.holidays = overview.holidays ?? [];
        this.buildUpcoming();
    }

    private buildUpcoming() {
        const days: OperatingModeDay[] = this.overview?.days ?? [];
        const today = this.overview?.today;
        this.upcoming = days
            .filter(a => a.modes?.length || a.date === today)
            .map(a => ({
                date: a.date,
                today: a.date === today,
                label: this.dayLabel(a.date),
                modes: (a.modes ?? []).map(m => ({ key: m, name: this.modeName(m), why: this.reasonText(a, m) }))
            }));
    }

    private dayLabel(date: string): string {
        const d = new Date(date + "T12:00:00");
        return d.toLocaleDateString(this.translate.getLocale()?.language || undefined, { weekday: "short", day: "2-digit", month: "2-digit" });
    }

    private reasonText(day: OperatingModeDay, mode: string): string {
        return (day.reasons?.[mode] ?? []).map(r => {
            switch (r.type) {
                case "holiday":
                    return this.holidayName(r.key);
                case "manual":
                    return this.translate.translate("UI.OPERATING_MODES.REASON.MANUAL");
                default:
                    return r.name || this.translate.translate("UI.OPERATING_MODES.REASON.PERIOD");
            }
        }).join(", ");
    }

    modeName(key: string): string {
        const mode = this.configuration?.modes.find(a => a.key === key) ?? this.overview?.configuration?.modes.find(a => a.key === key);
        return operatingModeName(mode, key, k => this.translate.translate(k));
    }

    holidayName(key: string): string {
        return this.translate.translate("UI.OPERATING_MODES.HOLIDAY." + key);
    }

    holidayDate(holiday: Holiday): string {
        return new Date(holiday.date + "T12:00:00").toLocaleDateString(this.translate.getLocale()?.language || undefined,
            { weekday: "short", day: "2-digit", month: "2-digit", year: "numeric" });
    }

    // ---------- modes ----------

    isBuiltIn(mode: OperatingModeDefinition): boolean {
        return BUILT_IN_MODES.includes(mode.key);
    }

    manualOf(mode: OperatingModeDefinition): ManualChoice {
        return mode.manual === true ? "on" : mode.manual === false ? "off" : "auto";
    }

    setManual(mode: OperatingModeDefinition, choice: ManualChoice) {
        mode.manual = choice === "on" ? true : choice === "off" ? false : null;
    }

    get newModeKey(): string {
        return operatingModeSlug(this.newModeName);
    }

    get canAddMode(): boolean {
        const key = this.newModeKey;
        return !!key && !this.configuration?.modes.some(a => a.key === key);
    }

    addMode() {
        if (!this.configuration || !this.canAddMode) {
            return;
        }
        const lowest = Math.min(100, ...this.configuration.modes.map(a => a.priority));
        this.configuration.modes = [...this.configuration.modes, { key: this.newModeKey, name: this.newModeName.trim(), priority: Math.max(0, lowest - 10), builtIn: false, manual: null }];
        this.newModeName = "";
    }

    removeMode(mode: OperatingModeDefinition) {
        if (!this.configuration || this.isBuiltIn(mode)) {
            return;
        }
        this.configuration.modes = this.configuration.modes.filter(a => a !== mode);
        this.configuration.periods = this.configuration.periods.filter(a => a.mode !== mode.key);
    }

    // ---------- holidays ----------

    async countryChanged(country: string) {
        if (!this.configuration || (country || "") === this.configuration.country) {
            return;
        }
        this.configuration.country = country || "";
        if (!this.country?.regions.some(a => a.code === this.configuration.region)) {
            this.configuration.region = "";
        }
        await this.loadHolidays();
    }

    async regionChanged(region: string) {
        if (!this.configuration || (region || "") === this.configuration.region) {
            return;
        }
        this.configuration.region = region || "";
        await this.loadHolidays();
    }

    private async loadHolidays() {
        const request = ++this.holidayRequest;
        const { country, region } = this.configuration;
        if (!country) {
            this.holidays = [];
            return;
        }
        const year = +(this.overview?.today ?? isoDate(new Date())).substring(0, 4);
        try {
            const [a, b] = await Promise.all([this.service.holidays(country, region, year), this.service.holidays(country, region, year + 1)]);
            if (request === this.holidayRequest) {
                this.holidays = [...a, ...b];
            }
        } catch (error) {
            this.handleError(error);
        }
    }

    holidayEnabled(holiday: Holiday): boolean {
        return !this.configuration?.disabledHolidays.includes(holiday.key);
    }

    setHolidayEnabled(holiday: Holiday, enabled: boolean) {
        if (!this.configuration || this.holidayEnabled(holiday) === enabled) {
            return;
        }
        const rest = this.configuration.disabledHolidays.filter(a => a !== holiday.key);
        this.configuration.disabledHolidays = enabled ? rest : [...rest, holiday.key];
    }

    // ---------- operating times ----------

    addPeriod() {
        if (!this.configuration) {
            return;
        }
        const today = isoDate(new Date());
        this.configuration.periods = [...this.configuration.periods, {
            name: "",
            mode: "vacation",
            start: today,
            end: today,
            yearly: false,
            enabled: true
        }];
    }

    removePeriod(period: OperatingPeriod) {
        if (this.configuration) {
            this.configuration.periods = this.configuration.periods.filter(a => a !== period);
        }
    }

    periodInvalid(period: OperatingPeriod): boolean {
        return !period.start || !period.end || !period.mode;
    }

    // ---------- save ----------

    reset() {
        if (this.overview) {
            this.apply(this.overview);
        }
    }

    async save() {
        if (!this.configuration || this.saving) {
            return;
        }
        if (this.configuration.periods.some(a => this.periodInvalid(a))) {
            this.notifyService.notifyError(this.translate.translate("UI.OPERATING_MODES.PERIODS.INVALID"));
            return;
        }
        this.saving = true;
        const done = this.appService.beginLoading();
        try {
            this.apply(await this.service.save(this.configuration));
            this.notifyService.notifySuccess(this.translate.translate("UI.OPERATING_MODES.SAVED"));
        } catch (error) {
            this.handleError(error);
        } finally {
            this.saving = false;
            done();
        }
    }
}
