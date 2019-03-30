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

    nodeInstanceModel: NodeInstance;

    public get isLoading(): boolean {
        return this.item.isLoading;
    }
    public set isLoading(v: boolean) {
        this.item.isLoading = v;
    }


    private _nodeInstance: string;

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
            await this.registerForNodeValues();
        });

        super.registerEvent(this.dataHub.dispatchValue, (args) => {
            if (args[1] === this._nodeInstance && args[0] === 0) { // check if node instance and dispatchable type is correct
                this.value = args[2];
            }
        });

        if (nodeProperty.Value) {
            await this.registerForNodeValues();
        }
    }

    private async unregisterForNodeValues() {
        if (this._nodeInstance) {
            await this.dataHub.unsubscribe(this._nodeInstance);
        }

    }
    private async registerForNodeValues() {

        this.unregisterForNodeValues();
        const nodeProperty = this.getPropertyValue("nodeInstance");

        if (nodeProperty) {
            this.nodeInstanceModel = await this.configService.getSingleNodeInstance(nodeProperty);

            await this.dataHub.subscribe(nodeProperty);
            this._nodeInstance = nodeProperty;
        }
    }


    public abstract onItemResized();
}
