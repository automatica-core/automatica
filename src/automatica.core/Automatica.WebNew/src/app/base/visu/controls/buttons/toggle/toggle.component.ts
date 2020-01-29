import { Component, OnInit, OnDestroy, ChangeDetectorRef } from "@angular/core";
import { PropertyInstance } from "../../../../model/property-instance";
import { DataHubService } from "../../../../communication/hubs/data-hub.service";
import { TranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { BaseMobileComponent } from "../../../base-mobile-component";
import { ConfigService } from "src/app/services/config.service";
import { AppService } from "src/app/services/app.service";

@Component({
  selector: "visu-toggle",
  templateUrl: "./toggle.component.html",
  styleUrls: ["./toggle.component.scss"]
})
export class ToggleComponent extends BaseMobileComponent implements OnInit, OnDestroy {


  private _readOnly: boolean;
  public get readOnly(): boolean {
    return this._readOnly;
  }
  public set readOnly(v: boolean) {
    this._readOnly = v;
  }


  private _nodeProperty: PropertyInstance;
  public get nodeProperty(): PropertyInstance {
    return this._nodeProperty;
  }
  public set nodeProperty(v: PropertyInstance) {
    this._nodeProperty = v;
  }

  private nodeInstanceState: string;

  public get toggle_on_text(): any {
    if (this.item.StateTextValueTrue) {
      return this.item.StateTextValueTrue;
    }

    return this.getPropertyValue("toggle_on_text");
  }
  public get toggle_off_text(): any {
    if (this.item.StateTextValueFalse) {
      return this.item.StateTextValueFalse;
    }

    return this.getPropertyValue("toggle_off_text");
  }


  private _toggleValue: any;
  public get toggleValue(): any {

    return this._toggleValue;
  }

  public set toggleValue(value: any) {
    this._toggleValue = value;
  }

  public onItemResized() {

  }

  constructor(
    dataHub: DataHubService,
    notify: NotifyService,
    translate: TranslationService,
    configService: ConfigService,
    private changeRef: ChangeDetectorRef,
    appService: AppService) {
    super(dataHub, notify, translate, configService, appService);
  }

  protected async propertyChanged() {
    this.readOnly = super.getPropertyValue("readonly");

    if (this.editMode) {
      this.readOnly = true;
    }

    const nodeProperty = this.getProperty("nodeInstance");

    if (!nodeProperty) {
      return;
    }
    this.nodeProperty = nodeProperty;

    this.nodeInstanceState = this.getPropertyValue("nodeInstanceState");
    await this.registerForNodeValues(this.nodeInstanceState)

    super.propertyChanged();
  }

  ngOnInit() {
    super.baseOnInit();
    this.propertyChanged();
  }
  ngOnDestroy() {
    this.baseOnDestroy();
  }

  async onToggleValueChanged($event) {
    if ($event.event && this.nodeProperty.Value) {
      const value = $event.value;
      this.toggleValue = value;
      await this.dataHub.setValue(this.nodeProperty.Value, value);
    }
  }

  protected nodeValueReceived(nodeId: string, value: any): Promise<void> {
    if (this.nodeInstanceState && nodeId === this.nodeInstanceState) {
      this.toggleValue = value;
    }
    if (!this.nodeInstanceState && nodeId === this._primaryNodeInstance) {
      this.toggleValue = value;
    }

    return Promise.resolve();
  }


}
