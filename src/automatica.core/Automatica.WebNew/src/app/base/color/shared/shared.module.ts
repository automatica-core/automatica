import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';

import { ColorUtilityService } from './color-utility/color-utility.service';
import { MouseHandlerDirective } from './mouse-handler/mouse-handler.directive';

@NgModule({
    imports: [CommonModule],
    declarations: [MouseHandlerDirective],
    exports: [MouseHandlerDirective],
    providers: [ColorUtilityService],
})
export class SharedModule {}
