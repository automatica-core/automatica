import { Component, OnInit, OnDestroy, ViewChild } from "@angular/core";
import { ConfigMenuComponent } from "src/app/shared/config-menu/config-menu.component";
import { ConfigTreeComponent } from "src/app/shared/config-tree/config-tree.component";
import { UserGroup } from "src/app/base/model/user/user-group";
import { ConfigService } from "src/app/services/config.service";
import { AreaService } from "src/app/services/areas.service";
import { CategoryService } from "src/app/services/categories.service";
import { TranslationService } from "angular-l10n";
import { GroupsService } from "src/app/services/groups.service";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { NodeInstance } from "src/app/base/model/node-instance";
import { NodeTemplate } from "src/app/base/model/node-template";
import { AreaInstance } from "src/app/base/model/areas";
import { CategoryInstance } from "src/app/base/model/categories";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";

@Component({
  selector: "app-config",
  templateUrl: "./config.component.html",
  styleUrls: ["./config.component.scss"]
})
export class ConfigComponent extends BaseComponent implements OnInit, OnDestroy {

  selectedItem: NodeInstance;

  nodeTemplates: NodeTemplate[];

  @ViewChild("configTree", { static: false })
  configTree: ConfigTreeComponent;

  deletedConfig: number[] = [];

  areaInstances: AreaInstance[] = [];
  categoryInstances: CategoryInstance[] = [];
  userGroups: UserGroup[] = [];

  constructor(private configService: ConfigService, private dataHub: DataHubService, private notify: NotifyService,
    private areaService: AreaService, private categoryService: CategoryService, translate: TranslationService, private userGroupsService: GroupsService, private appService: AppService) {
    super(notify, translate);

    appService.setAppTitle("CONFIGURATION.NAME");
  }

  async ngOnInit() {
    try {
      this.appService.isLoading = true;

      const [userGroups,  configTree, areaInstances, categoryInstances] = await Promise.all(
        [
          this.userGroupsService.getUserGroups(),
          this.configTree.loadTree(),
          this.areaService.getAreaInstances(),
          this.categoryService.getCategoryInstances()
        ])
      this.userGroups = userGroups;

      await this.dataHub.subscribe("All");

      this.areaInstances = areaInstances;
      this.categoryInstances = categoryInstances;


    } catch (error) {
      super.handleError(error);
    }

    this.appService.isLoading = false;
  }

  delete(e) {
    this.configTree.removeItem();

  }
  addItem(nodeTemplate: NodeTemplate) {
    this.configTree.addItem(this.selectedItem, nodeTemplate, true);

  }
  async save($event) {
    try {
      this.appService.isLoading = true;
      await this.configTree.save();
    } catch (error) {
      this.notify.notifyError(error);
      throw error;
    }
    this.appService.isLoading = false;
  }

  validate($event) {
    this.configTree.validate($event);
  }

  nodeSelect(event: NodeInstance) {
    this.selectedItem = event;
  }

  async onReinit($event) {
    await this.configService.reInitServer();
  }

  scan(nodeInstance: NodeInstance) {
    this.configTree.scan(nodeInstance);
  }

  async fileUploaded($event) {
    this.appService.isLoading = true;
    await this.configTree.fileUploaded($event.item, $event.file.name);
    this.appService.isLoading = false;
  }

  async ngOnDestroy() {
    await this.dataHub.unsubscribe("All");
  }

  onImportData($event) {
    this.configTree.add($event, this.selectedItem);
  }

  saveLearnedNodes($event: any) {
    this.configTree.saveLearnNodeInstances($event.nodeInstance, $event.learnedNodes);
  }

}
