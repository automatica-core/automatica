import { Component, OnInit, ViewChild, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from "@angular/core";
import { RulePage } from "src/app/base/model/rule-page";
import { ConfigTreeComponent } from "src/app/shared/config-tree/config-tree.component";
import { UserGroup } from "src/app/base/model/user/user-group";
import { LogicEngineService } from "src/app/services/logicengine.service";
import { ConfigService } from "src/app/services/config.service";
import { L10nTranslationService } from "angular-l10n";
import { AreaService } from "src/app/services/areas.service";
import { CategoryService } from "src/app/services/categories.service";
import { GroupsService } from "src/app/services/groups.service";
import { Guid } from "src/app/base/utils/Guid";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { NodeInstance2RulePage } from "src/app/base/model/node-instance-2-rule-page";
import { BaseComponent } from "src/app/base/base-component";
import { RuleTemplate } from "src/app/base/model/rule-template";
import { NodeInstance } from "src/app/base/model/node-instance";
import { CustomMenuItem } from "src/app/base/model/custom-menu-item";
import { AreaInstance } from "src/app/base/model/areas";
import { CategoryInstance } from "src/app/base/model/categories";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { RuleInstance } from "src/app/base/model/rule-instance";
import { LogicEditorInstanceService } from "src/app/services/logic-editor-instance.service";
import DataSource from "devextreme/data/data_source";
import { DxListComponent, DxPopupComponent } from "devextreme-angular";
import { ActivatedRoute, Router } from "@angular/router";
import { HubConnectionService } from "src/app/base/communication/hubs/hub-connection.service";
import { ITreeNode } from "src/app/base/model/ITreeNode";

@Component({
  selector: "app-logic-editor",
  templateUrl: "./logic-editor.component.html",
  styleUrls: ["./logic-editor.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LogicEditorComponent extends BaseComponent implements OnInit, OnDestroy {

  public pages: RulePage[] = [];
  public pagesDataSource: DataSource = void 0;
  ruleTemplates: RuleTemplate[];
  linkableNodes: NodeInstance[] = [];

  selectedPage: RulePage = void 0;

  selectedItem: NodeInstance | RulePage | NodeInstance2RulePage | RuleInstance;

  @ViewChild("configTree")
  configTree: ConfigTreeComponent;

  @ViewChild("logicPagesList")
  logicPageList: DxListComponent;

  @ViewChild("infoPopup")
  popupLearnMode: DxPopupComponent;

  areaInstances: AreaInstance[] = [];
  categoryInstances: CategoryInstance[] = [];
  userGroups: UserGroup[] = [];

  public infoPopupVisible: boolean = false;

  constructor(private ruleEngineService: LogicEngineService,
    private configService: ConfigService,
    private notify: NotifyService,
    translate: L10nTranslationService,
    private areaService: AreaService,
    private categoryService: CategoryService,
    private userGroupsService: GroupsService,
    appService: AppService,
    private dataHub: DataHubService,
    private nodeInstanceService: LogicEditorInstanceService,
    private changeRef: ChangeDetectorRef,
    private route: ActivatedRoute,
    private router: Router,
    private hubConnectionService: HubConnectionService) {
    super(notify, translate, appService);

    appService.setAppTitle("RULEENGINE.NAME");
  }

  onPopupHiding($event) {
    this.infoPopupVisible = false;
  }

  async ngOnInit() {
    await this.hubConnectionService.init();
    await this.loadData();

    const pageId = this.route.snapshot.params["id"];
    this.selectLogicPageById(pageId);
    const that = this;

    super.registerEvent(this.ruleEngineService.showInfo, async (a) => {
      this.infoPopupVisible = true;
      this.changeRef.detectChanges();
      await this.popupLearnMode.instance.show();
    });

    this.route.params.subscribe(async (params) => {
      that.selectLogicPageById(params.id);
    });

    this.baseOnInit();
  }
  async load() {
    await this.loadData();
  }

  unselectItem() {
    this.selectedItem = void 0;
  }

  initPages() {
    this.selectedItem = this.pages[0];
    this.selectedPage = this.selectedItem;

    for (const x of this.pages) {
      for (const link of x.Links) {
        if (link.FromRuleInstance) {
          link.FromRuleInstance.RuleInstance = x.RuleInstances.find(a => a.ObjId === link.FromRuleInstance.This2RuleInstance);
        }
        if (link.ToRuleInstance) {
          link.ToRuleInstance.RuleInstance = x.RuleInstances.find(a => a.ObjId === link.ToRuleInstance.This2RuleInstance);
        }
      }
    }
  }

  async ngOnDestroy() {
    super.baseOnDestroy();
    await this.dataHub.unSubscribeForAll();
  }

  async loadData() {
    try {
      this.isLoading = true;

      const [ruleTemplates, linkableNodes, areaInstances, categoryInstances, userGroups] = await Promise.all(
        [
          this.ruleEngineService.getRuleTemplates(),
          this.configService.getLinkableNodes(),
          this.areaService.getAreaInstances(),
          this.categoryService.getCategoryInstances(),
          this.userGroupsService.getUserGroups(),
          this.nodeInstanceService.load()
        ]);

      this.pages = this.sortPages(this.nodeInstanceService.pages);


      this.pagesDataSource = new DataSource({
        paginate: false,
        pageSize: 1000,
        load: (loadOptions) => {
          return new Promise((resolve, reject) => {
            resolve(this.pages);
          });
        },

      });

      this.initPages();

      this.ruleTemplates = ruleTemplates;
      this.linkableNodes = linkableNodes;

      this.areaInstances = areaInstances;
      this.categoryInstances = categoryInstances;
      this.userGroups = userGroups;

      for (const page of this.pages) {

        for (const nodeInstance of page.NodeInstances) {
          nodeInstance.NodeInstance = this.nodeInstanceService.getNodeInstance(nodeInstance.This2NodeInstance);
        }
      }

      // await this.dataHub.subscribeForAll();

    } catch (error) {
      super.handleError(error);
    }

    this.isLoading = false;
    this.changeRef.detectChanges();
  }

  private sortPages(pages) {
    return pages.sort((n1, n2) => {
      if (n1.Name > n2.Name) {
        return 1;
      }
      if (n1.Name < n2.Name) {
        return -1;
      }
      return 0;
    });

  }

  validate($event) {
    this.configTree.validate($event);

    // this.sortPages();
  }


  private selectLogicPageById(id: string) {
    if (!id) {
      return;
    }

    const page = this.pages.filter(a => a.ObjId == id);

    if (page.length == 0) {
      return;
    }
    this.selectLogicPage(page[0]);
  }

  private selectLogicPage(page: RulePage) {
    this.configTree.selectNodeById(void 0);
    this.selectedPage = page;
    this.selectedItem = page;
  }

  onImportData($event) {
    if (this.selectedItem instanceof NodeInstance) {
      this.configTree.add($event, this.selectedItem);
    }
  }

  async removeSelectedItem($event) {
    const item = $event.item;
    const page: RulePage = $event.page;
    if (item instanceof RuleInstance) {
      if (this.selectedItem && this.selectedItem.ObjId === item.ObjId) {
        page.removeRuleInstance(item.ObjId);
        $event.removed = true;
        await this.ruleEngineService.removeItem(item);
      }
    }
    else if (item instanceof NodeInstance2RulePage) {
      page.removeNodeInstance(item.ObjId);
      $event.removed = true;
      await this.ruleEngineService.removeItem(item);
      this.nodeInstanceService.removeLogicNodeInstance(item);
      this.configTree.refreshTree();
    }

  }

  onSelectedItemsChanged($event) {
    const page = $event.page;
    const items: any[] = $event.items;
    if (this.selectedPage.ObjId === page.ObjId) {
      if (items.length === 1) {
        const item = items[0];

        if (item instanceof NodeInstance2RulePage) {
          if (this.selectedItem && this.selectedItem.ObjId != item.ObjId)
            this.configTree.selectNodeById(item.This2NodeInstance);

          const node = this.nodeInstanceService.getNodeInstance(item.This2NodeInstance);
          this.selectedItem = node; // this.configTree.selectNodeById(item.This2NodeInstance); // this class will set the selectedItem anyway
        } else if (item instanceof RuleInstance) {
          this.selectedItem = item;
        } else {
          this.configTree.selectNodeById(void 0);
          this.selectedItem = void 0;
        }
      } else {
        this.configTree.selectNodeById(void 0);
        this.selectedItem = void 0;
      }
    }
  }

  onTabSelectionChanged($event) {
    if ($event.addedItems && $event.addedItems.length > 0) {
      const selectedPage = <RulePage>$event.addedItems[0];
      this.router.navigate(["../", selectedPage.ObjId], { relativeTo: this.route });
    }
  }

  async restart() {
    this.isLoading = true;

    try {
      await this.ruleEngineService.reload();
    } catch (error) {
      //ignore error, we will not receive any callback!
    }
    this.notify.notifySuccess("COMMON.RELOAD");
    this.isLoading = false;
  }

  async onLogicNodeInstanceRemoved(item: NodeInstance2RulePage){
    const page = item.RulePage;
    page.NodeInstances = page.NodeInstances.filter(a => a.ObjId !== this.selectedItem.ObjId);
    this.pages.filter(a => a.ObjId === page.ObjId)[0].NodeInstances = page.NodeInstances;

    this.ruleEngineService.reInit.emit(page);

    await this.ruleEngineService.removeItem(item);
    this.nodeInstanceService.removeLogicNodeInstance(item);
    await this.ruleEngineService.removed.emit({logicNodeInstance: item, logicPage: item.RulePage});
  }

  async delete() {
    try {
      if (this.selectedItem instanceof NodeInstance2RulePage) {
        this.onLogicNodeInstanceRemoved(this.selectedItem);
      } else if (this.selectedItem instanceof RuleInstance) {
        const selectedPage = this.selectedPage;
        selectedPage.RuleInstances = selectedPage.RuleInstances.filter(a => a.ObjId !== this.selectedItem.ObjId);

        this.ruleEngineService.reInit.emit(this.selectedPage);

        await this.ruleEngineService.removeItem(this.selectedItem);

      } else if (this.selectedItem instanceof NodeInstance) {
        this.configTree.removeItem();

      } else if (this.selectedItem instanceof RulePage) {
        await this.removeRulePage();
      }
    }
    catch (error) {
      this.handleError(error);
    }

  }

  async nodeSelect(event: ITreeNode) {
    if (event instanceof NodeInstance) {
      this.selectedItem = event;
    }
    else if (event instanceof NodeInstance2RulePage) {
      const toPage = this.pages.find(a => a.ObjId === event.This2RulePage);
      if (this.selectedPage.ObjId != toPage.ObjId) {
        await this.router.navigate(["../", toPage.ObjId], { relativeTo: this.route });
      }
      this.selectedItem = event;
      this.selectedPage = toPage;

      setTimeout(() => {
        this.ruleEngineService.selected.emit({ logicNodeInstance: event, logicPage: this.selectedPage });
      }, 10);
    }
  }

  scan(nodeInstance: NodeInstance) {
    this.configTree.scan(nodeInstance);
  }

  async fileUploaded($event) {
    this.isLoading = true;
    await this.configTree.fileUploaded($event.item, $event.file.name, $event.password);
    this.isLoading = false;
  }

  itemClick($event) {
    const item: CustomMenuItem = $event.itemData;

    if (item && item.command) {
      item.command($event);
    }
  }

  async removeRulePage() {
    this.appService.isLoading = true;

    try {
      const currentPage = this.selectedPage;

      await this.ruleEngineService.removePage(currentPage);

      this.pages = this.pages.filter(a => a.ObjId != currentPage.ObjId);
      this.logicPageList.selectedItems = [];

      await this.pagesDataSource.reload();
      this.changeRef.detectChanges();
    } catch (error) {
      this.handleError(error);
    }

    this.appService.isLoading = false;
  }

  async addRulePage() {

    this.appService.isLoading = true;

    const rulePage = new RulePage();
    rulePage.ObjId = Guid.MakeNew().ToString();
    rulePage.Name = "Page " + (this.pages.length + 1);
    rulePage.Description = "Page " + (this.pages.length + 1);
    rulePage.afterFromJson();

    try {
      await this.ruleEngineService.addPage(rulePage);
      this.pages.push(rulePage);

      this.selectedPage = rulePage;
      this.selectedItem = rulePage;

      this.logicPageList.selectedItems = [rulePage];

      await this.pagesDataSource.reload();
      this.changeRef.detectChanges();

    } catch (error) {
      this.handleError(error);
      await this.load();
    }


    this.appService.isLoading = false;
  }

  async addRule($event) {
    await this.add($event, this.selectedPage);
  }

  async add(template: RuleTemplate | NodeInstance, rulePage: RulePage) {
    try {
      if (template instanceof RuleTemplate) {
        const rule = RuleInstance.fromRuleTemplate(template, this.translate);

        rule.Name = this.translate.translate(rule.Name);
        rule.Description = this.translate.translate(rule.Description);

        this.selectedPage.RuleInstances.push(rule);

        await this.ruleEngineService.addItem({
          data: rule,
          pageId: this.selectedPage.ObjId
        });

      } else if (template instanceof NodeInstance) {
        const node = NodeInstance2RulePage.createFromNodeInstance(template, rulePage);

        this.selectedPage.NodeInstances.push(node);
        await this.ruleEngineService.addItem({
          data: node,
          pageId: this.selectedPage.ObjId
        });

        this.nodeInstanceService.addLogicNodeInstance(node);

        this.configTree.refreshTree();
      }
    }
    catch (error) {
      this.handleError(error);
    }
  }

  saveLearnedNodes($event: any) {
    this.configTree.saveLearnNodeInstances($event.nodeInstance, $event.learnedNodes);
  }

  onZoomIn($event) {
    this.selectedPage.onZoomIn.emit();

  }
  onZoomOut($event) {
    this.selectedPage.onZoomOut.emit();
  }
  onZoomToView($event) {
    this.selectedPage.onZoomToView.emit();
  }
}
