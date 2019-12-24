import { Model, BaseModel, JsonFieldInfo, JsonProperty, JsonPropertyName } from "./base-model";
import { PropertyTemplate } from "./property-template";

@Model()
export class VisuObjectTemplate extends BaseModel {

    private _MaxWidth: number;
    private _MaxHeight: number;
    private _MinWidth: number;
    private _MinHeight: number;

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

    @JsonProperty()
    Width: number;

    @JsonProperty()
    Height: number;

    @JsonProperty()
    This2VisuPageType: number;

    @JsonPropertyName("PropertyTemplate")
    Properties: PropertyTemplate[] = [];

    @JsonProperty()
    IsVisibleForUser: boolean;

    @JsonProperty()
    public get MaxWidth(): number {
        return this._MaxWidth;
    }
    public set MaxWidth(v: number) {
        this._MaxWidth = v;
    }

    @JsonProperty()
    public get MaxHeight(): number {
        return this._MaxHeight;
    }
    public set MaxHeight(v: number) {
        this._MaxHeight = v;
    }

    @JsonProperty()
    public get MinWidth(): number {
        return this._MinWidth;
    }
    public set MinWidth(v: number) {
        this._MinWidth = v;
    }

    @JsonProperty()
    public get MinHeight(): number {
        return this._MinHeight;
    }
    public set MinHeight(v: number) {
        this._MinHeight = v;
    }

    constructor() {
        super();

    }
    public typeInfo(): string {
        return "VisuObjectTemplate";
    }


    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

}
