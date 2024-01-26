import { Component, OnInit, Input, Output, EventEmitter, AfterViewInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild, ElementRef } from "@angular/core";
import { LogicEngineService, AddLogicData, SelectLogicNodeInstance } from "../../services/logicengine.service";
import { L10nTranslationService } from "angular-l10n";
import { RulePage } from "src/app/base/model/rule-page";
import { NotifyService } from "src/app/services/notify.service";
import { NodeInstance2RulePage, NodeInterfaceInstance } from "src/app/base/model/node-instance-2-rule-page";
import { RuleInstance } from "src/app/base/model/rule-instance";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";
import { BaseComponent } from "src/app/base/base-component";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { NodeInstance } from "src/app/base/model/node-instance";
import { LogicShapes } from "./shapes/logic-shape";
import { LogicLocators } from "./shapes/logic-locators";
import { LogicLables } from "./shapes/logic-label";
import { LinkService } from "./link.service";
import { AppService } from "src/app/services/app.service";
import { ThemeService } from "src/app/services/theme.service";
import { Subscription } from "rxjs";
import { ILogicErrorHandler } from "./ilogicErrorHandler";
import { ILogicInfoHandler } from "./ilogicInfoHandler";
import { NodeTemplate } from "src/app/base/model/node-template";
import { LogicEditorInstanceService } from "src/app/services/logic-editor-instance.service";
import { BaseModel } from "src/app/base/model/base-model";
import { InterfaceTypeEnum } from "src/app/base/model/interface-type";

declare var draw2d: any;

@Component({
  selector: "p3-ruleeditor",
  templateUrl: "./ruleeditor.component.html",
  styleUrls: ["./ruleeditor.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RuleEditorComponent extends BaseComponent implements OnInit, AfterViewInit, OnDestroy, ILogicErrorHandler, ILogicInfoHandler {

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
  @Output()
  removeSelectedItem = new EventEmitter<any>();


  private completeMap = new Map<string, any>();
  private nodeInstance2RulePageMap = new Map<string, NodeInterfaceInstance[]>();

  private workplace: any;

  logic: any = {};

  private linkService: LinkService;

  private themeChangedSubDraw2d: Subscription;

  public height: number;
  public infoPopupVisible: boolean = false;

  @ViewChild("loadingPangel")
  loadingPangel: ElementRef;

  constructor(private ruleEngineService: LogicEngineService,
    private dataHub: DataHubService,
    notify: NotifyService,
    translate: L10nTranslationService,
    private changeRef: ChangeDetectorRef,
    appService: AppService,
    private nodeInstanceService: LogicEditorInstanceService,
    private themeService: ThemeService) {
    super(notify, translate, appService);

  }

  notifyError(error: any) {
    this.handleError(error);
  }

  showInfoForLogic(logic: RuleInstance) {
    this.ruleEngineService.showInfo.emit(logic);
  }

  async ngOnInit() {


    super.registerEvent(this.ruleEngineService.add, (data: AddLogicData) => {
      if (data.pageId === this.page.ObjId) {
        if (data.data instanceof RuleInstance) {
          this.addLogic(data.data, this.page);
        } else if (data.data instanceof NodeInstance2RulePage) {
          this.addNode(data.data, this.page);
        }
      }
    });

    super.registerEvent(this.ruleEngineService.selected, (data: SelectLogicNodeInstance) => {
      if (data.logicPage.ObjId === this.page.ObjId) {
        // this.selectedItems = [data.logicNodeInstance];
        const object = this.completeMap.get(data.logicNodeInstance.ObjId);
        this.workplace.setCurrentSelection(object);
      }
    });


    super.registerEvent(this.ruleEngineService.removed, (data: SelectLogicNodeInstance) => {
      if (data.logicPage.ObjId === this.page.ObjId) {
        // this.selectedItems = [data.logicNodeInstance];
        const object = this.completeMap.get(data.logicNodeInstance.ObjId);
        this.workplace.remove(object);
      }
    });

    super.registerEvent(this.dataHub.dispatchValue, (data) => {
      const id = data[1];

      if (this.completeMap.has(id)) {
        const d = this.completeMap.get(id);
        if (d instanceof RuleInterfaceInstance) {
          d.PortValue = data[2].value;
        }
      } else if (this.nodeInstance2RulePageMap.has(id)) {
        for (const x of this.nodeInstance2RulePageMap.get(id)) {
          x.PortValue = data[2];
        }
      }
    });

    super.registerEvent(this.page.onZoomIn, () => {
      this.workplace.setZoom(this.workplace.getZoom() * 0.7, true);
    });
    super.registerEvent(this.page.onZoomToView, () => {
      this.workplace.setZoom(1.0, true);
    });
    super.registerEvent(this.page.onZoomOut, () => {
      this.workplace.setZoom(this.workplace.getZoom() * 1.3, true);
    });

    this.changeRef.detectChanges();
  }

  ngOnDestroy(): void {
    this.themeChangedSubDraw2d?.unsubscribe();
    super.baseOnDestroy();
  }

  ngAfterViewInit() {
    this.height = this.loadingPangel.nativeElement.clientHeight;

    this.onInit();
  }

  allowDrop() {
    return () => {

      return true;
    }
  };

  async dropSuccess($event) {
    const event: any = $event.mouseEvent;
    const point = this.workplace.fromDocumentToCanvasCoordinate(event.clientX, event.clientY);

    if ($event.dragData instanceof NodeInstance) {

      const nodeInstance: NodeInstance = $event.dragData;
      if (nodeInstance.NodeTemplate.ProvidesInterface.Type !== InterfaceTypeEnum.Value) {

        for (const child of nodeInstance.Children) {
          const success = await this.addNodeInstanceToPage(child, point)
          if (success) {
            point.y += 35;
          }
        }
      } else {
        await this.addNodeInstanceToPage(nodeInstance, point);
      }
    }
  }

  async addNodeInstanceToPage(nodeInstance: NodeInstance, point) {
    if (nodeInstance.NodeTemplate.ProvidesInterface.Type !== InterfaceTypeEnum.Value) {
      return false;
    }

    const node = NodeInstance2RulePage.createFromNodeInstance(nodeInstance, this.page);
    this.page.NodeInstances.push(node);

    node.X = point.getX();
    node.Y = point.getY();
    const copyRulePage = <RulePage>this.page.copy();
    copyRulePage.NodeInstances = [];
    copyRulePage.RuleInstances = [];

    node.RulePage = copyRulePage;

    await this.ruleEngineService.addItem({ data: node, pageId: this.page.ObjId });
    this.nodeInstanceService.addLogicNodeInstance(node);

    return true;
  }

  init(): any {

    LogicShapes.addShape(this.logic, this.ruleEngineService, this, this);
    LogicLocators.addLocators(this.logic);
    LogicLables.addLables(this.logic);

    this.workplace = new draw2d.Canvas("ruleditor-" + this.page.ObjId);
    // this.workplace.setZoom(1.3);
    // this.workplace.setScrollArea(window);
    this.workplace.installEditPolicy(new draw2d.policy.canvas.SnapToGeometryEditPolicy());
    this.workplace.installEditPolicy(new draw2d.policy.canvas.SnapToInBetweenEditPolicy());
    this.workplace.installEditPolicy(new draw2d.policy.canvas.SnapToCenterEditPolicy());
    this.workplace.installEditPolicy(new draw2d.policy.canvas.ZoomPolicy());
    this.workplace.installEditPolicy(new draw2d.policy.canvas.WheelZoomPolicy());
    let gridPolicy = new draw2d.policy.canvas.ShowGridEditPolicy();

    this.workplace.installEditPolicy(gridPolicy);
    this.themeChangedSubDraw2d = this.themeService.themeChanged.subscribe(a => {
      gridPolicy.setBackgroundColor(this.themeService.getBackgroundColor());
      gridPolicy.setGridColor(this.themeService.getGridColor());

    });
    gridPolicy.setBackgroundColor(this.themeService.getBackgroundColor());
    gridPolicy.setGridColor(this.themeService.getGridColor());

    this.workplace.on("select", (emitter, event) => {
      if (event.figure !== null) {
        this.selectedItems = [event.figure.getUserData()];
      }
    });

  }

  onInit() {
    this.init();

    const router = new draw2d.layout.connection.ManhattanBridgedConnectionRouter();
    this.linkService = new LinkService(this.page, this.translate, this.ruleEngineService, router);
    this.loadModel(this.page, router);

    this.linkService.isInit = false;
  }

  private addNode(element: NodeInstance2RulePage, page: RulePage) {
    for (const x of element.Inputs) {
      this.buildNodeInstanceMap(x);
    }
    for (const x of element.Outputs) {
      this.buildNodeInstanceMap(x);
    }

    const d = new this.logic.NodeInstanceShape({}, element, this.linkService);

    d.on("removed", async () => {
      var item = { item: element, page: page, removed: false };
      await this.removeSelectedItem.emit(item);
      if (!item.removed) {
        this.workplace.add(d);
      }
    });

    this.completeMap.set(element.ObjId, d);

    this.workplace.add(d);
  }

  private addLogic(element: RuleInstance, page: RulePage) {
    this.completeMap.set(element.ObjId, element);

    for (const x of element.Interfaces) {
      this.completeMap.set(x.ObjId, x);
    }

    const d = new this.logic.LogicShape({}, element, this.linkService);

    d.on("removed", async () => {
      var item = { item: element, page: page, removed: false };
      await this.removeSelectedItem.emit(item);

      if (!item.removed) {
        this.workplace.add(d);
      }
    });

    this.workplace.add(d);
  }

  loadModel(data: RulePage, router: any) {

    this.workplace.clear();

    for (const element of data.RuleInstances) {
      this.addLogic(element, data);
    }

    for (const element of data.NodeInstances) {
      this.addNode(element, data);
    }

    for (const link of data.Links) {
      const c = new draw2d.Connection({ router: router, userData: link });
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
