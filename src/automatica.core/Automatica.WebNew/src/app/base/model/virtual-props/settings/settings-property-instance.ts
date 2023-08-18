import { PropertyInstance } from "../../property-instance"
import { MultiSelectPropertyTemplate, PropertyTemplate, PropertyTemplateType } from "../../property-template"
import { PropertyType } from "../../property-type"
import { VirtualPropertyInstance } from "./../virtual-property-instance"
import { INameModel } from "../../INameModel";
import { Setting } from "../../setting";


export class VirtualSettingsPropertyInstance extends VirtualPropertyInstance {

    constructor(public setting: Setting) {
        super(setting);

        this.PropertyTemplate.Name = "COMMON.PROPERTY." + setting.ValueKey.toUpperCase() + ".NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.SETTING.DESCRIPTION";
        this.PropertyTemplate.Key = setting.ValueKey;
        this.PropertyTemplate.IsReadonly = false;

        if (setting.Group === "SERVER.COMMON") {
            this.PropertyTemplate.Order = 1;
            this.PropertyTemplate.Group = "COMMON.CATEGORY.MISC";
        } else {
            this.PropertyTemplate.Group = setting.Group;
        }
        this.PropertyTemplate.Order = 3;
        this.PropertyTemplate.PropertyType.Type = setting.Type;

        this.PropertyTemplate.IsReadonly = setting.IsReadonly;
        this.PropertyTemplate.Meta = setting.Meta;

        if(setting.Type == PropertyTemplateType.MultiSelect) {
            this.PropertyTemplate.ExtendedType = new MultiSelectPropertyTemplate(this.PropertyTemplate);
        }
    }

    get Value(): any {
        return this.setting.Value;
    }
    set Value(value: any) {
       
        this.setting.Value = value;

        this.propertyChanged.emit(this);
    }
}
