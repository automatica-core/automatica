import { VirtualPropertyInstance } from "./virtual-property-instance"
import { PropertyTemplateType } from "../property-template";
import { ITimestampModifiedTrackingModel } from "../ITimestampModifiedTrackingModel";

export class VirtualCreatedAtPropertyInstance extends VirtualPropertyInstance {

    /**
     *
     */
    constructor(private nodeInstance: ITimestampModifiedTrackingModel) {
        super(nodeInstance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.CREATED_AT.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.CREATED_AT.DESCRIPTION";
        this.PropertyTemplate.Key = "created_at";
        this.PropertyTemplate.IsReadonly = true;
        
        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.DateTime;
        this.PropertyTemplate.Order = 10;
    }

    get Value(): any {
        return this.nodeInstance.CreatedAt;
    }

}
