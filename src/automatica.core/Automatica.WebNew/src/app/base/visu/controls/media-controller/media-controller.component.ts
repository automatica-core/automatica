import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";
import { MediaGroup, MediaMode, mediaModes, visibleMediaGroups } from "./media-controller-model";

/**
 * Tile of the media controller logic (universal remote): tap switches power, the popup holds the modes (TV, radio, ...)
 * and the remote keys. Every key writes the matching input of the logic, the logic sends it to the devices of the mode.
 */
@Component({
  selector: "visu-media-controller",
  templateUrl: "./media-controller.component.html",
  styleUrls: ["./media-controller.component.scss"],
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: false
})
export class MediaControllerComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

  readonly digits = [1, 2, 3, 4, 5, 6, 7, 8, 9, 0];

  mode = 0;
  volume: number = undefined;
  channel: number = undefined;
  locked = false;
  modes: MediaMode[] = mediaModes([]);
  groups: Set<MediaGroup> = visibleMediaGroups(new Set());
  showNumpad = false;

  private modeOutput: RuleInterfaceInstance;
  private powerOutput: RuleInterfaceInstance;
  private volumeOutput: RuleInterfaceInstance;
  private channelOutput: RuleInterfaceInstance;

  async ngOnInit() {
    this.baseOnInit();
    await super.mobileRuleInit();

    this.modeOutput = this.getInterfaceByKey("modeOutput");
    this.powerOutput = this.getInterfaceByKey("powerOutput");
    this.volumeOutput = this.getInterfaceByKey("volumeOutput");
    this.channelOutput = this.getInterfaceByKey("channelOutput");

    const linked = new Set<string>((this.ruleInstance?.Interfaces ?? [])
      .filter(a => a.IsLinked)
      .map(a => a.Template?.Key));
    this.groups = visibleMediaGroups(linked);

    await this.refresh();
  }

  private async refresh() {
    if (!this.ruleInstance?.ObjId) {
      return;
    }
    try {
      const data = await this.ruleInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId);
      if (data) {
        this.applyData(data);
      }
    } catch (error) {
      console.error("could not load the media controller values", error);
    }
  }

  applyData(data: any) {
    this.mode = Number(data.mode ?? 0) || 0;
    this.locked = !!data.locked;
    this.modes = mediaModes(data.modeNames);
    this.volume = typeof data.volume === "number" ? data.volume : undefined;
    this.channel = typeof data.channel === "number" ? data.channel : undefined;
  }

  onRuleInstanceValueChanged(interfaceId, value) {
    if (this.modeOutput?.ObjId === interfaceId) {
      this.mode = Number(value) || 0;
    } else if (this.powerOutput?.ObjId === interfaceId && !value) {
      this.mode = 0;
    } else if (this.volumeOutput?.ObjId === interfaceId) {
      const v = Number(value);
      this.volume = isNaN(v) ? this.volume : v;
    } else if (this.channelOutput?.ObjId === interfaceId) {
      const v = Number(value);
      this.channel = isNaN(v) ? this.channel : v;
    }
  }

  get power(): boolean {
    return this.mode > 0;
  }

  /** name of the running mode, its number when it has no name */
  get modeName(): string {
    const mode = this.modes.find(a => a.mode === this.mode);
    return mode?.name || `${this.mode}`;
  }

  has(group: MediaGroup): boolean {
    return this.groups.has(group);
  }

  /** a key of the remote: writes 1 (true) to the input with this key */
  async key(inputKey: string) {
    await this.write(inputKey, true);
  }

  async selectMode(mode: number) {
    await this.write("mode", mode);
  }

  async togglePower() {
    await this.write("powerToggle", true);
  }

  async digit(d: number) {
    await this.write("digit", d);
  }

  async setVolume(event: Event) {
    const v = Number((event.target as HTMLInputElement)?.value);
    if (!isNaN(v)) {
      this.volume = v;
      await this.write("volume", v);
    }
  }

  private async write(inputKey: string, value: any) {
    const input = this.getInterfaceByKey(inputKey);
    if (!input || this.locked) {
      return;
    }
    await this.dataHub.setValue(input.ObjId, value);
  }

  ngOnDestroy(): void {
    this.baseOnDestroy();
  }
}
