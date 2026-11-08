import { mediaModes, visibleMediaGroups } from "./media-controller-model";

describe("media controller model", () => {

  it("mediaModes_OnlyTheNamedOnes", () => {
    expect(mediaModes(["TV", "", " Radio ", null])).toEqual([{ mode: 1, name: "TV" }, { mode: 3, name: "Radio" }]);
  });

  it("mediaModes_NoNames_AllEightByNumber", () => {
    const modes = mediaModes(undefined);
    expect(modes.length).toBe(8);
    expect(modes[0]).toEqual({ mode: 1, name: "" });
    expect(modes[7].mode).toBe(8);
  });

  it("visibleMediaGroups_TheLinkedGroups", () => {
    const groups = visibleMediaGroups(new Set(["volumeOutput", "navOkOutput", "modeActive1"]));
    expect(Array.from(groups).sort()).toEqual(["navigation", "volume"]);
  });

  it("visibleMediaGroups_NothingLinked_AllGroups", () => {
    expect(visibleMediaGroups(new Set(["modeActive1"])).size).toBe(4);
  });
});
