import { NodeInstance } from "../node-instance";
import { PropertyTemplateType } from "../property-template"
import { VirtualPropertyInstance } from "./virtual-property-instance"

export class VirtualOnlyWriteIfChangedPropertyInstance extends VirtualPropertyInstance {
    /**
     *
     */
    constructor(private nodeInstance: NodeInstance) {
        super(nodeInstance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.ONLY_WRITE_IF_CHANGED.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.ONLY_WRITE_IF_CHANGED.DESCRIPTION";
        this.PropertyTemplate.Key = "only_write_if_changed";

        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Bool;

        this.PropertyTemplate.GroupOrder = 0;
        this.PropertyTemplate.Order = 10;
    }

    get Value(): any {
        return this.nodeInstance.WriteOnlyIfChanged;
    }
    set Value(value: any) {
        this.nodeInstance.WriteOnlyIfChanged = value;
    }

}
