import { ViewChild, Component, OnInit, Input, Output, EventEmitter, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, NgZone } from "@angular/core";
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
import { DesignTimeDataService } from "src/app/services/design-time-data.service";
import { NodeInstanceService } from "src/app/services/node-instance.service";

@Component({
  selector: "p3-config-tree",
  templateUrl: "./config-tree.component.html",
  styleUrls: ["./config-tree.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush

})
export class ConfigTreeComponent extends BaseComponent implements OnInit, OnDestroy {

  NodeInstanceState: typeof NodeInstanceState = NodeInstanceState;
  expandedRowKeys: any[] = ["b0"];

  @Output()
  public configChange: EventEmitter<any> = new EventEmitter<any>();

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

  @ViewChild("tree", { static: false }) tree: DxTreeListComponent;

  @Output() onNodeSelect = new EventEmitter<ITreeNode>();
  @Output() onNodeDrag: EventEmitter<any> = new EventEmitter();
  @Output() onConfigLoaded = new EventEmitter();

  @Input() useContextMenu: boolean;

  private _isLoading: boolean;
  @Input()
  public get isLoading(): boolean {
    return this._isLoading;
  }
  public set isLoading(v: boolean) {
    this._isLoading = v;
    this.changeRef.detectChanges();
  }


  @Input()
  showLoadingPanel = false;

  @Input()
  loadOnInit = false;

  settings: Setting[] = [];

  constructor(
    private configService: ConfigService,
    private designTimeDataService: DesignTimeDataService,
    public nodeInstanceService: NodeInstanceService,
    translate: TranslationService,
    private notify: NotifyService,
    private hub: DataHubService,
    private settingsService: SettingsService,
    private appService: AppService,
    private changeRef: ChangeDetectorRef,
    private ngZone: NgZone) {

    super(notify, translate);
    this.useContextMenu = true;
    this.nextId = 0;
  }

  async ngOnInit() {

    super.baseOnInit();

    try {
      super.registerEvent(this.hub.dispatchValue, (data) => {

        this.ngZone.runOutsideAngular(() => {

          if (data[0] === 0) { // 0 = nodeinstance value
            const id = data[1];
            this.nodeInstanceService.setNodeInstanceValue(id, data[2]);
          }

        });
      });

      super.registerEvent(this.appService.isLoadingChanged, (isLoading) => {
        this.changeRef.detectChanges();
      });

    } catch (error) {
      super.handleError(error);
    }

  }

  async ngOnDestroy() {
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
    this.changeRef.detectChanges();
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
    this.changeRef.detectChanges();
    this.appService.isLoading = false;
  }

  private prepareRecurisve(nodeInstances: NodeInstance[], parent: NodeInstance) {
    for (const ni of nodeInstances) {
      const children = ni.Children;

      if (!this.nodeInstanceService.hasNodeInstance(ni.ObjId)) {
        ni.isNewObject = true;

        ni.Children = [];
        const existingParent = this.nodeInstanceService.getNodeInstance(parent.Id);
        this.prepareNewItem(ni, existingParent, false, false);
      }
      if (children) {
        this.prepareRecurisve(children, ni);
      }
    }
  }


  getNodeValue = (rowData: ITreeNode): any => {
    return rowData.Value;
  }

  getDisplayName = (rowData: ITreeNode) => {
    const instance = this.nodeInstanceService.getNodeInstance(rowData.Id);

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



  onRowClicked($event) {
    const item = this.nodeInstanceService.getNodeInstance($event.data.ObjId);
    this.selectNode(item);
  }

  selectNode(node: ITreeNode) {
    if (!node) {
      this.selectedRowKeys = [];
      this.selectedNode = void 0;
      return;
    }

    if (!this.selectedNode || this.selectedNode.Id !== node.Id) {
      this.selectedRowKeys = [node.Id];
      this.selectedNode = node;
    }

    this.tree.instance.repaint();
  }

  selectNodeById(id: any) {
    if (this.nodeInstanceService.hasNodeInstance(id)) {
      const node = this.nodeInstanceService.getNodeInstance(id);
      this.selectNode(node);

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
    this.nodeInstanceService.addNodeInstancesRec(nodeInstance, tmpConfig)

    this.tree.instance.refresh();
  }

  async createNodeInstanceChildren(nodeInstance: NodeInstance, nodeTemplate: NodeTemplate) {
    const nodeTemplates = await this.designTimeDataService.getNodeTemplates();

    for (const e of nodeTemplates) {
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

    this.nodeInstanceService.addNodeInstance(data);

    this.setDefaultParams(data);

    if (selectNode) {
      this.selectNode(data);
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

    this.nodeInstanceService.removeItem(item);
    parentNode.Children = parentNode.Children.filter(a => a.Id !== item.Id);
  }

  removeItem() {
    const selectedItem = this.selectedNode;
    this.deleteItem(selectedItem);
  }

  public async save(notify: boolean = true) {
    const array = [];

    for (const x of this.nodeInstanceService.nodeInstanceList) {
      if (!x.Parent) {
        array.push(x);
        break;
      }
    }

    const instances = await this.configService.save(array);
    this.nodeInstanceService.convertConfig(instances);

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

      const drop = that.nodeInstanceService.getNodeInstance(dropTarget.Id);
      const drag = that.nodeInstanceService.getNodeInstance(dragData.Id);

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

    const drop = this.nodeInstanceService.getNodeInstance(dropTarget.Id);
    const drag = this.nodeInstanceService.getNodeInstance(dragData.Id);

    if (drag instanceof NodeInstance) {
      const parentDrag = this.nodeInstanceService.getNodeInstance(drag.ParentId);
      parentDrag.Children = [];

      drag.setParent(drop);
      drop.Children = [...drop.Children, drag];

      this.selectNode(drag);

      this.setDefaultParams(drag, true);

      this.tree.instance.refresh();
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

  async onContextMenuPreparing($event) {

    if (!$event.row) {
      return;
    }

    this.popoverVisible = false;
    const that = this;
    const treeNode: ITreeNode = $event.row.data;
    const model: ITreeNode = this.nodeInstanceService.getNodeInstance(treeNode.Id);

    this.selectNode(model);

    const addMenu = [];
    $event.items = [];

    let nodeTemplates: NodeTemplate[] = this.nodeInstanceService.getSupportedNodeTemplates(model);

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
      this.changeRef.detectChanges();
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

  async saveLearnNodeInstances(nodeInstance: NodeInstance, learnedNodes: LearnNodeInstance[]) {
    for (const learnNode of learnedNodes) {
      let existingNode = this.nodeInstanceService.getNodeInstanceByNeedsInterface(nodeInstance, learnNode.nodeTemplateInstance.nodeTemplate.NeedsInterface2InterfacesType);

      let created = void 0;
      if (!existingNode) {
        created = await this.nodeInstanceService.createFromNodeTemplate(nodeInstance, learnNode.nodeTemplateInstance.nodeTemplate, learnNode.propertyInstances);

        existingNode = created.node;

        this.createNodeInstanceChildren(existingNode, existingNode.NodeTemplate);

      } else {
        created = await this.nodeInstanceService.createFromNodeTemplate(existingNode, learnNode.nodeTemplateInstance.nodeTemplate, learnNode.propertyInstances);
      }

      created.node.Name = learnNode.name;
      created.node.Description = learnNode.description;

      for (const newNode of created.created) {
        this.prepareNewItem(newNode, newNode.Parent, false, true);
      }

    }
  }

}
