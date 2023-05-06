import { NodeTemplate } from "../node-template";

export class LearnModeNodeTemplate {


    private _parentIdSet: boolean;

    public get ParentId(): string {
        if (this._parentIdSet) {
            return void 0;
        }

        return this.nodeTemplate.NeedsInterface2InterfacesType;
    }
    public set ParentId(v: string) {
        this._parentIdSet = true;
    }

    public get Id(): string {
        return this.nodeTemplate.ProvidesInterface2InterfaceType;
    }

    public get ObjId(): string {
        return this.nodeTemplate.ObjId;
    }

    public get DisplayName(): string {
        return this.nodeTemplate.DisplayName;
    }


    constructor(public nodeTemplate: NodeTemplate) {


    }

}
