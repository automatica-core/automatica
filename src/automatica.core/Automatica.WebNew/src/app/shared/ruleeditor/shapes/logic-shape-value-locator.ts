
declare var draw2d: any;

export const LogicShapeValueLocator = draw2d.layout.locator.Locator.extend(
    /** @lends draw2d.layout.locator.RightLocator.prototype */
    {

        NAME: "draw2d.layout.locator.LogicShapeValueLocator",

        /**
         * Constructs a locator with associated parent.
         *
         */
        init: function (attr, setter, getter) {
            this._super(attr, setter, getter)


            this.marginLeft = (attr && ("marginLeft" in attr)) ? attr.marginLeft : 5
            this.marginRight = (attr && ("marginRight" in attr)) ? attr.marginRight : 5
            this.marginBottom = (attr && ("marginBottom" in attr)) ? attr.marginBottom : 5
            this.marginTop = (attr && ("marginTop" in attr)) ? attr.marginTop : 5
        },


        /**
         * 
         * Relocates the given Figure.
         *
         * @param {Number} index child index of the target
         * @param {draw2d.Figure} target The figure to relocate
         **/
        relocate: function (index, target) {
            var parent = target.getParent()
            var boundingBox = parent.getBoundingBox()

            // I made a wrong decision in the port handling: anchor point
            // is in the center and not topLeft. Now I must correct this flaw here, and there, and...
            // shit happens.
            var offset = (parent instanceof draw2d.Port) ? boundingBox.h / 2 : 0

            if (target instanceof draw2d.Port) {
                target.setPosition(boundingBox.w + this.marginLeft - this.marginRight, (boundingBox.h / 2) - this.marginBottom + this.marginTop - offset)
            }
            else {
                var targetBoundingBox = target.getBoundingBox()
                target.setPosition(boundingBox.w + this.marginLeft - this.marginRight, (boundingBox.h / 2) - (targetBoundingBox.h / 2) - this.marginBottom + this.marginTop - offset)
            }
        }
    })