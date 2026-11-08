import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, inject } from "@angular/core";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";
import { OperatingModesService } from "src/app/services/operating-modes.service";
import { operatingModeName } from "src/app/base/model/operating-modes/operating-modes";

/** the logic template of the alarm clock (P3.Logic.Time, "time.alarm_clock") */
export const ALARM_CLOCK_TEMPLATE = "3b9bcc74-6577-40ed-964b-029979fa0187";

/** one alarm of the alarm clock logic, the JSON of its "alarms" parameter */
export interface AlarmEntry {
    id: string;
    name: string;
    /** HH:mm */
    time: string;
    enabled: boolean;
    /** 0 = Sunday ... 6 = Saturday */
    days: number[];
    /** operating mode keys: the alarm rings only on days with one of them */
    modes: string[];
}

/** Monday first */
const WEEK = [1, 2, 3, 4, 5, 6, 0];

export function parseLocalDate(value: any): Date | undefined {
    if (value === null || value === undefined || value === "") {
        return void 0;
    }
    const date = value instanceof Date ? value : new Date(value);
    return isNaN(date.getTime()) || date.getFullYear() < 2000 ? void 0 : date;
}

/**
 * Alarm clock tile (Loxone "Alarm Clock"): the next alarm, snooze and stop while it rings, the alarms are edited in the popup
 * and written to the logic's "alarms" parameter (the logic keeps them across restarts).
 */
@Component({
    selector: "visu-alarm-clock",
    templateUrl: "./alarm-clock.component.html",
    styleUrls: ["./alarm-clock.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AlarmClockComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

    private readonly operatingModes = inject(OperatingModesService);

    private alarmsParam: RuleInterfaceInstance;
    snoozeInput: RuleInterfaceInstance;
    confirmInput: RuleInterfaceInstance;
    private alarmOutput: RuleInterfaceInstance;
    private buzzerOutput: RuleInterfaceInstance;
    private nextOutput: RuleInterfaceInstance;
    private entriesOutput: RuleInterfaceInstance;
    private snoozeOutput: RuleInterfaceInstance;

    entries: AlarmEntry[] = [];
    edit: AlarmEntry[] = [];
    private editBase = "[]";
    nextAlarm: Date | undefined;
    alarmActive = false;
    buzzer = false;
    snoozeSeconds = 0;
    activeModes: string[] = [];
    saving = false;

    modeItems: { key: string; name: string }[] = [];
    readonly week = WEEK;

    async ngOnInit() {
        this.baseOnInit();
        super.mobileRuleInit();

        this.alarmsParam = this.getInterfaceByKey("alarms");
        this.snoozeInput = this.getInterfaceByKey("snooze");
        this.confirmInput = this.getInterfaceByKey("confirm");
        this.alarmOutput = this.getInterfaceByKey("alarm");
        this.buzzerOutput = this.getInterfaceByKey("buzzer");
        this.nextOutput = this.getInterfaceByKey("next_alarm");
        this.entriesOutput = this.getInterfaceByKey("entries");
        this.snoozeOutput = this.getInterfaceByKey("snooze_remaining");

        for (const output of [this.alarmOutput, this.buzzerOutput, this.nextOutput, this.entriesOutput, this.snoozeOutput]) {
            const current = output ? this.dataHub.getCurrentValue(output.ObjId) : void 0;
            if (current) {
                this.onRuleInstanceValueChanged(output.ObjId, current.value);
            }
        }

        await this.loadData();
    }

    private async loadData() {
        try {
            const data = await this.ruleInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId);
            if (!data) {
                return;
            }
            this.setEntries(this.parseEntries(data.alarms));
            this.nextAlarm = parseLocalDate(data.nextAlarm);
            this.alarmActive = !!data.alarmActive;
            this.buzzer = !!data.buzzer;
            this.activeModes = Array.isArray(data.modes) ? data.modes : [];
        } catch (error) {
            console.warn("alarm clock: data", error);
        }
    }

    /** new entries from the rule: the editor copy follows unless the user changed it */
    private setEntries(entries: AlarmEntry[]) {
        const untouched = !this.editDirty;
        this.entries = entries;
        if (untouched) {
            this.resetEdit();
        }
    }

    private resetEdit() {
        this.editBase = JSON.stringify(this.entries);
        this.edit = JSON.parse(this.editBase);
    }

    private parseEntries(value: any): AlarmEntry[] {
        try {
            const list = typeof value === "string" ? JSON.parse(value) : value;
            return Array.isArray(list) ? list.map(a => ({
                id: a.id ?? "",
                name: a.name ?? "",
                time: a.time ?? "06:00",
                enabled: a.enabled !== false,
                days: Array.isArray(a.days) ? a.days : [],
                modes: Array.isArray(a.modes) ? a.modes : []
            })) : [];
        } catch {
            return [];
        }
    }

    onRuleInstanceValueChanged(interfaceId, value) {
        if (this.alarmOutput?.ObjId === interfaceId) {
            this.alarmActive = !!value;
        } else if (this.buzzerOutput?.ObjId === interfaceId) {
            this.buzzer = !!value;
        } else if (this.nextOutput?.ObjId === interfaceId) {
            this.nextAlarm = parseLocalDate(value);
        } else if (this.entriesOutput?.ObjId === interfaceId) {
            this.setEntries(this.parseEntries(value));
        } else if (this.snoozeOutput?.ObjId === interfaceId) {
            this.snoozeSeconds = Math.max(0, +value || 0);
        }
    }

    private get locale(): string | undefined {
        return this.translate.getLocale()?.language || undefined;
    }

    get alarmsParamPresent(): boolean {
        return !!this.alarmsParam;
    }

    get ringing(): boolean {
        return this.alarmActive && this.buzzer;
    }

    get snoozing(): boolean {
        return this.alarmActive && !this.buzzer && this.snoozeSeconds > 0;
    }

    get nextText(): string {
        if (!this.nextAlarm) {
            return this.translate.translate("UI.VISU.ALARM_CLOCK.NONE");
        }
        const sameDay = this.nextAlarm.toDateString() === new Date().toDateString();
        const time = this.nextAlarm.toLocaleTimeString(this.locale, { hour: "2-digit", minute: "2-digit" });
        if (sameDay) {
            return time;
        }
        // within the next week the weekday is enough, later ones (vacation, holidays skipped) need the date
        const withinWeek = this.nextAlarm.getTime() - Date.now() < 6 * 24 * 3600 * 1000;
        const day = this.nextAlarm.toLocaleDateString(this.locale, withinWeek ? { weekday: "short" } : { weekday: "short", day: "2-digit", month: "2-digit" });
        return `${day} ${time}`;
    }

    get stateText(): string {
        if (this.ringing) {
            return this.translate.translate("UI.VISU.ALARM_CLOCK.RINGING");
        }
        if (this.snoozing) {
            const until = new Date(Date.now() + this.snoozeSeconds * 1000);
            return this.translate.translate("UI.VISU.ALARM_CLOCK.SNOOZING", { time: until.toLocaleTimeString(this.locale, { hour: "2-digit", minute: "2-digit" }) });
        }
        return "";
    }

    dayName(day: number): string {
        // 2023-01-01 was a Sunday
        return new Date(2023, 0, 1 + day).toLocaleDateString(this.locale, { weekday: "short" });
    }

    async snooze() {
        if (this.snoozeInput) {
            await this.dataHub.setValue(this.snoozeInput.ObjId, true);
        }
    }

    async confirm() {
        if (this.confirmInput) {
            await this.dataHub.setValue(this.confirmInput.ObjId, true);
        }
    }

    // ---------- editor (popup) ----------

    async onPopupShowing() {
        // the popup raises onShowing while the template is checked: change the bound state only after this pass
        await Promise.resolve();
        this.resetEdit();
        try {
            const state = await this.operatingModes.getState();
            this.modeItems = (state?.modes ?? []).map(m => ({ key: m.key, name: operatingModeName(m, m.key, k => this.translate.translate(k)) }));
        } catch {
            this.modeItems = [];
        }
    }

    hasDay(entry: AlarmEntry, day: number): boolean {
        return entry.days.includes(day);
    }

    toggleDay(entry: AlarmEntry, day: number) {
        entry.days = this.hasDay(entry, day) ? entry.days.filter(a => a !== day) : [...entry.days, day].sort();
    }

    add() {
        this.edit = [...this.edit, {
            id: Math.random().toString(36).substring(2, 10),
            name: "",
            time: "07:00",
            enabled: true,
            days: [1, 2, 3, 4, 5],
            modes: []
        }];
    }

    remove(entry: AlarmEntry) {
        this.edit = this.edit.filter(a => a !== entry);
    }

    get editDirty(): boolean {
        return JSON.stringify(this.edit) !== this.editBase;
    }

    async save() {
        if (!this.alarmsParam || this.saving) {
            return;
        }
        this.saving = true;
        try {
            const json = JSON.stringify(this.edit.map(a => ({ ...a, time: (a.time || "").substring(0, 5) })));
            await this.dataHub.setValue(this.alarmsParam.ObjId, json);
            this.entries = this.parseEntries(json);
            this.resetEdit();
            this.notifyService.notifySuccess(this.translate.translate("UI.VISU.ALARM_CLOCK.SAVED"));
            setTimeout(() => this.loadData(), 1500);
        } catch (error) {
            this.notifyService.notifyError(this.translate.translate("UI.VISU.ALARM_CLOCK.FAILED"));
        } finally {
            this.saving = false;
        }
    }

    ngOnDestroy(): void {
        this.baseOnDestroy();
    }
}
