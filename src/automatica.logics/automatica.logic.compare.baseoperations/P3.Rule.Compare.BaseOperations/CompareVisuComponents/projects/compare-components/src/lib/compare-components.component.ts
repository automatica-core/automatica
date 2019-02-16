import { Component, OnInit } from '@angular/core';
import { BaseMobileComponent } from 'automatica-base/base-mobile-component';
import { DataHubService } from 'automatica-base/communication/hubs/data-hub.service';


@Component({
  // tslint:disable-next-line:component-selector
  selector: 'lib-CompareComponents',
  template: `
    <p>
      compare-components works!
    </p>
  `,
  styles: []
})
export class CompareComponentsComponent extends BaseMobileComponent implements OnInit {

  constructor(dataHub: DataHubService) { super(dataHub); }

  ngOnInit() {

  }

  onItemResized() {

  }

}
