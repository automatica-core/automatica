import { Component, OnInit, ChangeDetectionStrategy } from "@angular/core";
import { LicenseService } from "src/app/services/license.service";
import { AppService } from "src/app/services/app.service";
import { L10nTranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { BaseComponent } from "src/app/base/base-component";
import { LicenseInfo, parseLicense } from "../system/license-info";

@Component({
    selector: "app-license",
    templateUrl: "./license.component.html",
    styleUrls: ["./license.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class LicenseComponent extends BaseComponent implements OnInit {

  private _license: string = "";
  private savedLicense: string = "";
  info: LicenseInfo;
  loaded = false;
  fileError: string = void 0;

  constructor(
    private licenseService: LicenseService,
    appService: AppService,
    translate: L10nTranslationService,
    notify: NotifyService) {
    super(notify, translate, appService);

    appService.setAppTitle("LICENSE.NAME");
  }

  get license(): string {
    return this._license;
  }
  set license(v: string) {
    this._license = v ?? "";
    this.info = parseLicense(this._license);
  }

  get isDirty() {
    return this.license !== this.savedLicense;
  }

  /** text that is there but is not a license file */
  get isUnreadable() {
    return !!this.license.trim() && !this.info;
  }

  async ngOnInit() {
    const loadingDone = this.appService.beginLoading();
    try {
      const lic = await this.licenseService.getLicense();
      this.license = lic?.License ?? lic?.license ?? "";
      this.savedLicense = this.license;
    } catch (error) {
      this.handleError(error);
    } finally {
      this.loaded = true;
      loadingDone();
    }
  }

  formatDate(d: Date) {
    if (!d) {
      return "";
    }
    return d.toLocaleDateString(this.translate.getLocale()?.language || undefined, { year: "numeric", month: "2-digit", day: "2-digit" });
  }

  async onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files[0];
    this.fileError = void 0;
    if (!file) {
      return;
    }
    try {
      const text = await file.text();
      if (!parseLicense(text)) {
        this.fileError = this.translate.translate("UI.LICENSE.FILE_UNREADABLE");
        return;
      }
      this.license = text;
    } catch (error) {
      this.fileError = this.translate.translate("UI.LICENSE.FILE_UNREADABLE");
    } finally {
      input.value = "";
    }
  }

  revert() {
    this.license = this.savedLicense;
  }

  async save() {
    const loadingDone = this.appService.beginLoading();
    try {
      await this.licenseService.saveLicense(this.license);
      this.savedLicense = this.license;
      this.notifyService.notifySuccess("COMMON.SAVED");
    } catch (error) {
      this.handleError(error);
    } finally {
      loadingDone();
    }
  }

}
