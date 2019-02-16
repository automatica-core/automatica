import { PropertyInstance } from "../property-instance"
import { PropertyTemplate, PropertyTemplateType } from "../property-template"
import { PropertyType } from "../property-type"
import { VirtualPropertyInstance } from "./virtual-property-instance"
import { INameModel } from "../INameModel";
import { IAreaInstanceModel } from "../IAreaInstanceModel";
import { AreaInstance } from "../areas/area-instance";

export class VirtualAreaPropertyInstance extends VirtualPropertyInstance {

    /**
     *
     */
    constructor(private areaInstance: IAreaInstanceModel) {
        super(areaInstance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.AREA.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.AREA.DESCRIPTION";
        this.PropertyTemplate.Key = "area-name";
        this.PropertyTemplate.IsReadonly = false;

        this.PropertyTemplate.Group = "COMMON.CATEGORY.VISU";
        this.PropertyTemplate.Order = 2;
        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.AreaInstance;
    }

    get Value(): any {
        return this.areaInstance.This2AreaInstance;
    }
    set Value(value: any) {
        this.areaInstance.This2AreaInstance = value;
        this.propertyChanged.emit(this);
    }
}
