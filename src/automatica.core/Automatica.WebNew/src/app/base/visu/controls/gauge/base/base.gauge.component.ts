import { BaseComponent } from "src/app/base/base-component";
import { ViewChild, Input } from "@angular/core";
import { DxComponent } from "devextreme-angular";
import { BaseMobileComponent } from "../../../base-mobile-component";

export class BaseGaugeComponent<T extends DxComponent> extends BaseComponent {

    @ViewChild("gauge", { static: true })
    gauge: T;


    @Input()
    ref: BaseMobileComponent;

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
