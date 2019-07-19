import { ViewChild, Component, OnInit, Input, Output, EventEmitter, OnDestroy } from "@angular/core";
import { ConfigService } from "../../services/config.service";
import { DxTreeListComponent } from "devextreme-angular";
import { TranslationService } from "angular-l10n";
import { DataService } from "../../services/data.service";
import { SettingsService } from "src/app/services/settings.service";
import { LearnNodeInstance } from "../propertyeditor/propertyeditor.component";
import { Setting } from "src/app/base/model/setting";
import { VirtualSettingsPropertyInstance } from "src/app/base/model/virtual-props/settings/settings-property-instance";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { NodeInstanceState, NodeInstance } from "src/app/base/model/node-instance";
import { BoardType } from "src/app/base/model/board-type";
import { ITreeNode } from "src/app/base/model/ITreeNode";
import { NodeTemplate } from "src/app/base/model/node-template";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { BoardInterface } from "src/app/base/model/board-interface";
import { PropertyInstance } from "src/app/base/model/property-instance";

@Component({
  selector: "p3-config-tree",
  templateUrl: "./config-tree.component.html",
  styleUrls: ["./config-tree.component.scss"]
})
export class ConfigTreeComponent extends BaseComponent implements OnInit, OnDestroy {

  NodeInstanceState: typeof NodeInstanceState = NodeInstanceState;


  expandedRowKeys: any[] = ["b0"];

  @Input()
  public config: NodeInstance[];

  @Output()
  public configChange: EventEmitter<any> = new EventEmitter<any>();

  private mapList: Map<any, NodeInstance> = new Map<any, NodeInstance>();

  boardTypeOrig: BoardType;
  nodeInstances: NodeInstance[];

  copyItem: NodeInstance;

  private _selectedNode: ITreeNode;
  public get selectedNode(): ITreeNode {
    return this._selectedNode;
  }
  public set selectedNode(v: ITreeNode) {
    this._selectedNode = v;
    this.onNodeSelect.emit(v);
  }

  selectedRowKeys: any[] = [];


  popoverVisible: boolean = false;

  nextId: number;

  @Input()
  nodeTemplates: NodeTemplate[];

  @ViewChild("tree", { static: false }) tree: DxTreeListComponent;

  @Output() onNodeSelect = new EventEmitter<ITreeNode>();
  @Output() onNodeDrag: EventEmitter<any> = new EventEmitter();
  @Output() onConfigLoaded = new EventEmitter();

  @Input() useContextMenu: boolean;

  @Output() isLoadingChange = new EventEmitter<boolean>();

  @Input()
  showLoadingPanel = false;

  @Input()
  loadOnInit = false;

  private _isLoading: boolean;

  @Input()
  public get isLoading(): boolean {
    return this._isLoading;
  }
  public set isLoading(v: boolean) {
    this._isLoading = v;
    this.isLoadingChange.emit(v);
  }


  settings: Setting[] = [];

  constructor(
    private configService: ConfigService,
    translate: TranslationService,
    private notify: NotifyService,
    private hub: DataHubService,
    private dataService: DataService,
    private settingsService: SettingsService,
    private appService: AppService) {

    super(notify, translate);
    this.useContextMenu = true;
    this.nextId = 0;
  }

  async ngOnInit() {

    super.baseOnInit();

    if (this.loadOnInit) {
      try {
        this.isLoading = true;
        await this.loadTree();
      } catch {
        this.isLoading = false;
      }
    }
  }

  async loadTree() {
    this.nodeTemplates = await this.configService.getNodeTemplates();

    await this.loadConfig();

    super.registerEvent(this.hub.dispatchValue, (data) => {
      if (data[0] === 0) { // 0 = nodeinstance value
        const id = data[1];

        if (this.mapList.has(id)) {
          const treeNode = this.mapList.get(id);
          treeNode.Value = data[2];
        }
      }
    });

    await this.hub.subscribeForAll();


    const nodeData = await this.dataService.getCurrentNodeValues();

    // tslint:disable-next-line:forin
    for (const id in nodeData) {
      if (this.mapList.has(id)) {
        const treeNode = this.mapList.get(id);
        treeNode.Value = nodeData[id];
      }
    }

    this.appService.isLoading = false;
  }

  async ngOnDestroy() {
    await this.hub.unSubscribeForAll();
    super.baseOnDestroy();
  }

  sortByName(a, b) {
    if (a.Name < b.Name) {
      return -1;
    }
    if (a.Name > b.Name) {
      return 1;
    }

    return 0;

  }

  public async scan(nodeInstance: NodeInstance) {
    try {
      this.appService.isLoading = true;
      const nodeInstances = await this.configService.scan(nodeInstance);

      this.prepareRecurisve(nodeInstances, nodeInstance);
    } catch (error) {
      super.handleError(error);
    }
    this.appService.isLoading = false;
  }

  public async fileUploaded(nodeInstance: NodeInstance, fileName: string) {
    try {
      this.appService.isLoading = true;
      const nodeInstances = await this.configService.import(nodeInstance, fileName);
      this.prepareRecurisve(nodeInstances, nodeInstance);
    } catch (error) {
      super.handleError(error);
    }
    this.appService.isLoading = false;
  }

  private prepareRecurisve(nodeInstances: NodeInstance[], parent: NodeInstance) {
    for (const ni of nodeInstances) {
      const children = ni.Children;

      if (!this.mapList.has(ni.ObjId)) {
        ni.isNewObject = true;

        ni.Children = [];
        const existingParent = this.mapList.get(parent.Id);
        this.prepareNewItem(ni, existingParent, false, false);
      }
      if (children) {
        this.prepareRecurisve(children, ni);
      }
    }
  }

  private findNodeTemplate(id: string) {
    for (const nt of this.nodeTemplates) {
      if (nt.ObjId === id) {
        return nt;
      }
    }
    return void 0;
  }

  public convertConfig(instances: NodeInstance[]) {
    this.mapList.clear();
    this.mapList = new Map<number, NodeInstance>();

    this.config = [];

    const tmpConfig: NodeInstance[] = [];

    for (const node of instances) {
      this.mapList.set(node.Id, node);
      this.addNodeInstancesRec(node, tmpConfig);

      tmpConfig.push(node);
    }
    tmpConfig.sort((a, b) => (a.Name.localeCompare(b.Name) - (b.Name.localeCompare(a.Name))));

    this.config = tmpConfig;

    const rootNode = this.config.filter(a => !a.This2ParentNodeInstance)[0];
    const rootNodeSettings: VirtualSettingsPropertyInstance[] = [];

    for (const x of this.settings) {
      rootNodeSettings.push(new VirtualSettingsPropertyInstance(x));
    }

    rootNode.Properties = [...rootNode.Properties, ...rootNodeSettings];

    this.onConfigLoaded.emit(this.config);
  }

  async loadConfig() {
    try {
      this.isLoadingChange.emit(true);
      const instances = await this.configService.getNodeInstances();

      // this.expandedRowKeys = [instances[0].ObjId];

      this.settings = await this.settingsService.getSettings();

      this.convertConfig(instances);
    } catch (error) {
      super.handleError(error);
    }

    this.isLoadingChange.emit(false);
  }

  private getExpandedRowKeys(id: string, expandedRowKeys: string[]) {
    expandedRowKeys = [...expandedRowKeys, id];

    if (this.mapList.has(id)) {
      const item = this.mapList.get(id);
      if (item && item.ParentId) {
        return this.getExpandedRowKeys(item.ParentId, expandedRowKeys);
      }
    }
    return expandedRowKeys;
  }

  getNodeValue = (rowData: ITreeNode): any => {
    return rowData.Value;
  }

  getDisplayName = (rowData: ITreeNode) => {
    const instance = this.mapList.get(rowData.Id);

    if (!instance.Parent) {
      return rowData.DisplayName;
    }

    if (instance) {
      const translatedName = this.translate.translate(instance.NodeTemplate.Name);

      if (rowData.DisplayName !== translatedName && translatedName) {
        return `${rowData.DisplayName} (${translatedName})`;
      }
    }
    return rowData.DisplayName;
  }

  private addNodeInstancesRec(node: NodeInstance, tmpConfig: NodeInstance[]) {

    for (const x of node.Children) {
      this.mapList.set(x.Id, x);
      tmpConfig.push(x);

      this.addNodeInstancesRec(x, tmpConfig);
    }
  }

  selectionChanged($event) {
    // this.selectedNode = $event.selectedRowsData[0];
    // console.log($event);
    // const node = this.mapList.get($event.selectedRowsData[0].ObjId)

    // console.log(node);
    // this.selectNode(node);
  }


  onRowClicked($event) {
    const item = this.mapList.get($event.data.ObjId);
    this.selectNode(item);
  }

  selectNode(node: ITreeNode) {
    if (!node) {
      this.selectedRowKeys = [];
      // this.tree.instance.selectRows([], false);
      this.selectedNode = void 0;
      return;
    }

    if (!this.selectedNode || this.selectedNode.Id !== node.Id) {
      // this.tree.instance.selectRows([node.Id], false);
      this.selectedRowKeys = [node.Id];
      this.selectedNode = node;
      // this.expandedRowKeys = this.getExpandedRowKeys(node.Id, this.expandedRowKeys);
    }

    this.tree.instance.repaint();
  }

  selectNodeById(id: any) {
    if (this.mapList.has(id)) {
      const node = this.mapList.get(id);
      this.selectNode(node);

      const expandedRowKeys = this.getExpandedRowKeys(id, this.expandedRowKeys);
      // this.expandedRowKeys = expandedRowKeys;
    } else if (id === void 0) {
      this.selectedNode = void 0;
      this.tree.instance.selectRows([], false);

      this.onNodeSelect.emit(void 0);
    }
  }

  add(nodeInstance: NodeInstance, parent: NodeInstance) {
    nodeInstance.This2ParentNodeInstance = parent.ObjId;
    parent.Children = [...parent.Children, nodeInstance];

    const tmpConfig = [nodeInstance];
    this.addNodeInstancesRec(nodeInstance, tmpConfig)

    for (const x of tmpConfig) {
      this.mapList.set(x.Id, x);
    }
    this.config = [...this.config, ...tmpConfig];

    this.tree.instance.refresh();
  }

  createNodeInstanceChildren(nodeInstance: NodeInstance, nodeTemplate: NodeTemplate) {

    for (const e of this.nodeTemplates) {
      if (e.NeedsInterface2InterfacesType === nodeTemplate.ProvidesInterface2InterfaceType) {
        if (e.DefaultCreated) {
          this.addItem(nodeInstance, e, false);
        }
      }
    }
  }

  async addItem(parentNode: ITreeNode, nodeTemplate: NodeTemplate, selectNode: boolean = true) {
    let data: NodeInstance = void 0;
    if (parentNode instanceof NodeInstance) {
      data = NodeInstance.createForNodeInstanceFromTemplate(nodeTemplate, parentNode);
    } else if (parentNode instanceof BoardInterface) {
      data = NodeInstance.createForBoardInterfaceFromTemplate(nodeTemplate, parentNode);

    }

    if (!data) {
      return;
    }

    this.prepareNewItem(data, parentNode, selectNode, true);
    this.createNodeInstanceChildren(data, nodeTemplate);

    for (const e of parentNode.Children) {
      e.validate();
    }

    this.tree.instance.refresh();
  }

  private prepareNewItem(data: NodeInstance, parentNode: ITreeNode, selectNode: boolean, refreshTree: boolean) {
    data.setParent(parentNode);

    data.Name = this.translate.translate(data.Name);
    data.Description = "";

    parentNode.Children = [...parentNode.Children, data];

    this.mapList.set(data.Id, data);
    this.config = [...this.config, data];

    this.configChange.emit(this.config);
    this.setDefaultParams(data);

    if (selectNode) {
      this.selectNode(data);

      const expandedRowKeys = this.getExpandedRowKeys(data.Id, this.expandedRowKeys);
      // this.expandedRowKeys = expandedRowKeys;
    }
  }

  private setDefaultParams(node: NodeInstance, setForChilds: boolean = false) {
    if (node.hasPropertyValue("port")) {
    }

    if (setForChilds) {
      for (const x of node.Children) {
        this.setDefaultParams(x);
      }
    }
  }

  deleteItem(item: ITreeNode) {
    const parentNode = item.Parent;
    this.selectNode(parentNode);

    this.config = this.config.filter(a => a.Id !== item.Id);
    parentNode.Children = parentNode.Children.filter(a => a.Id !== item.Id);

    // this.tree.instance.refresh();
  }

  removeItem() {
    const selectedItem = this.selectedNode;
    this.deleteItem(selectedItem);
  }

  public async save(notify: boolean = true) {
    const array = [];

    for (const x of this.config) {
      if (!x.Parent) {
        array.push(x);
        break;
      }
    }

    const instances = await this.configService.save(array);
    this.convertConfig(instances);

    const settings = [];
    for (const x of array[0].Properties) {
      if (x instanceof VirtualSettingsPropertyInstance) {
        settings.push(x.setting);
      }
    }

    this.settings = await this.settingsService.saveSettings(settings);

    if (notify) {
      this.notify.notifySuccess("COMMON.SAVED");
    }

    this.selectNode(void 0);
  }

  allowDrop(dropTarget: ITreeNode) {
    const that = this;
    return (dragData: any) => {

      if (!dragData || !dropTarget) {
        return false;
      }

      const drop = that.mapList.get(dropTarget.Id);
      const drag = that.mapList.get(dragData.Id);

      if (drag.ParentId === drop.Id) {
        return false;
      }
      if (drag.Id === drop.Id) {
        return false;
      }

      if (drop instanceof BoardType) {
        return false;
      }
      if (drag instanceof BoardInterface) {
        return false;
      }

      if (drop instanceof NodeInstance) {
        if (drag instanceof NodeInstance) {

          if (drop.NodeTemplate.ProvidesInterface2InterfaceType !== drag.NodeTemplate.NeedsInterface2InterfacesType) {
            return false;
          }

          if (drop.Children.length >= drop.NodeTemplate.ProvidesInterface.MaxChilds) {
            return false;
          }

          return true;
        }
      }

      if (drop instanceof BoardInterface) {
        if (drag instanceof NodeInstance) {
          if (drop.InterfaceType.Type !== drag.NodeTemplate.NeedsInterface2InterfacesType) {
            return false;
          }

          if (drop.Children.length >= drop.InterfaceType.MaxChilds) {
            return false;
          }
          return true;
        }
      }
      return false;
    };
  }

  dropSuccess($event, dropTarget: any) {
    const dragData = $event.dragData;

    const drop = this.mapList.get(dropTarget.Id);
    const drag = this.mapList.get(dragData.Id);

    if (drag instanceof NodeInstance) {
      const parentDrag = this.mapList.get(drag.ParentId);
      parentDrag.Children = [];

      drag.setParent(drop);

      const filteredConfig = this.config.filter(a => a.Id !== drag.Id);
      drop.Children = [...drop.Children, drag];
      this.config = [...filteredConfig, drag];

      this.selectNode(drag);

      this.setDefaultParams(drag, true);
    }
  }

  doCopyItem(target: NodeInstance, toCopy: NodeInstance) {
    const node = NodeInstance.createForNodeInstanceFromTemplate(toCopy.NodeTemplate, target);

    node.Name = toCopy.Name;
    node.Description = toCopy.Description;

    this.prepareNewItem(node, target, false, false);

    for (const p of toCopy.Properties) {
      node.setPropertyValueIfPresent(p.PropertyTemplate.Key, p.Value);
    }

    for (const x of toCopy.Children) {
      this.doCopyItem(node, x);
    }
  }

  beginCopy(target: NodeInstance, toCopy: NodeInstance) {
    this.doCopyItem(target, toCopy);


    for (const e of target.Children) {
      e.validate();
    }

    this.tree.instance.refresh();
  }

  onContextMenuPreparing($event) {
    this.popoverVisible = false;
    const that = this;
    const treeNode: ITreeNode = $event.row.data;
    const model: ITreeNode = this.mapList.get(treeNode.Id);

    this.selectNode(model);

    const addMenu = [];
    $event.items = [];

    let nodeTemplates: NodeTemplate[] = NodeInstance.getSupportedTypes(model, this.nodeTemplates);

    if (nodeTemplates) {
      nodeTemplates = nodeTemplates.sort(this.sortByName);
    }

    if (nodeTemplates) {
      for (const x of nodeTemplates) {
        addMenu.push({ text: this.translate.translate(x.Name), key: "add", onItemClick: function () { that.addItem(model, x); } });
      }

      if (addMenu.length > 0) {
        $event.items.push({ text: this.translate.translate("COMMON.NEW"), items: addMenu });
      }
    }

    if (model.Parent && model instanceof NodeInstance) {
      $event.items.push({ text: this.translate.translate("COMMON.COPY"), data: model, onItemClick: function () { that.copyItem = model; } });


      if (this.copyItem && model.NodeTemplate.ProvidesInterface2InterfaceType === this.copyItem.NodeTemplate.NeedsInterface2InterfacesType) {
        $event.items.push({ text: this.translate.translate("COMMON.PASTE"), data: model, onItemClick: function () { that.beginCopy(model, that.copyItem); } });
      }

      if (model.NodeTemplate.IsDeleteable) {
        $event.items.push({ text: this.translate.translate("COMMON.DELETE"), data: model, onItemClick: function () { that.deleteItem(model); } });
      }

      if (model.NodeTemplate.This2NodeDataType > 0 && !model.isNewObject) {
        $event.items.push({ beginGroup: true, text: this.translate.translate("COMMON.READ"), data: model, onItemClick: function () { that.readNode(model); } });
      }
    }
  }

  async readNode(node: NodeInstance) {
    try {
      await this.configService.read(node);
    } catch (error) {
      super.handleError(error);
    }
  }

  async validate(prop: any) {

    if (prop instanceof PropertyInstance) {
      // prop.Parent is the NodeInstance owning the property instance, therefore parent.Parent => the parent of the node
      // we call this like that, because we need to re-validate all child nodes
      if (prop.Parent.Parent) {
        if (prop.Parent.Parent.Children) {
          for (const x of prop.Parent.Parent.Children) {
            x.validate();
          }
        }
      }
      prop.validate();

      this.tree.instance.refresh();
    }
  }

  saveLearnNodeInstances(nodeInstance: NodeInstance, learnedNodes: LearnNodeInstance[]) {
    for (const x of learnedNodes) {
      let existingNode = this.getNodeInstanceByNeedsInterface(nodeInstance, x.nodeTemplateInstance.NeedsInterface2InterfacesType);

      let created = void 0;
      if (!existingNode) {
        created = this.createFromNodeTemplate(nodeInstance, x.nodeTemplateInstance, x.propertyInstances);
        existingNode = created.node;

        this.createNodeInstanceChildren(existingNode, existingNode.NodeTemplate);

      } else {
        created = this.createFromNodeTemplate(existingNode, x.nodeTemplateInstance, x.propertyInstances);
      }

      created.node.Name = x.name;
      created.node.Description = x.description;

      for (const newNode of created.created) {
        this.prepareNewItem(newNode, newNode.Parent, false, true);
      }

    }
  }

  private createFromNodeTemplate(baseNode: NodeInstance, nodeTemplate: NodeTemplate, propertyInstances: PropertyInstance[]): { node: NodeInstance, created: NodeInstance[] } {
    const node = NodeInstance.createForNodeInstanceFromTemplate(nodeTemplate, void 0);
    let created = [node];

    if (node) {
      for (const prop of propertyInstances) {
        const nodeProp = node.Properties.find(a => a.PropertyTemplate.Key === prop.PropertyTemplate.Key);

        if (nodeProp) {
          nodeProp.Value = prop.Value;
        }
      }
    }

    let parent = void 0;
    if (baseNode.NodeTemplate.ProvidesInterface2InterfaceType === nodeTemplate.NeedsInterface2InterfacesType) {
      parent = baseNode;
    }

    parent = this.getNodeInstanceByNeedsInterface(baseNode, nodeTemplate.NeedsInterface2InterfacesType);

    if (!parent) {
      const nextTemplate = this.nodeTemplates.find(a => a.ProvidesInterface2InterfaceType === nodeTemplate.NeedsInterface2InterfacesType);
      const newNode = this.createFromNodeTemplate(baseNode, nextTemplate, propertyInstances);
      if (newNode) {
        parent = newNode.node;
        created = [...created, ...newNode.created];
      }
    }

    if (parent) {
      node.Parent = parent;
      node.This2ParentNodeInstance = parent.ObjId;
    }
    return { node: node, created: created };
  }

  private getNodeInstanceByNeedsInterface(nodeInstance: NodeInstance, needsInterfaceGuid: string): NodeInstance {
    if (nodeInstance.NodeTemplate.ProvidesInterface2InterfaceType === needsInterfaceGuid) {
      return nodeInstance;
    }

    if (!nodeInstance.Children || nodeInstance.Children.length === 0) {
      return void 0;
    }
    for (const x of nodeInstance.Children) {
      if (x.NodeTemplate.ProvidesInterface2InterfaceType === needsInterfaceGuid) {
        return x;
      }

      const child = this.getNodeInstanceByNeedsInterface(x, needsInterfaceGuid);
      if (child) {
        return child;
      }
    }

    return void 0;
  }
}
