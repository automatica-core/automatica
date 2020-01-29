import { PropertyTemplateType } from "../../property-template"
import { VirtualPropertyInstance } from "../virtual-property-instance"
import { IVisuPage } from "../../visu";

export class VirtualVisuPageHeightProperty extends VirtualPropertyInstance {
    constructor(private visupage: IVisuPage) {
        super(visupage);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.HEIGHT.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.HEIGHT.DESCRIPTION";
        this.PropertyTemplate.Key = "visu-page-height";

        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Integer;

        this.PropertyTemplate.GroupOrder = 10;
    }

    get Value(): number {
        return this.visupage.Height;
    }
    set Value(value: number) {
        if (value > 10 || value < 0) {
            return;
        }
        this.visupage.Height = value;
        super.notifyChange("Value");
    }

}
