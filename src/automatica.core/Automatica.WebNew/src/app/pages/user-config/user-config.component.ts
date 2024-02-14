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


  constructor(private userService: UsersService,
    translate: L10nTranslationService,
    notify: NotifyService,
    private userGroupService: GroupsService,
    appService: AppService) {

    super(notify, translate, appService);

    appService.setAppTitle("USERS.NAME");
  }

  async ngOnInit() {
    await this.load();
  }

  async load() {
    this.appService.isLoading = true;
    try {

      var selectedNode = void 0;
      if (this.selectedNode) {
        selectedNode = this.selectedNode.ObjId;
      }

      this.users = await this.userService.getUsers();
      this.userGroups = await this.userGroupService.getUserGroups();
      this.roles = await this.userService.getRoles();

      if (selectedNode) {
        this.selectedNode = this.users.find(x => x.ObjId === selectedNode);
        this.grid.instance.selectRows([selectedNode], false);
      }
    } catch (error) {
      super.handleError(error);
    }
    this.appService.isLoading = false;
  }

  async delete() {
    this.appService.isLoading = true;
    try {
      await this.userService.deleteUser(this.selectedNode);
    }
    catch (error) {
      super.handleError(error);
    }
    this.appService.isLoading = false;
  }


  colorBoxOnValueChanged($event, cell) {
    cell.setValue($event.value);
  }
  onRowRemoving($event) {
    const data: User = $event.data;
    this.selectedNode = data;
    this.delete();
  }

  itemClick($event) {
    const item: CustomMenuItem = $event.itemData;

    if (item && item.command) {
      item.command($event);
    }
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

    newObject.UserName = $event.data.UserName;
    newObject.Description = $event.data.Description;
    newObject.FirstName = $event.data.FirstName;
    newObject.LastName = $event.data.LastName;

    $event.data = newObject;

    this.grid.instance.selectRows([newObject.ObjId], false);
    this.selectedNode = newObject;

    this.appService.isLoading = true;
    try {
      this.userService.saveUser(newObject);
    } catch (error) {
      this.handleError(error);
    } finally {
      this.appService.isLoading = false;
    }
  }

  onRowClicked($event) {
    this.selectedNode = $event.data;
  }

  onInitialized($event) {
    $event.component.columnOption("command:edit", "width", 150);
  }

  
  async onChanged($event) {
    var { item } = $event;

    try {
      this.userService.saveUser(item);
    } catch (error) {
      this.handleError(error);
    } finally {
      this.appService.isLoading = false;
    }
  }

}
