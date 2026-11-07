import { TestBed } from "@angular/core/testing";
import { provideHttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";

import { ConfigService } from "./config.service";
import { DesignTimeDataService } from "./design-time-data.service";
import { SatelliteService } from "./satellite.services";
import { Satellite } from "../base/model/satellites/satellite";
import { NodeDataTypeEnum } from "../base/model/node-data-type";
import { createTranslateSpy, isolateLocalStorage } from "../testing/test-helpers";
import { makeNodeInstance } from "../testing/logic-fixtures";

describe("ConfigService", () => {
  let service: ConfigService;
  let http: HttpTestingController;
  let restore: () => void;

  beforeEach(() => {
    restore = isolateLocalStorage("s1server");
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ConfigService,
        { provide: DesignTimeDataService, useValue: {} },
        { provide: Router, useValue: {} },
        { provide: L10nTranslationService, useValue: createTranslateSpy() }
      ]
    });
    service = TestBed.inject(ConfigService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    restore();
  });

  const gets: [string, (s: ConfigService) => Promise<any>, string, any][] = [
    ["getNodeInstances", s => s.getNodeInstances(), "/webapi/nodeInstances", []],
    ["getLinkableNodes", s => s.getLinkableNodes(), "/webapi/nodeInstances/linkable", []],
    ["getNodeInstancesLinkedToArea", s => s.getNodeInstancesLinkedToArea("area-1"), "/webapi/nodeInstances/areaLinked/area-1", []],
    ["getSingleNodeInstance", s => s.getSingleNodeInstance("node-1"), "/webapi/nodeInstances/single/node-1", null],
    ["getServerState", s => s.getServerState(), "/webapi/server/state", { state: 1 }],
    ["getVersion", s => s.getVersion(), "/webapi/version", { version: "1.2.3" }],
    ["getLogicNodeInstances", s => s.getLogicNodeInstances(), "/webapi/nodeInstancesV2/logicNodeInstances", []]
  ];

  for (const [name, call, url, answer] of gets) {
    it(`${name}_GetsItsUrl`, async () => {
      const done = call(service);
      const req = http.expectOne(url);
      expect(req.request.method).toBe("GET");
      req.flush(answer);
      await done;
    });
  }

  it("getVersion_ReturnsTheJson", async () => {
    const done = service.getVersion();
    http.expectOne("/webapi/version").flush({ version: "1.2.3" });

    expect(await done).toEqual({ version: "1.2.3" });
  });

  it("getLinkableNodes_NotAnArray_Fails", async () => {
    spyOn(console, "error");
    const done = service.getLinkableNodes();
    http.expectOne("/webapi/nodeInstances/linkable").flush({ TypeInfo: "NodeInstance" });

    await expectAsync(done).toBeRejectedWithError("array expected at response");
  });

  it("writes_UseTheirVerbsAndUrls", async () => {
    const node = makeNodeInstance("Lamp", NodeDataTypeEnum.Boolean);
    const target = makeNodeInstance("Folder", NodeDataTypeEnum.NoAttribute);

    const calls: [Promise<any>, string, string][] = [
      [service.add(node), "PUT", "/webapi/nodeInstancesV2/add"],
      [service.update(node, <any>3), "POST", "/webapi/nodeInstancesV2/update?updateScope=3"],
      [service.delete(node), "POST", "/webapi/nodeInstancesV2/delete"],
      [service.reload(), "POST", "/webapi/nodeInstancesV2/reload"],
      [service.scan(node), "POST", "/webapi/nodeInstancesV2/scan"],
      [service.read(node), "POST", "/webapi/nodeInstances/read"],
      [service.customAction(node, "learn"), "POST", "/webapi/nodeInstances/customAction/learn"],
      [service.copy(node, target), "POST", `/webapi/nodeInstancesV2/copy/${node.ObjId}/${target.ObjId}`],
      [service.reInitServer(), "POST", "/webapi/server"]
    ];

    for (const [, method, url] of calls) {
      const req = http.expectOne(r => r.url === url);
      expect(req.request.method).withContext(url).toBe(method);
      req.flush(method === "POST" && url.endsWith("scan") ? [] : null);
    }
    await Promise.all(calls.map(a => a[0]));
  });

  it("import_SendsTheNodeTheFileAndThePassword", async () => {
    const node = makeNodeInstance("KNX", NodeDataTypeEnum.NoAttribute);

    const done = service.import(node, "project.knxproj", "made-up-test-password");
    const req = http.expectOne("/webapi/nodeInstancesV2/import/");
    expect(req.request.method).toBe("POST");
    expect(req.request.body.FileName).toBe("project.knxproj");
    expect(req.request.body.Password).toBe("made-up-test-password");
    expect(req.request.body.Node.ObjId).toBe(node.ObjId);
    req.flush([]);
    await done;
  });

  it("createFromTemplate_UsesTheCurrentLanguage", async () => {
    const parent = makeNodeInstance("Root", NodeDataTypeEnum.NoAttribute);

    const done = service.createFromTemplate(parent, parent.NodeTemplate);
    const req = http.expectOne(`/webapi/nodeInstancesV2/create/en/${parent.ObjId}/${parent.NodeTemplate.ObjId}`);
    expect(req.request.method).toBe("POST");
    req.flush(null);
    await done;
  });
});

describe("SatelliteService", () => {
  let service: SatelliteService;
  let http: HttpTestingController;
  let restore: () => void;

  beforeEach(() => {
    restore = isolateLocalStorage("s1server");
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        SatelliteService,
        { provide: DesignTimeDataService, useValue: {} },
        { provide: Router, useValue: {} },
        { provide: L10nTranslationService, useValue: createTranslateSpy() }
      ]
    });
    service = TestBed.inject(SatelliteService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    restore();
  });

  it("getAll_GetsTheSatellites", async () => {
    const done = service.getAll();
    const req = http.expectOne("/webapi/satellite");
    expect(req.request.method).toBe("GET");
    req.flush([{ TypeInfo: "Satellite", ObjId: "s1", Name: "Garage", Description: "", ClientId: "gw", ClientKey: "", Connected: true }]);

    const list = await done;
    expect(list.length).toBe(1);
    expect(list[0] instanceof Satellite).toBeTrue();
    expect(list[0].Connected).toBeTrue();
    // name, description, client id and key are editable in the inspector
    expect(list[0].Properties.length).toBe(4);
  });

  it("save_PostsTheJsonOfEverySatellite", async () => {
    const a = new Satellite();
    a.ObjId = "s1";
    a.Name = "Garage";
    a.ClientId = "gw";
    a.ClientKey = "made-up-test-key-0123456789";
    const b = new Satellite();
    b.ObjId = "s2";
    b.Name = "Attic";

    const done = service.save([a, b]);
    const req = http.expectOne("/webapi/satellite");
    expect(req.request.method).toBe("POST");
    expect(req.request.body.map((x: any) => x.ObjId)).toEqual(["s1", "s2"]);
    expect(req.request.body[0].ClientKey).toBe("made-up-test-key-0123456789");
    expect(req.request.body[1].Name).toBe("Attic");
    req.flush([]);
    await done;
  });

  it("save_KeyTooShort_RejectsWithTheWarning", async () => {
    const done = service.save([new Satellite()]);
    http.expectOne("/webapi/satellite").flush({ TypeInfo: "WebApiException", errorText: "SATELLITE_KEY_TOO_SHORT", severity: 1 },
      { status: 400, statusText: "Bad Request" });

    await expectAsync(done).toBeRejectedWith(jasmine.objectContaining({ ErrorText: "SATELLITE_KEY_TOO_SHORT", Severity: 1, status: 400 }));
  });
});
