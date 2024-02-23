import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"
import { BoardType } from "./board-type"
import { InterfaceType } from "./interface-type"
import { ITreeNode } from "./ITreeNode";
import { PropertyInstance } from "./property-instance";
import { VirtualNamePropertyInstance } from "./virtual-props/virtual-name-property-instance";
import { VirtualDescriptionPropertyInstance } from "./virtual-props/virtual-description-property-instance";
import { INameModel } from "./INameModel";
import { IDescriptionModel } from "./IDescriptionModel";
import { IPropertyModel } from "./interfaces/ipropertyModel";

@Model()
export class BoardInterface extends BaseModel implements ITreeNode, INameModel, IDescriptionModel, IPropertyModel {
    Icon: string = "automatica-logo";
    Validate: boolean = false;
    Value?: any;
    ValidationOk: boolean = true;

    public Parent: ITreeNode;

    @JsonProperty()
    public ObjId: string;

    @JsonProperty()
    public Name: string;


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
    public Description: string;

    @JsonProperty()
    public This2BoardType: string;

    @JsonProperty()
    public This2InterfaceType: string;

    @JsonProperty()
    public BoardType: BoardType;

    @JsonPropertyName("This2InterfaceTypeNavigation")
    public InterfaceType: InterfaceType;

    @JsonProperty()
    public Meta: string;

    public Children: ITreeNode[] = [];

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

    public getPropertyValue(property: string) {
        return void 0;
    }
    public getPropertyValueById(property: string) {
        return void 0;
    }

    public validate(): boolean {
        return true;
    }

    protected afterFromJson() {
        this.Properties.push(new VirtualNamePropertyInstance(this, true));
        this.Properties.push(new VirtualDescriptionPropertyInstance(this, true));
    }
}
