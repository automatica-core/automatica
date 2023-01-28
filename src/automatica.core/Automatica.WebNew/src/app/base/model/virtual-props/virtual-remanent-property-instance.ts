import { NodeInstance } from "../node-instance";
import { PropertyTemplateType } from "../property-template"
import { VirtualPropertyInstance } from "./virtual-property-instance"

export class VirtualRemanentPropertyInstance extends VirtualPropertyInstance {
    /**
     *
     */
    constructor(private nodeInstance: NodeInstance) {
        super(nodeInstance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.REMANENT.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.REMANENT.DESCRIPTION";
        this.PropertyTemplate.Key = "remanent";

        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Bool;

        this.PropertyTemplate.GroupOrder = 0;
        this.PropertyTemplate.Order = 9;
    }

    get Value(): any {
        return this.nodeInstance.Remanent;
    }
    set Value(value: any) {
        this.nodeInstance.Remanent = value;
    }

}
