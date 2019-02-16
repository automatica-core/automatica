import { Component, OnInit, ViewChild } from "@angular/core";
import { DxDataGridComponent } from "devextreme-angular";
import { UserGroup } from "src/app/base/model/user/user-group";
import { Role } from "src/app/base/model/user/role";
import { GroupsService } from "src/app/services/groups.service";
import { TranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { Guid } from "src/app/base/utils/Guid";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { CustomMenuItem } from "src/app/base/model/custom-menu-item";

@Component({
  selector: "app-usergroup-config",
  templateUrl: "./usergroup-config.component.html",
  styleUrls: ["./usergroup-config.component.scss"]
})
export class UsergroupConfigComponent extends BaseComponent implements OnInit {

  @ViewChild("grid")
  grid: DxDataGridComponent;
  
  groups: UserGroup[] = [];
  roles: Role[] = [];
  selectedNode: UserGroup = void 0;

  menuItems: CustomMenuItem[] = [];

  menuSave: CustomMenuItem = {
    id: "save",
    label: "Save",
    icon: "fa-save",
    items: undefined,
    command: (event) => { this.save(); }
  }

  constructor(private catService: GroupsService, translate: TranslationService, private notify: NotifyService, private appService: AppService) {
    super(notify, translate);
    this.menuItems.push(this.menuSave);
    this.menuSave.label = translate.translate("COMMON.SAVE");

    appService.setAppTitle("USER_GROUPS.NAME");
  }

  async ngOnInit() {
    this.appService.isLoading = true;

    try {
      this.groups = await this.catService.getUserGroups();
      this.roles = await this.catService.getRoles();


    } catch (error) {
      super.handleError(error);
    }

    this.appService.isLoading = false;
  }
  async save() {
    this.appService.isLoading = true;

    try {

      await this.catService.saveUserGroups(this.groups);

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
    const data: UserGroup = $event.data;
  }

  itemClick($event) {
    const item: CustomMenuItem = $event.itemData;

    if (item && item.command) {
      item.command($event);
    }
  }

  onRowUpdating($event) {

  }

  onInitNewRow($event) {
    const newObject = new UserGroup();
    newObject.ObjId = Guid.MakeNew().ToString();
    newObject.addVirtualProperties();

    $event.data = newObject;

  }

  async onRowInserting($event) {
    const newObject = new UserGroup();
    newObject.addVirtualProperties();

    Object.assign(newObject, $event.data);
    $event.data = newObject;

    // await this.grid.instance.selectRows([newObject.ObjId], false);
    this.selectedNode = newObject;
  }

  onRowClicked($event) {
    this.selectedNode = $event.data;
  }

  onInitialized($event) {
    $event.component.columnOption("command:edit", "width", 150);
  }
}
