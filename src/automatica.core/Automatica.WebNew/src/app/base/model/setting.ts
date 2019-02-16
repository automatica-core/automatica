import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"
import { PropertyTemplateType } from "./property-template";
import * as moment from "moment";

@Model()
export class Setting extends BaseModel {

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    ValueText: string;

    @JsonProperty()
    ValueKey: string;

    @JsonProperty()
    ValueInt?: number;

    @JsonProperty()
    ValueDouble?: number;

    @JsonProperty()
    Group: string;

    @JsonProperty()
    Type: number;

    @JsonProperty()
    IsVisible: boolean;

    @JsonProperty()
    IsReadonly: boolean;

    get Value(): any {
        switch (this.Type) {
            case PropertyTemplateType.Long:
                return this.ValueInt;
            case PropertyTemplateType.Text:
            case PropertyTemplateType.Parity:
            case PropertyTemplateType.Interface:
            case PropertyTemplateType.Ip:
            case PropertyTemplateType.Color:
            case PropertyTemplateType.UsbPort:
            case PropertyTemplateType.AreaIcon:
            case PropertyTemplateType.Password:
                return this.ValueText;
            case PropertyTemplateType.Time:
                return moment(this.ValueText).toDate();
            case PropertyTemplateType.Integer:
            case PropertyTemplateType.Enum:
            case PropertyTemplateType.DropDown:
            case PropertyTemplateType.Baudrate:
            case PropertyTemplateType.Databits:
            case PropertyTemplateType.Range:
                return this.ValueInt;
            case PropertyTemplateType.Stopbits:
            case PropertyTemplateType.Numeric:
                return this.ValueDouble;
            case PropertyTemplateType.Bool:
                return !!this.ValueInt;
        }

        return void 0;
    }

    set Value(value: any) {
        switch (this.Type) {
            case PropertyTemplateType.Long:
                this.ValueInt = value;
                break;
            case PropertyTemplateType.Text:
            case PropertyTemplateType.Parity:
            case PropertyTemplateType.Interface:
            case PropertyTemplateType.Ip:
            case PropertyTemplateType.Color:
            case PropertyTemplateType.UsbPort:
            case PropertyTemplateType.AreaIcon:
            case PropertyTemplateType.Password:
                this.ValueText = value;
                break;
            case PropertyTemplateType.Time:
                this.ValueText = moment(value).toISOString(true);
                break;
            case PropertyTemplateType.DropDown:
            case PropertyTemplateType.Baudrate:
            case PropertyTemplateType.Databits:
            case PropertyTemplateType.Integer:
            case PropertyTemplateType.Enum:
            case PropertyTemplateType.Range:
                this.ValueInt = parseInt(value, 10);
                if (isNaN(this.ValueInt)) {
                    this.ValueInt = 0;
                }
                break;
            case PropertyTemplateType.Stopbits:
            case PropertyTemplateType.Numeric:

                this.ValueDouble = parseFloat(value);
                if (isNaN(this.ValueDouble)) {
                    this.ValueDouble = 0;
                }
                break;
            case PropertyTemplateType.Bool:
                this.ValueInt = value ? 1 : 0;
                break;
        }
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "Setting";
    }
}
