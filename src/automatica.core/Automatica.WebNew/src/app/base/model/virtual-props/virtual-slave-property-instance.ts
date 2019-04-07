import { PropertyTemplateType } from "../property-template"
import { VirtualPropertyInstance } from "./virtual-property-instance";
import { NodeInstance } from "../node-instance";

export class VirtualSlavePropertyInstance extends VirtualPropertyInstance {
    /**
     *
     */
    constructor(private instance: NodeInstance) {
        super(instance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.SLAVE.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.SLAVE.DESCRIPTION";
        this.PropertyTemplate.Key = "slave";

        this.PropertyTemplate.Group = "COMMON.CATEGORY.VISU";
        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Slave;
        this.PropertyTemplate.IsReadonly = false;

        this.PropertyTemplate.Order = 4;
    }

    get Value(): any {
        return this.instance.This2Slave;
    }
    set Value(value: any) {
        this.instance.This2Slave = value;
    }

}
