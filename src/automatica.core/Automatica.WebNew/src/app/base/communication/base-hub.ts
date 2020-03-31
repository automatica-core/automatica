import { EventEmitter } from "@angular/core";
import * as signalR from "@aspnet/signalr";

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
}

export class BaseHub {

    public reconnectTime = 10000;

    protected hubName: string;
    private subscriptions: any[] = [];
    private connection: signalR.HubConnection;
    private started = false;

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
        this.init();
    }

    public init() {
        const methods: string[] = METHODS.get(this.constructor.name);

        if (methods == null) {
            return;
        }
        if (this.hubName == null) {
            return;
        }

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/signalr/" + this.hubName, { accessTokenFactory: () => localStorage.getItem("jwt") })
            .configureLogging(signalR.LogLevel.None)
            .build();

        for (const method of methods) {
            this.connection.on(method, (...args: any[]) => this.methodCalled(method, args));
        }

        this.connection.onclose((error) => {
            setTimeout(async () => {
                await this.reconnect();
            }, this.reconnectTime);
        });
    }

    public async reconnect() {
        console.log("signalr connection lost, try to reinit");

        if (this.connection.state !== signalR.HubConnectionState.Connected) {
            this.setConnectedState(false);
            await this.start().catch(() => {
                setTimeout(async () => {
                    await this.reconnect();
                }, this.reconnectTime);
            });
        }
    }

    public async start() {
        if (!localStorage.getItem("jwt")) {
            return;
        }
        if (!this.started) {

            try {
                await this.connection.start();

                for (const cached of this._cachedRequests) {
                    await this.connection.invoke(cached.methodName, cached.params);
                }

                this.setConnectedState(true);
            } catch (error) {
                throw error;
            }
            return true;
        }
        return false;
    }

    public async stop() {
        if (this.started) {
            await this.connection.stop();
            this.unregisterAll();
            this.started = false;
        }
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

    callHubProxyWithParam(methodName: string, params: any): Promise<any> {

        if (!this.started) {
            this._cachedRequests.push({
                methodName: methodName,
                params: params
            });
            return;
        }

        const proxy = this.connection;
        return proxy.invoke(methodName, params);
    }

    callHubProxyWithParams(methodName: string, ...params: any[]): Promise<any> {

        if (!this.started) {
            this._cachedRequests.push({
                methodName: methodName,
                params: params
            });
            return;
        }

        const proxy = this.connection;
        return proxy.send(methodName, params);
    }

    callHubProxy(methodName: string): Promise<any> {

        if (!this.started) {
            this._cachedRequests.push({
                methodName: methodName
            });
            return;
        }

        const proxy = this.connection;
        return proxy.send(methodName);
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
