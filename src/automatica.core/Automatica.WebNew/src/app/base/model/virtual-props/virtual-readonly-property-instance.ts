import { PropertyTemplateType } from "../property-template"
import { VirtualPropertyInstance } from "./virtual-property-instance";
import { VisuPage } from "../visu-page";
import { VisuObjectInstance } from "../visu-object-instance";
import { NodeInstance } from "../node-instance";
import { AreaInstance } from "../areas";
import { CategoryInstance } from "../categories";
import { RuleInstance } from "../rule-instance";

export class VirtualReadonlyPropertyInstance extends VirtualPropertyInstance {
    /**
     *
     */
    readOnly = false;
    constructor(private instance: VisuPage | VisuObjectInstance | NodeInstance | AreaInstance | CategoryInstance | RuleInstance) {
        super(instance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.READONLY.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.READONLY.DESCRIPTION";
        this.PropertyTemplate.Key = "readonly";

        this.PropertyTemplate.Group = "COMMON.CATEGORY.VISU";
        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.UserGroup;
        this.PropertyTemplate.IsReadonly = false;
        this.PropertyTemplate.IsVisible = false;

        this.PropertyTemplate.Order = 4;
    }

    get Value(): any {
        return this.readOnly;
    }
    set Value(value: any) {
        this.readOnly = value;
    }

}
