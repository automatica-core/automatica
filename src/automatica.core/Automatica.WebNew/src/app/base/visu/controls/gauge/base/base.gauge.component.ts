import { BaseComponent } from "src/app/base/base-component";
import { ViewChild, Input } from "@angular/core";
import { DxComponent } from "devextreme-angular";
import { BaseMobileComponent } from "../../../base-mobile-component";

export class BaseGaugeComponent<T extends DxComponent> extends BaseComponent {

    @ViewChild("gauge", { static: false })
    gauge: T;


    @Input()
    ref: BaseMobileComponent;

    public get scaleStart() {
        return this.ref.getPropertyValue("scale_start");
    }
    public get scaleEnd() {
        return this.ref.getPropertyValue("scale_end");
    }
    public get tickInterval() {
        return this.ref.getPropertyValue("ticks");
    }

    customizeText = (arg: any) => {
        if (!this.ref) {
            return arg.valueText;
        }

        const unit = this.ref.getPropertyValue("unit");

        if (!unit) {
            return arg.valueText;
        }

        return arg.valueText + this.ref.getPropertyValue("unit");
    }

    protected initGauge() {
        this.registerEvent(this.ref.item.itemResized, (a) => {
            if (this.gauge && this.gauge.instance) {
                this.gauge.instance.render();
            }
        });

        super.registerEvent((this.ref.item.notifyChangeEvent), (prop) => {
            this.gauge.instance.render();
        });
    }

    protected destroyGauge() {
        this.baseOnDestroy();
    }
}
