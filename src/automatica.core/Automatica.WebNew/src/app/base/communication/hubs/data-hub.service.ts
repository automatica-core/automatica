import { Injectable, EventEmitter } from "@angular/core";
import { BaseHub, SignalRMethod } from "../base-hub";
import { NodeInstance } from "../../model/node-instance";

@Injectable()
export class DataHubService extends BaseHub {

    @SignalRMethod
    public dispatchValue: EventEmitter<any> = new EventEmitter<any>();

    @SignalRMethod
    public serverStateChanged: EventEmitter<any> = new EventEmitter<any>();

    @SignalRMethod
    public ruleInstanceValueChanged: EventEmitter<any> = new EventEmitter<any>();

    @SignalRMethod
    public notifyLearnMode: EventEmitter<any> = new EventEmitter<any>();

    constructor() {
        super("dataHub");
    }

    public subscribeForAll() {
        return super.callHubProxy("subscribeAll");
    }

    public unSubscribeForAll() {
        return super.callHubProxy("unsubscribeAll");
    }

    public setValue(nodeInstance: string, value: any): Promise<any> {
        return this.Connection.send("setValue", nodeInstance, value);
    }

    public subscribe(nodeInstance: string) {
        super.callHubProxyWithParam("subscribe", nodeInstance);
    }
    public unsubscribe(nodeInstance: string) {
        super.callHubProxyWithParam("unsubscribe", nodeInstance);
    }

    public enableLearnMode(nodeInstance: NodeInstance) {
        super.callHubProxyWithParam("enableLearnMode", nodeInstance.toJson());
    }

    public disableLearnMode(nodeInstance: NodeInstance) {
        super.callHubProxyWithParam("disalbeLearnMode", nodeInstance.toJson());
    }

}
