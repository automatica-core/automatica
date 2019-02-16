import { Component, OnInit } from "@angular/core";
import { AppService } from "src/app/services/app.service";
import { NotifyService } from "src/app/services/notify.service";
import { TranslationService } from "angular-l10n";
import { BaseComponent } from "src/app/base/base-component";

@Component({
  selector: "app-starting-overlay",
  templateUrl: "./starting-overlay.component.html",
  styleUrls: ["./starting-overlay.component.scss"]
})
export class StartingOverlayComponent extends BaseComponent implements OnInit {


  popupVisible: boolean = false;

  constructor(private appService: AppService, notify: NotifyService, translation: TranslationService) {
    super(notify, translation);
  }

  ngOnInit() {
    super.registerEvent(this.appService.isStartingChanged, async (v) => {
      this.popupVisible = v;

      if (!v) {
        await this.translate.init();
      }
    });
  }

}
