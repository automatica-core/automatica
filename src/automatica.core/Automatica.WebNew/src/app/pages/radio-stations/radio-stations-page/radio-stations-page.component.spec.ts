import { normalizeStation, stationProblem, uniqueStationKey } from "./radio-stations-page.component";
import { RadioStation } from "src/app/base/model/radio-stations/radio-station";

describe("radio stations page", () => {

  const station = (key: string, extra: Partial<RadioStation> = {}): RadioStation =>
    ({ key, name: "Mine", streamUrl: "https://mine.example/live", ...extra });

  it("stationProblem_ChecksLikeTheServer", () => {
    const all = [station("ok"), station("twice"), station("TWICE")];

    expect(stationProblem(station("ok"), all)).toBeUndefined();
    expect(stationProblem(station("Bad Key"), all)).toBe("UI.RADIO_STATIONS.INVALID.KEY");
    expect(stationProblem(station("ok", { name: " " }), all)).toBe("UI.RADIO_STATIONS.INVALID.NAME");
    expect(stationProblem(station("ok", { streamUrl: "ftp://a" }), all)).toBe("UI.RADIO_STATIONS.INVALID.STREAM_URL");
    expect(stationProblem(station("ok", { logo: "data:image/png" }), all)).toBe("UI.RADIO_STATIONS.INVALID.LOGO");
    expect(stationProblem(station("twice"), all)).toBe("UI.RADIO_STATIONS.INVALID.DUPLICATE");
  });

  it("uniqueStationKey_AddsANumberWhenTaken", () => {
    expect(uniqueStationKey("mine", [])).toBe("mine");
    expect(uniqueStationKey("mine", ["mine", "mine-2"])).toBe("mine-3");
    expect(uniqueStationKey("", [])).toBe("station");
  });

  it("normalizeStation_TrimsAndDropsEmptyFields", () => {
    expect(normalizeStation({ key: " Mine ", name: " My radio ", country: "at", genre: " ", streamUrl: " http://a.example ", logo: "", isCustom: true }))
      .toEqual({ key: "mine", name: "My radio", country: "AT", genre: undefined, streamUrl: "http://a.example", logo: undefined, codec: undefined, tuneInId: undefined });
  });
});
