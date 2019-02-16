import { BaseModel, Model, JsonFieldInfo, JsonPropertyName } from "./base-model";

export enum PropertyConstraintConditionType {
    None,
    Unique,
    UniqueRange,
    ParentCondition
}

@Model()
export class PropertyTemplateConstraintData extends BaseModel {
    @JsonPropertyName("ObjId")
    public ObjId: string;

    @JsonPropertyName("ConditionType")
    public ConditionType: PropertyConstraintConditionType;

    @JsonPropertyName("Factor")
    public Factor: number;

    @JsonPropertyName("Offset")
    public Offset: number;

    @JsonPropertyName("PropertyKey")
    public Key: string;

    public conditionResult: boolean = true;

    protected createInstance(): BaseModel {
        return new PropertyTemplateConstraintData();
    }
    public typeInfo(): string {
        return "PropertyTemplateConstraintData";
    }



    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

}
