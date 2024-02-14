import { Component, OnInit, ViewChild } from "@angular/core";
import { DxDataGridComponent } from "devextreme-angular";
import { UserGroup } from "src/app/base/model/user/user-group";
import { Role } from "src/app/base/model/user/role";
import { GroupsService } from "src/app/services/groups.service";
import { L10nTranslationService } from "angular-l10n";
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

  constructor(
    private userGroupService: GroupsService,
    translate: L10nTranslationService,
    notify: NotifyService,
    appService: AppService) {

    super(notify, translate, appService);
    appService.setAppTitle("USER_GROUPS.NAME");
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

      this.groups = await this.userGroupService.getUserGroups();
      this.roles = await this.userGroupService.getRoles();

      if (selectedNode) {
        this.selectedNode = this.groups.find(x => x.ObjId === selectedNode);
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
      await this.userGroupService.deleteUserGroup(this.selectedNode);
    }
    catch (error) {
      super.handleError(error);
    }
    this.appService.isLoading = false;
  }

  colorBoxOnValueChanged($event, cell) {
    cell.setValue($event.value);
  }

  async onRowRemoving($event) {
    const data: UserGroup = $event.data;
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

    this.grid.instance.selectRows([newObject.ObjId], false);
    this.selectedNode = newObject;
    

    this.appService.isLoading = true;

    try {

      await this.userGroupService.saveUserGroup(this.selectedNode);
    } catch (error) {
      super.handleError(error);
    } finally {
      this.appService.isLoading = false;
    }
  }
   
  async onChanged($event) {
    var { item } = $event;

    try {
      this.userGroupService.saveUserGroup(item);
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
}
