import { Component, OnInit, OnDestroy, ViewChild } from "@angular/core";
import { ConfigTreeComponent } from "src/app/shared/config-tree/config-tree.component";
import { UserGroup } from "src/app/base/model/user/user-group";
import { ConfigService } from "src/app/services/config.service";
import { AreaService } from "src/app/services/areas.service";
import { CategoryService } from "src/app/services/categories.service";
import { L10nTranslationService } from "angular-l10n";
import { GroupsService } from "src/app/services/groups.service";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { NodeInstance } from "src/app/base/model/node-instance";
import { AreaInstance } from "src/app/base/model/areas";
import { CategoryInstance } from "src/app/base/model/categories";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { LogicEditorInstanceService } from "src/app/services/logic-editor-instance.service";
import { HubConnectionService } from "src/app/base/communication/hubs/hub-connection.service";

@Component({
  selector: "app-config",
  templateUrl: "./config.component.html",
  styleUrls: ["./config.component.scss"]
})
export class ConfigComponent extends BaseComponent implements OnInit, OnDestroy {

  selectedItem: NodeInstance;

  @ViewChild("configTree")
  configTree: ConfigTreeComponent;

  deletedConfig: number[] = [];

  areaInstances: AreaInstance[] = [];
  categoryInstances: CategoryInstance[] = [];
  userGroups: UserGroup[] = [];

  constructor(private configService: ConfigService,
    private dataHub: DataHubService,
    private notify: NotifyService,
    private areaService: AreaService,
    private categoryService: CategoryService,
    translate: L10nTranslationService,
    private userGroupsService: GroupsService,
    appService: AppService,
    private nodeInstanceService: LogicEditorInstanceService,
    private hubConnectionService: HubConnectionService) {
    super(notify, translate, appService);

    appService.setAppTitle("CONFIGURATION.NAME");
  }

  async load() {
    try {
      await this.hubConnectionService.init();
      this.isLoading = true;

      const [userGroups, areaInstances, categoryInstances] = await Promise.all(
        [
          this.userGroupsService.getUserGroups(),
          this.areaService.getAllAreaInstances(),
          this.categoryService.getAllCategoryInstances(),
          this.nodeInstanceService.load()
        ]);

      this.userGroups = userGroups;
      this.areaInstances = areaInstances;
      this.categoryInstances = categoryInstances;

    } catch (error) {
      super.handleError(error);
    }

    this.isLoading = false;
  }

  async ngOnInit() {
    await this.load();
    this.baseOnInit();
  }

  delete(e) {
    this.configTree.removeItem();

  }

  async reload($event) {
    try {
      this.isLoading = true;
      await this.configTree.restart();
    } catch (error) {
      //ignore error, we will not receive any callback!
    }
    this.isLoading = false;
  }

  validate($event) {
    this.configTree.validate($event);
  }

  nodeSelect(event: NodeInstance) {
    this.selectedItem = event;
  }

  scan(nodeInstance: NodeInstance) {
    this.configTree.scan(nodeInstance);
  }

  async fileUploaded($event) {
    this.isLoading = true;
    await this.configTree.fileUploaded($event.item, $event.file.name, $event.password);
    this.isLoading = false;
  }

  async ngOnDestroy() {
    await this.dataHub.unSubscribeForAll();
  }

  async onImportData($event) {
    await this.configTree.load();
    await this.configTree.tree.instance.refresh();
  }

  saveLearnedNodes($event: any) {
    this.configTree.saveLearnNodeInstances($event.nodeInstance, $event.learnedNodes);
  }

}
