import { BaseModel, Model, JsonFieldInfo, JsonProperty } from "./base-model";
;
import { VisuObjectInstance } from "./visu-object-instance";
import { IPropertyModel } from "./interfaces/ipropertyModel";
import { INameModel } from "./INameModel";
import { IDescriptionModel } from "./IDescriptionModel";
import { PropertyInstance } from "./property-instance";
import { VirtualNamePropertyInstance } from "./virtual-props/virtual-name-property-instance";
import { VirtualDescriptionPropertyInstance } from "./virtual-props/virtual-description-property-instance";
import { VirtualVisuDefaultPageProperty } from "./visu/virtual-visu-default-page-property";
import { EventEmitter } from "@angular/core";
import { IVisuPage } from "./visu/visu-page-interface";
import { Guid } from "../utils/Guid";
import { VirtualVisuPageHeightProperty } from "./virtual-props/visu-page/virtual-visu-page-height-property";
import { VirtualVisuPageWidthProperty } from "./virtual-props/visu-page/virtual-visu-page-width-property";
import { VirtualUserGroupPropertyInstance } from "./virtual-props/virtual-usergroup-property-instance";

export enum VisuPageType {
    PC = 1,
    Mobile = 2
}

@Model()
export class VisuPage extends BaseModel implements INameModel, IDescriptionModel, IPropertyModel, IVisuPage {

    public defaultPageChange = new EventEmitter<VisuPage>();
    public heightWidthChange = new EventEmitter<void>();
    public VisuObjectInstancesChange = new EventEmitter<VisuObjectInstance[]>();
    private _defaultPage: boolean;

    Properties: PropertyInstance[] = [];

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    This2VisuPageType: number;

    private _VisuObjectInstances: VisuObjectInstance[] = [];
    public get VisuObjectInstances(): VisuObjectInstance[] {
        return this._VisuObjectInstances;
    }
    @JsonProperty()
    public set VisuObjectInstances(v: VisuObjectInstance[]) {
        this._VisuObjectInstances = v;
        this.VisuObjectInstancesChange.emit(v);
    }


    @JsonProperty()
    Height: number;

    @JsonProperty()
    Width: number;

    @JsonProperty()
    IsFavorite: boolean;

    @JsonProperty()
    Rating: number;

    @JsonProperty()
    This2UserGroup: string;

    private _DisplayDescription: string;
    public get DisplayDescription(): string {
        if (!this._DisplayDescription) {
            return this.Description;
        }
        return this._DisplayDescription;
    }
    public set DisplayDescription(v: string) {
        this._DisplayDescription = v;
    }


    constructor() {
        super();
    }

    @JsonProperty()
    public get DefaultPage(): boolean {
        return this._defaultPage;
    }
    public set DefaultPage(v: boolean) {
        this._defaultPage = v;
    }

    public typeInfo() {
        return "VisuPage";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    protected afterFromJson() {
        this.addVirtualProperties();
    }

    public get DisplayName(): string {
        return this.Name;
    }

    private addVirtualProperties() {
        this.Properties.push(new VirtualNamePropertyInstance(this));
        this.Properties.push(new VirtualDescriptionPropertyInstance(this));


        const heightProperty = new VirtualVisuPageHeightProperty(this);
        const widthProperty = new VirtualVisuPageWidthProperty(this);

        heightProperty.notifyChangeEvent.subscribe((changed) => {
            this.heightWidthChange.emit();
        });
        widthProperty.notifyChangeEvent.subscribe((changed) => {
            this.heightWidthChange.emit();
        });

        this.Properties.push(heightProperty);
        this.Properties.push(widthProperty);

        const virtualDefaultPage = new VirtualVisuDefaultPageProperty(this);
        this.Properties.push(virtualDefaultPage);

        virtualDefaultPage.defaultPageChange.subscribe((value) => {
            this.defaultPageChange.emit(this);
        });

        this.Properties.push(new VirtualUserGroupPropertyInstance(this));
    }

    validate(): boolean {
        return true;
    }
}
