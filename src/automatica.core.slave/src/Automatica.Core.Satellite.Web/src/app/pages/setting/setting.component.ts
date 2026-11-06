import { Component, OnInit } from '@angular/core';
import notify from 'devextreme/ui/notify';
import { Config, ConfigService } from 'src/app/services/config.service';

@Component({
  selector: 'app-setting',
  templateUrl: './setting.component.html',
  styleUrls: ['./setting.component.scss']
})
export class SettingComponent implements OnInit {
  config?: Config;

  tlsModes = [
    { value: null, text: 'Automatic (switch to TLS when the core offers it)' },
    { value: true, text: 'TLS' },
    { value: false, text: 'Plaintext (not recommended)' }
  ];

  constructor(private configService: ConfigService) {

  }

  async ngOnInit() {
    this.config = await this.configService.getConfig();
  }

  get clientKeyPlaceholder(): string {
    return this.config?.hasClientKey ? 'unchanged' : 'not set';
  }

  generateKey = () => {
    if (!this.config) {
      return;
    }
    const key = ConfigService.generateKey();
    this.config = { ...this.config, clientKey: key };
    // shown once, the api never returns it: it has to be entered as the satellite key in the core
    navigator.clipboard?.writeText(key).catch(() => { });
    notify('New key generated and copied to the clipboard. Enter it as the satellite key in the core, then save.', 'info', 6000);
  }

  async save() {
    try {
      this.config = await this.configService.saveConfig(this.config!);
      notify('Saved', 'success', 2000);
    } catch (e: any) {
      notify(e?.error ?? 'Could not save the settings', 'error', 4000);
    }
  }

}
