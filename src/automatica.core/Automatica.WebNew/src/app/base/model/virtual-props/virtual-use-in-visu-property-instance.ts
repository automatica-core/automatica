import { PropertyInstance } from "../property-instance"
import { PropertyTemplate, PropertyTemplateType } from "../property-template"
import { PropertyType } from "../property-type"
import { NodeInstance } from "../node-instance"
import { VirtualPropertyInstance } from "./virtual-property-instance"
import { RuleInstance } from "../rule-instance";

export class VirtualUseInVisuPropertyInstance extends VirtualPropertyInstance {
    /**
     *
     */
    constructor(private instance: NodeInstance | RuleInstance) {
        super(instance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.USE_IN_VISU.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.USE_IN_VISU.DESCRIPTION";
        this.PropertyTemplate.Key = "use_in_visu";

        this.PropertyTemplate.Group = "COMMON.CATEGORY.VISU";
        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Bool;
        this.PropertyTemplate.IsReadonly = false;

        this.PropertyTemplate.Order = 1;
    }

    get Value(): any {
        return this.instance.UseInVisu;
    }
    set Value(value: any) {
        this.instance.UseInVisu = value;
    }

}
