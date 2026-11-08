import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnInit, Output } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import DataSource from "devextreme/data/data_source";
import { RadioStationsService } from "src/app/services/radio-stations.service";
import { RadioStation, joinStationKeys, parseStationKeys } from "src/app/base/model/radio-stations/radio-station";

/** an entry of the picker: a station of the catalog, or a value that is not in it (e.g. "Ö3=s8007" from before the catalog) */
export interface RadioStationPickerItem {
  key: string;
  name: string;
  genre?: string;
  logo?: string;
  group: string;
}

/**
 * Picks stations of the server's radio station catalog for a logic parameter (value "at-oe3;de-1live"). With
 * single one station. Values that are not in the catalog are kept and shown under "other".
 */
@Component({
  selector: "radio-station-picker",
  templateUrl: "./radio-station-picker.component.html",
  styleUrls: ["./radio-station-picker.component.scss"],
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: false
})
export class RadioStationPickerComponent implements OnInit {

  @Input() single = false;
  @Input() readOnly = false;

  @Output() valueChange = new EventEmitter<string>();

  private _value = "";
  @Input()
  get value(): string {
    return this._value;
  }
  set value(v: string) {
    this._value = (v ?? "").toString();
    this.selected = parseStationKeys(this._value);
    this.rebuild();
  }

  selected: string[] = [];
  dataSource: DataSource;
  loadError = false;
  private stations: RadioStation[] = [];
  private readonly brokenLogos = new Set<string>();

  constructor(private service: RadioStationsService, private translate: L10nTranslationService) { }

  async ngOnInit() {
    try {
      this.stations = await this.service.getStations();
      this.loadError = false;
    } catch (error) {
      console.error("could not load the radio stations", error);
      this.loadError = true;
    }
    this.rebuild();
  }

  get singleValue(): string | null {
    return this.selected[0] ?? null;
  }

  onSingleChanged(key: string | null) {
    this.emit(key ? [key] : []);
  }

  onMultipleChanged(keys: string[]) {
    this.emit(keys ?? []);
  }

  showLogo(item: RadioStationPickerItem): boolean {
    return !!item?.logo && !this.brokenLogos.has(item.logo);
  }

  logoFailed(item: RadioStationPickerItem) {
    if (item?.logo) {
      this.brokenLogos.add(item.logo);
    }
  }

  private emit(keys: string[]) {
    const value = joinStationKeys(keys);
    if (value === this._value) {
      return;
    }
    this._value = value;
    this.selected = parseStationKeys(value);
    this.valueChange.emit(value);
  }

  private rebuild() {
    const items = buildPickerItems(this.stations, this.selected, code => this.countryName(code), this.translate.translate("UI.RADIO_STATIONS.OTHER"));
    this.dataSource = new DataSource({
      store: { type: "array", key: "key", data: items },
      group: "group",
      paginate: false,
      searchExpr: ["name", "genre", "key"]
    });
  }

  private countryName(code: string): string {
    try {
      const names = new Intl.DisplayNames([this.translate.getLocale()?.language || "en"], { type: "region" });
      return names.of(code) || code;
    } catch {
      return code;
    }
  }
}

/** the catalog grouped by country (custom stations without a country and unknown values go to "other") */
export function buildPickerItems(stations: RadioStation[], selected: string[], countryName: (code: string) => string, other: string): RadioStationPickerItem[] {
  const items: RadioStationPickerItem[] = (stations ?? []).map(a => ({
    key: a.key,
    name: a.name,
    genre: a.genre,
    logo: a.logo && /^https?:\/\//i.test(a.logo) ? a.logo : undefined,
    group: a.country ? countryName(a.country.toUpperCase()) : other
  }));

  for (const key of selected ?? []) {
    if (!items.some(a => a.key.toLowerCase() === key.toLowerCase())) {
      const separator = key.indexOf("=");
      const name = separator > 0 && !key.substring(0, separator).includes("://") ? key.substring(0, separator).trim() : key;
      items.push({ key, name, group: other });
    }
  }
  return items;
}
