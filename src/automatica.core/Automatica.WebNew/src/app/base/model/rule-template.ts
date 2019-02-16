import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"
import { RuleInterfaceTemplate } from "./rule-interface-template"

function sortBySortOrder(a: RuleInterfaceTemplate, b: RuleInterfaceTemplate) {
    return a.SortOrder - b.SortOrder;
};

@Model()
export class RuleTemplate extends BaseModel {

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    Key: string;

    @JsonProperty()
    Group: string;

    @JsonPropertyName("RuleInterfaceTemplate")
    Interfaces: RuleInterfaceTemplate[] = [];

    @JsonProperty()
    Width: number;

    @JsonProperty()
    Height: number;

    @JsonProperty()
    This2DefaultMobileVisuTemplate: string;

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    get Inputs(): RuleInterfaceTemplate[] {
        const interfaces = new Array<RuleInterfaceTemplate>();
        this.Interfaces.forEach(element => {
            if (element.InterfaceDirection.Key === "I") {
                interfaces.push(element);
            }
        });

        return interfaces;
    }

    get Outputs(): RuleInterfaceTemplate[] {
        const interfaces = new Array<RuleInterfaceTemplate>();
        this.Interfaces.forEach(element => {
            if (element.InterfaceDirection.Key === "O") {
                interfaces.push(element);
            }
        });

        return interfaces;
    }


    protected afterFromJson() {
        this.Interfaces.sort(sortBySortOrder);
    }

    public typeInfo(): string {
        return "RuleTemplate";
    }
}
