import { NgModule } from '@angular/core';
import { CompareComponentsComponent } from './compare-components.component';
@NgModule({
  imports: [
  ],
  declarations: [CompareComponentsComponent],
  exports: [CompareComponentsComponent],
  entryComponents: [
    CompareComponentsComponent
  ],
  providers: [
    {
      provide: 'CompareComponentsComponent',
      useValue: [
        {
          name: 'lib-CompareComponents',
          component: CompareComponentsComponent
        }
      ],
      multi: true
    }
  ]
})
export class CompareComponentsModule { }
