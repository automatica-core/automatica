import {
  Component, OnInit, Input, Output, EventEmitter, AfterViewInit, OnDestroy, ChangeDetectionStrategy,
  ViewChild, ElementRef, DoCheck, NgZone, HostListener, ChangeDetectorRef
} from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { LogicEngineService, AddLogicData, SelectLogicNodeInstance, LogicUpdateScope } from "../../services/logicengine.service";
import { RulePage } from "src/app/base/model/rule-page";
import { NotifyService } from "src/app/services/notify.service";
import { NodeInstance2RulePage } from "src/app/base/model/node-instance-2-rule-page";
import { RuleInstance } from "src/app/base/model/rule-instance";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";
import { BaseComponent } from "src/app/base/base-component";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { NodeInstance } from "src/app/base/model/node-instance";
import { AppService } from "src/app/services/app.service";
import { LogicEditorInstanceService } from "src/app/services/logic-editor-instance.service";
import { InterfaceTypeEnum } from "src/app/base/model/interface-type";
import { NodeDataTypeEnum } from "src/app/base/model/node-data-type";
import { Link } from "src/app/base/model/link";
import { Guid } from "src/app/base/utils/Guid";

/** what flows through a port, drives the port and wire colour */
export type PortKind = "bool" | "num" | "text" | "time" | "any";

export interface PortVM {
  id: string;
  dir: "in" | "out";
  label: string;
  kind: PortKind;
  row: number;
  maxLinks: number;
  node: NodeVM;
  model: RuleInterfaceInstance | NodeInstance2RulePage;
}

export interface NodeVM {
  id: string;
  type: "rule" | "dp";
  model: RuleInstance | NodeInstance2RulePage;
  x: number;
  y: number;
  w: number;
  h: number;
  inputs: PortVM[];
  outputs: PortVM[];
  rows: number;
  dataType?: string;
}

export interface WireVM {
  link: Link;
  from: PortVM;
  to: PortVM;
  kind: PortKind;
  d: string;
}

// geometry of the blocks in screen pixels at zoom 1
const RULE_W = 220;
const HEAD_H = 42;
const ROW_H = 22;
const BODY_PAD = 6;
const DP_H = 30;
const DP_MIN_W = 150;
const DP_MAX_W = 260;
const GRID = 10;

// The stored X/Y come from the draw2d editor, whose blocks were 100px wide. The new blocks are wider,
// the stored layout is spread by these factors so existing pages keep their arrangement without overlaps.
const SX = 1.5;
const SY = 1.2;

const MIN_ZOOM = 0.2;
const MAX_ZOOM = 2.5;

@Component({
  selector: "p3-ruleeditor",
  templateUrl: "./ruleeditor.component.html",
  styleUrls: ["./ruleeditor.component.scss"],
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: false
})
export class RuleEditorComponent extends BaseComponent implements OnInit, AfterViewInit, OnDestroy, DoCheck {

  @Input()
  page: RulePage;
  @Input()
  name: string;

  @Output()
  selectedItemsChanged = new EventEmitter<any>();
  @Output()
  removeSelectedItem = new EventEmitter<any>();

  @ViewChild("canvas", { static: true })
  canvasRef: ElementRef<HTMLDivElement>;

  readonly headHeight = HEAD_H;
  readonly rowHeight = ROW_H;
  readonly bodyPad = BODY_PAD;

  nodes: NodeVM[] = [];
  wires: WireVM[] = [];

  zoom = 1;
  panX = 40;
  panY = 40;

  selectedNode: NodeVM;
  selectedLink: Link;

  /** wire being dragged out of a port */
  pending: { from: PortVM, x: number, y: number, d: string, target?: PortVM };

  private ports = new Map<string, PortVM>();
  private lastRules: any[];
  private lastNodes: any[];
  private lastLinks: any[];
  private lastRuleCount = -1;
  private lastNodeCount = -1;
  private lastLinkCount = -1;

  drag: { mode: "pan" | "node", startX: number, startY: number, originX: number, originY: number, node?: NodeVM, moved: boolean, pointerId: number };
  private nudgeTimer: any;
  private resizeObserver: ResizeObserver;
  private viewportW = 0;
  private viewportH = 0;
  private fitted = false;

  constructor(private ruleEngineService: LogicEngineService,
    private dataHub: DataHubService,
    notify: NotifyService,
    translate: L10nTranslationService,
    appService: AppService,
    private nodeInstanceService: LogicEditorInstanceService,
    private ngZone: NgZone,
    private changeRef: ChangeDetectorRef) {
    super(notify, translate, appService);
  }

  // ---------- lifecycle ----------

  /** the logic page is OnPush: anything not started by a template event has to mark the view */
  private on<T>(emitter: any, handler: (data: T) => void) {
    super.registerEvent(emitter, (data: T) => {
      handler(data);
      this.changeRef.markForCheck();
    });
  }

  ngOnInit() {
    this.on(this.ruleEngineService.add, (data: AddLogicData) => {
      if (data.pageId === this.page.ObjId && data.data instanceof RuleInstance) {
        // a block from the menu has no position yet: put it into the middle of the view
        if (!Number.isFinite(data.data.X) || !Number.isFinite(data.data.Y) || (data.data.X === 0 && data.data.Y === 0)) {
          const center = this.toModel(this.viewportW / 2 - RULE_W / 2 * this.zoom, this.viewportH / 3);
          data.data.X = center.x;
          data.data.Y = center.y;
        }
      }
      this.rebuild();
    });

    this.on(this.ruleEngineService.selected, (data: SelectLogicNodeInstance) => {
      if (data.logicPage.ObjId === this.page.ObjId) {
        const node = this.nodes.find(a => a.id === data.logicNodeInstance.ObjId);
        if (node) {
          this.select(node, false);
          this.centerOn(node);
        }
      }
    });

    this.on(this.ruleEngineService.removed, () => this.rebuild());
    this.on(this.ruleEngineService.reInit, (page: RulePage) => {
      if (page?.ObjId === this.page.ObjId) {
        this.rebuild();
      }
    });

    this.on(this.dataHub.dispatchValue, (data) => {
      const id = data[1];
      const port = this.ports.get(id);
      if (port && port.model instanceof RuleInterfaceInstance) {
        port.model.PortValue = data[2]?.value;
        return;
      }
      // data points are keyed by their node instance, one node can be on the page several times
      for (const dp of this.page.NodeInstances) {
        if (dp.This2NodeInstance === id) {
          dp.Inputs.forEach(a => a.PortValue = data[2]);
          dp.Outputs.forEach(a => a.PortValue = data[2]);
        }
      }
    });

    this.on(this.page.onZoomIn, () => this.zoomBy(1.25));
    this.on(this.page.onZoomOut, () => this.zoomBy(1 / 1.25));
    this.on(this.page.onZoomToView, () => this.fitToView());

    this.rebuild();
  }

  ngAfterViewInit() {
    const element = this.canvasRef.nativeElement;
    this.viewportW = element.clientWidth;
    this.viewportH = element.clientHeight;

    this.resizeObserver = new ResizeObserver(entries => {
      const rect = entries[0].contentRect;
      this.ngZone.run(() => {
        this.viewportW = rect.width;
        this.viewportH = rect.height;
        this.fitOnce();
        this.changeRef.markForCheck();
      });
    });
    this.resizeObserver.observe(element);

    // wheel must be non-passive to keep the page from scrolling while zooming
    this.ngZone.runOutsideAngular(() => element.addEventListener("wheel", this.onWheel, { passive: false }));
  }

  /** the first fit waits until the canvas has a size and the page has blocks */
  private fitOnce() {
    if (!this.fitted && this.viewportW > 0 && this.viewportH > 0 && this.nodes.length > 0) {
      this.fitted = true;
      this.fitToView(1);
    }
  }

  ngDoCheck() {
    if (!this.page) {
      return;
    }
    // the page component replaces or pushes into these arrays, rebuild when they change
    if (this.page.RuleInstances !== this.lastRules || this.page.RuleInstances.length !== this.lastRuleCount
      || this.page.NodeInstances !== this.lastNodes || this.page.NodeInstances.length !== this.lastNodeCount
      || this.page.Links !== this.lastLinks || this.page.Links.length !== this.lastLinkCount) {
      this.rebuild();
    }
  }

  ngOnDestroy() {
    super.baseOnDestroy();
    this.resizeObserver?.disconnect();
    this.canvasRef.nativeElement.removeEventListener("wheel", this.onWheel);
    clearTimeout(this.nudgeTimer);
  }

  // ---------- model -> view ----------

  private rebuild() {
    const page = this.page;
    this.lastRules = page.RuleInstances;
    this.lastNodes = page.NodeInstances;
    this.lastLinks = page.Links;
    this.lastRuleCount = page.RuleInstances.length;
    this.lastNodeCount = page.NodeInstances.length;
    this.lastLinkCount = page.Links.length;

    this.ports.clear();
    this.nodes = [];

    for (const rule of page.RuleInstances) {
      this.nodes.push(this.ruleNode(rule));
    }
    for (const dp of page.NodeInstances) {
      if (dp.NodeInstance) {
        this.nodes.push(this.dataPointNode(dp));
      }
    }

    if (this.selectedNode) {
      this.selectedNode = this.nodes.find(a => a.id === this.selectedNode.id);
    }

    this.buildWires();
    this.fitOnce();
    this.changeRef.markForCheck();
  }

  private ruleNode(rule: RuleInstance): NodeVM {
    const node: NodeVM = {
      id: rule.ObjId,
      type: "rule",
      model: rule,
      x: 0, y: 0,
      w: RULE_W,
      h: 0,
      inputs: [],
      outputs: [],
      rows: 0
    };
    this.placeFromModel(node);

    const ports = (list: RuleInterfaceInstance[], dir: "in" | "out") => list.map((a, i) => {
      const port: PortVM = {
        id: a.ObjId,
        dir,
        label: a.Name,
        kind: "any",
        row: i,
        maxLinks: dir === "in" ? 1 : a.FromMaxLinks,
        node,
        model: a
      };
      this.ports.set(port.id, port);
      return port;
    });
    node.inputs = ports(rule.Inputs, "in");
    node.outputs = ports(rule.Outputs, "out");
    node.rows = Math.max(node.inputs.length, node.outputs.length, 1);
    node.h = HEAD_H + BODY_PAD * 2 + node.rows * ROW_H;
    return node;
  }

  private dataPointNode(dp: NodeInstance2RulePage): NodeVM {
    const kind = this.kindOf(dp.NodeInstance.NodeTemplate?.NodeTypeEnum);
    const node: NodeVM = {
      id: dp.ObjId,
      type: "dp",
      model: dp,
      x: 0, y: 0,
      w: this.dataPointWidth(dp.Name),
      h: DP_H,
      inputs: [],
      outputs: [],
      rows: 1,
      dataType: this.shortType(dp.NodeInstance.NodeTemplate?.NodeTypeEnum)
    };
    this.placeFromModel(node);

    if (dp.Inputs.length > 0) {
      const port: PortVM = { id: dp.Inputs[0].PortId, dir: "in", label: "", kind, row: 0, maxLinks: 65535, node, model: dp };
      node.inputs.push(port);
      this.ports.set(port.id, port);
    }
    if (dp.Outputs.length > 0) {
      const port: PortVM = { id: dp.Outputs[0].PortId, dir: "out", label: "", kind, row: 0, maxLinks: 65535, node, model: dp };
      node.outputs.push(port);
      this.ports.set(port.id, port);
    }
    return node;
  }

  private placeFromModel(node: NodeVM) {
    const model = node.model;
    const x = Number.isFinite(model.X) ? Math.abs(model.X) : 0;
    const y = Number.isFinite(model.Y) ? Math.abs(model.Y) : 0;
    node.x = x * SX;
    node.y = y * SY;
  }

  private buildWires() {
    const wires: WireVM[] = [];
    for (const link of [...this.page.Links]) {
      const from = this.ports.get(link.fromPort);
      const to = this.ports.get(link.toPort);
      if (!from || !to) {
        // the block of one end is gone (or never loaded), the link cannot be drawn
        continue;
      }
      wires.push({ link, from, to, kind: this.wireKind(from, to), d: "" });
    }
    this.wires = wires;
    this.updateWirePaths();
    this.colorRulePorts();
  }

  /** rule ports have no data type, they take the type of a connected data point */
  private colorRulePorts() {
    for (const node of this.nodes) {
      if (node.type !== "rule") {
        continue;
      }
      for (const port of [...node.inputs, ...node.outputs]) {
        port.kind = this.wires.find(w => (w.from === port || w.to === port) && w.kind !== "any")?.kind ?? "any";
      }
    }
  }

  private wireKind(from: PortVM, to: PortVM): PortKind {
    if (from.node.type === "dp") {
      return from.kind;
    }
    if (to.node.type === "dp") {
      return to.kind;
    }
    return "any";
  }

  private updateWirePaths(nodes?: Set<NodeVM>) {
    for (const wire of this.wires) {
      if (nodes && !nodes.has(wire.from.node) && !nodes.has(wire.to.node)) {
        continue;
      }
      const a = this.anchor(wire.from);
      const b = this.anchor(wire.to);
      wire.d = this.curve(a.x, a.y, b.x, b.y);
    }
  }

  anchor(port: PortVM) {
    const node = port.node;
    const y = node.type === "rule"
      ? node.y + HEAD_H + BODY_PAD + port.row * ROW_H + ROW_H / 2
      : node.y + DP_H / 2;
    return { x: port.dir === "in" ? node.x : node.x + node.w, y };
  }

  private curve(x1: number, y1: number, x2: number, y2: number) {
    const dx = Math.max(40, Math.abs(x2 - x1) / 2);
    return `M ${x1} ${y1} C ${x1 + dx} ${y1}, ${x2 - dx} ${y2}, ${x2} ${y2}`;
  }

  private kindOf(type: NodeDataTypeEnum): PortKind {
    switch (type) {
      case NodeDataTypeEnum.Boolean:
        return "bool";
      case NodeDataTypeEnum.Integer:
      case NodeDataTypeEnum.Double:
      case NodeDataTypeEnum.WindowState:
        return "num";
      case NodeDataTypeEnum.String:
        return "text";
      case NodeDataTypeEnum.Date:
      case NodeDataTypeEnum.DateTime:
      case NodeDataTypeEnum.Time:
        return "time";
    }
    return "any";
  }

  private shortType(type: NodeDataTypeEnum) {
    switch (type) {
      case NodeDataTypeEnum.Boolean: return "bool";
      case NodeDataTypeEnum.Integer: return "int";
      case NodeDataTypeEnum.Double: return "num";
      case NodeDataTypeEnum.String: return "text";
      case NodeDataTypeEnum.Date: return "date";
      case NodeDataTypeEnum.DateTime: return "dt";
      case NodeDataTypeEnum.Time: return "time";
      case NodeDataTypeEnum.WindowState: return "win";
    }
    return "?";
  }

  // ---------- template helpers ----------

  get worldTransform() {
    return `translate(${this.panX}px, ${this.panY}px) scale(${this.zoom})`;
  }

  get gridSize() {
    const size = 20 * this.zoom;
    return `${size}px ${size}px`;
  }

  get gridPosition() {
    return `${this.panX}px ${this.panY}px`;
  }

  get zoomPercent() {
    return Math.round(this.zoom * 100);
  }

  title(node: NodeVM) {
    return node.model.Name;
  }

  subtitle(node: NodeVM) {
    if (node.model instanceof RuleInstance) {
      return this.translate.translate(node.model.RuleTemplateName);
    }
    return "";
  }

  fullName(node: NodeVM) {
    return node.model instanceof NodeInstance2RulePage ? node.model.FullName : node.model.Name;
  }

  /** read on every check: plugin translations can arrive after the page was built */
  portLabel(port: PortVM) {
    return port.model instanceof RuleInterfaceInstance ? port.model.Name : "";
  }

  inverted(port: PortVM) {
    return port.model.Inverted;
  }

  canInvert(port: PortVM) {
    if (port.model instanceof NodeInstance2RulePage) {
      return port.kind === "bool";
    }
    return true;
  }

  value(port: PortVM) {
    let value: any;
    if (port.model instanceof RuleInterfaceInstance) {
      value = port.model.PortValue;
    } else {
      const intf = port.dir === "in" ? port.model.Inputs[0] : port.model.Outputs[0];
      value = intf?.PortValue?.value;
      if (value === undefined) {
        const ni = port.model.NodeInstance;
        value = (port.dir === "in" ? ni?.WriteValue : ni?.ReadValue) ?? ni?.Value;
      }
    }
    return this.format(value);
  }

  private format(value: any): string {
    if (value === undefined || value === null || value === "") {
      return "";
    }
    if (typeof value === "boolean") {
      return value ? "1" : "0";
    }
    if (typeof value === "number") {
      return Number.isInteger(value) ? String(value) : value.toFixed(2);
    }
    if (typeof value === "object") {
      return "{…}";
    }
    const text = String(value);
    return text.length > 14 ? text.substring(0, 13) + "…" : text;
  }

  isLinked(port: PortVM) {
    return this.wires.some(w => w.from === port || w.to === port);
  }

  isTargetCandidate(port: PortVM) {
    return this.pending && this.canConnect(this.pending.from, port, false);
  }

  // ---------- selection ----------

  select(node: NodeVM, emit = true) {
    this.selectedNode = node;
    this.selectedLink = void 0;
    if (emit) {
      this.selectedItemsChanged.emit({ page: this.page, items: node ? [node.model] : [] });
    }
  }

  selectLink(event: PointerEvent, link: Link) {
    event.stopPropagation();
    this.selectedLink = link;
    this.selectedNode = void 0;
    this.canvasRef.nativeElement.focus();
    this.selectedItemsChanged.emit({ page: this.page, items: [link] });
  }

  // ---------- pointer handling ----------

  onCanvasPointerDown(event: PointerEvent) {
    if (event.button !== 0 && event.button !== 1) {
      return;
    }
    this.canvasRef.nativeElement.focus();
    this.startDrag(event, "pan");
  }

  onNodePointerDown(event: PointerEvent, node: NodeVM) {
    if (event.button !== 0) {
      return;
    }
    event.stopPropagation();
    this.canvasRef.nativeElement.focus();
    if (this.selectedNode !== node) {
      this.select(node);
    }
    this.startDrag(event, "node", node);
  }

  onPortPointerDown(event: PointerEvent, port: PortVM) {
    if (event.button !== 0) {
      return;
    }
    event.stopPropagation();
    event.preventDefault();
    const used = this.wires.filter(w => w.from === port || w.to === port).length;
    if (used >= port.maxLinks) {
      this.notifyService.notifyWarning(port.dir === "in" ? "UI.LOGIC.INPUT_TAKEN" : "UI.LOGIC.OUTPUT_FULL");
      return;
    }
    const a = this.anchor(port);
    this.pending = { from: port, x: a.x, y: a.y, d: this.curve(a.x, a.y, a.x, a.y) };
    this.canvasRef.nativeElement.setPointerCapture(event.pointerId);
  }

  private startDrag(event: PointerEvent, mode: "pan" | "node", node?: NodeVM) {
    this.drag = {
      mode,
      node,
      startX: event.clientX,
      startY: event.clientY,
      originX: mode === "pan" ? this.panX : node.x,
      originY: mode === "pan" ? this.panY : node.y,
      moved: false,
      pointerId: event.pointerId
    };
    this.canvasRef.nativeElement.setPointerCapture(event.pointerId);
  }

  @HostListener("pointermove", ["$event"])
  onPointerMove(event: PointerEvent) {
    if (this.pending) {
      const p = this.clientToWorld(event.clientX, event.clientY);
      const target = this.portAt(event.clientX, event.clientY);
      this.pending.target = target && this.canConnect(this.pending.from, target, false) ? target : void 0;
      const end = this.pending.target ? this.anchor(this.pending.target) : p;
      const start = this.anchor(this.pending.from);
      this.pending.d = this.pending.from.dir === "out"
        ? this.curve(start.x, start.y, end.x, end.y)
        : this.curve(end.x, end.y, start.x, start.y);
      return;
    }
    if (!this.drag) {
      return;
    }
    const dx = event.clientX - this.drag.startX;
    const dy = event.clientY - this.drag.startY;
    if (!this.drag.moved && Math.abs(dx) + Math.abs(dy) < 3) {
      return;
    }
    this.drag.moved = true;

    if (this.drag.mode === "pan") {
      this.panX = this.drag.originX + dx;
      this.panY = this.drag.originY + dy;
    } else {
      const node = this.drag.node;
      node.x = Math.max(0, this.snap(this.drag.originX + dx / this.zoom));
      node.y = Math.max(0, this.snap(this.drag.originY + dy / this.zoom));
      this.updateWirePaths(new Set([node]));
    }
  }

  @HostListener("pointerup", ["$event"])
  async onPointerUp(event: PointerEvent) {
    if (this.pending) {
      const pending = this.pending;
      this.pending = void 0;
      const target = this.portAt(event.clientX, event.clientY);
      if (target && target !== pending.from) {
        await this.connect(pending.from, target);
      }
      return;
    }
    const drag = this.drag;
    this.drag = void 0;
    if (!drag) {
      return;
    }
    if (drag.mode === "pan" && !drag.moved) {
      // a click on the empty canvas clears the selection
      if (this.selectedNode || this.selectedLink) {
        this.select(void 0);
      }
    } else if (drag.mode === "node" && drag.moved) {
      await this.saveNodePosition(drag.node);
    }
  }

  @HostListener("pointercancel")
  onPointerCancel() {
    this.pending = void 0;
    this.drag = void 0;
  }

  private onWheel = (event: WheelEvent) => {
    event.preventDefault();
    const factor = Math.exp(-event.deltaY * (event.ctrlKey ? 0.01 : 0.0015));
    const rect = this.canvasRef.nativeElement.getBoundingClientRect();
    this.ngZone.run(() => {
      this.zoomAt(this.zoom * factor, event.clientX - rect.left, event.clientY - rect.top);
      this.changeRef.markForCheck();
    });
  }

  // ---------- keyboard ----------

  async onKeyDown(event: KeyboardEvent) {
    if (event.key === "Escape") {
      this.pending = void 0;
      this.select(void 0);
      return;
    }
    if (event.key === "Delete" || event.key === "Backspace") {
      event.preventDefault();
      await this.deleteSelection();
      return;
    }
    const step = event.shiftKey ? 1 : GRID;
    const moves: { [key: string]: [number, number] } = { ArrowLeft: [-step, 0], ArrowRight: [step, 0], ArrowUp: [0, -step], ArrowDown: [0, step] };
    const move = moves[event.key];
    if (move && this.selectedNode) {
      event.preventDefault();
      const node = this.selectedNode;
      node.x = Math.max(0, node.x + move[0]);
      node.y = Math.max(0, node.y + move[1]);
      this.updateWirePaths(new Set([node]));
      clearTimeout(this.nudgeTimer);
      this.nudgeTimer = setTimeout(() => this.saveNodePosition(node), 400);
    } else if (event.key === "+" || event.key === "=") {
      this.zoomBy(1.25);
    } else if (event.key === "-") {
      this.zoomBy(1 / 1.25);
    } else if (event.key === "0") {
      this.fitToView();
    }
  }

  // ---------- editing ----------

  private canConnect(from: PortVM, to: PortVM, notify: boolean) {
    if (!to || from === to || from.dir === to.dir || from.node === to.node) {
      return false;
    }
    const out = from.dir === "out" ? from : to;
    const input = from.dir === "in" ? from : to;
    if (this.wires.some(w => w.from === out && w.to === input)) {
      return false;
    }
    if (this.wires.filter(w => w.to === input).length >= input.maxLinks) {
      if (notify) {
        this.notifyService.notifyWarning("UI.LOGIC.INPUT_TAKEN");
      }
      return false;
    }
    if (this.wires.filter(w => w.from === out).length >= out.maxLinks) {
      if (notify) {
        this.notifyService.notifyWarning("UI.LOGIC.OUTPUT_FULL");
      }
      return false;
    }
    return true;
  }

  private async connect(a: PortVM, b: PortVM) {
    if (!this.canConnect(a, b, true)) {
      return;
    }
    const out = a.dir === "out" ? a : b;
    const input = a.dir === "in" ? a : b;

    const link = new Link();
    link.ObjId = Guid.MakeNew().ToString();
    link.This2RulePage = this.page.ObjId;

    if (out.model instanceof RuleInterfaceInstance) {
      link.FromRuleInstance = out.model;
      link.This2RuleInterfaceInstanceOutput = out.model.ObjId;
    } else {
      link.FromNodeInstance = out.model;
      link.This2NodeInstance2RulePageOutput = out.model.ObjId;
    }
    if (input.model instanceof RuleInterfaceInstance) {
      link.ToRuleInstance = input.model;
      link.This2RuleInterfaceInstanceInput = input.model.ObjId;
    } else {
      link.ToNodeInstance = input.model;
      link.This2NodeInstance2RulePageInput = input.model.ObjId;
    }

    this.page.Links.push(link);
    this.rebuild();

    try {
      await this.ruleEngineService.addOrUpdateLink(link);
    } catch (error) {
      this.page.removeLink(link);
      this.rebuild();
      this.handleError(error);
    }
  }

  private async removeLink(link: Link) {
    this.page.removeLink(link);
    if (this.selectedLink === link) {
      this.selectedLink = void 0;
    }
    this.rebuild();
    await this.ruleEngineService.removeLink(link);
  }

  async deleteSelection() {
    try {
      if (this.selectedLink) {
        await this.removeLink(this.selectedLink);
        this.select(void 0);
      } else if (this.selectedNode) {
        const node = this.selectedNode;
        // links first, as the draw2d editor did when a block was removed
        for (const wire of this.wires.filter(w => w.from.node === node || w.to.node === node)) {
          await this.removeLink(wire.link);
        }
        const item = { item: node.model, page: this.page, removed: false };
        this.removeSelectedItem.emit(item);
        this.select(void 0);
        this.rebuild();
      }
    } catch (error) {
      this.handleError(error);
    }
  }

  async toggleInvert(event: MouseEvent, port: PortVM) {
    event.stopPropagation();
    if (!this.canInvert(port)) {
      return;
    }
    port.model.Inverted = !port.model.Inverted;
    try {
      const owner = port.model instanceof RuleInterfaceInstance ? <RuleInstance>port.node.model : port.model;
      await this.ruleEngineService.updateItem(owner, LogicUpdateScope.InvertedValueUpdated);
    } catch (error) {
      this.handleError(error);
    }
  }

  private async saveNodePosition(node: NodeVM) {
    const model = node.model;
    model.X = node.x / SX;
    model.Y = node.y / SY;
    try {
      if (model.itemMoved) {
        await this.ruleEngineService.updateItem(model, LogicUpdateScope.Drag);
      }
    } catch (error) {
      this.handleError(error);
    }
  }

  /** data points dropped from the config tree */
  allowDrop() {
    return () => true;
  }

  async dropSuccess($event) {
    const event: MouseEvent = $event.mouseEvent;
    const world = this.clientToWorld(event.clientX, event.clientY);
    const point = { x: world.x / SX, y: world.y / SY };

    if ($event.dragData instanceof NodeInstance) {
      const nodeInstance: NodeInstance = $event.dragData;
      if (nodeInstance.NodeTemplate.ProvidesInterface.Type !== InterfaceTypeEnum.Value) {
        for (const child of nodeInstance.Children) {
          if (await this.addNodeInstanceToPage(<NodeInstance>child, point)) {
            point.y += 35;
          }
        }
      } else {
        await this.addNodeInstanceToPage(nodeInstance, point);
      }
    }
  }

  private async addNodeInstanceToPage(nodeInstance: NodeInstance, point: { x: number, y: number }) {
    if (nodeInstance.NodeTemplate.ProvidesInterface.Type !== InterfaceTypeEnum.Value) {
      return false;
    }

    const node = NodeInstance2RulePage.createFromNodeInstance(nodeInstance, this.page);
    node.X = point.x;
    node.Y = point.y;

    const copyRulePage = <RulePage>this.page.copy();
    copyRulePage.NodeInstances = [];
    copyRulePage.RuleInstances = [];
    node.RulePage = copyRulePage;

    this.page.NodeInstances.push(node);

    try {
      await this.ruleEngineService.addItem({ data: node, pageId: this.page.ObjId });
      this.nodeInstanceService.addLogicNodeInstance(node);
    } catch (error) {
      this.handleError(error);
    }
    return true;
  }

  private measureContext: CanvasRenderingContext2D;

  /** data point blocks size to their name; measured here so the wires know the width without a layout pass */
  private dataPointWidth(name: string) {
    if (!this.measureContext) {
      this.measureContext = document.createElement("canvas").getContext("2d");
    }
    let text = 0;
    if (this.measureContext) {
      this.measureContext.font = "600 13px \"Figtree Variable\", \"Segoe UI\", sans-serif";
      text = this.measureContext.measureText(name ?? "").width;
    } else {
      text = (name?.length ?? 0) * 7;
    }
    // badge + gaps + value column
    return Math.min(DP_MAX_W, Math.max(DP_MIN_W, Math.ceil(text) + 112));
  }

  // ---------- zoom / view ----------

  zoomBy(factor: number) {
    this.zoomAt(this.zoom * factor, this.viewportW / 2, this.viewportH / 2);
  }

  private zoomAt(zoom: number, cx: number, cy: number) {
    zoom = Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, zoom));
    const wx = (cx - this.panX) / this.zoom;
    const wy = (cy - this.panY) / this.zoom;
    this.zoom = zoom;
    this.panX = cx - wx * zoom;
    this.panY = cy - wy * zoom;
  }

  fitToView(maxZoom = 1.25) {
    const box = this.contentBox();
    if (!box || this.viewportW === 0) {
      this.zoom = 1;
      this.panX = 40;
      this.panY = 40;
      return;
    }
    const margin = 48;
    const zoom = Math.min(maxZoom, (this.viewportW - margin * 2) / box.w, (this.viewportH - margin * 2) / box.h);
    this.zoom = Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, zoom));
    this.panX = (this.viewportW - box.w * this.zoom) / 2 - box.x * this.zoom;
    this.panY = (this.viewportH - box.h * this.zoom) / 2 - box.y * this.zoom;
  }

  private centerOn(node: NodeVM) {
    this.panX = this.viewportW / 2 - (node.x + node.w / 2) * this.zoom;
    this.panY = this.viewportH / 2 - (node.y + node.h / 2) * this.zoom;
  }

  contentBox() {
    if (this.nodes.length === 0) {
      return void 0;
    }
    let x1 = Infinity, y1 = Infinity, x2 = -Infinity, y2 = -Infinity;
    for (const n of this.nodes) {
      x1 = Math.min(x1, n.x);
      y1 = Math.min(y1, n.y);
      x2 = Math.max(x2, n.x + n.w);
      y2 = Math.max(y2, n.y + n.h);
    }
    return { x: x1, y: y1, w: Math.max(1, x2 - x1), h: Math.max(1, y2 - y1) };
  }

  // ---------- minimap ----------

  get minimap() {
    const box = this.contentBox();
    if (!box) {
      return void 0;
    }
    const view = { x: -this.panX / this.zoom, y: -this.panY / this.zoom, w: this.viewportW / this.zoom, h: this.viewportH / this.zoom };
    const x1 = Math.min(box.x, view.x) - 40, y1 = Math.min(box.y, view.y) - 40;
    const x2 = Math.max(box.x + box.w, view.x + view.w) + 40, y2 = Math.max(box.y + box.h, view.y + view.h) + 40;
    return { viewBox: `${x1} ${y1} ${x2 - x1} ${y2 - y1}`, view, x1, y1, w: x2 - x1, h: y2 - y1 };
  }

  onMinimapPointer(event: PointerEvent, svg: SVGSVGElement) {
    event.stopPropagation();
    if (event.type === "pointerdown") {
      svg.setPointerCapture(event.pointerId);
    } else if (event.buttons !== 1) {
      return;
    }
    const map = this.minimap;
    if (!map) {
      return;
    }
    const rect = svg.getBoundingClientRect();
    // preserveAspectRatio meet: the drawing is centred in the element
    const scale = Math.min(rect.width / map.w, rect.height / map.h);
    const offsetX = (rect.width - map.w * scale) / 2;
    const offsetY = (rect.height - map.h * scale) / 2;
    const wx = map.x1 + (event.clientX - rect.left - offsetX) / scale;
    const wy = map.y1 + (event.clientY - rect.top - offsetY) / scale;
    this.panX = this.viewportW / 2 - wx * this.zoom;
    this.panY = this.viewportH / 2 - wy * this.zoom;
  }

  // ---------- coordinates ----------

  private clientToWorld(clientX: number, clientY: number) {
    const rect = this.canvasRef.nativeElement.getBoundingClientRect();
    return { x: (clientX - rect.left - this.panX) / this.zoom, y: (clientY - rect.top - this.panY) / this.zoom };
  }

  private toModel(screenX: number, screenY: number) {
    return { x: ((screenX - this.panX) / this.zoom) / SX, y: ((screenY - this.panY) / this.zoom) / SY };
  }

  private snap(v: number) {
    return Math.round(v / GRID) * GRID;
  }

  private portAt(clientX: number, clientY: number): PortVM {
    const element = document.elementFromPoint(clientX, clientY)?.closest("[data-port]");
    return element ? this.ports.get(element.getAttribute("data-port")) : void 0;
  }

  trackNode(index: number, node: NodeVM) {
    return node.id;
  }
}
