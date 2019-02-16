import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"
import { BoardType } from "./board-type"
import { InterfaceType } from "./interface-type"
import { NodeInstance } from "./node-instance"
import { ITreeNode } from "./ITreeNode";
import { PropertyInstance } from "./property-instance";
import { VirtualNamePropertyInstance } from "./virtual-props/virtual-name-property-instance";
import { VirtualDescriptionPropertyInstance } from "./virtual-props/virtual-description-property-instance";
import { INameModel } from "./INameModel";
import { IDescriptionModel } from "./IDescriptionModel";
import { IPropertyModel } from "./interfaces/ipropertyModel";

@Model()
export class BoardInterface extends BaseModel implements ITreeNode, INameModel, IDescriptionModel, IPropertyModel {
    ValidationOk: boolean = true;

    Parent: ITreeNode;

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    Name: string;


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


    private _displayName: string;
    public get DisplayName() {
        if (this._displayName) {
            return this._displayName;
        }
        return this.Name;
    }
    public set DisplayName(value: string) {
        this._displayName = value;
    }

    @JsonProperty()
    Description: string;

    @JsonProperty()
    This2BoardType: string;

    @JsonProperty()
    This2InterfaceType: string;

    @JsonProperty()
    BoardType: BoardType;

    @JsonPropertyName("This2InterfaceTypeNavigation")
    InterfaceType: InterfaceType;

    @JsonProperty()
    Meta: string;

    Children: ITreeNode[] = [];

    Properties: PropertyInstance[] = [];

    public get Id() {
        return this.ObjId;
    }

    public get ParentId() {
        return "b0";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "BoardInterface";
    }

    getPropertyValue(property: string) {
        return void 0;
    }
    getPropertyValueById(property: string) {
        return void 0;
    }

    validate(): boolean {
        return true;
    }

    protected afterFromJson() {
        this.Properties.push(new VirtualNamePropertyInstance(this, true));
        this.Properties.push(new VirtualDescriptionPropertyInstance(this, true));
    }
}
