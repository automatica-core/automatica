import { Component, OnInit, ViewChild, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from "@angular/core";
import { RulePage } from "src/app/base/model/rule-page";
import { ConfigTreeComponent } from "src/app/shared/config-tree/config-tree.component";
import { UserGroup } from "src/app/base/model/user/user-group";
import { RuleEngineService } from "src/app/services/ruleengine.service";
import { ConfigService } from "src/app/services/config.service";
import { TranslationService } from "angular-l10n";
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
import { NodeTemplate } from "src/app/base/model/node-template";
import { AreaInstance } from "src/app/base/model/areas";
import { CategoryInstance } from "src/app/base/model/categories";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { RuleInstance } from "src/app/base/model/rule-instance";
import { NodeInstanceService } from "src/app/services/node-instance.service";

@Component({
  selector: "app-logic-editor",
  templateUrl: "./logic-editor.component.html",
  styleUrls: ["./logic-editor.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LogicEditorComponent extends BaseComponent implements OnInit, OnDestroy {

  public pages: RulePage[] = [];
  ruleTemplates: RuleTemplate[];
  linkableNodes: NodeInstance[] = [];

  selectedPageIndex = 0;
  menuItems: CustomMenuItem[] = [];


  menuItemNew: CustomMenuItem = {
    id: "new",
    icon: "fa-plus",
    disabled: false,
    // items: this.addItemsMenu
  }
  menuSave: CustomMenuItem = {
    id: "save",
    icon: "fa-save",
    items: undefined,
    command: () => { this.save(); }
  }

  selectedItem: NodeInstance | RulePage | NodeInstance2RulePage | RuleInstance;

  nodeTemplates: NodeTemplate[];

  @ViewChild("configTree", { static: false })
  configTree: ConfigTreeComponent;

  areaInstances: AreaInstance[] = [];
  categoryInstances: CategoryInstance[] = [];
  userGroups: UserGroup[] = [];


  constructor(private ruleEngineService: RuleEngineService,
    private configService: ConfigService,
    private notify: NotifyService,
    translate: TranslationService,
    private areaService: AreaService,
    private categoryService: CategoryService,
    private userGroupsService: GroupsService,
    appService: AppService,
    private dataHub: DataHubService,
    private nodeInstanceService: NodeInstanceService,
    private changeRef: ChangeDetectorRef) {
    super(notify, translate, appService);

    appService.setAppTitle("RULEENGINE.NAME");
  }

  async load() {
    await this.loadData();
  }


  initPages() {
    this.selectedItem = this.pages[this.selectedPageIndex];

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

      const [pages, ruleTemplates, linkableNodes, templates, areaInstances, categoryInstances, userGroups] = await Promise.all(
        [
          this.ruleEngineService.getPages(),
          this.ruleEngineService.getRuleTemplates(),
          this.configService.getLinkableNodes(),
          this.configService.getNodeTemplates(),
          this.areaService.getAreaInstances(),
          this.categoryService.getCategoryInstances(),
          this.userGroupsService.getUserGroups(),
          this.nodeInstanceService.load()
        ]);

      this.pages = pages;
      this.pages = this.pages.sort((n1, n2) => {
        if (n1.Name > n2.Name) {
          return 1;
        }
        if (n1.Name < n2.Name) {
          return -1;
        }
        return 0;
      });

      this.initPages();

      this.ruleTemplates = ruleTemplates;
      this.linkableNodes = linkableNodes;

      this.areaInstances = areaInstances;
      this.categoryInstances = categoryInstances;
      this.userGroups = userGroups;

      this.nodeTemplates = templates;

      // await this.dataHub.subscribeForAll();

    } catch (error) {
      super.handleError(error);
    }

    this.isLoading = false;
    this.changeRef.detectChanges();
  }

  validate($event) {
    this.configTree.validate($event);
  }
  async ngOnInit() {

    await this.loadData();

    this.menuItems = [];

    this.menuItemNew.items = [];
    for (const temp of this.ruleTemplates) {
      const menuItem: CustomMenuItem = {
        id: "add-" + temp.Key,
        label: temp.Name,
        icon: "fa-plus",
        command: () => { this.add(temp, this.pages[this.selectedPageIndex]); }
      };

      this.menuItemNew.items.push(menuItem);
    }

    for (const temp of this.linkableNodes) {
      const menuItem: CustomMenuItem = {
        id: "add-node-" + temp.Id,
        label: temp.Name,
        icon: "fa-plus",
        command: () => { this.add(temp, this.pages[this.selectedPageIndex]); }
      };

      this.menuItemNew.items.push(menuItem);
    }

    this.menuItems.push(this.menuItemNew);
    this.menuItems.push(this.menuSave);

    this.baseOnInit();

  }

  onImportData($event) {
    if (this.selectedItem instanceof NodeInstance) {
      this.configTree.add($event, this.selectedItem);
    }
  }

  onSelectedItemsChanged($event) {
    const page = $event.page;
    const items: any[] = $event.items;
    if (this.pages[this.selectedPageIndex].ObjId === page.ObjId) {
      if (items.length === 1) {
        const item = items[0];

        if (item instanceof NodeInstance2RulePage) {
          this.configTree.selectNodeById(item.This2NodeInstance);
          this.selectedItem = item; // this.configTree.selectNodeById(item.This2NodeInstance); // this class will set the selectedItem anyway
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
      this.configTree.selectNodeById(void 0);
      this.selectedItem = <RulePage>$event.addedItems[0];
    }
  }

  async save() {
    this.isLoading = true;

    try {
      const config = this.configTree.prepareForSave();
      await this.configTree.saveSettings(config[0]);
      const all = await this.ruleEngineService.saveAll(this.pages, config);

      this.nodeInstanceService.convertConfig(all.NodeInstances);
      this.pages = all.LogicPages;

      this.initPages();

      this.notify.notifySuccess("COMMON.SAVED");

    } catch (error) {
      this.notify.notifyError(error);
      throw error;
    }
    this.isLoading = false;
  }

  delete() {
    if (this.selectedItem instanceof NodeInstance2RulePage) {
      const selectedPage = this.pages[this.selectedPageIndex];
      selectedPage.NodeInstances = selectedPage.NodeInstances.filter(a => a.ObjId !== this.selectedItem.ObjId);

      this.ruleEngineService.reInit.emit(this.selectedPageIndex);
    } else if (this.selectedItem instanceof RuleInstance) {
      const selectedPage = this.pages[this.selectedPageIndex];
      selectedPage.RuleInstances = selectedPage.RuleInstances.filter(a => a.ObjId !== this.selectedItem.ObjId);

      this.ruleEngineService.reInit.emit(this.selectedPageIndex);
    } else if (this.selectedItem instanceof NodeInstance) {
      this.configTree.removeItem();
    } else if (this.selectedItem instanceof RulePage) {
      this.pages = this.pages.filter(a => a.ObjId !== this.selectedItem.ObjId);
    }

  }
  addItem(nodeTemplate: NodeTemplate) {
    if (this.selectedItem instanceof NodeInstance) {
      this.configTree.addItem(this.selectedItem, nodeTemplate, true);
    }
  }

  nodeSelect(event: NodeInstance) {
    this.selectedItem = event;
  }

  scan(nodeInstance: NodeInstance) {
    this.configTree.scan(nodeInstance);
  }

  async fileUploaded($event) {
    this.isLoading = true;
    await this.configTree.fileUploaded($event.item, $event.file.name);
    this.isLoading = false;
  }

  itemClick($event) {
    const item: CustomMenuItem = $event.itemData;

    if (item && item.command) {
      item.command($event);
    }
  }

  addRulePage() {
    const rulePage = new RulePage();
    rulePage.ObjId = Guid.MakeNew().ToString();
    rulePage.Name = "Page " + (this.pages.length + 1);
    rulePage.Description = "Page " + (this.pages.length + 1);
    this.pages.push(rulePage);
  }

  addRule($event) {
    this.add($event, this.pages[this.selectedPageIndex]);
  }

  add(template: RuleTemplate | NodeInstance, rulePage: RulePage) {
    if (template instanceof RuleTemplate) {
      const rule = RuleInstance.fromRuleTemplate(template);

      rule.Name = this.translate.translate(rule.Name);
      rule.Description = this.translate.translate(rule.Description);

      this.pages[this.selectedPageIndex].RuleInstances.push(rule);

      this.ruleEngineService.add.emit({
        data: rule,
        pageIndex: this.pages[this.selectedPageIndex].ObjId
      });
    } else if (template instanceof NodeInstance) {
      const node = NodeInstance2RulePage.createFromNodeInstance(template, rulePage);

      this.pages[this.selectedPageIndex].NodeInstances.push(node);


      this.ruleEngineService.add.emit({
        data: node,
        pageIndex: this.pages[this.selectedPageIndex].ObjId
      });
    }
  }

  saveLearnedNodes($event: any) {
    this.configTree.saveLearnNodeInstances($event.nodeInstance, $event.learnedNodes);
  }
}
