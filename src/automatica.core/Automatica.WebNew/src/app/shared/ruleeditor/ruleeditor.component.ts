import { Component, OnInit, Input, Output, EventEmitter, AfterViewInit, OnDestroy } from "@angular/core";
import { RuleEngineService, AddLogicData } from "../../services/ruleengine.service";
import { Subscription } from "rxjs";
import { TranslationService } from "angular-l10n";
import { RulePage } from "src/app/base/model/rule-page";
import { Guid } from "src/app/base/utils/Guid";
import { LinkChangeData, Link } from "src/app/base/model/link";
import { NotifyService } from "src/app/services/notify.service";
import { NodeInstance2RulePage, NodeInterfaceInstance } from "src/app/base/model/node-instance-2-rule-page";
import { RuleInstance } from "src/app/base/model/rule-instance";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";
import { BaseComponent } from "src/app/base/base-component";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { NodeInstance } from "src/app/base/model/node-instance";
import { RuleTemplate } from "src/app/base/model/rule-template";
import { BaseModel } from "src/app/base/model/base-model";
import { PropertyInstance } from "src/app/base/model/property-instance";
import { LogicShapes } from "./shapes/logic-shape";
import { LogicLocators } from "./shapes/logic-locators";
import { LogicLables } from "./shapes/logic-label";
import { LinkService } from "./link.service";

declare var draw2d: any;

@Component({
  selector: "p3-ruleeditor",
  templateUrl: "./ruleeditor.component.html",
  styleUrls: ["./ruleeditor.component.sass"]
})
export class RuleEditorComponent extends BaseComponent implements OnInit, AfterViewInit, OnDestroy {

  @Input()
  page: RulePage;
  @Input()
  name: string;

  _selectedItem: any[];
  @Input()
  public get selectedItems(): any[] {
    return this._selectedItem;
  }
  public set selectedItems(v: any[]) {
    this._selectedItem = v;
    this.selectedItemChange.emit(v);

    this.selectedItemsChanged.emit({ page: this.page, items: v });
  }


  @Output()
  selectedItemChange = new EventEmitter<any[]>();
  @Output()
  selectedItemsChanged = new EventEmitter<any>();

  private completeMap = new Map<string, any>();
  private nodeInstance2RulePageMap = new Map<string, NodeInterfaceInstance[]>();

  private workplace: any;

  logic: any = {};

  private linkService: LinkService;

  private _isLoading: boolean = false;
  public get isLoading(): boolean {
    return this._isLoading;
  }
  public set isLoading(v: boolean) {
    this._isLoading = v;
  }


  constructor(private ruleEngineService: RuleEngineService, private dataHub: DataHubService, notify: NotifyService, translate: TranslationService) {
    super(notify, translate);
  }

  async ngOnInit() {
    this.linkService = new LinkService(this.page, this.translate);

    this.ruleEngineService.reInit.subscribe((index) => {
      // this.loadModel(this.page);
    });

    super.registerEvent(this.ruleEngineService.add, (data: AddLogicData) => {
      if (data.pageIndex === this.page.ObjId) {
        if (data.data instanceof RuleInstance) {
          this.addLogic(data.data, this.page);
        } else if (data.data instanceof NodeInstance2RulePage) {
          this.addNode(data.data, this.page);
        }
      }
    });

    super.registerEvent(this.dataHub.dispatchValue, (data) => {
      const id = data[1];

      if (this.completeMap.has(id)) {
        const d = this.completeMap.get(id);
        if (d instanceof RuleInterfaceInstance) {
          d.PortValue = data[2];
        }
      } else if (this.nodeInstance2RulePageMap.has(id)) {
        for (const x of this.nodeInstance2RulePageMap.get(id)) {
          x.PortValue = data[2];
        }
      }
    });
  }

  ngOnDestroy(): void {
    super.baseOnDestroy();
  }

  ngAfterViewInit() {
    const that = this;
    this.isLoading = true;
    setTimeout(function () {
      try {
        that.onInit();
      } catch (error) {
        console.error(error);
      } finally {

        that.isLoading = false;
      }
    }, 100);
  }

  allowDrop() {
    const that = this;
    return (dragData: NodeInstance) => {

      if (dragData && dragData.NodeTemplate && dragData.NodeTemplate.ProvidesInterface) {
        if (dragData.NodeTemplate.ProvidesInterface.Type === "00000000-0000-0000-0000-000000000001") {
          return true;
        }
      }
      return false;
    }
  };

  dropSuccess($event) {
    const event: any = $event.mouseEvent;
    const point = this.workplace.fromDocumentToCanvasCoordinate(event.clientX, event.clientY);

    if ($event.dragData instanceof NodeInstance) {
      const nodeInstance = $event.dragData;

      const node = NodeInstance2RulePage.createFromNodeInstance(nodeInstance, this.page);
      this.page.NodeInstances.push(node);

      node.X = point.getX();
      node.Y = point.getY();
      this.addNode(node, this.page);
    }
  }

  init(): any {

    LogicShapes.addShape(this.logic);
    LogicLocators.addLocators(this.logic);
    LogicLables.addLables(this.logic);

    this.workplace = new draw2d.Canvas("ruleditor-" + this.page.ObjId);

    this.workplace.installEditPolicy(new draw2d.policy.canvas.SnapToGeometryEditPolicy());
    this.workplace.installEditPolicy(new draw2d.policy.canvas.ShowGridEditPolicy());

    this.workplace.on("select", (emitter, event) => {
      if (event.figure !== null) {
        this.selectedItems = [event.figure.getUserData()];
      }
    });
  }

  onInit() {
    this.init();
    this.loadModel(this.page);

    this.linkService.isInit = false;
  }

  private addNode(element: NodeInstance2RulePage, page: RulePage) {
    for (const x of element.Inputs) {
      this.buildNodeInstanceMap(x);
    }
    for (const x of element.Outputs) {
      this.buildNodeInstanceMap(x);
    }

    const d = new this.logic.ItemShape({}, element, this.linkService);

    d.on("removed", (figure) => {
      page.removeNodeInstance(element.ObjId);
    });

    this.workplace.add(d);
  }

  private addLogic(element: RuleInstance, page: RulePage) {
    this.completeMap.set(element.ObjId, element);

    for (const x of element.Interfaces) {
      this.completeMap.set(x.ObjId, x);
    }

    const d = new this.logic.LogicShape({}, element, this.linkService);

    d.on("removed", (figure) => {
      page.removeRuleInstance(element.ObjId);
    });

    this.workplace.add(d);
  }

  loadModel(data: RulePage) {

    this.workplace.clear();

    for (const element of data.RuleInstances) {
      this.addLogic(element, data);
    }

    for (const element of data.NodeInstances) {
      this.addNode(element, data);
    }

    for (const link of data.Links) {
      const c = new draw2d.Connection();
      c.setUserData(link);
      const sourcePort = this.getSourcePort(link.from, link.fromPort);

      if (!sourcePort) {
        // could not find source port ignore link
        this.page.removeLink(link);
        continue;
      }
      c.setSource(sourcePort);

      const targetPort = this.getTargetPort(link.to, link.toPort);

      if (!targetPort) {
        // could not find target port ignore link
        this.page.removeLink(link);
        continue;
      }
      c.setTarget(targetPort);
      this.workplace.add(c);
    }
  }

  private getSourcePort(nodeId: string, portId: string) {
    const figure = this.workplace.getFigure(nodeId);


    if (figure instanceof this.logic.LogicShape) {
      const children = figure.getChildren().asArray();
      const port = children[children.length - 1].getOutputPort(portId);

      if (!port) {
        return void 0;
      }
      return port;
    }
    if (figure instanceof draw2d.shape.node.Node) {
      const port = figure.getOutputPort(portId);
      if (!port) {
        return void 0;
      }
      return port;
    }
  }
  private getTargetPort(nodeId: string, portId: string) {
    const figure = this.workplace.getFigure(nodeId);
    if (figure instanceof this.logic.LogicShape) {
      const children = figure.getChildren().asArray();
      const port = children[children.length - 1].getInputPort(portId);

      if (!port) {
        return void 0;
      }
      return port;
    }
    if (figure instanceof draw2d.shape.node.Node) {
      const port = figure.getInputPort(portId);
      if (!port) {
        return void 0;
      }
      return port;
    }
  }

  buildNodeInstanceMap(nodeInterface: NodeInterfaceInstance) {
    if (!this.nodeInstance2RulePageMap.has(nodeInterface.ObjId)) {
      this.nodeInstance2RulePageMap.set(nodeInterface.ObjId, []);
    }
    this.nodeInstance2RulePageMap.get(nodeInterface.ObjId).push(nodeInterface);
  }

}
