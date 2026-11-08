import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"
import { NodeDataType, NodeDataTypeEnum } from "./node-data-type";
import { InterfaceType } from "./interface-type"
import { PropertyType } from "./property-type"
import { PropertyTemplate } from "./property-template"
import { NodeInstance } from "./node-instance";
import { BoardInterface } from "./board-interface";

/** VisuMobileObjectTemplateTypes.Label, the default tile of every node template */
export const LABEL_VISU_TEMPLATE = "16780dfd-887a-4a0a-9b2a-4d62ccc32c93";

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

    NodeTypeEnum: NodeDataTypeEnum;

    @JsonPropertyName("ProvidesInterface2InterfaceTypeNavigation")
    ProvidesInterface: InterfaceType;

    @JsonPropertyName("NeedsInterface2InterfacesTypeNavigation")
    NeedsInterface: InterfaceType;

    @JsonPropertyName("PropertyTemplate")
    Properties: PropertyTemplate[] = [];

    @JsonProperty()
    This2DefaultMobileVisuTemplate: string;

    /**
     * true if the node has a tile of its own in the generated visu (e.g. the camera of the camera driver), so it can be put
     * in an area or category although it has no value. Every other node defaults to the label tile
     */
    public get HasOwnVisuTile(): boolean {
        return !!this.This2DefaultMobileVisuTemplate && this.This2DefaultMobileVisuTemplate.toLowerCase() !== LABEL_VISU_TEMPLATE;
    }


    public get DisplayName() {
        return this.translationService.translate(this.Name);
    }

    protected afterFromJson(): void {
        this.NodeTypeEnum = <NodeDataTypeEnum>this.This2NodeDataType;
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
