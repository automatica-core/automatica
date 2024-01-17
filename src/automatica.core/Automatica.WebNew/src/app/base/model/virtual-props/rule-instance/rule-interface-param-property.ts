import { PropertyTemplateType } from "../../property-template"
import { VirtualPropertyInstance } from "../virtual-property-instance"
import { RuleInterfaceInstance } from "../../rule-interface-instance";
import { RuleInterfaceParameterDataType } from "../../rule-interface-template";

export class RuleInterfaceParamProperty extends VirtualPropertyInstance {
    constructor(private ruleInterface: RuleInterfaceInstance) {
        super(ruleInterface);

        this.PropertyTemplate.Name = ruleInterface.Template.Name;
        this.PropertyTemplate.Description = ruleInterface.Template.Description;
        this.PropertyTemplate.Key = ruleInterface.Template.Name;

        this.PropertyTemplate.Order = ruleInterface.Template.SortOrder;

        this.PropertyTemplate.Group = "COMMON.CATEGORY.PARAMETERS";

        switch (ruleInterface.Template.ParameterDataType) {
            case RuleInterfaceParameterDataType.Integer:
                this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Integer;
                break;
            case RuleInterfaceParameterDataType.Double:
                this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Numeric;
                break;
            case RuleInterfaceParameterDataType.Text:
                this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Text;
                break;
            case RuleInterfaceParameterDataType.Timer:
                this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Timer;
                break;
            case RuleInterfaceParameterDataType.Calendar:
                this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Calendar;
                break;
            case RuleInterfaceParameterDataType.ConstantString:
                this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Text;
                this.PropertyTemplate.IsReadonly = true;
                break;
            case RuleInterfaceParameterDataType.Color:
                this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Color;
                break;
            case RuleInterfaceParameterDataType.Enum:
                this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Enum;
                this.PropertyTemplate.Meta = ruleInterface.Template.Meta;
                this.PropertyTemplate.updatePropertyType();
                break;
            case RuleInterfaceParameterDataType.Bool:
                this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Bool;
                break;
        }

    }

    get Value(): number {
        return this.ruleInterface.Value;
    }
    set Value(value: number) {
        this.ruleInterface.Value = value;
        super.notifyChange("Value");
    }

}
