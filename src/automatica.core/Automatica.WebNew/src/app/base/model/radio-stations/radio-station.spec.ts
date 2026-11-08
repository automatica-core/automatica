import { isValidStationKey, isValidStreamUrl, joinStationKeys, parseStationKeys, stationKeyOf } from "./radio-station";

describe("radio station", () => {

  it("parseStationKeys_SplitsTrimsAndDropsDuplicates", () => {
    expect(parseStationKeys(" at-oe3 ;de-1live,\nAT-OE3;;Ö3=s8007\r\n")).toEqual(["at-oe3", "de-1live", "Ö3=s8007"]);
    expect(parseStationKeys(null)).toEqual([]);
    expect(parseStationKeys("")).toEqual([]);
  });

  it("joinStationKeys_WithoutEmptyEntries", () => {
    expect(joinStationKeys(["at-oe3", " ", "de-1live "])).toBe("at-oe3;de-1live");
    expect(joinStationKeys([])).toBe("");
  });

  it("isValidStationKey_AsTheServer", () => {
    expect(isValidStationKey("at-oe3")).toBeTrue();
    expect(isValidStationKey("1live")).toBeTrue();
    expect(isValidStationKey("At-oe3")).toBeFalse();
    expect(isValidStationKey("-x")).toBeFalse();
    expect(isValidStationKey("a b")).toBeFalse();
    expect(isValidStationKey("a".repeat(65))).toBeFalse();
    expect(isValidStationKey(undefined)).toBeFalse();
  });

  it("isValidStreamUrl_HttpOrHttps", () => {
    expect(isValidStreamUrl("http://a.example/live")).toBeTrue();
    expect(isValidStreamUrl(" https://a.example ")).toBeTrue();
    expect(isValidStreamUrl("x-rincon-mp3radio://a.example")).toBeFalse();
    expect(isValidStreamUrl("a.example")).toBeFalse();
  });

  it("stationKeyOf_MakesAKeyFromTheName", () => {
    expect(stationKeyOf("Ö3 Hitradio")).toBe("o3-hitradio");
    expect(stationKeyOf("  Radio Süd & Straße!! ")).toBe("radio-sud-strasse");
    expect(stationKeyOf("")).toBe("");
    expect(isValidStationKey(stationKeyOf("Lounge FM Wien"))).toBeTrue();
  });
});
