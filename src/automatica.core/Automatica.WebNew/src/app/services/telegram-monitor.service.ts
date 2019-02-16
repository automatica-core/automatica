import { Injectable } from "@angular/core";
import { BaseService } from "../services/base-service";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { TranslationService } from "angular-l10n";
import { TelegramMonitorInstance } from "../base/model/telegram-monitor/telegram-monitor-instance";

@Injectable()
export class TelegramMonitorService extends BaseService {

  constructor(httpService: HttpClient, pRouter: Router, translationService: TranslationService) {
    super(httpService, pRouter, translationService);
  }

  getMonitorInstances(): Promise<TelegramMonitorInstance[]> {
    return super.getMultiple<TelegramMonitorInstance>("telegramMonitor");
  }
}
