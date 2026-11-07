import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { NotifyService } from "src/app/services/notify.service";
import { L10nTranslationService } from "angular-l10n";
import { ConfigService } from "src/app/services/config.service";
import { LogicInstanceVisuService } from "src/app/services/logic-visu.service";
import { AppService } from "src/app/services/app.service";
import { RuleInterfaceType } from "src/app/base/model/rule-interface-template";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";


interface SliderData {
  lastValue?: number;
  minValue?: number;
  maxValue?: number;
}

@Component({
    selector: "visu-slider",
    templateUrl: "./slider.component.html",
    styleUrls: ["./slider.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class SliderComponent extends BaseMobileRuleComponent implements OnInit, OnDestroy {

  private _state: number;

  valueType: RuleInterfaceInstance;
  valueMinType: RuleInterfaceInstance;
  valueMaxType: RuleInterfaceInstance;

  outputType: RuleInterfaceInstance;

  public get state(): number {
    return this._state;
  }
  public set state(v: number) {
    this._state = v;
  }


  private _valueMax?: number;
  public get valueMax(): number {
    return this._valueMax;
  }
  public set valueMax(v: number) {
    this._valueMax = v;
  }

  private _valueMin?: number;
  public get valueMin(): number {
    return this._valueMin;
  }
  public set valueMin(v: number) {
    this._valueMin = v;
  }


  constructor(
    dataHubService: DataHubService,
    notify: NotifyService,
    translate: L10nTranslationService,
    configService: ConfigService,
    private logicInstanceVisuService: LogicInstanceVisuService,
    appService: AppService) {
    super(dataHubService, notify, translate, configService, logicInstanceVisuService, appService);
  }


  async ngOnInit() {
    this.baseOnInit();

    super.mobileRuleInit();

    this.valueType = this.getInterfaceByType(RuleInterfaceType.Status);
    this.outputType = this.getInterfaceByType(RuleInterfaceType.Output);

    this.valueMaxType = this.getInterfaceByTypeAndName(RuleInterfaceType.Unknown, "value_max");
    this.valueMinType = this.getInterfaceByTypeAndName(RuleInterfaceType.Unknown, "value_min");

    var data = await <SliderData>this.logicInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId);
    this.valueMax = data.maxValue;
    this.valueMin = data.minValue;


    this.value = this.dataHub.getCurrentValue(this.valueType.ObjId)?.value;
    this.state = this.value;
  }
  format(value) {
    return `${value}`;
  }

  /** the tile is "on" when the value is above the minimum */
  public get isOn(): boolean {
    const v = Number(this.state);
    return !isNaN(v) && this.state !== null && this.state !== void 0 && v > (this.valueMin ?? 0);
  }

  public get levelPercent(): number {
    const v = Number(this.state);
    const min = this.valueMin ?? 0;
    const max = this.valueMax ?? 100;
    if (this.state === null || this.state === void 0 || isNaN(v) || max === min) {
      return void 0;
    }
    return ((v - min) / (max - min)) * 100;
  }

  public get stateText(): string {
    if (this.state === null || this.state === void 0) {
      return void 0;
    }
    const v = Number(this.state);
    return isNaN(v) ? `${this.state}` : new Intl.NumberFormat(void 0, { maximumFractionDigits: 1 }).format(v);
  }

  /** tapping the tile jumps between minimum and maximum, the exact value is in the details popup */
  toggle() {
    if (!this.valueType) {
      return;
    }
    this.state = this.isOn ? (this.valueMin ?? 0) : (this.valueMax ?? 100);
    this.switch(this.state);
  }

  onRuleInstanceValueChanged(interfaceId, value) {
    if (this.outputType && this.outputType.ObjId === interfaceId) {
      this.state = value;
    }
  }

  onValueChanged($event) {
    // e.event is only set for user input, a value pushed by the server must not be written back to the bus
    if (!$event?.event) {
      return;
    }
    if ($event.value === true) {
      this.state = this.valueMax;
      this.switch(this.valueMax);
    }
    else if ($event.value === false) {
      this.state = this.valueMin;
      this.switch(this.valueMin);
    }
  }

  switch($event) {
    // called by the slider's onValueChanged too, which also fires when the server pushes a value
    if ($event && typeof $event === "object" && !$event.event) {
      return;
    }
    if (this.valueType)
      this.dataHub.setValue(this.valueType.ObjId, this.state);
  }


  ngOnDestroy(): void {
    this.baseOnDestroy();
  }


}
