import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";
import { BaseMobileRuleComponent } from "../../../base-mobile-rule-component";
import { Debounce, formatDateTime, formatTime, safeColor } from "../text-status-helpers";

export interface MonitorState {
  state: number;
  text: string;
  color: string;
  count: number;
}

export interface MonitorObject {
  id: string;
  name: string;
  value: string;
  state: number;
  changed: string;
}

export interface MonitorLogEntry {
  time: string;
  name: string;
  state: number;
  text: string;
}

/** what GetDataForVisu of the status monitor logic returns under "monitor" */
export interface MonitorData {
  states: MonitorState[];
  rest: MonitorState;
  objects: MonitorObject[];
  log: MonitorLogEntry[];
  lastChange: string;
}

export interface MonitorGroup {
  state: MonitorState;
  color: string;
  objects: MonitorObject[];
}

/**
 * Tile of the status monitor logic (string operations plugin): the state with the highest priority that has objects,
 * a chip per state with its count; the popup lists the objects per state and the activity log.
 * Only shows values, it writes nothing; everyone who may see the visu page may see it.
 */
@Component({
    selector: "visu-status-monitor",
    templateUrl: "./status-monitor.component.html",
    styleUrls: ["./status-monitor.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class StatusMonitorComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

  private readonly refresh = new Debounce(300);

  data: MonitorData;
  /** the configured states, then the rest when it has objects */
  chips: MonitorState[] = [];
  groups: MonitorGroup[] = [];
  /** the state with the highest priority (lowest number) that has objects */
  top: MonitorState;

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
      this.apply(data?.monitor);
    } catch (error) {
      console.error("could not load the status monitor", error);
    }
  }

  apply(data: MonitorData | null | undefined) {
    if (!data) {
      return;
    }
    this.data = data;
    const states = data.states ?? [];
    this.top = states.find(a => a.count > 0);

    this.chips = [...states];
    if (data.rest?.count > 0) {
      this.chips.push(data.rest);
    }

    const all = [...states, ...(data.rest ? [data.rest] : [])];
    this.groups = all
      .map(state => ({
        state,
        color: this.color(state),
        objects: (data.objects ?? []).filter(o => o.state === state.state)
      }))
      .filter(g => g.objects.length > 0);
  }

  onRuleInstanceValueChanged(_ruleInterfaceId: string, _value: any) {
    // counts and Txlc change together: read the whole state once
    this.refresh.run(() => this.load());
  }

  async onPopupShowing() {
    await this.load();
  }

  stateText(state: MonitorState | undefined): string {
    if (!state) {
      return "";
    }
    if (state.text) {
      return state.text;
    }
    return state.state === 0 ? this.translate.translate("UI.VISU.STATUS_OTHER") : `#${state.state}`;
  }

  color(state: MonitorState | undefined): string | undefined {
    return safeColor(state?.color);
  }

  colorOf(stateNumber: number): string | undefined {
    if (stateNumber === 0) {
      return this.color(this.data?.rest);
    }
    return this.color(this.data?.states?.find(a => a.state === stateNumber));
  }

  get totalObjects(): number {
    return this.data?.objects?.length ?? 0;
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
