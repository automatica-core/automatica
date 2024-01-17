import { Component, OnInit, OnDestroy } from "@angular/core";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { NotifyService } from "src/app/services/notify.service";
import { L10nTranslationService } from "angular-l10n";
import { ConfigService } from "src/app/services/config.service";
import { AppService } from "src/app/services/app.service";
import { BaseMobileComponent } from "../../../base-mobile-component";


@Component({
  selector: "visu-toggle-node",
  templateUrl: "./toggle.component.html",
  styleUrls: ["./toggle.component.scss"]
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

    return this.value;
  }

  onValueChanged($event) {
    this.switch($event.value);
  }

  ngOnDestroy(): void {
    this.baseOnDestroy();
  }


  switch(value) {
    this.dataHub.setValue(this.getPropertyValue("nodeInstance"), value);
  }

}
