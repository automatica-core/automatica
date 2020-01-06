import { HostBinding, Input, ElementRef } from "@angular/core";
import { BaseComponent } from "../base-component";
import { VisuObjectMobileInstance } from "../model/visu";
import { DataHubService } from "../communication/hubs/data-hub.service";
import { PropertyInstance } from "../model/property-instance";
import { NotifyService } from "src/app/services/notify.service";
import { TranslationService } from "angular-l10n";
import { ConfigService } from "src/app/services/config.service";
import { NodeInstance } from "../model/node-instance";

export abstract class BaseMobileComponent extends BaseComponent {
    @HostBinding("class.mobile-control") true;

    @Input()
    public item: VisuObjectMobileInstance;

    @Input()
    editMode: boolean = false;

    @Input()
    public parent: ElementRef;

    private subscribedNodeInstances: Map<string, NodeInstance> = new Map<string, NodeInstance>();
    _primaryNodeInstance: any;
    nodeInstanceModel: NodeInstance;

    public get isLoading(): boolean {
        return this.item.isLoading;
    }
    public set isLoading(v: boolean) {
        this.item.isLoading = v;
    }

    protected _value: any;

    public get realValue() {
        return this._value;
    }

    public get value(): any {
        if (this.item.StateTextValueTrue && this._value === true) {
            return this.item.StateTextValueTrue;
        }
        if (this.item.StateTextValueFalse && this._value === false) {
            return this.item.StateTextValueFalse;
        }
        return this._value;
    }

    public set value(value: any) {
        this._value = value;
    }

    public get foregroundColor(): any {

        if (this.item.StateColorValueTrue && this._value === true) {
            return this.item.StateColorValueTrue;
        }
        if (this.item.StateColorValueFalse && this._value === false) {
            return this.item.StateColorValueFalse;
        }

        return this.getPropertyValue("foreground_color");
    }

    public get width() {
        return this.parent.nativeElement.offsetWidth;
    }

    public get height() {
        return this.parent.nativeElement.offsetHeight;
    }

    public get displayText() {
        if (this.item.VisuName) {
            return this.item.VisuName;
        }
        return this.getPropertyValue("text");
    }

    constructor(protected dataHub: DataHubService, notify: NotifyService, translate: TranslationService, private configService: ConfigService) {
        super(notify, translate);

    }

    public get actualHeight(): number {
        return this.parent.nativeElement.offsetHeight;
    }


    getFontSize() {
        return this.getPropertyValue("text_size") + "px";
    }

    public get fontSize() {
        return this.getPropertyValue("text_size");
    }

    getForegroundColor() {
        return this.getPropertyValue("foreground_color");
    }

    public get actualWidth(): number {
        return this.parent.nativeElement.offsetWidth;
    }

    public baseOnInit() {

        if (this.item) {
            if (this.item.itemResized) {
                super.registerEvent((this.item.itemResized), () => {
                    this.onItemResized();
                });
            }

            super.registerEvent((this.item.notifyChangeEvent), (prop) => {
                this.propertyChanged()
            });
        }
    }

    protected propertyChanged() {
        const nodeProperty = this.getProperty("nodeInstance");

        if (!nodeProperty) {
            return;
        }

        this.registerForItemValues(nodeProperty);
    }

    public getPropertyValue(key: string) {
        return this.item.getPropertyValue(key);
    }
    public getProperty(key: string): PropertyInstance {
        return this.item.getProperty(key);
    }

    public async registerForItemValues(nodeProperty: PropertyInstance) {
        super.registerEvent(nodeProperty.propertyChanged, async (value) => {
            const nodeId = this.getPropertyValue("nodeInstance");
            await this.registerForNodeValues(nodeId);
            this._primaryNodeInstance = nodeId;
        });

        super.registerEvent(this.dataHub.dispatchValue, async (args) => {
            const nodeId = args[1];
            if (this.subscribedNodeInstances.has(nodeId) && args[0] === 0) { // check if node instance and dispatchable type is correct

                if (this._primaryNodeInstance === nodeId) {
                    this.value = args[2];
                }
                await this.nodeValueReceived(nodeId, args[2]);
            }
        });

        if (nodeProperty.Value) {
            const nodeId = this.getPropertyValue("nodeInstance");
            this.nodeInstanceModel = await this.registerForNodeValues(nodeId);
            this._primaryNodeInstance = nodeId;
        }
    }

    protected nodeValueReceived(nodeId: string, value: any): Promise<void> {
        return Promise.resolve();
    }

    private async unregisterForNodeValues(nodeId: string) {
        if (nodeId) {
            await this.dataHub.unsubscribe(nodeId);

            if (this.subscribedNodeInstances.has(nodeId)) {
                this.subscribedNodeInstances.delete(nodeId);
            }
        }

    }

    protected async registerForNodeValues(nodeId: string) {
        this.unregisterForNodeValues(nodeId);

        if (nodeId) {
            const node = await this.configService.getSingleNodeInstance(nodeId);

            this.subscribedNodeInstances.set(nodeId, node);

            await this.dataHub.subscribe(nodeId);

            return node;
        }
        return void 0;
    }


    public abstract onItemResized();
}
