import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, HostBinding, inject } from "@angular/core";
import { FaIconLibrary } from "@fortawesome/angular-fontawesome";
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import { BaseMobileRuleComponent } from "../../../base-mobile-rule-component";
import { Debounce, parseIcon, safeColor } from "../text-status-helpers";

/** what GetDataForVisu of the status logic returns under "status" */
export interface StatusData {
  rule: number;
  text: string;
  value: number;
  icon: string;
  color: string;
}

const FALLBACK_ICON: IconProp = "circle-info";

/**
 * Tile of the status logic (string operations plugin): the status text of the rule that applies, with its icon and color.
 * Only shows values, it writes nothing; everyone who may see the visu page may see it.
 */
@Component({
    selector: "visu-status",
    templateUrl: "./status.component.html",
    styleUrls: ["./status.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class StatusComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

  private readonly library = inject(FaIconLibrary);
  private readonly refresh = new Debounce(250);
  private textId: string;

  status: StatusData;
  statusIcon: IconProp = FALLBACK_ICON;

  @HostBinding("style.--status-color")
  statusColor: string;

  @HostBinding("class.has-color")
  get hasColor(): boolean {
    return !!this.statusColor;
  }

  async ngOnInit() {
    this.baseOnInit();
    await this.mobileRuleInit();

    this.textId = this.getInterfaceByKey("text")?.ObjId?.toLowerCase();
    if (this.textId) {
      const current = this.dataHub.getCurrentValue(this.textId);
      if (current) {
        this.value = current.value;
      }
    }
    await this.load();
  }

  async ngOnDestroy() {
    this.refresh.cancel();
    this.baseOnDestroy();
  }

  /** the text shown, "no status yet" before the logic got its first value */
  get statusText(): string {
    const text = this.value ?? this.status?.text;
    return text === undefined || text === null ? "" : String(text);
  }

  async load() {
    if (!this.ruleInstance?.ObjId) {
      return;
    }
    try {
      const data = await this.ruleInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId);
      this.apply(data?.status);
    } catch (error) {
      // the tile still shows the live text
      console.error("could not load the status", error);
    }
  }

  apply(status: StatusData | null | undefined) {
    if (!status) {
      return;
    }
    this.status = status;
    this.value = status.text;
    this.statusIcon = parseIcon(this.library, status.icon, FALLBACK_ICON);
    this.statusColor = safeColor(status.color);
  }

  onRuleInstanceValueChanged(ruleInterfaceId: string, value: any) {
    if (String(ruleInterfaceId).toLowerCase() === this.textId) {
      this.value = value;
    }
    // icon and color belong to the rule: read them once all outputs are through
    this.refresh.run(() => this.load());
  }
}
