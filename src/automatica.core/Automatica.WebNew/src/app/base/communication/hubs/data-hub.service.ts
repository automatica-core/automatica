import { Injectable, EventEmitter } from "@angular/core";
import { BaseHub, SignalRMethod } from "../base-hub";
import { NodeInstance } from "../../model/node-instance";
import { DataService, NodeInstanceValue } from "src/app/services/data.service";



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

    // reference count per node id, the server group is only joined/left on the first/last reference
    private nodeSubscriptions = new Map<string, number>();

    constructor(private dataService: DataService) {
        super("dataHub");
    }

    protected async onConnected(): Promise<void> {
        // a new connection has no server side groups, re-join all currently referenced nodes
        for (const nodeId of Array.from(this.nodeSubscriptions.keys())) {
            try {
                await super.callHubProxyWithParam("subscribe", nodeId);
            } catch (error) {
                console.error(error);
            }
        }
    }

    public getCurrentValue(id: string): NodeInstanceValue {
        return this.dataService.getLocalValue(id);
    }

    public subscribeForAll() {
        return super.callHubProxy("subscribeAll");
    }

    public unSubscribeForAll() {
        return super.callHubProxy("unsubscribeAll");
    }

    public setValue(nodeInstance: string, value: any): Promise<any> {
        // never cache writes, replaying them after a reconnect would re-actuate devices
        return this.sendIfConnected("setValue", nodeInstance, value);
    }

    public async subscribe(nodeInstance: string) {
        const count = this.nodeSubscriptions.get(nodeInstance) ?? 0;
        this.nodeSubscriptions.set(nodeInstance, count + 1);

        // when offline, onConnected subscribes all referenced nodes
        if (count === 0 && this.isOnline) {
            return await super.callHubProxyWithParam("subscribe", nodeInstance);
        }
    }
    public async unsubscribe(nodeInstance: string) {
        const count = this.nodeSubscriptions.get(nodeInstance);
        if (!count) {
            return;
        }
        if (count > 1) {
            this.nodeSubscriptions.set(nodeInstance, count - 1);
            return;
        }
        this.nodeSubscriptions.delete(nodeInstance);

        if (this.isOnline) {
            return await super.callHubProxyWithParam("unsubscribe", nodeInstance);
        }
    }

    public enableLearnMode(nodeInstance: NodeInstance) {
        return super.callHubProxyWithParam("enableLearnMode", nodeInstance.ObjId);
    }

    public disableLearnMode(nodeInstance: NodeInstance) {
        return super.callHubProxyWithParam("disableLearnMode", nodeInstance.ObjId);
    }

}
