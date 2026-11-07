import { Component, OnInit, ChangeDetectionStrategy } from "@angular/core";
import { UserGroup } from "src/app/base/model/user/user-group";
import { Role } from "src/app/base/model/user/role";
import { GroupsService } from "src/app/services/groups.service";
import { L10nTranslationService } from "angular-l10n";
import { NotifyService } from "src/app/services/notify.service";
import { Guid } from "src/app/base/utils/Guid";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { confirm } from "devextreme/ui/dialog";

@Component({
    selector: "app-usergroup-config",
    templateUrl: "./usergroup-config.component.html",
    styleUrls: ["./usergroup-config.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class UsergroupConfigComponent extends BaseComponent implements OnInit {

  groups: UserGroup[] = [];
  roles: Role[] = [];
  selectedNode: UserGroup = void 0;

  search = "";
  loaded = false;

  createVisible = false;
  newGroup = { Name: "", Description: "" };
  createError: string = void 0;

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

  get filteredGroups(): UserGroup[] {
    const q = (this.search || "").trim().toLowerCase();
    return this.groups
      .filter(g => !q || [g.Name, g.Description].some(v => (v || "").toLowerCase().includes(q)))
      .sort((a, b) => (a.Name || "").localeCompare(b.Name || ""));
  }

  roleCount(g: UserGroup): number {
    return (g.InverseThis2Roles || []).length;
  }

  async load() {
    const loadingDone = this.appService.beginLoading();

    try {
      let selectedNode = void 0;
      if (this.selectedNode) {
        selectedNode = this.selectedNode.ObjId;
      }

      this.groups = await this.userGroupService.getUserGroups();
      this.roles = await this.userGroupService.getRoles();

      if (selectedNode) {
        this.selectedNode = this.groups.find(x => x.ObjId === selectedNode);
      }

    } catch (error) {
      super.handleError(error);
    } finally {
      this.loaded = true;
      loadingDone();
    }
  }

  select(group: UserGroup) {
    this.selectedNode = group;
  }

  closeEditor() {
    this.selectedNode = void 0;
  }

  /** asks first, then deletes the selected group on the server */
  async deleteSelected() {
    const group = this.selectedNode;
    if (!group) {
      return;
    }
    const message = this.translate.translate("UI.USER_GROUPS.DELETE_CONFIRM", { name: group.Name || "" });
    const ok = await confirm(message, this.translate.translate("UI.USER_GROUPS.DELETE_TITLE"));
    if (!ok) {
      return;
    }

    const loadingDone = this.appService.beginLoading();
    try {
      await this.userGroupService.deleteUserGroup(group);
      this.groups = this.groups.filter(a => a !== group);
      this.selectedNode = void 0;
    }
    catch (error) {
      super.handleError(error);
    } finally {
      loadingDone();
    }
  }

  openCreate() {
    this.newGroup = { Name: "", Description: "" };
    this.createError = void 0;
    this.createVisible = true;
  }

  async create() {
    const name = (this.newGroup.Name || "").trim();
    if (!name) {
      this.createError = this.translate.translate("UI.USER_GROUPS.NAME_REQUIRED");
      return;
    }

    const newObject = new UserGroup();
    newObject.ObjId = Guid.MakeNew().ToString();
    newObject.Name = name;
    newObject.Description = (this.newGroup.Description || "").trim();
    newObject.addVirtualProperties();

    if (await this.saveUserGroup(newObject)) {
      this.groups = [...this.groups, newObject];
      this.selectedNode = newObject;
      this.createVisible = false;
    }
  }

  /** true if the group was saved on the server */
  private async saveUserGroup(group: UserGroup): Promise<boolean> {
    const loadingDone = this.appService.beginLoading();
    try {
      await this.userGroupService.saveUserGroup(group);
      return true;
    } catch (error) {
      super.handleError(error);
      return false;
    } finally {
      loadingDone();
    }
  }

  async onChanged($event) {
    const { item } = $event;

    await this.saveUserGroup(item);
  }
}
