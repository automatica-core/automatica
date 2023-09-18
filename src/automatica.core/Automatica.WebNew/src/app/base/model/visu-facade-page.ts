import { Model } from "./base-model";
;
import { VisuPage, VisuPageGroupType } from "./visu-page";


@Model()
export class VisuFacadePage extends VisuPage {

    public visuPageType: VisuPageGroupType;
}
