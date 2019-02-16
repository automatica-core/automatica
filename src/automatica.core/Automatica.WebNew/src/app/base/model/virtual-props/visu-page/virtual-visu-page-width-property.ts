import { PropertyInstance } from "../../property-instance"
import { PropertyTemplate, PropertyTemplateType } from "../../property-template"
import { PropertyType } from "../../property-type"
import { VirtualPropertyInstance } from "../virtual-property-instance"
import { VisuPage } from "../../visu-page";

export class VirtualVisuPageWidthProperty extends VirtualPropertyInstance {

    constructor(private visupage: VisuPage) {
        super(visupage);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.WIDTH.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.WIDTH.DESCRIPTION";
        this.PropertyTemplate.Key = "visu-page-width";

        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Integer;
        this.PropertyTemplate.GroupOrder = 11;
    }

    get Value(): number {
        return this.visupage.Width;
    }
    set Value(value: number) {
        if (value > 10 || value < 0) {
            return;
        }
        this.visupage.Width = value;
        super.notifyChange("Value");
    }

}
