import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"


@Model()
export class Control extends BaseModel {

    @JsonProperty()
    public Id: string;

    @JsonProperty()
    public Name: string;

   
    constructor() {
        super();

      
    }
    protected afterFromJson() {
      
    }


    toJson(): { [name: string]: any } {

        return super.toJson();
    }

    protected createInstance(): BaseModel {
        return new Control();
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "Control";
    }
}
