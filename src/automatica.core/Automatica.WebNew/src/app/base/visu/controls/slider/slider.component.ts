import { Component, OnInit, ViewChild, OnDestroy } from "@angular/core";
import { DxSliderComponent } from "devextreme-angular";
import { PropertyInstance } from "../../../model/property-instance";
import { DataHubService } from "../../../communication/hubs/data-hub.service";
import { TranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { BaseMobileComponent } from "../../base-mobile-component";
import { ConfigService } from "src/app/services/config.service";
import { AppService } from "src/app/services/app.service";

@Component({
  selector: "visu-slider",
  templateUrl: "./slider.component.html",
  styleUrls: ["./slider.component.scss"]
})
export class SliderComponent extends BaseMobileComponent implements OnInit, OnDestroy {

  @ViewChild("slider", { static: false })
  slider: DxSliderComponent;

  text: string = void 0;
  value: number = 0;
  min: number = 0;
  max: number = 100;
  readOnly: boolean = false;

  textSize: number = 10;

  nodeProperty: PropertyInstance;

  constructor(
    private dataHubService: DataHubService,
    notify: NotifyService,
    translate: TranslationService,
    configService: ConfigService,
    appService: AppService) {
    super(dataHubService, notify, translate, configService, appService);
  }

  ngOnInit() {
    super.baseOnInit();
    this.propertyChanged();
  }
  ngOnDestroy(): void {
    this.baseOnDestroy();
  }

  protected propertyChanged() {
    this.text = super.getPropertyValue("text");
    this.min = super.getPropertyValue("min");
    this.max = super.getPropertyValue("max");
    this.readOnly = super.getPropertyValue("readonly");
    this.textSize = super.getPropertyValue("text_size");

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

      await this.dataHubService.setValue(this.nodeProperty.Value, value);
    }
  }

  public onItemResized() {
    if (this.slider && this.slider.instance) {
      this.slider.instance.repaint();
    }
  }
}
