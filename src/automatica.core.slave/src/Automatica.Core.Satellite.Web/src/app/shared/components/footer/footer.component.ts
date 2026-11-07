import { Component, NgModule } from '@angular/core';

@Component({
  standalone: false,
  selector: 'app-footer',
  template: `
    <footer><ng-content></ng-content></footer>
  `,
  styleUrls: ['./footer.component.scss']
})

export class FooterComponent {

}

@NgModule({
  declarations: [ FooterComponent ],
  exports: [ FooterComponent ]
})
export class FooterModule { }
