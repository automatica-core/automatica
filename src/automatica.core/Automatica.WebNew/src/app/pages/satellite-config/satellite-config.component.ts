import { Component, OnInit } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { Guid } from "src/app/base/utils/Guid";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { CustomMenuItem } from "src/app/base/model/custom-menu-item";
import { Satellite } from "src/app/base/model/satellites/satellite";
import { SatelliteService } from "src/app/services/satellite.services";

@Component({
  selector: "app-satellite-config",
  templateUrl: "./satellite-config.component.html",
  styleUrls: ["./satellite-config.component.scss"]
})
export class SatelliteConfigComponent extends BaseComponent implements OnInit {
  satellites: Satellite[] = [];
  selectedSatellite: Satellite = void 0;


  menuItems: CustomMenuItem[] = [];

  menuSave: CustomMenuItem = {
    id: "save",
    label: "Save",
    icon: "fa-save",
    items: undefined,
    command: (event) => { this.save(); }
  }  
  
  menuReload: CustomMenuItem = {
    id: "reload",
    label: "Reload",
    icon: "fa-save",
    items: undefined,
    command: (event) => { this.load(); }
  }

  constructor(
    private catService: SatelliteService,
    translate: L10nTranslationService,
    private notify: NotifyService,
    appService: AppService) {
    super(notify, translate, appService);

    appService.setAppTitle("CATEGORIES.NAME");

    this.menuItems.push(this.menuSave);
    this.menuItems.push(this.menuReload);

    this.translate.onChange().subscribe({
      next: () => {
        this.menuSave.label = translate.translate("COMMON.SAVE");
      }
    });

  }

  async ngOnInit() {
    this.load();
  }

  async load() {
    this.appService.isLoading = true;

    try {
      this.satellites = await this.catService.getAll();

    } catch (error) {
      this.handleError(error);
    }

    this.appService.isLoading = false;
  }
  save(): any {
    this.appService.isLoading = true;

    try {
      this.catService.save(this.satellites);
      this.notify.notifySuccess("COMMON.SAVED");
    } catch (error) {
      super.handleError(error);
    }

    this.appService.isLoading = false;
  }

  colorBoxOnValueChanged($event, cell) {
    cell.setValue($event.value);
  }
  onRowRemoving($event) {

  }

  itemClick($event) {
    const item: CustomMenuItem = $event.itemData;

    if (item && item.command) {
      item.command($event);
    }
  }

  onRowUpdating($event) {
    const oldData: Satellite = $event.oldData;

    if ($event.newData.DisplayName || $event.newData.DisplayDescription) {
      $event.newData.DisplayName = this.translate.translate(oldData.Name);
      $event.newData.DisplayDescription = ""; // oldData.Description;
    }
  }

  onInitNewRow($event) {
    const newObject = new Satellite();
    newObject.ObjId = Guid.MakeNew().ToString();
    newObject.addVirtualProperties();

    $event.data = newObject;

  }

  onRowInserting($event) {
    const newObject = new Satellite();
    newObject.addVirtualProperties();

    Object.assign(newObject, $event.data);
    $event.data = newObject;
  }

  onRowClicked($event) {
    this.selectedSatellite = $event.data;
  }

  onInitialized($event) {
    $event.component.columnOption("command:edit", "width", 150);
  }
}
