import { Component, OnInit, OnDestroy, ChangeDetectorRef } from "@angular/core";
import { Language, TranslationService } from "angular-l10n";
import { IPropertyModel } from "src/app/base/model/interfaces";
import { UserGroup } from "src/app/base/model/user/user-group";
import { VisuService } from "src/app/services/visu.service";
import { AreaService } from "src/app/services/areas.service";
import { GroupsService } from "src/app/services/groups.service";
import { Guid } from "src/app/base/utils/Guid";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { VisuPageType, VisuPage } from "src/app/base/model/visu-page";
import { VisuObjectTemplate } from "src/app/base/model/visu-object-template";
import { VisuObjectInstance } from "src/app/base/model/visu-object-instance";
import { CustomMenuItem } from "src/app/base/model/custom-menu-item";
import { AreaInstance } from "src/app/base/model/areas";
import { VisuObjectMobileInstance } from "src/app/base/model/visu";

@Component({
  selector: "app-visualisation-edit",
  templateUrl: "./visualisation-edit.component.html",
  styleUrls: ["./visualisation-edit.component.scss"]
})
export class VisualisationEditComponent extends BaseComponent implements OnInit, OnDestroy {

  VisuPageTypes: typeof VisuPageType = VisuPageType;

  pages: VisuPage[] = [];

  @Language()
  lang: any;

  public templates: VisuObjectTemplate[] = [];

  private _selectedItem: IPropertyModel;
  public get selectedItem(): IPropertyModel {
    return this._selectedItem;
  }
  public set selectedItem(v: IPropertyModel) {
    if (!v) {
      this.menuDelete.disabled = true;
    } else {
      this.menuDelete.disabled = false;
    }
    this._selectedItem = v;
  }

  private _selectedVisuObject: VisuObjectInstance;
  public get selectedVisuObject(): VisuObjectInstance {
    return this._selectedVisuObject;
  }
  public set selectedVisuObject(v: VisuObjectInstance) {
    this._selectedVisuObject = v;
    this.selectedItem = v;
  }


  private _selectedVisuPage: VisuPage;
  public get selectedVisuPage(): VisuPage {
    return this._selectedVisuPage;
  }
  public set selectedVisuPage(v: VisuPage) {
    this._selectedVisuPage = v;
  }

  menuItems: CustomMenuItem[] = [];
  menuSave: CustomMenuItem = {
    id: "save",
    label: "Save",
    icon: "fa-save",
    items: undefined,
    command: (event) => { this.save(); }
  }

  menuAdd: CustomMenuItem = {
    id: "Add",
    label: "Save",
    icon: "fa-add",
    items: []
  }

  menuDelete: CustomMenuItem = {
    id: "Delete",
    label: "Delete",
    icon: "fa-delete",
    items: [],
    command: (event) => { this.delete(); }
  };
  menuVisuElements: CustomMenuItem = {
    id: "VisuElements",
    label: "VisuElements",
    icon: "fa-delete",
    items: []
  };

  areaInstances: AreaInstance[] = [];
  userGroups: UserGroup[] = [];

  constructor(private visuService: VisuService,
    translate: TranslationService,
    private changeRef: ChangeDetectorRef,
    private notify: NotifyService,
    private areaService: AreaService,
    private userGroupsService: GroupsService, private appService: AppService) {
    super(notify, translate);

    appService.setAppTitle("VISU.NAME");
  }


  async save() {
    this.appService.isLoading = true;
    try {

      await this.visuService.saveVisuPages(this.pages);
      this.notify.notifySuccess("COMMON.SAVED");

    } catch (error) {
      this.notify.notifyError(error);
    }
    this.appService.isLoading = false;
  }

  delete() {
    const selected = this.selectedItem;
    if (selected instanceof VisuPage) {
      this.pages = this.pages.filter(a => a.ObjId !== selected.ObjId);
    } else if (selected instanceof VisuObjectInstance && this.selectedVisuPage) {
      this.selectedVisuPage.VisuObjectInstances = this.selectedVisuPage.VisuObjectInstances.filter(a => a.ObjId !== selected.ObjId);
    }
  }

  addMobile(): any {
    const mobile = new VisuPage();
    mobile.Height = 4;
    mobile.Width = 6;
    mobile.ObjId = Guid.MakeNew().ToString();
    mobile.Name = this.translate.translate("VISU.PAGE.NAME") + ` ${this.pages.length + 1}`;
    mobile.Description = "";
    mobile.This2VisuPageType = VisuPageType.Mobile;

    mobile.DefaultPage = this.pages.length === 0;

    this.pages.push(mobile);
  }

  itemClick($event) {
    const item: CustomMenuItem = $event.itemData;

    if (item && item.command) {
      item.command($event);
    }
  }

  onTabClick($event) {
    this.selectedVisuObject = void 0;
    this.changeRef.detectChanges();

    this.selectedItem = $event.itemData;
    this.selectedVisuPage = $event.itemData;
  }

  async ngOnInit() {

    this.appService.isLoading = true;

    this.menuSave.label = this.translate.translate("COMMON.SAVE");
    this.menuAdd.label = this.translate.translate("COMMON.NEW");
    this.menuDelete.label = this.translate.translate("COMMON.DELETE");
    this.menuVisuElements.label = this.translate.translate("VISU.ELEMENTS");

    this.menuAdd.items.push({
      id: "Add-mobile",
      label: this.translate.translate("VISU.MOBILE.NAME"),
      icon: "fa-add",
      command: (event) => { this.addMobile(); }
    });

    this.menuItems.push(this.menuAdd);
    this.menuItems.push(this.menuSave);
    this.menuItems.push(this.menuDelete);


    try {
      this.userGroups = await this.userGroupsService.getUserGroups();

      this.areaInstances = await this.areaService.getAreaInstances();
      this.templates = await this.visuService.getVisuTemplates();

      for (const x of this.templates) {
        const menu = {
          id: "add-visu-element-" + x.ObjId,
          label: this.translate.translate(x.Name),
          command: (event) => { this.addVisuElement(x); }
        };
        this.menuVisuElements.items.push(menu);
      }

      this.menuItems.push(this.menuVisuElements);

      this.templates = this.templates.filter(a => a.IsVisibleForUser);

      this.pages = await this.visuService.getVisuPages();

      if (this.pages.length > 0) {
        this.selectedVisuPage = this.pages[0];
      }

      for (const x of this.pages) {
        super.registerEvent(x.defaultPageChange, (defaultPage) => {
          for (const y of this.pages) {

            if (x.This2VisuPageType !== y.This2VisuPageType) { // ignore different types
              continue;
            }

            if (x.ObjId === y.ObjId) {
              y.DefaultPage = true;
              continue;
            }
            y.DefaultPage = false;
          }
        });
      }

    } catch (error) {
      console.error(error);
      this.notify.notifyError(error);
      throw error;
    }


    this.appService.isLoading = false;
  }
  addVisuElement(x: VisuObjectTemplate) {
    const instance = VisuObjectMobileInstance.CreateFromTemplate(x);

    instance.Height = instance.VisuObjectTemplate.Height;
    instance.Width = instance.VisuObjectTemplate.Width;

    this._selectedVisuPage.VisuObjectInstances.push(instance);

    this.selectedItem = instance;
  }

  ngOnDestroy() {
    super.baseOnDestroy();
  }

}
