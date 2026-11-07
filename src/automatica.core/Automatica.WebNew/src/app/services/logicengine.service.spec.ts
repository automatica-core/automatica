import { TestBed } from "@angular/core/testing";
import { provideHttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";

import { LogicEngineService, LogicUpdateScope } from "./logicengine.service";
import { DesignTimeDataService } from "./design-time-data.service";
import { NodeDataTypeEnum } from "../base/model/node-data-type";
import { createTranslateSpy, isolateLocalStorage } from "../testing/test-helpers";
import { input, linkRules, makeDataPoint, makeNodeInstance, makePage, makeRule, makeRuleTemplate, output } from "../testing/logic-fixtures";

describe("LogicEngineService", () => {
  let service: LogicEngineService;
  let http: HttpTestingController;
  let design: { getLogicTemplates: jasmine.Spy };
  let translate: jasmine.SpyObj<L10nTranslationService>;
  let restore: () => void;

  beforeEach(() => {
    restore = isolateLocalStorage("s1server");
    translate = createTranslateSpy();
    design = { getLogicTemplates: jasmine.createSpy("getLogicTemplates").and.resolveTo([makeRuleTemplate("AND")]) };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        LogicEngineService,
        { provide: DesignTimeDataService, useValue: design },
        { provide: Router, useValue: {} },
        { provide: L10nTranslationService, useValue: translate }
      ]
    });
    service = TestBed.inject(LogicEngineService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    restore();
  });

  it("getRuleTemplates_ComeFromTheDesignTimeData", async () => {
    const templates = await service.getRuleTemplates();

    expect(design.getLogicTemplates).toHaveBeenCalled();
    expect(templates[0].Name).toBe("AND");
  });

  it("addItem_Rule_EmitsAddAndPostsTheLogicInstance", async () => {
    const page = makePage();
    const rule = makeRule(translate, "AND", ["I1"], ["O1"]);
    const added: any[] = [];
    service.add.subscribe(a => added.push(a));

    const done = service.addItem({ data: rule, pageId: page.ObjId });
    // the editor draws the block right away, before the server answered
    expect(added).toEqual([{ data: rule, pageId: page.ObjId }]);
    const req = http.expectOne(`/webapi/logics/item/logicInstance/${page.ObjId}`);
    expect(req.request.method).toBe("POST");
    expect(req.request.body.ObjId).toBe(rule.ObjId);
    req.flush(null);
    await done;
  });

  it("addItem_DataPoint_PostsTheNodeInstance", async () => {
    const page = makePage();
    const dp = makeDataPoint(page, makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean));

    const done = service.addItem({ data: dp, pageId: page.ObjId });
    const req = http.expectOne(`/webapi/logics/item/nodeInstance/${page.ObjId}`);
    expect(req.request.body.ObjId).toBe(dp.ObjId);
    expect(req.request.body.This2NodeInstance).toBe(dp.This2NodeInstance);
    req.flush(null);
    await done;
  });

  it("removeItem_DeletesByKind", async () => {
    const rule = makeRule(translate, "AND");
    const dp = makeDataPoint(makePage(), makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean));

    const a = service.removeItem(rule);
    const b = service.removeItem(dp);
    const ra = http.expectOne(`/webapi/logics/item/logicInstance/${rule.ObjId}`);
    const rb = http.expectOne(`/webapi/logics/item/nodeInstance/${dp.ObjId}`);
    expect(ra.request.method).toBe("DELETE");
    expect(rb.request.method).toBe("DELETE");
    ra.flush(null);
    rb.flush(null);
    await Promise.all([a, b]);
  });

  it("updateItem_PatchesWithTheScope", async () => {
    const rule = makeRule(translate, "AND");
    const dp = makeDataPoint(makePage(), makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean));

    const a = service.updateItem(rule, LogicUpdateScope.Drag);
    const b = service.updateItem(dp, LogicUpdateScope.InvertedValueUpdated);
    const c = service.updateItem(rule);
    const ra = http.expectOne("/webapi/logics/item/logicInstance?updateScope=1");
    const rb = http.expectOne("/webapi/logics/item/nodeInstance?updateScope=2");
    const rc = http.expectOne("/webapi/logics/item/logicInstance?updateScope=0");
    expect(ra.request.method).toBe("PATCH");
    expect(rb.request.method).toBe("PATCH");
    ra.flush(null);
    rb.flush(null);
    rc.flush(null);
    await Promise.all([a, b, c]);
  });

  it("links_ArePostedAndDeleted", async () => {
    const page = makePage();
    const a = makeRule(translate, "A", ["I1"], ["O1"]);
    const b = makeRule(translate, "B", ["I1"], ["O1"]);
    const link = linkRules(page, output(a), input(b));

    const add = service.addOrUpdateLink(link);
    const req = http.expectOne("/webapi/logics/link");
    expect(req.request.method).toBe("POST");
    expect(req.request.body.This2RuleInterfaceInstanceOutput).toBe(output(a).ObjId);
    expect(req.request.body.This2RuleInterfaceInstanceInput).toBe(input(b).ObjId);
    req.flush(null);
    await add;

    const remove = service.removeLink(link);
    const del = http.expectOne(`/webapi/logics/link/${link.ObjId}`);
    expect(del.request.method).toBe("DELETE");
    del.flush(null);
    await remove;
  });

  it("pages_AddRemoveAndReload", async () => {
    const page = makePage("Lights");

    const add = service.addPage(page);
    const addReq = http.expectOne("/webapi/logics/page/add");
    expect(addReq.request.method).toBe("POST");
    expect(addReq.request.body.Name).toBe("Lights");
    addReq.flush(null);
    await add;

    const remove = service.removePage(page);
    const removeReq = http.expectOne(`/webapi/logics/page/${page.ObjId}`);
    expect(removeReq.request.method).toBe("DELETE");
    removeReq.flush(null);
    await remove;

    const reload = service.reload();
    const reloadReq = http.expectOne("/webapi/logics/reload");
    expect(reloadReq.request.method).toBe("POST");
    reloadReq.flush(null);
    await reload;
  });

  it("serverError_IsRejectedAsWebApiException", async () => {
    const done = service.removePage(makePage());
    http.expectOne(() => true).flush({ TypeInfo: "WebApiException", errorText: "PAGE_IN_USE", severity: 2 }, { status: 409, statusText: "Conflict" });

    await expectAsync(done).toBeRejectedWith(jasmine.objectContaining({ ErrorText: "PAGE_IN_USE", status: 409 }));
  });
});
