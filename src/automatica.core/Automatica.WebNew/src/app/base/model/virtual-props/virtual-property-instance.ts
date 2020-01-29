import { PropertyInstance } from "../property-instance"
import { PropertyTemplate, PropertyTemplateType } from "../property-template"
import { PropertyType } from "../property-type"

export class VirtualPropertyInstance extends PropertyInstance {
    /**
     *
     */
    constructor(parent: any) {
        super(parent);

        this.PropertyTemplate = new PropertyTemplate();
        this.PropertyTemplate.PropertyType = new PropertyType();
        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Text;

        this.PropertyTemplate.Group = "COMMON.CATEGORY.MISC";

        this.PropertyTemplate.GroupOrder = -10;
        this.PropertyTemplate.Order = -10;
        this.PropertyTemplate.IsVisible = true;
    }
}
