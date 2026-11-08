import { GarageState, clampPercent, extrapolateGaragePosition, stateOfPosition } from "./garage-model";

describe("garage model", () => {

  const travel = { openSeconds: 20, closeSeconds: 10 };

  it("extrapolate_Standing_KeepsThePosition", () => {
    expect(extrapolateGaragePosition({ position: 40, motion: 0, ...travel }, 5000)).toBe(40);
  });

  it("extrapolate_Opening_RunsWithTheOpenTime", () => {
    // 20 s for 100 %: 5 s are 25 %
    expect(extrapolateGaragePosition({ position: 10, motion: 1, ...travel }, 5000)).toBeCloseTo(35);
  });

  it("extrapolate_Closing_RunsWithTheCloseTime", () => {
    expect(extrapolateGaragePosition({ position: 80, motion: -1, ...travel }, 5000)).toBeCloseTo(30);
  });

  it("extrapolate_StopsAtTheTarget", () => {
    expect(extrapolateGaragePosition({ position: 0, motion: 1, target: 30, ...travel }, 60000)).toBe(30);
    expect(extrapolateGaragePosition({ position: 100, motion: -1, ...travel }, 60000)).toBe(0);
    expect(extrapolateGaragePosition({ position: 90, motion: 1, ...travel }, 60000)).toBe(100);
  });

  it("extrapolate_WithoutTravelTime_KeepsThePosition", () => {
    expect(extrapolateGaragePosition({ position: 50, motion: 1, openSeconds: 0, closeSeconds: 0 }, 5000)).toBe(50);
  });

  it("clampPercent_InvalidAndOutOfRange", () => {
    expect(clampPercent(null)).toBe(0);
    expect(clampPercent("abc")).toBe(0);
    expect(clampPercent(-5)).toBe(0);
    expect(clampPercent(140)).toBe(100);
    expect(clampPercent("42.5")).toBe(42.5);
  });

  it("stateOfPosition", () => {
    expect(stateOfPosition(0)).toBe(GarageState.Closed);
    expect(stateOfPosition(100)).toBe(GarageState.Open);
    expect(stateOfPosition(30)).toBe(GarageState.PartiallyOpen);
  });
});
