import { Injectable } from "@angular/core";
import { BaseService } from "./base-service";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { L10nTranslationService } from "angular-l10n";

export enum LogLevel
{
  Debug,
  Information,
  Warning,
  Error,
  Critical,
  None
}

@Injectable()
export class LogsService extends BaseService {

    constructor(http: HttpClient, pRouter: Router, translationService: L10nTranslationService) {
        super(http, pRouter, translationService);
    }

    getLogFiles(): Promise<any[]> {
        return super.getJson("logging/files");
    }

    getLogFile(logName: string): Promise<any> {
        return super.getRaw("logging/file?file=" + logName);
    }

    getLogger(): Promise<any> {
        return super.getRaw("logging/logger");
    } 
    
    setLogLevel(name: string, level: LogLevel): Promise<any> {
        return super.postRaw(`logging/logger?name=${name}&level=${level}`, null);
    }
}
