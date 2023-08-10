import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppComponent } from './app.component';
import { SideNavOuterToolbarModule, SideNavInnerToolbarModule, SingleCardModule } from './layouts';
import { FooterModule, ResetPasswordFormModule, CreateAccountFormModule, ChangePasswordFormModule, LoginFormModule } from './shared/components';
import { AuthService, ScreenService, AppInfoService } from './shared/services';
import { UnauthenticatedContentModule } from './unauthenticated-content';
import { AppRoutingModule } from './app-routing.module';
import { ThemeService } from './services/theme.service';
import { HttpClientModule } from '@angular/common/http';
import { SettingComponent } from './pages/setting/setting.component';
import { DxButtonModule, DxDataGridModule, DxFormModule } from 'devextreme-angular';
import { ConfigService } from './services/config.service';
import { StatusService } from './shared/services/statu.service';
import { CommonModule } from '@angular/common';

@NgModule({
  declarations: [
    AppComponent,
    SettingComponent
  ],
  imports: [
    BrowserModule,
    SideNavOuterToolbarModule,
    SideNavInnerToolbarModule,
    SingleCardModule,
    FooterModule,
    ResetPasswordFormModule,
    CreateAccountFormModule,
    ChangePasswordFormModule,
    LoginFormModule,
    UnauthenticatedContentModule,
    AppRoutingModule,
    HttpClientModule,
    DxFormModule,
    DxButtonModule,
    DxDataGridModule,
    CommonModule
  ],
  providers: [
    AuthService,
    ScreenService,
    AppInfoService,
    ThemeService,
    ConfigService,
    StatusService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
