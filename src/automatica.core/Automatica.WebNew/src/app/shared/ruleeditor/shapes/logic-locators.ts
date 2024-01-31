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
                target.setVisible(true);
                const parent = this.realParent;
                const boundingBox = parent.getBoundingBox()
                const y = this.label.y + this.label.height / 2;

                if (target instanceof draw2d.Port) {
                    target.setPosition(boundingBox.w, y);
                    this.label.setPosition(boundingBox.w, y);
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
                target.setVisible(true);
            
                const y = this.label.y + this.label.height / 2;

                if (target instanceof draw2d.Port) {
                    target.setPosition(0, y)
                }
            }
        });

    }
}
