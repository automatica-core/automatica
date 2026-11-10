import { EventEmitter, NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { L10nTranslationService } from "angular-l10n";

import { DETAILS_REFRESH_MS, EnergyFlowComponent } from "./energy-flow.component";
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
const OUTPUTS = ["grid", "pv", "battery", "soc", "consumer1", "consumer2", "consumer3", "consumer4", "consumer5", "consumer6", "house", "consumption",
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
    parameters = { peakPower: 10, consumer1Name: "", consumer2Name: "", consumer3Name: "", consumer4Name: "", consumer5Name: "", consumer6Name: "" };
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

  it("shows all six consumers with their names and grows the stage for the bottom row", async () => {
    parameters.consumer4Name = "EG";
    parameters.consumer6Name = "Sauna";
    visuData = { "id-consumer1": 7.4, hasConsumer2: true, hasConsumer3: true, "id-consumer4": 1.25, hasConsumer5: true, "id-consumer6": 0.3 };
    await create();

    for (const i of [1, 2, 3, 4, 5, 6]) {
      expect(node(`consumer${i}`)).withContext(`consumer${i}`).not.toBeNull();
    }
    expect(nodeText("consumer4")).toContain("EG");
    expect(nodeText("consumer4")).toContain("1.25kW");
    expect(nodeText("consumer6")).toContain("Sauna");
    expect(nodeText("consumer6")).toContain("300W");
    expect(nodeText("consumer5")).toContain("UI.VISU.ENERGY_CONSUMER");
    expect(el().querySelector(".energy-lines").getAttribute("viewBox")).toBe("0 0 320 300");
    expect((el().querySelector(".energy-stage") as HTMLElement).style.aspectRatio.replace(/\s/g, "")).toBe("320/300");
    // node positions follow the taller view box: the bottom row sits at 270 / 300
    expect(parseFloat(node("consumer5").style.top)).toBeCloseTo(90, 5);
  });

  it("keeps the compact stage with up to four consumers", async () => {
    visuData = { hasConsumer1: true, hasConsumer4: true };
    await create();

    expect(node("consumer4")).not.toBeNull();
    expect(el().querySelector(".energy-lines").getAttribute("viewBox")).toBe("0 0 320 220");
  });

  it("follows live values of consumer 6", async () => {
    await create();
    expect(node("consumer6")).toBeNull();

    push("consumer6", 2);

    expect(nodeText("consumer6")).toContain("2.00kW");
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

  describe("circuit details", () => {
    const circuitGroups = () => [
      { group: 1, total: 3, measured: 2.6, rest: 0.4, restToday: 1.1,
        circuits: [{ circuit: 3, name: "Compressor", hasValue: true, power: 2.6, today: 7.25 }] },
      { group: 0, total: 1.2, measured: 0.9, rest: 0.3, restToday: 0.8,
        circuits: [
          { circuit: 1, name: "Kitchen", hasValue: true, power: 0.6, today: 2 },
          { circuit: 2, name: null, hasValue: true, power: 0.3, today: 0.5 },
          { circuit: 7, name: "Garage", hasValue: false, power: 0, today: 0 }
        ] }
    ];

    function rows(group: Element): string[] {
      return Array.from(group.querySelectorAll(".energy-row")).map(r => r.textContent.replace(/\s+/g, " ").trim());
    }

    it("offers no details without circuits", async () => {
      visuData = { "id-house": 1 };
      await create();

      expect(component.hasDetails).toBeFalse();
      expect(el().querySelector(".energy-more")).toBeNull();
      expect(el().querySelectorAll(".energy-group").length).toBe(0);
    });

    it("lists the house and the consumers with their circuits and the rest", async () => {
      parameters.consumer1Name = "Heat pump";
      visuData = { "id-house": 1.2, "id-consumer1": 3, circuitGroups: circuitGroups() };
      await create();

      expect(el().querySelector(".energy-more")).not.toBeNull();
      const groups = el().querySelectorAll(".energy-group");
      expect(groups.length).toBe(2);
      expect(groups[0].querySelector(".energy-group-name").textContent).toContain("UI.VISU.ENERGY_HOUSE");
      expect(groups[0].querySelector(".energy-group-value").textContent).toContain("1.20 kW");
      expect(groups[1].querySelector(".energy-group-name").textContent).toContain("Heat pump");

      const house = rows(groups[0]);
      expect(house[0]).toContain("Kitchen");
      expect(house[0]).toContain("600 W");
      expect(house[0]).toContain("2.00 kWh");
      expect(house[1]).toContain("UI.VISU.ENERGY_CIRCUIT");
      expect(house[2]).toContain("Garage");
      expect(house[2]).toContain("–");
      expect(house[3]).toContain("UI.VISU.ENERGY_NOT_MEASURED");
      expect(house[3]).toContain("300 W");
      expect(groups[0].querySelectorAll(".energy-row.is-rest").length).toBe(1);
      expect(groups[0].querySelectorAll(".energy-row.no-value").length).toBe(1);
    });

    describe("circuits below circuits", () => {
      const nested = () => [{ group: 0, total: 2, measured: 1, rest: 1, circuits: [
        { circuit: 1, name: "Kitchen", hasValue: true, power: 1, measured: 0.6, rest: 0.4, restToday: 0.2, circuits: [
          { circuit: 2, name: "Oven", hasValue: true, power: 0.6, today: 1, measured: 0.5, rest: 0.1, circuits: [
            { circuit: 3, name: "Grill", hasValue: true, power: 0.5, circuits: [] }
          ] }
        ] }
      ] }];

      const items = () => Array.from(el().querySelectorAll<HTMLElement>(".energy-group .energy-row"));
      const names = () => items().map(r => r.querySelector(".energy-row-name").textContent.trim());
      const toggle = (name: string) => items().find(r => r.textContent.includes(name)).querySelector<HTMLButtonElement>(".energy-row-toggle");
      const click = async (name: string) => {
        toggle(name).click();
        fixture.detectChanges();
        await fixture.whenStable();
      };

      beforeEach(async () => {
        visuData = { circuitGroups: nested() };
        await create();
      });

      it("keeps the sub-circuits closed at first", () => {
        expect(names()).toEqual(["Kitchen", "UI.VISU.ENERGY_NOT_MEASURED"]);
        expect(toggle("Kitchen").getAttribute("aria-expanded")).toBe("false");
        // a circuit without sub-circuits has no toggle
        expect(el().querySelectorAll(".energy-row-toggle").length).toBe(1);
      });

      it("opens a circuit with its sub-circuits and its rest, indented", async () => {
        await click("Kitchen");

        expect(names()).toEqual(["Kitchen", "Oven", "UI.VISU.ENERGY_NOT_MEASURED", "UI.VISU.ENERGY_NOT_MEASURED"]);
        expect(toggle("Kitchen").getAttribute("aria-expanded")).toBe("true");
        const [kitchen, oven, kitchenRest, houseRest] = items();
        expect(kitchen.classList).toContain("is-open");
        expect(kitchen.classList).not.toContain("is-nested");
        expect(oven.classList).toContain("is-nested");
        expect(oven.style.getPropertyValue("--depth")).toBe("1");
        // the oven has a sub-circuit too, but it stays closed
        expect(oven.classList).not.toContain("is-open");
        expect(kitchenRest.textContent).toContain("400 W");
        expect(houseRest.classList).not.toContain("is-nested");
        expect(houseRest.textContent).toContain("1.00 kW");
      });

      it("opens the next level on its own and closes all below with its parent", async () => {
        await click("Kitchen");
        await click("Oven");
        expect(names()).toEqual(["Kitchen", "Oven", "Grill", "UI.VISU.ENERGY_NOT_MEASURED", "UI.VISU.ENERGY_NOT_MEASURED",
          "UI.VISU.ENERGY_NOT_MEASURED"]);

        await click("Kitchen");
        expect(names()).toEqual(["Kitchen", "UI.VISU.ENERGY_NOT_MEASURED"]);

        // opening the kitchen again shows the oven as it was left: open
        await click("Kitchen");
        expect(names()).toContain("Grill");
      });

      it("keeps what is open when the popup reloads", async () => {
        await click("Kitchen");

        component.applyVisuData({ circuitGroups: nested() });
        fixture.detectChanges();

        expect(names()).toContain("Oven");
      });

      it("does not close the popup or open the diagram when toggling", async () => {
        const openPopup = jasmine.createSpy("openPopup");
        component.baseControl = { openPopup } as any;
        const event = new MouseEvent("click", { bubbles: true });
        spyOn(event, "stopPropagation").and.callThrough();

        toggle("Kitchen").dispatchEvent(event);

        expect(event.stopPropagation).toHaveBeenCalled();
        expect(openPopup).not.toHaveBeenCalled();
      });
    });

    it("opens the popup from the button and from the diagram", async () => {
      visuData = { circuitGroups: circuitGroups() };
      await create();
      const openPopup = jasmine.createSpy("openPopup");
      component.baseControl = { openPopup } as any;

      (el().querySelector(".energy-more") as HTMLElement).click();
      (el().querySelector(".energy-stage") as HTMLElement).click();

      expect(openPopup).toHaveBeenCalledTimes(2);
    });

    it("does not open the details in the visu editor", async () => {
      visuData = { circuitGroups: circuitGroups() };
      await create(true);
      const openPopup = jasmine.createSpy("openPopup");
      component.baseControl = { openPopup } as any;

      expect(el().querySelector(".energy-more")).toBeNull();
      (el().querySelector(".energy-stage") as HTMLElement).click();

      expect(openPopup).not.toHaveBeenCalled();
    });

    it("reloads the circuits while the popup is open", async () => {
      visuData = { circuitGroups: circuitGroups() };
      await create();
      jasmine.clock().install();
      try {
        const calls = visuService.getRuleInstanceData.calls.count();

        component.onDetailsShowing();
        expect(visuService.getRuleInstanceData.calls.count()).toBe(calls + 1);
        jasmine.clock().tick(DETAILS_REFRESH_MS * 2);
        expect(visuService.getRuleInstanceData.calls.count()).toBe(calls + 3);

        component.onDetailsHiding();
        jasmine.clock().tick(DETAILS_REFRESH_MS * 3);
        expect(visuService.getRuleInstanceData.calls.count()).toBe(calls + 3);
      } finally {
        jasmine.clock().uninstall();
      }
    });

    it("updates the rows from the reloaded data", async () => {
      visuData = { circuitGroups: circuitGroups() };
      await create();

      visuData = { circuitGroups: [{ group: 0, total: 2, rest: 0.5, circuits: [{ circuit: 1, name: "Kitchen", hasValue: true, power: 1.5 }] }] };
      component.onDetailsShowing();
      await flushPromises();
      component.onDetailsHiding();
      fixture.detectChanges();

      const groups = el().querySelectorAll(".energy-group");
      expect(groups.length).toBe(1);
      expect(rows(groups[0])[0]).toContain("1.50 kW");
    });
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
