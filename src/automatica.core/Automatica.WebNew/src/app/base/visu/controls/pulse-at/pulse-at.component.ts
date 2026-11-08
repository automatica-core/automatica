import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";
import { parseLocalDate } from "../alarm-clock/alarm-clock.component";

/** the logic template of "Pulse at" (P3.Logic.Time, "time.pulse_at") */
export const PULSE_AT_TEMPLATE = "5a8a909c-6ab2-4e06-9224-f1da282486d7";

/** the types that use the time of day: time, every day/week/month/year */
const TIME_TYPES = [0, 7, 8, 9, 10];

/**
 * Pulse at tile (Loxone "Pulse At"): the next pulse; the time of day can be changed in the popup, it is written to the
 * logic's "time" parameter (the logic keeps it across restarts).
 */
@Component({
    selector: "visu-pulse-at",
    templateUrl: "./pulse-at.component.html",
    styleUrls: ["./pulse-at.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class PulseAtComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

    private timeParam: RuleInterfaceInstance;
    private nextOutput: RuleInterfaceInstance;
    private pulseOutput: RuleInterfaceInstance;

    next: Date | undefined;
    pulse = false;
    type = 0;
    time = "";
    editTime = "";
    saving = false;

    async ngOnInit() {
        this.baseOnInit();
        super.mobileRuleInit();

        this.timeParam = this.getInterfaceByKey("time");
        this.nextOutput = this.getInterfaceByKey("next");
        this.pulseOutput = this.getInterfaceByKey("pulse") ?? this.getInterfaceByKey("output");

        const next = this.nextOutput ? this.dataHub.getCurrentValue(this.nextOutput.ObjId) : void 0;
        if (next) {
            this.next = parseLocalDate(next.value);
        }
        await this.loadData();
    }

    private async loadData() {
        try {
            const data = await this.ruleInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId);
            if (!data) {
                return;
            }
            const untouched = this.editTime === this.time.substring(0, 5);
            this.time = (data.time ?? "").toString();
            if (untouched) {
                this.editTime = this.time.substring(0, 5);
            }
            this.type = +data.type || 0;
            this.next = parseLocalDate(data.next) ?? this.next;
        } catch (error) {
            console.warn("pulse at: data", error);
        }
    }

    onRuleInstanceValueChanged(interfaceId, value) {
        if (this.nextOutput?.ObjId === interfaceId) {
            this.next = parseLocalDate(value);
        } else if (this.pulseOutput?.ObjId === interfaceId) {
            this.pulse = !!value;
        }
    }

    get editable(): boolean {
        return !!this.timeParam && TIME_TYPES.includes(this.type);
    }

    get nextText(): string {
        if (!this.next) {
            return this.translate.translate("UI.VISU.PULSE_AT.NONE");
        }
        const locale = this.translate.getLocale()?.language || undefined;
        const time = this.next.toLocaleTimeString(locale, { hour: "2-digit", minute: "2-digit" });
        const sameDay = this.next.toDateString() === new Date().toDateString();
        return sameDay ? time : `${this.next.toLocaleDateString(locale, { weekday: "short", day: "2-digit", month: "2-digit" })} ${time}`;
    }

    async onPopupShowing() {
        // the popup raises onShowing while the template is checked: change the bound state only after this pass
        await Promise.resolve();
        this.editTime = (this.time || "07:00").substring(0, 5);
    }

    async save() {
        if (!this.timeParam || this.saving || !/^\d{2}:\d{2}$/.test(this.editTime || "")) {
            return;
        }
        this.saving = true;
        try {
            const value = this.editTime + ":00";
            await this.dataHub.setValue(this.timeParam.ObjId, value);
            this.time = value;
            this.notifyService.notifySuccess(this.translate.translate("UI.VISU.PULSE_AT.SAVED"));
            setTimeout(() => this.loadData(), 1500);
        } catch (error) {
            this.notifyService.notifyError(this.translate.translate("UI.VISU.PULSE_AT.FAILED"));
        } finally {
            this.saving = false;
        }
    }

    ngOnDestroy(): void {
        this.baseOnDestroy();
    }
}
