import { PropertyInstance } from "../property-instance"
import { PropertyTemplate, PropertyTemplateType } from "../property-template"
import { PropertyType } from "../property-type"
import { VirtualPropertyInstance } from "./virtual-property-instance"
import { INameModel } from "../INameModel";

export class VirtualColorPropertyInstance extends VirtualPropertyInstance {

    /**
     *
     */
    constructor(private nodeInstance: INameModel, private get: () => string, private set: (value: string) => void) {
        super(nodeInstance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.COLOR.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.COLOR.DESCRIPTION";
        this.PropertyTemplate.Key = "color";
        this.PropertyTemplate.IsReadonly = false;

        this.PropertyTemplate.Order = 3;
        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Color;
    }

    get Value(): any {
        return this.get();
    }
    set Value(value: any) {
        this.set(value);
        this.propertyChanged.emit(this);
    }


}
