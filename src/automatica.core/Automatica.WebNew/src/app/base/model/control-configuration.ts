import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"


@Model()
export class ControlConfiguration extends BaseModel {

    @JsonProperty()
    public Switches: string[] = [];

    @JsonProperty()
    public Dimmer: string[] = [];

    @JsonProperty()
    public Blinds: string[] = [];

    @JsonProperty()
    public All: string[] = [];
   
    constructor() {
        super();

      
    }
    protected afterFromJson() {
        this.All = [...this.Switches, ...this.Dimmer, ...this.Blinds];
    }


    toJson(): { [name: string]: any } {

        return super.toJson();
    }

    protected createInstance(): BaseModel {
        return new ControlConfiguration();
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "ControlConfigurationBase";
    }
}
