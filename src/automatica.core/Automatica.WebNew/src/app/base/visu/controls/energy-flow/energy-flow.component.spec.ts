import { EventEmitter, NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { L10nTranslationService } from "angular-l10n";

import { EnergyFlowComponent } from "./energy-flow.component";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { NotifyService } from "src/app/services/notify.service";
import { ConfigService } from "src/app/services/config.service";
import { LogicInstanceVisuService } from "src/app/services/logic-visu.service";
import { AppService } from "src/app/services/app.service";
import { RuleInterfaceType } from "src/app/base/model/rule-interface-template";
import { VisuObjectSourceType } from "src/app/base/model/visu-object-instance";
import { VisuItemComponent } from "../visu-item.component";
import { FakeTranslateAsyncPipe, createNotifySpy, createTranslateSpy, flushPromises } from "src/app/testing/test-helpers";

/** output keys of the energy monitor logic (EnergyMonitorLogicFactory), id = "id-" + key */
const OUTPUTS = ["grid", "pv", "battery", "soc", "consumer1", "consumer2", "consumer3", "house", "consumption",
  "selfConsumption", "autarky", "pvToday", "importToday", "exportToday", "chargeToday", "dischargeToday", "consumptionToday",
  "selfConsumptionToday", "autarkyToday"];

describe("EnergyFlowComponent", () => {
  let fixture: ComponentFixture<EnergyFlowComponent>;
  let component: EnergyFlowComponent;
  let dataHub: { ruleInstanceValueChanged: EventEmitter<any>, subscribe: jasmine.Spy, getCurrentValue: jasmine.Spy };
  let visuService: { getRuleInstanceData: jasmine.Spy };
  let parameters: { [key: string]: any };
  let visuData: { [key: string]: any };

  function ruleInstance() {
    const interfaces = OUTPUTS.map(key => ({
      ObjId: `id-${key}`,
      Template: { Key: key, InterfaceType: key === "house" ? RuleInterfaceType.Status : RuleInterfaceType.Output }
    })) as any[];
    for (const [key, value] of Object.entries(parameters)) {
      interfaces.push({ ObjId: `param-${key}`, Template: { Key: key, InterfaceType: RuleInterfaceType.Unknown }, Value: value });
    }
    return { ObjId: "rule-1", Interfaces: interfaces, VisuDisplayName: "Energy", This2AreaInstanceNavigation: { DisplayName: "Technik" } };
  }

  function item() {
    return {
      ObjId: "visu-1",
      objectType: ruleInstance(),
      type: VisuObjectSourceType.RuleInstance,
      isLoading: false,
      getPropertyValue: () => undefined,
      getProperty: () => undefined
    } as any;
  }

  beforeEach(() => {
    parameters = { peakPower: 10, consumer1Name: "", consumer2Name: "", consumer3Name: "" };
    visuData = {};
    dataHub = {
      ruleInstanceValueChanged: new EventEmitter<any>(),
      subscribe: jasmine.createSpy("subscribe").and.resolveTo(undefined),
      getCurrentValue: jasmine.createSpy("getCurrentValue").and.returnValue(undefined)
    };
    visuService = { getRuleInstanceData: jasmine.createSpy("getRuleInstanceData").and.callFake(() => Promise.resolve(visuData)) };

    TestBed.configureTestingModule({
      declarations: [EnergyFlowComponent],
      imports: [FakeTranslateAsyncPipe],
      providers: [
        AppService,
        { provide: DataHubService, useValue: dataHub },
        { provide: NotifyService, useValue: createNotifySpy() },
        { provide: L10nTranslationService, useValue: createTranslateSpy() },
        { provide: ConfigService, useValue: {} },
        { provide: LogicInstanceVisuService, useValue: visuService }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    });
  });

  afterEach(() => fixture?.destroy());

  async function create(editMode = false) {
    fixture = TestBed.createComponent(EnergyFlowComponent);
    component = fixture.componentInstance;
    component.item = item();
    component.editMode = editMode;
    fixture.detectChanges();
    // ngOnInit awaits one subscription per interface before it loads the values
    await new Promise(resolve => setTimeout(resolve));
    await flushPromises();
    fixture.detectChanges();
  }

  function el(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function node(id: string): HTMLElement {
    return el().querySelector(`[data-node="${id}"]`);
  }

  function nodeText(id: string): string {
    return node(id)?.textContent.replace(/\s+/g, " ").trim();
  }

  function flow(kind: string): SVGPathElement {
    return el().querySelector(`.energy-flow.tone-${kind}`);
  }

  function push(key: string, value: any) {
    dataHub.ruleInstanceValueChanged.emit([`id-${key}`, value]);
    fixture.detectChanges();
  }

  it("subscribes the interfaces and loads the current values of the logic", async () => {
    visuData = { "id-pv": 4.2, "id-grid": -1.5, "id-house": 2.7, hasBattery: false };
    await create();

    expect(dataHub.subscribe).toHaveBeenCalledWith("id-house");
    expect(visuService.getRuleInstanceData).toHaveBeenCalledWith("rule-1");
    expect(nodeText("pv")).toContain("4.20kW");
    expect(nodeText("grid")).toContain("1.50kW");
    expect(nodeText("grid")).toContain("UI.VISU.ENERGY_EXPORT");
    expect(nodeText("house")).toContain("2.70kW");
    expect(node("battery")).toBeNull();
    expect(el().querySelector(".tile-name").textContent).toContain("Energy");
    expect(el().querySelector(".tile-room").textContent).toContain("Technik");
  });

  it("matches the interface ids of the logic data case-insensitively", async () => {
    visuData = { "ID-PV": 0.3 };
    await create();

    expect(nodeText("pv")).toContain("300W");
  });

  it("follows live values: grid switches from export to import and the dots turn round", async () => {
    visuData = { "id-grid": -2 };
    await create();
    expect(flow("grid").getAttribute("data-direction")).toBe("out");

    push("grid", 0.8);

    expect(nodeText("grid")).toContain("800W");
    expect(nodeText("grid")).toContain("UI.VISU.ENERGY_IMPORT");
    expect(flow("grid").getAttribute("data-direction")).toBe("in");
  });

  it("draws nothing flowing without power", async () => {
    await create();

    expect(el().querySelectorAll(".energy-flow").length).toBe(0);
    expect(el().querySelectorAll(".energy-track").length).toBe(3);
    expect(nodeText("pv")).toContain("–");
  });

  it("thicker and faster dots for more power", async () => {
    visuData = { "id-pv": 8, "id-house": 0.5 };
    await create();

    const pv = flow("pv");
    const house = flow("house");
    expect(parseFloat(pv.style.strokeWidth)).toBeGreaterThan(parseFloat(house.style.strokeWidth));
    expect(parseFloat(pv.style.animationDuration)).toBeLessThan(parseFloat(house.style.animationDuration));
  });

  it("scales by the peak power parameter", async () => {
    parameters.peakPower = 4;
    visuData = { "id-pv": 4 };
    await create();

    expect(parseFloat(flow("pv").style.strokeWidth)).toBe(6);
  });

  it("shows the battery with direction and state of charge", async () => {
    visuData = { "id-battery": 1.2, "id-soc": 71, hasBattery: true };
    await create();

    expect(nodeText("battery")).toContain("1.20kW");
    expect(nodeText("battery")).toContain("UI.VISU.ENERGY_CHARGE");
    expect(nodeText("battery")).toContain("71 %");
    expect(flow("battery").getAttribute("data-direction")).toBe("out");

    push("battery", -0.5);
    expect(nodeText("battery")).toContain("UI.VISU.ENERGY_DISCHARGE");
    expect(flow("battery").getAttribute("data-direction")).toBe("in");
  });

  it("adds the battery as soon as a battery value comes in", async () => {
    await create();
    expect(node("battery")).toBeNull();

    push("soc", 50);

    expect(node("battery")).not.toBeNull();
  });

  it("shows consumers with their configured name", async () => {
    parameters.consumer1Name = "Wallbox";
    visuData = { "id-consumer1": 7.4, "id-consumer3": 0.9, hasConsumer3: true };
    await create();

    expect(nodeText("consumer1")).toContain("Wallbox");
    expect(nodeText("consumer1")).toContain("7.40kW");
    expect(nodeText("consumer3")).toContain("UI.VISU.ENERGY_CONSUMER");
    expect(node("consumer2")).toBeNull();
  });

  it("shows today's autarky, self-consumption and PV energy", async () => {
    visuData = { "id-autarkyToday": 82.4, "id-selfConsumptionToday": 64.6, "id-pvToday": 12.34 };
    await create();

    const stats = el().querySelector(".energy-stats").textContent.replace(/\s+/g, " ");
    expect(stats).toContain("82 %");
    expect(stats).toContain("65 %");
    expect(stats).toContain("12.3 kWh");
  });

  it("hides the stats row without daily values", async () => {
    await create();

    expect(el().querySelector(".energy-stats")).toBeNull();
  });

  it("animates in the visu but not in the editor", async () => {
    await create();
    expect(el().querySelector(".energy").classList).toContain("is-animated");

    fixture.destroy();
    await create(true);
    expect(el().querySelector(".energy").classList).not.toContain("is-animated");
  });

  it("ignores values of other interfaces", async () => {
    visuData = { "id-pv": 1 };
    await create();

    dataHub.ruleInstanceValueChanged.emit(["someone-else", 99]);
    fixture.detectChanges();

    expect(nodeText("pv")).toContain("1.00kW");
  });

  it("still shows live values when loading the logic data fails", async () => {
    visuService.getRuleInstanceData.and.callFake(() => { const p = Promise.reject(new Error("offline")); p.catch(() => void 0); return p; });
    spyOn(console, "error");
    await create();

    push("pv", 2);

    expect(nodeText("pv")).toContain("2.00kW");
  });

  it("gives screen readers a summary of all nodes", async () => {
    visuData = { "id-pv": 1, "id-house": 1 };
    await create();

    expect(el().querySelector(".sr-only").textContent).toContain("UI.VISU.ENERGY_PV 1.00 kW");
  });
});

describe("VisuItemComponent energy-flow", () => {
  function pick(type: VisuObjectSourceType) {
    TestBed.configureTestingModule({ providers: [AppService, { provide: NotifyService, useValue: createNotifySpy() },
      { provide: L10nTranslationService, useValue: createTranslateSpy() }] });
    const component = TestBed.runInInjectionContext(() => new VisuItemComponent({ nativeElement: document.createElement("div") } as any,
      TestBed.inject(NotifyService), TestBed.inject(L10nTranslationService), TestBed.inject(AppService)));
    component.item = { VisuObjectTemplate: { Key: "energy-flow" }, type } as any;
    component.ngOnInit();
    component.ngOnDestroy();
    return component.component;
  }

  it("uses the energy flow tile for the energy monitor logic", () => {
    expect(pick(VisuObjectSourceType.RuleInstance)).toBe(EnergyFlowComponent);
  });

  it("falls back to the label for anything else", () => {
    expect(pick(VisuObjectSourceType.NodeInstance)).not.toBe(EnergyFlowComponent);
  });
});
