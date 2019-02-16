import { Component, OnInit, OnDestroy, ViewChild } from "@angular/core";
import { PropertyInstance } from "../../../model/property-instance";
import { DataHubService } from "../../../communication/hubs/data-hub.service";
import { TranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { BaseMobileComponent } from "../../base-mobile-component";
import { ConfigService } from "src/app/services/config.service";
import { ColorOutput } from "src/app/base/color";

@Component({
  selector: "visu-rgb",
  templateUrl: "./rgb.component.html",
  styleUrls: ["./rgb.component.scss"]
})
export class RgbComponent extends BaseMobileComponent implements OnInit, OnDestroy {

  private _readOnly: boolean;
  public get readOnly(): boolean {
    return this._readOnly;
  }
  public set readOnly(v: boolean) {
    this._readOnly = v;
  }


  private _colorValue: ColorOutput;
  public get colorValue(): ColorOutput {
    return this._colorValue;
  }
  public set colorValue(v: ColorOutput) {
    this._colorValue = v;
  }


  private _nodeProperty: PropertyInstance;
  public get nodeProperty(): PropertyInstance {
    return this._nodeProperty;
  }
  public set nodeProperty(v: PropertyInstance) {
    this._nodeProperty = v;
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


  public async colorChange(color: ColorOutput) {
    if (color && this.nodeProperty.Value) {
      const hex = color.hexString;
      await this.dataHub.setValue(this.nodeProperty.Value, hex.substr(1, hex.length));
    }
  }

}
