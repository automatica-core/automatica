import { ChangeDetectionStrategy, Component, NO_ERRORS_SCHEMA, ViewChild } from "@angular/core";
import { ComponentFixture, TestBed, fakeAsync, tick, flush } from "@angular/core/testing";
import { L10nTranslationService } from "angular-l10n";

import { RuleEditorComponent, NodeVM, PortVM } from "./ruleeditor.component";
import { LogicEngineService, LogicUpdateScope } from "src/app/services/logicengine.service";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { LogicEditorInstanceService } from "src/app/services/logic-editor-instance.service";
import { RulePage } from "src/app/base/model/rule-page";
import { RuleInstance } from "src/app/base/model/rule-instance";
import { NodeInstance2RulePage } from "src/app/base/model/node-instance-2-rule-page";
import { NodeDataTypeEnum } from "src/app/base/model/node-data-type";
import { InterfaceTypeEnum } from "src/app/base/model/interface-type";
import { RuleTemplate } from "src/app/base/model/rule-template";
import { Link } from "src/app/base/model/link";
import { WebApiException, ExceptionSeverity } from "src/app/base/model/web-api-exception";
import {
  FakeTranslateAsyncPipe, createNotifySpy, createTranslateSpy, emitter, flushPromises, key, nextFrames, pointer, rejected
} from "src/app/testing/test-helpers";
import {
  input, linkDataPointToRule, linkRuleToDataPoint, linkRules, makeDataPoint, makeNodeInstance, makePage, makeRule, output
} from "src/app/testing/logic-fixtures";

// geometry of the editor (ruleeditor.component.ts), repeated here on purpose: a change of the layout must be a conscious one
const RULE_W = 220;
const HEAD_H = 42;
const ROW_H = 22;
const BODY_PAD = 6;
const DP_H = 30;
const SX = 1.5;
const SY = 1.2;

function curve(x1: number, y1: number, x2: number, y2: number) {
  const dx = Math.max(40, Math.abs(x2 - x1) / 2);
  return `M ${x1} ${y1} C ${x1 + dx} ${y1}, ${x2 - dx} ${y2}, ${x2} ${y2}`;
}

@Component({
  template: `<div style="position: relative; width: 800px; height: 600px;">
    <p3-ruleeditor [page]="page" (selectedItemsChanged)="selected.push($event)" (removeSelectedItem)="removed.push($event)"></p3-ruleeditor>
  </div>`,
  // Angular 22 components are OnPush unless they say otherwise; this host re-checks on every detectChanges
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: false
})
class HostComponent {
  page: RulePage;
  selected: any[] = [];
  removed: any[] = [];
  @ViewChild(RuleEditorComponent) editor: RuleEditorComponent;
}

describe("RuleEditorComponent", () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let editor: RuleEditorComponent;
  let engine: any;
  let dataHub: any;
  let notify: jasmine.SpyObj<NotifyService>;
  let translate: jasmine.SpyObj<L10nTranslationService>;
  let instances: jasmine.SpyObj<LogicEditorInstanceService>;
  let page: RulePage;

  beforeEach(() => {
    translate = createTranslateSpy();
    notify = createNotifySpy();
    engine = {
      add: emitter(), selected: emitter(), removed: emitter(), reInit: emitter(), showInfo: emitter(),
      addOrUpdateLink: jasmine.createSpy("addOrUpdateLink").and.resolveTo(undefined),
      removeLink: jasmine.createSpy("removeLink").and.resolveTo(undefined),
      updateItem: jasmine.createSpy("updateItem").and.resolveTo(undefined),
      addItem: jasmine.createSpy("addItem").and.resolveTo(undefined)
    };
    dataHub = { dispatchValue: emitter() };
    instances = jasmine.createSpyObj<LogicEditorInstanceService>("LogicEditorInstanceService", ["addLogicNodeInstance"]);

    TestBed.configureTestingModule({
      declarations: [HostComponent, RuleEditorComponent],
      imports: [FakeTranslateAsyncPipe],
      providers: [
        { provide: LogicEngineService, useValue: engine },
        { provide: DataHubService, useValue: dataHub },
        { provide: NotifyService, useValue: notify },
        { provide: L10nTranslationService, useValue: translate },
        { provide: LogicEditorInstanceService, useValue: instances },
        AppService
      ],
      schemas: [NO_ERRORS_SCHEMA]
    });

    // synthetic pointer events have no active pointer to capture
    spyOn(Element.prototype, "setPointerCapture");
    // handleError logs every error it reports
    spyOn(console, "log");
    page = makePage();
  });

  async function create(p: RulePage = page) {
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    host.page = p;
    // no checkNoChanges on the first pass: ngAfterViewInit reads the canvas size the minimap depends on (see the BUG spec below)
    fixture.detectChanges(false);
    editor = host.editor;
    // the first ResizeObserver callback (and with it the first fit) arrives with the next frame
    await nextFrames();
    fixture.detectChanges();
    return editor;
  }

  /** a known view: zoom 1, pan 40/40, 800x600 viewport */
  function resetView(zoom = 1, panX = 40, panY = 40) {
    editor.zoom = zoom;
    editor.panX = panX;
    editor.panY = panY;
    (editor as any).viewportW = 800;
    (editor as any).viewportH = 600;
  }

  function nodeOf(model: any): NodeVM {
    return editor.nodes.find(a => a.model === model);
  }

  function portOf(id: string): PortVM {
    for (const node of editor.nodes) {
      const port = [...node.inputs, ...node.outputs].find(a => a.id === id);
      if (port) {
        return port;
      }
    }
    return void 0;
  }

  function portElement(id: string): HTMLElement {
    return fixture.nativeElement.querySelector(`[data-port="${id}"]`);
  }

  function canvas(): HTMLElement {
    return fixture.nativeElement.querySelector(".canvas");
  }

  function hostElement(): HTMLElement {
    return fixture.nativeElement.querySelector("p3-ruleeditor");
  }

  function addRule(name: string, inputs: any[], outputs: any[], x = 0, y = 0) {
    const rule = makeRule(translate, name, inputs, outputs, x, y);
    page.RuleInstances.push(rule);
    return rule;
  }

  function addDataPoint(name: string, type: NodeDataTypeEnum, x = 0, y = 0, opts: any = {}) {
    const dp = makeDataPoint(page, makeNodeInstance(name, type, opts), x, y);
    page.NodeInstances.push(dp);
    return dp;
  }

  // ---------------------------------------------------------------------------------------------
  describe("model to nodes", () => {
    it("ngOnInit_PageWithRulesAndDataPoints_BuildsOneNodePerBlock", async () => {
      addRule("AND", ["I1", "I2"], ["O1"]);
      addRule("OR", ["I1"], ["O1"]);
      addDataPoint("Lamp", NodeDataTypeEnum.Boolean);
      await create();

      expect(editor.nodes.length).toBe(3);
      expect(editor.nodes.filter(a => a.type === "rule").length).toBe(2);
      expect(editor.nodes.filter(a => a.type === "dp").length).toBe(1);
      expect(fixture.nativeElement.querySelectorAll(".node.rule").length).toBe(2);
      expect(fixture.nativeElement.querySelectorAll(".node.dp").length).toBe(1);
    });

    it("rebuild_DataPointWithoutNodeInstance_IsSkipped", async () => {
      const dp = new NodeInstance2RulePage();
      dp.ObjId = "orphan";
      page.NodeInstances.push(dp);
      await create();

      expect(editor.nodes.length).toBe(0);
    });

    it("ruleNode_TwoInputsOneOutput_HeightFollowsTheRowCount", async () => {
      const rule = addRule("AND", ["I1", "I2"], ["O1"]);
      await create();

      const node = nodeOf(rule);
      expect(node.w).toBe(RULE_W);
      expect(node.rows).toBe(2);
      expect(node.h).toBe(HEAD_H + BODY_PAD * 2 + 2 * ROW_H);
      expect(node.inputs.map(a => a.label)).toEqual(["I1", "I2"]);
      expect(node.outputs.map(a => a.label)).toEqual(["O1"]);
      expect(node.inputs.map(a => a.row)).toEqual([0, 1]);
    });

    it("ruleNode_NoPorts_KeepsOneRow", async () => {
      const rule = addRule("CONST", [], []);
      await create();

      expect(nodeOf(rule).rows).toBe(1);
      expect(nodeOf(rule).h).toBe(HEAD_H + BODY_PAD * 2 + ROW_H);
    });

    it("placeFromModel_StoredDraw2dCoordinates_AreSpreadBy1_5And1_2", async () => {
      const rule = addRule("AND", ["I1"], ["O1"], 100, 50);
      await create();

      expect(nodeOf(rule).x).toBe(100 * SX);
      expect(nodeOf(rule).y).toBe(50 * SY);
    });

    it("placeFromModel_NegativeCoordinates_UseTheAbsoluteValue", async () => {
      const rule = addRule("AND", ["I1"], ["O1"], -100, -50);
      await create();

      expect(nodeOf(rule).x).toBe(100 * SX);
      expect(nodeOf(rule).y).toBe(50 * SY);
    });

    it("placeFromModel_MissingCoordinates_StartAtZero", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      rule._x = void 0;
      rule._y = NaN;
      await create();

      expect(nodeOf(rule).x).toBe(0);
      expect(nodeOf(rule).y).toBe(0);
    });

    it("ruleNode_PortLimits_InputTakesOneLinkOutputFollowsMaxLinks", async () => {
      const rule = addRule("AND", ["I1"], ["O_UNLIMITED", { name: "O_TWO", maxLinks: 2 }]);
      await create();

      const node = nodeOf(rule);
      expect(node.inputs[0].maxLinks).toBe(1);
      expect(node.outputs[0].maxLinks).toBe(65535);
      expect(node.outputs[1].maxLinks).toBe(2);
    });

    it("dataPointNode_ReadAndWriteable_HasAnInputAndAnOutputPort", async () => {
      const dp = addDataPoint("Lamp", NodeDataTypeEnum.Boolean);
      await create();

      const node = nodeOf(dp);
      expect(node.inputs.length).toBe(1);
      expect(node.outputs.length).toBe(1);
      expect(node.inputs[0].id).toBe(dp.ObjId + "I");
      expect(node.outputs[0].id).toBe(dp.ObjId + "O");
      expect(node.inputs[0].maxLinks).toBe(65535);
      expect(node.outputs[0].maxLinks).toBe(65535);
      expect(node.h).toBe(DP_H);
    });

    it("dataPointNode_ReadOnly_HasOnlyAnOutputPort", async () => {
      const dp = addDataPoint("Sensor", NodeDataTypeEnum.Double, 0, 0, { writeable: false });
      await create();

      expect(nodeOf(dp).inputs.length).toBe(0);
      expect(nodeOf(dp).outputs.length).toBe(1);
      expect(fixture.nativeElement.querySelectorAll(".node.dp .port.in").length).toBe(0);
      expect(fixture.nativeElement.querySelectorAll(".node.dp .port.out").length).toBe(1);
    });

    const kinds: [NodeDataTypeEnum, string, string][] = [
      [NodeDataTypeEnum.Boolean, "bool", "bool"],
      [NodeDataTypeEnum.Integer, "num", "int"],
      [NodeDataTypeEnum.Double, "num", "num"],
      [NodeDataTypeEnum.WindowState, "num", "win"],
      [NodeDataTypeEnum.String, "text", "text"],
      [NodeDataTypeEnum.Date, "time", "date"],
      [NodeDataTypeEnum.DateTime, "time", "dt"],
      [NodeDataTypeEnum.Time, "time", "time"],
      [NodeDataTypeEnum.NoAttribute, "any", "?"]
    ];
    for (const [type, kind, short] of kinds) {
      it(`dataPointNode_${NodeDataTypeEnum[type]}_IsKind_${kind}_WithBadge_${short}`, async () => {
        const dp = addDataPoint("Value", type);
        await create();

        const node = nodeOf(dp);
        expect(node.outputs[0].kind).toBe(kind as any);
        expect(node.inputs[0].kind).toBe(kind as any);
        expect(node.dataType).toBe(short);
        expect(fixture.nativeElement.querySelector(".node.dp .badge").textContent.trim()).toBe(short);
        expect(fixture.nativeElement.querySelector(".node.dp .badge").getAttribute("data-kind")).toBe(kind);
      });
    }

    it("dataPointWidth_EmptyName_UsesTheMinimumWidth", async () => {
      const dp = addDataPoint("", NodeDataTypeEnum.Boolean);
      await create();

      expect(nodeOf(dp).w).toBe(150);
    });

    it("dataPointWidth_VeryLongName_IsCappedAtTheMaximumWidth", async () => {
      const dp = addDataPoint("A very very long data point name that never fits into one block", NodeDataTypeEnum.Boolean);
      await create();

      expect(nodeOf(dp).w).toBe(340);
    });

    it("titles_RuleAndDataPoint_ShowNameTemplateAndFullName", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      const dp = addDataPoint("Lamp", NodeDataTypeEnum.Boolean);
      await create();

      expect(editor.title(nodeOf(rule))).toBe("AND");
      expect(editor.subtitle(nodeOf(rule))).toBe("AND");
      expect(editor.subtitle(nodeOf(dp))).toBe("");
      expect(editor.fullName(nodeOf(rule))).toBe("AND");
      expect(editor.fullName(nodeOf(dp))).toBe(dp.FullName);
      expect(editor.trackNode(0, nodeOf(rule))).toBe(rule.ObjId);
      expect(fixture.nativeElement.querySelector(".node.rule .title").textContent.trim()).toBe("AND");
      expect(fixture.nativeElement.querySelector(".node.dp .name").textContent.trim()).toBe("Lamp");
    });

    it("ngDoCheck_RuleInstancesPushed_Rebuilds", async () => {
      await create();
      expect(editor.nodes.length).toBe(0);

      addRule("AND", ["I1"], ["O1"]);
      fixture.detectChanges();

      expect(editor.nodes.length).toBe(1);
      expect(fixture.nativeElement.querySelectorAll(".node.rule").length).toBe(1);
    });

    it("ngDoCheck_ArraysReplaced_Rebuilds", async () => {
      addRule("AND", ["I1"], ["O1"]);
      await create();

      page.RuleInstances = [];
      fixture.detectChanges();

      expect(editor.nodes.length).toBe(0);
    });

    it("ngDoCheck_LinksPushed_DrawsTheWire", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 300, 0);
      await create();

      page.Links.push(linkRules(page, output(a), input(b)));
      fixture.detectChanges();

      expect(editor.wires.length).toBe(1);
    });

    it("ngDoCheck_NothingChanged_KeepsTheSameNodes", async () => {
      addRule("AND", ["I1"], ["O1"]);
      await create();
      const before = editor.nodes[0];

      fixture.detectChanges();

      expect(editor.nodes[0]).toBe(before);
    });

    it("rebuild_SelectedNode_IsKeptByIdOnTheNewViewModel", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      await create();
      editor.select(nodeOf(rule), false);

      addRule("OR", ["I1"], ["O1"]);
      fixture.detectChanges();

      expect(editor.selectedNode).toBe(nodeOf(rule));
      expect(editor.selectedNode.id).toBe(rule.ObjId);
    });

    it("rebuild_SelectedNodeRemoved_DropsTheSelection", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      await create();
      editor.select(nodeOf(rule), false);

      page.RuleInstances = [];
      fixture.detectChanges();

      expect(editor.selectedNode).toBeUndefined();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("wires", () => {
    it("buildWires_RuleToRule_IsKindAny", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 200, 0);
      page.Links.push(linkRules(page, output(a), input(b)));
      await create();

      expect(editor.wires.length).toBe(1);
      expect(editor.wires[0].kind).toBe("any");
      expect(fixture.nativeElement.querySelectorAll("g.wire").length).toBe(1);
      expect(fixture.nativeElement.querySelector("g.wire").getAttribute("data-kind")).toBe("any");
    });

    it("buildWires_DataPointToRule_TakesTheDataPointKind", async () => {
      const dp = addDataPoint("Temperature", NodeDataTypeEnum.Double);
      const rule = addRule("CMP", ["I1"], ["O1"], 300, 0);
      page.Links.push(linkDataPointToRule(page, dp, input(rule)));
      await create();

      expect(editor.wires[0].kind).toBe("num");
      expect(editor.wires[0].from.id).toBe(dp.ObjId + "O");
      expect(editor.wires[0].to.id).toBe(input(rule).ObjId);
    });

    it("buildWires_RuleToDataPoint_TakesTheDataPointKind", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      const dp = addDataPoint("Lamp", NodeDataTypeEnum.Boolean, 300, 0);
      page.Links.push(linkRuleToDataPoint(page, output(rule), dp));
      await create();

      expect(editor.wires[0].kind).toBe("bool");
      expect(editor.wires[0].to.id).toBe(dp.ObjId + "I");
    });

    it("buildWires_EndWithoutBlock_IsNotDrawn", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const link = linkRules(page, output(a), input(a));
      link.This2RuleInterfaceInstanceInput = "gone";
      page.Links.push(link);
      await create();

      expect(editor.wires.length).toBe(0);
      expect(fixture.nativeElement.querySelectorAll("g.wire").length).toBe(0);
    });

    it("colorRulePorts_PortLinkedToDataPoint_TakesItsKindOthersStayAny", async () => {
      const dp = addDataPoint("Lamp", NodeDataTypeEnum.Boolean);
      const rule = addRule("AND", ["I1", "I2"], ["O1"], 300, 0);
      page.Links.push(linkDataPointToRule(page, dp, input(rule, 0)));
      await create();

      const node = nodeOf(rule);
      expect(node.inputs[0].kind).toBe("bool");
      expect(node.inputs[1].kind).toBe("any");
      expect(node.outputs[0].kind).toBe("any");
      expect(portElement(input(rule, 0).ObjId).getAttribute("data-kind")).toBe("bool");
    });

    it("isLinked_LinkedAndFreePorts_AreTold", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 200, 0);
      page.Links.push(linkRules(page, output(a), input(b)));
      await create();

      expect(editor.isLinked(portOf(output(a).ObjId))).toBeTrue();
      expect(editor.isLinked(portOf(input(b).ObjId))).toBeTrue();
      expect(editor.isLinked(portOf(input(a).ObjId))).toBeFalse();
      expect(portElement(output(a).ObjId).classList).toContain("linked");
      expect(portElement(input(a).ObjId).classList).not.toContain("linked");
    });

    it("anchor_RuleRows_SitInTheMiddleOfTheirRow", async () => {
      const rule = addRule("AND", ["I1", "I2"], ["O1"], 100, 50);
      await create();
      const node = nodeOf(rule);

      expect(editor.anchor(node.inputs[0])).toEqual({ x: node.x, y: node.y + HEAD_H + BODY_PAD + ROW_H / 2 });
      expect(editor.anchor(node.inputs[1])).toEqual({ x: node.x, y: node.y + HEAD_H + BODY_PAD + ROW_H + ROW_H / 2 });
      expect(editor.anchor(node.outputs[0])).toEqual({ x: node.x + RULE_W, y: node.y + HEAD_H + BODY_PAD + ROW_H / 2 });
    });

    it("anchor_DataPoint_SitsInTheMiddleOfTheBlock", async () => {
      const dp = addDataPoint("Lamp", NodeDataTypeEnum.Boolean, 100, 100);
      await create();
      const node = nodeOf(dp);

      expect(editor.anchor(node.inputs[0])).toEqual({ x: node.x, y: node.y + DP_H / 2 });
      expect(editor.anchor(node.outputs[0])).toEqual({ x: node.x + node.w, y: node.y + DP_H / 2 });
    });

    it("wirePath_CloseBlocks_UsesTheMinimumControlDistance", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 200, 0);
      page.Links.push(linkRules(page, output(a), input(b)));
      await create();

      // A's output at x 220, B's input at 300: the control points are 40px out
      const y = HEAD_H + BODY_PAD + ROW_H / 2;
      expect(editor.wires[0].d).toBe(`M 220 ${y} C 260 ${y}, 260 ${y}, 300 ${y}`);
      expect(fixture.nativeElement.querySelector("path.wire-line").getAttribute("d")).toBe(editor.wires[0].d);
      expect(fixture.nativeElement.querySelector("path.wire-hit").getAttribute("d")).toBe(editor.wires[0].d);
    });

    it("wirePath_DistantBlocks_ControlPointsAreHalfTheDistance", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 400, 100);
      page.Links.push(linkRules(page, output(a), input(b)));
      await create();

      const y1 = HEAD_H + BODY_PAD + ROW_H / 2;
      const y2 = 100 * SY + y1;
      expect(editor.wires[0].d).toBe(curve(220, y1, 600, y2));
      expect(editor.wires[0].d).toBe(`M 220 ${y1} C 410 ${y1}, 410 ${y2}, 600 ${y2}`);
    });

    it("wirePath_BackwardsWire_StillUsesThePositiveDistance", async () => {
      const a = addRule("A", ["I1"], ["O1"], 400, 0);
      const b = addRule("B", ["I1"], ["O1"]);
      page.Links.push(linkRules(page, output(a), input(b)));
      await create();

      const y = HEAD_H + BODY_PAD + ROW_H / 2;
      // from 820 back to 0: dx = 410, the curve loops out of the output and into the input
      expect(editor.wires[0].d).toBe(`M 820 ${y} C 1230 ${y}, -410 ${y}, 0 ${y}`);
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("port compatibility", () => {
    let a: RuleInstance;
    let b: RuleInstance;

    beforeEach(() => {
      a = addRule("A", ["I1", "I2"], ["O1", { name: "O_ONE", maxLinks: 1 }]);
      b = addRule("B", ["I1", "I2"], ["O1"], 300, 0);
    });

    it("isTargetCandidate_NoPendingWire_IsFalse", async () => {
      await create();

      expect(editor.isTargetCandidate(portOf(input(b).ObjId))).toBeFalsy();
    });

    it("isTargetCandidate_FromOutput_InputsOfOtherBlocksOnly", async () => {
      await create();
      editor.pending = { from: portOf(output(a).ObjId), x: 0, y: 0, d: "" };

      expect(editor.isTargetCandidate(portOf(input(b, 0).ObjId))).toBeTrue();
      expect(editor.isTargetCandidate(portOf(input(b, 1).ObjId))).toBeTrue();
      expect(editor.isTargetCandidate(portOf(output(b).ObjId))).toBeFalse();
      expect(editor.isTargetCandidate(portOf(input(a).ObjId))).toBeFalse();
      expect(editor.isTargetCandidate(portOf(output(a).ObjId))).toBeFalse();
    });

    it("isTargetCandidate_FromInput_OutputsOfOtherBlocksOnly", async () => {
      await create();
      editor.pending = { from: portOf(input(b).ObjId), x: 0, y: 0, d: "" };

      expect(editor.isTargetCandidate(portOf(output(a).ObjId))).toBeTrue();
      expect(editor.isTargetCandidate(portOf(input(a).ObjId))).toBeFalse();
      expect(editor.isTargetCandidate(portOf(output(b).ObjId))).toBeFalse();
    });

    it("isTargetCandidate_PairAlreadyLinked_IsFalse", async () => {
      page.Links.push(linkRules(page, output(a), input(b)));
      await create();
      editor.pending = { from: portOf(output(a).ObjId), x: 0, y: 0, d: "" };

      expect(editor.isTargetCandidate(portOf(input(b).ObjId))).toBeFalse();
    });

    it("isTargetCandidate_InputAlreadyTaken_IsFalse", async () => {
      page.Links.push(linkRules(page, output(b), input(a)));
      const c = addRule("C", ["I1"], ["O1"], 600, 0);
      await create();
      editor.pending = { from: portOf(output(c).ObjId), x: 0, y: 0, d: "" };

      expect(editor.isTargetCandidate(portOf(input(a, 0).ObjId))).toBeFalse();
      expect(editor.isTargetCandidate(portOf(input(a, 1).ObjId))).toBeTrue();
    });

    it("isTargetCandidate_OutputAtItsLimit_IsFalse", async () => {
      page.Links.push(linkRules(page, output(a, 1), input(b, 0)));
      await create();
      editor.pending = { from: portOf(input(b, 1).ObjId), x: 0, y: 0, d: "" };

      expect(editor.isTargetCandidate(portOf(output(a, 1).ObjId))).toBeFalse();
      expect(editor.isTargetCandidate(portOf(output(a, 0).ObjId))).toBeTrue();
    });

    it("isTargetCandidate_DataPointInput_AcceptsSeveralWires", async () => {
      const dp = addDataPoint("Lamp", NodeDataTypeEnum.Boolean, 600, 0);
      page.Links.push(linkRuleToDataPoint(page, output(a), dp));
      await create();
      editor.pending = { from: portOf(output(b).ObjId), x: 0, y: 0, d: "" };

      expect(editor.isTargetCandidate(portOf(dp.ObjId + "I"))).toBeTrue();
    });

    it("isTargetCandidate_DataPointOutputToDataPointInput_IsAllowed", async () => {
      const from = addDataPoint("Switch", NodeDataTypeEnum.Boolean, 0, 300);
      const to = addDataPoint("Lamp", NodeDataTypeEnum.Boolean, 300, 300);
      await create();
      editor.pending = { from: portOf(from.ObjId + "O"), x: 0, y: 0, d: "" };

      expect(editor.isTargetCandidate(portOf(to.ObjId + "I"))).toBeTrue();
      expect(editor.isTargetCandidate(portOf(from.ObjId + "I"))).toBeFalse();
    });

    it("candidateClass_WhileConnecting_MarksTheCompatiblePorts", async () => {
      await create();
      portElement(output(a).ObjId).dispatchEvent(pointer("pointerdown"));
      fixture.detectChanges();

      expect(canvas().classList).toContain("connecting");
      expect(portElement(input(b).ObjId).classList).toContain("candidate");
      expect(portElement(output(b).ObjId).classList).not.toContain("candidate");
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("pending connection", () => {
    let a: RuleInstance;
    let b: RuleInstance;

    beforeEach(async () => {
      a = addRule("A", ["I1"], ["O1"]);
      b = addRule("B", ["I1"], ["O1"], 300, 0);
      await create();
      resetView();
      fixture.detectChanges();
    });

    function startAt(portId: string, init: PointerEventInit = {}) {
      portElement(portId).dispatchEvent(pointer("pointerdown", init));
      fixture.detectChanges();
    }

    async function releaseOver(target: HTMLElement) {
      spyOn(document, "elementFromPoint").and.returnValue(target);
      canvas().dispatchEvent(pointer("pointerup", { buttons: 0 }));
      await flushPromises();
      fixture.detectChanges();
    }

    it("onPortPointerDown_FreePort_StartsAWireAtItsAnchor", () => {
      startAt(output(a).ObjId);

      const from = portOf(output(a).ObjId);
      const anchor = editor.anchor(from);
      expect(editor.pending.from).toBe(from);
      expect(editor.pending.x).toBe(anchor.x);
      expect(editor.pending.y).toBe(anchor.y);
      expect(editor.pending.d).toBe(curve(anchor.x, anchor.y, anchor.x, anchor.y));
      expect(Element.prototype.setPointerCapture).toHaveBeenCalledWith(1);
      expect(fixture.nativeElement.querySelector("path.wire-pending")).not.toBeNull();
    });

    it("onPortPointerDown_RightButton_IsIgnored", () => {
      startAt(output(a).ObjId, { button: 2 });

      expect(editor.pending).toBeUndefined();
    });

    it("onPortPointerDown_InputTaken_WarnsAndStartsNothing", () => {
      page.Links.push(linkRules(page, output(a), input(b)));
      fixture.detectChanges();

      startAt(input(b).ObjId);

      expect(editor.pending).toBeUndefined();
      expect(notify.notifyWarning).toHaveBeenCalledWith("UI.LOGIC.INPUT_TAKEN");
    });

    it("onPortPointerDown_OutputFull_WarnsAndStartsNothing", () => {
      const c = addRule("C", ["I1"], [{ name: "O1", maxLinks: 1 }], 0, 200);
      page.Links.push(linkRules(page, output(c), input(b)));
      fixture.detectChanges();

      startAt(output(c).ObjId);

      expect(editor.pending).toBeUndefined();
      expect(notify.notifyWarning).toHaveBeenCalledWith("UI.LOGIC.OUTPUT_FULL");
    });

    it("onPortPointerDown_DoesNotStartANodeDrag", () => {
      startAt(output(a).ObjId);

      expect(editor.drag).toBeUndefined();
      expect(host.selected.length).toBe(0);
    });

    it("onPointerMove_OverACandidate_SnapsToItsAnchor", () => {
      startAt(output(a).ObjId);
      spyOn(document, "elementFromPoint").and.returnValue(portElement(input(b).ObjId));

      hostElement().dispatchEvent(pointer("pointermove", { clientX: 10, clientY: 10 }));

      const target = portOf(input(b).ObjId);
      const start = editor.anchor(portOf(output(a).ObjId));
      const end = editor.anchor(target);
      expect(editor.pending.target).toBe(target);
      expect(editor.pending.d).toBe(curve(start.x, start.y, end.x, end.y));
    });

    it("onPointerMove_OverNothing_FollowsThePointerInWorldCoordinates", () => {
      startAt(output(a).ObjId);
      spyOn(document, "elementFromPoint").and.returnValue(null);
      const rect = canvas().getBoundingClientRect();

      hostElement().dispatchEvent(pointer("pointermove", { clientX: rect.left + 540, clientY: rect.top + 240 }));

      const start = editor.anchor(portOf(output(a).ObjId));
      expect(editor.pending.target).toBeUndefined();
      expect(editor.pending.d).toBe(curve(start.x, start.y, 500, 200));
    });

    it("onPointerMove_OverAnIncompatiblePort_HasNoTarget", () => {
      startAt(output(a).ObjId);
      spyOn(document, "elementFromPoint").and.returnValue(portElement(output(b).ObjId));

      hostElement().dispatchEvent(pointer("pointermove", { clientX: 10, clientY: 10 }));

      expect(editor.pending.target).toBeUndefined();
    });

    it("onPointerMove_FromAnInput_DrawsTheCurveFromThePointerToTheInput", () => {
      startAt(input(b).ObjId);
      spyOn(document, "elementFromPoint").and.returnValue(null);
      const rect = canvas().getBoundingClientRect();

      hostElement().dispatchEvent(pointer("pointermove", { clientX: rect.left + 140, clientY: rect.top + 140 }));

      const start = editor.anchor(portOf(input(b).ObjId));
      expect(editor.pending.d).toBe(curve(100, 100, start.x, start.y));
    });

    it("hoverTarget_WhileOverACandidate_IsMarked", () => {
      startAt(output(a).ObjId);
      spyOn(document, "elementFromPoint").and.returnValue(portElement(input(b).ObjId));
      hostElement().dispatchEvent(pointer("pointermove", { clientX: 10, clientY: 10 }));
      fixture.detectChanges();

      expect(portElement(input(b).ObjId).classList).toContain("hover-target");
    });

    it("onPointerUp_OverACompatibleInput_CreatesAndSavesTheLink", async () => {
      startAt(output(a).ObjId);
      await releaseOver(portElement(input(b).ObjId));

      expect(editor.pending).toBeUndefined();
      expect(page.Links.length).toBe(1);
      const link = page.Links[0];
      expect(link instanceof Link).toBeTrue();
      expect(link.This2RulePage).toBe(page.ObjId);
      expect(link.This2RuleInterfaceInstanceOutput).toBe(output(a).ObjId);
      expect(link.FromRuleInstance).toBe(output(a));
      expect(link.This2RuleInterfaceInstanceInput).toBe(input(b).ObjId);
      expect(link.ToRuleInstance).toBe(input(b));
      expect(link.This2NodeInstance2RulePageInput).toBeUndefined();
      expect(link.This2NodeInstance2RulePageOutput).toBeUndefined();
      expect(link.ObjId).toMatch(/^[0-9a-f-]{36}$/);
      expect(engine.addOrUpdateLink).toHaveBeenCalledOnceWith(link);
      expect(editor.wires.length).toBe(1);
      expect(fixture.nativeElement.querySelectorAll("g.wire").length).toBe(1);
      expect(fixture.nativeElement.querySelector("path.wire-pending")).toBeNull();
    });

    it("onPointerUp_FromAnInputOntoAnOutput_LinksInTheRightDirection", async () => {
      startAt(input(b).ObjId);
      await releaseOver(portElement(output(a).ObjId));

      const link = page.Links[0];
      expect(link.This2RuleInterfaceInstanceOutput).toBe(output(a).ObjId);
      expect(link.This2RuleInterfaceInstanceInput).toBe(input(b).ObjId);
    });

    it("onPointerUp_RuleOutputOntoDataPoint_LinksTheDataPointInput", async () => {
      const dp = addDataPoint("Lamp", NodeDataTypeEnum.Boolean, 600, 0);
      fixture.detectChanges();

      startAt(output(b).ObjId);
      await releaseOver(portElement(dp.ObjId + "I"));

      const link = page.Links[0];
      expect(link.ToNodeInstance).toBe(dp);
      expect(link.This2NodeInstance2RulePageInput).toBe(dp.ObjId);
      expect(link.toPort).toBe(dp.ObjId + "I");
      expect(editor.wires[0].kind).toBe("bool");
    });

    it("onPointerUp_DataPointOutputOntoRule_LinksTheDataPointOutput", async () => {
      const dp = addDataPoint("Switch", NodeDataTypeEnum.Boolean, 0, 200);
      fixture.detectChanges();

      startAt(dp.ObjId + "O");
      await releaseOver(portElement(input(b).ObjId));

      const link = page.Links[0];
      expect(link.FromNodeInstance).toBe(dp);
      expect(link.This2NodeInstance2RulePageOutput).toBe(dp.ObjId);
      expect(link.fromPort).toBe(dp.ObjId + "O");
      expect(link.ToRuleInstance).toBe(input(b));
    });

    it("onPointerUp_OverNothing_CancelsTheWire", async () => {
      startAt(output(a).ObjId);
      await releaseOver(null);

      expect(editor.pending).toBeUndefined();
      expect(page.Links.length).toBe(0);
      expect(engine.addOrUpdateLink).not.toHaveBeenCalled();
    });

    it("onPointerUp_OverTheStartPort_CreatesNothing", async () => {
      startAt(output(a).ObjId);
      await releaseOver(portElement(output(a).ObjId));

      expect(page.Links.length).toBe(0);
    });

    it("onPointerUp_OverATakenInput_WarnsAndCreatesNothing", async () => {
      const c = addRule("C", ["I1"], ["O1"], 0, 200);
      page.Links.push(linkRules(page, output(c), input(b)));
      fixture.detectChanges();

      startAt(output(a).ObjId);
      await releaseOver(portElement(input(b).ObjId));

      expect(page.Links.length).toBe(1);
      expect(notify.notifyWarning).toHaveBeenCalledWith("UI.LOGIC.INPUT_TAKEN");
      expect(engine.addOrUpdateLink).not.toHaveBeenCalled();
    });

    it("onPointerUp_ServerRejectsTheLink_RemovesItAgainAndReports", async () => {
      engine.addOrUpdateLink.and.callFake(() => rejected(new Error("save failed")));

      startAt(output(a).ObjId);
      await releaseOver(portElement(input(b).ObjId));

      expect(page.Links.length).toBe(0);
      expect(editor.wires.length).toBe(0);
      expect(notify.notifyError).toHaveBeenCalled();
    });

    it("onPointerUp_ServerAnswersWithWebApiException_ShowsTheTranslatedError", async () => {
      const ex = new WebApiException();
      ex.ErrorText = "LINK_INVALID";
      ex.Severity = ExceptionSeverity.Error;
      engine.addOrUpdateLink.and.callFake(() => rejected(ex));

      startAt(output(a).ObjId);
      await releaseOver(portElement(input(b).ObjId));

      expect(translate.translate).toHaveBeenCalledWith("ERROR.LINK_INVALID");
      expect(notify.notifyError).toHaveBeenCalledWith("ERROR.LINK_INVALID");
    });

    it("onPointerCancel_DropsTheWireAndTheDrag", () => {
      startAt(output(a).ObjId);
      editor.drag = <any>{ mode: "pan" };

      hostElement().dispatchEvent(new PointerEvent("pointercancel", { bubbles: true }));

      expect(editor.pending).toBeUndefined();
      expect(editor.drag).toBeUndefined();
    });

    it("onKeyDown_Escape_DropsTheWireAndClearsTheSelection", async () => {
      editor.select(nodeOf(a));
      startAt(output(a).ObjId);

      canvas().dispatchEvent(key("Escape"));
      await flushPromises();

      expect(editor.pending).toBeUndefined();
      expect(editor.selectedNode).toBeUndefined();
      expect(host.selected[host.selected.length - 1].items).toEqual([]);
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("selection and delete", () => {
    let a: RuleInstance;
    let b: RuleInstance;
    let link: Link;

    beforeEach(async () => {
      a = addRule("A", ["I1"], ["O1"]);
      b = addRule("B", ["I1"], ["O1"], 300, 0);
      link = linkRules(page, output(a), input(b));
      page.Links.push(link);
      await create();
    });

    it("selectLink_PointerDownOnTheWire_SelectsTheLinkAndEmitsIt", () => {
      editor.select(nodeOf(a), false);

      fixture.nativeElement.querySelector("path.wire-hit").dispatchEvent(pointer("pointerdown"));
      fixture.detectChanges();

      expect(editor.selectedLink).toBe(link);
      expect(editor.selectedNode).toBeUndefined();
      expect(host.selected).toEqual([{ page, items: [link] }]);
      expect(fixture.nativeElement.querySelector("g.wire").classList).toContain("selected");
      // the wire handled the pointer, the canvas did not start a pan
      expect(editor.drag).toBeUndefined();
    });

    it("select_Node_EmitsTheModel", () => {
      editor.select(nodeOf(a));

      expect(host.selected).toEqual([{ page, items: [a] }]);
      expect(editor.selectedLink).toBeUndefined();
    });

    it("select_WithoutEmit_EmitsNothing", () => {
      editor.select(nodeOf(a), false);

      expect(editor.selectedNode).toBe(nodeOf(a));
      expect(host.selected.length).toBe(0);
    });

    it("selectedClass_SelectedNode_IsMarked", () => {
      editor.select(nodeOf(a), false);
      fixture.detectChanges();

      const nodes = fixture.nativeElement.querySelectorAll(".node.rule");
      expect(nodes[0].classList).toContain("selected");
      expect(nodes[1].classList).not.toContain("selected");
    });

    it("onKeyDown_DeleteWithASelectedLink_RemovesItLocallyAndOnTheServer", async () => {
      editor.selectLink(pointer("pointerdown"), link);

      canvas().dispatchEvent(key("Delete"));
      await flushPromises();
      fixture.detectChanges();

      expect(engine.removeLink).toHaveBeenCalledOnceWith(link);
      expect(page.Links.length).toBe(0);
      expect(editor.wires.length).toBe(0);
      expect(editor.selectedLink).toBeUndefined();
      expect(host.selected[host.selected.length - 1]).toEqual({ page, items: [] });
      expect(fixture.nativeElement.querySelectorAll("g.wire").length).toBe(0);
    });

    it("onKeyDown_Backspace_AlsoDeletes", async () => {
      editor.selectLink(pointer("pointerdown"), link);
      const event = key("Backspace");

      canvas().dispatchEvent(event);
      await flushPromises();

      expect(event.defaultPrevented).toBeTrue();
      expect(engine.removeLink).toHaveBeenCalledOnceWith(link);
    });

    it("deleteSelection_SelectedNode_RemovesItsLinksFirstThenAsksThePageToRemoveTheBlock", async () => {
      const c = addRule("C", ["I1"], ["O1"], 600, 0);
      const second = linkRules(page, output(b), input(c));
      page.Links.push(second);
      fixture.detectChanges();
      editor.select(nodeOf(b), false);

      await editor.deleteSelection();

      expect(engine.removeLink).toHaveBeenCalledTimes(2);
      expect(engine.removeLink).toHaveBeenCalledWith(link);
      expect(engine.removeLink).toHaveBeenCalledWith(second);
      expect(page.Links.length).toBe(0);
      expect(host.removed).toEqual([{ item: b, page, removed: false }]);
      expect(editor.selectedNode).toBeUndefined();
    });

    it("deleteSelection_NodeWithoutLinks_OnlyEmitsTheRemove", async () => {
      const c = addRule("C", ["I1"], ["O1"], 600, 0);
      fixture.detectChanges();
      editor.select(nodeOf(c), false);

      await editor.deleteSelection();

      expect(engine.removeLink).not.toHaveBeenCalled();
      expect(host.removed.length).toBe(1);
      expect(page.Links.length).toBe(1);
    });

    it("deleteSelection_NothingSelected_DoesNothing", async () => {
      await editor.deleteSelection();

      expect(engine.removeLink).not.toHaveBeenCalled();
      expect(host.removed.length).toBe(0);
      expect(page.Links.length).toBe(1);
    });

    it("deleteSelection_ServerFails_ReportsTheError", async () => {
      engine.removeLink.and.callFake(() => rejected(new Error("gone")));
      editor.selectLink(pointer("pointerdown"), link);

      await editor.deleteSelection();

      expect(notify.notifyError).toHaveBeenCalled();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("invert", () => {
    it("toggleInvert_RulePortLabelClicked_FlipsAndSavesTheRule", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      await create();

      fixture.nativeElement.querySelector(".row.in .label").click();
      await flushPromises();
      fixture.detectChanges();

      expect(input(rule).Inverted).toBeTrue();
      expect(engine.updateItem).toHaveBeenCalledOnceWith(rule, LogicUpdateScope.InvertedValueUpdated);
      expect(fixture.nativeElement.querySelector(".row.in .label").classList).toContain("inverted");
      expect(portElement(input(rule).ObjId).classList).toContain("inverted");
    });

    it("toggleInvert_Twice_RestoresTheValue", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      await create();
      const port = portOf(output(rule).ObjId);

      await editor.toggleInvert(new MouseEvent("click"), port);
      await editor.toggleInvert(new MouseEvent("click"), port);

      expect(output(rule).Inverted).toBeFalse();
      expect(engine.updateItem).toHaveBeenCalledTimes(2);
    });

    it("toggleInvert_BooleanDataPoint_FlipsAndSavesTheDataPoint", async () => {
      const dp = addDataPoint("Lamp", NodeDataTypeEnum.Boolean);
      await create();
      const port = portOf(dp.ObjId + "O");

      expect(editor.canInvert(port)).toBeTrue();
      await editor.toggleInvert(new MouseEvent("click"), port);
      fixture.detectChanges();

      expect(dp.Inverted).toBeTrue();
      expect(editor.inverted(port)).toBeTrue();
      expect(engine.updateItem).toHaveBeenCalledOnceWith(dp, LogicUpdateScope.InvertedValueUpdated);
      expect(fixture.nativeElement.querySelector(".node.dp .name").classList).toContain("inverted");
    });

    it("toggleInvert_NumericDataPoint_CannotBeInverted", async () => {
      const dp = addDataPoint("Temperature", NodeDataTypeEnum.Double);
      await create();
      const port = portOf(dp.ObjId + "O");

      expect(editor.canInvert(port)).toBeFalse();
      await editor.toggleInvert(new MouseEvent("click"), port);

      expect(dp.Inverted).toBeFalsy();
      expect(engine.updateItem).not.toHaveBeenCalled();
    });

    it("canInvert_RulePort_IsAlwaysAllowed", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      await create();

      expect(editor.canInvert(portOf(input(rule).ObjId))).toBeTrue();
      expect(editor.canInvert(portOf(output(rule).ObjId))).toBeTrue();
    });

    it("toggleInvert_LabelClick_DoesNotSelectOrDragTheBlock", async () => {
      addRule("AND", ["I1"], ["O1"]);
      await create();
      const label = fixture.nativeElement.querySelector(".row.in .label");

      label.dispatchEvent(pointer("pointerdown"));
      label.click();
      await flushPromises();

      expect(editor.drag).toBeUndefined();
      expect(host.selected.length).toBe(0);
    });

    it("toggleInvert_SaveFails_ReportsTheError", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      engine.updateItem.and.callFake(() => rejected(new Error("save failed")));
      await create();

      await editor.toggleInvert(new MouseEvent("click"), portOf(input(rule).ObjId));

      expect(notify.notifyError).toHaveBeenCalled();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("zoom, fit and minimap", () => {
    it("templateHelpers_KnownView_FormatTransformGridAndPercent", async () => {
      await create();
      resetView(1.5, 12, 34);

      expect(editor.worldTransform).toBe("translate(12px, 34px) scale(1.5)");
      expect(editor.gridSize).toBe("30px 30px");
      expect(editor.gridPosition).toBe("12px 34px");
      expect(editor.zoomPercent).toBe(150);
      editor.zoom = 0.333;
      expect(editor.zoomPercent).toBe(33);
    });

    it("zoomBy_ZoomIn_KeepsTheWorldPointUnderTheViewCenter", async () => {
      await create();
      resetView();

      editor.zoomBy(1.25);

      expect(editor.zoom).toBe(1.25);
      expect(editor.panX).toBe(-50);
      expect(editor.panY).toBe(-25);
      expect((400 - editor.panX) / editor.zoom).toBeCloseTo(360, 10);
      expect((300 - editor.panY) / editor.zoom).toBeCloseTo(260, 10);
    });

    it("zoomBy_ZoomOutThenIn_ReturnsToTheSameView", async () => {
      await create();
      resetView();

      editor.zoomBy(1 / 1.25);
      editor.zoomBy(1.25);

      expect(editor.zoom).toBeCloseTo(1, 10);
      expect(editor.panX).toBeCloseTo(40, 10);
      expect(editor.panY).toBeCloseTo(40, 10);
    });

    it("zoomBy_FarIn_ClampsAt250Percent", async () => {
      await create();
      resetView();

      editor.zoomBy(100);

      expect(editor.zoom).toBe(2.5);
    });

    it("zoomBy_FarOut_ClampsAt20Percent", async () => {
      await create();
      resetView();

      editor.zoomBy(0.0001);

      expect(editor.zoom).toBe(0.2);
    });

    it("fitToView_EmptyPage_ResetsTheView", async () => {
      await create();
      resetView(2, -300, -300);

      editor.fitToView();

      expect(editor.zoom).toBe(1);
      expect(editor.panX).toBe(40);
      expect(editor.panY).toBe(40);
    });

    it("fitToView_NoViewportYet_ResetsTheView", async () => {
      addRule("AND", ["I1"], ["O1"]);
      await create();
      resetView(2, -300, -300);
      (editor as any).viewportW = 0;

      editor.fitToView();

      expect(editor.zoom).toBe(1);
      expect(editor.panX).toBe(40);
    });

    it("fitToView_SmallContent_CentersAndCapsTheZoom", async () => {
      addRule("AND", ["I1"], ["O1"]);
      await create();
      resetView();

      editor.fitToView();

      // 220x76 block in 800x600 with 48px margins: the zoom would be 3.2, the default cap is 1.25
      expect(editor.zoom).toBe(1.25);
      expect(editor.panX).toBe((800 - 220 * 1.25) / 2);
      expect(editor.panY).toBe((600 - 76 * 1.25) / 2);
    });

    it("fitToView_MaxZoomOne_UsesOneHundredPercent", async () => {
      addRule("AND", ["I1"], ["O1"], 100, 100);
      await create();
      resetView();

      editor.fitToView(1);

      expect(editor.zoom).toBe(1);
      expect(editor.panX).toBe((800 - 220) / 2 - 100 * SX);
      expect(editor.panY).toBe((600 - 76) / 2 - 100 * SY);
    });

    it("fitToView_WideContent_FitsTheWidthWithMargins", async () => {
      addRule("A", ["I1"], ["O1"]);
      addRule("B", ["I1"], ["O1"], 1000, 0);
      await create();
      resetView();

      editor.fitToView();

      const width = 1000 * SX + 220;
      expect(editor.zoom).toBeCloseTo((800 - 96) / width, 10);
    });

    it("fitToView_HugeContent_ClampsAtTheMinimumZoom", async () => {
      addRule("A", ["I1"], ["O1"]);
      addRule("B", ["I1"], ["O1"], 20000, 0);
      await create();
      resetView();

      editor.fitToView();

      expect(editor.zoom).toBe(0.2);
    });

    it("contentBox_SeveralBlocks_IsTheirUnion", async () => {
      addRule("A", ["I1"], ["O1"], 100, 100);
      addRule("B", ["I1", "I2"], ["O1"], 300, 200);
      await create();

      expect(editor.contentBox()).toEqual({ x: 150, y: 120, w: 450 + 220 - 150, h: 240 + 98 - 120 });
    });

    it("contentBox_EmptyPage_IsUndefined", async () => {
      await create();

      expect(editor.contentBox()).toBeUndefined();
      expect(editor.minimap).toBeUndefined();
    });

    it("minimap_OneBlock_ViewBoxCoversContentAndViewWithPadding", async () => {
      addRule("AND", ["I1"], ["O1"]);
      await create();
      resetView();

      const map = editor.minimap;

      expect(map.view).toEqual({ x: -40, y: -40, w: 800, h: 600 });
      expect(map.viewBox).toBe("-80 -80 880 680");
      expect(map.x1).toBe(-80);
      expect(map.y1).toBe(-80);
      expect(map.w).toBe(880);
      expect(map.h).toBe(680);
    });

    it("minimap_ContentOutsideTheView_GrowsToIncludeIt", async () => {
      addRule("AND", ["I1"], ["O1"], 2000, 2000);
      await create();
      resetView();

      const map = editor.minimap;

      expect(map.x1).toBe(-80);
      expect(map.w).toBe(3000 + 220 + 40 + 80);
      expect(map.h).toBe(2400 + 76 + 40 + 80);
    });

    it("minimap_Zoomed_ViewRectShrinks", async () => {
      addRule("AND", ["I1"], ["O1"]);
      await create();
      resetView(2, 0, 0);

      const view = editor.minimap.view;
      // -pan / zoom gives -0 for a pan of 0
      expect([view.x + 0, view.y + 0, view.w, view.h]).toEqual([0, 0, 400, 300]);
    });

    it("onMinimapPointer_PointerDown_CentersTheViewOnThatWorldPoint", async () => {
      addRule("AND", ["I1"], ["O1"]);
      await create();
      resetView();
      const svg: any = {
        setPointerCapture: jasmine.createSpy("setPointerCapture"),
        getBoundingClientRect: () => ({ left: 0, top: 0, width: 176, height: 136 })
      };

      // 880x680 drawn at scale 0.2: (176, 0) is world (800, -80)
      editor.onMinimapPointer(pointer("pointerdown", { clientX: 176, clientY: 0 }), svg);

      expect(svg.setPointerCapture).toHaveBeenCalledWith(1);
      expect(editor.panX).toBeCloseTo(400 - 800, 10);
      expect(editor.panY).toBeCloseTo(300 + 80, 10);
    });

    it("onMinimapPointer_LetterboxedDrawing_SubtractsTheOffset", async () => {
      addRule("AND", ["I1"], ["O1"]);
      await create();
      resetView();
      // twice as wide as needed: scale 0.2 by height, 88px empty on each side
      const svg: any = { setPointerCapture: () => void 0, getBoundingClientRect: () => ({ left: 10, top: 20, width: 352, height: 136 }) };

      editor.onMinimapPointer(pointer("pointerdown", { clientX: 10 + 88, clientY: 20 }), svg);

      // left edge of the drawing = world x -80
      expect(editor.panX).toBeCloseTo(400 + 80, 10);
      expect(editor.panY).toBeCloseTo(300 + 80, 10);
    });

    it("onMinimapPointer_MoveWithoutButton_IsIgnored", async () => {
      addRule("AND", ["I1"], ["O1"]);
      await create();
      resetView();
      const svg: any = { setPointerCapture: () => void 0, getBoundingClientRect: () => ({ left: 0, top: 0, width: 176, height: 136 }) };

      editor.onMinimapPointer(pointer("pointermove", { clientX: 176, clientY: 0, buttons: 0 }), svg);

      expect(editor.panX).toBe(40);
      expect(editor.panY).toBe(40);
    });

    it("onMinimapPointer_DragWithButton_MovesTheView", async () => {
      addRule("AND", ["I1"], ["O1"]);
      await create();
      resetView();
      const svg: any = { setPointerCapture: () => void 0, getBoundingClientRect: () => ({ left: 0, top: 0, width: 176, height: 136 }) };

      editor.onMinimapPointer(pointer("pointermove", { clientX: 88, clientY: 68, buttons: 1 }), svg);

      expect(editor.panX).toBeCloseTo(400 - 360, 10);
      expect(editor.panY).toBeCloseTo(300 - 260, 10);
    });

    it("onMinimapPointer_EmptyPage_DoesNothing", async () => {
      await create();
      resetView();
      const svg: any = { setPointerCapture: () => void 0, getBoundingClientRect: () => ({ left: 0, top: 0, width: 176, height: 136 }) };

      editor.onMinimapPointer(pointer("pointerdown", { clientX: 88, clientY: 68 }), svg);

      expect(editor.panX).toBe(40);
    });

    it("pageEmitters_ZoomInOutAndFit_DriveTheView", async () => {
      addRule("AND", ["I1"], ["O1"]);
      await create();
      resetView();

      page.onZoomIn.emit();
      expect(editor.zoom).toBe(1.25);
      page.onZoomOut.emit();
      expect(editor.zoom).toBeCloseTo(1, 10);
      page.onZoomToView.emit();
      expect(editor.zoom).toBe(1.25);
      expect(editor.panX).toBe((800 - 220 * 1.25) / 2);
    });

    it("onKeyDown_PlusEqualsMinusZero_Zoom", async () => {
      addRule("AND", ["I1"], ["O1"]);
      await create();
      resetView();

      await editor.onKeyDown(key("+"));
      expect(editor.zoom).toBe(1.25);
      await editor.onKeyDown(key("-"));
      expect(editor.zoom).toBeCloseTo(1, 10);
      await editor.onKeyDown(key("="));
      expect(editor.zoom).toBeCloseTo(1.25, 10);
      resetView(2, 0, 0);
      await editor.onKeyDown(key("0"));
      expect(editor.zoom).toBe(1.25);
    });

    it("hudButtons_Click_ZoomAndFit", async () => {
      addRule("AND", ["I1"], ["O1"]);
      await create();
      resetView();
      fixture.detectChanges();
      const buttons = fixture.nativeElement.querySelectorAll(".hud button");

      buttons[2].click();
      expect(editor.zoom).toBe(1.25);
      buttons[0].click();
      expect(editor.zoom).toBeCloseTo(1, 10);
      fixture.detectChanges();
      expect(buttons[1].textContent.trim()).toBe("100%");
      resetView(2, 0, 0);
      buttons[3].click();
      expect(editor.zoom).toBe(1.25);
      resetView(2, 0, 0);
      buttons[1].click();
      expect(editor.zoom).toBe(1.25);
    });

    it("hud_PointerDown_DoesNotPanTheCanvas", async () => {
      await create();

      fixture.nativeElement.querySelector(".hud").dispatchEvent(pointer("pointerdown"));

      expect(editor.drag).toBeUndefined();
    });

    it("onWheel_ScrollUp_ZoomsInAroundThePointer", async () => {
      await create();
      resetView();
      const rect = canvas().getBoundingClientRect();
      const event = new WheelEvent("wheel", { deltaY: -100, clientX: rect.left + 100, clientY: rect.top + 50, cancelable: true, bubbles: true });

      canvas().dispatchEvent(event);

      expect(event.defaultPrevented).toBeTrue();
      expect(editor.zoom).toBeCloseTo(Math.exp(0.15), 10);
      expect((100 - editor.panX) / editor.zoom).toBeCloseTo(60, 8);
      expect((50 - editor.panY) / editor.zoom).toBeCloseTo(10, 8);
    });

    it("onWheel_PinchWithCtrl_ZoomsFaster", async () => {
      await create();
      resetView();
      const rect = canvas().getBoundingClientRect();

      canvas().dispatchEvent(new WheelEvent("wheel", { deltaY: 50, ctrlKey: true, clientX: rect.left, clientY: rect.top, cancelable: true }));

      expect(editor.zoom).toBeCloseTo(Math.exp(-0.5), 10);
    });

    it("ngOnDestroy_Wheel_IsNoLongerHandled", async () => {
      await create();
      resetView();
      const element = canvas();

      fixture.destroy();
      element.dispatchEvent(new WheelEvent("wheel", { deltaY: -100, cancelable: true }));

      expect(editor.zoom).toBe(1);
    });

    it("firstFit_CanvasHasASizeAndBlocks_FitsOnceAtMostOneHundredPercent", async () => {
      addRule("AND", ["I1"], ["O1"]);
      await create();

      expect((editor as any).fitted).toBeTrue();
      expect((editor as any).viewportW).toBe(800);
      expect((editor as any).viewportH).toBe(600);
      expect(editor.zoom).toBe(1);
      expect(editor.panX).toBe((800 - 220) / 2);
      expect(editor.panY).toBe((600 - 76) / 2);
    });

    it("firstFit_LaterRebuilds_DoNotFitAgain", async () => {
      addRule("AND", ["I1"], ["O1"]);
      await create();
      resetView(2, 0, 0);

      addRule("OR", ["I1"], ["O1"], 500, 500);
      fixture.detectChanges();

      expect(editor.zoom).toBe(2);
      expect(editor.panX).toBe(0);
    });

    it("firstFit_EmptyPage_WaitsForTheFirstBlock", async () => {
      await create();
      expect((editor as any).fitted).toBeFalse();
      expect(editor.zoom).toBe(1);

      addRule("AND", ["I1"], ["O1"]);
      fixture.detectChanges();

      expect((editor as any).fitted).toBeTrue();
      expect(editor.panX).toBe((800 - 220) / 2);
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("drag and pan", () => {
    let rule: RuleInstance;

    beforeEach(async () => {
      rule = addRule("AND", ["I1"], ["O1"]);
      await create();
      resetView();
      fixture.detectChanges();
    });

    function nodeElement(): HTMLElement {
      return fixture.nativeElement.querySelector(".node.rule");
    }

    it("pan_DragOnTheCanvas_MovesTheView", () => {
      canvas().dispatchEvent(pointer("pointerdown", { clientX: 100, clientY: 100 }));
      expect(editor.drag.mode).toBe("pan");

      hostElement().dispatchEvent(pointer("pointermove", { clientX: 130, clientY: 120 }));
      fixture.detectChanges();

      expect(editor.panX).toBe(70);
      expect(editor.panY).toBe(60);
      expect(editor.drag.moved).toBeTrue();
      expect(canvas().classList).toContain("panning");
      expect(fixture.nativeElement.querySelector(".world").style.transform).toBe("translate(70px, 60px) scale(1)");
    });

    it("pan_LessThanThreePixels_DoesNotMove", () => {
      canvas().dispatchEvent(pointer("pointerdown", { clientX: 100, clientY: 100 }));
      hostElement().dispatchEvent(pointer("pointermove", { clientX: 101, clientY: 101 }));

      expect(editor.panX).toBe(40);
      expect(editor.drag.moved).toBeFalse();
    });

    it("pan_MiddleButton_AlsoPans", () => {
      canvas().dispatchEvent(pointer("pointerdown", { clientX: 100, clientY: 100, button: 1 }));

      expect(editor.drag.mode).toBe("pan");
    });

    it("pan_RightButton_IsIgnored", () => {
      canvas().dispatchEvent(pointer("pointerdown", { clientX: 100, clientY: 100, button: 2 }));

      expect(editor.drag).toBeUndefined();
    });

    it("pointerMove_WithoutDragOrWire_DoesNothing", () => {
      hostElement().dispatchEvent(pointer("pointermove", { clientX: 300, clientY: 300 }));

      expect(editor.panX).toBe(40);
      expect(nodeOf(rule).x).toBe(0);
    });

    it("pan_AfterAMove_KeepsTheSelection", async () => {
      editor.select(nodeOf(rule), false);
      canvas().dispatchEvent(pointer("pointerdown", { clientX: 100, clientY: 100 }));
      hostElement().dispatchEvent(pointer("pointermove", { clientX: 150, clientY: 100 }));

      await editor.onPointerUp(pointer("pointerup", { clientX: 150, clientY: 100 }));

      expect(editor.drag).toBeUndefined();
      expect(editor.selectedNode).toBe(nodeOf(rule));
    });

    it("click_OnTheEmptyCanvas_ClearsTheSelection", async () => {
      editor.select(nodeOf(rule), false);
      canvas().dispatchEvent(pointer("pointerdown", { clientX: 100, clientY: 100 }));

      await editor.onPointerUp(pointer("pointerup", { clientX: 100, clientY: 100 }));

      expect(editor.selectedNode).toBeUndefined();
      expect(host.selected).toEqual([{ page, items: [] }]);
    });

    it("click_OnTheEmptyCanvasWithoutSelection_EmitsNothing", async () => {
      canvas().dispatchEvent(pointer("pointerdown", { clientX: 100, clientY: 100 }));

      await editor.onPointerUp(pointer("pointerup", { clientX: 100, clientY: 100 }));

      expect(host.selected.length).toBe(0);
    });

    it("pointerUp_WithoutDrag_DoesNothing", async () => {
      await editor.onPointerUp(pointer("pointerup", { clientX: 100, clientY: 100 }));

      expect(host.selected.length).toBe(0);
      expect(engine.updateItem).not.toHaveBeenCalled();
    });

    it("nodeDrag_PointerDown_SelectsTheBlockOnce", () => {
      nodeElement().dispatchEvent(pointer("pointerdown", { clientX: 10, clientY: 10 }));
      nodeElement().dispatchEvent(pointer("pointerdown", { clientX: 10, clientY: 10 }));

      expect(editor.selectedNode).toBe(nodeOf(rule));
      expect(host.selected).toEqual([{ page, items: [rule] }]);
      expect(editor.drag.mode).toBe("node");
      expect(editor.drag.node).toBe(nodeOf(rule));
    });

    it("nodeDrag_RightButton_IsIgnored", () => {
      nodeElement().dispatchEvent(pointer("pointerdown", { clientX: 10, clientY: 10, button: 2 }));

      expect(editor.drag).toBeUndefined();
      expect(editor.selectedNode).toBeUndefined();
    });

    it("nodeDrag_Move_SnapsToTheTenPixelGrid", () => {
      nodeElement().dispatchEvent(pointer("pointerdown", { clientX: 10, clientY: 10 }));

      hostElement().dispatchEvent(pointer("pointermove", { clientX: 47, clientY: 34 }));
      fixture.detectChanges();

      expect(nodeOf(rule).x).toBe(40);
      expect(nodeOf(rule).y).toBe(20);
      expect(nodeElement().style.left).toBe("40px");
      expect(nodeElement().style.top).toBe("20px");
      // the view itself does not move
      expect(editor.panX).toBe(40);
    });

    it("nodeDrag_Zoomed_MovesByScreenDistanceDividedByZoom", () => {
      resetView(2, 40, 40);
      nodeElement().dispatchEvent(pointer("pointerdown", { clientX: 10, clientY: 10 }));

      hostElement().dispatchEvent(pointer("pointermove", { clientX: 47, clientY: 10 }));

      expect(nodeOf(rule).x).toBe(20);
    });

    it("nodeDrag_PastTheOrigin_StopsAtZero", () => {
      nodeElement().dispatchEvent(pointer("pointerdown", { clientX: 100, clientY: 100 }));

      hostElement().dispatchEvent(pointer("pointermove", { clientX: 0, clientY: 0 }));

      expect(nodeOf(rule).x).toBe(0);
      expect(nodeOf(rule).y).toBe(0);
    });

    it("nodeDrag_Release_SavesThePositionInStoredUnits", async () => {
      nodeElement().dispatchEvent(pointer("pointerdown", { clientX: 10, clientY: 10 }));
      hostElement().dispatchEvent(pointer("pointermove", { clientX: 160, clientY: 130 }));

      await editor.onPointerUp(pointer("pointerup", { clientX: 160, clientY: 130 }));

      expect(nodeOf(rule).x).toBe(150);
      expect(nodeOf(rule).y).toBe(120);
      expect(rule.X).toBe(100);
      expect(rule.Y).toBe(100);
      expect(engine.updateItem).toHaveBeenCalledOnceWith(rule, LogicUpdateScope.Drag);
    });

    it("nodeDrag_ClickWithoutMove_DoesNotSave", async () => {
      nodeElement().dispatchEvent(pointer("pointerdown", { clientX: 10, clientY: 10 }));

      await editor.onPointerUp(pointer("pointerup", { clientX: 10, clientY: 10 }));

      expect(engine.updateItem).not.toHaveBeenCalled();
    });

    it("nodeDrag_SaveFails_ReportsTheError", async () => {
      engine.updateItem.and.callFake(() => rejected(new Error("save failed")));
      nodeElement().dispatchEvent(pointer("pointerdown", { clientX: 10, clientY: 10 }));
      hostElement().dispatchEvent(pointer("pointermove", { clientX: 60, clientY: 10 }));

      await editor.onPointerUp(pointer("pointerup", { clientX: 60, clientY: 10 }));

      expect(notify.notifyError).toHaveBeenCalled();
    });

    it("nodeDrag_Move_UpdatesOnlyTheWiresOfThatBlock", async () => {
      const b = addRule("B", ["I1"], ["O1"], 300, 0);
      const c = addRule("C", ["I1"], ["O1"], 300, 200);
      const d = addRule("D", ["I1"], ["O1"], 600, 200);
      page.Links.push(linkRules(page, output(rule), input(b)));
      page.Links.push(linkRules(page, output(c), input(d)));
      fixture.detectChanges();
      const untouched = editor.wires[1].d;

      nodeElement().dispatchEvent(pointer("pointerdown", { clientX: 10, clientY: 10 }));
      hostElement().dispatchEvent(pointer("pointermove", { clientX: 10, clientY: 110 }));

      const from = editor.anchor(editor.wires[0].from);
      const to = editor.anchor(editor.wires[0].to);
      expect(editor.wires[0].d).toBe(curve(from.x, from.y, to.x, to.y));
      expect(from.y).toBe(100 + HEAD_H + BODY_PAD + ROW_H / 2);
      expect(editor.wires[1].d).toBe(untouched);
    });

    it("nudge_ArrowKeys_MoveTheSelectedBlockAndSaveAfterAPause", fakeAsync(() => {
      editor.select(nodeOf(rule), false);

      editor.onKeyDown(key("ArrowRight"));
      editor.onKeyDown(key("ArrowDown", { shiftKey: true }));

      expect(nodeOf(rule).x).toBe(10);
      expect(nodeOf(rule).y).toBe(1);
      tick(399);
      expect(engine.updateItem).not.toHaveBeenCalled();
      tick(1);
      expect(engine.updateItem).toHaveBeenCalledOnceWith(rule, LogicUpdateScope.Drag);
      expect(rule.X).toBe(Math.round(10 / SX));
      flush();
    }));

    it("nudge_SeveralKeysInARow_SaveOnlyOnce", fakeAsync(() => {
      editor.select(nodeOf(rule), false);

      for (let i = 0; i < 5; i++) {
        editor.onKeyDown(key("ArrowRight"));
        tick(100);
      }
      tick(400);

      expect(nodeOf(rule).x).toBe(50);
      expect(engine.updateItem).toHaveBeenCalledTimes(1);
      flush();
    }));

    it("nudge_LeftAndUpAtTheOrigin_StayAtZero", fakeAsync(() => {
      editor.select(nodeOf(rule), false);
      const event = key("ArrowLeft");

      editor.onKeyDown(event);
      editor.onKeyDown(key("ArrowUp"));

      expect(event.defaultPrevented).toBeTrue();
      expect(nodeOf(rule).x).toBe(0);
      expect(nodeOf(rule).y).toBe(0);
      flush();
    }));

    it("nudge_NothingSelected_DoesNothing", fakeAsync(() => {
      const event = key("ArrowRight");

      editor.onKeyDown(event);
      tick(500);

      expect(event.defaultPrevented).toBeFalse();
      expect(nodeOf(rule).x).toBe(0);
      expect(engine.updateItem).not.toHaveBeenCalled();
    }));

    it("ngOnDestroy_PendingNudge_IsNotSaved", fakeAsync(() => {
      editor.select(nodeOf(rule), false);
      editor.onKeyDown(key("ArrowRight"));

      fixture.destroy();
      tick(500);

      expect(engine.updateItem).not.toHaveBeenCalled();
    }));
  });

  // ---------------------------------------------------------------------------------------------
  describe("values", () => {
    it("value_RulePort_FormatsByType", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      await create();
      const port = portOf(input(rule).ObjId);
      const cases: [any, string][] = [
        [undefined, ""], [null, ""], ["", ""],
        [true, "1"], [false, "0"],
        [5, "5"], [-3, "-3"], [0, "0"], [1.234, "1.23"], [0.5, "0.50"],
        [{ a: 1 }, "{…}"],
        ["short text", "short text"],
        ["fourteen chars", "fourteen chars"],
        ["fifteen chars!!", "fifteen chars…"],
        ["75937.51123456", "75937.51"],
        ["1.5", "1.5"]
      ];

      for (const [raw, shown] of cases) {
        input(rule).PortValue = raw;
        expect(editor.value(port)).withContext(String(raw)).toBe(shown);
      }
    });

    it("portLabel_RuleAndDataPoint_OnlyRulePortsHaveALabel", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      const dp = addDataPoint("Lamp", NodeDataTypeEnum.Boolean);
      await create();

      expect(editor.portLabel(portOf(input(rule).ObjId))).toBe("I1");
      expect(editor.portLabel(portOf(dp.ObjId + "O"))).toBe("");
    });

    it("value_DataPointWithPortValue_ShowsIt", async () => {
      const dp = addDataPoint("Temperature", NodeDataTypeEnum.Double);
      await create();
      dp.Outputs[0].PortValue = { value: 21.456 };
      dp.Inputs[0].PortValue = { value: 3 };

      expect(editor.value(portOf(dp.ObjId + "O"))).toBe("21.46");
      expect(editor.value(portOf(dp.ObjId + "I"))).toBe("3");
    });

    it("value_DataPointWithoutPortValue_FallsBackToReadWriteThenValue", async () => {
      const ni = makeNodeInstance("Temperature", NodeDataTypeEnum.Integer);
      const dp = makeDataPoint(page, ni);
      page.NodeInstances.push(dp);
      await create();

      ni.Value = 7;
      expect(editor.value(portOf(dp.ObjId + "O"))).toBe("7");
      expect(editor.value(portOf(dp.ObjId + "I"))).toBe("7");

      ni.ReadValue = 8;
      ni.WriteValue = 9;
      expect(editor.value(portOf(dp.ObjId + "O"))).toBe("8");
      expect(editor.value(portOf(dp.ObjId + "I"))).toBe("9");
    });

    it("dispatchValue_RulePort_SetsItsPortValue", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      await create();

      dataHub.dispatchValue.emit([0, output(rule).ObjId, { value: true }]);
      fixture.detectChanges();

      expect(output(rule).PortValue as any).toBeTrue();
      expect(fixture.nativeElement.querySelector(".row.out .value").textContent.trim()).toBe("1");
    });

    it("dispatchValue_NodeInstanceOnThePageTwice_UpdatesBothBlocks", async () => {
      const ni = makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean);
      const first = makeDataPoint(page, ni);
      const second = makeDataPoint(page, ni, 0, 100);
      page.NodeInstances.push(first, second);
      await create();

      dataHub.dispatchValue.emit([0, ni.ObjId, { value: false }]);
      fixture.detectChanges();

      expect(editor.value(portOf(first.ObjId + "O"))).toBe("0");
      expect(editor.value(portOf(second.ObjId + "O"))).toBe("0");
      expect(editor.value(portOf(second.ObjId + "I"))).toBe("0");
      const shown = Array.from(fixture.nativeElement.querySelectorAll(".node.dp .value")).map((a: HTMLElement) => a.textContent.trim());
      expect(shown).toEqual(["0", "0"]);
    });

    it("dispatchValue_UnknownId_ChangesNothing", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      await create();

      dataHub.dispatchValue.emit([0, "unknown", { value: 1 }]);

      expect(output(rule).PortValue).toBeUndefined();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("engine events", () => {
    it("add_NewRuleWithoutPosition_IsPlacedInTheMiddleOfTheView", async () => {
      await create();
      resetView();
      const rule = addRule("AND", ["I1"], ["O1"]);

      engine.add.emit({ pageId: page.ObjId, data: rule });

      // screen (400 - 110, 600 / 3) back into stored units
      expect(rule.X).toBe(Math.round((400 - 110 - 40) / SX));
      expect(rule.Y).toBe(Math.round((200 - 40) / SY));
      expect(nodeOf(rule)).toBeDefined();
    });

    it("add_NewRuleWhileZoomed_UsesTheZoomForThePlacement", async () => {
      await create();
      resetView(2, 0, 0);
      const rule = addRule("AND", ["I1"], ["O1"]);

      engine.add.emit({ pageId: page.ObjId, data: rule });

      expect(rule.X).toBe(Math.round(((400 - 110 * 2) / 2) / SX));
      expect(rule.Y).toBe(Math.round((200 / 2) / SY));
    });

    it("add_RuleWithAPosition_KeepsIt", async () => {
      await create();
      const rule = addRule("AND", ["I1"], ["O1"], 10, 20);

      engine.add.emit({ pageId: page.ObjId, data: rule });

      expect(rule.X).toBe(10);
      expect(rule.Y).toBe(20);
    });

    it("add_ForAnotherPage_DoesNotPlaceTheRule", async () => {
      await create();
      const rule = addRule("AND", ["I1"], ["O1"]);

      engine.add.emit({ pageId: "other", data: rule });

      expect(rule.X).toBe(0);
      expect(rule.Y).toBe(0);
    });

    it("add_DataPoint_IsNotMoved", async () => {
      await create();
      const dp = addDataPoint("Lamp", NodeDataTypeEnum.Boolean);

      engine.add.emit({ pageId: page.ObjId, data: dp });

      expect(dp.X).toBe(0);
      expect(nodeOf(dp)).toBeDefined();
    });

    it("selected_BlockOnThisPage_SelectsAndCentersWithoutEmitting", async () => {
      const dp = addDataPoint("Lamp", NodeDataTypeEnum.Boolean, 200, 100);
      await create();
      resetView();

      engine.selected.emit({ logicPage: page, logicNodeInstance: dp });

      const node = nodeOf(dp);
      expect(editor.selectedNode).toBe(node);
      expect(host.selected.length).toBe(0);
      expect(editor.panX).toBe(400 - (node.x + node.w / 2));
      expect(editor.panY).toBe(300 - (node.y + DP_H / 2));
    });

    it("selected_OtherPage_IsIgnored", async () => {
      const dp = addDataPoint("Lamp", NodeDataTypeEnum.Boolean);
      await create();

      engine.selected.emit({ logicPage: makePage("other"), logicNodeInstance: dp });

      expect(editor.selectedNode).toBeUndefined();
    });

    it("selected_BlockNotOnThePage_IsIgnored", async () => {
      await create();
      const dp = makeDataPoint(page, makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean));

      engine.selected.emit({ logicPage: page, logicNodeInstance: dp });

      expect(editor.selectedNode).toBeUndefined();
    });

    it("reInit_ThisPage_RebuildsFromTheModel", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      await create();

      rule.X = 100;
      engine.reInit.emit(page);

      expect(nodeOf(rule).x).toBe(150);
    });

    it("reInit_OtherPage_DoesNotRebuild", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      await create();

      rule.X = 100;
      engine.reInit.emit(makePage("other"));
      engine.reInit.emit(undefined);

      expect(nodeOf(rule).x).toBe(0);
    });

    it("removed_AnyPage_Rebuilds", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      await create();

      rule.X = 100;
      engine.removed.emit({ logicPage: page, logicNodeInstance: void 0 });

      expect(nodeOf(rule).x).toBe(150);
    });

    it("events_EverySubscription_MarksTheViewForCheck", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      await create();
      const markForCheck = spyOn((editor as any).changeRef, "markForCheck").and.callThrough();

      const emits: [string, () => void][] = [
        ["add", () => engine.add.emit({ pageId: page.ObjId, data: rule })],
        ["selected", () => engine.selected.emit({ logicPage: page, logicNodeInstance: rule })],
        ["removed", () => engine.removed.emit({})],
        ["reInit", () => engine.reInit.emit(page)],
        ["dispatchValue", () => dataHub.dispatchValue.emit([0, input(rule).ObjId, { value: 1 }])],
        ["onZoomIn", () => page.onZoomIn.emit()],
        ["onZoomOut", () => page.onZoomOut.emit()],
        ["onZoomToView", () => page.onZoomToView.emit()]
      ];
      for (const [name, emit] of emits) {
        markForCheck.calls.reset();
        emit();
        expect(markForCheck).withContext(name).toHaveBeenCalled();
      }
    });

    it("ngOnDestroy_Events_AreNoLongerHandled", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      await create();
      const rebuild = spyOn(editor as any, "rebuild");
      const observer: ResizeObserver = (editor as any).resizeObserver;
      const disconnect = spyOn(observer, "disconnect").and.callThrough();

      fixture.destroy();
      engine.reInit.emit(page);
      engine.removed.emit({});
      dataHub.dispatchValue.emit([0, input(rule).ObjId, { value: 1 }]);

      expect(rebuild).not.toHaveBeenCalled();
      expect(disconnect).toHaveBeenCalled();
      expect(input(rule).PortValue).toBeUndefined();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("drop from the config tree", () => {
    function dropAt(dragData: any, worldX: number, worldY: number) {
      const rect = canvas().getBoundingClientRect();
      return editor.dropSuccess({
        dragData,
        mouseEvent: { clientX: rect.left + worldX * editor.zoom + editor.panX, clientY: rect.top + worldY * editor.zoom + editor.panY }
      });
    }

    beforeEach(async () => {
      await create();
      resetView();
    });

    it("allowDrop_AnyData_IsAccepted", () => {
      expect(editor.allowDrop()()).toBeTrue();
      expect(fixture.nativeElement.querySelector(".canvas[dnd-droppable]")).not.toBeNull();
    });

    it("dropSuccess_ValueNode_AddsADataPointAtTheDropPoint", async () => {
      const ni = makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean);

      await dropAt(ni, 300, 120);

      expect(page.NodeInstances.length).toBe(1);
      const dp = page.NodeInstances[0];
      expect(dp instanceof NodeInstance2RulePage).toBeTrue();
      expect(dp.NodeInstance).toBe(ni);
      expect(dp.This2NodeInstance).toBe(ni.ObjId);
      expect(dp.This2RulePage).toBe(page.ObjId);
      expect(dp.X).toBe(200);
      expect(dp.Y).toBe(100);
      expect(dp.RulePage).not.toBe(page);
      expect(dp.RulePage.ObjId).toBe(page.ObjId);
      expect(dp.RulePage.NodeInstances).toEqual([]);
      expect(dp.RulePage.RuleInstances).toEqual([]);
      expect(engine.addItem).toHaveBeenCalledOnceWith({ data: dp, pageId: page.ObjId });
      expect(instances.addLogicNodeInstance).toHaveBeenCalledOnceWith(dp);

      fixture.detectChanges();
      expect(fixture.nativeElement.querySelectorAll(".node.dp").length).toBe(1);
      expect(fixture.nativeElement.querySelector(".empty")).toBeNull();
    });

    it("dropSuccess_Zoomed_ConvertsThePointerIntoStoredUnits", async () => {
      resetView(2, 10, 20);

      await dropAt(makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean), 150, 60);

      expect(page.NodeInstances[0].X).toBe(100);
      expect(page.NodeInstances[0].Y).toBe(50);
    });

    it("dropSuccess_Device_AddsItsValueChildrenStackedBelowEachOther", async () => {
      const device = makeNodeInstance("Device", NodeDataTypeEnum.NoAttribute, { interfaceType: InterfaceTypeEnum.Ethernet });
      const a = makeNodeInstance("A", NodeDataTypeEnum.Boolean, { parent: device });
      makeNodeInstance("Channel", NodeDataTypeEnum.NoAttribute, { parent: device, interfaceType: InterfaceTypeEnum.Virtual });
      const b = makeNodeInstance("B", NodeDataTypeEnum.Integer, { parent: device });

      await dropAt(device, 300, 120);

      expect(page.NodeInstances.map(x => x.NodeInstance)).toEqual([a, b]);
      expect(page.NodeInstances.map(x => x.Y)).toEqual([100, 135]);
      expect(page.NodeInstances.map(x => x.X)).toEqual([200, 200]);
      expect(engine.addItem).toHaveBeenCalledTimes(2);
    });

    it("dropSuccess_SomethingElse_IsIgnored", async () => {
      await dropAt(new RuleTemplate(), 300, 120);
      await dropAt(void 0, 300, 120);

      expect(page.NodeInstances.length).toBe(0);
      expect(engine.addItem).not.toHaveBeenCalled();
    });

    it("dropSuccess_ServerFails_ReportsAndDoesNotRegisterTheInstance", async () => {
      engine.addItem.and.callFake(() => rejected(new Error("save failed")));

      await dropAt(makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean), 300, 120);

      expect(notify.notifyError).toHaveBeenCalled();
      expect(instances.addLogicNodeInstance).not.toHaveBeenCalled();
    });

    // BUG: NodeInstance2RulePage.createFromNodeInstance (node-instance-2-rule-page.ts:124) assigns NodeInstance, whose setter builds
    // the ports, before ObjId is set: the dropped block gets the ports "undefinedO"/"undefinedI" (plus a second, correct pair),
    // the editor uses the first pair, and wires to the dropped block are not drawn until the page is reloaded.
    it("dropSuccess_DroppedDataPoint_PortIdsMatchTheLinkPortIds", async () => {
      await dropAt(makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean), 300, 120);
      fixture.detectChanges();

      const dp = page.NodeInstances[0];
      const node = nodeOf(dp);
      // links address data point ports as <ObjId>O / <ObjId>I (link.ts fromPort / toPort)
      expect(node.outputs[0].id).toBe(dp.ObjId + "O");
      expect(node.inputs[0].id).toBe(dp.ObjId + "I");
    });

    it("connect_FromADroppedDataPoint_DrawsTheWire", async () => {
      const rule = addRule("AND", ["I1"], ["O1"], 300, 0);
      await dropAt(makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean), 0, 0);
      fixture.detectChanges();
      const node = nodeOf(page.NodeInstances[0]);

      portElement(node.outputs[0].id).dispatchEvent(pointer("pointerdown"));
      spyOn(document, "elementFromPoint").and.returnValue(portElement(input(rule).ObjId));
      canvas().dispatchEvent(pointer("pointerup"));
      await flushPromises();

      expect(page.Links.length).toBe(1);
      expect(editor.wires.length).toBe(1);
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("first render", () => {
    // BUG: ngAfterViewInit (ruleeditor.component.ts:206) reads the canvas size after the view was checked; the minimap view box
    // depends on it, so dev builds throw NG0100 (ExpressionChangedAfterItHasBeenChecked) when a page with blocks is opened.
    it("firstRender_PageWithBlocks_HasNoExpressionChangedError", () => {
      addRule("AND", ["I1"], ["O1"]);
      fixture = TestBed.createComponent(HostComponent);
      fixture.componentInstance.page = page;

      expect(() => fixture.detectChanges()).not.toThrow();
    });

    it("firstRender_EmptyPage_HasNoExpressionChangedError", () => {
      fixture = TestBed.createComponent(HostComponent);
      fixture.componentInstance.page = page;

      expect(() => fixture.detectChanges()).not.toThrow();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("empty state", () => {
    it("emptyPage_ShowsTheHintAndNoMinimap", async () => {
      await create();

      const empty: HTMLElement = fixture.nativeElement.querySelector(".empty");
      expect(empty).not.toBeNull();
      expect(empty.textContent).toContain("UI.LOGIC.EMPTY_TITLE");
      expect(empty.textContent).toContain("UI.LOGIC.EMPTY_TEXT");
      expect(fixture.nativeElement.querySelector(".minimap")).toBeNull();
      expect(fixture.nativeElement.querySelectorAll(".node").length).toBe(0);
      expect(fixture.nativeElement.querySelectorAll("g.wire").length).toBe(0);
    });

    it("pageWithBlocks_HidesTheHintAndDrawsTheMinimap", async () => {
      const rule = addRule("AND", ["I1"], ["O1"]);
      addDataPoint("Lamp", NodeDataTypeEnum.Boolean, 0, 200);
      await create();
      editor.select(nodeOf(rule), false);
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector(".empty")).toBeNull();
      const minimap: SVGElement = fixture.nativeElement.querySelector("svg.minimap");
      expect(minimap).not.toBeNull();
      expect(minimap.getAttribute("viewBox")).toBe(editor.minimap.viewBox);
      expect(minimap.querySelectorAll("rect").length).toBe(3);
      expect(minimap.querySelector("rect.mini-rule").getAttribute("class")).toBe("mini-rule selected");
      expect(minimap.querySelector("rect.mini-dp")).not.toBeNull();
      expect(minimap.querySelector("rect.mini-view")).not.toBeNull();
    });

    it("lastBlockRemoved_ShowsTheHintAgain", async () => {
      addRule("AND", ["I1"], ["O1"]);
      await create();

      page.RuleInstances = [];
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector(".empty")).not.toBeNull();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("multi selection and alignment", () => {

    function elementOf(model: any): HTMLElement {
      return fixture.nativeElement.querySelectorAll(".node")[editor.nodes.indexOf(nodeOf(model))];
    }

    /** client coordinates of a world point */
    function client(x: number, y: number) {
      const rect = canvas().getBoundingClientRect();
      return { clientX: rect.left + editor.panX + x * editor.zoom, clientY: rect.top + editor.panY + y * editor.zoom };
    }

    async function setup() {
      await create();
      resetView();
      fixture.detectChanges();
    }

    function dragNode(model: any, dx: number, dy: number, init: PointerEventInit = {}) {
      elementOf(model).dispatchEvent(pointer("pointerdown", { clientX: 500, clientY: 500, ...init }));
      hostElement().dispatchEvent(pointer("pointermove", { clientX: 500 + dx, clientY: 500 + dy, ...init }));
      fixture.detectChanges();
    }

    it("shiftClick_AddsTheBlockAndShowsTheAlignBar", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 200, 200);
      await setup();

      elementOf(a).dispatchEvent(pointer("pointerdown", { clientX: 10, clientY: 10 }));
      elementOf(b).dispatchEvent(pointer("pointerdown", { clientX: 10, clientY: 10, shiftKey: true }));
      await editor.onPointerUp(pointer("pointerup", { clientX: 10, clientY: 10 }));
      fixture.detectChanges();

      expect(editor.selectedNodes).toEqual([nodeOf(a), nodeOf(b)]);
      expect(editor.selectedNode).toBe(nodeOf(b));
      expect(host.selected[host.selected.length - 1]).toEqual({ page, items: [a, b] });
      expect(fixture.nativeElement.querySelectorAll(".node.selected").length).toBe(2);
      expect(fixture.nativeElement.querySelector(".align-bar .count").textContent).toContain("UI.LOGIC.SELECTED {\"count\":2}");
    });

    it("ctrlClick_OnASelectedBlock_TakesItOutAndStartsNoDrag", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 200, 200);
      await setup();
      editor.selectNodes([nodeOf(a), nodeOf(b)], false);

      elementOf(b).dispatchEvent(pointer("pointerdown", { clientX: 10, clientY: 10, ctrlKey: true }));
      fixture.detectChanges();

      expect(editor.selectedNodes).toEqual([nodeOf(a)]);
      expect(editor.selectedNode).toBe(nodeOf(a));
      expect(editor.drag).toBeUndefined();
      expect(fixture.nativeElement.querySelector(".align-bar")).toBeNull();
    });

    it("ctrlA_SelectsEveryBlock", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const dp = addDataPoint("Lamp", NodeDataTypeEnum.Boolean, 300, 0);
      await setup();

      await editor.onKeyDown(key("a", { ctrlKey: true }));

      expect(editor.selectedNodes).toEqual([nodeOf(a), nodeOf(dp)]);
      expect(host.selected[host.selected.length - 1]).toEqual({ page, items: [a, dp] });
    });

    it("boxSelect_ShiftDragOnTheCanvas_SelectsTheBlocksItTouches", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 200, 200);   // 300/240
      await setup();

      canvas().dispatchEvent(pointer("pointerdown", { ...client(260, 200), shiftKey: true }));
      expect(editor.drag.mode).toBe("box");
      hostElement().dispatchEvent(pointer("pointermove", { ...client(320, 260), shiftKey: true }));
      fixture.detectChanges();

      expect(editor.selectedNodes).toEqual([nodeOf(b)]);
      expect(fixture.nativeElement.querySelector(".overlay rect.marquee")).not.toBeNull();
      expect(editor.panX).toBe(40);

      await editor.onPointerUp(pointer("pointerup", client(320, 260)));
      fixture.detectChanges();

      expect(editor.box).toBeUndefined();
      expect(fixture.nativeElement.querySelector(".marquee")).toBeNull();
      expect(host.selected[host.selected.length - 1]).toEqual({ page, items: [b] });
      expect(nodeOf(a).x).toBe(0);
    });

    it("boxSelect_AddsToTheSelection", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 200, 200);
      await setup();
      editor.select(nodeOf(a), false);

      canvas().dispatchEvent(pointer("pointerdown", { ...client(260, 200), shiftKey: true }));
      hostElement().dispatchEvent(pointer("pointermove", { ...client(320, 260), shiftKey: true }));

      expect(editor.selectedNodes).toEqual([nodeOf(a), nodeOf(b)]);
    });

    it("boxSelect_ClickWithoutMove_KeepsTheSelection", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      await setup();
      editor.select(nodeOf(a), false);

      canvas().dispatchEvent(pointer("pointerdown", { ...client(600, 400), shiftKey: true }));
      await editor.onPointerUp(pointer("pointerup", client(600, 400)));

      expect(editor.selectedNodes).toEqual([nodeOf(a)]);
      expect(host.selected.length).toBe(0);
    });

    it("groupDrag_MovesEverySelectedBlockAndSavesEach", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 200, 200);
      await setup();
      editor.selectNodes([nodeOf(a), nodeOf(b)], false);

      dragNode(a, 50, 30);
      await editor.onPointerUp(pointer("pointerup"));

      expect([nodeOf(a).x, nodeOf(a).y]).toEqual([50, 30]);
      expect([nodeOf(b).x, nodeOf(b).y]).toEqual([350, 270]);
      expect(engine.updateItem).toHaveBeenCalledTimes(2);
      expect(engine.updateItem).toHaveBeenCalledWith(a, LogicUpdateScope.Drag);
      expect(engine.updateItem).toHaveBeenCalledWith(b, LogicUpdateScope.Drag);
      // the model stores whole numbers
      expect(b.X).toBe(Math.round(350 / SX));
      expect(b.Y).toBe(Math.round(270 / SY));
      expect(editor.selectedNodes.length).toBe(2);
    });

    it("groupDrag_StopsWhenTheFirstBlockReachesTheEdge", async () => {
      const a = addRule("A", ["I1"], ["O1"], 20, 50);     // 30/60
      const b = addRule("B", ["I1"], ["O1"], 200, 200);   // 300/240
      await setup();
      editor.selectNodes([nodeOf(a), nodeOf(b)], false);

      dragNode(b, -500, -500);

      expect([nodeOf(a).x, nodeOf(a).y]).toEqual([0, 0]);
      expect([nodeOf(b).x, nodeOf(b).y]).toEqual([270, 180]);
    });

    it("groupClick_WithoutMove_SelectsOnlyThatBlock", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 200, 200);
      await setup();
      editor.selectNodes([nodeOf(a), nodeOf(b)], false);

      elementOf(a).dispatchEvent(pointer("pointerdown", { clientX: 10, clientY: 10 }));
      expect(editor.selectedNodes.length).toBe(2);
      await editor.onPointerUp(pointer("pointerup", { clientX: 10, clientY: 10 }));

      expect(editor.selectedNodes).toEqual([nodeOf(a)]);
      expect(host.selected[host.selected.length - 1]).toEqual({ page, items: [a] });
      expect(engine.updateItem).not.toHaveBeenCalled();
    });

    it("snap_NearTheEdgeOfAnotherBlock_AlignsAndShowsAGuide", async () => {
      addRule("A", ["I1"], ["O1"], 2, 0);   // x = 3, off the grid
      const b = addRule("B", ["I1"], ["O1"], 200, 200);
      await setup();

      // B from 300 to 6: the grid would say 10, the left edge of A says 3
      dragNode(b, -294, 0);

      expect(nodeOf(b).x).toBe(3);
      expect(nodeOf(b).y).toBe(240);
      expect(editor.guides.length).toBe(1);
      expect(editor.guides[0].x1).toBe(3);
      expect(editor.guides[0].x2).toBe(3);
      expect(fixture.nativeElement.querySelectorAll(".overlay line.guide").length).toBe(1);

      await editor.onPointerUp(pointer("pointerup"));
      fixture.detectChanges();
      expect(editor.guides.length).toBe(0);
      expect(fixture.nativeElement.querySelector(".guide")).toBeNull();
    });

    it("pointerCancel_DuringAGroupDrag_PutsTheBlocksBackWithoutSaving", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 200, 200);
      await setup();
      editor.selectNodes([nodeOf(a), nodeOf(b)], false);

      dragNode(a, 50, 30);
      editor.onPointerCancel();

      expect([nodeOf(a).x, nodeOf(a).y]).toEqual([0, 0]);
      expect([nodeOf(b).x, nodeOf(b).y]).toEqual([300, 240]);
      expect(editor.guides.length).toBe(0);
      expect(engine.updateItem).not.toHaveBeenCalled();
    });

    it("snap_WithAlt_UsesTheGridOnly", async () => {
      addRule("A", ["I1"], ["O1"], 2, 0);
      const b = addRule("B", ["I1"], ["O1"], 200, 200);
      await setup();

      dragNode(b, -294, 0, { altKey: true });

      expect(nodeOf(b).x).toBe(10);
      expect(editor.guides.length).toBe(0);
    });

    it("snap_TooFarAway_UsesTheGrid", async () => {
      addRule("A", ["I1"], ["O1"], 2, 0);
      const b = addRule("B", ["I1"], ["O1"], 200, 200);
      await setup();

      dragNode(b, -280, 0);   // 20 is 17 away from the edge of A

      expect(nodeOf(b).x).toBe(20);
      expect(editor.guides.length).toBe(0);
    });

    it("snap_ToleranceIsInScreenPixels", async () => {
      addRule("A", ["I1"], ["O1"], 2, 0);
      const b = addRule("B", ["I1"], ["O1"], 200, 200);
      await setup();
      resetView(0.5);

      // 9 world pixels are 4.5 screen pixels at 50 %
      dragNode(b, (12 - 300) * 0.5, 0);

      expect(nodeOf(b).x).toBe(3);
    });

    it("snap_NearTheWiredPort_StraightensTheWire", async () => {
      const a = addRule("A", ["I1"], ["O1", "O2"]);
      const b = addRule("B", ["I1"], ["O1"], 300, 150);   // 450/180
      page.Links.push(linkRules(page, output(a, 1), input(b)));
      await setup();

      // O2 of A sits at 42 + 6 + 22 + 11 = 81, I1 of B 59 below its top: a straight wire needs y = 22 (the grid says 20,
      // and the bottom edges would be just as close)
      dragNode(b, 0, 25 - 180);

      expect(nodeOf(b).y).toBe(22);
      expect(nodeOf(b).x).toBe(450);
      const wire = editor.wires[0];
      expect(editor.anchor(wire.from).y).toBe(editor.anchor(wire.to).y);
      expect(editor.guides).toEqual([{ x1: -8, y1: 81, x2: 678, y2: 81 }]);
    });

    it("align_Left_LinesUpTheBlocksAndSavesThem", async () => {
      const a = addRule("A", ["I1"], ["O1"], 100, 0);
      const b = addRule("B", ["I1"], ["O1"], 200, 200);
      await setup();
      editor.selectNodes([nodeOf(a), nodeOf(b)], false);

      await editor.align("left");

      expect(nodeOf(a).x).toBe(150);
      expect(nodeOf(b).x).toBe(150);
      expect(nodeOf(b).y).toBe(240);
      expect(engine.updateItem).toHaveBeenCalledTimes(2);
      expect(b.X).toBeCloseTo(100);
    });

    it("align_EveryMode_UsesTheBoxAroundTheSelection", async () => {
      const a = addRule("A", ["I1"], ["O1"]);                  // 0/0, 220 x 76
      const b = addRule("B", ["I1", "I2"], ["O1"], 200, 200);  // 300/240, 220 x 98
      await setup();
      const na = nodeOf(a), nb = nodeOf(b);
      editor.selectNodes([na, nb], false);

      await editor.align("right");
      expect([na.x, nb.x]).toEqual([300, 300]);

      na.x = 0;
      await editor.align("center");
      expect([na.x, nb.x]).toEqual([150, 150]);

      await editor.align("top");
      expect([na.y, nb.y]).toEqual([0, 0]);

      nb.y = 240;
      await editor.align("bottom");
      expect([na.y + na.h, nb.y + nb.h]).toEqual([338, 338]);

      na.y = 0;
      nb.y = 240;
      await editor.align("middle");
      expect([na.y + na.h / 2, nb.y + nb.h / 2]).toEqual([169, 169]);
    });

    it("align_SingleBlock_DoesNothing", async () => {
      const a = addRule("A", ["I1"], ["O1"], 100, 0);
      await setup();
      editor.select(nodeOf(a), false);

      await editor.align("left");

      expect(engine.updateItem).not.toHaveBeenCalled();
    });

    it("alignBar_Button_Aligns", async () => {
      const a = addRule("A", ["I1"], ["O1"], 100, 0);
      const b = addRule("B", ["I1"], ["O1"], 200, 200);
      await setup();
      editor.selectNodes([nodeOf(a), nodeOf(b)], false);
      fixture.detectChanges();

      const buttons = fixture.nativeElement.querySelectorAll(".align-bar button");
      expect(buttons.length).toBe(8);
      // distribute needs three blocks
      expect(buttons[6].disabled).toBeTrue();
      buttons[0].click();
      await flushPromises();

      expect(nodeOf(b).x).toBe(150);
    });

    it("distribute_Horizontal_LeavesEqualGaps", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 200, 0);   // 300
      const c = addRule("C", ["I1"], ["O1"], 800, 0);   // 1200
      await setup();
      editor.selectNodes([nodeOf(c), nodeOf(a), nodeOf(b)], false);

      await editor.distribute("h");

      // span 0..1420, three blocks of 220: two gaps of 380
      expect([nodeOf(a).x, nodeOf(b).x, nodeOf(c).x]).toEqual([0, 600, 1200]);
      expect(engine.updateItem).toHaveBeenCalledTimes(3);
    });

    it("distribute_VerticalOverlapping_PullsTheBlocksApart", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 0, 10);   // 12
      const c = addRule("C", ["I1"], ["O1"], 0, 20);   // 24
      await setup();
      editor.selectNodes([nodeOf(a), nodeOf(b), nodeOf(c)], false);

      await editor.distribute("v");

      // blocks of 76 with the smallest gap of 20
      expect([nodeOf(a).y, nodeOf(b).y, nodeOf(c).y]).toEqual([0, 96, 192]);
    });

    it("distribute_TwoBlocks_DoesNothing", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 200, 0);
      await setup();
      editor.selectNodes([nodeOf(a), nodeOf(b)], false);

      await editor.distribute("h");

      expect(engine.updateItem).not.toHaveBeenCalled();
    });

    it("arrowKeys_MoveTheWholeSelectionAndSaveEachOnce", async () => {
      const a = addRule("A", ["I1"], ["O1"], 20, 50);     // 30/60
      const b = addRule("B", ["I1"], ["O1"], 200, 200);   // 300/240
      await setup();
      editor.selectNodes([nodeOf(a), nodeOf(b)], false);

      await editor.onKeyDown(key("ArrowRight"));
      await editor.onKeyDown(key("ArrowUp"));
      await editor.onKeyDown(key("ArrowUp", { shiftKey: true }));

      expect([nodeOf(a).x, nodeOf(a).y]).toEqual([40, 49]);
      expect([nodeOf(b).x, nodeOf(b).y]).toEqual([310, 229]);
      expect(engine.updateItem).not.toHaveBeenCalled();

      await new Promise(resolve => setTimeout(resolve, 450));
      await flushPromises();
      expect(engine.updateItem).toHaveBeenCalledTimes(2);
    });

    it("deleteSelection_MultipleBlocks_RemovesTheirLinksOnceAndEveryBlock", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 200, 0);
      const c = addRule("C", ["I1"], ["O1"], 400, 0);
      page.Links.push(linkRules(page, output(a), input(b)));
      page.Links.push(linkRules(page, output(b), input(c)));
      await setup();
      editor.selectNodes([nodeOf(a), nodeOf(b)], false);

      await editor.deleteSelection();

      expect(engine.removeLink).toHaveBeenCalledTimes(2);
      expect(host.removed.map(r => r.item)).toEqual([a, b]);
      expect(host.removed.every(r => r.multiple === true)).toBeTrue();
      expect(editor.selectedNodes).toEqual([]);
    });

    it("deleteSelection_SingleBlock_SendsNoMultipleFlag", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      await setup();
      editor.select(nodeOf(a), false);

      await editor.deleteSelection();

      expect(host.removed).toEqual([{ item: a, page, removed: false }]);
    });

    it("selectLink_ClearsTheBlockSelection", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 200, 0);
      const link = linkRules(page, output(a), input(b));
      page.Links.push(link);
      await setup();
      editor.selectNodes([nodeOf(a), nodeOf(b)], false);

      editor.selectLink(pointer("pointerdown"), link);

      expect(editor.selectedNodes).toEqual([]);
      expect(editor.selectedLink).toBe(link);
    });

    it("rebuild_KeepsTheMultiSelectionById", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 200, 0);
      await setup();
      editor.selectNodes([nodeOf(a), nodeOf(b)], false);

      page.RuleInstances = [...page.RuleInstances];
      fixture.detectChanges();

      expect(editor.selectedNodes).toEqual([nodeOf(a), nodeOf(b)]);
    });

    it("minimap_MarksEverySelectedBlock", async () => {
      const a = addRule("A", ["I1"], ["O1"]);
      const b = addRule("B", ["I1"], ["O1"], 200, 0);
      await setup();
      editor.selectNodes([nodeOf(a), nodeOf(b)], false);
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelectorAll(".minimap rect.selected").length).toBe(2);
    });
  });
});
