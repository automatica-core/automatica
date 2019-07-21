import { PropertyTemplateType } from "../property-template"
import { VirtualPropertyInstance } from "./virtual-property-instance"
import { INodeInstance } from "../INodeInstance";

export class VirtualWriteablePropertyInstance extends VirtualPropertyInstance {
    /**
     *
     */
    constructor(private nodeInstance: INodeInstance) {
        super(nodeInstance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.ISWRITEABLE.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.ISWRITEABLE.DESCRIPTION";
        this.PropertyTemplate.Key = "isWriteable";

        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Bool;
        this.PropertyTemplate.IsReadonly = this.nodeInstance.NodeTemplate.IsWriteableFixed;

        this.PropertyTemplate.Order = 10;
    }

    get Value(): any {
        return this.nodeInstance.IsWriteable;
    }
    set Value(value: any) {
        if (this.nodeInstance.NodeTemplate.IsWriteableFixed) {
            return;
        }
        this.nodeInstance.IsWriteable = value;
    }

}
