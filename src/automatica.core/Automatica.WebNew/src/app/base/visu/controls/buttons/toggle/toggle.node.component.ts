import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { NotifyService } from "src/app/services/notify.service";
import { L10nTranslationService } from "angular-l10n";
import { ConfigService } from "src/app/services/config.service";
import { AppService } from "src/app/services/app.service";
import { BaseMobileComponent } from "../../../base-mobile-component";


@Component({
    selector: "visu-toggle-node",
    templateUrl: "./toggle.component.html",
    styleUrls: ["./toggle.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ToggleNodeComponent extends BaseMobileComponent implements OnInit, OnDestroy {

  readOnly: boolean = false;

  constructor(
    dataHubService: DataHubService,
    notify: NotifyService,
    translate: L10nTranslationService,
    configService: ConfigService,
    appService: AppService) {
    super(dataHubService, notify, translate, configService, appService);
  }


  async ngOnInit() {
    this.baseOnInit();

    this.readOnly = this.getReadOnly() ?? false;

    
    var cachedValue = this.dataHub.getCurrentValue(this.getPropertyValue("nodeInstance"));
    this.value = cachedValue?.value;
  }

  public get displayValue() {
    if (this.value === void 0) {
      return void 0;
    }

    // a plain boolean without state texts reads "Ein" / "Aus"
    if (typeof this.realValue === "boolean" && this.value === this.realValue) {
      return this.translate.translate(this.realValue ? "COMMON.TOGGLE.ON" : "COMMON.TOGGLE.OFF");
    }

    return this.value;
  }

  public get isOn(): boolean {
    return this.realValue === true || this.realValue === 1 || this.realValue === "true";
  }

  onValueChanged($event) {
    this.switch($event.value);
  }

  /** the whole tile is the switch */
  toggle() {
    if (this.readOnly) {
      return;
    }
    this.switch(!this.isOn);
  }

  ngOnDestroy(): void {
    this.baseOnDestroy();
  }


  switch(value) {
    this.dataHub.setValue(this.getPropertyValue("nodeInstance"), value);
  }

}
