import { NodeInstance } from "../node-instance";
import { PropertyTemplateType } from "../property-template"
import { VirtualPropertyInstance } from "./virtual-property-instance"

export class VirtualDisabledPropertyInstance extends VirtualPropertyInstance {
    /**
     *
     */
    constructor(private nodeInstance: NodeInstance) {
        super(nodeInstance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.ISDISABLED.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.ISDISABLED.DESCRIPTION";
        this.PropertyTemplate.Key = "isDisabled";

        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Bool;

        this.PropertyTemplate.GroupOrder = 0;
        this.PropertyTemplate.Order = 10;
    }

    get Value(): any {
        return this.nodeInstance.IsDisabled;
    }
    set Value(value: any) {
        this.nodeInstance.IsDisabled = value;
    }

}
