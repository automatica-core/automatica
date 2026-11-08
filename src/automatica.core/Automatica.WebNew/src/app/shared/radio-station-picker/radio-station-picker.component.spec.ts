import { buildPickerItems } from "./radio-station-picker.component";
import { RadioStation } from "src/app/base/model/radio-stations/radio-station";

describe("radio station picker", () => {

  const stations: RadioStation[] = [
    { key: "at-oe3", name: "Ö3", country: "AT", genre: "Pop", streamUrl: "http://oe3.example", logo: "https://oe3.example/logo.png" },
    { key: "de-1live", name: "1LIVE", country: "de", streamUrl: "http://1live.example", logo: "javascript:alert(1)" },
    { key: "mine", name: "Mine", streamUrl: "https://mine.example", isCustom: true }
  ];
  const country = (code: string) => code === "AT" ? "Österreich" : code === "DE" ? "Deutschland" : code;

  it("buildPickerItems_GroupsByCountry_OtherWithoutCountry", () => {
    const items = buildPickerItems(stations, [], country, "Weitere");

    expect(items.map(a => a.group)).toEqual(["Österreich", "Deutschland", "Weitere"]);
    expect(items[0]).toEqual({ key: "at-oe3", name: "Ö3", genre: "Pop", logo: "https://oe3.example/logo.png", group: "Österreich" });
    expect(items[1].logo).toBeUndefined();
  });

  it("buildPickerItems_KeepsSelectedValuesThatAreNotInTheCatalog", () => {
    const items = buildPickerItems(stations, ["AT-OE3", "Ö3 alt=s8007", "http://legacy.example/live", "s8235"], country, "Weitere");

    expect(items.length).toBe(6);
    expect(items.slice(3)).toEqual([
      { key: "Ö3 alt=s8007", name: "Ö3 alt", group: "Weitere" },
      { key: "http://legacy.example/live", name: "http://legacy.example/live", group: "Weitere" },
      { key: "s8235", name: "s8235", group: "Weitere" }
    ]);
  });

  it("buildPickerItems_WithoutCatalog_OnlyTheSelectedValues", () => {
    expect(buildPickerItems(undefined, ["at-oe3"], country, "Other")).toEqual([{ key: "at-oe3", name: "at-oe3", group: "Other" }]);
  });
});
