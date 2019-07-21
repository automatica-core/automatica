import { PropertyTemplateType } from "../../property-template"
import { VirtualPropertyInstance } from "../virtual-property-instance"
import { IVisuPage } from "../../visu";

export class VirtualVisuPageWidthProperty extends VirtualPropertyInstance {

    constructor(private visupage: IVisuPage) {
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
