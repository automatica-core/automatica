import { Injectable, EventEmitter } from "@angular/core";
import { BaseHub, SignalRMethod } from "../base-hub";
import { NodeInstance } from "../../model/node-instance";
import { DataService } from "src/app/services/data.service";

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

    constructor(private dataService: DataService) {
        super("dataHub");
    }

    public getCurrentValue(id: string) {
        return this.dataService.getLocalValue(id);
    }

    public subscribeForAll() {
        return super.callHubProxy("subscribeAll");
    }

    public unSubscribeForAll() {
        return super.callHubProxy("unsubscribeAll");
    }

    public setValue(nodeInstance: string, value: any): Promise<any> {
        return this.callHubProxyWithParams("setValue", nodeInstance, value);
    }

    public subscribe(nodeInstance: string) {
        return super.callHubProxyWithParam("subscribe", nodeInstance);
    }
    public unsubscribe(nodeInstance: string) {
        return super.callHubProxyWithParam("unsubscribe", nodeInstance);
    }

    public enableLearnMode(nodeInstance: NodeInstance) {
        return super.callHubProxyWithParam("enableLearnMode", nodeInstance.ObjId);
    }

    public disableLearnMode(nodeInstance: NodeInstance) {
        return super.callHubProxyWithParam("disalbeLearnMode", nodeInstance.ObjId);
    }

}
