import { EventEmitter } from "@angular/core";
import * as signalR from "@microsoft/signalr";
import { BaseServiceHelper } from "src/app/services/base-server-helper";
import { v4 as uuidv4 } from 'uuid';

const METHODS = new Map<string, Array<string>>();
export function SignalRMethod(target: any, propertyKey: string) {
    const className = target.constructor.name;
    let list: Array<string>;

    if (METHODS.has(className)) {
        list = METHODS.get(className);
    } else {
        list = new Array<string>();
        METHODS.set(className, list);
    }

    list.push(propertyKey);
}

interface CachedProxyRequest {
    methodName: string;
    params?: any;
    invoke: any;
}

export class BaseHub {

    public reconnectTime = 10000;

    protected hubName: string;
    private subscriptions: any[] = [];
    private connection: signalR.HubConnection;
    private started = false;
    private stopRequested = false;
    private reconnectTimer: any;


    private _id : string = uuidv4();
    public get Id() : string {
        return this._id;
    }


    public connectionStateChanged = new EventEmitter<any>();

    private _cachedRequests: CachedProxyRequest[] = [];

    public get connected() {
        return this.started;
    }

    private setConnectedState(value: boolean) {
        if (value !== this.started) {
            this.connectionStateChanged.emit(value);
        }
        this.started = value;
    }

    public get Connection(): signalR.HubConnection {
        return this.connection;
    }

    constructor(hubName: string) {
        this.hubName = hubName;
    }

    public init() {
        const methods: string[] = METHODS.get(this.constructor.name);

        if (methods == null) {
            return;
        }
        if (this.hubName == null) {
            return;
        }

        if (this.connection) {
            // never leave an orphaned connection behind whose handlers keep emitting
            const old = this.connection;
            this.connection = null;
            old.stop().catch(() => { });
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(BaseServiceHelper.getSignalRBaseUrl() + "/signalr/" + this.hubName, { accessTokenFactory: () => localStorage.getItem("jwt") })
            .configureLogging(signalR.LogLevel.Warning)
            .build();
        this.connection = connection;

        for (const method of methods) {
            connection.on(method, (...args: any[]) => this.methodCalled(method, args));
        }

        connection.onclose((error) => {
            if (connection !== this.connection) {
                return;
            }
            this.setConnectedState(false);
            if (!this.stopRequested) {
                this.scheduleReconnect();
            }
        });
    }

    private scheduleReconnect() {
        clearTimeout(this.reconnectTimer);
        this.reconnectTimer = setTimeout(async () => {
            this.reconnectTimer = undefined;
            await this.reconnect();
        }, this.reconnectTime);
    }

    /**
     * Called after the connection was (re)established, before cached requests are replayed.
     */
    protected onConnected(): Promise<void> {
        return Promise.resolve();
    }

    public async reconnect() {
        console.log("signalr connection lost, try to reinit");

        if (this.stopRequested || !this.connection) {
            return;
        }

        if (this.connection.state !== signalR.HubConnectionState.Connected) {
            this.setConnectedState(false);
            await this.start().catch(() => {
                this.scheduleReconnect();
            });
        }
    }

    public async start() {
        if (!localStorage.getItem("jwt")) {
            return;
        }

        console.log(`start ${this.Id} ${this.hubName}`);

        this.stopRequested = false;

        if (!this.connection) {
            this.init();
        }

        if (this.connection && this.connection.state === signalR.HubConnectionState.Disconnected) {

            await this.connection.start();

            this.setConnectedState(true);

            await this.onConnected();

            const pending = this._cachedRequests.splice(0);
            for (const cached of pending) {
                try {
                    await cached.invoke(cached.methodName, cached.params);
                } catch (error) {
                    console.error(error);
                }
            }
            return true;
        }
        return false;
    }

    public async stop() {
        this.stopRequested = true;
        clearTimeout(this.reconnectTimer);
        this.reconnectTimer = undefined;
        this._cachedRequests = [];

        if (this.connection && this.connection.state !== signalR.HubConnectionState.Disconnected) {
            await this.connection.stop();
        }
        this.unregisterAll();
        this.setConnectedState(false);
    }

    protected get isOnline() {
        return this.started && this.connection?.state === signalR.HubConnectionState.Connected;
    }

    protected registerEvent(event: EventEmitter<any>, callback: (any)) {
        const subscriber = event.subscribe((data) => callback(data));
        this.subscriptions.push(subscriber);
    }

    public unregisterAll() {
        this.subscriptions.forEach(sub => {
            sub.unsubscribe();
        });
        this.subscriptions = [];
    }

    async callHubProxyWithParam(methodName: string, param: any): Promise<any> {

        if (!this.isOnline) {
            this._cachedRequests.push({
                methodName: methodName,
                params: param,
                invoke: async () => await this.callHubProxyWithParam(methodName, param)
            });
            return;
        }

        return await this.connection.invoke(methodName, param);
    }

    /**
     * Sends immediately and never caches - for writes that must not be replayed after a reconnect.
     */
    async sendIfConnected(methodName: string, ...args: any[]): Promise<any> {
        if (!this.isOnline) {
            console.warn(`signalr ${this.hubName} not connected, dropping ${methodName}`);
            return;
        }

        return await this.connection.send(methodName, ...args);
    }

    async callHubProxyWithParams(methodName: string, ...args: any[]): Promise<any> {

        if (!this.isOnline) {
            this._cachedRequests.push({
                methodName: methodName,
                params: args,
                invoke: async () => await this.callHubProxyWithParams(methodName, ...args)
            });
            return;
        }

        return await this.connection.send(methodName, ...args);
    }

    async callHubProxy(methodName: string): Promise<any> {

        if (!this.isOnline) {
            this._cachedRequests.push({
                methodName: methodName,
                invoke: async () => await this.callHubProxy(methodName)
            });
            return;
        }

        return await this.connection.send(methodName);
    }

    private methodCalled(methodName: string, args) {
        // if (environment.detailLogSignalR) {
        //     if (environment.errorLogging) {
        //         console.log("SignalR: " + this.hubName + ":" + methodName);
        //     }
        //     if (args) {
        //         console.log(args);
        //     }
        // }

        if (this[methodName] instanceof EventEmitter) {
            this[methodName].emit(args);
        }
    }
}
