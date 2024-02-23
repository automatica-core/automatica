import { Component, OnInit, OnDestroy, ViewChild, ChangeDetectorRef } from "@angular/core";
import { DxTreeListComponent } from "devextreme-angular";
import { AreaService } from "src/app/services/areas.service";
import { L10nTranslationService } from "angular-l10n";
import { Router, ActivatedRoute } from "@angular/router";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { AreaInstance, AreaTemplate } from "src/app/base/model/areas";
import { CustomMenuItem } from "src/app/base/model/custom-menu-item";
import { GroupsService } from "src/app/services/groups.service";

@Component({
  selector: "app-area-config",
  templateUrl: "./area-config.component.html",
  styleUrls: ["./area-config.component.scss"]
})
export class AreaConfigComponent extends BaseComponent implements OnInit, OnDestroy {
  selectedNode: AreaInstance;
  templates: AreaTemplate[] = [];
  instances: AreaInstance[] = [];

  @ViewChild("tree")
  tree: DxTreeListComponent;

  private mapList: Map<any, AreaInstance> = new Map<any, AreaInstance>();


  menuItems: CustomMenuItem[] = [];

  addItemsMenu: CustomMenuItem[] = void 0;



  menuImportEts: CustomMenuItem = {
    id: "ets-import",
    label: "EtsImport",
    icon: "fa-file-import",
    disabled: true,
    items: undefined,
    command: (event) => { this.importEts(); }
  };
  userGroups: any;

  constructor(
    private areasService: AreaService,
    translationService: L10nTranslationService,
    private userGroupService: GroupsService,
    notify: NotifyService,
    private router: Router,
    private activeRoute: ActivatedRoute,
    appService: AppService) {
    super(notify, translationService, appService);
    this.menuItems = [];


    appService.setAppTitle("AREAS.NAME");

    this.menuItems.push(this.menuImportEts);


    this.translate.onChange().subscribe({
      next: () => {
        this.menuImportEts.label = this.translate.translate("COMMON.ETS_IMPORT");
      }
    });

  }

  async ngOnInit() {
    await this.loadConfig();

    super.registerEvent(this.areasService.etsImported, (data) => {

      const tmpConfig = [];
      for (const node of data) {
        this.mapList.set(node.ObjId, node);
        this.addInstancesRec(node, tmpConfig);

        tmpConfig.push(node);
      }
      tmpConfig.sort((a, b) => (a.Name.localeCompare(b.Name) - (b.Name.localeCompare(a.Name))));
      this.instances = tmpConfig;
    });
  }

  ngOnDestroy() {
    super.baseOnDestroy();
  }

  async loadConfig() {
    this.appService.isLoading = true;
    try {
      const [templates, instances] = await Promise.all(
        [
          this.areasService.getAreaTemplates(),
          this.areasService.getAllAreaInstances()
        ]);

        
      this.userGroups = await this.userGroupService.getUserGroups();
      this.templates = templates;

      this.instances = [];

      const tmpConfig = [];

      for (const node of instances) {
        this.mapList.set(node.ObjId, node);

        tmpConfig.push(node);
      }
      tmpConfig.sort((a, b) => (a.Name.localeCompare(b.Name) - (b.Name.localeCompare(a.Name))));

      this.instances = tmpConfig;
    } catch (error) {
      this.handleError(error);
    }
    this.appService.isLoading = false;
  }

  itemClick($event) {
    const item: CustomMenuItem = $event.itemData;

    if (item && item.command) {
      item.command($event);
    }
  }

  selectNode(node: AreaInstance) {
    if (!node) {
      this.tree.instance.selectRows([], false);
      this.selectedNode = void 0;
      return;
    }

    if (!this.selectedNode || this.selectedNode.ObjId !== node.ObjId) {
      this.tree.instance.selectRows([node.ObjId], false);
      this.selectedNode = node;
      //  this.expandedRowKeys = this.getExpandedRowKeys(node.Id, this.expandedRowKeys); TODO
    }

    this.buildMenuAdd(node);
  }

  selectionChanged($event) {
    this.selectedNode = $event.selectedRowsData[0];
    this.buildMenuAdd(this.selectedNode);
  }

  private buildMenuAdd(data: AreaInstance) {
    this.menuImportEts.disabled = !data;

    if (!data) {
      return;
    }

    this.addItemsMenu = [];


    const supported: AreaTemplate[] = AreaInstance.getSupportedAreaTemplats(data, this.templates);
    this.addSupportedItems(supported);

    const menuItems = this.menuItems;
    this.menuItems = [];
    // this.changeRef.detectChanges();
    this.menuItems = menuItems;
  }

  addSupportedItems(items: AreaTemplate[]) {
    if (!items) {
      return;
    }
    for (const a of items) {
      const menuItem: CustomMenuItem = {
        id: "addNode",
        label: this.translate.translate(a.Name),
        icon: "fa-plus",
        disabled: false,
        command: (event) => { this.addItem(this.selectedNode, a); }
      };
      this.addItemsMenu.push(menuItem);
    }
  }

  addInstancesRec(instance: AreaInstance, tmpConfig: AreaInstance[]): any {
    if (instance.InverseThis2ParentNavigation) {
      for (const x of instance.InverseThis2ParentNavigation) {
        this.mapList.set(x.ObjId, x);
        tmpConfig.push(x);

        this.addInstancesRec(x, tmpConfig);
      }
    }
  }

  async addItem(parent: AreaInstance, template: AreaTemplate) {
    const newInstance = AreaInstance.createFromTemplate(template, parent);
    newInstance.Name = this.translate.translate(newInstance.Name);

    this.instances = [...this.instances, newInstance];

    this.selectNode(newInstance);
    
    this.appService.isLoading = true;
    try {
      this.areasService.addAreaInstances([newInstance]);
    } catch (error) {
      this.handleError(error);
    } finally {
      this.appService.isLoading = false;
    }
  }

  async delete() {
    await this.deleteItem(this.selectedNode);

  }

  importEts(): any {
    this.router.navigate([this.selectedNode.ObjId, "import-ets"], { relativeTo: this.activeRoute });
  }

  async saveSingle(instance: AreaInstance) {

    this.appService.isLoading = true;
    try {
      await this.areasService.saveAreaInstance(instance);
    }
    catch (error) {
      this.handleError(error);
    }
    finally {
      this.appService.isLoading = false;
    }
  }


  async deleteItem(item: AreaInstance) {
    const parentNode = item.This2ParentNavigation;
    this.selectNode(parentNode);

    this.instances = this.instances.filter(a => a.ObjId !== item.ObjId);

    this.appService.isLoading = true;
    try {
      await this.areasService.deleteAreaInstance(item);
    } catch (error) {
      super.handleError(error);
    } finally {
      this.appService.isLoading = false;
    }

  }

  async onChanged($event) {
    var { item } = $event;

    await this.saveSingle(item);
  }

  onContextMenuPreparing($event) {
    const that = this;
    const selected: AreaInstance = $event.row.data;
    const supported = AreaInstance.getSupportedAreaTemplats(selected, this.templates);

    this.selectNode(selected);

    const addMenu = [];
    $event.items = [];

    if (supported) {
      for (const x of supported) {
        addMenu.push({ text: this.translate.translate(x.Name), key: "add", onItemClick: function () { that.addItem(selected, x); } });
      }

      if (addMenu.length > 0) {
        $event.items.push({ text: this.translate.translate("COMMON.NEW"), items: addMenu });
      }
    }


    if (selected.This2AreaTemplateNavigation.IsDeleteable) {
      $event.items.push({ text: this.translate.translate("COMMON.DELETE"), data: selected, onItemClick: function () { that.deleteItem(selected); } });
    }


  }

}
