import { Component, OnInit, Input, ViewChild, Output, EventEmitter, ChangeDetectionStrategy, ChangeDetectorRef } from "@angular/core";
import { CommonModule, NgSwitch, NgSwitchCase, NgSwitchDefault } from "@angular/common";
import { ConfigService } from "../../services/config.service";
import { L10nTranslationService } from "angular-l10n";
import { DxDataGridComponent, DxCheckBoxComponent, DxPopupComponent, DxValidatorComponent, DxTreeViewComponent, DxDropDownBoxComponent } from "devextreme-angular";
import { BaseService } from "src/app/services/base-service";
import { IPropertyModel } from "src/app/base/model/interfaces";
import { UserGroup } from "src/app/base/model/user/user-group";
import { Role } from "src/app/base/model/user/role";
import { NotifyService } from "src/app/services/notify.service";
import { TimerPropertyData } from "src/app/base/model/timer-property-data";
import { InputValidator } from "src/app/base/utils/utils";
import { PropertyInstance } from "src/app/base/model/property-instance";
import { NodeTemplate } from "src/app/base/model/node-template";
import { PropertyTemplateTypeAware, PropertyTemplateType, PropertyTemplate } from "src/app/base/model/property-template";
import { BaseComponent } from "src/app/base/base-component";
import { NodeInstance } from "src/app/base/model/node-instance";
import { VisuPage } from "src/app/base/model/visu-page";
import { AreaInstance } from "src/app/base/model/areas";
import { CategoryInstance } from "src/app/base/model/categories";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { VirtualAreaPropertyInstance, VirtualDescriptionPropertyInstance } from "src/app/base/model/virtual-props";
import { ConfigTreeComponent } from "../config-tree/config-tree.component";
import { Satellite } from "src/app/base/model/satellites/satellite";
import { SatelliteService } from "src/app/services/satellite.services";
import { LearnModeNodeTemplate } from "src/app/base/model/learnmode/learn-mode-node-template";
import { AppService } from "src/app/services/app.service";
import { NodeInstanceService } from "src/app/services/node-instance.service";
import { RuleInstance } from "src/app/base/model/rule-instance";
import { LogicEngineService } from "src/app/services/logicengine.service";
import { RulePage } from "src/app/base/model/rule-page";
import DataSource from "devextreme/data/data_source";
import ArrayStore from "devextreme/data/array_store";
import { NodeInstanceImportComponent } from "./node-instance-ets-import/node-instance-import.component";
import { NodeInstanceImportSerivce } from "./node-instance-ets-import/node-instance-import.service";
import { Router } from "@angular/router";

function sortProperties(a: PropertyInstance, b: PropertyInstance) {
  if (a.PropertyTemplate.Order < b.PropertyTemplate.Order) {
    return -1;
  }
  if (a.PropertyTemplate.Order > b.PropertyTemplate.Order) {
    return 1;
  }

  if (a.Name < b.Name) {
    return -1;
  }
  if (a.Name > b.Name) {
    return 1;
  }

  return 0;

}


export class LearnNodeInstance {
  nodeTemplateInstance: LearnModeNodeTemplate;

  private _nodeTemplates: LearnModeNodeTemplate[];
  public get nodeTemplates(): LearnModeNodeTemplate[] {
    return this._nodeTemplates;
  }
  public set nodeTemplates(v: LearnModeNodeTemplate[]) {
    this._nodeTemplates = v;
  }

  nodeTemplateChanged = new EventEmitter<string>();

  private _nodeTemplate: string;
  public get nodeTemplate(): string {
    return this._nodeTemplate;
  }
  public set nodeTemplate(v: string) {
    this._nodeTemplate = v;
    this.nodeTemplateChanged?.emit(v);
  }

  public get nodeTemplateItem(): NodeTemplate {
    return this.nodeTemplateInstance?.nodeTemplate;
  }


  private _propertyInstances: PropertyInstance[];
  public get propertyInstances(): PropertyInstance[] {
    return this._propertyInstances;
  }
  public set propertyInstances(v: PropertyInstance[]) {
    this._propertyInstances = v;
  }

  public dataSource: DataSource;

  public init() {
    this.dataSource = new DataSource({
      store: new ArrayStore({
        key: "ObjId",
        data: this.nodeTemplates
      })
    }
    );
  }


  private _name: string;
  public get name(): string {
    return this._name;
  }
  public set name(v: string) {
    this._name = v;
  }


  private _description: string;
  public get description(): string {
    return this._description;
  }
  public set description(v: string) {
    this._description = v;
  }

  private _key: string;
  public get key(): string {
    return this._key;
  }
  public set key(v: string) {
    this._key = v;
  }



}

@Component({
  selector: "p3-propertyeditor",
  templateUrl: "./propertyeditor.component.html",
  styleUrls: ["./propertyeditor.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush

})
@PropertyTemplateTypeAware
export class PropertyEditorComponent extends BaseComponent implements OnInit {
  PropertyTemplateType: typeof PropertyTemplateType = PropertyTemplateType;

  @ViewChild(DxTreeViewComponent, { static: false }) treeView;

  @ViewChild("configTree")
  configTree: ConfigTreeComponent;

  @ViewChild("popupNodeSelect")
  popupNodeSelect: DxPopupComponent;
  private _popupPropertyInstance: PropertyInstance;
  public selectedNodeInstance: NodeInstance;
  public nodeSelectorVisible: boolean = false;
  public nodeSelectorTreeLoading: boolean = false;

  @ViewChild("popupVisuPageSelect")
  popupVisuPageSelect: DxPopupComponent;
  public selectedVisuPage: VisuPage;
  public visuPageSelectorVisible: boolean = false;

  @ViewChild("popupLearnMode")
  popupLearnMode: DxPopupComponent;
  public learnedNodeInstances: NodeInstance[] = [];
  public learnModeVisible: boolean = false;
  public learnNodeInstance: LearnNodeInstance[] = [];

  @ViewChild("timerEditPopup")
  popupTimerEdit: DxPopupComponent;
  public timerEditPopupVisible: boolean = false;
  public timerEditValue: TimerPropertyData = void 0;
  public selectedProperty: PropertyInstance = void 0;

  @ViewChild("nodeInstanceImport")
  NodeInstanceImportComponent: NodeInstanceImportComponent;

  @Output()
  public scan = new EventEmitter<NodeInstance>();
  @Output()
  public fileUploaded = new EventEmitter<any>();

  @Output()
  public saveLearnedNodes = new EventEmitter<any>();

  learnModeSub: any;

  @Input()
  set item(value: IPropertyModel) {
    this._item = value;

    if (this.item && this.item.Properties) {
      this._properties = this.item.Properties.filter(a => a.IsVisible).sort(sortProperties);

      this._properties.forEach(a => {
        if(a instanceof VirtualDescriptionPropertyInstance) {
          a.Value = this.translate.translate(a.Value);
        }
      });
    }
    if (!value) {
      this._properties = [];
    }
  }
  get item(): IPropertyModel {
    return this._item;
  }
  _item: IPropertyModel;

  _properties: PropertyInstance[] = [];

  public get Properties(): PropertyInstance[] {
    return this._properties;
  }

  @ViewChild("dataTable")
  private dataTable: DxDataGridComponent;

  @Output()
  validate = new EventEmitter<any>();


  private _areaInstances: AreaInstance[] = [];

  @Input()
  public get areaInstances(): AreaInstance[] {
    return this._areaInstances;
  }
  public set areaInstances(v: AreaInstance[]) {
    this._areaInstances = v;
    this.flattenAraInit();
  }


  private areaInstancesFlat: AreaInstance[] = [];
  private areaInstanceMap = new Map<string, AreaInstance>();


  private _categoryInstances: CategoryInstance[] = [];
  @Input()
  public get categoryInstances(): CategoryInstance[] {
    return this._categoryInstances;
  }
  public set categoryInstances(v: CategoryInstance[]) {
    this._categoryInstances = v;
    for (const x of this._categoryInstances) {
      if (!x.IsDeleteable) {
        x.DisplayName = this.translate.translate(x.Name);
        x.DisplayDescription = ""; // this.translate.translate(x.Description);
      }
    }
  }


  private _userGroups: UserGroup[];
  @Input()
  public get userGroups(): UserGroup[] {
    return this._userGroups;
  }
  public set userGroups(v: UserGroup[]) {
    this._userGroups = v;
  }


  private _roles: Role[];
  @Input()
  public get roles(): Role[] {
    return this._roles;
  }
  public set roles(v: Role[]) {
    this._roles = v;
  }



  private _satellites: Satellite[];

  @Input()
  public get satellites(): Satellite[] {
    return this._satellites;
  }
  public set satellites(v: Satellite[]) {
    this._satellites = v;
  }


  get uploadHeader() {
    return { "Authorization": "Bearer " + localStorage.getItem("jwt") };
  }


  private inputValidator: InputValidator = new InputValidator();

  @ViewChild("startDateValidator")
  startDateValidator: DxValidatorComponent;

  @ViewChild("endDateValidator")
  endDateValidator: DxValidatorComponent;

  constructor(
    private config: ConfigService,
    translate: L10nTranslationService,
    private dataHub: DataHubService,
    private notify: NotifyService,
    private satellitesService: SatelliteService,
    appService: AppService,
    private nodeInstanceService: NodeInstanceService,
    private ruleEngineService: LogicEngineService,
    private changeDetection: ChangeDetectorRef,
    private router: Router,
    private nodeInstanceImportService: NodeInstanceImportSerivce) {
    super(notify, translate, appService);
  }

  async ngOnInit() {
    this.satellites = await this.satellitesService.getAll();
  }

  flattenAraInit() {
    for (const x of this.areaInstances) {
      this.areaInstancesFlat.push(x);
      this.areaInstanceMap.set(x.ObjId, x);
      this.flattenAra(x);
    }
  }

  flattenAra(instance: AreaInstance) {
    for (const x of instance.InverseThis2ParentNavigation) {

      this.areaInstancesFlat.push(x);
      this.areaInstanceMap.set(x.ObjId, x);

      this.flattenAra(x);
    }
  }




  nodeSelect($event) {
    if ($event instanceof NodeInstance && $event.NodeTemplate.ProvidesInterface2InterfaceType === NodeTemplate.ValueInterfaceId()) {
      this.selectedNodeInstance = $event;
    } else {
      this.selectedNodeInstance = void 0;
    }
  }

  onVisuPageSelected($event) {
    this.selectedVisuPage = $event;
  }
  async useSelectedNode($event) {
    this._popupPropertyInstance.Value = this.selectedNodeInstance.ObjId;
    this.nodeSelectorVisible = false;
  }
  useSelectedVisuPage($event) {
    this._popupPropertyInstance.Value = this.selectedVisuPage.ObjId;
    this._popupPropertyInstance.VisuPage = this.selectedVisuPage;
    this.visuPageSelectorVisible = false;
  }

  async openNodeSelectDialog($event, prop: PropertyInstance) {
    this._popupPropertyInstance = prop;
    this.nodeSelectorVisible = true;
  }

  async onNodeSelectClosing($event) {
    this._popupPropertyInstance = void 0;
    this.selectedNodeInstance = void 0;
    this.nodeSelectorVisible = false;
  }

  openVisuPageSelectDialog($event, prop: PropertyInstance) {
    this._popupPropertyInstance = prop;
    this.visuPageSelectorVisible = true;
  }
  onVisuPageSelectClosing($event) {
    this._popupPropertyInstance = void 0;
    this.selectedVisuPage = void 0;
    this.visuPageSelectorVisible = false;

    for (const x of this.Properties) {
      if (x.PropertyTemplate.PropertyType.Type === PropertyTemplateType.AreaInstanceLink) {
        x.Value = void 0;
        break;
      }
    }
  }

  onTimerEditClick($event, prop: PropertyInstance) {
    this.timerEditValue = new TimerPropertyData();
    this.selectedProperty = prop;

    if (prop.Value && prop.Value instanceof TimerPropertyData) {
      this.timerEditValue = prop.Value.copy() as TimerPropertyData;
    }

    this.timerEditPopupVisible = true;
  }

  onTimerEditPopupClosing($event, ok: boolean) {
    this.timerEditPopupVisible = false;

    if (ok) {
      this.selectedProperty.Value = this.timerEditValue;
    }
    this.selectedProperty = void 0;
    this.timerEditValue = void 0;
  }

  translateName = (data: PropertyInstance) => {
    return this.translate.translate(data.PropertyTemplate.Name);
  }

  sortGroupOrder(a: PropertyTemplate, b: PropertyTemplate) {
    if (a.GroupOrder < b.GroupOrder) {
      return -1;
    }
    if (a.GroupOrder > b.GroupOrder) {
      return 1;
    }

    return 0;
  }

  groupOrderValue = (data: PropertyInstance) => {
    return data.PropertyTemplate.GroupOrder;
  }
  groupName = (data: PropertyInstance) => {
    return this.translate.translate(data.PropertyTemplate.Group);
  }

  async onScanClick($event, data: PropertyInstance) {
    if (data.PropertyTemplate.Meta === "OBJECT_SAVED" && this.item.isNewObject) {
      this.notify.notifyWarning("COMMON.SAVE_BEFORE_CONTINUE", 5000);
      return;
    }
    if (this.item instanceof NodeInstance) {
      this.scan.emit(this.item);
    }
  }

  async onImportOpen($event, data: PropertyInstance) {
    this.NodeInstanceImportComponent.isVisible = true;
    this.NodeInstanceImportComponent.nodeInstance = this.item;
    this.NodeInstanceImportComponent.propertyInstance = data;

    this.NodeInstanceImportComponent.fileUploaded = this.fileUploaded;
  }

  nodeTemplateSelectItem($event, learnInstance: LearnNodeInstance) {
    if ($event.node.children.length === 0) {
      learnInstance.nodeTemplateInstance = $event.itemData;
      learnInstance.nodeTemplate = learnInstance.nodeTemplateInstance.nodeTemplate.ObjId;

      $event.component.selectItem(learnInstance.nodeTemplate);
    }
  }

  nodeTemplateSelectItemSync($event) {
    if (!this.treeView) return;


    this.treeView.instance.selectItem($event.value);
  }

  nodeTemplateBoxOptionChanged(e) {
    if (e.name === 'value') {
      //(<DxDropDownBoxComponent>e).instance.close();
      e.component.close();
      this.changeDetection.detectChanges();
    }
  }

  saveLearnNodes($event) {
    if (this.item instanceof NodeInstance) {
      this.saveLearnedNodes.emit({ learnedNodes: this.learnNodeInstance, nodeInstance: this.item });
      this.learnNodeInstance = [];
      this.learnModeVisible = false;
      this.changeDetection.detectChanges();
    }
  }

  async onCustomActionClick($event, data: PropertyInstance) {
    await this.config.customAction(this.item as NodeInstance, data.PropertyTemplate.Key);
  }

  async onLearnClick($event, data: PropertyInstance) {
    if (data.Parent instanceof NodeInstance) {
      const currentNode: NodeInstance = data.Parent;

      this.learnModeVisible = true;
      this.isLoading = true;
      await this.dataHub.enableLearnMode(data.Parent);
      this.learnedNodeInstances = [];

      this.learnModeSub = super.registerEvent(this.dataHub.notifyLearnMode, (hubData) => {

        if (!this.learnNodeInstance.find(a => a.key === hubData[0])) {
          const learnNode = new LearnNodeInstance();
          learnNode.name = hubData[0];
          learnNode.key = hubData[0];
          learnNode.description = hubData[1];
          const nodeTemplates = BaseService.getValidBaseModels<NodeTemplate>(hubData[2], this.translate);
          learnNode.nodeTemplates = [];

          for (const nodeTemplate of nodeTemplates) {
            learnNode.nodeTemplates.push(new LearnModeNodeTemplate(nodeTemplate));
          }

          const rootElements = learnNode.nodeTemplates.filter(a => a.nodeTemplate.NeedsInterface2InterfacesType === currentNode.NodeTemplate.ProvidesInterface2InterfaceType);

          for (const rootEl of rootElements) {
            rootEl.ParentId = void 0;
          }

          if (hubData[3]) {
            learnNode.propertyInstances = BaseService.getValidBaseModels<PropertyInstance>(hubData[3], this.translate);
          }

          learnNode.init();
          this.learnNodeInstance = [...this.learnNodeInstance, learnNode];
          console.log(this.learnNodeInstance);
          this.changeDetection.detectChanges();
        }
      });
      this.isLoading = false;
    }
  }
  async onLearnModeClosing($event) {
    if (this.item instanceof NodeInstance) {
      await this.dataHub.disableLearnMode(this.item);

      super.unregisterEvent(this.learnModeSub);
      this.learnModeSub = void 0;
    }
  }

  async valueChanged(e, data) {
    const prop = data.data as PropertyInstance;

    this.validate.emit(prop);
    this.appService.isLoading = true;
    try {
      if (this.item instanceof NodeInstance) {
        await this.config.update(this.item);

        if (!this.item.ParentId) {
          this.nodeInstanceService.saveSettings();
        }
      } else if (this.item instanceof RuleInstance) {
        await this.ruleEngineService.updateItem(this.item);
      } else if (this.item instanceof RulePage) {
        await this.ruleEngineService.updatePage(this.item);
      }
    } catch (error) {
      this.handleError(error);
    }

    this.appService.isLoading = false;
  }


  onAreaItemSelectionChanged($event, data) {
    const prop = data.data as VirtualAreaPropertyInstance;

    prop.AreaInstance = $event.itemData;
    prop.Value = $event.itemData.ObjId;

    this.valueChanged($event, data);

    for (const x of this.Properties) {
      if (x.PropertyTemplate.PropertyType.Type === PropertyTemplateType.VisuPage) {
        x.Value = void 0;
        break;
      }
    }

  }

  onFileUploaded($event, data) {
    if (data.PropertyTemplate.Meta === "OBJECT_SAVED" && this.item.isNewObject) {
      this.notify.notifyWarning("COMMON.SAVE_BEFORE_CONTINUE", 5000);
      return;
    }
    if (this.item instanceof NodeInstance) {
      this.fileUploaded.emit({ item: this.item, file: $event.file });
    }
  }

  validateDates = (data: any): any => {
    this.timerEditValue.StartTime.setDate(1);
    this.timerEditValue.StartTime.setMonth(1);
    this.timerEditValue.StartTime.setFullYear(2000);

    this.timerEditValue.StopTime.setDate(1);
    this.timerEditValue.StopTime.setMonth(1);
    this.timerEditValue.StopTime.setFullYear(2000);
    return this.inputValidator.validateDates(data, this.timerEditValue.StartTime, this.timerEditValue.StopTime, this.startDateValidator, this.endDateValidator);
  }

  get timerSaveButtonDisabled(): boolean {
    if (!this.startDateValidator || !this.endDateValidator || !this.startDateValidator.instance || !this.endDateValidator.instance) {
      return false;
    }
    let result = this.startDateValidator.instance.validate();
    if (!result.isValid) {
      return true;
    }
    result = this.endDateValidator.instance.validate();
    if (!result.isValid) {
      return true;
    }
    return false;
  }
}
