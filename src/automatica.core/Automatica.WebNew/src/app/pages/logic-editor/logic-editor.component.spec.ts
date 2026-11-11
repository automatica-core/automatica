import { Component, EventEmitter, Input, NO_ERRORS_SCHEMA, Output } from "@angular/core";
import { ComponentFixture, TestBed, fakeAsync, tick, flush } from "@angular/core/testing";
import { ActivatedRoute, Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { Subject } from "rxjs";
import { DndModule, DragDropService } from "p3root-angular-dnd";

import { LogicEditorComponent } from "./logic-editor.component";
import { RuleEditorComponent } from "src/app/shared/ruleeditor/ruleeditor.component";
import { LogicEngineService } from "src/app/services/logicengine.service";
import { ConfigService } from "src/app/services/config.service";
import { NotifyService } from "src/app/services/notify.service";
import { AreaService } from "src/app/services/areas.service";
import { CategoryService } from "src/app/services/categories.service";
import { GroupsService } from "src/app/services/groups.service";
import { AppService } from "src/app/services/app.service";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { LogicEditorInstanceService } from "src/app/services/logic-editor-instance.service";
import { HubConnectionService } from "src/app/base/communication/hubs/hub-connection.service";
import { ViewportService } from "src/app/services/viewport.service";
import { RulePage } from "src/app/base/model/rule-page";
import { RuleInstance } from "src/app/base/model/rule-instance";
import { NodeInstance } from "src/app/base/model/node-instance";
import { NodeDataTypeEnum } from "src/app/base/model/node-data-type";
import { InterfaceTypeEnum } from "src/app/base/model/interface-type";
import {
  FakeDxTemplateDirective, FakeTranslateAsyncPipe, FakeTranslatePipe, createNotifySpy, createTranslateSpy, emitter, flushPromises, nextFrames, rejected
} from "src/app/testing/test-helpers";
import { input, linkRules, makeDataPoint, makeNodeInstance, makePage, makeRule, makeRuleTemplate, output } from "src/app/testing/logic-fixtures";

/** the config tree is a big DevExtreme tree list, the page only calls these methods on it */
@Component({ selector: "p3-config-tree", template: "", standalone: false })
class StubConfigTreeComponent {
  @Input() isLoading: boolean;
  @Input() loadLogicNodeInstances: boolean;
  @Output() onNodeSelect = new EventEmitter<any>();
  @Output() onLogicNodeInstanceRemoved = new EventEmitter<any>();

  selectNodeById = jasmine.createSpy("selectNodeById");
  refreshTree = jasmine.createSpy("refreshTree");
  add = jasmine.createSpy("add");
  removeItem = jasmine.createSpy("removeItem");
  validate = jasmine.createSpy("validate");
  scan = jasmine.createSpy("scan");
  fileUploaded = jasmine.createSpy("fileUploaded").and.resolveTo(undefined);
  saveLearnNodeInstances = jasmine.createSpy("saveLearnNodeInstances");
}

describe("LogicEditorComponent", () => {
  let fixture: ComponentFixture<LogicEditorComponent>;
  let component: LogicEditorComponent;
  let engine: any;
  let config: any;
  let notify: jasmine.SpyObj<NotifyService>;
  let translate: jasmine.SpyObj<L10nTranslationService>;
  let instances: any;
  let dataHub: any;
  let hub: any;
  let router: any;
  let route: any;
  let nodes: Map<string, NodeInstance>;
  let pageA: RulePage;
  let pageB: RulePage;
  let pageC: RulePage;

  beforeEach(() => {
    translate = createTranslateSpy();
    notify = createNotifySpy();
    nodes = new Map<string, NodeInstance>();

    // pages arrive unsorted, the page sorts them by name
    pageB = makePage("B page");
    pageA = makePage("A page");
    pageC = makePage("C page");

    engine = {
      add: emitter(), selected: emitter(), removed: emitter(), reInit: emitter(), showInfo: emitter(),
      getRuleTemplates: jasmine.createSpy("getRuleTemplates").and.resolveTo([makeRuleTemplate("AND", ["I1", "I2"], ["O1"])]),
      // the real service emits `add` before it posts (logicengine.service.ts addItem)
      addItem: jasmine.createSpy("addItem").and.callFake((item: any) => { engine.add.emit(item); return Promise.resolve(); }),
      removeItem: jasmine.createSpy("removeItem").and.resolveTo(undefined),
      addPage: jasmine.createSpy("addPage").and.resolveTo(undefined),
      removePage: jasmine.createSpy("removePage").and.resolveTo(undefined),
      reload: jasmine.createSpy("reload").and.resolveTo(undefined),
      updateItem: jasmine.createSpy("updateItem").and.resolveTo(undefined),
      addOrUpdateLink: jasmine.createSpy("addOrUpdateLink").and.resolveTo(undefined),
      removeLink: jasmine.createSpy("removeLink").and.resolveTo(undefined)
    };
    config = { getLinkableNodes: jasmine.createSpy("getLinkableNodes").and.resolveTo([]) };
    instances = {
      pages: [pageB, pageA, pageC],
      load: jasmine.createSpy("load").and.resolveTo(undefined),
      getNodeInstance: jasmine.createSpy("getNodeInstance").and.callFake((id: string) => nodes.get(id)),
      addLogicNodeInstance: jasmine.createSpy("addLogicNodeInstance"),
      removeLogicNodeInstance: jasmine.createSpy("removeLogicNodeInstance")
    };
    dataHub = { dispatchValue: emitter(), unSubscribeForAll: jasmine.createSpy("unSubscribeForAll").and.resolveTo(undefined) };
    hub = { init: jasmine.createSpy("init").and.resolveTo(undefined) };
    router = { navigate: jasmine.createSpy("navigate").and.resolveTo(true), url: "/admin/logic-editor" };
    route = { snapshot: { params: {} as any }, params: new Subject<any>() };

    TestBed.configureTestingModule({
      declarations: [LogicEditorComponent, RuleEditorComponent, StubConfigTreeComponent],
      imports: [FakeTranslateAsyncPipe, FakeTranslatePipe, FakeDxTemplateDirective, DndModule.forRoot()],
      providers: [
        { provide: LogicEngineService, useValue: engine },
        { provide: ConfigService, useValue: config },
        { provide: NotifyService, useValue: notify },
        { provide: L10nTranslationService, useValue: translate },
        { provide: AreaService, useValue: { getAllAreaInstances: () => Promise.resolve([]) } },
        { provide: CategoryService, useValue: { getAllCategoryInstances: () => Promise.resolve([]) } },
        { provide: GroupsService, useValue: { getUserGroups: () => Promise.resolve([]) } },
        { provide: DataHubService, useValue: dataHub },
        { provide: LogicEditorInstanceService, useValue: instances },
        { provide: HubConnectionService, useValue: hub },
        { provide: ActivatedRoute, useValue: route },
        { provide: Router, useValue: router },
        AppService
      ],
      schemas: [NO_ERRORS_SCHEMA]
    });
    spyOn(Element.prototype, "setPointerCapture");
    // handleError logs every error it reports
    spyOn(console, "log");
  });

  async function init(pageId?: string) {
    route.snapshot.params.id = pageId;
    fixture = TestBed.createComponent(LogicEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
    await flushPromises();
    // the ResizeObserver of the canvas reports with the next frame and marks the view
    await nextFrames();
    fixture.detectChanges();
  }

  function tabs(): HTMLButtonElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll(".page-tab"));
  }

  function editors(): HTMLElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll("p3-ruleeditor"));
  }

  function configTree(): StubConfigTreeComponent {
    return <any>component.configTree;
  }

  function ruleEditor(): RuleEditorComponent {
    return fixture.debugElement.query(a => a.componentInstance instanceof RuleEditorComponent)?.componentInstance;
  }

  /** adds a block the way the menu does it: into the model, announced through the engine's add event */
  function addBlock(page: RulePage) {
    const rule = makeRule(translate, "AND", ["I1"], ["O1"]);
    page.RuleInstances.push(rule);
    engine.add.emit({ pageId: page.ObjId, data: rule });
    fixture.detectChanges();
    return rule;
  }

  // ---------------------------------------------------------------------------------------------
  describe("loading", () => {
    it("ngOnInit_LoadsEverythingAndSortsThePagesByName", async () => {
      await init();

      expect(hub.init).toHaveBeenCalled();
      expect(instances.load).toHaveBeenCalled();
      expect(engine.getRuleTemplates).toHaveBeenCalled();
      expect(config.getLinkableNodes).toHaveBeenCalled();
      expect(component.pages.map(a => a.Name)).toEqual(["A page", "B page", "C page"]);
      expect(component.ruleTemplates.length).toBe(1);
      expect(component.isLoading).toBeFalse();
    });

    it("ngOnInit_NoPageInTheRoute_SelectsTheFirstPage", async () => {
      await init();

      expect(component.selectedPage).toBe(pageA);
      expect(component.selectedItem).toBe(pageA);
    });

    it("ngOnInit_PageIdInTheRoute_SelectsThatPage", async () => {
      await init(pageC.ObjId);

      expect(component.selectedPage).toBe(pageC);
      expect(configTree().selectNodeById).toHaveBeenCalledWith(undefined);
    });

    it("ngOnInit_UnknownPageId_KeepsTheFirstPage", async () => {
      await init("does-not-exist");

      expect(component.selectedPage).toBe(pageA);
    });

    it("constructor_SetsTheAppTitle", async () => {
      await init();

      expect(TestBed.inject(AppService).title).toBe("RULEENGINE.NAME");
    });

    it("loadData_DataPointsGetTheirNodeInstanceFromTheInstanceService", async () => {
      const ni = makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean);
      const dp = makeDataPoint(pageB, ni);
      pageB.NodeInstances.push(dp);
      // the instance service holds the live node (with the current values and settings)
      const live = makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean);
      live.ObjId = ni.ObjId;
      nodes.set(ni.ObjId, live);

      await init();

      expect(instances.getNodeInstance).toHaveBeenCalledWith(ni.ObjId);
      expect(dp.NodeInstance).toBe(live);
    });

    it("initPages_LinksBetweenRules_GetTheirRuleInstancesBack", async () => {
      const a = makeRule(translate, "A", ["I1"], ["O1"]);
      const b = makeRule(translate, "B", ["I1"], ["O1"], 200, 0);
      pageB.RuleInstances.push(a, b);
      const link = linkRules(pageB, output(a), input(b));
      output(a).RuleInstance = void 0;
      input(b).RuleInstance = void 0;
      pageB.Links.push(link);

      await init();

      expect(link.FromRuleInstance.RuleInstance).toBe(a);
      expect(link.ToRuleInstance.RuleInstance).toBe(b);
    });

    it("loadData_ServiceFails_ReportsTheErrorAndStopsLoading", async () => {
      engine.getRuleTemplates.and.callFake(() => rejected(new Error("offline")));

      await init();

      expect(notify.notifyError).toHaveBeenCalled();
      expect(component.isLoading).toBeFalse();
      expect(TestBed.inject(AppService).isLoading).toBeFalse();
    });

    it("isStartingChanged_ServerStartedAgain_ReloadsTheData", async () => {
      await init();
      instances.load.calls.reset();
      const appService = TestBed.inject(AppService);

      appService.isStarting = true;
      appService.isStarting = false;
      await flushPromises();

      expect(instances.load).toHaveBeenCalledTimes(1);
    });

    it("ngOnDestroy_UnsubscribesTheValues", async () => {
      await init();

      fixture.destroy();

      expect(dataHub.unSubscribeForAll).toHaveBeenCalled();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("page tabs", () => {
    it("tabs_OnePerPageInNameOrder_TheSelectedOneIsActive", async () => {
      await init();

      expect(tabs().map(a => a.textContent.trim())).toEqual(["A page", "B page", "C page"]);
      expect(tabs()[0].classList).toContain("active");
      expect(tabs()[0].getAttribute("aria-selected")).toBe("true");
      expect(tabs()[1].classList).not.toContain("active");
      expect(tabs()[1].getAttribute("aria-selected")).toBe("false");
    });

    it("tabs_OnlyTheSelectedPageHasAnEditor", async () => {
      await init();

      expect(editors().length).toBe(1);
      expect(editors()[0].id).toBe(pageA.ObjId);
      expect(ruleEditor().page).toBe(pageA);
    });

    it("tabs_Click_SwitchesTheCanvasAndTheSelection", async () => {
      await init();
      configTree().selectNodeById.calls.reset();

      tabs()[2].click();
      fixture.detectChanges();

      expect(component.selectedPage).toBe(pageC);
      expect(component.selectedItem).toBe(pageC);
      expect(configTree().selectNodeById).toHaveBeenCalledWith(undefined);
      expect(tabs()[2].classList).toContain("active");
      expect(tabs()[0].classList).not.toContain("active");
      expect(editors().length).toBe(1);
      expect(editors()[0].id).toBe(pageC.ObjId);
    });

    it("onTabSelectionChanged_PageChosenInTheList_NavigatesToItsRoute", async () => {
      await init();

      component.onTabSelectionChanged({ addedItems: [pageB] });

      expect(router.navigate).toHaveBeenCalledOnceWith(["../", pageB.ObjId], { relativeTo: route });
    });

    it("onTabSelectionChanged_NothingAdded_DoesNotNavigate", async () => {
      await init();

      component.onTabSelectionChanged({ addedItems: [] });
      component.onTabSelectionChanged({});

      expect(router.navigate).not.toHaveBeenCalled();
    });

    it("routeParams_OtherPageId_SelectsThatPage", async () => {
      await init();

      route.params.next({ id: pageB.ObjId });

      expect(component.selectedPage).toBe(pageB);
    });

    it("routeParams_UnknownOrMissingId_KeepsThePage", async () => {
      await init();

      route.params.next({ id: "unknown" });
      route.params.next({});

      expect(component.selectedPage).toBe(pageA);
    });

    // BUG: the route.params subscription (logic-editor.component.ts:110) changes selectedPage outside a template event and does
    // not mark the OnPush view, so choosing a page in the "logic pages" list only switches the canvas on the next unrelated check.
    it("routeParams_OtherPageId_UpdatesTheTabsAndTheCanvas", async () => {
      await init();

      route.params.next({ id: pageB.ObjId });
      fixture.detectChanges();

      expect(tabs()[1].classList).toContain("active");
      expect(editors()[0].id).toBe(pageB.ObjId);
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("OnPush and the editor events", () => {
    it("plainModelChange_WithoutAnEvent_IsNotRendered", async () => {
      await init();

      pageA.RuleInstances.push(makeRule(translate, "AND", ["I1"], ["O1"]));
      fixture.detectChanges();

      // the page is OnPush: a change nobody announced stays invisible (this is why the editor marks its view on every event)
      expect(fixture.nativeElement.querySelectorAll(".node.rule").length).toBe(0);
    });

    it("engineAdd_RuleAddedToTheSelectedPage_IsRenderedThroughTheOnPushPage", async () => {
      await init();

      addBlock(pageA);

      expect(fixture.nativeElement.querySelectorAll(".node.rule").length).toBe(1);
      expect(fixture.nativeElement.querySelector(".empty")).toBeNull();
    });

    it("engineReInit_RuleRemoved_DisappearsFromTheCanvas", async () => {
      await init();
      addBlock(pageA);

      pageA.RuleInstances = [];
      engine.reInit.emit(pageA);
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelectorAll(".node.rule").length).toBe(0);
      expect(fixture.nativeElement.querySelector(".empty")).not.toBeNull();
    });

    it("engineRemoved_IsRenderedThroughTheOnPushPage", async () => {
      await init();
      const rule = addBlock(pageA);

      pageA.removeRuleInstance(rule.ObjId);
      engine.removed.emit({ logicNodeInstance: rule, logicPage: pageA });
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelectorAll(".node.rule").length).toBe(0);
    });

    it("dispatchValue_NewValue_IsShownThroughTheOnPushPage", async () => {
      await init();
      const rule = addBlock(pageA);

      dataHub.dispatchValue.emit([0, output(rule).ObjId, { value: 42 }]);
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector(".row.out .value").textContent.trim()).toBe("42");
    });

    it("zoomButtons_ZoomTheCanvasThroughTheOnPushPage", async () => {
      await init();
      addBlock(pageA);
      const editor = ruleEditor();
      const before = editor.zoom;

      component.onZoomIn(void 0);
      fixture.detectChanges();

      expect(editor.zoom).toBeCloseTo(Math.min(2.5, before * 1.25), 5);
      expect(fixture.nativeElement.querySelector(".world").style.transform).toContain(`scale(${editor.zoom})`);
    });

    it("addRule_FromTheMenu_PutsTheRuleOnTheCanvas", async () => {
      await init();

      await component.addRule(component.ruleTemplates[0]);
      fixture.detectChanges();

      expect(pageA.RuleInstances.length).toBe(1);
      expect(fixture.nativeElement.querySelectorAll(".node.rule").length).toBe(1);
      expect(fixture.nativeElement.querySelector(".node.rule .title").textContent.trim()).toBe("AND");
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("drop onto the canvas", () => {
    let dnd: DragDropService;

    beforeEach(async () => {
      await init();
      dnd = TestBed.inject(DragDropService);
    });

    afterEach(() => {
      dnd.isDragged = false;
      dnd.dragData = void 0;
    });

    function drop(data: any, dragged = true) {
      dnd.isDragged = dragged;
      dnd.dragData = data;
      const canvas: HTMLElement = fixture.nativeElement.querySelector(".canvas");
      const rect = canvas.getBoundingClientRect();
      const editor = ruleEditor();
      // world (300, 120) = stored (200, 100)
      const event = new DragEvent("drop", {
        bubbles: true, cancelable: true,
        clientX: rect.left + editor.panX + 300 * editor.zoom, clientY: rect.top + editor.panY + 120 * editor.zoom
      });
      canvas.dispatchEvent(event);
      return event;
    }

    it("canvas_IsADropZoneThatAcceptsTreeNodes", () => {
      const canvas: HTMLElement = fixture.nativeElement.querySelector(".canvas");

      expect(canvas.hasAttribute("dnd-droppable")).toBeTrue();
      expect((<any>ruleEditor().allowDrop())(makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean))).toBeTrue();
    });

    it("drop_ValueNodeFromTheTree_AddsADataPointToTheSelectedPage", async () => {
      const ni = makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean);

      const event = drop(ni);
      await flushPromises();
      fixture.detectChanges();

      expect(event.defaultPrevented).toBeTrue();
      expect(pageA.NodeInstances.length).toBe(1);
      const dp = pageA.NodeInstances[0];
      expect(dp.NodeInstance).toBe(ni);
      expect(dp.X).toBeCloseTo(200, 5);
      expect(dp.Y).toBeCloseTo(100, 5);
      expect(engine.addItem).toHaveBeenCalledOnceWith({ data: dp, pageId: pageA.ObjId });
      expect(instances.addLogicNodeInstance).toHaveBeenCalledOnceWith(dp);
      expect(fixture.nativeElement.querySelectorAll(".node.dp").length).toBe(1);
      expect(fixture.nativeElement.querySelector(".node.dp .name").textContent.trim()).toBe("Lamp");
    });

    it("drop_Device_AllValueChildrenAppearOnTheCanvas", async () => {
      const device = makeNodeInstance("Device", NodeDataTypeEnum.NoAttribute, { interfaceType: InterfaceTypeEnum.Ethernet });
      makeNodeInstance("A", NodeDataTypeEnum.Boolean, { parent: device });
      makeNodeInstance("B", NodeDataTypeEnum.Double, { parent: device });
      makeNodeInstance("C", NodeDataTypeEnum.String, { parent: device });

      drop(device);
      await flushPromises(20);
      fixture.detectChanges();

      expect(pageA.NodeInstances.map(a => a.NodeInstance.Name)).toEqual(["A", "B", "C"]);
      // the children after the first are added after an await, the engine's add event has to bring them onto the OnPush page
      expect(fixture.nativeElement.querySelectorAll(".node.dp").length).toBe(3);
    });

    it("drop_NothingDragged_IsIgnored", async () => {
      const event = drop(makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean), false);
      await flushPromises();

      expect(event.defaultPrevented).toBeFalse();
      expect(pageA.NodeInstances.length).toBe(0);
      expect(engine.addItem).not.toHaveBeenCalled();
    });

    it("drop_RuleTemplate_IsIgnoredByTheCanvas", async () => {
      drop(makeRuleTemplate("AND", ["I1"], ["O1"]));
      await flushPromises();

      expect(pageA.NodeInstances.length).toBe(0);
      expect(pageA.RuleInstances.length).toBe(0);
      expect(engine.addItem).not.toHaveBeenCalled();
    });

    it("drop_OnAnotherSelectedPage_GoesToThatPage", async () => {
      tabs()[1].click();
      fixture.detectChanges();
      await nextFrames();
      fixture.detectChanges();

      drop(makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean));
      await flushPromises();

      expect(pageA.NodeInstances.length).toBe(0);
      expect(pageB.NodeInstances.length).toBe(1);
      expect(engine.addItem.calls.mostRecent().args[0].pageId).toBe(pageB.ObjId);
    });

    it("drop_DataPoint_BelongsToThePageItself", async () => {
      drop(makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean));
      await flushPromises();

      const dp = pageA.NodeInstances[0];
      // the tree row shows dp.RulePage.Name: a copy kept the old name after a rename
      expect(dp.RulePage).toBe(pageA);
      // the page is not sent along (the page lists the data point, that would recurse)
      const json: any = dp.toJson();
      expect(json.This2RulePageNavigation).toBeUndefined();
      expect(json.This2RulePage).toBe(pageA.ObjId);
      expect(() => JSON.stringify(pageA.toJson())).not.toThrow();
    });

    it("drop_ThenDeletedInTheTree_OnlyThatDataPointLeavesThePage", async () => {
      const lamp = makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean);
      const fan = makeNodeInstance("Fan", NodeDataTypeEnum.Boolean);
      drop(lamp);
      await flushPromises();
      drop(fan);
      await flushPromises();
      fixture.detectChanges();
      const [lampDp, fanDp] = pageA.NodeInstances;

      // the tree's context menu "delete" on the lamp's page row
      configTree().onLogicNodeInstanceRemoved.emit(lampDp);
      await flushPromises();
      fixture.detectChanges();

      // the page used to get the (empty) data points of a page copy: every data point vanished from the canvas
      expect(pageA.NodeInstances).toEqual([fanDp]);
      expect(engine.removeItem).toHaveBeenCalledOnceWith(lampDp);
      expect(instances.removeLogicNodeInstance).toHaveBeenCalledOnceWith(lampDp);
      expect(Array.from(fixture.nativeElement.querySelectorAll(".node.dp .name")).map((a: any) => a.textContent.trim())).toEqual(["Fan"]);
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("selection", () => {
    beforeEach(async () => {
      await init();
    });

    it("onSelectedItemsChanged_Rule_BecomesTheSelectedItem", () => {
      const rule = makeRule(translate, "AND", ["I1"], ["O1"]);

      component.onSelectedItemsChanged({ page: pageA, items: [rule] });

      expect(component.selectedItem).toBe(rule);
    });

    it("onSelectedItemsChanged_DataPoint_SelectsItsNodeInstanceInTheTree", () => {
      const ni = makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean);
      nodes.set(ni.ObjId, ni);
      const dp = makeDataPoint(pageA, ni);

      component.onSelectedItemsChanged({ page: pageA, items: [dp] });

      expect(configTree().selectNodeById).toHaveBeenCalledWith(ni.ObjId);
      expect(component.selectedItem).toBe(ni);
    });

    it("onSelectedItemsChanged_Nothing_ClearsTheSelection", () => {
      component.onSelectedItemsChanged({ page: pageA, items: [] });

      expect(component.selectedItem).toBeUndefined();
      expect(configTree().selectNodeById).toHaveBeenCalledWith(undefined);
    });

    it("onSelectedItemsChanged_Link_ClearsTheSelection", () => {
      const a = makeRule(translate, "A", ["I1"], ["O1"]);
      const b = makeRule(translate, "B", ["I1"], ["O1"]);

      component.onSelectedItemsChanged({ page: pageA, items: [linkRules(pageA, output(a), input(b))] });

      expect(component.selectedItem).toBeUndefined();
    });

    it("onSelectedItemsChanged_OtherPage_IsIgnored", () => {
      component.onSelectedItemsChanged({ page: pageB, items: [] });

      expect(component.selectedItem).toBe(pageA);
    });

    it("selectedItemsChanged_FromTheEditor_ReachesThePage", () => {
      const rule = addBlock(pageA);

      ruleEditor().select(ruleEditor().nodes[0]);

      expect(component.selectedItem).toBe(rule);
    });

    it("unselectItem_ClearsTheSelectedItem", () => {
      component.unselectItem();

      expect(component.selectedItem).toBeUndefined();
    });

    it("nodeSelect_NodeInstance_BecomesTheSelectedItem", async () => {
      const ni = makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean);

      configTree().onNodeSelect.emit(ni);
      await flushPromises();

      expect(component.selectedItem).toBe(ni);
    });

    it("nodeSelect_DataPointOnAnotherPage_NavigatesThereAndSelectsTheBlock", fakeAsync(() => {
      const dp = makeDataPoint(pageC, makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean));
      const selected: any[] = [];
      engine.selected.subscribe((a: any) => selected.push(a));

      component.nodeSelect(dp);
      tick();

      expect(router.navigate).toHaveBeenCalledOnceWith(["../", pageC.ObjId], { relativeTo: route });
      expect(component.selectedPage).toBe(pageC);
      expect(component.selectedItem).toBe(dp);
      expect(selected.length).toBe(0);
      tick(10);
      expect(selected).toEqual([{ logicNodeInstance: dp, logicPage: pageC }]);
      flush();
    }));

    it("nodeSelect_DataPointOnTheSelectedPage_DoesNotNavigate", fakeAsync(() => {
      const dp = makeDataPoint(pageA, makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean));

      component.nodeSelect(dp);
      tick(10);

      expect(router.navigate).not.toHaveBeenCalled();
      expect(component.selectedItem).toBe(dp);
      flush();
    }));

    // selectedPage changes after an await; the block selection the editor gets 10 ms later marks the OnPush page
    it("nodeSelect_DataPointOnAnotherPage_ShowsThatPage", fakeAsync(() => {
      const dp = makeDataPoint(pageC, makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean));

      configTree().onNodeSelect.emit(dp);
      tick(10);
      fixture.detectChanges();

      expect(editors()[0].id).toBe(pageC.ObjId);
      expect(tabs()[2].classList).toContain("active");
      flush();
    }));
  });

  // ---------------------------------------------------------------------------------------------
  describe("editing", () => {
    beforeEach(async () => {
      await init();
    });

    it("addRule_Template_AddsATranslatedRuleToTheSelectedPage", async () => {
      await component.addRule(component.ruleTemplates[0]);

      expect(pageA.RuleInstances.length).toBe(1);
      const rule = pageA.RuleInstances[0];
      expect(rule instanceof RuleInstance).toBeTrue();
      expect(rule.Name).toBe("AND");
      expect(rule.This2RuleTemplate).toBe(component.ruleTemplates[0].ObjId);
      expect(translate.translate).toHaveBeenCalledWith("AND");
      expect(engine.addItem).toHaveBeenCalledOnceWith({ data: rule, pageId: pageA.ObjId });
    });

    it("add_NodeInstance_AddsADataPointAndHandsItToTheTree", async () => {
      const ni = makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean);

      await component.add(ni, pageA);

      expect(pageA.NodeInstances.length).toBe(1);
      const dp = pageA.NodeInstances[0];
      expect(dp.NodeInstance).toBe(ni);
      expect(engine.addItem).toHaveBeenCalledOnceWith({ data: dp, pageId: pageA.ObjId });
      expect(instances.addLogicNodeInstance).toHaveBeenCalledOnceWith(dp);
      // the tree reads its rows again on the instance service's treeChanged (config-tree.component.spec)
      expect(configTree().refreshTree).not.toHaveBeenCalled();
    });

    it("add_ServerFails_ReportsTheError", async () => {
      engine.addItem.and.callFake(() => rejected(new Error("save failed")));

      await component.addRule(component.ruleTemplates[0]);

      expect(notify.notifyError).toHaveBeenCalled();
    });

    it("removeSelectedItem_SelectedRule_IsRemovedFromThePageAndTheServer", async () => {
      const rule = makeRule(translate, "AND", ["I1"], ["O1"]);
      pageA.RuleInstances.push(rule);
      component.selectedItem = rule;
      const event = { item: rule, page: pageA, removed: false };

      await component.removeSelectedItem(event);

      expect(pageA.RuleInstances.length).toBe(0);
      expect(event.removed).toBeTrue();
      expect(engine.removeItem).toHaveBeenCalledOnceWith(rule);
    });

    it("removeSelectedItem_RuleThatIsNotSelected_IsKept", async () => {
      const rule = makeRule(translate, "AND", ["I1"], ["O1"]);
      pageA.RuleInstances.push(rule);
      const event = { item: rule, page: pageA, removed: false };

      await component.removeSelectedItem(event);

      expect(pageA.RuleInstances.length).toBe(1);
      expect(event.removed).toBeFalse();
      expect(engine.removeItem).not.toHaveBeenCalled();
    });

    it("removeSelectedItem_RuleOfAMultiSelection_IsRemovedWithoutBeingTheSelectedItem", async () => {
      const rule = makeRule(translate, "AND", ["I1"], ["O1"]);
      pageA.RuleInstances.push(rule);
      const event = { item: rule, page: pageA, removed: false, multiple: true };

      await component.removeSelectedItem(event);

      expect(pageA.RuleInstances.length).toBe(0);
      expect(event.removed).toBeTrue();
      expect(engine.removeItem).toHaveBeenCalledOnceWith(rule);
    });

    it("removeSelectedItem_DeleteOfTwoBlocksInTheEditor_RemovesBoth", async () => {
      const first = addBlock(pageA);
      const second = addBlock(pageA);
      fixture.detectChanges();
      const editor = ruleEditor();
      editor.selectNodes(editor.nodes);

      await editor.deleteSelection();
      await flushPromises();
      fixture.detectChanges();

      expect(pageA.RuleInstances.length).toBe(0);
      expect(engine.removeItem).toHaveBeenCalledWith(first);
      expect(engine.removeItem).toHaveBeenCalledWith(second);
    });

    it("removeSelectedItem_DataPoint_IsRemovedEverywhere", async () => {
      const dp = makeDataPoint(pageA, makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean));
      pageA.NodeInstances.push(dp);
      const event = { item: dp, page: pageA, removed: false };

      await component.removeSelectedItem(event);

      expect(pageA.NodeInstances.length).toBe(0);
      expect(event.removed).toBeTrue();
      expect(engine.removeItem).toHaveBeenCalledOnceWith(dp);
      expect(instances.removeLogicNodeInstance).toHaveBeenCalledOnceWith(dp);
      // the tree reads its rows again on the instance service's treeChanged (config-tree.component.spec)
      expect(configTree().refreshTree).not.toHaveBeenCalled();
    });

    it("removeSelectedItem_DeleteInTheEditor_RemovesTheRule", async () => {
      const rule = addBlock(pageA);
      const editor = ruleEditor();
      editor.select(editor.nodes[0]);

      await editor.deleteSelection();
      await flushPromises();
      fixture.detectChanges();

      expect(pageA.RuleInstances.length).toBe(0);
      expect(engine.removeItem).toHaveBeenCalledOnceWith(rule);
      expect(fixture.nativeElement.querySelectorAll(".node.rule").length).toBe(0);
    });

    it("delete_SelectedRule_RemovesItAndReinitializesThePage", async () => {
      const rule = addBlock(pageA);
      component.selectedItem = rule;
      const reInit: any[] = [];
      engine.reInit.subscribe((a: any) => reInit.push(a));

      await component.delete();
      fixture.detectChanges();

      expect(pageA.RuleInstances.length).toBe(0);
      expect(reInit).toEqual([pageA]);
      expect(engine.removeItem).toHaveBeenCalledOnceWith(rule);
      expect(fixture.nativeElement.querySelectorAll(".node.rule").length).toBe(0);
    });

    it("delete_SelectedNodeInstance_RemovesItInTheTree", async () => {
      component.selectedItem = makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean);

      await component.delete();

      expect(configTree().removeItem).toHaveBeenCalled();
    });

    it("delete_SelectedDataPoint_RemovesItFromThePage", async () => {
      const dp = makeDataPoint(pageA, makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean));
      dp.RulePage = pageA;
      pageA.NodeInstances.push(dp);
      component.selectedItem = dp;

      await component.delete();
      await flushPromises();

      expect(pageA.NodeInstances.length).toBe(0);
      expect(engine.removeItem).toHaveBeenCalledOnceWith(dp);
      expect(instances.removeLogicNodeInstance).toHaveBeenCalledOnceWith(dp);
    });

    it("delete_SelectedPage_RemovesThePage", async () => {
      const dp = makeDataPoint(pageA, makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean));
      pageA.NodeInstances.push(dp);
      component.selectedItem = pageA;

      await component.delete();

      expect(engine.removePage).toHaveBeenCalledOnceWith(pageA);
      expect(instances.removeLogicNodeInstance).toHaveBeenCalledOnceWith(dp);
      expect(component.pages.map(a => a.Name)).toEqual(["B page", "C page"]);
      expect(component.selectedPage).toBeUndefined();
      expect(component.selectedItem).toBeUndefined();
      // the tree reads its rows again on the instance service's treeChanged (config-tree.component.spec)
      expect(configTree().refreshTree).not.toHaveBeenCalled();
      expect(tabs().map(a => a.textContent.trim())).toEqual(["B page", "C page"]);
      expect(editors().length).toBe(0);
      expect(TestBed.inject(AppService).isLoading).toBeFalse();
    });

    it("removeRulePage_ServerFails_KeepsThePageAndReports", async () => {
      engine.removePage.and.callFake(() => rejected(new Error("in use")));

      await component.removeRulePage();

      expect(component.pages.length).toBe(3);
      expect(component.selectedPage).toBe(pageA);
      expect(notify.notifyError).toHaveBeenCalled();
      expect(TestBed.inject(AppService).isLoading).toBeFalse();
    });

    it("addRulePage_NewPage_IsSavedSortedAndSelected", async () => {
      await component.addRulePage();

      expect(engine.addPage).toHaveBeenCalledTimes(1);
      const page: RulePage = engine.addPage.calls.mostRecent().args[0];
      expect(page.Name).toBe("Page 4");
      expect(page.Description).toBe("Page 4");
      expect(page.ObjId).toBeTruthy();
      expect([pageA.ObjId, pageB.ObjId, pageC.ObjId]).not.toContain(page.ObjId);
      expect(component.pages.map(a => a.Name)).toEqual(["A page", "B page", "C page", "Page 4"]);
      expect(component.selectedPage).toBe(page);
      expect(component.selectedItem).toBe(page);
      expect(tabs().length).toBe(4);
      expect(tabs()[3].classList).toContain("active");
      expect(editors()[0].id).toBe(page.ObjId);
      expect(TestBed.inject(AppService).isLoading).toBeFalse();
    });

    it("addRulePage_ServerFails_ReportsAndReloads", async () => {
      engine.addPage.and.callFake(() => rejected(new Error("save failed")));
      instances.load.calls.reset();

      await component.addRulePage();

      expect(notify.notifyError).toHaveBeenCalled();
      expect(instances.load).toHaveBeenCalledTimes(1);
      expect(component.pages.length).toBe(3);
    });

    it("onLogicNodeInstanceRemoved_DataPointRemovedInTheTree_LeavesThePage", async () => {
      const dp = makeDataPoint(pageA, makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean));
      dp.RulePage = pageA;
      pageA.NodeInstances.push(dp);
      component.selectedItem = dp;
      const removed: any[] = [];
      engine.removed.subscribe((a: any) => removed.push(a));

      configTree().onLogicNodeInstanceRemoved.emit(dp);
      await flushPromises();

      expect(pageA.NodeInstances.length).toBe(0);
      expect(engine.removeItem).toHaveBeenCalledOnceWith(dp);
      expect(instances.removeLogicNodeInstance).toHaveBeenCalledOnceWith(dp);
      expect(removed).toEqual([{ logicNodeInstance: dp, logicPage: pageA }]);
    });

    // BUG: onLogicNodeInstanceRemoved (logic-editor.component.ts:310) filters the page by this.selectedItem instead of the removed item:
    // when something else is selected, the removed data point stays on the canvas (and with nothing selected it throws).
    it("onLogicNodeInstanceRemoved_OtherItemSelected_RemovesTheRemovedDataPoint", async () => {
      const dp = makeDataPoint(pageA, makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean));
      dp.RulePage = pageA;
      pageA.NodeInstances.push(dp);
      component.selectedItem = pageA;

      await component.onLogicNodeInstanceRemoved(dp);

      expect(pageA.NodeInstances.length).toBe(0);
    });

    it("onZoom_EmitOnTheSelectedPage", () => {
      const calls: string[] = [];
      pageA.onZoomIn.subscribe(() => calls.push("in"));
      pageA.onZoomOut.subscribe(() => calls.push("out"));
      pageA.onZoomToView.subscribe(() => calls.push("fit"));

      component.onZoomIn(void 0);
      component.onZoomOut(void 0);
      component.onZoomToView(void 0);

      expect(calls).toEqual(["in", "out", "fit"]);
    });

    it("restart_ReloadsTheEngineAndReports", async () => {
      await component.restart();

      expect(engine.reload).toHaveBeenCalled();
      expect(notify.notifySuccess).toHaveBeenCalledWith("COMMON.RELOAD");
      expect(component.isLoading).toBeFalse();
    });

    it("restart_ReloadFails_StillReports", async () => {
      engine.reload.and.callFake(() => rejected(new Error("restarting")));

      await component.restart();

      expect(notify.notifySuccess).toHaveBeenCalledWith("COMMON.RELOAD");
      expect(component.isLoading).toBeFalse();
    });

    it("showInfo_OpensTheInfoPopup", async () => {
      const show = jasmine.createSpy("show").and.resolveTo(true);
      component.popupLearnMode = <any>{ instance: { show } };

      engine.showInfo.emit(void 0);
      await flushPromises();

      expect(component.infoPopupVisible).toBeTrue();
      expect(show).toHaveBeenCalled();
      component.onPopupHiding(void 0);
      expect(component.infoPopupVisible).toBeFalse();
    });

    it("itemClick_MenuItemWithCommand_RunsIt", () => {
      const command = jasmine.createSpy("command");
      const event = { itemData: { command } };

      component.itemClick(event);
      component.itemClick({ itemData: {} });
      component.itemClick({});

      expect(command).toHaveBeenCalledOnceWith(event);
    });

    it("treeForwarders_CallTheConfigTree", async () => {
      const ni = makeNodeInstance("Device", NodeDataTypeEnum.NoAttribute);
      component.selectedItem = ni;

      component.validate("v");
      component.scan(ni);
      component.onImportData("data");
      component.saveLearnedNodes({ nodeInstance: ni, learnedNodes: [] });
      await component.fileUploaded({ item: ni, file: { name: "a.knxproj" }, password: "made-up-test-password" });
      await component.fileUploaded({ item: ni, fileName: "b.knxproj", file: { name: "ignored" } });

      expect(configTree().validate).toHaveBeenCalledWith("v");
      expect(configTree().scan).toHaveBeenCalledWith(ni);
      expect(configTree().add).toHaveBeenCalledWith("data", ni);
      expect(configTree().saveLearnNodeInstances).toHaveBeenCalledWith(ni, []);
      expect(configTree().fileUploaded).toHaveBeenCalledWith(ni, "a.knxproj", "made-up-test-password");
      expect(configTree().fileUploaded).toHaveBeenCalledWith(ni, "b.knxproj", undefined);
      expect(component.isLoading).toBeFalse();
    });

    it("onImportData_NoNodeInstanceSelected_DoesNothing", () => {
      component.onImportData("data");

      expect(configTree().add).not.toHaveBeenCalled();
    });
  });

  // ---------------------------------------------------------------------------------------------
  describe("layout", () => {
    it("split_WideScreen_IsHorizontal", async () => {
      TestBed.inject(ViewportService).narrow.set(false);
      await init();

      expect(fixture.nativeElement.querySelector("as-split").direction).toBe("horizontal");
    });

    it("split_PhoneWidth_StacksThePanes", async () => {
      await init();
      const viewport = TestBed.inject(ViewportService);

      viewport.narrow.set(true);
      fixture.detectChanges();

      expect(component.viewport).toBe(viewport);
      expect(fixture.nativeElement.querySelector("as-split").direction).toBe("vertical");

      viewport.narrow.set(false);
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector("as-split").direction).toBe("horizontal");
    });

    it("configMenu_ShownOnceTheTemplatesAreLoaded", async () => {
      await init();

      expect(fixture.nativeElement.querySelector("p3-config-menu")).not.toBeNull();
    });
  });
});
