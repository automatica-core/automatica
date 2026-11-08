import { ChangeDetectionStrategy, Component, HostListener, OnDestroy, OnInit } from "@angular/core";
import { HttpErrorResponse } from "@angular/common/http";
import { L10nTranslationService } from "angular-l10n";
import { BaseComponent } from "src/app/base/base-component";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { RadioStationsService } from "src/app/services/radio-stations.service";
import { RadioStation, isValidStationKey, isValidStreamUrl, stationKeyOf } from "src/app/base/model/radio-stations/radio-station";

/**
 * The radio stations of the server: the built-in list (read only) and the stations the administrator adds. Logics such
 * as Sonos Control pick their stations from this list, the device drivers translate them.
 */
@Component({
  selector: "app-radio-stations-page",
  templateUrl: "./radio-stations-page.component.html",
  styleUrls: ["./radio-stations-page.component.scss"],
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: false
})
export class RadioStationsPageComponent extends BaseComponent implements OnInit, OnDestroy {

  loaded = false;
  loadError: string | undefined;
  saving = false;

  builtIn: RadioStation[] = [];
  custom: RadioStation[] = [];
  private savedJson = "[]";

  /** the stream played as a test in the browser */
  playing: string | undefined;
  private audio: HTMLAudioElement | undefined;
  private readonly brokenLogos = new Set<string>();

  readonly keyPattern = /^[a-z0-9][a-z0-9-]{0,63}$/;
  readonly urlPattern = /^https?:\/\/[^\s/]+/i;

  constructor(
    private service: RadioStationsService,
    translate: L10nTranslationService,
    notify: NotifyService,
    appService: AppService) {
    super(notify, translate, appService);
    appService.setAppTitle("UI.RADIO_STATIONS.TITLE");
  }

  async ngOnInit() {
    super.baseOnInit();
    await this.load();
  }

  ngOnDestroy() {
    this.stop();
    super.baseOnDestroy();
  }

  @HostListener("window:beforeunload", ["$event"])
  beforeUnload(event: BeforeUnloadEvent) {
    if (this.dirty) {
      event.preventDefault();
    }
  }

  get dirty(): boolean {
    return JSON.stringify(this.custom.map(normalizeStation)) !== this.savedJson;
  }

  protected async load() {
    const done = this.appService.beginLoading();
    try {
      const [all, custom] = await Promise.all([this.service.getStations(), this.service.getCustomStations()]);
      this.builtIn = all.filter(a => !a.isCustom);
      this.applyCustom(custom);
      this.loadError = void 0;
    } catch (error) {
      this.loadError = this.parseErrorString(error)?.toString() ?? "";
    } finally {
      this.loaded = true;
      done();
    }
  }

  private applyCustom(custom: RadioStation[]) {
    this.custom = (custom ?? []).map(a => ({ ...a }));
    this.savedJson = JSON.stringify(this.custom.map(normalizeStation));
  }

  reset() {
    this.applyCustom(JSON.parse(this.savedJson));
  }

  /** a new row without a key gets one from its name */
  onRowInserting(e: { data: RadioStation }) {
    if (!e.data.key && e.data.name) {
      e.data.key = uniqueStationKey(stationKeyOf(e.data.name), this.custom.map(a => a.key));
    }
  }

  /** the translation key of the reason a station cannot be saved, or undefined */
  problemOf(station: RadioStation): string | undefined {
    return stationProblem(station, this.custom);
  }

  /** a custom station with the key of a built-in one replaces it */
  replacesBuiltIn(station: RadioStation): boolean {
    return this.builtIn.some(a => a.key === station.key);
  }

  async save() {
    if (this.saving) {
      return;
    }
    const invalid = this.custom.find(a => this.problemOf(a));
    if (invalid) {
      this.notifyService.notifyError(`${invalid.name || invalid.key}: ${this.translate.translate(this.problemOf(invalid))}`);
      return;
    }
    this.saving = true;
    const done = this.appService.beginLoading();
    try {
      this.applyCustom(await this.service.saveCustomStations(this.custom.map(normalizeStation)));
      this.notifyService.notifySuccess(this.translate.translate("UI.RADIO_STATIONS.SAVED"));
    } catch (error) {
      const message = error instanceof HttpErrorResponse ? error.error?.message : undefined;
      if (message) {
        this.notifyService.notifyError(message);
      } else {
        this.handleError(error);
      }
    } finally {
      this.saving = false;
      done();
    }
  }

  showLogo(url: string | undefined): boolean {
    return !!url && /^https?:\/\//i.test(url) && !this.brokenLogos.has(url);
  }

  logoFailed(url: string) {
    this.brokenLogos.add(url);
  }

  /** plays the stream in the browser (an http stream may be blocked when the page is https) */
  toggle(station: RadioStation) {
    if (this.playing === station.streamUrl) {
      this.stop();
      return;
    }
    this.stop();
    if (!isValidStreamUrl(station.streamUrl)) {
      return;
    }
    const audio = new Audio(station.streamUrl);
    const failed = () => {
      if (this.audio === audio) {
        this.stop();
        this.notifyService.notifyError(this.translate.translate("UI.RADIO_STATIONS.PLAY_FAILED"));
      }
    };
    audio.onerror = failed;
    this.audio = audio;
    this.playing = station.streamUrl;
    audio.play().catch(failed);
  }

  stop() {
    if (this.audio) {
      this.audio.onerror = null;
      this.audio.pause();
      this.audio.removeAttribute("src");
    }
    this.audio = void 0;
    this.playing = void 0;
  }
}

/** trimmed, key lower case, country upper case, empty optional fields removed */
export function normalizeStation(a: RadioStation): RadioStation {
  const text = (value: any) => (value ?? "").toString().trim();
  return {
    key: text(a.key).toLowerCase(),
    name: text(a.name),
    country: text(a.country).toUpperCase() || undefined,
    genre: text(a.genre) || undefined,
    streamUrl: text(a.streamUrl),
    logo: text(a.logo) || undefined,
    codec: text(a.codec) || undefined,
    tuneInId: text(a.tuneInId) || undefined
  };
}

/** the translation key of the reason a custom station cannot be saved (the server checks the same), or undefined */
export function stationProblem(station: RadioStation, all: RadioStation[]): string | undefined {
  const key = (station.key ?? "").trim().toLowerCase();
  if (!isValidStationKey(key)) {
    return "UI.RADIO_STATIONS.INVALID.KEY";
  }
  if (!(station.name ?? "").trim()) {
    return "UI.RADIO_STATIONS.INVALID.NAME";
  }
  if (!isValidStreamUrl(station.streamUrl)) {
    return "UI.RADIO_STATIONS.INVALID.STREAM_URL";
  }
  if ((station.logo ?? "").trim() && !isValidStreamUrl(station.logo)) {
    return "UI.RADIO_STATIONS.INVALID.LOGO";
  }
  if ((all ?? []).filter(a => (a.key ?? "").trim().toLowerCase() === key).length > 1) {
    return "UI.RADIO_STATIONS.INVALID.DUPLICATE";
  }
  return undefined;
}

/** key, or key-2, key-3, ... when it is taken */
export function uniqueStationKey(key: string, taken: string[]): string {
  const base = key || "station";
  let candidate = base;
  for (let i = 2; (taken ?? []).includes(candidate); i++) {
    candidate = `${base}-${i}`;
  }
  return candidate;
}
