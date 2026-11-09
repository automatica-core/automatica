import {
  ACTIVE_THRESHOLD, buildEnergyFlow, CONSUMER_COUNT, CONSUMER_SLOTS, flowDirection, flowDuration, formatNumber, formatPower, HUB,
  linkPath, load, strokeWidth, EnergyFlowOptions, EnergyValues, VIEW_HEIGHT, VIEW_HEIGHT_TALL, VIEW_WIDTH
} from "./energy-flow-model";

describe("energy flow model", () => {

  const options = (extra: Partial<EnergyFlowOptions> = {}): EnergyFlowOptions => ({
    peakPower: 10,
    hasBattery: false,
    consumers: Array.from({ length: CONSUMER_COUNT }, () => ({ present: false })),
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

    it("shows up to six consumers, each at its own fixed place", () => {
      const layout = buildEnergyFlow(values({ consumers: [1, 1, 1, 1, 1, 1] }), options());

      const consumers = layout.nodes.filter(n => n.kind === "consumer");
      expect(consumers.map(n => n.id)).toEqual(["consumer1", "consumer2", "consumer3", "consumer4", "consumer5", "consumer6"]);
      expect(consumers.map(n => n.position)).toEqual(CONSUMER_SLOTS);
      expect(layout.links.filter(l => l.kind === "consumer").every(l => l.active)).toBeTrue();
      expect(layout.height).toBe(VIEW_HEIGHT_TALL);
    });

    it("keeps consumers 1-3 where they always were and moves none when another appears", () => {
      const alone = buildEnergyFlow(values({ consumers: [undefined, undefined, 1] }), options());
      const withSix = buildEnergyFlow(values({ consumers: [undefined, undefined, 1, undefined, undefined, 1] }), options());

      const consumer3 = (l: typeof alone) => l.nodes.find(n => n.id === "consumer3").position;
      expect(consumer3(alone)).toEqual({ x: 42, y: 190 });
      expect(consumer3(withSix)).toEqual({ x: 42, y: 190 });
      expect(withSix.nodes.find(n => n.id === "consumer1")).toBeUndefined();
    });

    it("stays compact up to four consumers and grows for the fifth or sixth", () => {
      expect(buildEnergyFlow(values({ consumers: [1, 1, 1, 1] }), options()).height).toBe(VIEW_HEIGHT);
      expect(buildEnergyFlow(values({ consumers: [] }), options()).height).toBe(VIEW_HEIGHT);
      expect(buildEnergyFlow(values({ consumers: [undefined, undefined, undefined, undefined, 2] }), options()).height).toBe(VIEW_HEIGHT_TALL);
      expect(buildEnergyFlow(values(), options({
        consumers: [{ present: false }, { present: false }, { present: false }, { present: false }, { present: false }, { present: false, name: "Sauna" }]
      })).height).toBe(VIEW_HEIGHT_TALL);
    });

    it("places every node inside the view, apart from each other and off the curves of the bottom row", () => {
      const layout = buildEnergyFlow(values({ consumers: [1, 1, 1, 1, 1, 1] }), options({ hasBattery: true }));
      const points = layout.nodes.map(n => n.position);

      for (const p of points) {
        expect(p.x).toBeGreaterThan(0);
        expect(p.x).toBeLessThan(VIEW_WIDTH);
        expect(p.y).toBeGreaterThan(0);
        expect(p.y).toBeLessThan(layout.height);
      }
      // phone nodes are ~68 view units wide and ~55 high: two nodes never share that box
      for (let i = 0; i < points.length; i++) {
        for (let j = i + 1; j < points.length; j++) {
          const apart = Math.abs(points[i].x - points[j].x) >= 58 || Math.abs(points[i].y - points[j].y) >= 60;
          expect(apart).withContext(`${layout.nodes[i].id} / ${layout.nodes[j].id}`).toBeTrue();
        }
      }
      // the bottom row curves pass the battery row at least 40 units from the battery and the corner nodes
      for (const id of ["consumer5", "consumer6"]) {
        const from = layout.nodes.find(n => n.id === id).position;
        const t = 1 - Math.sqrt((190 - HUB.y) / (from.y - HUB.y));
        const x = (1 - t) * (1 - t) * from.x + 2 * t * (1 - t) * from.x + t * t * HUB.x;
        for (const other of [42, 160, 278]) {
          expect(Math.abs(x - other)).withContext(id).toBeGreaterThanOrEqual(40);
        }
      }
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
