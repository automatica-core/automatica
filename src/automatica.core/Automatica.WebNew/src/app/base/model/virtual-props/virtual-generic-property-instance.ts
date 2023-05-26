import { PropertyInstance } from "../property-instance"
import { PropertyTemplate, PropertyTemplateType } from "../property-template"
import { PropertyType } from "../property-type"
import { VirtualPropertyInstance } from "./virtual-property-instance"
import { INameModel } from "../INameModel";

export class VirtualGenericPropertyInstance<T> extends VirtualPropertyInstance {


    constructor(name: string, order: number, private model: any, private get: () => T,
        private set: (value: T) => void, isReadOnly = false, propertyType: PropertyTemplateType = PropertyTemplateType.Text, category: string = "COMMON.CATEGORY.MISC") {
        super(model);

        this.PropertyTemplate.Name = `COMMON.PROPERTY.${name}.NAME`;
        this.PropertyTemplate.Description = `COMMON.PROPERTY.${name}.DESCRIPTION`;
        this.PropertyTemplate.Key = name.toLowerCase();
        this.PropertyTemplate.IsReadonly = isReadOnly;

        this.PropertyTemplate.Order = order;
        this.PropertyTemplate.PropertyType.Type = propertyType;
        this.PropertyTemplate.Group = category;

        if (!set) {
            this.PropertyTemplate.IsReadonly = true;
        }
    }

    get Value(): any {
        return this.get();
    }
    set Value(value: any) {
        if (this.set) {
            this.set(value);
            this.propertyChanged.emit(this);
        }
    }


}
