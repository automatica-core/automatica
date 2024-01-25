import { ViewChild, Component, OnInit, Input, Output, EventEmitter, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, NgZone } from "@angular/core";
import { ConfigService } from "../../services/config.service";
import { DxTreeListComponent, DxContextMenuComponent } from "devextreme-angular";
import { L10nTranslationService } from "angular-l10n";
import { LearnNodeInstance } from "../propertyeditor/propertyeditor.component";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { NodeInstanceState, NodeInstance } from "src/app/base/model/node-instance";
import { BoardType } from "src/app/base/model/board-type";
import { ITreeNode } from "src/app/base/model/ITreeNode";
import { NodeTemplate } from "src/app/base/model/node-template";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { BoardInterface } from "src/app/base/model/board-interface";
import { PropertyInstance, UpdateScope } from "src/app/base/model/property-instance";
import { LogicEditorInstanceService } from "src/app/services/logic-editor-instance.service";
import { NodeDataTypeEnum } from "src/app/base/model/node-data-type";
import { WindowState } from "src/app/base/model/window-state";
import { NodeInstance2RulePage } from "src/app/base/model/node-instance-2-rule-page";

@Component({
  selector: "p3-config-tree",
  templateUrl: "./config-tree.component.html",
  styleUrls: ["./config-tree.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush

})
export class ConfigTreeComponent extends BaseComponent implements OnInit, OnDestroy {

  NodeDataTypeEnum: typeof NodeDataTypeEnum = NodeDataTypeEnum
  WindowState: typeof WindowState = WindowState;

  NodeInstanceState: typeof NodeInstanceState = NodeInstanceState;
  expandedRowKeys: any[] = ["b0"];

  @Output()
  public configChange: EventEmitter<any> = new EventEmitter<any>();

  boardTypeOrig: BoardType;
  nodeInstances: NodeInstance[];

  copyItem: NodeInstance;

  private _selectedNode: ITreeNode;
  refreshTimeout: NodeJS.Timeout;
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

  @ViewChild("tree") tree: DxTreeListComponent;
  @ViewChild("contextMenu") contextMenu: DxContextMenuComponent;

  @Output() onNodeSelect = new EventEmitter<ITreeNode>();
  @Output() onNodeDrag: EventEmitter<any> = new EventEmitter();
  @Output() onConfigLoaded = new EventEmitter();
  @Output() onLogicNodeInstanceRemoved = new EventEmitter<NodeInstance2RulePage>();

  @Input() useContextMenu: boolean;

  @Input() loadLogicNodeInstances: boolean = false;


  @Input()
  showLoadingPanel = false;

  @Input()
  loadOnInit = false;

  constructor(
    private configService: ConfigService,
    public nodeInstanceService: LogicEditorInstanceService,
    translate: L10nTranslationService,
    private notify: NotifyService,
    private hub: DataHubService,
    appService: AppService,
    private changeRef: ChangeDetectorRef,
    private ngZone: NgZone) {

    super(notify, translate, appService);
    this.useContextMenu = true;
    this.nextId = 0;
  }

  async ngOnInit() {

    super.baseOnInit();

    this.refreshTimeout = setInterval(() => {
      this.changeRef.detectChanges();
    }, 1000);

    try {
      super.registerEvent(this.hub.dispatchValue, (data) => {

        this.ngZone.runOutsideAngular(() => {

          if (data[0] === 0) { // 0 = nodeinstance value
            const id = data[1];
            this.nodeInstanceService.setNodeInstanceValue(id, data[2]);
          }

        });
      });

      super.registerEvent(this.appService.isLoadingChanged, () => {
        this.changeRef.detectChanges();
      });

    } catch (error) {
      super.handleError(error);
    }

  }

  public async load(): Promise<any> {
    await this.nodeInstanceService.load();
  }

  async ngOnDestroy() {
    clearTimeout(this.refreshTimeout);
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
      await this.configService.scan(nodeInstance);

      await this.load();
    } catch (error) {
      super.handleError(error);
    }
    this.changeRef.detectChanges();
    this.appService.isLoading = false;
  }

  public async fileUploaded(nodeInstance: NodeInstance, fileName: string, password: string) {
    try {
      this.appService.isLoading = true;
      await this.configService.import(nodeInstance, fileName, password);
      await this.load();

    } catch (error) {
      super.handleError(error);
    }
    this.changeRef.detectChanges();
    this.appService.isLoading = false;
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

    const item = $event.data;

    if (item instanceof NodeInstance2RulePage) {
      const item = this.nodeInstanceService.getLogicNodeInstance($event.data.ObjId);
      this.selectNode(item);
    }
    else {
      const item = this.nodeInstanceService.getNodeInstance($event.data.ObjId);
      this.selectNode(item);
    }
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
      this.expandRowRecursive(node);

      //scroll to element...does not work right now!
       const nodeIndex = this.tree.instance.getRowIndexByKey(node.Id);
       const rowElement = this.tree.instance.getRowElement(nodeIndex)
       this.tree.instance.getScrollable().scrollToElement(rowElement[0]);
       this.tree.instance.repaint();
    }

  }

  expandRowRecursive(node: ITreeNode) {
    if (!node || !node.Parent) {
      return;
    }

    this.tree.instance.expandRow(node.ParentId);
    this.expandRowRecursive(node.Parent);
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

  async add(nodeInstance: NodeInstance, parent: NodeInstance) {
    this.appService.isLoading = true;
    nodeInstance.This2ParentNodeInstance = parent.ObjId;
    parent.Children = [...parent.Children, nodeInstance];

    const tmpConfig = [nodeInstance];
    this.nodeInstanceService.addNodeInstancesRec(nodeInstance, tmpConfig)

    try {
      const newNodeInstance = await this.configService.add(nodeInstance);
      newNodeInstance.Parent = nodeInstance;
      this.nodeInstanceService.updateNodeInstance(newNodeInstance);

    } catch (error) {

      super.handleError(error);

      await this.load();
    }

    this.tree.instance.refresh();
    this.appService.isLoading = false;
  }


  private async createItem(parentNode: NodeInstance, nodeTemplate: NodeTemplate, selectNode: boolean = true) {
    this.appService.isLoading = true;
    try {
      const nodeInstance = await this.addItem(parentNode, nodeTemplate, selectNode);
      this.nodeInstanceService.updateNodeInstance(nodeInstance);
    } catch (error) {
      this.handleError(error);

      await this.load();
    }


    this.tree.instance.refresh();
    this.appService.isLoading = false;
  }

  private async addItem(parentNode: NodeInstance, nodeTemplate: NodeTemplate, selectNode: boolean = true) {
    const data: NodeInstance = await this.configService.createFromTemplate(parentNode, nodeTemplate);

    if (!data) {
      return;
    }

    this.prepareNewItem(data, parentNode, selectNode);

    for (const e of parentNode.Children) {
      e.validate();
    }
    return data;
  }

  private prepareNewItem(data: NodeInstance, parentNode: ITreeNode, selectNode: boolean) {
    parentNode.Children = [...parentNode.Children, data];

    if (selectNode) {
      this.selectNode(data);
    }
  }


  async deleteItem(item: ITreeNode) {
    this.nodeInstanceService.removeItem(item);
    this.appService.isLoading = true;

    try {
      this.configService.delete(<NodeInstance>item);
      if (item.Parent && item.Parent.Children && item.Parent.Children.length > 0) {
        item.Parent.Children = item.Parent.Children.filter(a => a.Id != item.Id);
      }
    } catch (error) {
      super.handleError(error);

      await this.load();
    }
    this.selectedNode = item.Parent;
    this.appService.isLoading = false;

  }

  async removeItem() {
    const selectedItem = this.selectedNode;
    await this.deleteItem(selectedItem);
  }

  public getRootNode(): NodeInstance {
    return this.nodeInstanceService.rootNode;
  }


  public async restart() {
    this.notify.notifySuccess("COMMON.RELOADED");
    this.configService.reload();
    this.selectNode(void 0);
  }

  public refreshTree() {
    this.tree.instance.refresh();
  }

  allowDrop(dropTarget: ITreeNode) {
    const that = this;
    return (dragData: any) => {

      if (!dragData || !dropTarget) {
        return false;
      }

      if (dragData instanceof NodeInstance2RulePage) {
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
          if ((<BoardInterface>drop).InterfaceType.Type !== drag.NodeTemplate.NeedsInterface2InterfacesType) {
            return false;
          }

          if ((<BoardInterface>drop).Children.length >= (<BoardInterface>drop).InterfaceType.MaxChilds) {
            return false;
          }
          return true;
        }
      }
      return false;
    };
  }

  async dropSuccess($event, dropTarget: any) {
    const dragData = $event.dragData;

    const drop = this.nodeInstanceService.getNodeInstance(dropTarget.Id);
    const drag = this.nodeInstanceService.getNodeInstance(dragData.Id);

    if (drag instanceof NodeInstance) {
      const parentDrag = this.nodeInstanceService.getNodeInstance(drag.ParentId);
      parentDrag.Children = parentDrag.Children.filter(a => a.ObjId != drag.ObjId);

      //drag.setParent(drop);
      // const contains = drop.Children.filter(a => a.ObjId == drag.ObjId);
      // if(contains.length == 0)
      drop.Children = [...drop.Children, drag];
      drag.This2ParentNodeInstance = drop.ObjId;

      this.selectNode(drag);

      this.appService.isLoading = true;
      this.tree.instance.refresh();
      await this.configService.update(drag, UpdateScope.ParentChanged);
      this.appService.isLoading = false;
    }
  }


  async copy(toCopy: NodeInstance, target: NodeInstance) {
    this.appService.isLoading = true;
    try {
      await this.configService.copy(toCopy, target);
    } catch (error) {
      this.handleError(error);
    }
    await this.load();
    this.appService.isLoading = false;
  }

  onContextMenuHiding() {
    this.contextMenu.instance.option({ items: null, target: "#config" });
  }

  onItemContextMenu() {
    this.contextMenu.instance.hide();
  }

  onContextMenuClick($event) {
    $event.itemData.onItemClick();
  }

  async onContextMenuPreparing($event) {

    if (!$event.row) {
      return;
    }

    this.popoverVisible = false;
    const that = this;
    const treeNode: ITreeNode = $event.row.data;

    const items = [];
    if (treeNode instanceof NodeInstance) {
      const nodeInstance = this.nodeInstanceService.getNodeInstance(treeNode.Id);

      this.selectNode(nodeInstance);

      $event.items = [];

      if (!nodeInstance.ParentId) {
        return;
      }

      const addMenu = [];
      let nodeTemplates: NodeTemplate[] = await this.nodeInstanceService.getSupportedNodeTemplates(nodeInstance);

      if (nodeTemplates) {
        nodeTemplates = nodeTemplates.sort(this.sortByName);
      }

      if (nodeTemplates) {
        for (const x of nodeTemplates) {
          addMenu.push({ text: this.translate.translate(x.Name), key: "add", onItemClick: async function () { await that.createItem(nodeInstance, x); } });
        }

        if (addMenu.length > 0) {
          items.push({ text: this.translate.translate("COMMON.NEW"), items: addMenu });
        }
      }

      if (nodeInstance.ParentId && nodeInstance instanceof NodeInstance) {
        items.push({ text: this.translate.translate("COMMON.COPY"), data: nodeInstance, onItemClick: function () { that.copyItem = nodeInstance; } });



        if (this.copyItem && nodeInstance.NodeTemplate.ProvidesInterface2InterfaceType === this.copyItem.NodeTemplate.NeedsInterface2InterfacesType) {
          items.push({ text: this.translate.translate("COMMON.PASTE"), data: nodeInstance, onItemClick: function () { that.copy(that.copyItem, nodeInstance); } });
        }

        if (nodeInstance.NodeTemplate.IsDeleteable) {
          items.push({ text: this.translate.translate("COMMON.DELETE"), data: nodeInstance, onItemClick: function () { that.deleteItem(nodeInstance); } });
        }

        if (nodeInstance.NodeTemplate.This2NodeDataType > 0 && !nodeInstance.isNewObject) {
          items.push({ beginGroup: true, text: this.translate.translate("COMMON.READ"), data: nodeInstance, onItemClick: function () { that.readNode(nodeInstance); } });
        }
        items.push({ beginGroup: true, text: this.translate.translate("COMMON.COPY_VALUE"), data: nodeInstance, onItemClick: function () { navigator.clipboard.writeText(nodeInstance.Value); } });
      }


    }
    else if (treeNode instanceof NodeInstance2RulePage) {
      $event.items = [];

      items.push({ text: this.translate.translate("COMMON.DELETE"), data: treeNode, onItemClick: function () { that.onLogicNodeInstanceRemoved?.emit(treeNode); } });      
    }

    if (items.length > 0) {
      this.contextMenu.instance.option({ items: items, target: $event.event });
      await this.contextMenu.instance.show();
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

  async saveLearnNodeInstances(parentNodeInstance: NodeInstance, learnedNodes: LearnNodeInstance[]) {

    try {
      this.isLoading = true;

      for (const learnNode of learnedNodes) {
        var nodeInstance = await this.configService.createFromTemplate(parentNodeInstance, learnNode.nodeTemplateItem);

        for (const prop of learnNode.propertyInstances) {
          var property = nodeInstance.Properties.find(a => a.PropertyTemplate.Key == prop.PropertyTemplate.Key);
          if (property) {
            property.Value = prop.Value;
          }
        }

        for (const child of nodeInstance.Children) {
          for (const prop of learnNode.propertyInstances) {
            var property = child.Properties.find(a => a.PropertyTemplate.Key == prop.PropertyTemplate.Key);
            if (property) {
              property.Value = prop.Value;
            }
          }
        }

        await this.configService.update(nodeInstance, UpdateScope.SpecificProperty);

      }

      await this.load();
    }
    catch (error) {
      this.handleError(error);
    }
    finally {

      this.isLoading = false;
    }
    //   let existingNode = this.nodeInstanceService.getNodeInstanceByNeedsInterface(nodeInstance, learnNode.nodeTemplateInstance.nodeTemplate.NeedsInterface2InterfacesType);

    //   let created = void 0;
    //   if (!existingNode) {
    //     created = await this.nodeInstanceService.createFromNodeTemplate(nodeInstance, learnNode.nodeTemplateInstance.nodeTemplate, learnNode.propertyInstances);

    //     existingNode = created.node;

    //     this.createNodeInstanceChildren(existingNode, existingNode.NodeTemplate);

    //   } else {
    //     created = await this.nodeInstanceService.createFromNodeTemplate(existingNode, learnNode.nodeTemplateInstance.nodeTemplate, learnNode.propertyInstances);
    //   }

    //   created.node.Name = learnNode.name;
    //   created.node.Description = learnNode.description;

    //   for (const newNode of created.created) {
    //     this.prepareNewItem(newNode, newNode.Parent, false);
    //   }

    // }
  }

}
