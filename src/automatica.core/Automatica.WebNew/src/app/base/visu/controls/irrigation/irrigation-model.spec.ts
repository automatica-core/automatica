import { formatDuration, formatHistoryTime, irrigationHistory, irrigationZones, weekdayNames, zoneProgress } from "./irrigation-model";

describe("irrigation model", () => {

  it("formatDuration", () => {
    expect(formatDuration(0)).toBe("0:00");
    expect(formatDuration(75)).toBe("1:15");
    expect(formatDuration(3725)).toBe("1:02:05");
    expect(formatDuration(-3)).toBe("0:00");
    expect(formatDuration("abc")).toBe("0:00");
  });

  it("irrigationZones_TheZonesWithATime_AndTheActiveOne", () => {
    const zones = irrigationZones(["Lawn", "", "Beds"], [600, 300, 0, 0], 3);
    expect(zones).toEqual([
      { zone: 1, name: "Lawn", duration: 600 },
      { zone: 2, name: "2", duration: 300 },
      { zone: 3, name: "Beds", duration: 0 }
    ]);
  });

  it("zoneProgress_WithTheRainFactor", () => {
    expect(zoneProgress(300, 600)).toBe(50);
    // factor 0.5: the zone runs 300 s
    expect(zoneProgress(150, 600, 0.5)).toBe(50);
    expect(zoneProgress(10, 0)).toBe(0);
    expect(zoneProgress(900, 600)).toBe(0);
  });

  it("irrigationHistory_NewestFirst_BothCasings", () => {
    const history = irrigationHistory([
      { At: "2026-06-01T06:00:00Z", Kind: "start", Zone: 0 },
      { at: "2026-06-01T06:10:00Z", kind: "zone", zone: 2 },
      { at: "not a date", kind: "zone" }
    ]);
    expect(history.map(a => a.kind)).toEqual(["zone", "start"]);
    expect(history[0].zone).toBe(2);
  });

  it("formatHistoryTime_InTheGivenLanguage", () => {
    const at = new Date(2026, 10, 8, 9, 28);
    expect(formatHistoryTime(at, "de-AT")).toBe("08.11.26, 09:28");
  });

  it("weekdayNames_MondayFirst", () => {
    const names = weekdayNames([0, 1, 3], "en-US");
    expect(names).toEqual(["Mon", "Wed", "Sun"]);
    expect(weekdayNames(null)).toEqual([]);
  });
});
