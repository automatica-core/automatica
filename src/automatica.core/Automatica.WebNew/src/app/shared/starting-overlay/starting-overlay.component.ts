import { Component, OnInit, NgZone } from "@angular/core";
import { AppService } from "src/app/services/app.service";
import { NotifyService } from "src/app/services/notify.service";
import { L10nTranslationService } from "angular-l10n";
import { BaseComponent } from "src/app/base/base-component";
import { TranslationConfigService } from "src/app/services/translation-config.service";
import { ActivatedRoute, Router } from "@angular/router";

@Component({
  selector: "app-starting-overlay",
  templateUrl: "./starting-overlay.component.html",
  styleUrls: ["./starting-overlay.component.scss"]
})
export class StartingOverlayComponent extends BaseComponent implements OnInit {


  popupVisible: boolean = false;

  constructor(
    appService: AppService,
    notify: NotifyService,
    translation: L10nTranslationService,
    private ngZone: NgZone,
    private translationConfigService: TranslationConfigService,
    private actiavtedRoute: ActivatedRoute,
    private router: Router) {
    super(notify, translation, appService);
  }

  ngOnInit() {
    super.registerEvent(this.appService.isStartingChanged, async (v) => {
      this.ngZone.runOutsideAngular(() => {

        if (this.router.url.endsWith("login")) {
          this.popupVisible = false;
        } else {
          this.popupVisible = v;
        }
      });

      if (!v) {
        await this.translationConfigService.init();
      }
    });
  }

}
