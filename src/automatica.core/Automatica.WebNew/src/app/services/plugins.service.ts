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
    Major: number;
    Minor: number;
    Build: number;
    Revision: number;
}

export interface Plugin {
    AzureFileName: string;
    ComponentName: string;
    MinCoreServerVersionObj: Version;
    Version: string;
    PluginGuid: string;
    IsPrerelease: boolean;
    Name: string;
    PluginType: PluginType;
}

export interface PluginState {
    CloudPlugin: Plugin;
    LoadedPlugin: Plugin;
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
