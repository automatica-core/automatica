import { BaseModel, JsonFieldInfo, JsonProperty, Model, JsonPropertyName } from "./base-model"
import { PropertyTemplate, PropertyTemplateType } from "./property-template";
import { EventEmitter } from "@angular/core";
import { ITreeNode } from "./ITreeNode";
import { MetaHelper } from "../base/meta-helper";
import { PropertyConstraintType } from "./property-template-constraint";
import { PropertyConstraintConditionType } from "./property-template-constraint-data";
;
import { Guid } from "../utils/Guid";
import { TimerPropertyData } from "./timer-property-data";
import { ControlConfiguration } from "./control-configuration";

class PropertyInstanceMetaHelper {
    public static validate(propertyInstance: PropertyInstance): boolean {

        let resultValue = true;
        for (const nodeInstance of propertyInstance.Parent.Parent.Children) {
            if (nodeInstance === propertyInstance.Parent) {
                continue;
            }

            for (const prop of nodeInstance.Properties) {
                if (prop.PropertyTemplate.Key === propertyInstance.PropertyTemplate.Key) {
                    for (const constraint of propertyInstance.PropertyTemplate.PropertyConstraints) {

                        for (const condition of constraint.ConstraintData) {
                            condition.conditionResult = void 0;
                        }

                        for (const condition of constraint.ConstraintData) {
                            condition.conditionResult = true;

                            const propertyValue = nodeInstance.getPropertyValue(condition.Key);
                            const ownValue = +propertyInstance.Parent.getPropertyValue(condition.Key);

                            if (condition.ConditionType === PropertyConstraintConditionType.UniqueRange) {

                                const min = +propertyInstance.Value;
                                const max = (ownValue + min) * condition.Factor + condition.Offset;
                                const propertyValueMin = nodeInstance.getPropertyValue(propertyInstance.PropertyTemplate.Key);
                                const propertyValueMax = propertyValueMin + (nodeInstance.getPropertyValue(condition.Key) * condition.Factor + condition.Offset);

                                if (this.isInRange(min, max, propertyValueMin, propertyValueMax)) {
                                    condition.conditionResult = true;
                                } else {
                                    condition.conditionResult = false;
                                    break;
                                }
                            } else if (condition.ConditionType === PropertyConstraintConditionType.Unique) {
                                if (ownValue === propertyValue) {
                                    condition.conditionResult = true;
                                } else {
                                    condition.conditionResult = false;
                                    break;
                                }
                            } else {
                                condition.conditionResult = true;
                            }
                        }
                        let conditionEvaluation = false;
                        for (const condition of constraint.ConstraintData) {
                            if (condition.conditionResult === void 0) {
                                conditionEvaluation = true;
                                break;
                            }
                            if (!condition.conditionResult) {
                                conditionEvaluation = true;
                                break;
                            }
                        }
                        nodeInstance.ValidationOk = conditionEvaluation;
                        propertyInstance.Parent.ValidationOk = conditionEvaluation;

                        if (!conditionEvaluation) {
                            resultValue = conditionEvaluation;
                        }
                    }
                }
            }
        }
        return resultValue;
    }

    private static isInRange(x1: number, x2: number, y1: number, y2: number): boolean {
        return Math.max(x1, y1) <= Math.min(x2, y2);
    }
}

export enum UpdateScope
{
    Unknown = 0,
    GenericProperty = 1,
    SpecificProperty = 2,
    ParentChanged = 3,
    Imported = 4
}

@Model()
export class PropertyInstance extends BaseModel {


    private _isVisible: boolean = void 0;

    public propertyChanged = new EventEmitter<PropertyInstance>();
    @JsonProperty()
    ObjId: string;

    @JsonProperty()
    This2PropertyTemplate: string;

    @JsonProperty()
    This2NodeInstance: string;

    @JsonProperty()
    This2VisuObjectInstance: string;

    @JsonPropertyName("ValueVisuPageNavigation")
    VisuPage: any;

    @JsonPropertyName("ValueAreaInstanceNavigation")
    AreaInstance: any;


    private _ValueString?: string;
    @JsonProperty()
    public get ValueString(): string {
        return this._ValueString;
    }
    public set ValueString(v: string) {
        this._ValueString = v;
        this.propertyChanged.emit(this);
    }


    private _ValueInt?: number;
    @JsonProperty()
    public get ValueInt(): number {
        return this._ValueInt;
    }
    public set ValueInt(v: number) {
        this._ValueInt = v;
        this.propertyChanged.emit(this);
    }

    @JsonProperty()
    ValueBool?: boolean;

    private _ValueDouble?: number;
    @JsonProperty()
    public get ValueDouble(): number {
        return this._ValueDouble;
    }
    public set ValueDouble(v: number) {
        this._ValueDouble = v;
        this.propertyChanged.emit(this);
    }



    private _ValueNodeInstance: string;
    @JsonProperty()
    public get ValueNodeInstance(): string {
        return this._ValueNodeInstance;
    }
    public set ValueNodeInstance(v: string) {
        this._ValueNodeInstance = v;
        this.propertyChanged.emit(this);
    }


    private _ValueVisuPage: string;
    @JsonProperty()
    public get ValueVisuPage(): string {
        return this._ValueVisuPage;
    }
    public set ValueVisuPage(v: string) {
        this._ValueVisuPage = v;
        this.propertyChanged.emit(this);
    }


    private _ValueAreaInstance: string;
    @JsonProperty()
    public get ValueAreaInstance(): string {
        return this._ValueAreaInstance;
    }
    public set ValueAreaInstance(v: string) {
        this._ValueAreaInstance = v;
    }

    private _ValueSlave: string;
    @JsonProperty()
    public get ValueSlave(): string {
        return this._ValueSlave;
    }
    public set ValueSlave(v: string) {
        this._ValueSlave = v;
    }


    private _ValueLong: number;
    @JsonProperty()
    public get ValueLong(): number {
        return this._ValueLong;
    }
    public set ValueLong(v: number) {
        this._ValueLong = v;
        this.propertyChanged.emit(this);
    }

    public get updateScope() : UpdateScope {
        return UpdateScope.SpecificProperty;
    }

    @JsonPropertyName("This2PropertyTemplateNavigation")
    PropertyTemplate: PropertyTemplate;

    public static createFromTemplate(parent: any, template: PropertyTemplate): PropertyInstance {
        const pi = new PropertyInstance(parent);
        pi.PropertyTemplate = template;
        pi.This2PropertyTemplate = template.ObjId;
        pi.This2NodeInstance = void 0;
        pi.This2VisuObjectInstance = void 0;
        pi.ObjId = Guid.MakeNew().ToString();
        3
        if (template.DefaultValue) {
            pi.setPropertyValue(template.DefaultValue);
        }

        pi.afterFromJson();

        return pi;
    }

    constructor(public Parent: any) {
        super();
    }

    afterFromJson() {
        if (this.PropertyTemplate && this.PropertyTemplate.PropertyConstraints && this.PropertyTemplate.PropertyConstraints.length > 0) {
            for (const x of this.PropertyTemplate.PropertyConstraints) {
                if (x.ConstraintType === PropertyConstraintType.Visible) {
                    let property = void 0;
                    let value = void 0;

                    for (const cd of x.ConstraintData) {
                        if (cd.ConditionType !== PropertyConstraintConditionType.ParentCondition) {
                            continue;
                        }
                        if (cd.Key.startsWith(":")) {
                            value = cd.Key.substr(1, cd.Key.length);
                            continue;
                        }
                        property = cd.Key;
                    }

                    const parentPropValue = this.Parent.Parent.getPropertyValue(property);
                    if (parentPropValue !== value) {
                        this.IsVisible = false;
                    } else {
                        this.IsVisible = true;
                    }
                }
            }
        }
    }

    public validate(): boolean {
        for (const constraint of this.PropertyTemplate.PropertyConstraints) {
            switch (constraint.ConstraintType) {

                case PropertyConstraintType.None:
                    return true;
                case PropertyConstraintType.Unique: {
                    if (this.Parent?.Parent?.Children) {
                        for (const x of this.Parent.Parent.Children) {
                            if (x === this.Parent) {
                                continue;
                            }
                            for (const prop of x.Properties) {
                                if (prop.PropertyTemplate.Key === this.PropertyTemplate.Key) {
                                    if (prop.Value === this.Value) {
                                        x.ValidationOk = false;
                                        this.Parent.ValidationOk = false;
                                        return false;
                                    } else {
                                        x.ValidationOk = true;
                                    }
                                }
                            }
                        }
                        this.Parent.ValidationOk = true;
                        return true;
                    }
                    break;
                } case PropertyConstraintType.UniqueOnCondition: {
                    const validationResult = PropertyInstanceMetaHelper.validate(this);
                    this.Parent.ValidationOk = validationResult;

                    return validationResult;
                }
            }
        }

        return true;
    }

    public get Group(): string {
        return this.PropertyTemplate.Group;
    }

    public get Name(): string {
        return this.PropertyTemplate.Name;
    }

    public get PropertyType(): number {
        return this.PropertyTemplate.PropertyType.Type;
    }

    public get IsReadonly(): boolean {
        return this.PropertyTemplate.IsReadonly;
    }

    public get IsVisible(): boolean {
        if (this._isVisible === void 0) {
            if (!this.PropertyTemplate) {
                return false;
            }
            return this.PropertyTemplate.IsVisible;
        }
        return this._isVisible;
    }

    public set IsVisible(v: boolean) {
        this._isVisible = v;
    }


    protected useBaseModelInstanceForJson(baseModel: BaseModel): boolean {
        // if (baseModel instanceof PropertyTemplate) {
        //     return false;

        // }
        return true;
    }

    protected getJsonProperty(): Map<string, JsonFieldInfo> {
        return void 0;
    }

    get Value(): any {

        if (!this.PropertyTemplate.PropertyType) {
            console.error("could not set property value", this);
            return void 0;
        }

        switch (this.PropertyTemplate.PropertyType.Type) {
            case PropertyTemplateType.Long:
                return this.ValueLong;
            case PropertyTemplateType.Text:
            case PropertyTemplateType.Parity:
            case PropertyTemplateType.Interface:
            case PropertyTemplateType.Ip:
            case PropertyTemplateType.Color:
            case PropertyTemplateType.UsbPort:
            case PropertyTemplateType.AreaIcon:
            case PropertyTemplateType.Password:
                return this.ValueString;
            case PropertyTemplateType.Integer:
            case PropertyTemplateType.Enum:
            case PropertyTemplateType.DropDown:
            case PropertyTemplateType.Baudrate:
            case PropertyTemplateType.Databits:
            case PropertyTemplateType.Range:
                return this.ValueInt;
            case PropertyTemplateType.NodeInstance:
                return this.ValueNodeInstance;
            case PropertyTemplateType.VisuPage:
                return this.ValueVisuPage;
            case PropertyTemplateType.AreaInstanceLink:
                return this.ValueAreaInstance;
            case PropertyTemplateType.Satellite:
                return this.ValueSlave;
            case PropertyTemplateType.Bool:
                return this.ValueBool;
            case PropertyTemplateType.Stopbits:
            case PropertyTemplateType.Numeric:
                return this.ValueDouble;
            case PropertyTemplateType.Controls: {
                const controlsProperty = new ControlConfiguration();
                if (this.ValueString) {
                    controlsProperty.fromJson(JSON.parse(this.ValueString), this.translationService);
                }
                return controlsProperty;
            }
        }
    }
    set Value(value: any) {
        this.setPropertyValue(value);
        this.notifyChange("Value");
        this.propertyChanged.emit(this);
    }


    private setPropertyValue(value: any) {

        if (!this.PropertyTemplate.PropertyType) {
            console.error("could not set property value", value);
            return;
        }

        switch (this.PropertyTemplate.PropertyType.Type) {
            case PropertyTemplateType.Long:
                this.ValueLong = value;
                break;
            case PropertyTemplateType.Text:
            case PropertyTemplateType.Parity:
            case PropertyTemplateType.Interface:
            case PropertyTemplateType.Ip:
            case PropertyTemplateType.Color:
            case PropertyTemplateType.UsbPort:
            case PropertyTemplateType.AreaIcon:
            case PropertyTemplateType.Password:
                this.ValueString = value;
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
            case PropertyTemplateType.NodeInstance:
                this.ValueNodeInstance = value;
                break;
            case PropertyTemplateType.VisuPage:
                this.ValueVisuPage = value;
                break;
            case PropertyTemplateType.AreaInstanceLink:
                this.ValueAreaInstance = value;
                break;
            case PropertyTemplateType.Satellite:
                this.ValueSlave = value;
                break;
            case PropertyTemplateType.Bool:
                if (typeof (value) === "boolean") {
                    this.ValueBool = value;
                } else if (typeof (value) === "string") {
                    if (value.toLowerCase() === "true") {
                        this.ValueBool = true;
                    } else {
                        this.ValueBool = false;
                    }
                } else {
                    this.ValueBool = parseInt(value, 10) === 1;
                } break;
            case PropertyTemplateType.Stopbits:
            case PropertyTemplateType.Numeric:

                this.ValueDouble = parseFloat(value);
                if (isNaN(this.ValueDouble)) {
                    this.ValueDouble = 0;
                }
                break;
            case PropertyTemplateType.Controls:
                {
                    if (value instanceof ControlConfiguration) {
                        this.ValueString = JSON.stringify(value.toJson());
                    } else {
                        this.ValueString = void 0;
                    }
                    break;
                }
        }
    }

    public typeInfo(): string {
        return "PropertyInstance";
    }
}
