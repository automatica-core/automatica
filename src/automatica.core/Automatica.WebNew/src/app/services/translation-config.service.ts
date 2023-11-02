
import { Inject, Injectable, Optional } from "@angular/core";
import { L10nTranslationService, L10nConfig, L10nLoader, L10nTranslationLoader, L10nProvider } from "angular-l10n";
import { loadMessages, locale } from "devextreme/localization"
import { HttpClient, HttpHeaders, HttpParams } from "@angular/common/http";

import * as deMessages from "devextreme/localization/messages/de.json";
import * as enMessages from "devextreme/localization/messages/en.json";
import { Observable } from "rxjs";

@Injectable() export class HttpTranslationLoader implements L10nTranslationLoader {

  private headers = new HttpHeaders({ "Content-Type": "application/json" });

  constructor(@Optional() private http: HttpClient) { }

  public get(language: string, provider: L10nProvider): Observable<{ [key: string]: any }> {

    if (provider.options.type === "file") {

      const url = `${provider.asset}-${language}.json`;
      const options = {
        headers: this.headers
      };
      return this.http.get(url, options);

    } else if (provider.options.type === "webapi") {

      const url = `${provider.asset}/${language}`;
      const options = {
        headers: this.headers
      };

      return this.http.get(url, options);
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
    private l10Loader: L10nLoader) {
    // this.translation.translationError.subscribe((error: any) => console.log(error));
  }

  async init() {
    await this.l10Loader.init();



    loadMessages(deMessages);
    loadMessages(enMessages);

    locale("de");

  }

}
