import { Component, HostBinding, OnInit } from '@angular/core';
import { AuthService, ScreenService, AppInfoService } from './shared/services';
import { ThemeService } from './services/theme.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  @HostBinding('class') get getClass() {
    return Object.keys(this.screen.sizes).filter(cl => this.screen.sizes[cl]).join(' ');
  }

  constructor(private authService: AuthService, private screen: ScreenService, public appInfo: AppInfoService, private themeService: ThemeService) { }

  ngOnInit() {
    
    this.themeService.applyTheme("dark");
  }

  isAuthenticated() {
    return this.authService.loggedIn;
  }
}
