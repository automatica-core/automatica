import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";
import { BaseMobileRuleComponent } from "../../../base-mobile-rule-component";
import { Debounce, formatDateTime, formatTime } from "../text-status-helpers";

export interface TrackerEntry {
  time: string;
  text: string;
  source: string;
}

/**
 * Tile of the tracker logic (string operations plugin): the newest entry with its time on the tile, all kept entries
 * in the popup. Only shows values, it writes nothing; everyone who may see the visu page may see it.
 */
@Component({
    selector: "visu-tracker",
    templateUrl: "./tracker.component.html",
    styleUrls: ["./tracker.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class TrackerComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

  private readonly refresh = new Debounce(300);

  entries: TrackerEntry[] = [];

  async ngOnInit() {
    this.baseOnInit();
    await this.mobileRuleInit();
    await this.load();
  }

  async ngOnDestroy() {
    this.refresh.cancel();
    this.baseOnDestroy();
  }

  async load() {
    if (!this.ruleInstance?.ObjId) {
      return;
    }
    try {
      const data = await this.ruleInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId);
      this.apply(data?.entries);
    } catch (error) {
      console.error("could not load the tracker entries", error);
    }
  }

  apply(entries: TrackerEntry[] | null | undefined) {
    this.entries = Array.isArray(entries) ? entries : [];
  }

  onRuleInstanceValueChanged(_ruleInterfaceId: string, _value: any) {
    // a new entry changes the last text and the count: read the list once
    this.refresh.run(() => this.load());
  }

  async onPopupShowing() {
    await this.load();
  }

  get latest(): TrackerEntry | undefined {
    return this.entries[0];
  }

  get countText(): string {
    return this.entries.length > 0 ? this.translate.translate("UI.VISU.TRACKER_COUNT", { count: this.entries.length }) : "";
  }

  private get locale(): string {
    return this.translate?.getLocale?.()?.language || "en";
  }

  time(value: string): string {
    return formatTime(value, this.locale);
  }

  fullTime(value: string): string {
    return formatDateTime(value, this.locale);
  }
}
