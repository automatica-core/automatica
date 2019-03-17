import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"
import { PropertyType } from "./property-type";
import { NodeTemplate } from "./node-template";
import { PropertyTemplateConstraint } from "./property-template-constraint";


export function PropertyTemplateTypeAware(constructor: Function) {
    constructor.prototype.PropertyTemplateType = PropertyTemplateType;
}

export enum PropertyTemplateType {
    Text = 0,
    Integer = 1,
    Numeric = 2,
    DropDown = 3,
    Bool = 4,
    Ip = 5,
    Interface = 6,
    Baudrate = 7,
    Parity = 8,
    Databits = 9,
    Stopbits = 10,

    Scan = 11,
    Enum = 12,
    Range = 13,

    Color = 14,
    NodeInstance = 15,
    VisuPage = 16,
    UsbPort = 17,
    Long = 18,
    Password = 19,
    AreaInstanceLink = 20,
    CategoryInstanceLink = 21,
    ImportData = 22,
    LearnMode = 23,
    Timer = 24,
    Time = 25,

    AreaIcon = 100,
    AreaInstance = 101,
    CategoryInstance = 102,

    User2Groups = 200,
    User2Roles = 201,
    UserGroup2Roles = 202,
    UserGroup = 203,

    Invalid = 2147483647
}


export class DefaultExtendedPropertyTemplate {
    constructor(protected template: PropertyTemplate) {

    }
}

export class EnumExtendedPropertyTemplate extends DefaultExtendedPropertyTemplate {

    public items: any[] = [];

    static createFromEnum(data): string {
        const objects: { id: number; name: string }[] = [];
        for (const n in data) {
            if (typeof data[n] === "number") {
                objects.push({ id: <any>data[n], name: n });
            }
        }

        let retString = "ENUM(";
        for (const obj of objects) {
            retString += obj.id + "," + obj.name + ";";
        }
        // remove last ;
        retString = retString.substring(0, retString.length - 1)
        return retString + ")";
    }



    constructor(template: PropertyTemplate) {
        super(template);

        let meta = template.Meta;

        if (!meta.startsWith("ENUM(")) {
            console.log("meta data error, enum metadata invalid (", meta, ")");
        }

        meta = meta.slice(5, meta.length - 1);

        const split = meta.split(";");

        split.forEach(v => {
            const split2 = v.split(",");

            this.items.push({
                id: +split2[0],
                name: split2[1]
            });
        });
    }


}

export class RangeExtendedPropertyTemplate extends DefaultExtendedPropertyTemplate {

    public max: number;
    public min: number;

    constructor(template: PropertyTemplate) {
        super(template);

        let meta = template.Meta;

        if (!meta.startsWith("RANGE(")) {
            console.log("meta data error, enum metadata invalid (", meta, ")");
        }

        meta = meta.slice(6, meta.length - 1);

        const split = meta.split(",");
        this.min = +split[0];
        this.max = +split[1];
    }
}

class MaxLengthPropertyTemplate extends DefaultExtendedPropertyTemplate {
    private _length: number;
    public get length(): number {
        return this._length;
    }
    public set length(v: number) {
        this._length = v;
    }


    constructor(template: PropertyTemplate, max?: number) {
        super(template);

        if (max) {
            this.length = max;
        } else {
            let meta = template.Meta;

            if (!meta.startsWith("LENGTH(")) {
                console.log("meta data error, enum metadata invalid (", meta, ")");
            }

            meta = meta.slice(7, meta.length - 1);
            this.length = parseInt(meta, 10);
        }
    }
}

class IPExtendedPropertyTemplate extends DefaultExtendedPropertyTemplate {
    public rules = {};
}

export class ListExtendedPropertyTemplate extends DefaultExtendedPropertyTemplate {
    public list: any[] = [];

    constructor(template: PropertyTemplate) {
        super(template);

        const meta = template.Meta;

        const splited = meta.split(",");

        for (const x of splited) {
            this.list.push({ key: x, value: x });
        }

    }
}


@Model()
export class PropertyTemplate extends BaseModel {

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    Name: string;

    @JsonProperty()
    Description: string;

    @JsonProperty()
    Key: string;

    @JsonProperty()
    This2PropertyType: number;

    @JsonProperty()
    This2NodeTemplate: string;

    @JsonProperty()
    Group: string;

    @JsonProperty()
    IsVisible: boolean;

    @JsonProperty()
    IsReadonly: boolean;

    @JsonProperty()
    Meta: string;

    @JsonProperty()
    DefaultValue: string;

    @JsonProperty()
    GroupOrder: number;

    @JsonProperty()
    Order: number;

    @JsonPropertyName("This2PropertyTypeNavigation")
    PropertyType: PropertyType;
    // NodeTemplate: NodeTemplate;

    @JsonPropertyName("Constraints")
    PropertyConstraints: PropertyTemplateConstraint[] = [];

    private _ExtendedType: DefaultExtendedPropertyTemplate;
    public get ExtendedType(): DefaultExtendedPropertyTemplate {
        if (!this._ExtendedType) {
            this.afterFromJson();
        }
        return this._ExtendedType;
    }
    public set ExtendedType(v: DefaultExtendedPropertyTemplate) {
        this._ExtendedType = v;
    }


    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }
    public typeInfo(): string {
        return "PropertyTemplate";
    }

    protected afterFromJson() {
        switch (this.PropertyType.Type) {
            case PropertyTemplateType.Enum:
                this._ExtendedType = new EnumExtendedPropertyTemplate(this);
                break;
            case PropertyTemplateType.Range:
                this._ExtendedType = new RangeExtendedPropertyTemplate(this);
                break;
            case PropertyTemplateType.Ip:
                this._ExtendedType = new IPExtendedPropertyTemplate(this);
                break;
            case PropertyTemplateType.AreaIcon:
                this._ExtendedType = new ListExtendedPropertyTemplate(this);
                break;
            case PropertyTemplateType.Text:
                if (this.Meta && this.Meta.startsWith("LENGTH")) {
                    this._ExtendedType = new MaxLengthPropertyTemplate(this);
                } else {
                    this._ExtendedType = new MaxLengthPropertyTemplate(this, 65535);
                }
                break;
            default:
                this._ExtendedType = new DefaultExtendedPropertyTemplate(this);
                break;
        }
    }

}
