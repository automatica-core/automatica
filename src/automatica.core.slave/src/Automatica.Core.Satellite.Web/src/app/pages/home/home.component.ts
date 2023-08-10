import { Component, OnDestroy, OnInit } from '@angular/core';
import { StatusService } from 'src/app/shared/services/statu.service';

@Component({
  templateUrl: 'home.component.html',
  styleUrls: [ './home.component.scss' ]
})

export class HomeComponent implements OnInit, OnDestroy {

  status: any = void 0;
  interval: NodeJS.Timeout | undefined;

  constructor(private statusService: StatusService) {

  }

  async ngOnInit() {
    
    await this.load();

    this.interval = setInterval(async () => {
      await this.load();
    }, 1000);

  }
  ngOnDestroy(): void {
    if(this.interval) {
      clearInterval(this.interval);
      this.interval = undefined;
    }
  }

  async load() {
    this.status = await this.statusService.getStatus();
  }
}
