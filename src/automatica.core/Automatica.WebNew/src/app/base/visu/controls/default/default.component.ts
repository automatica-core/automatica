import { Component, OnInit } from "@angular/core";
import { DataHubService } from "../../../communication/hubs/data-hub.service";
import { TranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { BaseMobileComponent } from "../../base-mobile-component";
import { ConfigService } from "src/app/services/config.service";
import { AppService } from "src/app/services/app.service";

@Component({
  selector: "visu-default",
  templateUrl: "./default.component.html",
  styleUrls: ["./default.component.scss"]
})
export class DefaultComponent extends BaseMobileComponent implements OnInit {

  public onItemResized() {
    throw new Error("Method not implemented.");
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
  }

}
