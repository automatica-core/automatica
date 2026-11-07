import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";
import { UpdateHubService } from "src/app/base/communication/hubs/update-hub.service";
import { L10nTranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { SystemService } from "src/app/services/system.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { ConfigService } from "src/app/services/config.service";
import { UsersService } from "src/app/services/users.service";
import { LicenseError, LicenseInfo, describeLicenseErrors, parseLicense } from "./license-info";

@Component({
    selector: "app-system",
    templateUrl: "./system.component.html",
    styleUrls: ["./system.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class SystemComponent extends BaseComponent implements OnInit, OnDestroy {

  update: any;
  alreadyDownloaded: any;

  downloadMaxValue = "0";
  downloadValue = "0";
  fileMax: any = void 0;
  fileCurrent: any = 0;

  errorText: any;
  updateError: any;
  updateChecked = false;
  installing = false;

  serverVersion: string = "";

  license: string;
  licenseInfo: LicenseInfo;
  licenseErrors: LicenseError[] = [];
  licenseLoaded = false;
  userCount: number = void 0;

  constructor(private systemService: SystemService,
    private updateHubService: UpdateHubService,
    notifyService: NotifyService,
    translate: L10nTranslationService,
    private configService: ConfigService,
    private usersService: UsersService,
    appService: AppService) {
    super(notifyService, translate, appService);

    appService.setAppTitle("SYSTEM.NAME");
  }

  get licenseState(): "ok" | "expired" | "invalid" | "none" {
    if (!this.licenseInfo) {
      return "none";
    }
    if (this.licenseInfo.isExpired || this.licenseErrors.some(a => a.kind === "expired")) {
      return "expired";
    }
    if (this.licenseErrors.length > 0) {
      return "invalid";
    }
    return "ok";
  }

  get updateVersion(): string {
    return this.update?.version ?? this.update?.Version ?? "";
  }

  formatDate(d: Date) {
    if (!d) {
      return "";
    }
    return d.toLocaleDateString(this.translate.getLocale()?.language || undefined, { year: "numeric", month: "2-digit", day: "2-digit" });
  }

  formatNumber(n: number) {
    if (n === void 0 || n === null) {
      return "";
    }
    return n.toLocaleString(this.translate.getLocale()?.language || undefined);
  }

  protected async load(): Promise<any> {
    const loadingDone = this.appService.beginLoading();
    try {
      // the update check asks the cloud and can be slow, the license must not wait for it
      await Promise.all([this.loadVersion(), this.loadUpdate(), this.loadLicense()]);
    } finally {
      loadingDone();
    }
  }

  private async loadVersion() {
    try {
      const version = await this.configService.getVersion();
      this.serverVersion = version?.version ?? "";
    } catch (error) {
      // the version is only informative
    }
  }

  private async loadUpdate() {
    try {
      this.updateError = void 0;
      const downloaded = await this.systemService.alreadyDownloaded();
      this.alreadyDownloaded = downloaded.result;

      this.update = await this.systemService.checkForUpdate();
    } catch (error) {
      this.updateError = this.parseErrorString(error);
    }
    this.updateChecked = true;
  }

  private async loadLicense() {
    try {
      this.license = await this.systemService.getLicense();
      this.licenseInfo = parseLicense(this.license);
    }
    catch (error) {
      this.handleError(error);
    }

    try {
      this.licenseErrors = describeLicenseErrors(await this.systemService.getLicenseErrors());
    }
    catch (error) {
      this.handleError(error);
    }

    try {
      const users = await this.usersService.getUsers();
      this.userCount = users.length;
    } catch (error) {
      // usage is only informative
    }
    this.licenseLoaded = true;
  }

  async ngOnInit() {
    await this.load();

    super.registerEvent(this.updateHubService.UpdateDownloadProgressChanged, (a) => {
      this.downloadMaxValue = UpdateHubService.formatBytes(a[0][1]);
      this.downloadValue = UpdateHubService.formatBytes(a[0][0]);

      this.fileMax = a[0][1];
      this.fileCurrent = a[0][0];
    });

    super.registerEvent(this.updateHubService.UpdateFailed, (a) => {
      this.errorText = a[0][0];
      this.fileMax = void 0;
    });
    super.registerEvent(this.updateHubService.UpdateFinished, (a) => {
      this.alreadyDownloaded = true;
    });
  }

  ngOnDestroy(): void {
    super.baseOnDestroy();
  }

  async installClick($event) {
    this.installing = true;
    this.notifyService.notifyInfo(this.translate.translate("SYSTEM.INSTALLING_UPDATE"));
    try {
      await this.systemService.installUpdate(this.update);
    } catch (error) {
      this.installing = false;
      this.handleError(error);
    }
  }

  async downloadClick($event) {
    this.errorText = void 0;
    this.notifyService.notifyInfo(this.translate.translate("SYSTEM.DOWNLOADING_UPDATE"));
    await this.updateHubService.downloadUpdate(this.update);
  }

  get downloadPercent(): number {
    if (!this.fileMax) {
      return 0;
    }
    return Math.min(100, Math.round(this.fileCurrent / this.fileMax * 100));
  }
}
