import {
  ACTIVE_THRESHOLD, buildEnergyFlow, flowDirection, flowDuration, formatNumber, formatPower, HUB, linkPath, load, strokeWidth,
  EnergyFlowOptions, EnergyValues
} from "./energy-flow-model";

describe("energy flow model", () => {

  const options = (extra: Partial<EnergyFlowOptions> = {}): EnergyFlowOptions => ({
    peakPower: 10,
    hasBattery: false,
    consumers: [{ present: false }, { present: false }, { present: false }],
    ...extra
  });

  const values = (extra: Partial<EnergyValues> = {}): EnergyValues => ({ consumers: [], ...extra });

  describe("flowDirection", () => {
    it("PV flows into the hub, never out", () => {
      expect(flowDirection("pv", 3)).toBe("in");
      expect(flowDirection("pv", -0.5)).toBe("none");
    });

    it("grid import flows in, export flows out", () => {
      expect(flowDirection("grid", 1.2)).toBe("in");
      expect(flowDirection("grid", -1.2)).toBe("out");
    });

    it("battery charging takes from the hub, discharging gives to it", () => {
      expect(flowDirection("battery", 2)).toBe("out");
      expect(flowDirection("battery", -2)).toBe("in");
    });

    it("house and consumers only take", () => {
      expect(flowDirection("house", 0.4)).toBe("out");
      expect(flowDirection("consumer", 7)).toBe("out");
      expect(flowDirection("consumer", -1)).toBe("none");
    });

    it("ignores jitter below the threshold and invalid values", () => {
      expect(flowDirection("grid", ACTIVE_THRESHOLD / 2)).toBe("none");
      expect(flowDirection("grid", -ACTIVE_THRESHOLD / 2)).toBe("none");
      expect(flowDirection("pv", NaN)).toBe("none");
      expect(flowDirection("pv", undefined)).toBe("none");
    });
  });

  describe("scaling", () => {
    it("load is the share of the peak power, capped at 1, absolute", () => {
      expect(load(5, 10)).toBe(0.5);
      expect(load(-5, 10)).toBe(0.5);
      expect(load(25, 10)).toBe(1);
      expect(load(0, 10)).toBe(0);
    });

    it("falls back to 10 kW without a usable peak power", () => {
      expect(load(5, 0)).toBe(0.5);
      expect(load(5, -3)).toBe(0.5);
      expect(load(5, NaN)).toBe(0.5);
    });

    it("lines get thicker with more power and stop growing at the peak", () => {
      expect(strokeWidth(0.1, 10)).toBeLessThan(strokeWidth(2, 10));
      expect(strokeWidth(2, 10)).toBeLessThan(strokeWidth(8, 10));
      expect(strokeWidth(10, 10)).toBe(strokeWidth(50, 10));
      expect(strokeWidth(0, 10)).toBe(1.5);
      expect(strokeWidth(10, 10)).toBe(6);
    });

    it("dots get faster (shorter duration) with more power", () => {
      expect(flowDuration(0.1, 10)).toBeGreaterThan(flowDuration(5, 10));
      expect(flowDuration(5, 10)).toBeGreaterThan(flowDuration(10, 10));
      expect(flowDuration(10, 10)).toBe(flowDuration(30, 10));
      expect(flowDuration(10, 10)).toBe(0.25);
      expect(flowDuration(0, 10)).toBe(1.75);
    });
  });

  describe("linkPath", () => {
    it("draws axis nodes straight, in flow direction", () => {
      expect(linkPath({ x: 160, y: 30 }, "in")).toBe(`M 160 30 L ${HUB.x} ${HUB.y}`);
      expect(linkPath({ x: 278, y: 110 }, "out")).toBe(`M ${HUB.x} ${HUB.y} L 278 110`);
    });

    it("curves corner nodes and reverses the curve for flows out of the hub", () => {
      expect(linkPath({ x: 278, y: 190 }, "in")).toBe(`M 278 190 Q 278 ${HUB.y} ${HUB.x} ${HUB.y}`);
      expect(linkPath({ x: 278, y: 190 }, "out")).toBe(`M ${HUB.x} ${HUB.y} Q 278 ${HUB.y} 278 190`);
    });

    it("draws idle lines like incoming ones", () => {
      expect(linkPath({ x: 42, y: 110 }, "none")).toBe(linkPath({ x: 42, y: 110 }, "in"));
    });
  });

  describe("buildEnergyFlow", () => {
    it("always shows PV, grid and house; battery and consumers only when present", () => {
      const layout = buildEnergyFlow(values(), options());

      expect(layout.nodes.map(n => n.id)).toEqual(["pv", "grid", "house"]);
      expect(layout.links.every(l => !l.active)).toBeTrue();
    });

    it("shows the battery when the logic has one or it carries power", () => {
      expect(buildEnergyFlow(values(), options({ hasBattery: true })).nodes.map(n => n.id)).toContain("battery");
      expect(buildEnergyFlow(values({ battery: -1 }), options()).nodes.map(n => n.id)).toContain("battery");
    });

    it("shows consumers that are present, named or carry power, with their number", () => {
      const layout = buildEnergyFlow(values({ consumers: [undefined, undefined, 2] }), options({
        consumers: [{ present: true }, { present: false, name: "Wallbox" }, { present: false }]
      }));

      const consumers = layout.nodes.filter(n => n.kind === "consumer");
      expect(consumers.map(n => n.id)).toEqual(["consumer1", "consumer2", "consumer3"]);
      expect(consumers.map(n => n.index)).toEqual([1, 2, 3]);
      expect(consumers[1].name).toBe("Wallbox");
      expect(consumers[2].direction).toBe("out");
    });

    it("a sunny noon: PV in, export out, battery charging, house taking", () => {
      const layout = buildEnergyFlow(values({ pv: 6, grid: -1.5, battery: 2, house: 2.5 }), options({ hasBattery: true }));
      const link = (id: string) => layout.links.find(l => l.id === id);

      expect(link("pv").direction).toBe("in");
      expect(link("grid").direction).toBe("out");
      expect(link("battery").direction).toBe("out");
      expect(link("house").direction).toBe("out");
      expect(layout.links.every(l => l.active)).toBeTrue();
      // the biggest flow has the thickest line
      expect(link("pv").width).toBeGreaterThan(link("grid").width);
      expect(link("pv").power).toBe(6);
      expect(link("grid").power).toBe(1.5);
      // export runs from the hub to the grid
      expect(link("grid").path.startsWith(`M ${HUB.x} ${HUB.y}`)).toBeTrue();
    });

    it("idle lines are thin and not active", () => {
      const layout = buildEnergyFlow(values({ pv: 0, grid: 0.001 }), options());

      expect(layout.links.find(l => l.id === "grid").active).toBeFalse();
      expect(layout.links.find(l => l.id === "grid").width).toBe(1.5);
    });

    it("treats invalid numbers as 0", () => {
      const layout = buildEnergyFlow(values({ pv: NaN, grid: Infinity }), options());

      expect(layout.nodes.find(n => n.id === "pv").power).toBe(0);
      expect(layout.nodes.find(n => n.id === "grid").power).toBe(0);
    });
  });

  describe("formatting", () => {
    it("shows W below 1 kW and kW with 2 or 1 decimals above", () => {
      expect(formatPower(0.42)).toEqual({ value: "420", unit: "W" });
      expect(formatPower(-0.0049)).toEqual({ value: "5", unit: "W" });
      expect(formatPower(3.456)).toEqual({ value: "3.46", unit: "kW" });
      expect(formatPower(-12.34)).toEqual({ value: "12.3", unit: "kW" });
    });

    it("uses the locale", () => {
      expect(formatPower(3.456, "de")).toEqual({ value: "3,46", unit: "kW" });
      expect(formatNumber(1234.5, 1, "de")).toBe("1.234,5");
    });

    it("shows a dash without a value", () => {
      expect(formatPower(undefined)).toEqual({ value: "–", unit: "" });
      expect(formatPower(NaN)).toEqual({ value: "–", unit: "" });
      expect(formatNumber(undefined, 0)).toBe("–");
    });
  });
});
