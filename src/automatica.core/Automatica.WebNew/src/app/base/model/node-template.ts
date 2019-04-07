import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"
import { NodeDataType } from "./node-data-type";
import { InterfaceType } from "./interface-type"
import { PropertyType } from "./property-type"
import { PropertyTemplate } from "./property-template"
import { NodeInstance } from "./node-instance";
import { BoardInterface } from "./board-interface";

@Model()
export class NodeTemplate extends BaseModel {
    @JsonProperty()
    ObjId: string;

    private _name: string;
    @JsonProperty()
    public get Name(): string {
        return this._name;
    }
    public set Name(v: string) {
        this._name = v;
    }


    @JsonProperty()
    Description: string;

    @JsonProperty()
    NeedsInterface2InterfacesType: string;

    @JsonProperty()
    ProvidesInterface2InterfaceType: string;

    @JsonProperty()
    IsDeleteable: boolean;

    @JsonProperty()
    DefaultCreated: boolean;

    @JsonProperty()
    IsReadable: boolean;

    @JsonProperty()
    IsReadableFixed: boolean;

    @JsonProperty()
    IsWriteable: boolean;

    @JsonProperty()
    IsWriteableFixed: boolean;

    @JsonProperty()
    This2NodeDataType: number;
    @JsonProperty()
    MaxInstances: number;

    @JsonProperty()
    NameMeta: string;

    @JsonPropertyName("This2NodeDataTypeNavigation")
    NodeType: NodeDataType;

    @JsonPropertyName("ProvidesInterface2InterfaceTypeNavigation")
    ProvidesInterface: InterfaceType;

    @JsonPropertyName("NeedsInterface2InterfacesTypeNavigation")
    NeedsInterface: InterfaceType;

    @JsonPropertyName("PropertyTemplate")
    Properties: PropertyTemplate[] = [];

    @JsonProperty()
    This2DefaultMobileVisuTemplate: string;


    public get DisplayName() {
        return this.translationService.translate(this.Name);
    }

    public static ValueInterfaceId(): string {
        return "00000000-0000-0000-0000-000000000001";
    }

    public isDriverNode() {
        if (this.ProvidesInterface && this.ProvidesInterface.IsDriverInterface) {
            return true;
        }
        return false;
    }

    protected useBaseModelInstanceForJson(baseModel: BaseModel): boolean {

        return true;
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "NodeTemplate";
    }
}
