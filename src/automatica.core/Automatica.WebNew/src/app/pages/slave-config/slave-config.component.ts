import { Component, OnInit } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { Guid } from "src/app/base/utils/Guid";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { CustomMenuItem } from "src/app/base/model/custom-menu-item";
import { Slave } from "src/app/base/model/slaves/slave";
import { SlavesService } from "src/app/services/slaves.services";

@Component({
  selector: "app-slave-config",
  templateUrl: "./slave-config.component.html",
  styleUrls: ["./slave-config.component.scss"]
})
export class SlaveConfigComponent extends BaseComponent implements OnInit {
  slaves: Slave[] = [];
  selectedSlave: Slave = void 0;


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
    private catService: SlavesService,
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
      this.slaves = await this.catService.getSlaves();

    } catch (error) {
      this.handleError(error);
    }

    this.appService.isLoading = false;
  }
  save(): any {
    this.appService.isLoading = true;

    try {
      this.catService.saveSlaves(this.slaves);
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
    const oldData: Slave = $event.oldData;

    if ($event.newData.DisplayName || $event.newData.DisplayDescription) {
      $event.newData.DisplayName = this.translate.translate(oldData.Name);
      $event.newData.DisplayDescription = ""; // oldData.Description;
    }
  }

  onInitNewRow($event) {
    const newObject = new Slave();
    newObject.ObjId = Guid.MakeNew().ToString();
    newObject.addVirtualProperties();

    $event.data = newObject;

  }

  onRowInserting($event) {
    const newObject = new Slave();
    newObject.addVirtualProperties();

    Object.assign(newObject, $event.data);
    $event.data = newObject;
  }

  onRowClicked($event) {
    this.selectedSlave = $event.data;
  }

  onInitialized($event) {
    $event.component.columnOption("command:edit", "width", 150);
  }
}
