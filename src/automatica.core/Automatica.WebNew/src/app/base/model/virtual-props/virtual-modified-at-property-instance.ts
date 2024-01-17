import { VirtualPropertyInstance } from "./virtual-property-instance"
import { PropertyTemplateType } from "../property-template";
import { ITimestampModifiedTrackingModel } from "../ITimestampModifiedTrackingModel";

export class VirtualModifedAtPropertyInstance extends VirtualPropertyInstance {

    /**
     *
     */
    constructor(private nodeInstance: ITimestampModifiedTrackingModel) {
        super(nodeInstance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.MODIFIED_AT.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.MODIFIED_AT.DESCRIPTION";
        this.PropertyTemplate.Key = "modifed_at";
        this.PropertyTemplate.IsReadonly = true;

        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.DateTime;
        this.PropertyTemplate.Order = 11;
    }

    get Value(): any {
        return this.nodeInstance.ModifiedAt;
    }

}
