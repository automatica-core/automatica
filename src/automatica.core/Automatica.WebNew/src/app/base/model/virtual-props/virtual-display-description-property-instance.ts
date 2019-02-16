import { PropertyInstance } from "../property-instance"
import { PropertyTemplate, PropertyTemplateType } from "../property-template"
import { PropertyType } from "../property-type"
import { NodeInstance } from "../node-instance"
import { VirtualPropertyInstance } from "./virtual-property-instance"
import { IDescriptionModel } from "../IDescriptionModel";

export class VirtualDisplayDescriptionPropertyInstance extends VirtualPropertyInstance {

    /**
     *
     */
    constructor(private nodeInstance: IDescriptionModel, isReadonly: boolean = false) {
        super(nodeInstance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.DESCRIPTION.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.DESCRIPTION.DESCRIPTION";
        this.PropertyTemplate.Key = "description";
        this.PropertyTemplate.GroupOrder = 0;
        this.PropertyTemplate.Order = 1;
        this.PropertyTemplate.IsReadonly = isReadonly;
    }

    get Value(): any {
        return this.nodeInstance.DisplayDescription;
    }
    set Value(value: any) {
        this.nodeInstance.DisplayDescription = value;
    }


}
