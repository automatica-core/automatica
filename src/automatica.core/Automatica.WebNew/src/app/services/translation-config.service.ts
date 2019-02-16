
import { Injectable } from "@angular/core";
import { TranslationService, LocaleService, StorageStrategy, ProviderType, L10nConfig, L10nLoader } from "angular-l10n";
import { loadMessages, locale } from "devextreme/localization"
import { HttpClient } from "@angular/common/http";

import "devextreme-intl";

import * as deMessages from "devextreme/localization/messages/de.json";
import * as enMessages from "devextreme/localization/messages/en.json";

export const TranslationConfiguration: L10nConfig = {
  locale: {
    languages: [
      { code: "en", dir: "ltr" },
      { code: "de", dir: "ltr" }
    ],
    language: "de",
    defaultLocale: {
      countryCode: "DE",
      languageCode: "de"
    },
    currency: "€",
    storage: StorageStrategy.Disabled
  },
  translation: {
    providers: [
      { type: ProviderType.Static, prefix: "./assets/locale/locale-" },
      { type: ProviderType.Static, prefix: "./assets/locale/permission/locale-" },
      { type: ProviderType.Static, prefix: "./assets/locale/visu/locale-visu-" },
      { type: ProviderType.Static, prefix: "./assets/locale/login/locale-" },
      { type: ProviderType.Static, prefix: "./assets/locale/error/locale-" },
      { type: ProviderType.WebAPI, path: "./webapi/localization/" }
    ],
    caching: true,
    composedKeySeparator: ".",
    missingValue: miss
  }
};

export function miss(path: string): string {
  if (path.endsWith(".DESCRIPTION")) {
    return "";
  }
  return path;
}


@Injectable()
export class TranslationConfigService {
  constructor(public localeService: LocaleService, public translation: TranslationService, private http: HttpClient, private l10Loader: L10nLoader) {
    this.translation.translationError.subscribe((error: any) => console.log(error));
  }

  async init() {
    await this.l10Loader.load();

    loadMessages(deMessages);
    loadMessages(enMessages);

    locale("de");

  }

}
