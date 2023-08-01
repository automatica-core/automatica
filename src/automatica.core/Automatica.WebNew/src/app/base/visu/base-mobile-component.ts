import { HostBinding, Input, ElementRef, Directive } from "@angular/core";
import { BaseComponent } from "../base-component";
import { VisuObjectMobileInstance } from "../model/visu";
import { DataHubService } from "../communication/hubs/data-hub.service";
import { PropertyInstance } from "../model/property-instance";
import { NotifyService } from "src/app/services/notify.service";
import { L10nTranslationService } from "angular-l10n";
import { ConfigService } from "src/app/services/config.service";
import { NodeInstance } from "../model/node-instance";
import { AppService } from "src/app/services/app.service";
import { AreaInstance } from "../model/areas";
import { CategoryInstance } from "../model/categories";
import { VisuPageGroupType } from "../model/visu-page";
import { NodeDataTypeEnum } from "../model/node-data-type";
import * as moment from "moment";

export interface VisuObjectType {
    This2AreaInstanceNavigation: AreaInstance;
    This2CategoryInstanceNavigation: CategoryInstance;

    DisplayName: string;
    IsFavorite: boolean;
}

@Directive()
export abstract class BaseMobileComponent extends BaseComponent {
    @HostBinding("class.mobile-control") true;

    @Input()
    public item: VisuObjectMobileInstance;

    public get visuObjectType() {
        return this.item.objectType;
    }

    @Input()
    editMode: boolean = false;

    @Input()
    public parent: ElementRef;

    @Input()
    pageGroupType: VisuPageGroupType = VisuPageGroupType.Favorites;

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

        if (this.nodeInstanceModel) {
            switch (this.nodeInstanceModel.NodeTemplate.This2NodeDataType) {
                case NodeDataTypeEnum.DateTime:
                    const dateTime = moment(this._value).toDate();

                    return dateTime.toLocaleString();
                default:
                    return this._value;
            }
        }

        return this._value;
    }

    public set value(value: any) {
        this._value = value;
    }

    public get width() {
        return this.parent.nativeElement.offsetWidth;
    }

    public get height() {
        return this.parent.nativeElement.offsetHeight;
    }

    public get displayText() {
        return this.visuObjectType.DisplayName;
    }

    public get icon() {

        switch (this.pageGroupType) {
            case VisuPageGroupType.Category:
                if (this.visuObjectType.This2CategoryInstanceNavigation) {
                    return this.visuObjectType.This2CategoryInstanceNavigation.Icon;
                }
                break;
            case VisuPageGroupType.Area:
                if (this.visuObjectType.This2AreaInstanceNavigation) {
                    return this.visuObjectType.This2AreaInstanceNavigation.Icon;
                }
                break;
            case VisuPageGroupType.Favorites:
                if (this.visuObjectType.This2CategoryInstanceNavigation) {
                    return this.visuObjectType.This2CategoryInstanceNavigation.Icon;
                }
                return "star";
        }

        return void 0;
    }

    public get location() {
        if (this.visuObjectType.This2AreaInstanceNavigation) {
            return this.visuObjectType.This2AreaInstanceNavigation.DisplayName;
        }


        if (this.visuObjectType.IsFavorite) {
            return this.translate.translate("COMMON.PROPERTY.IS_FAVORITE.NAME");
        }
        return void 0;
    }

    constructor(
        protected dataHub: DataHubService,
        notify: NotifyService,
        translate: L10nTranslationService,
        private configService: ConfigService,
        appService: AppService) {
        super(notify, translate, appService);

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
    
    getReadOnly() {
        return this.getPropertyValue("readonly");
    }

    public get actualWidth(): number {
        return this.parent.nativeElement.offsetWidth;
    }

    public baseOnInit() {

        if (this.item) {
            this.propertyChanged();
            const value = this.dataHub.getCurrentValue(this.item.ObjId);;
            if(value)
                this.value = value.value;
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
                    this.value = args[2].value;
                    console.log("nodevalue dispatch: ", this.value);
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
}
