import { Component, OnInit, ViewChild, ElementRef } from "@angular/core";
import { PropertyInstance } from "../../../model/property-instance";
import { DataHubService } from "../../../communication/hubs/data-hub.service";
import { TranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { BaseMobileComponent } from "../../base-mobile-component";
import { ConfigService } from "src/app/services/config.service";

@Component({
  selector: "visu-clock",
  templateUrl: "./clock.component.html",
  styleUrls: ["./clock.component.scss"]
})
export class ClockComponent extends BaseMobileComponent implements OnInit {

  constructor(dataHub: DataHubService, notify: NotifyService, translate: TranslationService, configService: ConfigService) {
    super(dataHub, notify, translate, configService);
  }

  public onItemResized() {

  }

  ngOnInit() {

  }


}
