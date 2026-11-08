import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from "@angular/core";
import { AppService } from "src/app/services/app.service";
import { NotifyService } from "src/app/services/notify.service";
import { L10nTranslationService } from "angular-l10n";
import { BaseComponent } from "src/app/base/base-component";
import { TranslationConfigService } from "src/app/services/translation-config.service";
import { Router } from "@angular/router";
import { HubConnectionService } from "src/app/base/communication/hubs/hub-connection.service";

/** short drops (a hub reconnect, a driver reload) are not shown at all */
export const RECONNECT_BANNER_DELAY_MS = 2000;
/** only a long outage (server start, restart after an update) blocks the page */
export const RECONNECT_FULLSCREEN_DELAY_MS = 20000;

export type ReconnectIndication = "none" | "banner" | "fullscreen";

/**
 * Shows that the server is not available (connection lost or the server is (re)starting):
 * nothing for a short drop, then a small banner that does not block the page, the full screen overlay only for a long outage.
 */
@Component({
    selector: "app-starting-overlay",
    templateUrl: "./starting-overlay.component.html",
    styleUrls: ["./starting-overlay.component.scss"],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class StartingOverlayComponent extends BaseComponent implements OnInit, OnDestroy {

  indication: ReconnectIndication = "none";

  private bannerTimer: any;
  private fullscreenTimer: any;

  constructor(
    appService: AppService,
    notify: NotifyService,
    translation: L10nTranslationService,
    private changeRef: ChangeDetectorRef,
    private translationConfigService: TranslationConfigService,
    private hubConnectionService: HubConnectionService,
    private router: Router) {
    super(notify, translation, appService);
  }

  get fullscreenVisible() {
    return this.indication === "fullscreen";
  }

  ngOnInit() {
    super.registerEvent(this.appService.isStartingChanged, async (starting: boolean) => {
      if (starting) {
        this.outageStarted();
      } else {
        await this.outageEnded();
      }
    });
  }

  private outageStarted() {
    this.clearTimers();
    this.bannerTimer = setTimeout(() => this.show("banner"), RECONNECT_BANNER_DELAY_MS);
    this.fullscreenTimer = setTimeout(() => this.show("fullscreen"), RECONNECT_FULLSCREEN_DELAY_MS);
  }

  private async outageEnded() {
    this.clearTimers();
    const wasShown = this.indication !== "none";
    this.show("none");

    if (wasShown) {
      // after a real outage the server may have new plugins (translations) and the hubs may have given up
      await this.translationConfigService.init();
      await this.hubConnectionService.init();
    }
  }

  private show(indication: ReconnectIndication) {
    // the login page has its own connection handling
    if (indication !== "none" && this.router.url.endsWith("login")) {
      indication = "none";
    }
    if (this.indication !== indication) {
      this.indication = indication;
      this.changeRef.markForCheck();
    }
  }

  private clearTimers() {
    clearTimeout(this.bannerTimer);
    clearTimeout(this.fullscreenTimer);
    this.bannerTimer = undefined;
    this.fullscreenTimer = undefined;
  }

  ngOnDestroy() {
    this.clearTimers();
    super.baseOnDestroy();
  }

}
