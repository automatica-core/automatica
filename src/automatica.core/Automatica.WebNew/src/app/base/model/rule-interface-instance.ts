import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"
import { RuleInterfaceTemplate, RuleInterfaceParameterDataType, RuleInterfaceDirectionEnum } from "./rule-interface-template"

import { IKey } from "./IKey";
import { Guid } from "../utils/Guid";
import { TimerPropertyData } from "./timer-property-data";
import { L10nTranslationService } from "angular-l10n";

@Model()
export class RuleInterfaceInstance extends BaseModel {



    private _portValue: string;
    public get PortValue(): string {
        return this._portValue;
    }
    public set PortValue(v: string) {
        this._portValue = v;
        this.notifyChange("PortValue");
    }


    private _valueDouble: number;
    private _valueInteger: number;
    private _valueString: string

    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    This2RuleInstance: string;

    @JsonProperty()
    IsLinked: boolean;

    @JsonProperty()
    This2RuleInterfaceTemplate: string;

    @JsonPropertyName("This2RuleInterfaceTemplateNavigation")
    Template: RuleInterfaceTemplate;

    @JsonPropertyName("This2RuleInstanceNavigation", false, false, true)
    RuleInstance: IKey;

    public static fromTemplate(template: RuleInterfaceTemplate, ruleInstance: IKey, translationService: L10nTranslationService): RuleInterfaceInstance {
        const instance = new RuleInterfaceInstance(ruleInstance);

        instance.translationService = translationService;

        instance.ObjId = Guid.MakeNew().ToString();
        instance.Template = template;
        instance.This2RuleInterfaceTemplate = template.ObjId;
        instance.Value = template.DefaultValue;

        instance.afterFromJson();

        return instance;
    }

    constructor(ruleInstance: IKey) {
        super();
        this.RuleInstance = ruleInstance;
    }

    get PortId(): string {
        return this.ObjId;
    }

    get FromMaxLinks(): number {
        if (this.Template.This2RuleInterfaceDirection === RuleInterfaceDirectionEnum.Output) {
            if (this.Template.MaxLinks === 0) {
                return 65535;
            }
            return this.Template.MaxLinks;
        }
        return 1;
    }

    get ToMaxLinks(): number {
        if (this.Template.This2RuleInterfaceDirection === RuleInterfaceDirectionEnum.Input) {
            if (this.Template.MaxLinks === 0) {
                return 65535;
            }
            return this.Template.MaxLinks;
        }
        return 1;
    }

    set PortId(value: string) {
        console.log(value);
    }

    get Name(): string {
        return this.translationService.translate(this.Template.Name);
    }

    public get Value(): any {
        switch (this.Template.ParameterDataType) {
            case RuleInterfaceParameterDataType.Double:
                return this.ValueDouble;
            case RuleInterfaceParameterDataType.Integer:
                return this.ValueInteger;
            case RuleInterfaceParameterDataType.Text:
            case RuleInterfaceParameterDataType.ConstantString:
            case RuleInterfaceParameterDataType.Color:
                return this.ValueString;
            case RuleInterfaceParameterDataType.Timer: {
                const timerProperty = new TimerPropertyData();
                if (this.ValueString) {
                    timerProperty.fromJson(JSON.parse(this.ValueString), this.translationService);
                }
                return timerProperty;
            }
        }

        return void 0;
    }

    public set Value(value: any) {
        switch (this.Template.ParameterDataType) {
            case RuleInterfaceParameterDataType.Double:
                this.ValueDouble = parseFloat(value);
                break;
            case RuleInterfaceParameterDataType.Integer:
                this.ValueInteger = parseInt(value, 10);
                break;
            case RuleInterfaceParameterDataType.Text:
            case RuleInterfaceParameterDataType.ConstantString:
            case RuleInterfaceParameterDataType.Color:
                this.ValueString = value;
                break;
            case RuleInterfaceParameterDataType.Timer:
                {
                    if (value instanceof TimerPropertyData) {
                        this.ValueString = JSON.stringify(value.toJson());
                    } else {
                        this.ValueString = void 0;
                    }
                    break;
                }
        }
        this.notifyChange("Value");
    }



    get dockColor(): string {
        if (this.Template.InterfaceDirection.Key === "I") {
            return "green";
        } else {
            return "blue";
        }
    }

    @JsonProperty()
    public get ValueString(): string {
        return this._valueString;
    }
    public set ValueString(v: string) {
        this._valueString = v;
    }

    @JsonProperty()
    public get ValueInteger(): number {
        return this._valueInteger;
    }
    public set ValueInteger(v: number) {
        this._valueInteger = v;
    }

    @JsonProperty()
    public get ValueDouble(): number {
        return this._valueDouble;
    }
    public set ValueDouble(v: number) {
        this._valueDouble = v;
    }



    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    public typeInfo(): string {
        return "RuleInterfaceInstance";
    }
}
