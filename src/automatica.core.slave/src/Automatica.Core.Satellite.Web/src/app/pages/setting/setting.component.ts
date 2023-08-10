import { Component, OnInit } from '@angular/core';
import { Config, ConfigService } from 'src/app/services/config.service';

@Component({
  selector: 'app-setting',
  templateUrl: './setting.component.html',
  styleUrls: ['./setting.component.scss']
})
export class SettingComponent implements OnInit {
  config?: Config;

  constructor(private configService: ConfigService) {

  }

  async ngOnInit() {
    this.config = await this.configService.getConfig();
  }


  async save() {
    await this.configService.saveConfig(this.config!);
  }

}
