import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { L10nTranslationService } from "angular-l10n";
import { DxContextMenuModule, DxPopoverModule, DxTooltipModule, DxTreeListModule } from "devextreme-angular";
import { DndModule } from "p3root-angular-dnd";

import { ConfigTreeComponent, formatTreeValue } from "./config-tree.component";
import { ConfigService } from "src/app/services/config.service";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { LogicEditorInstanceService } from "src/app/services/logic-editor-instance.service";
import { NodeInstance } from "src/app/base/model/node-instance";
import { NodeInstance2RulePage } from "src/app/base/model/node-instance-2-rule-page";
import { NodeDataTypeEnum } from "src/app/base/model/node-data-type";
import { InterfaceTypeEnum } from "src/app/base/model/interface-type";
import { RulePage } from "src/app/base/model/rule-page";
import {
  FakeTranslateAsyncPipe, FakeTranslatePipe, createNotifySpy, createTranslateSpy, emitter, flushPromises, isolateLocalStorage, nextFrames
} from "src/app/testing/test-helpers";
import { makeDataPoint, makeNodeInstance, makePage } from "src/app/testing/logic-fixtures";

/**
 * The real DevExtreme tree list on top of the real LogicEditorInstanceService: the logic editor changes the rows through the
 * service (data points placed on or removed from a page, nodes deleted), the tree has to show that without a reload.
 */
describe("ConfigTreeComponent rows", () => {
  let fixture: ComponentFixture<ConfigTreeComponent>;
  let component: ConfigTreeComponent;
  let service: LogicEditorInstanceService;
  let config: any;
  let restoreStorage: () => void;
  let root: NodeInstance;
  let lamp: NodeInstance;
  let fan: NodeInstance;
  let pageA: RulePage;
  let pageB: RulePage;
  let lampOnA: NodeInstance2RulePage;

  function child(name: string, parent: NodeInstance) {
    const node = makeNodeInstance(name, NodeDataTypeEnum.Boolean, { parent });
    node.This2ParentNodeInstance = parent.ObjId;
    return node;
  }

  beforeEach(async () => {
    restoreStorage = isolateLocalStorage("automaticaConfigTree");

    root = makeNodeInstance("Root", NodeDataTypeEnum.NoAttribute, { interfaceType: InterfaceTypeEnum.Ethernet });
    fan = child("Fan", root);
    lamp = child("Lamp", root);
    pageA = makePage("A page");
    pageB = makePage("B page");
    // the server sends the tree's data points apart from the pages (other objects than the page's NodeInstances)
    lampOnA = makeDataPoint(pageA, lamp);

    config = {
      getNodeInstances: jasmine.createSpy("getNodeInstances").and.resolveTo([root]),
      getLogicNodeInstances: jasmine.createSpy("getLogicNodeInstances").and.resolveTo([lampOnA]),
      delete: jasmine.createSpy("delete").and.resolveTo(undefined)
    };
    const engine: any = { getPages: () => Promise.resolve([pageA, pageB]) };
    const hub: any = { subscribeForAll: () => Promise.resolve(), dispatchValue: emitter() };
    service = new LogicEditorInstanceService(<any>{}, config, <any>{ getSettings: () => Promise.resolve([]) }, hub,
      <any>{}, <any>{ getCurrentNodeValues: () => Promise.resolve({}) }, engine);
    await service.load();

    TestBed.configureTestingModule({
      declarations: [ConfigTreeComponent],
      imports: [DxTreeListModule, DxContextMenuModule, DxTooltipModule, DxPopoverModule, DndModule.forRoot(),
        FakeTranslateAsyncPipe, FakeTranslatePipe],
      providers: [
        { provide: ConfigService, useValue: config },
        { provide: LogicEditorInstanceService, useValue: service },
        { provide: L10nTranslationService, useValue: createTranslateSpy() },
        { provide: NotifyService, useValue: createNotifySpy() },
        { provide: DataHubService, useValue: hub },
        AppService
      ],
      schemas: [NO_ERRORS_SCHEMA]
    });

    fixture = TestBed.createComponent(ConfigTreeComponent);
    component = fixture.componentInstance;
    component.loaded = true;
    component.expandedRowKeys = [root.Id, lamp.Id, fan.Id];
    fixture.nativeElement.style.display = "block";
    fixture.nativeElement.style.height = "600px";
    fixture.detectChanges();
    await settle();
  });

  afterEach(() => {
    fixture.destroy();
    restoreStorage();
  });

  /** the tree list loads and renders asynchronously */
  async function settle() {
    for (let i = 0; i < 3; i++) {
      await flushPromises(10);
      await new Promise(resolve => setTimeout(resolve, 20));
      await nextFrames();
      fixture.detectChanges();
    }
  }

  function rows(): string[] {
    return Array.from(fixture.nativeElement.querySelectorAll(".dx-data-row .tree-name"))
      .map((a: HTMLElement) => a.textContent.trim());
  }

  function parentKey(id: string) {
    return component.tree.instance.getNodeByKey(id)?.parent?.key;
  }

  it("rows_LoadedDataPoint_ShowsItsPageUnderTheNode", () => {
    expect(rows()).toEqual(["Root", "Fan", "Lamp", "A page"]);
    expect(parentKey(lampOnA.Id)).toBe(lamp.Id);
  });

  it("addLogicNodeInstance_DataPointPlacedOnAPage_ShowsTheRowRightAway", async () => {
    // what the logic editor does once the server saved a data point dropped onto page B
    const fanOnB = NodeInstance2RulePage.createFromNodeInstance(fan, pageB);
    service.addLogicNodeInstance(fanOnB);
    await settle();

    expect(rows()).toEqual(["Root", "Fan", "B page", "Lamp", "A page"]);
    expect(parentKey(fanOnB.Id)).toBe(fan.Id);
  });

  it("removeLogicNodeInstance_DataPointRemovedFromThePage_RemovesTheRowRightAway", async () => {
    service.removeLogicNodeInstance(lampOnA);
    await settle();

    expect(rows()).toEqual(["Root", "Fan", "Lamp"]);
  });

  it("addAndRemove_SeveralChangesAtOnce_EndInOneRefresh", async () => {
    const refresh = spyOn(component.tree.instance, "refresh").and.callThrough();
    const fanOnA = NodeInstance2RulePage.createFromNodeInstance(fan, pageA);
    const fanOnB = NodeInstance2RulePage.createFromNodeInstance(fan, pageB);

    service.addLogicNodeInstance(fanOnA);
    service.addLogicNodeInstance(fanOnB);
    service.removeLogicNodeInstance(lampOnA);
    await settle();

    expect(refresh).toHaveBeenCalledTimes(1);
    expect(rows()).toEqual(["Root", "Fan", "A page", "B page", "Lamp"]);
  });

  it("pageRenamed_DataPointPlacedInThisSession_ShowsTheNewName", async () => {
    const fanOnB = NodeInstance2RulePage.createFromNodeInstance(fan, pageB);
    service.addLogicNodeInstance(fanOnB);
    await settle();

    pageB.Name = "Ventilation";
    component.refreshTree();
    await settle();

    expect(rows()).toEqual(["Root", "Fan", "Ventilation", "Lamp", "A page"]);
  });

  it("deleteItem_Node_RemovesItsRowAndItsPageRows", async () => {
    await component.deleteItem(lamp);
    await settle();

    expect(config.delete).toHaveBeenCalledOnceWith(lamp);
    expect(rows()).toEqual(["Root", "Fan"]);
    expect(service.getLogicNodeInstance(lampOnA.Id)).toBeUndefined();
    expect(service.hasNodeInstance(lamp.Id)).toBeFalse();
  });

  it("destroyed_ChangesAfterwards_DoNotTouchTheTree", async () => {
    const refresh = spyOn(component.tree.instance, "refresh");
    fixture.destroy();

    service.removeLogicNodeInstance(lampOnA);
    await flushPromises();

    expect(refresh).not.toHaveBeenCalled();
  });
});

describe("config tree value", () => {
  it("formatTreeValue_FormatsByType", () => {
    expect(formatTreeValue(undefined)).toBe("");
    expect(formatTreeValue(null)).toBe("");
    expect(formatTreeValue("")).toBe("");
    expect(formatTreeValue(0)).toBe("0");
    expect(formatTreeValue(22.6)).toBe("22.6");
    expect(formatTreeValue(75937.51123)).toBe("75937.51");
    expect(formatTreeValue(true)).toBe("true");
    expect(formatTreeValue("unlocked")).toBe("unlocked");
    expect(formatTreeValue({ a: 1 })).toBe("{…}");
  });
});
