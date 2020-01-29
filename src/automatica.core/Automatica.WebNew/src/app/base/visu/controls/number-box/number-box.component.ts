import { Component, OnInit, OnDestroy } from "@angular/core";
import { PropertyInstance } from "../../../model/property-instance";
import { DataHubService } from "../../../communication/hubs/data-hub.service";
import { TranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { BaseMobileComponent } from "../../base-mobile-component";
import { ConfigService } from "src/app/services/config.service";
import { AppService } from "src/app/services/app.service";

@Component({
  // tslint:disable-next-line:component-selector
  selector: "visu-number-box",
  templateUrl: "./number-box.component.html",
  styleUrls: ["./number-box.component.scss"]
})
export class NumberBoxComponent extends BaseMobileComponent implements OnInit, OnDestroy {

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

  constructor(
    dataHub: DataHubService,
    notify: NotifyService,
    translate: TranslationService,
    configService: ConfigService,
    appService: AppService) {
    super(dataHub, notify, translate, configService, appService);
  }

  ngOnInit() {
    super.baseOnInit();
    this.propertyChanged();
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

  async onValueChanged($event) {
    if ($event.event && this.nodeProperty.Value) {
      const value = $event.value;

      await this.dataHub.setValue(this.nodeProperty.Value, value);
    }
  }

  ngOnDestroy() {
    this.baseOnDestroy();
  }

  public onItemResized() {

  }
}
