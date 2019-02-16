declare var draw2d: any;
declare var $: any;


export class LogicLocators {
    public static addLocators(logic) {

        logic.LogicOutputPortLocator = draw2d.layout.locator.RightLocator.extend({
            realParent: void 0,
            label: void 0,

            init: function (realParent, label) {
                this._super();
                this.realParent = realParent;
                this.label = label;
            },
            relocate: function (index, target) {
                const parent = this.realParent;
                const boundingBox = parent.getBoundingBox()
                let offset = (parent instanceof draw2d.Port) ? boundingBox.h / 2 : 0

                offset += 12;
                const y = (index + 1) * this.label.height;

                if (target instanceof draw2d.Port) {
                    target.setPosition(boundingBox.w, y - offset)
                }
            }
        });

        logic.LogicInputPortLocator = draw2d.layout.locator.LeftLocator.extend({
            realParent: void 0,
            label: void 0,

            init: function (realParent, label) {
                this._super();
                this.realParent = realParent;
                this.label = label;
            },
            relocate: function (index, target) {
                const parent = target.getParent()
                const boundingBox = parent.getBoundingBox()

                let offset = (parent instanceof draw2d.Port) ? boundingBox.h / 2 : 0

                offset += 12;
                const y = (index + 1) * this.label.height;

                if (target instanceof draw2d.Port) {
                    target.setPosition(0, y - offset)
                }
            }
        });

    }
}
