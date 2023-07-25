import { Injectable, EventEmitter } from "@angular/core";
import { BaseHub, SignalRMethod } from "../base-hub";

@Injectable()
export class UpdateHubService extends BaseHub {

    @SignalRMethod
    public UpdateDownloadProgressChanged: EventEmitter<any> = new EventEmitter<any>();
    @SignalRMethod
    public UpdateFinished: EventEmitter<any> = new EventEmitter<any>();
    @SignalRMethod
    public UpdateFailed: EventEmitter<any> = new EventEmitter<any>();

    @SignalRMethod
    public PluginDownloadProgressChanged: EventEmitter<any> = new EventEmitter<any>();
    @SignalRMethod
    public PluginFinished: EventEmitter<any> = new EventEmitter<any>();
    @SignalRMethod
    public PluginFailed: EventEmitter<any> = new EventEmitter<any>();
    @SignalRMethod
    public PluginLoaded: EventEmitter<any> = new EventEmitter<any>();

    public static formatBytes(bytes) {
        if (bytes < 1024) {
            return bytes + " Bytes";
        } else if (bytes < 1048576) {
            return (bytes / 1024).toFixed(2) + " KB";
        } else if (bytes < 1073741824) {
            return (bytes / 1048576).toFixed(2) + " MB";
        } else {
            return (bytes / 1073741824).toFixed(2) + " GB";
        }
    }

    constructor() {
        super("updateHub");
    }

    public downloadUpdate(coreServerVersion: any) {
        return super.callHubProxyWithParam("startUpdateDownload", coreServerVersion);
    }

    public updatePlugin(plugin: any) {
        return super.callHubProxyWithParam("startPluginUpdate", plugin);
    }

    public updateAllPlugins(plugin: any[]) {
        return super.callHubProxyWithParam("UpdateAllPlugin", plugin);
    }

    public installAllPlugins(plugin: any[]) {
        return super.callHubProxyWithParam("InstallAllPlugin", plugin);
    }
    public installUpdateAllPlugins(plugin: any[]) {
        return super.callHubProxyWithParam("InstallUpdateAllPlugin", plugin);
    }

    public installPlugin(plugin: any) {
        return super.callHubProxyWithParam("startPluginInstall", plugin);
    }
    public restart() {
        return super.callHubProxy("restart");
    }


}
