declare var draw2d: any;
declare var $: any;


export class LogicLables {
    public static addLables(logic) {

        logic.LogicText = draw2d.shape.basic.Label.extend({

            NAME: "LogicLabel",

            direction: 0,
            realParent: void 0,

            init: function (attr, direction, realParent) {
                this._super(attr);

                this.direction = direction;
                this.realParent = realParent;
            }
        });

        logic.Label = draw2d.shape.basic.Label.extend({
            NAME: "Label",

            init: function (attr) {
                this._super(attr);
            },

            getWidth() {
                return Math.max(this.width, this.minWidth);
            }

        });


        logic.MinWidthLabel = draw2d.shape.basic.Label.extend({

            getWidth() {
                return Math.max(this.width, this.minWidth);
            }
        });
    }
}
