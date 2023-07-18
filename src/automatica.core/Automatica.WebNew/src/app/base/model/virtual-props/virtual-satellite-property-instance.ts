import { PropertyTemplateType } from "../property-template"
import { VirtualPropertyInstance } from "./virtual-property-instance";
import { INodeInstance } from "../INodeInstance";

export class VirtualSatellitePropertyInstance extends VirtualPropertyInstance {
    /**
     *
     */
    constructor(private instance: INodeInstance) {
        super(instance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.SATELLITE.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.SATELLITE.DESCRIPTION";
        this.PropertyTemplate.Key = "satellite";

        this.PropertyTemplate.Group = "COMMON.CATEGORY.SATELLITE";
        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Satellite;
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
