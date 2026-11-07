import { Component, OnInit, ChangeDetectionStrategy } from "@angular/core";
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
import { confirm } from "devextreme/ui/dialog";

@Component({
    selector: "app-user-config",
    templateUrl: "./user-config.component.html",
    styleUrls: ["./user-config.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class UserConfigComponent extends BaseComponent implements OnInit {

  users: User[] = [];
  selectedNode: User = void 0;

  userGroups: UserGroup[] = [];
  roles: Role[] = [];

  search = "";
  loaded = false;

  createVisible = false;
  newUser = { UserName: "", FirstName: "", LastName: "" };
  createError: string = void 0;

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

  get filteredUsers(): User[] {
    const q = (this.search || "").trim().toLowerCase();
    return this.users
      .filter(u => !q || [u.UserName, u.FirstName, u.LastName].some(v => (v || "").toLowerCase().includes(q)))
      .sort((a, b) => (a.UserName || "").localeCompare(b.UserName || ""));
  }

  fullName(u: User): string {
    return [u.FirstName, u.LastName].filter(a => !!a).join(" ");
  }

  initials(u: User): string {
    const name = this.fullName(u) || u.UserName || "?";
    return name.split(/[\s._-]+/).filter(a => !!a).slice(0, 2).map(a => a[0].toUpperCase()).join("");
  }

  groupNames(u: User): string {
    const ids = (u.InverseThis2UserGroups || []).map(a => a.This2UserGroup);
    return this.userGroups.filter(g => ids.includes(g.ObjId)).map(g => g.Name).join(", ");
  }

  async load() {
    const loadingDone = this.appService.beginLoading();
    try {

      let selectedNode = void 0;
      if (this.selectedNode) {
        selectedNode = this.selectedNode.ObjId;
      }

      this.users = await this.userService.getUsers();
      this.userGroups = await this.userGroupService.getUserGroups();
      this.roles = await this.userService.getRoles();

      if (selectedNode) {
        this.selectedNode = this.users.find(x => x.ObjId === selectedNode);
      }
    } catch (error) {
      super.handleError(error);
    } finally {
      this.loaded = true;
      loadingDone();
    }
  }

  select(user: User) {
    this.selectedNode = user;
  }

  closeEditor() {
    this.selectedNode = void 0;
  }

  /** asks first, then deletes the selected user on the server */
  async deleteSelected() {
    const user = this.selectedNode;
    if (!user) {
      return;
    }
    const message = this.translate.translate("UI.USERS.DELETE_CONFIRM", { name: user.UserName || "" });
    const ok = await confirm(message, this.translate.translate("UI.USERS.DELETE_TITLE"));
    if (!ok) {
      return;
    }

    const loadingDone = this.appService.beginLoading();
    try {
      await this.userService.deleteUser(user);
      this.users = this.users.filter(a => a !== user);
      this.selectedNode = void 0;
    }
    catch (error) {
      super.handleError(error);
    } finally {
      loadingDone();
    }
  }

  openCreate() {
    this.newUser = { UserName: "", FirstName: "", LastName: "" };
    this.createError = void 0;
    this.createVisible = true;
  }

  async create() {
    const userName = (this.newUser.UserName || "").trim();
    if (!userName) {
      this.createError = this.translate.translate("UI.USERS.USERNAME_REQUIRED");
      return;
    }
    if (this.users.some(a => (a.UserName || "").toLowerCase() === userName.toLowerCase())) {
      this.createError = this.translate.translate("UI.USERS.USERNAME_TAKEN");
      return;
    }

    const newObject = new User();
    newObject.ObjId = Guid.MakeNew().ToString();
    newObject.Description = "";
    newObject.UserName = userName;
    newObject.FirstName = (this.newUser.FirstName || "").trim();
    newObject.LastName = (this.newUser.LastName || "").trim();
    newObject.addVirtualProperties();

    if (await this.saveUser(newObject)) {
      this.users = [...this.users, newObject];
      this.selectedNode = newObject;
      this.createVisible = false;
    }
  }

  /** true if the user was saved on the server */
  private async saveUser(user: User): Promise<boolean> {
    const loadingDone = this.appService.beginLoading();
    try {
      await this.userService.saveUser(user);
      return true;
    } catch (error) {
      this.handleError(error);
      return false;
    } finally {
      loadingDone();
    }
  }

  async onChanged($event) {
    const { item } = $event;

    await this.saveUser(item);
  }

}
