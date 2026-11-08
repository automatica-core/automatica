import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";
import { BaseService } from "./base-service";
import { RadioStation } from "../base/model/radio-stations/radio-station";

/**
 * webapi/radio-stations: the station catalog of the server (every role) and the custom stations (admins). The list is
 * loaded once and shared until the custom stations are saved.
 */
@Injectable({ providedIn: "root" })
export class RadioStationsService extends BaseService {

  private static readonly base = "radio-stations";

  private stations: Promise<RadioStation[]> | undefined;

  constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService) {
    super(http, pRouter, translationService);
  }

  getStations(): Promise<RadioStation[]> {
    if (!this.stations) {
      this.stations = super.getJson(RadioStationsService.base)
        .then(a => (Array.isArray(a) ? a : []) as RadioStation[])
        .catch(error => {
          this.stations = undefined;
          throw error;
        });
    }
    return this.stations;
  }

  async getCustomStations(): Promise<RadioStation[]> {
    const stations = await super.getJson(`${RadioStationsService.base}/custom`);
    return Array.isArray(stations) ? stations : [];
  }

  async saveCustomStations(stations: RadioStation[]): Promise<RadioStation[]> {
    const saved = await super.putJson(`${RadioStationsService.base}/custom`, stations);
    this.stations = undefined;
    return Array.isArray(saved) ? saved : [];
  }
}
