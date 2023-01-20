import { PropertyTemplateType, ListExtendedPropertyTemplate } from "../property-template"
import { VirtualPropertyInstance } from "./virtual-property-instance"
import { IIconModel } from "../IIconMode";

export class VirtualIconProperty extends VirtualPropertyInstance {
    constructor(private area: IIconModel) {
        super(area);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.AREA_ICON.NAME"
        this.PropertyTemplate.Description = "COMMON.PROPERTY.AREA_ICON.DESCRIPTION"
        this.PropertyTemplate.Key = "icon";


        this.PropertyTemplate.This2PropertyType = PropertyTemplateType.AreaIcon;
        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.AreaIcon;

        this.PropertyTemplate.Order = 10;

        this.PropertyTemplate.Meta = "booth-curtain,shoe-prints,alarm-clock,volume-up,lightbulb,th-large,plug,square,temperature-high,temperature-low,compact-disc,solar-panel,bolt,memory,thermometer,sun,home,project-diagram,building,box,bed,tv,bath";
        this.PropertyTemplate.ExtendedType = new ListExtendedPropertyTemplate(this.PropertyTemplate);
    }

    get Value(): string {
        return this.area.Icon;
    }
    set Value(value: string) {
        this.area.Icon = value;
        super.notifyChange("Value");
    }

}
