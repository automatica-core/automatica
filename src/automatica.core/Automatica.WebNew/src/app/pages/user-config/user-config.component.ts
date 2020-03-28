import { Component, OnInit, ViewChild } from "@angular/core";
import { DxDataGridComponent } from "devextreme-angular";
import { User } from "src/app/base/model/user/user";
import { UserGroup } from "src/app/base/model/user/user-group";
import { Role } from "src/app/base/model/user/role";
import { L10nTranslationService } from "angular-l10n";
import { GroupsService } from "src/app/services/groups.service";
import { Guid } from "src/app/base/utils/Guid";
import { UsersService } from "src/app/services/users.service";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { CustomMenuItem } from "src/app/base/model/custom-menu-item";

@Component({
  selector: "app-user-config",
  templateUrl: "./user-config.component.html",
  styleUrls: ["./user-config.component.scss"]
})
export class UserConfigComponent extends BaseComponent implements OnInit {


  @ViewChild("grid")
  grid: DxDataGridComponent;

  users: User[] = [];
  selectedNode: User = void 0;

  userGroups: UserGroup[] = [];
  roles: Role[] = [];


  menuItems: CustomMenuItem[] = [];

  menuSave: CustomMenuItem = {
    id: "save",
    label: "Save",
    icon: "fa-save",
    items: undefined,
    command: (event) => { this.save(); }
  }

  constructor(private catService: UsersService,
    translate: L10nTranslationService,
    private notify: NotifyService,
    private userGroupService: GroupsService,
    appService: AppService) {

    super(notify, translate, appService);
    this.menuItems.push(this.menuSave);
    this.menuSave.label = translate.translate("COMMON.SAVE");

    appService.setAppTitle("USERS.NAME");
  }

  async ngOnInit() {
    this.appService.isLoading = true;

    try {
      this.users = await this.catService.getUsers();

      this.userGroups = await this.userGroupService.getUserGroups();

      this.roles = await this.catService.getRoles();


    } catch (error) {
      super.handleError(error);
    }

    this.appService.isLoading = false;
  }
  async save() {
    this.appService.isLoading = true;

    try {


      let validated = true;
      for (const newUser of this.users) {
        if (!newUser.validate()) {
          validated = false;
        }
      }

      if (!validated) {
        this.notify.notifyError("INVALID.VALIDATED");
        this.appService.isLoading = false;
        return;
      }

      await this.catService.saveUsers(this.users);

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
    const data: User = $event.data;
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
    const newObject = new User();
    newObject.Description = "";
    newObject.ObjId = Guid.MakeNew().ToString();
    newObject.addVirtualProperties();

    $event.data = newObject;

  }

  onRowInserting($event) {
    const newObject = new User();
    newObject.addVirtualProperties();

    Object.assign(newObject, $event.data);
    $event.data = newObject;

    this.grid.instance.selectRows([newObject.ObjId], false);
    this.selectedNode = newObject;
  }

  onRowClicked($event) {
    this.selectedNode = $event.data;
  }

  onInitialized($event) {
    $event.component.columnOption("command:edit", "width", 150);
  }

}
