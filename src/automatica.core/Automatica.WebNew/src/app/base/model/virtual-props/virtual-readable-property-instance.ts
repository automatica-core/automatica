import { PropertyInstance } from "../property-instance"
import { PropertyTemplate, PropertyTemplateType } from "../property-template"
import { PropertyType } from "../property-type"
import { NodeInstance } from "../node-instance"
import { VirtualPropertyInstance } from "./virtual-property-instance"

export class VirtualReadablePropertyInstance extends VirtualPropertyInstance {
    /**
     *
     */
    constructor(private nodeInstance: NodeInstance) {
        super(nodeInstance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.ISREADABLE.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.ISREADABLE.DESCRIPTION";
        this.PropertyTemplate.Key = "isReadable";

        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Bool;

        this.PropertyTemplate.GroupOrder = 0;
        this.PropertyTemplate.Order = 9;

        this.PropertyTemplate.IsReadonly = this.nodeInstance.NodeTemplate.IsReadableFixed;
    }

    get Value(): any {
        return this.nodeInstance.IsReadable;
    }
    set Value(value: any) {
        if (this.nodeInstance.NodeTemplate.IsReadableFixed) {
            return;
        }
        this.nodeInstance.IsReadable = value;
    }

}
