import { Component, OnInit, OnDestroy } from "@angular/core";
import { PropertyInstance } from "../../../../model/property-instance";
import { DataHubService } from "../../../../communication/hubs/data-hub.service";
import { TranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { BaseMobileComponent } from "../../../base-mobile-component";
import { ConfigService } from "src/app/services/config.service";

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


  public onItemResized() {

  }

  constructor(dataHub: DataHubService, notify: NotifyService, translate: TranslationService, configService: ConfigService) {
    super(dataHub, notify, translate, configService);
  }

  protected propertyChanged() {
    this.readOnly = super.getPropertyValue("readonly");

    if (this.editMode) {
      this.readOnly = true;
    } const nodeProperty = this.getProperty("nodeInstance");

    if (!nodeProperty) {
      return;
    }
    this.nodeProperty = nodeProperty;

    super.propertyChanged();
  }

  ngOnInit() {
    super.baseOnInit();
    this.propertyChanged();
  }
  ngOnDestroy() {
    this.baseOnDestroy();
  }

  async onValueChanged($event) {
    if ($event.event && this.nodeProperty.Value) {
      const value = $event.value;

      await this.dataHub.setValue(this.nodeProperty.Value, value);
    }
  }

}
