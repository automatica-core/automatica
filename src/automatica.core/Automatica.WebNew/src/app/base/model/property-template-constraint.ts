import { BaseModel, Model, JsonFieldInfo, JsonPropertyName } from "./base-model";
import { PropertyTemplateConstraintData } from "./property-template-constraint-data";

export enum PropertyConstraintType {
    None,
    Unique,
    UniqueOnCondition,
    Visible
}

export enum PropertyConstraintLevel {
    None,
    Info,
    Warn,
    Error
}

@Model()
export class PropertyTemplateConstraint extends BaseModel {


    @JsonPropertyName("ObjId")
    public ObjId: string;

    @JsonPropertyName("Name")
    public Name: string;

    @JsonPropertyName("Description")
    public Description: string;

    @JsonPropertyName("ConstraintType")

    public ConstraintType: PropertyConstraintType;

    @JsonPropertyName("This2PropertyTemplate")
    public This2PropertyTemplate: string;

    @JsonPropertyName("ConstraintData")
    public ConstraintData: PropertyTemplateConstraintData[] = [];

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    protected createInstance(): BaseModel {
        return new PropertyTemplateConstraint();
    }
    public typeInfo(): string {
        return "PropertyTemplateConstraint";
    }


}
