import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"
import { BoardInterface } from "./board-interface"
import { ITreeNode } from "./ITreeNode";
import { PropertyInstance } from "./property-instance";
import { VirtualNamePropertyInstance } from "./virtual-props/virtual-name-property-instance";
import { VirtualDescriptionPropertyInstance } from "./virtual-props/virtual-description-property-instance";
import { INameModel } from "./INameModel";
import { IDescriptionModel } from "./IDescriptionModel";
import { IPropertyModel } from "./interfaces/ipropertyModel";

@Model()
export class BoardType extends BaseModel implements ITreeNode, INameModel, IDescriptionModel, IPropertyModel {
    Icon: string = "automatica-logo";
    Validate: boolean = false;
    Value?: any;
    Parent: ITreeNode;
    ValidationOk: boolean = true;

    public get Id() {
        return "b0";
    }

    public get ParentId() {
        return void 0;
    }

    @JsonProperty()
    Type: string;

    @JsonProperty()
    Name: string;

    public get DisplayName() {
        return this.Name;
    }



    @JsonProperty()
    Description: string;

    @JsonPropertyName("BoardInterface")
    Interfaces: BoardInterface[];

    Properties: PropertyInstance[] = [];
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


    Children: ITreeNode[] = [];

    protected afterFromJson() {
        this.Properties.push(new VirtualNamePropertyInstance(this, true));
        this.Properties.push(new VirtualDescriptionPropertyInstance(this, true));
    }

    protected createInstance(): BaseModel {
        return new BoardType();
    }
    public typeInfo(): string {
        return "BoardType";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
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
}
