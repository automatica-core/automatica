import { Component, OnInit } from "@angular/core";
import { LicenseService } from "src/app/services/license.service";
import { AppService } from "src/app/services/app.service";
import { L10nTranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { BaseComponent } from "src/app/base/base-component";
import { CustomMenuItem } from "src/app/base/model/custom-menu-item";

@Component({
  selector: "app-license",
  templateUrl: "./license.component.html",
  styleUrls: ["./license.component.scss"]
})
export class LicenseComponent extends BaseComponent implements OnInit {

  menuItems: CustomMenuItem[] = [];

  menuSave: CustomMenuItem = {
    id: "save",
    label: "Save",
    icon: "fa-save",
    items: undefined,
    command: (event) => { this.save(); }
  }


  license: string = "";
  constructor(
    private licenseService: LicenseService,
    appService: AppService,
    translate: L10nTranslationService,
    notify: NotifyService) {
    super(notify, translate, appService);

    appService.setAppTitle("LICENSE.NAME");

    this.menuItems.push(this.menuSave);
    this.menuSave.label = translate.translate("COMMON.SAVE");
  }

  async ngOnInit() {
    const lic = await this.licenseService.getLicense();
    this.license = lic.License;
  }


  itemClick($event) {
    const item: CustomMenuItem = $event.itemData;

    if (item && item.command) {
      item.command($event);
    }
  }

  async save() {
    try {
      await this.licenseService.saveLicense(this.license);
      this.notifyService.notifySuccess("COMMON.SAVED");
    } catch (error) {
      this.handleError(error);
    }
  }

}
