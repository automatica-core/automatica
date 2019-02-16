import { Model, BaseModel, JsonFieldInfo, JsonProperty } from "../base-model";
import { IPropertyModel } from "../interfaces/ipropertyModel";
import { INameModel } from "../INameModel";
import { IDescriptionModel } from "../IDescriptionModel";
import { PropertyInstance } from "../property-instance";

@Model()
export class CategoryGroup extends BaseModel implements IPropertyModel, INameModel, IDescriptionModel {

    Properties: PropertyInstance[] = [];


    public get DisplayName(): string {
        return this.translationService.translate(this.Name);
    }
    public set DisplayName(v: string) {
        this.Name = v;
    }

    public get DisplayDescription(): string {
        return this.translationService.translate(this.Description);
    }
    public set DisplayDescription(v: string) {
        this.Description = v;
    }

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;


    constructor() {
        super();

    }


    public typeInfo(): string {
        return "CategoryGroup";
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

}
