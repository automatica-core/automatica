import { Injectable, EventEmitter } from "@angular/core";
import { Observable, Subject } from "rxjs";
import { BaseHub, SignalRMethod } from "../base-hub";
import { NodeInstance } from "../../model/node-instance";
import { DataService, NodeInstanceValue } from "src/app/services/data.service";



@Injectable()
export class DataHubService extends BaseHub {

    /**
     * Every value the server pushes ([type, id, value]). Controls that only need some nodes should use nodeValues().
     */
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
    // groups that have to be re-joined after a reconnect, a new connection has no server side groups
    private subscribedToAll = false;
    private learnModeNodes = new Set<string>();

    // per node routing, each control only gets the values of its own nodes
    private valueSubjects = new Map<string, Subject<any[]>>();
    private refreshingValues: Promise<void>;

    constructor(private dataService: DataService) {
        super("dataHub");

        this.dispatchValue.subscribe((args: any[]) => {
            const nodeId = args?.[1];
            if (nodeId === undefined) {
                return;
            }
            this.dataService.setLocalValue(nodeId, args[2]);
            this.valueSubjects.get(nodeId)?.next(args);
        });
    }

    protected async onConnected(): Promise<void> {
        // a new connection has no server side groups, re-join everything that is currently referenced
        for (const nodeId of Array.from(this.nodeSubscriptions.keys())) {
            try {
                await super.callHubProxyWithParam("subscribe", nodeId);
            } catch (error) {
                console.error(error);
            }
        }
        if (this.subscribedToAll) {
            try {
                await super.callHubProxy("subscribeAll");
            } catch (error) {
                console.error(error);
            }
        }
        for (const nodeId of Array.from(this.learnModeNodes)) {
            try {
                await super.callHubProxyWithParam("enableLearnMode", nodeId);
            } catch (error) {
                console.error(error);
            }
        }

        // values that changed while disconnected were never pushed
        this.refreshValues().catch((error) => console.error(error));
    }

    /**
     * Reloads the value snapshot from the server and pushes every value that changed to the nodeValues() subscribers.
     */
    public refreshValues(): Promise<void> {
        if (!this.refreshingValues) {
            this.refreshingValues = this.doRefreshValues().finally(() => this.refreshingValues = undefined);
        }
        return this.refreshingValues;
    }

    private async doRefreshValues() {
        const before = new Map<string, NodeInstanceValue>();
        for (const nodeId of Array.from(this.valueSubjects.keys())) {
            before.set(nodeId, this.dataService.getLocalValue(nodeId));
        }

        await this.dataService.getAllValues();

        for (const [nodeId, oldValue] of Array.from(before.entries())) {
            const value = this.dataService.getLocalValue(nodeId);
            if (value === undefined || DataHubService.sameValue(oldValue, value)) {
                continue;
            }
            // the snapshot has no type, the per node subscribers only use id and value
            this.valueSubjects.get(nodeId)?.next([undefined, nodeId, value]);
        }
    }

    private static sameValue(a: NodeInstanceValue, b: NodeInstanceValue) {
        if (!a || !b) {
            return a === b;
        }
        return a.value === b.value && String(a.timestamp) === String(b.timestamp);
    }

    /**
     * Values ([type, id, value]) of the given nodes only.
     */
    public nodeValues(nodeIds: string | string[]): Observable<any[]> {
        const ids = (Array.isArray(nodeIds) ? nodeIds : [nodeIds]).filter(a => !!a);

        return new Observable<any[]>(subscriber => {
            const subscriptions = Array.from(new Set(ids)).map(id => {
                let subject = this.valueSubjects.get(id);
                if (!subject) {
                    subject = new Subject<any[]>();
                    this.valueSubjects.set(id, subject);
                }
                const subscription = subject.subscribe(subscriber);
                return { id, subject, subscription };
            });

            return () => {
                for (const s of subscriptions) {
                    s.subscription.unsubscribe();
                    if (!s.subject.observed && this.valueSubjects.get(s.id) === s.subject) {
                        this.valueSubjects.delete(s.id);
                    }
                }
            };
        });
    }

    public getCurrentValue(id: string): NodeInstanceValue {
        return this.dataService.getLocalValue(id);
    }

    public async subscribeForAll() {
        this.subscribedToAll = true;
        // when offline, onConnected joins the group
        if (this.isOnline) {
            return await super.callHubProxy("subscribeAll");
        }
    }

    public async unSubscribeForAll() {
        this.subscribedToAll = false;
        if (this.isOnline) {
            return await super.callHubProxy("unsubscribeAll");
        }
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

    public async enableLearnMode(nodeInstance: NodeInstance) {
        this.learnModeNodes.add(nodeInstance.ObjId);
        // when offline, onConnected enables it
        if (this.isOnline) {
            return await super.callHubProxyWithParam("enableLearnMode", nodeInstance.ObjId);
        }
    }

    public async disableLearnMode(nodeInstance: NodeInstance) {
        this.learnModeNodes.delete(nodeInstance.ObjId);
        if (this.isOnline) {
            return await super.callHubProxyWithParam("disableLearnMode", nodeInstance.ObjId);
        }
    }

    public override async stop() {
        // logout: nothing may be re-joined for the next user
        this.subscribedToAll = false;
        this.learnModeNodes.clear();
        await super.stop();
    }

}
