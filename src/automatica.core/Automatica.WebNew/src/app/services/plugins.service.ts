import { Injectable } from "@angular/core";
import { BaseService } from "../services/base-service";
import { Router } from "@angular/router";
import { TranslationService } from "angular-l10n";
import { HttpClient } from "@angular/common/http";


export enum PluginType {
    Driver = 0,
    Logic = 1
}

export interface Version {
    major: number;
    minor: number;
    build: number;
    revision: number;
}

export interface Plugin {
    azureFileName: string;
    componentName: string;
    minCoreServerVersionObj: Version;
    version: string;
    pluginGuid: string;
    isPrerelease: boolean;
    name: string;
    pluginType: PluginType;
}

export interface PluginState {
    cloudPlugin: Plugin;
    loadedPlugin: Plugin;
}


@Injectable()
export class PluginsService extends BaseService {
    constructor(httpService: HttpClient, pRouter: Router, translationService: TranslationService) {
        super(httpService, pRouter, translationService);
    }

    public getPlugins(): Promise<any[]> {
        return super.getJson("plugins/plugins");
    }
}
