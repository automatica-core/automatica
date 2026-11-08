
import { Inject, Injectable, Optional } from "@angular/core";
import { L10nTranslationService, L10nConfig, L10nLoader, L10nTranslationLoader, L10nProvider } from "angular-l10n";
import { loadMessages, locale } from "devextreme/localization"
import { HttpClient, HttpHeaders, HttpParams } from "@angular/common/http";

import * as deMessages from "devextreme/localization/messages/de.json";
import * as enMessages from "devextreme/localization/messages/en.json";
import { Observable, timeout } from "rxjs";
import { BaseServiceHelper } from "./base-server-helper";
import { AppService } from "./app.service";
import { Language, SettingsService } from "./settings.service";

@Injectable() export class HttpTranslationLoader implements L10nTranslationLoader {

  private headers = new HttpHeaders({ "Content-Type": "application/json" });

  constructor(@Optional() private http: HttpClient) { }

  public get(language: string, provider: L10nProvider): Observable<{ [key: string]: any }> {

    if (provider.options.type === "file") {

      const url = `${provider.asset}-${language}.json`;
      const options = {
        headers: this.headers
      };
      return this.http.get(url, options).pipe(timeout(2000));

    } else if (provider.options.type === "webapi") {

      const url = `${BaseServiceHelper.getBaseUrl()}/${provider.asset}/${language}`;
      const options = {
        headers: this.headers,

      };

      return this.http.get(url, options).pipe(timeout(2000));
    }
  }
}

export const LocalLanguageConfigration: L10nConfig = {
  format: "language-region",
  cache: true,
  keySeparator: ".",
  defaultLocale: { language: "de", currency: "EUR" },
  schema: [
    { locale: { language: "de", currency: "EUR" }, dir: "ltr", text: "German" },
    { locale: { language: "en", currency: "USD" }, dir: "ltr", text: "English" }
  ],
  providers: [
    { name: "locale", asset: "./assets/locale/locale", options: { type: "file" } },
    { name: "visu", asset: "./assets/locale/permission/locale", options: { type: "file" } },
    { name: "visu_login", asset: "./assets/locale/visu/locale-visu", options: { type: "file" } },
    { name: "login", asset: "./assets/locale/login/locale", options: { type: "file" } },
    { name: "error", asset: "./assets/locale/error/locale", options: { type: "file" } }
  ]
};

export function miss(path: string): string {
  if (path.endsWith(".DESCRIPTION")) {
    return "";
  }
  return path;
}


@Injectable()
export class TranslationConfigService {
  constructor(public translation: L10nTranslationService,
    private http: HttpClient,
    private l10Loader: L10nLoader,
    private appService: AppService,
    private settingsService: SettingsService) {
    // this.translation.translationError.subscribe((error: any) => console.log(error));
  }

  private dxMessagesLoaded = false;

  /**
   * Reloads all translations (e.g. after the server started, plugins may bring new ones) and applies the server language.
   */
  async init() {
    const loadingDone = this.appService.beginLoading();

    try {
      await this.l10Loader.init();

      this.loadDxMessages();

      try {
        const language = await this.settingsService.getLanguage();
        // the loader picks its own locale (storage, browser, default), set the server language again
        // or the page switches language (and shows keys of a language that was never loaded)
        await this.applyLanguage(<Language>language.ValueInt!);
      }
      catch (error) {
        // keep the current language, the translations of it are loaded
        console.error(error);
      }
    }
    finally {
      loadingDone();
    }

  }

  /**
   * Loads the translations and the DevExtreme messages once at startup, no server call (the user may not be logged in yet).
   */
  async initLocalization() {
    await this.l10Loader.init();
    this.loadDxMessages();
  }

  /**
   * Switches the app and the DevExtreme widgets to the server language without reloading every translation.
   */
  async applyLanguage(language: Language) {
    this.loadDxMessages();

    const lang = language == Language.German ? "de" : "en";
    await this.translation.setLocale({ language: lang });
    locale(lang);
  }

  private loadDxMessages() {
    if (this.dxMessagesLoaded) {
      return;
    }
    this.dxMessagesLoaded = true;
    loadMessages(deMessages);
    loadMessages(enMessages);
  }

}
