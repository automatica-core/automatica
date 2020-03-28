import { Component, OnInit, OnDestroy } from "@angular/core";
import { UpdateHubService } from "src/app/base/communication/hubs/update-hub.service";
import { L10nTranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { SystemService } from "src/app/services/system.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";

@Component({
  selector: "app-system",
  templateUrl: "./system.component.html",
  styleUrls: ["./system.component.scss"]
})
export class SystemComponent extends BaseComponent implements OnInit, OnDestroy {

  update: any;
  alreadyDownloaded: any;

  downloadMaxValue = "0";
  downloadValue = "0";
  fileMax: any = void 0;
  fileCurrent: any = 0;

  errorText: any;

  constructor(private systemService: SystemService,
    private updateHubService: UpdateHubService,
    notifyService: NotifyService,
    translate: L10nTranslationService,
    appService: AppService) {
    super(notifyService, translate, appService);

    appService.setAppTitle("SYSTEM.NAME");
  }

  async ngOnInit() {
    this.appService.isLoading = true;
    try {
      const downloaded = await this.systemService.alreadyDownloaded();
      this.alreadyDownloaded = downloaded.result;

      this.update = await this.systemService.checkForUpdate();
    } catch (error) {
      super.handleError(error);
    }
    this.appService.isLoading = false;

    super.registerEvent(this.updateHubService.UpdateDownloadProgressChanged, (a) => {
      this.downloadMaxValue = UpdateHubService.formatBytes(a[0][1]);
      this.downloadValue = UpdateHubService.formatBytes(a[0][0]);

      this.fileMax = a[0][1];
      this.fileCurrent = a[0][0];
    });

    super.registerEvent(this.updateHubService.UpdateFailed, (a) => {
      this.errorText = a[0][0];
    });
    super.registerEvent(this.updateHubService.UpdateFinished, (a) => {
      this.alreadyDownloaded = true;
    });
  }

  ngOnDestroy(): void {
    super.baseOnDestroy();
  }

  async installClick($event) {
    await this.systemService.installUpdate(this.update);
  }

  async downloadClick($event) {
    await this.updateHubService.downloadUpdate(this.update);
  }

  format = (value) => {
    if (this.fileCurrent === this.fileMax) {
      return this.translate.translate("COMMON.DOWNLOADED");
    }
    return this.translate.translate("COMMON.DOWNLOADING");
  }
}
