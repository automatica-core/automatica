import { BaseModel, JsonFieldInfo, JsonProperty, Model } from "./base-model"


export class ControlGrouped {
    key: string;
    items: Control[];
  }

@Model()
export class Control extends BaseModel {

    @JsonProperty()
    public Id: string;

    @JsonProperty()
    public Name: string;

    @JsonProperty()
    public Key: string;

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
