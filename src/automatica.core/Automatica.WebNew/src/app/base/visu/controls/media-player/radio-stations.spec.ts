import { currentRadioStation, radioStations } from "./radio-stations";

describe("radio stations", () => {

  it("radioStations_BothCasings_WithoutEmptyIds", () => {
    const list = radioStations([
      { name: "Ö3", id: "s8007" },
      { Name: "", Id: "s8235" },
      { name: "nothing", id: " " },
      null
    ]);
    expect(list).toEqual([
      { name: "Ö3", id: "s8007" },
      { name: "s8235", id: "s8235" }
    ]);
    expect(radioStations(undefined)).toEqual([]);
  });

  it("currentRadioStation_ByIdOrName", () => {
    const list = radioStations([{ name: "Ö3", id: "s8007" }, { name: "FM4", id: "s8235" }]);
    expect(currentRadioStation(list, "s8235")?.name).toBe("FM4");
    expect(currentRadioStation(list, "fm4")?.id).toBe("s8235");
    expect(currentRadioStation(list, "s1")).toBeUndefined();
    expect(currentRadioStation(list, null)).toBeUndefined();
  });
});
