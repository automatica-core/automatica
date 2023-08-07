"use strict";
(self["webpackChunkautomatica_satellite"] = self["webpackChunkautomatica_satellite"] || []).push([["main"],{

/***/ 8739:
/*!***********************************!*\
  !*** ./src/app/app-navigation.ts ***!
  \***********************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   navigation: () => (/* binding */ navigation)
/* harmony export */ });
const navigation = [{
  text: 'Home',
  path: '/home',
  icon: 'home'
}, {
  text: 'Settings',
  path: '/settings',
  icon: 'preferences'
}];

/***/ }),

/***/ 23966:
/*!***************************************!*\
  !*** ./src/app/app-routing.module.ts ***!
  \***************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   AppRoutingModule: () => (/* binding */ AppRoutingModule)
/* harmony export */ });
/* harmony import */ var _angular_router__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(/*! @angular/router */ 27947);
/* harmony import */ var _shared_components__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./shared/components */ 94974);
/* harmony import */ var _shared_services__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./shared/services */ 57175);
/* harmony import */ var _pages_home_home_component__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ./pages/home/home.component */ 50424);
/* harmony import */ var _pages_profile_profile_component__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ./pages/profile/profile.component */ 30862);
/* harmony import */ var _pages_tasks_tasks_component__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! ./pages/tasks/tasks.component */ 59398);
/* harmony import */ var devextreme_angular__WEBPACK_IMPORTED_MODULE_8__ = __webpack_require__(/*! devextreme-angular */ 3965);
/* harmony import */ var devextreme_angular__WEBPACK_IMPORTED_MODULE_9__ = __webpack_require__(/*! devextreme-angular */ 93739);
/* harmony import */ var _pages_setting_setting_component__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! ./pages/setting/setting.component */ 199);
/* harmony import */ var _angular_common__WEBPACK_IMPORTED_MODULE_11__ = __webpack_require__(/*! @angular/common */ 26575);
/* harmony import */ var devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_10__ = __webpack_require__(/*! devextreme-angular/ui/nested */ 71723);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! @angular/core */ 61699);












const routes = [{
  path: 'tasks',
  component: _pages_tasks_tasks_component__WEBPACK_IMPORTED_MODULE_4__.TasksComponent,
  canActivate: [_shared_services__WEBPACK_IMPORTED_MODULE_1__.AuthGuardService]
}, {
  path: 'profile',
  component: _pages_profile_profile_component__WEBPACK_IMPORTED_MODULE_3__.ProfileComponent,
  canActivate: [_shared_services__WEBPACK_IMPORTED_MODULE_1__.AuthGuardService]
}, {
  path: 'home',
  component: _pages_home_home_component__WEBPACK_IMPORTED_MODULE_2__.HomeComponent,
  canActivate: [_shared_services__WEBPACK_IMPORTED_MODULE_1__.AuthGuardService]
}, {
  path: 'settings',
  component: _pages_setting_setting_component__WEBPACK_IMPORTED_MODULE_5__.SettingComponent,
  canActivate: [_shared_services__WEBPACK_IMPORTED_MODULE_1__.AuthGuardService]
}, {
  path: 'login-form',
  component: _shared_components__WEBPACK_IMPORTED_MODULE_0__.LoginFormComponent,
  canActivate: [_shared_services__WEBPACK_IMPORTED_MODULE_1__.AuthGuardService]
}, {
  path: 'reset-password',
  component: _shared_components__WEBPACK_IMPORTED_MODULE_0__.ResetPasswordFormComponent,
  canActivate: [_shared_services__WEBPACK_IMPORTED_MODULE_1__.AuthGuardService]
}, {
  path: 'create-account',
  component: _shared_components__WEBPACK_IMPORTED_MODULE_0__.CreateAccountFormComponent,
  canActivate: [_shared_services__WEBPACK_IMPORTED_MODULE_1__.AuthGuardService]
}, {
  path: 'change-password/:recoveryCode',
  component: _shared_components__WEBPACK_IMPORTED_MODULE_0__.ChangePasswordFormComponent,
  canActivate: [_shared_services__WEBPACK_IMPORTED_MODULE_1__.AuthGuardService]
}, {
  path: '**',
  redirectTo: 'home'
}];
class AppRoutingModule {}
AppRoutingModule.ɵfac = function AppRoutingModule_Factory(t) {
  return new (t || AppRoutingModule)();
};
AppRoutingModule.ɵmod = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵdefineNgModule"]({
  type: AppRoutingModule
});
AppRoutingModule.ɵinj = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵdefineInjector"]({
  providers: [_shared_services__WEBPACK_IMPORTED_MODULE_1__.AuthGuardService],
  imports: [_angular_router__WEBPACK_IMPORTED_MODULE_7__.RouterModule.forRoot(routes, {
    useHash: true
  }), devextreme_angular__WEBPACK_IMPORTED_MODULE_8__.DxDataGridModule, devextreme_angular__WEBPACK_IMPORTED_MODULE_9__.DxFormModule, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_10__.DxiItemModule, _angular_common__WEBPACK_IMPORTED_MODULE_11__.CommonModule, _angular_router__WEBPACK_IMPORTED_MODULE_7__.RouterModule]
});
(function () {
  (typeof ngJitMode === "undefined" || ngJitMode) && _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵsetNgModuleScope"](AppRoutingModule, {
    declarations: [_pages_home_home_component__WEBPACK_IMPORTED_MODULE_2__.HomeComponent, _pages_profile_profile_component__WEBPACK_IMPORTED_MODULE_3__.ProfileComponent, _pages_tasks_tasks_component__WEBPACK_IMPORTED_MODULE_4__.TasksComponent],
    imports: [_angular_router__WEBPACK_IMPORTED_MODULE_7__.RouterModule, devextreme_angular__WEBPACK_IMPORTED_MODULE_8__.DxDataGridModule, devextreme_angular__WEBPACK_IMPORTED_MODULE_9__.DxFormModule, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_10__.DxiItemModule, _angular_common__WEBPACK_IMPORTED_MODULE_11__.CommonModule],
    exports: [_angular_router__WEBPACK_IMPORTED_MODULE_7__.RouterModule]
  });
})();

/***/ }),

/***/ 66401:
/*!**********************************!*\
  !*** ./src/app/app.component.ts ***!
  \**********************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   AppComponent: () => (/* binding */ AppComponent)
/* harmony export */ });
/* harmony import */ var C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./node_modules/@babel/runtime/helpers/esm/asyncToGenerator.js */ 71670);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var _shared_services__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./shared/services */ 57175);
/* harmony import */ var _services_theme_service__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ./services/theme.service */ 42471);
/* harmony import */ var _angular_common__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(/*! @angular/common */ 26575);
/* harmony import */ var _layouts_side_nav_inner_toolbar_side_nav_inner_toolbar_component__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ./layouts/side-nav-inner-toolbar/side-nav-inner-toolbar.component */ 77843);
/* harmony import */ var _shared_components_footer_footer_component__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! ./shared/components/footer/footer.component */ 68014);
/* harmony import */ var _unauthenticated_content__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! ./unauthenticated-content */ 40203);
/* harmony import */ var _angular_router__WEBPACK_IMPORTED_MODULE_8__ = __webpack_require__(/*! @angular/router */ 27947);









function AppComponent_ng_container_0_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵelementContainerStart"](0);
    _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵelementStart"](1, "app-side-nav-inner-toolbar", 2);
    _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵelement"](2, "router-outlet");
    _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵelementStart"](3, "app-footer");
    _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵtext"](4);
    _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵelement"](5, "br");
    _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵtext"](6, " All trademarks or registered trademarks are property of their respective owners. ");
    _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵelementEnd"]()();
    _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵelementContainerEnd"]();
  }
  if (rf & 2) {
    const ctx_r0 = _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵnextContext"]();
    _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵadvance"](1);
    _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵpropertyInterpolate"]("title", ctx_r0.appInfo.title);
    _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵadvance"](3);
    _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵtextInterpolate2"](" Copyright \u00A9 2011-", ctx_r0.appInfo.currentYear, " ", ctx_r0.appInfo.title, " Inc. ");
  }
}
function AppComponent_ng_template_1_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵelement"](0, "app-unauthenticated-content");
  }
}
class AppComponent {
  get getClass() {
    return Object.keys(this.screen.sizes).filter(cl => this.screen.sizes[cl]).join(' ');
  }
  constructor(authService, screen, appInfo, themeService) {
    this.authService = authService;
    this.screen = screen;
    this.appInfo = appInfo;
    this.themeService = themeService;
  }
  ngOnInit() {
    var _this = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      _this.themeService.applyTheme("dark");
      yield _this.authService.loadIsReady();
    })();
  }
  isAuthenticated() {
    return this.authService.loggedIn;
  }
}
AppComponent.ɵfac = function AppComponent_Factory(t) {
  return new (t || AppComponent)(_angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵdirectiveInject"](_shared_services__WEBPACK_IMPORTED_MODULE_1__.AuthService), _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵdirectiveInject"](_shared_services__WEBPACK_IMPORTED_MODULE_1__.ScreenService), _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵdirectiveInject"](_shared_services__WEBPACK_IMPORTED_MODULE_1__.AppInfoService), _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵdirectiveInject"](_services_theme_service__WEBPACK_IMPORTED_MODULE_2__.ThemeService));
};
AppComponent.ɵcmp = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵdefineComponent"]({
  type: AppComponent,
  selectors: [["app-root"]],
  hostVars: 2,
  hostBindings: function AppComponent_HostBindings(rf, ctx) {
    if (rf & 2) {
      _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵclassMap"](ctx.getClass);
    }
  },
  decls: 3,
  vars: 2,
  consts: [[4, "ngIf", "ngIfElse"], ["unauthenticated", ""], [2, "min-width", "400px", 3, "title"]],
  template: function AppComponent_Template(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵtemplate"](0, AppComponent_ng_container_0_Template, 7, 3, "ng-container", 0);
      _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵtemplate"](1, AppComponent_ng_template_1_Template, 1, 0, "ng-template", null, 1, _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵtemplateRefExtractor"]);
    }
    if (rf & 2) {
      const _r1 = _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵreference"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_6__["ɵɵproperty"]("ngIf", ctx.isAuthenticated())("ngIfElse", _r1);
    }
  },
  dependencies: [_angular_common__WEBPACK_IMPORTED_MODULE_7__.NgIf, _layouts_side_nav_inner_toolbar_side_nav_inner_toolbar_component__WEBPACK_IMPORTED_MODULE_3__.SideNavInnerToolbarComponent, _shared_components_footer_footer_component__WEBPACK_IMPORTED_MODULE_4__.FooterComponent, _unauthenticated_content__WEBPACK_IMPORTED_MODULE_5__.UnauthenticatedContentComponent, _angular_router__WEBPACK_IMPORTED_MODULE_8__.RouterOutlet],
  styles: ["[_nghost-%COMP%] {\n  background-color: #f2f2f2;\n}\n/*# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbIndlYnBhY2s6Ly8uL3NyYy9hcHAvYXBwLmNvbXBvbmVudC5zY3NzIl0sIm5hbWVzIjpbXSwibWFwcGluZ3MiOiJBQUFBO0VBRUUseUJBQUE7QUFBRiIsInNvdXJjZXNDb250ZW50IjpbIjpob3N0IHtcbiAgQGltcG9ydCBcIi4uL3RoZW1lcy9nZW5lcmF0ZWQvdmFyaWFibGVzLmJhc2Uuc2Nzc1wiO1xuICBiYWNrZ3JvdW5kLWNvbG9yOiBkYXJrZW4oJGJhc2UtYmcsIDUuMDApO1xufVxuIl0sInNvdXJjZVJvb3QiOiIifQ== */"]
});

/***/ }),

/***/ 78629:
/*!*******************************!*\
  !*** ./src/app/app.module.ts ***!
  \*******************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   AppModule: () => (/* binding */ AppModule)
/* harmony export */ });
/* harmony import */ var _angular_platform_browser__WEBPACK_IMPORTED_MODULE_11__ = __webpack_require__(/*! @angular/platform-browser */ 36480);
/* harmony import */ var _app_component__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./app.component */ 66401);
/* harmony import */ var _layouts__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./layouts */ 81447);
/* harmony import */ var _shared_components__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ./shared/components */ 94974);
/* harmony import */ var _shared_services__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ./shared/services */ 57175);
/* harmony import */ var _unauthenticated_content__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! ./unauthenticated-content */ 40203);
/* harmony import */ var _app_routing_module__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! ./app-routing.module */ 23966);
/* harmony import */ var _services_theme_service__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! ./services/theme.service */ 42471);
/* harmony import */ var _angular_common_http__WEBPACK_IMPORTED_MODULE_12__ = __webpack_require__(/*! @angular/common/http */ 54860);
/* harmony import */ var _pages_setting_setting_component__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(/*! ./pages/setting/setting.component */ 199);
/* harmony import */ var devextreme_angular__WEBPACK_IMPORTED_MODULE_13__ = __webpack_require__(/*! devextreme-angular */ 93739);
/* harmony import */ var devextreme_angular__WEBPACK_IMPORTED_MODULE_14__ = __webpack_require__(/*! devextreme-angular */ 64766);
/* harmony import */ var devextreme_angular__WEBPACK_IMPORTED_MODULE_15__ = __webpack_require__(/*! devextreme-angular */ 3965);
/* harmony import */ var _services_config_service__WEBPACK_IMPORTED_MODULE_8__ = __webpack_require__(/*! ./services/config.service */ 342);
/* harmony import */ var _shared_services_statu_service__WEBPACK_IMPORTED_MODULE_9__ = __webpack_require__(/*! ./shared/services/statu.service */ 761);
/* harmony import */ var _angular_common__WEBPACK_IMPORTED_MODULE_16__ = __webpack_require__(/*! @angular/common */ 26575);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_10__ = __webpack_require__(/*! @angular/core */ 61699);















class AppModule {}
AppModule.ɵfac = function AppModule_Factory(t) {
  return new (t || AppModule)();
};
AppModule.ɵmod = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_10__["ɵɵdefineNgModule"]({
  type: AppModule,
  bootstrap: [_app_component__WEBPACK_IMPORTED_MODULE_0__.AppComponent]
});
AppModule.ɵinj = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_10__["ɵɵdefineInjector"]({
  providers: [_shared_services__WEBPACK_IMPORTED_MODULE_3__.AuthService, _shared_services__WEBPACK_IMPORTED_MODULE_3__.ScreenService, _shared_services__WEBPACK_IMPORTED_MODULE_3__.AppInfoService, _services_theme_service__WEBPACK_IMPORTED_MODULE_6__.ThemeService, _services_config_service__WEBPACK_IMPORTED_MODULE_8__.ConfigService, _shared_services_statu_service__WEBPACK_IMPORTED_MODULE_9__.StatusService],
  imports: [_angular_platform_browser__WEBPACK_IMPORTED_MODULE_11__.BrowserModule, _layouts__WEBPACK_IMPORTED_MODULE_1__.SideNavOuterToolbarModule, _layouts__WEBPACK_IMPORTED_MODULE_1__.SideNavInnerToolbarModule, _layouts__WEBPACK_IMPORTED_MODULE_1__.SingleCardModule, _shared_components__WEBPACK_IMPORTED_MODULE_2__.FooterModule, _shared_components__WEBPACK_IMPORTED_MODULE_2__.ResetPasswordFormModule, _shared_components__WEBPACK_IMPORTED_MODULE_2__.CreateAccountFormModule, _shared_components__WEBPACK_IMPORTED_MODULE_2__.ChangePasswordFormModule, _shared_components__WEBPACK_IMPORTED_MODULE_2__.LoginFormModule, _unauthenticated_content__WEBPACK_IMPORTED_MODULE_4__.UnauthenticatedContentModule, _app_routing_module__WEBPACK_IMPORTED_MODULE_5__.AppRoutingModule, _angular_common_http__WEBPACK_IMPORTED_MODULE_12__.HttpClientModule, devextreme_angular__WEBPACK_IMPORTED_MODULE_13__.DxFormModule, devextreme_angular__WEBPACK_IMPORTED_MODULE_14__.DxButtonModule, devextreme_angular__WEBPACK_IMPORTED_MODULE_15__.DxDataGridModule, _angular_common__WEBPACK_IMPORTED_MODULE_16__.CommonModule]
});
(function () {
  (typeof ngJitMode === "undefined" || ngJitMode) && _angular_core__WEBPACK_IMPORTED_MODULE_10__["ɵɵsetNgModuleScope"](AppModule, {
    declarations: [_app_component__WEBPACK_IMPORTED_MODULE_0__.AppComponent, _pages_setting_setting_component__WEBPACK_IMPORTED_MODULE_7__.SettingComponent],
    imports: [_angular_platform_browser__WEBPACK_IMPORTED_MODULE_11__.BrowserModule, _layouts__WEBPACK_IMPORTED_MODULE_1__.SideNavOuterToolbarModule, _layouts__WEBPACK_IMPORTED_MODULE_1__.SideNavInnerToolbarModule, _layouts__WEBPACK_IMPORTED_MODULE_1__.SingleCardModule, _shared_components__WEBPACK_IMPORTED_MODULE_2__.FooterModule, _shared_components__WEBPACK_IMPORTED_MODULE_2__.ResetPasswordFormModule, _shared_components__WEBPACK_IMPORTED_MODULE_2__.CreateAccountFormModule, _shared_components__WEBPACK_IMPORTED_MODULE_2__.ChangePasswordFormModule, _shared_components__WEBPACK_IMPORTED_MODULE_2__.LoginFormModule, _unauthenticated_content__WEBPACK_IMPORTED_MODULE_4__.UnauthenticatedContentModule, _app_routing_module__WEBPACK_IMPORTED_MODULE_5__.AppRoutingModule, _angular_common_http__WEBPACK_IMPORTED_MODULE_12__.HttpClientModule, devextreme_angular__WEBPACK_IMPORTED_MODULE_13__.DxFormModule, devextreme_angular__WEBPACK_IMPORTED_MODULE_14__.DxButtonModule, devextreme_angular__WEBPACK_IMPORTED_MODULE_15__.DxDataGridModule, _angular_common__WEBPACK_IMPORTED_MODULE_16__.CommonModule]
  });
})();

/***/ }),

/***/ 81447:
/*!**********************************!*\
  !*** ./src/app/layouts/index.ts ***!
  \**********************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   SideNavInnerToolbarComponent: () => (/* reexport safe */ _side_nav_inner_toolbar_side_nav_inner_toolbar_component__WEBPACK_IMPORTED_MODULE_1__.SideNavInnerToolbarComponent),
/* harmony export */   SideNavInnerToolbarModule: () => (/* reexport safe */ _side_nav_inner_toolbar_side_nav_inner_toolbar_component__WEBPACK_IMPORTED_MODULE_1__.SideNavInnerToolbarModule),
/* harmony export */   SideNavOuterToolbarComponent: () => (/* reexport safe */ _side_nav_outer_toolbar_side_nav_outer_toolbar_component__WEBPACK_IMPORTED_MODULE_0__.SideNavOuterToolbarComponent),
/* harmony export */   SideNavOuterToolbarModule: () => (/* reexport safe */ _side_nav_outer_toolbar_side_nav_outer_toolbar_component__WEBPACK_IMPORTED_MODULE_0__.SideNavOuterToolbarModule),
/* harmony export */   SingleCardComponent: () => (/* reexport safe */ _single_card_single_card_component__WEBPACK_IMPORTED_MODULE_2__.SingleCardComponent),
/* harmony export */   SingleCardModule: () => (/* reexport safe */ _single_card_single_card_component__WEBPACK_IMPORTED_MODULE_2__.SingleCardModule)
/* harmony export */ });
/* harmony import */ var _side_nav_outer_toolbar_side_nav_outer_toolbar_component__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./side-nav-outer-toolbar/side-nav-outer-toolbar.component */ 44134);
/* harmony import */ var _side_nav_inner_toolbar_side_nav_inner_toolbar_component__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./side-nav-inner-toolbar/side-nav-inner-toolbar.component */ 77843);
/* harmony import */ var _single_card_single_card_component__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ./single-card/single-card.component */ 61010);




/***/ }),

/***/ 77843:
/*!************************************************************************************!*\
  !*** ./src/app/layouts/side-nav-inner-toolbar/side-nav-inner-toolbar.component.ts ***!
  \************************************************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   SideNavInnerToolbarComponent: () => (/* binding */ SideNavInnerToolbarComponent),
/* harmony export */   SideNavInnerToolbarModule: () => (/* binding */ SideNavInnerToolbarModule)
/* harmony export */ });
/* harmony import */ var _shared_components__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../../shared/components */ 94974);
/* harmony import */ var devextreme_angular_ui_drawer__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(/*! devextreme-angular/ui/drawer */ 73850);
/* harmony import */ var devextreme_angular_ui_scroll_view__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! devextreme-angular/ui/scroll-view */ 15018);
/* harmony import */ var devextreme_angular_ui_toolbar__WEBPACK_IMPORTED_MODULE_9__ = __webpack_require__(/*! devextreme-angular/ui/toolbar */ 70493);
/* harmony import */ var _angular_common__WEBPACK_IMPORTED_MODULE_11__ = __webpack_require__(/*! @angular/common */ 26575);
/* harmony import */ var _angular_router__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! @angular/router */ 27947);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var _shared_services__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../../shared/services */ 57175);
/* harmony import */ var _shared_components_side_navigation_menu_side_navigation_menu_component__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ../../shared/components/side-navigation-menu/side-navigation-menu.component */ 5994);
/* harmony import */ var devextreme_angular_core__WEBPACK_IMPORTED_MODULE_8__ = __webpack_require__(/*! devextreme-angular/core */ 65787);
/* harmony import */ var _shared_components_header_header_component__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ../../shared/components/header/header.component */ 10074);
/* harmony import */ var devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_10__ = __webpack_require__(/*! devextreme-angular/ui/nested */ 71723);

















const _c0 = function (a2) {
  return {
    icon: "menu",
    stylingMode: "text",
    onClick: a2
  };
};
function SideNavInnerToolbarComponent_app_side_navigation_menu_1_dxi_item_2_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelement"](0, "dxi-item", 10);
  }
  if (rf & 2) {
    const ctx_r2 = _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵnextContext"](2);
    _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵproperty"]("options", _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵpureFunction1"](1, _c0, ctx_r2.toggleMenu));
  }
}
function SideNavInnerToolbarComponent_app_side_navigation_menu_1_Template(rf, ctx) {
  if (rf & 1) {
    const _r4 = _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵgetCurrentView"]();
    _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementStart"](0, "app-side-navigation-menu", 7);
    _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵlistener"]("selectedItemChanged", function SideNavInnerToolbarComponent_app_side_navigation_menu_1_Template_app_side_navigation_menu_selectedItemChanged_0_listener($event) {
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵrestoreView"](_r4);
      const ctx_r3 = _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵnextContext"]();
      return _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵresetView"](ctx_r3.navigationChanged($event));
    })("openMenu", function SideNavInnerToolbarComponent_app_side_navigation_menu_1_Template_app_side_navigation_menu_openMenu_0_listener() {
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵrestoreView"](_r4);
      const ctx_r5 = _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵnextContext"]();
      return _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵresetView"](ctx_r5.navigationClick());
    });
    _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementStart"](1, "dx-toolbar", 8);
    _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵtemplate"](2, SideNavInnerToolbarComponent_app_side_navigation_menu_1_dxi_item_2_Template, 1, 3, "dxi-item", 9);
    _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementEnd"]()();
  }
  if (rf & 2) {
    const ctx_r0 = _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵnextContext"]();
    _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵproperty"]("compactMode", !ctx_r0.menuOpened)("selectedItem", ctx_r0.selectedRoute);
    _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵadvance"](2);
    _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵproperty"]("ngIf", ctx_r0.minMenuSize !== 0);
  }
}
const _c1 = ["*", [["app-footer"]]];
const _c2 = ["*", "app-footer"];
class SideNavInnerToolbarComponent {
  constructor(screen, router) {
    this.screen = screen;
    this.router = router;
    this.selectedRoute = '';
    this.temporaryMenuOpened = false;
    this.menuMode = 'shrink';
    this.menuRevealMode = 'expand';
    this.minMenuSize = 0;
    this.shaderEnabled = false;
    this.toggleMenu = e => {
      this.menuOpened = !this.menuOpened;
      e.event?.stopPropagation();
    };
  }
  ngOnInit() {
    this.menuOpened = this.screen.sizes['screen-large'];
    this.router.events.subscribe(val => {
      if (val instanceof _angular_router__WEBPACK_IMPORTED_MODULE_5__.NavigationEnd) {
        this.selectedRoute = val.urlAfterRedirects.split('?')[0];
      }
    });
    this.screen.changed.subscribe(() => this.updateDrawer());
    this.updateDrawer();
  }
  updateDrawer() {
    const isXSmall = this.screen.sizes['screen-x-small'];
    const isLarge = this.screen.sizes['screen-large'];
    this.menuMode = isLarge ? 'shrink' : 'overlap';
    this.menuRevealMode = isXSmall ? 'slide' : 'expand';
    this.minMenuSize = isXSmall ? 0 : 60;
    this.shaderEnabled = !isLarge;
  }
  get hideMenuAfterNavigation() {
    return this.menuMode === 'overlap' || this.temporaryMenuOpened;
  }
  get showMenuAfterClick() {
    return !this.menuOpened;
  }
  navigationChanged(event) {
    const path = event.itemData.path;
    const pointerEvent = event.event;
    if (path && this.menuOpened) {
      if (event.node?.selected) {
        pointerEvent?.preventDefault();
      } else {
        this.router.navigate([path]);
        this.scrollView.instance.scrollTo(0);
      }
      if (this.hideMenuAfterNavigation) {
        this.temporaryMenuOpened = false;
        this.menuOpened = false;
        pointerEvent?.stopPropagation();
      }
    } else {
      pointerEvent?.preventDefault();
    }
  }
  navigationClick() {
    if (this.showMenuAfterClick) {
      this.temporaryMenuOpened = true;
      this.menuOpened = true;
    }
  }
}
SideNavInnerToolbarComponent.ɵfac = function SideNavInnerToolbarComponent_Factory(t) {
  return new (t || SideNavInnerToolbarComponent)(_angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵdirectiveInject"](_shared_services__WEBPACK_IMPORTED_MODULE_1__.ScreenService), _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵdirectiveInject"](_angular_router__WEBPACK_IMPORTED_MODULE_5__.Router));
};
SideNavInnerToolbarComponent.ɵcmp = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵdefineComponent"]({
  type: SideNavInnerToolbarComponent,
  selectors: [["app-side-nav-inner-toolbar"]],
  viewQuery: function SideNavInnerToolbarComponent_Query(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵviewQuery"](devextreme_angular_ui_scroll_view__WEBPACK_IMPORTED_MODULE_6__.DxScrollViewComponent, 7);
    }
    if (rf & 2) {
      let _t;
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵqueryRefresh"](_t = _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵloadQuery"]()) && (ctx.scrollView = _t.first);
    }
  },
  inputs: {
    title: "title"
  },
  ngContentSelectors: _c2,
  decls: 9,
  vars: 9,
  consts: [["position", "before", 1, "drawer", 3, "closeOnOutsideClick", "openedStateMode", "revealMode", "minSize", "shading", "opened", "openedChange"], ["class", "dx-swatch-additional", 3, "compactMode", "selectedItem", "selectedItemChanged", "openMenu", 4, "dxTemplate", "dxTemplateOf"], [1, "container"], [3, "menuToggleEnabled", "title", "menuToggle"], [1, "layout-body", "with-footer"], [1, "content"], [1, "content-block"], [1, "dx-swatch-additional", 3, "compactMode", "selectedItem", "selectedItemChanged", "openMenu"], ["id", "navigation-header"], ["location", "before", "cssClass", "menu-button", "widget", "dxButton", 3, "options", 4, "ngIf"], ["location", "before", "cssClass", "menu-button", "widget", "dxButton", 3, "options"]],
  template: function SideNavInnerToolbarComponent_Template(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵprojectionDef"](_c1);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementStart"](0, "dx-drawer", 0);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵlistener"]("openedChange", function SideNavInnerToolbarComponent_Template_dx_drawer_openedChange_0_listener($event) {
        return ctx.menuOpened = $event;
      });
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵtemplate"](1, SideNavInnerToolbarComponent_app_side_navigation_menu_1_Template, 3, 3, "app-side-navigation-menu", 1);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementStart"](2, "div", 2)(3, "app-header", 3);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵlistener"]("menuToggle", function SideNavInnerToolbarComponent_Template_app_header_menuToggle_3_listener() {
        return ctx.menuOpened = !ctx.menuOpened;
      });
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementStart"](4, "dx-scroll-view", 4)(5, "div", 5);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵprojection"](6);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementStart"](7, "div", 6);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵprojection"](8, 1);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementEnd"]()()()();
    }
    if (rf & 2) {
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵproperty"]("closeOnOutsideClick", ctx.shaderEnabled)("openedStateMode", ctx.menuMode)("revealMode", ctx.menuRevealMode)("minSize", ctx.minMenuSize)("shading", ctx.shaderEnabled)("opened", ctx.menuOpened);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵproperty"]("dxTemplateOf", "panel");
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵproperty"]("menuToggleEnabled", ctx.minMenuSize === 0)("title", ctx.title);
    }
  },
  dependencies: [_shared_components_side_navigation_menu_side_navigation_menu_component__WEBPACK_IMPORTED_MODULE_2__.SideNavigationMenuComponent, devextreme_angular_ui_drawer__WEBPACK_IMPORTED_MODULE_7__.DxDrawerComponent, devextreme_angular_core__WEBPACK_IMPORTED_MODULE_8__.DxTemplateDirective, _shared_components_header_header_component__WEBPACK_IMPORTED_MODULE_3__.HeaderComponent, devextreme_angular_ui_toolbar__WEBPACK_IMPORTED_MODULE_9__.DxToolbarComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_10__.DxiItemComponent, devextreme_angular_ui_scroll_view__WEBPACK_IMPORTED_MODULE_6__.DxScrollViewComponent, _angular_common__WEBPACK_IMPORTED_MODULE_11__.NgIf],
  styles: ["[_nghost-%COMP%] {\n  width: 100%;\n}\n\n.drawer[_ngcontent-%COMP%]     h2 {\n  font-size: 30px;\n  margin-top: 20px;\n  margin-bottom: 20px;\n}\n.drawer[_ngcontent-%COMP%]     h2, .drawer[_ngcontent-%COMP%]     .content-block, .drawer[_ngcontent-%COMP%]   .footer[_ngcontent-%COMP%] {\n  margin-left: 20px;\n  margin-right: 20px;\n  margin-top: 10px;\n}\n.drawer[_ngcontent-%COMP%]     .dx-card {\n  padding: 20px;\n}\n\n  .dx-toolbar {\n  background-color: #325862 !important;\n}\n\n#navigation-header[_ngcontent-%COMP%] {\n  background-color: #457987 !important;\n  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.12), 0 1px 2px rgba(0, 0, 0, 0.24);\n}\n#navigation-header[_ngcontent-%COMP%]     .menu-button .dx-icon {\n  color: rgba(255, 255, 255, 0.87);\n}\n.screen-x-small[_nghost-%COMP%]   #navigation-header[_ngcontent-%COMP%], .screen-x-small   [_nghost-%COMP%]   #navigation-header[_ngcontent-%COMP%] {\n  padding-left: 20px;\n}\n.dx-theme-generic[_nghost-%COMP%]   #navigation-header[_ngcontent-%COMP%], .dx-theme-generic   [_nghost-%COMP%]   #navigation-header[_ngcontent-%COMP%] {\n  padding-top: 10px;\n  padding-bottom: 10px;\n}\n/*# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbIndlYnBhY2s6Ly8uL3NyYy9hcHAvbGF5b3V0cy9zaWRlLW5hdi1pbm5lci10b29sYmFyL3NpZGUtbmF2LWlubmVyLXRvb2xiYXIuY29tcG9uZW50LnNjc3MiLCJ3ZWJwYWNrOi8vLi9zcmMvdGhlbWVzL2dlbmVyYXRlZC92YXJpYWJsZXMuYmFzZS5kYXJrLnNjc3MiXSwibmFtZXMiOltdLCJtYXBwaW5ncyI6IkFBSUE7RUFDRSxXQUFBO0FBSEY7O0FBT0U7RUFDSSxlQUFBO0VBQ0EsZ0JBQUE7RUFDQSxtQkFBQTtBQUpOO0FBT0U7RUFDSSxpQkFBQTtFQUNBLGtCQUFBO0VBQ0EsZ0JBQUE7QUFMTjtBQVFFO0VBQ0ksYUFBQTtBQU5OOztBQVVBO0VBQ0Usb0NBQUE7QUFQRjs7QUFVQTtFQUVFLG9DQUFBO0VBQ0Esd0VBQUE7QUFSRjtBQVVFO0VBQ0UsZ0NDbkNjO0FEMkJsQjtBQVdFO0VBQ0Usa0JBQUE7QUFUSjtBQVlFO0VBQ0UsaUJBQUE7RUFDQSxvQkFBQTtBQVZKIiwic291cmNlc0NvbnRlbnQiOlsiQGltcG9ydCBcIi4uLy4uLy4uL3RoZW1lcy9nZW5lcmF0ZWQvdmFyaWFibGVzLmFkZGl0aW9uYWwuc2Nzc1wiO1xuQGltcG9ydCBcIi4uLy4uLy4uL3RoZW1lcy9nZW5lcmF0ZWQvdmFyaWFibGVzLmJhc2UuZGFyay5zY3NzXCI7XG4gXG5cbjpob3N0IHtcbiAgd2lkdGg6IDEwMCU7XG59XG5cbi5kcmF3ZXIge1xuICA6Om5nLWRlZXAgaDIge1xuICAgICAgZm9udC1zaXplOiAzMHB4O1xuICAgICAgbWFyZ2luLXRvcDogMjBweDtcbiAgICAgIG1hcmdpbi1ib3R0b206IDIwcHg7XG4gIH1cblxuICA6Om5nLWRlZXAgaDIsIDo6bmctZGVlcCAuY29udGVudC1ibG9jaywgLmZvb3RlciB7XG4gICAgICBtYXJnaW4tbGVmdDogMjBweDtcbiAgICAgIG1hcmdpbi1yaWdodDogMjBweDtcbiAgICAgIG1hcmdpbi10b3A6IDEwcHg7XG4gIH1cblxuICA6Om5nLWRlZXAgLmR4LWNhcmQge1xuICAgICAgcGFkZGluZzogMjBweDtcbiAgfVxufVxuXG46Om5nLWRlZXAgLmR4LXRvb2xiYXIge1xuICBiYWNrZ3JvdW5kLWNvbG9yOiAkYmFzZS1hY2NlbnQyIWltcG9ydGFudDtcbn1cblxuI25hdmlnYXRpb24taGVhZGVyIHtcbiBcbiAgYmFja2dyb3VuZC1jb2xvcjogJGJhc2UtYWNjZW50IWltcG9ydGFudDtcbiAgYm94LXNoYWRvdzogMCAxcHggM3B4IHJnYmEoMCwgMCwgMCwgMC4xMiksIDAgMXB4IDJweCByZ2JhKDAsIDAsIDAsIDAuMjQpO1xuXG4gIDo6bmctZGVlcCAubWVudS1idXR0b24gLmR4LWljb24ge1xuICAgIGNvbG9yOiAkYmFzZS10ZXh0LWNvbG9yO1xuICB9XG5cbiAgOmhvc3QtY29udGV4dCguc2NyZWVuLXgtc21hbGwpICYge1xuICAgIHBhZGRpbmctbGVmdDogMjBweDtcbiAgfVxuXG4gIDpob3N0LWNvbnRleHQoLmR4LXRoZW1lLWdlbmVyaWMpICYge1xuICAgIHBhZGRpbmctdG9wOiAxMHB4O1xuICAgIHBhZGRpbmctYm90dG9tOiAxMHB4O1xuICB9XG59XG4iLCIkYmFzZS1iZzogIzJhMmEyYTtcbiRiYXNlLXRleHQtY29sb3I6IHJnYmEoMjU1LCAyNTUsIDI1NSwgMC44Nyk7XG4kYmFzZS1ib3JkZXItY29sb3I6ICM0ZDRkNGQ7XG4kYmFzZS1ib3JkZXItcmFkaXVzOiA0cHg7XG4kYmFzZS1hY2NlbnQ6ICM0NTc5ODc7XG4kYmFzZS1hY2NlbnQyOiAjMzI1ODYyO1xuIl0sInNvdXJjZVJvb3QiOiIifQ== */"]
});
class SideNavInnerToolbarModule {}
SideNavInnerToolbarModule.ɵfac = function SideNavInnerToolbarModule_Factory(t) {
  return new (t || SideNavInnerToolbarModule)();
};
SideNavInnerToolbarModule.ɵmod = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵdefineNgModule"]({
  type: SideNavInnerToolbarModule
});
SideNavInnerToolbarModule.ɵinj = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵdefineInjector"]({
  imports: [_shared_components__WEBPACK_IMPORTED_MODULE_0__.SideNavigationMenuModule, devextreme_angular_ui_drawer__WEBPACK_IMPORTED_MODULE_7__.DxDrawerModule, _shared_components__WEBPACK_IMPORTED_MODULE_0__.HeaderModule, devextreme_angular_ui_toolbar__WEBPACK_IMPORTED_MODULE_9__.DxToolbarModule, devextreme_angular_ui_scroll_view__WEBPACK_IMPORTED_MODULE_6__.DxScrollViewModule, _angular_common__WEBPACK_IMPORTED_MODULE_11__.CommonModule]
});
(function () {
  (typeof ngJitMode === "undefined" || ngJitMode) && _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵsetNgModuleScope"](SideNavInnerToolbarModule, {
    declarations: [SideNavInnerToolbarComponent],
    imports: [_shared_components__WEBPACK_IMPORTED_MODULE_0__.SideNavigationMenuModule, devextreme_angular_ui_drawer__WEBPACK_IMPORTED_MODULE_7__.DxDrawerModule, _shared_components__WEBPACK_IMPORTED_MODULE_0__.HeaderModule, devextreme_angular_ui_toolbar__WEBPACK_IMPORTED_MODULE_9__.DxToolbarModule, devextreme_angular_ui_scroll_view__WEBPACK_IMPORTED_MODULE_6__.DxScrollViewModule, _angular_common__WEBPACK_IMPORTED_MODULE_11__.CommonModule],
    exports: [SideNavInnerToolbarComponent]
  });
})();

/***/ }),

/***/ 44134:
/*!************************************************************************************!*\
  !*** ./src/app/layouts/side-nav-outer-toolbar/side-nav-outer-toolbar.component.ts ***!
  \************************************************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   SideNavOuterToolbarComponent: () => (/* binding */ SideNavOuterToolbarComponent),
/* harmony export */   SideNavOuterToolbarModule: () => (/* binding */ SideNavOuterToolbarModule)
/* harmony export */ });
/* harmony import */ var _shared_components__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../../shared/components */ 94974);
/* harmony import */ var devextreme_angular_ui_drawer__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(/*! devextreme-angular/ui/drawer */ 73850);
/* harmony import */ var devextreme_angular_ui_scroll_view__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! devextreme-angular/ui/scroll-view */ 15018);
/* harmony import */ var _angular_common__WEBPACK_IMPORTED_MODULE_9__ = __webpack_require__(/*! @angular/common */ 26575);
/* harmony import */ var _angular_router__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! @angular/router */ 27947);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var _shared_services__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../../shared/services */ 57175);
/* harmony import */ var _shared_components_side_navigation_menu_side_navigation_menu_component__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ../../shared/components/side-navigation-menu/side-navigation-menu.component */ 5994);
/* harmony import */ var devextreme_angular_core__WEBPACK_IMPORTED_MODULE_8__ = __webpack_require__(/*! devextreme-angular/core */ 65787);
/* harmony import */ var _shared_components_header_header_component__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ../../shared/components/header/header.component */ 10074);













function SideNavOuterToolbarComponent_app_side_navigation_menu_2_Template(rf, ctx) {
  if (rf & 1) {
    const _r3 = _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵgetCurrentView"]();
    _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementStart"](0, "app-side-navigation-menu", 6);
    _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵlistener"]("selectedItemChanged", function SideNavOuterToolbarComponent_app_side_navigation_menu_2_Template_app_side_navigation_menu_selectedItemChanged_0_listener($event) {
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵrestoreView"](_r3);
      const ctx_r2 = _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵnextContext"]();
      return _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵresetView"](ctx_r2.navigationChanged($event));
    })("openMenu", function SideNavOuterToolbarComponent_app_side_navigation_menu_2_Template_app_side_navigation_menu_openMenu_0_listener() {
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵrestoreView"](_r3);
      const ctx_r4 = _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵnextContext"]();
      return _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵresetView"](ctx_r4.navigationClick());
    });
    _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementEnd"]();
  }
  if (rf & 2) {
    const ctx_r0 = _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵnextContext"]();
    _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵproperty"]("compactMode", !ctx_r0.menuOpened)("selectedItem", ctx_r0.selectedRoute);
  }
}
const _c0 = ["*", [["app-footer"]]];
const _c1 = ["*", "app-footer"];
class SideNavOuterToolbarComponent {
  constructor(screen, router) {
    this.screen = screen;
    this.router = router;
    this.selectedRoute = '';
    this.temporaryMenuOpened = false;
    this.menuMode = 'shrink';
    this.menuRevealMode = 'expand';
    this.minMenuSize = 0;
    this.shaderEnabled = false;
  }
  ngOnInit() {
    this.menuOpened = this.screen.sizes['screen-large'];
    this.router.events.subscribe(val => {
      if (val instanceof _angular_router__WEBPACK_IMPORTED_MODULE_5__.NavigationEnd) {
        this.selectedRoute = val.urlAfterRedirects.split('?')[0];
      }
    });
    this.screen.changed.subscribe(() => this.updateDrawer());
    this.updateDrawer();
  }
  updateDrawer() {
    const isXSmall = this.screen.sizes['screen-x-small'];
    const isLarge = this.screen.sizes['screen-large'];
    this.menuMode = isLarge ? 'shrink' : 'overlap';
    this.menuRevealMode = isXSmall ? 'slide' : 'expand';
    this.minMenuSize = isXSmall ? 0 : 60;
    this.shaderEnabled = !isLarge;
  }
  get hideMenuAfterNavigation() {
    return this.menuMode === 'overlap' || this.temporaryMenuOpened;
  }
  get showMenuAfterClick() {
    return !this.menuOpened;
  }
  navigationChanged(event) {
    const path = event.itemData.path;
    const pointerEvent = event.event;
    if (path && this.menuOpened) {
      if (event.node?.selected) {
        pointerEvent?.preventDefault();
      } else {
        this.router.navigate([path]);
        this.scrollView.instance.scrollTo(0);
      }
      if (this.hideMenuAfterNavigation) {
        this.temporaryMenuOpened = false;
        this.menuOpened = false;
        pointerEvent?.stopPropagation();
      }
    } else {
      pointerEvent?.preventDefault();
    }
  }
  navigationClick() {
    if (this.showMenuAfterClick) {
      this.temporaryMenuOpened = true;
      this.menuOpened = true;
    }
  }
}
SideNavOuterToolbarComponent.ɵfac = function SideNavOuterToolbarComponent_Factory(t) {
  return new (t || SideNavOuterToolbarComponent)(_angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵdirectiveInject"](_shared_services__WEBPACK_IMPORTED_MODULE_1__.ScreenService), _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵdirectiveInject"](_angular_router__WEBPACK_IMPORTED_MODULE_5__.Router));
};
SideNavOuterToolbarComponent.ɵcmp = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵdefineComponent"]({
  type: SideNavOuterToolbarComponent,
  selectors: [["app-side-nav-outer-toolbar"]],
  viewQuery: function SideNavOuterToolbarComponent_Query(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵviewQuery"](devextreme_angular_ui_scroll_view__WEBPACK_IMPORTED_MODULE_6__.DxScrollViewComponent, 7);
    }
    if (rf & 2) {
      let _t;
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵqueryRefresh"](_t = _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵloadQuery"]()) && (ctx.scrollView = _t.first);
    }
  },
  inputs: {
    title: "title"
  },
  ngContentSelectors: _c1,
  decls: 8,
  vars: 9,
  consts: [[1, "layout-header", 3, "menuToggleEnabled", "title", "menuToggle"], ["position", "before", 1, "layout-body", 3, "closeOnOutsideClick", "openedStateMode", "revealMode", "minSize", "shading", "opened", "openedChange"], ["class", "dx-swatch-additional", 3, "compactMode", "selectedItem", "selectedItemChanged", "openMenu", 4, "dxTemplate", "dxTemplateOf"], [1, "with-footer"], [1, "content"], [1, "content-block"], [1, "dx-swatch-additional", 3, "compactMode", "selectedItem", "selectedItemChanged", "openMenu"]],
  template: function SideNavOuterToolbarComponent_Template(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵprojectionDef"](_c0);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementStart"](0, "app-header", 0);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵlistener"]("menuToggle", function SideNavOuterToolbarComponent_Template_app_header_menuToggle_0_listener() {
        return ctx.menuOpened = !ctx.menuOpened;
      });
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementStart"](1, "dx-drawer", 1);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵlistener"]("openedChange", function SideNavOuterToolbarComponent_Template_dx_drawer_openedChange_1_listener($event) {
        return ctx.menuOpened = $event;
      });
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵtemplate"](2, SideNavOuterToolbarComponent_app_side_navigation_menu_2_Template, 1, 2, "app-side-navigation-menu", 2);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementStart"](3, "dx-scroll-view", 3)(4, "div", 4);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵprojection"](5);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementStart"](6, "div", 5);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵprojection"](7, 1);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵelementEnd"]()()();
    }
    if (rf & 2) {
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵproperty"]("menuToggleEnabled", true)("title", ctx.title);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵproperty"]("closeOnOutsideClick", ctx.shaderEnabled)("openedStateMode", ctx.menuMode)("revealMode", ctx.menuRevealMode)("minSize", ctx.minMenuSize)("shading", ctx.shaderEnabled)("opened", ctx.menuOpened);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵproperty"]("dxTemplateOf", "panel");
    }
  },
  dependencies: [_shared_components_side_navigation_menu_side_navigation_menu_component__WEBPACK_IMPORTED_MODULE_2__.SideNavigationMenuComponent, devextreme_angular_ui_drawer__WEBPACK_IMPORTED_MODULE_7__.DxDrawerComponent, devextreme_angular_core__WEBPACK_IMPORTED_MODULE_8__.DxTemplateDirective, _shared_components_header_header_component__WEBPACK_IMPORTED_MODULE_3__.HeaderComponent, devextreme_angular_ui_scroll_view__WEBPACK_IMPORTED_MODULE_6__.DxScrollViewComponent],
  styles: ["[_nghost-%COMP%] {\n  flex-direction: column;\n  display: flex;\n  height: 100%;\n  width: 100%;\n}\n\n.layout-header[_ngcontent-%COMP%] {\n  z-index: 1501;\n}\n/*# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbIndlYnBhY2s6Ly8uL3NyYy9hcHAvbGF5b3V0cy9zaWRlLW5hdi1vdXRlci10b29sYmFyL3NpZGUtbmF2LW91dGVyLXRvb2xiYXIuY29tcG9uZW50LnNjc3MiXSwibmFtZXMiOltdLCJtYXBwaW5ncyI6IkFBQUE7RUFDRSxzQkFBQTtFQUNBLGFBQUE7RUFDQSxZQUFBO0VBQ0EsV0FBQTtBQUNGOztBQUVBO0VBQ0UsYUFBQTtBQUNGIiwic291cmNlc0NvbnRlbnQiOlsiOmhvc3Qge1xuICBmbGV4LWRpcmVjdGlvbjogY29sdW1uO1xuICBkaXNwbGF5OiBmbGV4O1xuICBoZWlnaHQ6IDEwMCU7XG4gIHdpZHRoOiAxMDAlO1xufVxuXG4ubGF5b3V0LWhlYWRlciB7XG4gIHotaW5kZXg6IDE1MDE7XG59XG4iXSwic291cmNlUm9vdCI6IiJ9 */"]
});
class SideNavOuterToolbarModule {}
SideNavOuterToolbarModule.ɵfac = function SideNavOuterToolbarModule_Factory(t) {
  return new (t || SideNavOuterToolbarModule)();
};
SideNavOuterToolbarModule.ɵmod = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵdefineNgModule"]({
  type: SideNavOuterToolbarModule
});
SideNavOuterToolbarModule.ɵinj = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵdefineInjector"]({
  imports: [_shared_components__WEBPACK_IMPORTED_MODULE_0__.SideNavigationMenuModule, devextreme_angular_ui_drawer__WEBPACK_IMPORTED_MODULE_7__.DxDrawerModule, _shared_components__WEBPACK_IMPORTED_MODULE_0__.HeaderModule, devextreme_angular_ui_scroll_view__WEBPACK_IMPORTED_MODULE_6__.DxScrollViewModule, _angular_common__WEBPACK_IMPORTED_MODULE_9__.CommonModule]
});
(function () {
  (typeof ngJitMode === "undefined" || ngJitMode) && _angular_core__WEBPACK_IMPORTED_MODULE_4__["ɵɵsetNgModuleScope"](SideNavOuterToolbarModule, {
    declarations: [SideNavOuterToolbarComponent],
    imports: [_shared_components__WEBPACK_IMPORTED_MODULE_0__.SideNavigationMenuModule, devextreme_angular_ui_drawer__WEBPACK_IMPORTED_MODULE_7__.DxDrawerModule, _shared_components__WEBPACK_IMPORTED_MODULE_0__.HeaderModule, devextreme_angular_ui_scroll_view__WEBPACK_IMPORTED_MODULE_6__.DxScrollViewModule, _angular_common__WEBPACK_IMPORTED_MODULE_9__.CommonModule],
    exports: [SideNavOuterToolbarComponent]
  });
})();

/***/ }),

/***/ 61010:
/*!**************************************************************!*\
  !*** ./src/app/layouts/single-card/single-card.component.ts ***!
  \**************************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   SingleCardComponent: () => (/* binding */ SingleCardComponent),
/* harmony export */   SingleCardModule: () => (/* binding */ SingleCardModule)
/* harmony export */ });
/* harmony import */ var _angular_common__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! @angular/common */ 26575);
/* harmony import */ var devextreme_angular_ui_scroll_view__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! devextreme-angular/ui/scroll-view */ 15018);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! @angular/core */ 61699);




const _c0 = ["*"];
class SingleCardComponent {
  constructor() {}
}
SingleCardComponent.ɵfac = function SingleCardComponent_Factory(t) {
  return new (t || SingleCardComponent)();
};
SingleCardComponent.ɵcmp = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵdefineComponent"]({
  type: SingleCardComponent,
  selectors: [["app-single-card"]],
  inputs: {
    title: "title",
    description: "description"
  },
  ngContentSelectors: _c0,
  decls: 8,
  vars: 2,
  consts: [["height", "100%", "width", "100%", 1, "with-footer", "single-card"], [1, "dx-card", "content"], [1, "header"], [1, "title"], [1, "description"]],
  template: function SingleCardComponent_Template(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵprojectionDef"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementStart"](0, "dx-scroll-view", 0)(1, "div", 1)(2, "div", 2)(3, "div", 3);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵtext"](4);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementStart"](5, "div", 4);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵtext"](6);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementEnd"]()();
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵprojection"](7);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementEnd"]()();
    }
    if (rf & 2) {
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵadvance"](4);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵtextInterpolate"](ctx.title);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵtextInterpolate"](ctx.description);
    }
  },
  dependencies: [devextreme_angular_ui_scroll_view__WEBPACK_IMPORTED_MODULE_1__.DxScrollViewComponent],
  styles: ["[_nghost-%COMP%] {\n  width: 100%;\n  height: 100%;\n  background-color: #325862 !important;\n}\n\n.single-card[_ngcontent-%COMP%] {\n  background-color: #325862 !important;\n}\n.single-card[_ngcontent-%COMP%]   .dx-card[_ngcontent-%COMP%] {\n  width: 330px;\n  margin: auto auto;\n  padding: 40px;\n  flex-grow: 0;\n}\n.screen-x-small[_nghost-%COMP%]   .single-card[_ngcontent-%COMP%]   .dx-card[_ngcontent-%COMP%], .screen-x-small   [_nghost-%COMP%]   .single-card[_ngcontent-%COMP%]   .dx-card[_ngcontent-%COMP%] {\n  width: 100%;\n  height: 100%;\n  border-radius: 0;\n  box-shadow: none;\n  margin: 0;\n  border: 0;\n  flex-grow: 1;\n}\n.single-card[_ngcontent-%COMP%]   .dx-card[_ngcontent-%COMP%]   .header[_ngcontent-%COMP%] {\n  margin-bottom: 30px;\n}\n.single-card[_ngcontent-%COMP%]   .dx-card[_ngcontent-%COMP%]   .header[_ngcontent-%COMP%]   .title[_ngcontent-%COMP%] {\n  color: rgba(255, 255, 255, 0.87);\n  line-height: 28px;\n  font-weight: 500;\n  font-size: 24px;\n  text-align: center;\n}\n.single-card[_ngcontent-%COMP%]   .dx-card[_ngcontent-%COMP%]   .header[_ngcontent-%COMP%]   .description[_ngcontent-%COMP%] {\n  color: rgba(255, 255, 255, 0.609);\n  line-height: 18px;\n}\n/*# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbIndlYnBhY2s6Ly8uL3NyYy9hcHAvbGF5b3V0cy9zaW5nbGUtY2FyZC9zaW5nbGUtY2FyZC5jb21wb25lbnQuc2NzcyIsIndlYnBhY2s6Ly8uL3NyYy90aGVtZXMvZ2VuZXJhdGVkL3ZhcmlhYmxlcy5iYXNlLmRhcmsuc2NzcyJdLCJuYW1lcyI6W10sIm1hcHBpbmdzIjoiQUFFQTtFQUNFLFdBQUE7RUFDQSxZQUFBO0VBQ0Esb0NBQUE7QUFERjs7QUFJQTtFQUNFLG9DQUFBO0FBREY7QUFHRTtFQUNFLFlBQUE7RUFDQSxpQkFBQTtFQUNBLGFBQUE7RUFDQSxZQUFBO0FBREo7QUFHSTtFQUNFLFdBQUE7RUFDQSxZQUFBO0VBQ0EsZ0JBQUE7RUFDQSxnQkFBQTtFQUNBLFNBQUE7RUFDQSxTQUFBO0VBQ0EsWUFBQTtBQUROO0FBSUk7RUFDRSxtQkFBQTtBQUZOO0FBSU07RUFDRSxnQ0M5QlU7RUQrQlYsaUJBQUE7RUFDQSxnQkFBQTtFQUNBLGVBQUE7RUFDQSxrQkFBQTtBQUZSO0FBS007RUFDRSxpQ0FBQTtFQUNBLGlCQUFBO0FBSFIiLCJzb3VyY2VzQ29udGVudCI6WyJAaW1wb3J0IFwiLi4vLi4vLi4vdGhlbWVzL2dlbmVyYXRlZC92YXJpYWJsZXMuYmFzZS5kYXJrLnNjc3NcIjtcblxuOmhvc3Qge1xuICB3aWR0aDogMTAwJTtcbiAgaGVpZ2h0OiAxMDAlO1xuICBiYWNrZ3JvdW5kLWNvbG9yOiAkYmFzZS1hY2NlbnQyIWltcG9ydGFudDtcbn1cblxuLnNpbmdsZS1jYXJkIHtcbiAgYmFja2dyb3VuZC1jb2xvcjogJGJhc2UtYWNjZW50MiFpbXBvcnRhbnQ7XG5cbiAgLmR4LWNhcmQge1xuICAgIHdpZHRoOiAzMzBweDtcbiAgICBtYXJnaW46IGF1dG8gYXV0bztcbiAgICBwYWRkaW5nOiA0MHB4O1xuICAgIGZsZXgtZ3JvdzogMDtcblxuICAgIDpob3N0LWNvbnRleHQoLnNjcmVlbi14LXNtYWxsKSAmIHtcbiAgICAgIHdpZHRoOiAxMDAlO1xuICAgICAgaGVpZ2h0OiAxMDAlO1xuICAgICAgYm9yZGVyLXJhZGl1czogMDtcbiAgICAgIGJveC1zaGFkb3c6IG5vbmU7XG4gICAgICBtYXJnaW46IDA7XG4gICAgICBib3JkZXI6IDA7XG4gICAgICBmbGV4LWdyb3c6IDE7XG4gICAgfVxuXG4gICAgLmhlYWRlciB7XG4gICAgICBtYXJnaW4tYm90dG9tOiAzMHB4O1xuXG4gICAgICAudGl0bGUge1xuICAgICAgICBjb2xvcjogJGJhc2UtdGV4dC1jb2xvcjtcbiAgICAgICAgbGluZS1oZWlnaHQ6IDI4cHg7XG4gICAgICAgIGZvbnQtd2VpZ2h0OiA1MDA7XG4gICAgICAgIGZvbnQtc2l6ZTogMjRweDtcbiAgICAgICAgdGV4dC1hbGlnbjogY2VudGVyO1xuICAgICAgfVxuXG4gICAgICAuZGVzY3JpcHRpb24ge1xuICAgICAgICBjb2xvcjogcmdiYSgkYmFzZS10ZXh0LWNvbG9yLCBhbHBoYSgkYmFzZS10ZXh0LWNvbG9yKSAqIDAuNyk7XG4gICAgICAgIGxpbmUtaGVpZ2h0OiAxOHB4O1xuICAgICAgfVxuICAgIH1cbiAgfVxufVxuIiwiJGJhc2UtYmc6ICMyYTJhMmE7XG4kYmFzZS10ZXh0LWNvbG9yOiByZ2JhKDI1NSwgMjU1LCAyNTUsIDAuODcpO1xuJGJhc2UtYm9yZGVyLWNvbG9yOiAjNGQ0ZDRkO1xuJGJhc2UtYm9yZGVyLXJhZGl1czogNHB4O1xuJGJhc2UtYWNjZW50OiAjNDU3OTg3O1xuJGJhc2UtYWNjZW50MjogIzMyNTg2MjtcbiJdLCJzb3VyY2VSb290IjoiIn0= */"]
});
class SingleCardModule {}
SingleCardModule.ɵfac = function SingleCardModule_Factory(t) {
  return new (t || SingleCardModule)();
};
SingleCardModule.ɵmod = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵdefineNgModule"]({
  type: SingleCardModule
});
SingleCardModule.ɵinj = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵdefineInjector"]({
  imports: [_angular_common__WEBPACK_IMPORTED_MODULE_2__.CommonModule, devextreme_angular_ui_scroll_view__WEBPACK_IMPORTED_MODULE_1__.DxScrollViewModule]
});
(function () {
  (typeof ngJitMode === "undefined" || ngJitMode) && _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵsetNgModuleScope"](SingleCardModule, {
    declarations: [SingleCardComponent],
    imports: [_angular_common__WEBPACK_IMPORTED_MODULE_2__.CommonModule, devextreme_angular_ui_scroll_view__WEBPACK_IMPORTED_MODULE_1__.DxScrollViewModule],
    exports: [SingleCardComponent]
  });
})();

/***/ }),

/***/ 50424:
/*!**********************************************!*\
  !*** ./src/app/pages/home/home.component.ts ***!
  \**********************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   HomeComponent: () => (/* binding */ HomeComponent)
/* harmony export */ });
/* harmony import */ var C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./node_modules/@babel/runtime/helpers/esm/asyncToGenerator.js */ 71670);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var src_app_shared_services_statu_service__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! src/app/shared/services/statu.service */ 761);
/* harmony import */ var devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! devextreme-angular/ui/nested */ 71723);
/* harmony import */ var devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! devextreme-angular/ui/form */ 93739);
/* harmony import */ var _angular_common__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! @angular/common */ 26575);






const _c0 = function () {
  return {
    readOnly: true
  };
};
function HomeComponent_dx_form_3_dxi_item_5_ng_container_1_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementContainerStart"](0);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementStart"](1, "dx-form", 8)(2, "dxi-item", 9)(3, "dxi-item", 3);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelement"](4, "dxi-item", 10)(5, "dxi-item", 11)(6, "dxi-item", 12);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementEnd"]()()();
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementContainerEnd"]();
  }
  if (rf & 2) {
    const item_r3 = ctx.$implicit;
    const i_r4 = ctx.index;
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵadvance"](1);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵpropertyInterpolate1"]("id", "innerForm-", i_r4, "");
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵproperty"]("formData", item_r3);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵadvance"](1);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵpropertyInterpolate1"]("caption", "Container ", i_r4 + 1, "");
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵadvance"](2);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵproperty"]("editorOptions", _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵpureFunction0"](6, _c0));
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵadvance"](1);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵproperty"]("editorOptions", _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵpureFunction0"](7, _c0));
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵadvance"](1);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵproperty"]("editorOptions", _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵpureFunction0"](8, _c0));
  }
}
function HomeComponent_dx_form_3_dxi_item_5_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementStart"](0, "dxi-item");
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵtemplate"](1, HomeComponent_dx_form_3_dxi_item_5_ng_container_1_Template, 7, 9, "ng-container", 7);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementEnd"]();
  }
  if (rf & 2) {
    const ctx_r1 = _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵnextContext"](2);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵadvance"](1);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵproperty"]("ngForOf", ctx_r1.status.runningImages);
  }
}
function HomeComponent_dx_form_3_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementStart"](0, "dx-form", 2)(1, "dxi-item", 3)(2, "dxi-item", 3);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelement"](3, "dxi-item", 4)(4, "dxi-item", 5);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵtemplate"](5, HomeComponent_dx_form_3_dxi_item_5_Template, 2, 1, "dxi-item", 6);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementEnd"]()()();
  }
  if (rf & 2) {
    const ctx_r0 = _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵnextContext"]();
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵproperty"]("formData", ctx_r0.status);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵadvance"](3);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵproperty"]("editorOptions", _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵpureFunction0"](4, _c0));
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵadvance"](1);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵproperty"]("editorOptions", _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵpureFunction0"](5, _c0));
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵadvance"](1);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵproperty"]("ngIf", ctx_r0.status.runningImages);
  }
}
class HomeComponent {
  constructor(statusService) {
    this.statusService = statusService;
    this.status = void 0;
  }
  ngOnInit() {
    var _this = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      yield _this.load();
      _this.interval = setInterval( /*#__PURE__*/(0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
        yield _this.load();
      }), 1000);
    })();
  }
  ngOnDestroy() {
    if (this.interval) {
      clearInterval(this.interval);
      this.interval = undefined;
    }
  }
  load() {
    var _this2 = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      _this2.status = yield _this2.statusService.getStatus();
    })();
  }
}
HomeComponent.ɵfac = function HomeComponent_Factory(t) {
  return new (t || HomeComponent)(_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵdirectiveInject"](src_app_shared_services_statu_service__WEBPACK_IMPORTED_MODULE_1__.StatusService));
};
HomeComponent.ɵcmp = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵdefineComponent"]({
  type: HomeComponent,
  selectors: [["ng-component"]],
  decls: 4,
  vars: 1,
  consts: [[1, "content-block"], ["id", "form", 3, "formData", 4, "ngIf"], ["id", "form", 3, "formData"], ["itemType", "group"], ["dataField", "connected", "editorType", "dxCheckBox", 3, "editorOptions"], ["dataField", "containerStarted", "editorType", "dxCheckBox", 3, "editorOptions"], [4, "ngIf"], [4, "ngFor", "ngForOf"], [3, "id", "formData"], ["itemType", "group", 3, "caption"], ["dataField", "dockerId", 3, "editorOptions"], ["dataField", "image", 3, "editorOptions"], ["dataField", "requestId", 3, "editorOptions"]],
  template: function HomeComponent_Template(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementStart"](0, "h2", 0);
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵtext"](1, "Home");
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementStart"](2, "div", 0);
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵtemplate"](3, HomeComponent_dx_form_3_Template, 6, 6, "dx-form", 1);
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementEnd"]();
    }
    if (rf & 2) {
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵadvance"](3);
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵproperty"]("ngIf", ctx.status);
    }
  },
  dependencies: [devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_3__.DxiItemComponent, devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_4__.DxFormComponent, _angular_common__WEBPACK_IMPORTED_MODULE_5__.NgForOf, _angular_common__WEBPACK_IMPORTED_MODULE_5__.NgIf],
  styles: [".logos-container[_ngcontent-%COMP%] {\n  margin: 20px 0 40px 0;\n  text-align: center;\n}\n.logos-container[_ngcontent-%COMP%]   svg[_ngcontent-%COMP%] {\n  display: inline-block;\n}\n\n.devextreme-logo[_ngcontent-%COMP%] {\n  width: 200px;\n  height: 34px;\n  margin-bottom: 13px;\n}\n\n.angular-logo[_ngcontent-%COMP%] {\n  width: 220px;\n  height: 62px;\n}\n\n.plus[_ngcontent-%COMP%] {\n  margin: 15px 10px;\n  width: 22px;\n  height: 22px;\n}\n\n.screen-x-small[_nghost-%COMP%]   .logos-container[_ngcontent-%COMP%]   svg[_ngcontent-%COMP%], .screen-x-small   [_nghost-%COMP%]   .logos-container[_ngcontent-%COMP%]   svg[_ngcontent-%COMP%] {\n  width: 100%;\n  display: block;\n}\n.screen-x-small[_nghost-%COMP%]   .logos-container[_ngcontent-%COMP%]   svg.plus[_ngcontent-%COMP%], .screen-x-small   [_nghost-%COMP%]   .logos-container[_ngcontent-%COMP%]   svg.plus[_ngcontent-%COMP%] {\n  margin: 0;\n}\n/*# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbIndlYnBhY2s6Ly8uL3NyYy9hcHAvcGFnZXMvaG9tZS9ob21lLmNvbXBvbmVudC5zY3NzIl0sIm5hbWVzIjpbXSwibWFwcGluZ3MiOiJBQUFBO0VBQ0UscUJBQUE7RUFDQSxrQkFBQTtBQUNGO0FBQUU7RUFDRSxxQkFBQTtBQUVKOztBQUVBO0VBQ0UsWUFBQTtFQUNBLFlBQUE7RUFDQSxtQkFBQTtBQUNGOztBQUVBO0VBQ0UsWUFBQTtFQUNBLFlBQUE7QUFDRjs7QUFFQTtFQUNFLGlCQUFBO0VBQ0EsV0FBQTtFQUNBLFlBQUE7QUFDRjs7QUFHRTtFQUNFLFdBQUE7RUFDQSxjQUFBO0FBQUo7QUFDSTtFQUNFLFNBQUE7QUFDTiIsInNvdXJjZXNDb250ZW50IjpbIi5sb2dvcy1jb250YWluZXIge1xuICBtYXJnaW46IDIwcHggMCA0MHB4IDA7XG4gIHRleHQtYWxpZ246IGNlbnRlcjtcbiAgc3ZnIHtcbiAgICBkaXNwbGF5OiBpbmxpbmUtYmxvY2s7XG4gIH1cbn1cblxuLmRldmV4dHJlbWUtbG9nbyB7XG4gIHdpZHRoOiAyMDBweDtcbiAgaGVpZ2h0OiAzNHB4O1xuICBtYXJnaW4tYm90dG9tOiAxM3B4O1xufVxuXG4uYW5ndWxhci1sb2dvIHtcbiAgd2lkdGg6IDIyMHB4O1xuICBoZWlnaHQ6IDYycHg7XG59XG5cbi5wbHVzIHtcbiAgbWFyZ2luOiAxNXB4IDEwcHg7XG4gIHdpZHRoOiAyMnB4O1xuICBoZWlnaHQ6IDIycHg7XG59XG5cbjpob3N0LWNvbnRleHQoLnNjcmVlbi14LXNtYWxsKSAubG9nb3MtY29udGFpbmVyIHtcbiAgc3ZnIHtcbiAgICB3aWR0aDogMTAwJTtcbiAgICBkaXNwbGF5OiBibG9jaztcbiAgICAmLnBsdXMge1xuICAgICAgbWFyZ2luOiAwO1xuICAgIH1cbiAgfVxufVxuIl0sInNvdXJjZVJvb3QiOiIifQ== */"]
});

/***/ }),

/***/ 30862:
/*!****************************************************!*\
  !*** ./src/app/pages/profile/profile.component.ts ***!
  \****************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   ProfileComponent: () => (/* binding */ ProfileComponent)
/* harmony export */ });
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! devextreme-angular/ui/form */ 93739);


class ProfileComponent {
  constructor() {
    this.employee = {
      ID: 7,
      FirstName: 'Sandra',
      LastName: 'Johnson',
      Prefix: 'Mrs.',
      Position: 'Controller',
      Picture: 'images/employees/06.png',
      BirthDate: new Date('1974/11/5'),
      HireDate: new Date('2005/05/11'),
      /* tslint:disable-next-line:max-line-length */
      Notes: 'Sandra is a CPA and has been our controller since 2008. She loves to interact with staff so if you`ve not met her, be certain to say hi.\r\n\r\nSandra has 2 daughters both of whom are accomplished gymnasts.',
      Address: '4600 N Virginia Rd.'
    };
    this.colCountByScreen = {
      xs: 1,
      sm: 2,
      md: 3,
      lg: 4
    };
  }
}
ProfileComponent.ɵfac = function ProfileComponent_Factory(t) {
  return new (t || ProfileComponent)();
};
ProfileComponent.ɵcmp = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵdefineComponent"]({
  type: ProfileComponent,
  selectors: [["ng-component"]],
  decls: 9,
  vars: 4,
  consts: [[1, "content-block"], [1, "content-block", "dx-card", "responsive-paddings"], [1, "form-avatar"], [3, "src"], ["id", "form", "labelLocation", "top", 3, "formData", "colCountByScreen"]],
  template: function ProfileComponent_Template(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementStart"](0, "h2", 0);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵtext"](1, "Profile");
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementStart"](2, "div", 1)(3, "div", 2);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelement"](4, "img", 3);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementStart"](5, "span");
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵtext"](6);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementEnd"]()();
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementStart"](7, "div", 1);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelement"](8, "dx-form", 4);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementEnd"]();
    }
    if (rf & 2) {
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵadvance"](4);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵpropertyInterpolate1"]("src", "https://js.devexpress.com/Demos/WidgetsGallery/JSDemos/", ctx.employee.Picture, "", _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵsanitizeUrl"]);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵtextInterpolate"](ctx.employee.Notes);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵproperty"]("formData", ctx.employee)("colCountByScreen", ctx.colCountByScreen);
    }
  },
  dependencies: [devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_1__.DxFormComponent],
  styles: [".form-avatar[_ngcontent-%COMP%] {\n  float: left;\n  height: 120px;\n  width: 120px;\n  margin-right: 20px;\n  border: 1px solid rgba(0, 0, 0, 0.1);\n  background-size: contain;\n  background-repeat: no-repeat;\n  background-position: center;\n  background-color: #fff;\n  overflow: hidden;\n}\n.form-avatar[_ngcontent-%COMP%]   img[_ngcontent-%COMP%] {\n  height: 120px;\n  display: block;\n  margin: 0 auto;\n}\n/*# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbIndlYnBhY2s6Ly8uL3NyYy9hcHAvcGFnZXMvcHJvZmlsZS9wcm9maWxlLmNvbXBvbmVudC5zY3NzIl0sIm5hbWVzIjpbXSwibWFwcGluZ3MiOiJBQUFBO0VBQ0UsV0FBQTtFQUNBLGFBQUE7RUFDQSxZQUFBO0VBQ0Esa0JBQUE7RUFDQSxvQ0FBQTtFQUNBLHdCQUFBO0VBQ0EsNEJBQUE7RUFDQSwyQkFBQTtFQUNBLHNCQUFBO0VBQ0EsZ0JBQUE7QUFDRjtBQUNFO0VBQ0UsYUFBQTtFQUNBLGNBQUE7RUFDQSxjQUFBO0FBQ0oiLCJzb3VyY2VzQ29udGVudCI6WyIuZm9ybS1hdmF0YXIge1xuICBmbG9hdDogbGVmdDtcbiAgaGVpZ2h0OiAxMjBweDtcbiAgd2lkdGg6IDEyMHB4O1xuICBtYXJnaW4tcmlnaHQ6IDIwcHg7XG4gIGJvcmRlcjogMXB4IHNvbGlkIHJnYmEoMCwgMCwgMCwgMC4xKTtcbiAgYmFja2dyb3VuZC1zaXplOiBjb250YWluO1xuICBiYWNrZ3JvdW5kLXJlcGVhdDogbm8tcmVwZWF0O1xuICBiYWNrZ3JvdW5kLXBvc2l0aW9uOiBjZW50ZXI7XG4gIGJhY2tncm91bmQtY29sb3I6ICNmZmY7XG4gIG92ZXJmbG93OiBoaWRkZW47XG5cbiAgaW1nIHtcbiAgICBoZWlnaHQ6IDEyMHB4O1xuICAgIGRpc3BsYXk6IGJsb2NrO1xuICAgIG1hcmdpbjogMCBhdXRvO1xuICB9XG59XG4iXSwic291cmNlUm9vdCI6IiJ9 */"]
});

/***/ }),

/***/ 199:
/*!****************************************************!*\
  !*** ./src/app/pages/setting/setting.component.ts ***!
  \****************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   SettingComponent: () => (/* binding */ SettingComponent)
/* harmony export */ });
/* harmony import */ var C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./node_modules/@babel/runtime/helpers/esm/asyncToGenerator.js */ 71670);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var src_app_services_config_service__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! src/app/services/config.service */ 342);
/* harmony import */ var _angular_common__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! @angular/common */ 26575);
/* harmony import */ var devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! devextreme-angular/ui/form */ 93739);
/* harmony import */ var devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! devextreme-angular/ui/nested */ 71723);
/* harmony import */ var devextreme_angular_ui_button__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! devextreme-angular/ui/button */ 64766);







const _c0 = function () {
  return {
    mode: "password"
  };
};
const _c1 = function () {
  return {
    min: 1005,
    max: 65535,
    showSpinButtons: true
  };
};
function SettingComponent_dx_form_3_Template(rf, ctx) {
  if (rf & 1) {
    const _r2 = _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵgetCurrentView"]();
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementStart"](0, "dx-form", 2)(1, "dxi-item", 3)(2, "dxi-item", 3);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelement"](3, "dxi-item", 4)(4, "dxi-item", 5)(5, "dxi-item", 6)(6, "dxi-item", 7)(7, "dxi-item", 8);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementEnd"]()();
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementStart"](8, "dxi-item")(9, "dx-button", 9);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵlistener"]("onClick", function SettingComponent_dx_form_3_Template_dx_button_onClick_9_listener() {
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵrestoreView"](_r2);
      const ctx_r1 = _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵnextContext"]();
      return _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵresetView"](ctx_r1.save());
    });
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵtext"](10, "Save");
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementEnd"]()()();
  }
  if (rf & 2) {
    const ctx_r0 = _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵnextContext"]();
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵproperty"]("formData", ctx_r0.config);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵadvance"](4);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵproperty"]("editorOptions", _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵpureFunction0"](4, _c0));
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵadvance"](1);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵproperty"]("editorOptions", _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵpureFunction0"](5, _c0));
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵadvance"](2);
    _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵproperty"]("editorOptions", _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵpureFunction0"](6, _c1));
  }
}
class SettingComponent {
  constructor(configService) {
    this.configService = configService;
  }
  ngOnInit() {
    var _this = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      _this.config = yield _this.configService.getConfig();
    })();
  }
  save() {
    var _this2 = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      yield _this2.configService.saveConfig(_this2.config);
    })();
  }
}
SettingComponent.ɵfac = function SettingComponent_Factory(t) {
  return new (t || SettingComponent)(_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵdirectiveInject"](src_app_services_config_service__WEBPACK_IMPORTED_MODULE_1__.ConfigService));
};
SettingComponent.ɵcmp = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵdefineComponent"]({
  type: SettingComponent,
  selectors: [["app-setting"]],
  decls: 4,
  vars: 1,
  consts: [[1, "content-block"], ["id", "form", 3, "formData", 4, "ngIf"], ["id", "form", 3, "formData"], ["itemType", "group"], ["dataField", "serverUrl"], ["dataField", "clientId", 3, "editorOptions"], ["dataField", "clientKey", 3, "editorOptions"], ["dataField", "dockerTag"], ["dataField", "port", "editorType", "dxNumberBox", 3, "editorOptions"], ["stylingMode", "outlined", "width", "100%", "type", "success", 3, "onClick"]],
  template: function SettingComponent_Template(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementStart"](0, "h2", 0);
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵtext"](1, "Settings");
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementStart"](2, "div", 0);
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵtemplate"](3, SettingComponent_dx_form_3_Template, 11, 7, "dx-form", 1);
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementEnd"]();
    }
    if (rf & 2) {
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵadvance"](3);
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵproperty"]("ngIf", ctx.config);
    }
  },
  dependencies: [_angular_common__WEBPACK_IMPORTED_MODULE_3__.NgIf, devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_4__.DxFormComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_5__.DxiItemComponent, devextreme_angular_ui_button__WEBPACK_IMPORTED_MODULE_6__.DxButtonComponent],
  styles: ["/*# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbXSwibmFtZXMiOltdLCJtYXBwaW5ncyI6IiIsInNvdXJjZVJvb3QiOiIifQ== */"]
});

/***/ }),

/***/ 59398:
/*!************************************************!*\
  !*** ./src/app/pages/tasks/tasks.component.ts ***!
  \************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   TasksComponent: () => (/* binding */ TasksComponent)
/* harmony export */ });
/* harmony import */ var devextreme_data_odata_store__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! devextreme/data/odata/store */ 89177);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var devextreme_angular__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! devextreme-angular */ 3965);
/* harmony import */ var devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! devextreme-angular/ui/nested */ 71723);




class TasksComponent {
  constructor() {
    this.dataSource = {
      store: {
        type: 'odata',
        key: 'Task_ID',
        url: 'https://js.devexpress.com/Demos/DevAV/odata/Tasks'
      },
      expand: 'ResponsibleEmployee',
      select: ['Task_ID', 'Task_Subject', 'Task_Start_Date', 'Task_Due_Date', 'Task_Status', 'Task_Priority', 'Task_Completion', 'ResponsibleEmployee/Employee_Full_Name']
    };
    this.priority = [{
      name: 'High',
      value: 4
    }, {
      name: 'Urgent',
      value: 3
    }, {
      name: 'Normal',
      value: 2
    }, {
      name: 'Low',
      value: 1
    }];
  }
}
TasksComponent.ɵfac = function TasksComponent_Factory(t) {
  return new (t || TasksComponent)();
};
TasksComponent.ɵcmp = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵdefineComponent"]({
  type: TasksComponent,
  selectors: [["ng-component"]],
  decls: 16,
  vars: 23,
  consts: [[1, "content-block"], [1, "dx-card", "wide-card", 3, "dataSource", "showBorders", "focusedRowEnabled", "focusedRowIndex", "columnAutoWidth", "columnHidingEnabled"], [3, "pageSize"], [3, "showPageSizeSelector", "showInfo"], [3, "visible"], ["dataField", "Task_ID", 3, "width", "hidingPriority"], ["dataField", "Task_Subject", "caption", "Subject", 3, "width", "hidingPriority"], ["dataField", "Task_Status", "caption", "Status", 3, "hidingPriority"], ["dataField", "Task_Priority", "caption", "Priority", 3, "hidingPriority"], ["valueExpr", "value", "displayExpr", "name", 3, "dataSource"], ["dataField", "ResponsibleEmployee.Employee_Full_Name", "caption", "Assigned To", 3, "allowSorting", "hidingPriority"], ["dataField", "Task_Start_Date", "caption", "Start Date", "dataType", "date", 3, "hidingPriority"], ["dataField", "Task_Due_Date", "caption", "Due Date", "dataType", "date", 3, "hidingPriority"], ["dataField", "Task_Priority", "caption", "Priority", "name", "Priority", 3, "hidingPriority"], ["dataField", "Task_Completion", "caption", "Completion", 3, "hidingPriority"]],
  template: function TasksComponent_Template(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵelementStart"](0, "h2", 0);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵtext"](1, "Tasks");
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵelementStart"](2, "dx-data-grid", 1);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵelement"](3, "dxo-paging", 2)(4, "dxo-pager", 3)(5, "dxo-filter-row", 4)(6, "dxi-column", 5)(7, "dxi-column", 6)(8, "dxi-column", 7);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵelementStart"](9, "dxi-column", 8);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵelement"](10, "dxo-lookup", 9);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵelement"](11, "dxi-column", 10)(12, "dxi-column", 11)(13, "dxi-column", 12)(14, "dxi-column", 13)(15, "dxi-column", 14);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵelementEnd"]();
    }
    if (rf & 2) {
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵproperty"]("dataSource", ctx.dataSource)("showBorders", false)("focusedRowEnabled", true)("focusedRowIndex", 0)("columnAutoWidth", true)("columnHidingEnabled", true);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵproperty"]("pageSize", 10);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵproperty"]("showPageSizeSelector", true)("showInfo", true);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵproperty"]("visible", true);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵproperty"]("width", 90)("hidingPriority", 2);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵproperty"]("width", 190)("hidingPriority", 8);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵproperty"]("hidingPriority", 6);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵproperty"]("hidingPriority", 5);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵproperty"]("dataSource", ctx.priority);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵproperty"]("allowSorting", false)("hidingPriority", 7);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵproperty"]("hidingPriority", 3);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵproperty"]("hidingPriority", 4);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵproperty"]("hidingPriority", 1);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_1__["ɵɵproperty"]("hidingPriority", 0);
    }
  },
  dependencies: [devextreme_angular__WEBPACK_IMPORTED_MODULE_2__.DxDataGridComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_3__.DxiColumnComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_3__.DxoLookupComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_3__.DxoFilterRowComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_3__.DxoPagerComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_3__.DxoPagingComponent],
  encapsulation: 2
});

/***/ }),

/***/ 342:
/*!********************************************!*\
  !*** ./src/app/services/config.service.ts ***!
  \********************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   ConfigService: () => (/* binding */ ConfigService)
/* harmony export */ });
/* harmony import */ var C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./node_modules/@babel/runtime/helpers/esm/asyncToGenerator.js */ 71670);
/* harmony import */ var _shared_services_base_service__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../shared/services/base.service */ 127);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var _angular_common_http__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! @angular/common/http */ 54860);




class ConfigService extends _shared_services_base_service__WEBPACK_IMPORTED_MODULE_1__.BaseService {
  constructor(http) {
    super(http);
  }
  getConfig() {
    var _this = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      const result = yield _this.http.get("/webapi/config", {
        headers: _this.headers()
      }).toPromise();
      return result;
    })();
  }
  saveConfig(config) {
    var _this2 = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      yield _this2.http.post("/webapi/config", config, {
        headers: _this2.headers()
      }).toPromise();
      return _this2.getConfig();
    })();
  }
}
ConfigService.ɵfac = function ConfigService_Factory(t) {
  return new (t || ConfigService)(_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵinject"](_angular_common_http__WEBPACK_IMPORTED_MODULE_3__.HttpClient));
};
ConfigService.ɵprov = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵdefineInjectable"]({
  token: ConfigService,
  factory: ConfigService.ɵfac
});

/***/ }),

/***/ 42471:
/*!*******************************************!*\
  !*** ./src/app/services/theme.service.ts ***!
  \*******************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   ThemeService: () => (/* binding */ ThemeService)
/* harmony export */ });
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var devextreme_viz_palette__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! devextreme/viz/palette */ 18884);
/* harmony import */ var devextreme_viz_themes__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! devextreme/viz/themes */ 45122);




class ThemeService {
  criteria(category) {
    let categoryItems = {
      'sector': ['Banking', 'Energy', 'Health', 'Insurance', 'Manufacturing', 'Telecom'],
      'product': ['Eco Max', 'Eco Supreme', 'EnviroCare', 'EnviroCare Max', 'SolarMax', 'SolarOne'],
      'channel': ['Consultants', 'Direct', 'Resellers', 'Retail', 'VARs']
    };
    return categoryItems[category.toLowerCase()];
  }
  getCriteriaIndex(category, criteria) {
    return this.criteria(category).indexOf(criteria);
  }
  getColor(category, criteria) {
    const palette = (0,devextreme_viz_palette__WEBPACK_IMPORTED_MODULE_0__.getPalette)(this.getThemeItem('defaultPalette'))['simpleSet'];
    return palette[this.getCriteriaIndex(category, criteria)];
  }
  getAccentColor() {
    return (0,devextreme_viz_palette__WEBPACK_IMPORTED_MODULE_0__.getPalette)(this.getThemeItem('defaultPalette')).accentColor;
  }
  getBackgroundColor() {
    const themeName = (0,devextreme_viz_themes__WEBPACK_IMPORTED_MODULE_1__.currentTheme)();
    const theme = (0,devextreme_viz_themes__WEBPACK_IMPORTED_MODULE_1__.getTheme)(themeName);
    return theme.backgroundColor;
  }
  getGridColor() {
    const themeName = (0,devextreme_viz_themes__WEBPACK_IMPORTED_MODULE_1__.currentTheme)();
    const theme = (0,devextreme_viz_themes__WEBPACK_IMPORTED_MODULE_1__.getTheme)(themeName);
    return theme.gridColor;
  }
  getLegendItems(category) {
    const that = this;
    return this.criteria(category).map(function (criteria) {
      return {
        color: that.getColor(category, criteria),
        name: criteria
      };
    }, this);
  }
  getThemeItem(...keys) {
    const theme = (0,devextreme_viz_themes__WEBPACK_IMPORTED_MODULE_1__.getTheme)((0,devextreme_viz_themes__WEBPACK_IMPORTED_MODULE_1__.currentTheme)());
    let item = theme;
    for (let key in keys) item = item[keys[key]];
    return item;
  }
  getCurrentTheme() {
    return this.currentTheme;
  }
  applyTheme(theme) {
    let themeMarker = "generic.",
      storageKey = "automaticaTheme";
    theme = theme || window.localStorage[storageKey] || "dark";
    for (let index in document.styleSheets) {
      let styleSheet = document.styleSheets[index],
        href = styleSheet.href;
      if (href) {
        let themeMarkerPosition = href.indexOf(themeMarker);
        if (themeMarkerPosition >= 0) {
          let startPosition = themeMarkerPosition + themeMarker.length,
            endPosition = href.indexOf(".", startPosition),
            fileNamePart = href.substring(startPosition, endPosition);
          styleSheet.disabled = fileNamePart != theme;
        }
      }
    }
    window.localStorage[storageKey] = theme;
    (0,devextreme_viz_themes__WEBPACK_IMPORTED_MODULE_1__.currentTheme)("generic." + theme + ".compact");
    (0,devextreme_viz_themes__WEBPACK_IMPORTED_MODULE_1__.refreshTheme)();
    this.currentTheme = theme;
    this.themeChanged.emit(theme);
  }
  constructor() {
    this.themeChanged = new _angular_core__WEBPACK_IMPORTED_MODULE_2__.EventEmitter();
    this.currentTheme = "light";
  }
}
ThemeService.ɵfac = function ThemeService_Factory(t) {
  return new (t || ThemeService)();
};
ThemeService.ɵprov = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵdefineInjectable"]({
  token: ThemeService,
  factory: ThemeService.ɵfac
});

/***/ }),

/***/ 68081:
/*!******************************************************************************************!*\
  !*** ./src/app/shared/components/change-password-form/change-password-form.component.ts ***!
  \******************************************************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   ChangePasswordFormComponent: () => (/* binding */ ChangePasswordFormComponent),
/* harmony export */   ChangePasswordFormModule: () => (/* binding */ ChangePasswordFormModule)
/* harmony export */ });
/* harmony import */ var C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./node_modules/@babel/runtime/helpers/esm/asyncToGenerator.js */ 71670);
/* harmony import */ var _angular_common__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! @angular/common */ 26575);
/* harmony import */ var _angular_router__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! @angular/router */ 27947);
/* harmony import */ var devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! devextreme-angular/ui/form */ 93739);
/* harmony import */ var devextreme_angular_ui_load_indicator__WEBPACK_IMPORTED_MODULE_9__ = __webpack_require__(/*! devextreme-angular/ui/load-indicator */ 2040);
/* harmony import */ var devextreme_ui_notify__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! devextreme/ui/notify */ 72599);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var _services__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ../../services */ 57175);
/* harmony import */ var devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(/*! devextreme-angular/ui/nested */ 71723);
/* harmony import */ var devextreme_angular_core__WEBPACK_IMPORTED_MODULE_8__ = __webpack_require__(/*! devextreme-angular/core */ 65787);














function ChangePasswordFormComponent_ng_container_11_ng_container_3_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementContainerStart"](0);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](1, "dx-load-indicator", 13);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementContainerEnd"]();
  }
  if (rf & 2) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("visible", true);
  }
}
function ChangePasswordFormComponent_ng_container_11_ng_template_4_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtext"](0, "Continue");
  }
}
function ChangePasswordFormComponent_ng_container_11_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementContainerStart"](0);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](1, "div")(2, "span", 10);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplate"](3, ChangePasswordFormComponent_ng_container_11_ng_container_3_Template, 2, 1, "ng-container", 11);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplate"](4, ChangePasswordFormComponent_ng_container_11_ng_template_4_Template, 1, 0, "ng-template", null, 12, _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplateRefExtractor"]);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]()();
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementContainerEnd"]();
  }
  if (rf & 2) {
    const _r3 = _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵreference"](5);
    const ctx_r0 = _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵnextContext"]();
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](3);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("ngIf", ctx_r0.loading)("ngIfElse", _r3);
  }
}
const _c0 = function () {
  return {
    stylingMode: "filled",
    placeholder: "Password",
    mode: "password"
  };
};
const _c1 = function () {
  return {
    stylingMode: "filled",
    placeholder: "Confirm Password",
    mode: "password"
  };
};
class ChangePasswordFormComponent {
  constructor(authService, router, route) {
    this.authService = authService;
    this.router = router;
    this.route = route;
    this.loading = false;
    this.formData = {};
    this.recoveryCode = '';
    this.confirmPassword = e => {
      return e.value === this.formData.password;
    };
  }
  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      this.recoveryCode = params.get('recoveryCode') || '';
    });
  }
  onSubmit(e) {
    var _this = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      e.preventDefault();
      const {
        password
      } = _this.formData;
      _this.loading = true;
      const result = yield _this.authService.changePassword(password, _this.recoveryCode);
      _this.loading = false;
      if (result.isOk) {
        _this.router.navigate(['/login-form']);
      } else {
        (0,devextreme_ui_notify__WEBPACK_IMPORTED_MODULE_1__["default"])(result.message, 'error', 2000);
      }
    })();
  }
}
ChangePasswordFormComponent.ɵfac = function ChangePasswordFormComponent_Factory(t) {
  return new (t || ChangePasswordFormComponent)(_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdirectiveInject"](_services__WEBPACK_IMPORTED_MODULE_2__.AuthService), _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdirectiveInject"](_angular_router__WEBPACK_IMPORTED_MODULE_4__.Router), _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdirectiveInject"](_angular_router__WEBPACK_IMPORTED_MODULE_4__.ActivatedRoute));
};
ChangePasswordFormComponent.ɵcmp = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdefineComponent"]({
  type: ChangePasswordFormComponent,
  selectors: [["app-change-passsword-form"]],
  decls: 12,
  vars: 12,
  consts: [[3, "submit"], [3, "formData", "disabled"], ["dataField", "password", "editorType", "dxTextBox", 3, "editorOptions"], ["type", "required", "message", "Password is required"], [3, "visible"], ["dataField", "confirmedPassword", "editorType", "dxTextBox", 3, "editorOptions"], ["type", "custom", "message", "Passwords do not match", 3, "validationCallback"], ["itemType", "button"], ["width", "100%", "type", "default", 3, "useSubmitBehavior", "template"], [4, "dxTemplate", "dxTemplateOf"], [1, "dx-button-text"], [4, "ngIf", "ngIfElse"], ["notLoading", ""], ["width", "24px", "height", "24px", 3, "visible"]],
  template: function ChangePasswordFormComponent_Template(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](0, "form", 0);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵlistener"]("submit", function ChangePasswordFormComponent_Template_form_submit_0_listener($event) {
        return ctx.onSubmit($event);
      });
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](1, "dx-form", 1)(2, "dxi-item", 2);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](3, "dxi-validation-rule", 3)(4, "dxo-label", 4);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](5, "dxi-item", 5);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](6, "dxi-validation-rule", 3)(7, "dxi-validation-rule", 6)(8, "dxo-label", 4);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](9, "dxi-item", 7);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](10, "dxo-button-options", 8);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplate"](11, ChangePasswordFormComponent_ng_container_11_Template, 6, 2, "ng-container", 9);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]()();
    }
    if (rf & 2) {
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("formData", ctx.formData)("disabled", ctx.loading);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("editorOptions", _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵpureFunction0"](10, _c0));
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("visible", false);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("editorOptions", _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵpureFunction0"](11, _c1));
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("validationCallback", ctx.confirmPassword);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("visible", false);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("useSubmitBehavior", true)("template", "changePasswordTemplate");
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("dxTemplateOf", "changePasswordTemplate");
    }
  },
  dependencies: [_angular_common__WEBPACK_IMPORTED_MODULE_5__.NgIf, devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_6__.DxFormComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__.DxiItemComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__.DxoLabelComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__.DxiValidationRuleComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__.DxoButtonOptionsComponent, devextreme_angular_core__WEBPACK_IMPORTED_MODULE_8__.DxTemplateDirective, devextreme_angular_ui_load_indicator__WEBPACK_IMPORTED_MODULE_9__.DxLoadIndicatorComponent],
  encapsulation: 2
});
class ChangePasswordFormModule {}
ChangePasswordFormModule.ɵfac = function ChangePasswordFormModule_Factory(t) {
  return new (t || ChangePasswordFormModule)();
};
ChangePasswordFormModule.ɵmod = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdefineNgModule"]({
  type: ChangePasswordFormModule
});
ChangePasswordFormModule.ɵinj = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdefineInjector"]({
  imports: [_angular_common__WEBPACK_IMPORTED_MODULE_5__.CommonModule, _angular_router__WEBPACK_IMPORTED_MODULE_4__.RouterModule, devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_6__.DxFormModule, devextreme_angular_ui_load_indicator__WEBPACK_IMPORTED_MODULE_9__.DxLoadIndicatorModule]
});
(function () {
  (typeof ngJitMode === "undefined" || ngJitMode) && _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵsetNgModuleScope"](ChangePasswordFormModule, {
    declarations: [ChangePasswordFormComponent],
    imports: [_angular_common__WEBPACK_IMPORTED_MODULE_5__.CommonModule, _angular_router__WEBPACK_IMPORTED_MODULE_4__.RouterModule, devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_6__.DxFormModule, devextreme_angular_ui_load_indicator__WEBPACK_IMPORTED_MODULE_9__.DxLoadIndicatorModule],
    exports: [ChangePasswordFormComponent]
  });
})();

/***/ }),

/***/ 18270:
/*!****************************************************************************************!*\
  !*** ./src/app/shared/components/create-account-form/create-account-form.component.ts ***!
  \****************************************************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   CreateAccountFormComponent: () => (/* binding */ CreateAccountFormComponent),
/* harmony export */   CreateAccountFormModule: () => (/* binding */ CreateAccountFormModule)
/* harmony export */ });
/* harmony import */ var C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./node_modules/@babel/runtime/helpers/esm/asyncToGenerator.js */ 71670);
/* harmony import */ var _angular_common__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! @angular/common */ 26575);
/* harmony import */ var _angular_router__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! @angular/router */ 27947);
/* harmony import */ var devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! devextreme-angular/ui/form */ 93739);
/* harmony import */ var devextreme_angular_ui_load_indicator__WEBPACK_IMPORTED_MODULE_9__ = __webpack_require__(/*! devextreme-angular/ui/load-indicator */ 2040);
/* harmony import */ var devextreme_ui_notify__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! devextreme/ui/notify */ 72599);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var _services__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ../../services */ 57175);
/* harmony import */ var devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(/*! devextreme-angular/ui/nested */ 71723);
/* harmony import */ var devextreme_angular_core__WEBPACK_IMPORTED_MODULE_8__ = __webpack_require__(/*! devextreme-angular/core */ 65787);














function CreateAccountFormComponent_ng_container_16_ng_container_3_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementContainerStart"](0);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](1, "dx-load-indicator", 16);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementContainerEnd"]();
  }
  if (rf & 2) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("visible", true);
  }
}
function CreateAccountFormComponent_ng_container_16_ng_template_4_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtext"](0, "Setup");
  }
}
function CreateAccountFormComponent_ng_container_16_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementContainerStart"](0);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](1, "div")(2, "span", 13);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplate"](3, CreateAccountFormComponent_ng_container_16_ng_container_3_Template, 2, 1, "ng-container", 14);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplate"](4, CreateAccountFormComponent_ng_container_16_ng_template_4_Template, 1, 0, "ng-template", null, 15, _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplateRefExtractor"]);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]()();
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementContainerEnd"]();
  }
  if (rf & 2) {
    const _r3 = _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵreference"](5);
    const ctx_r0 = _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵnextContext"]();
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](3);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("ngIf", ctx_r0.loading)("ngIfElse", _r3);
  }
}
const _c0 = function () {
  return {
    stylingMode: "filled",
    placeholder: "Username"
  };
};
const _c1 = function () {
  return {
    stylingMode: "filled",
    placeholder: "Password",
    mode: "password"
  };
};
const _c2 = function () {
  return {
    stylingMode: "filled",
    placeholder: "Confirm Password",
    mode: "password"
  };
};
class CreateAccountFormComponent {
  constructor(authService, router) {
    this.authService = authService;
    this.router = router;
    this.loading = false;
    this.formData = {};
    this.confirmPassword = e => {
      return e.value === this.formData.password;
    };
  }
  onSubmit(e) {
    var _this = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      e.preventDefault();
      const {
        username,
        password
      } = _this.formData;
      _this.loading = true;
      const result = yield _this.authService.setupAccount(username, password);
      _this.loading = false;
      if (result.isOk) {
        _this.router.navigate(['/login-form']);
      } else {
        (0,devextreme_ui_notify__WEBPACK_IMPORTED_MODULE_1__["default"])(result.message, 'error', 2000);
      }
    })();
  }
}
CreateAccountFormComponent.ɵfac = function CreateAccountFormComponent_Factory(t) {
  return new (t || CreateAccountFormComponent)(_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdirectiveInject"](_services__WEBPACK_IMPORTED_MODULE_2__.AuthService), _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdirectiveInject"](_angular_router__WEBPACK_IMPORTED_MODULE_4__.Router));
};
CreateAccountFormComponent.ɵcmp = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdefineComponent"]({
  type: CreateAccountFormComponent,
  selectors: [["app-create-account-form"]],
  decls: 17,
  vars: 15,
  consts: [[1, "create-account-form", 3, "submit"], [3, "formData", "disabled"], ["src", "/assets/images/Logo1.png", 1, "center_img"], ["dataField", "username", "editorType", "dxTextBox", 3, "editorOptions"], ["type", "required", "message", "Email is required"], [3, "visible"], ["dataField", "password", "editorType", "dxTextBox", 3, "editorOptions"], ["type", "required", "message", "Password is required"], ["dataField", "confirmedPassword", "editorType", "dxTextBox", 3, "editorOptions"], ["type", "custom", "message", "Passwords do not match", 3, "validationCallback"], ["itemType", "button"], ["width", "100%", "type", "default", 3, "useSubmitBehavior", "template"], [4, "dxTemplate", "dxTemplateOf"], [1, "dx-button-text"], [4, "ngIf", "ngIfElse"], ["notLoading", ""], ["width", "24px", "height", "24px", 3, "visible"]],
  template: function CreateAccountFormComponent_Template(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](0, "form", 0);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵlistener"]("submit", function CreateAccountFormComponent_Template_form_submit_0_listener($event) {
        return ctx.onSubmit($event);
      });
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](1, "dx-form", 1)(2, "dxi-item");
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](3, "img", 2);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](4, "dxi-item", 3);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](5, "dxi-validation-rule", 4)(6, "dxo-label", 5);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](7, "dxi-item", 6);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](8, "dxi-validation-rule", 7)(9, "dxo-label", 5);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](10, "dxi-item", 8);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](11, "dxi-validation-rule", 7)(12, "dxi-validation-rule", 9)(13, "dxo-label", 5);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](14, "dxi-item", 10);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](15, "dxo-button-options", 11);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplate"](16, CreateAccountFormComponent_ng_container_16_Template, 6, 2, "ng-container", 12);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]()();
    }
    if (rf & 2) {
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("formData", ctx.formData)("disabled", ctx.loading);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](3);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("editorOptions", _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵpureFunction0"](12, _c0));
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("visible", false);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("editorOptions", _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵpureFunction0"](13, _c1));
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("visible", false);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("editorOptions", _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵpureFunction0"](14, _c2));
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("validationCallback", ctx.confirmPassword);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("visible", false);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("useSubmitBehavior", true)("template", "createAccountTemplate");
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("dxTemplateOf", "createAccountTemplate");
    }
  },
  dependencies: [_angular_common__WEBPACK_IMPORTED_MODULE_5__.NgIf, devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_6__.DxFormComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__.DxiItemComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__.DxoLabelComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__.DxiValidationRuleComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__.DxoButtonOptionsComponent, devextreme_angular_core__WEBPACK_IMPORTED_MODULE_8__.DxTemplateDirective, devextreme_angular_ui_load_indicator__WEBPACK_IMPORTED_MODULE_9__.DxLoadIndicatorComponent],
  styles: [".center_img[_ngcontent-%COMP%] {\n  display: block;\n  margin-left: auto;\n  margin-right: auto;\n  width: 100%;\n}\n\n.create-account-form[_ngcontent-%COMP%]   .policy-info[_ngcontent-%COMP%] {\n  margin: 10px 0;\n  color: rgba(0, 0, 0, 0.6094117647);\n  font-size: 14px;\n  font-style: normal;\n}\n.create-account-form[_ngcontent-%COMP%]   .policy-info[_ngcontent-%COMP%]   a[_ngcontent-%COMP%] {\n  color: rgba(0, 0, 0, 0.6094117647);\n}\n.create-account-form[_ngcontent-%COMP%]   .login-link[_ngcontent-%COMP%] {\n  color: #457987;\n  font-size: 16px;\n  text-align: center;\n}\n/*# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbIndlYnBhY2s6Ly8uL3NyYy9hcHAvc2hhcmVkL2NvbXBvbmVudHMvY3JlYXRlLWFjY291bnQtZm9ybS9jcmVhdGUtYWNjb3VudC1mb3JtLmNvbXBvbmVudC5zY3NzIiwid2VicGFjazovLy4vc3JjL3RoZW1lcy9nZW5lcmF0ZWQvdmFyaWFibGVzLmJhc2Uuc2NzcyJdLCJuYW1lcyI6W10sIm1hcHBpbmdzIjoiQUFFQTtFQUNFLGNBQUE7RUFDQSxpQkFBQTtFQUNBLGtCQUFBO0VBQ0EsV0FBQTtBQURGOztBQU1FO0VBQ0UsY0FBQTtFQUNBLGtDQUFBO0VBQ0EsZUFBQTtFQUNBLGtCQUFBO0FBSEo7QUFLSTtFQUNFLGtDQUFBO0FBSE47QUFPRTtFQUNFLGNDdEJVO0VEdUJWLGVBQUE7RUFDQSxrQkFBQTtBQUxKIiwic291cmNlc0NvbnRlbnQiOlsiQGltcG9ydCBcIi4uLy4uLy4uLy4uL3RoZW1lcy9nZW5lcmF0ZWQvdmFyaWFibGVzLmJhc2Uuc2Nzc1wiO1xuXG4uY2VudGVyX2ltZyB7XG4gIGRpc3BsYXk6IGJsb2NrO1xuICBtYXJnaW4tbGVmdDogYXV0bztcbiAgbWFyZ2luLXJpZ2h0OiBhdXRvO1xuICB3aWR0aDogMTAwJTtcbn1cblxuXG4uY3JlYXRlLWFjY291bnQtZm9ybSB7XG4gIC5wb2xpY3ktaW5mbyB7XG4gICAgbWFyZ2luOiAxMHB4IDA7XG4gICAgY29sb3I6IHJnYmEoJGJhc2UtdGV4dC1jb2xvciwgYWxwaGEoJGJhc2UtdGV4dC1jb2xvcikgKiAwLjcpO1xuICAgIGZvbnQtc2l6ZTogMTRweDtcbiAgICBmb250LXN0eWxlOiBub3JtYWw7XG5cbiAgICBhIHtcbiAgICAgIGNvbG9yOiByZ2JhKCRiYXNlLXRleHQtY29sb3IsIGFscGhhKCRiYXNlLXRleHQtY29sb3IpICogMC43KTtcbiAgICB9XG4gIH1cblxuICAubG9naW4tbGluayB7XG4gICAgY29sb3I6ICRiYXNlLWFjY2VudDtcbiAgICBmb250LXNpemU6IDE2cHg7XG4gICAgdGV4dC1hbGlnbjogY2VudGVyO1xuICB9XG59XG4iLCIkYmFzZS1mb250LWZhbWlseTogXCJIZWx2ZXRpY2EgTmV1ZVwiLCBcIlNlZ29lIFVJXCIsIGhlbHZldGljYSwgdmVyZGFuYSwgc2Fucy1zZXJpZjtcbiRiYXNlLWFjY2VudDogIzQ1Nzk4NztcbiRiYXNlLWxhYmVsLWNvbG9yOiAjMDAwMDAwZGU7XG4kYmFzZS1saW5rLWNvbG9yOiAjNDU3OTg3O1xuJGJhc2UtaWNvbi1jb2xvcjogIzAwMDAwMGRlO1xuJGJhc2UtdGV4dC1jb2xvcjogIzAwMDAwMGRlO1xuJGJhc2UtYmc6ICNmZmZmZmY7XG4kYmFzZS1ib3JkZXItY29sb3I6ICNlMGUwZTA7XG4kYmFzZS1ib3JkZXItcmFkaXVzOiA0cHg7XG4kYmFzZS1zdWNjZXNzOiAjNWNiODVjO1xuJGJhc2Utd2FybmluZzogI2YwYWQ0ZTtcbiRiYXNlLWRhbmdlcjogI2Q5NTM0ZjtcbiRiYXNlLWhvdmVyLWNvbG9yOiAjMDAwMDAwZGU7XG4kYmFzZS1ob3Zlci1iZzogIzAwMDAwMDBhO1xuJGJhc2UtZm9jdXMtY29sb3I6ICNmZmZmZmY7XG4kYmFzZS1mb2N1cy1iZzogIzQ1Nzk4NztcbiRiYXNlLWludmFsaWQtY29sb3I6ICNkOTUzNGY7XG4kYmFzZS1pbnZhbGlkLWZhZGVkLWJvcmRlci1jb2xvcjogI2Q5NTM0ZjY2O1xuJGJhc2UtYm9yZGVyLXJhZGl1cy1zbWFsbDogM3B4O1xuJGJhc2UtYm9yZGVyLXJhZGl1cy1sYXJnZTogNnB4O1xuJGJ1dHRvbi1ub3JtYWwtY29sb3I6ICMwMDAwMDBkZTtcbiRidXR0b24tbm9ybWFsLWJnOiAjZmZmZmZmO1xuJGJ1dHRvbi1ub3JtYWwtYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJGJ1dHRvbi1kZWZhdWx0LWNvbG9yOiAjZmZmZmZmO1xuJGJ1dHRvbi1kZWZhdWx0LWJnOiAjNDU3OTg3O1xuJGJ1dHRvbi1kZWZhdWx0LWJvcmRlci1jb2xvcjogIzNjNmE3NjtcbiRidXR0b24tZGFuZ2VyLWNvbG9yOiAjZmZmZmZmO1xuJGJ1dHRvbi1kYW5nZXItYmc6ICNkOTUzNGY7XG4kYnV0dG9uLWRhbmdlci1ib3JkZXItY29sb3I6ICNkNDNmM2E7XG4kYnV0dG9uLXN1Y2Nlc3MtY29sb3I6ICNmZmZmZmY7XG4kYnV0dG9uLXN1Y2Nlc3MtYmc6ICM1Y2I4NWM7XG4kYnV0dG9uLXN1Y2Nlc3MtYm9yZGVyLWNvbG9yOiAjNGNhZTRjO1xuJGxpc3QtYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJGxpc3QtZ3JvdXAtY29sb3I6ICMwMDAwMDBkZTtcbiRsaXN0LWdyb3VwLWhlYWRlci1iZzogI2VlZWVlZTBkO1xuJGxpc3Qtbm9ybWFsLWNvbG9yOiAjMDAwMDAwZGU7XG4kbGlzdC1pdGVtLWhvdmVyLWJnOiAjMDAwMDAwMGE7XG4kbGlzdC1pdGVtLXNlbGVjdGVkLWJnOiAjMDAwMDAwMWE7XG4kbGlzdC1pdGVtLWZvY3VzZWQtc2VsZWN0ZWQtYmc6ICM0NTc5ODdiMztcbiRsaXN0LWZvY3VzZWQtYmc6ICM0NTc5ODc7XG4kbGlzdC1pdGVtLWFjdGl2ZS1iZzogIzQ1Nzk4NztcbiRmaWVsZHNldC1maWVsZC1sYWJlbC1jb2xvcjogIzAwMDAwMGRlO1xuJGNoZWNrYm94LWJvcmRlci1jb2xvcjogI2UwZTBlMDtcbiRjaGVja2JveC1mb2N1c2VkLWJvcmRlcmQtY29sb3I6ICM0NTc5ODc7XG4kY2hlY2tib3gtYmc6ICNmZmZmZmY7XG4kY2hlY2tib3gtaG92ZXItYm9yZGVyLWNvbG9yOiAjMzA1NTVmO1xuJGNoZWNrYm94LWFjdGl2ZS1pY29uLWJnOiAjNjA2MDYwMzM7XG4kYnV0dG9uLWdyb3VwLW5vcm1hbC1zZWxlY3RlZC1jb2xvcjogIzAwMDAwMGRlO1xuJGJ1dHRvbi1ncm91cC1ub3JtYWwtc2VsZWN0ZWQtYmc6ICMwMDAwMDAyYjtcbiRidXR0b24tZ3JvdXAtZGVmYXVsdC1zZWxlY3RlZC1jb2xvcjogIzQ1Nzk4NztcbiRidXR0b24tZ3JvdXAtZGVmYXVsdC1zZWxlY3RlZC1iZzogIzQ1Nzk4NzRkO1xuJGJ1dHRvbi1ncm91cC1kYW5nZXItc2VsZWN0ZWQtY29sb3I6ICNkOTUzNGY7XG4kYnV0dG9uLWdyb3VwLWRhbmdlci1zZWxlY3RlZC1iZzogI2Q5NTM0ZjRkO1xuJGJ1dHRvbi1ncm91cC1zdWNjZXNzLXNlbGVjdGVkLWNvbG9yOiAjNWNiODVjO1xuJGJ1dHRvbi1ncm91cC1zdWNjZXNzLXNlbGVjdGVkLWJnOiAjNWNiODVjNGQ7XG4kc2Nyb2xsYWJsZS1zY3JvbGwtYmc6ICNiZmJmYmZiMztcbiRzY3JvbGxhYmxlLXNjcm9sbGJhci1hY3RpdmUtYmc6ICNiZmJmYmYzMztcbiRzd2l0Y2gtYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJHN3aXRjaC1vbi1jb2xvcjogIzAwMDAwMGRlO1xuJHN3aXRjaC1jb250YWluZXItYWN0aXZlLWJnOiAjNjA2MDYwMzM7XG4kc3dpdGNoLW9mZi1jb2xvcjogIzY2NjY2NmRlO1xuJHRhYnMtdGFiLWNvbG9yOiAjMDAwMDAwZGU7XG4kdGFicy10YWItc2VsZWN0ZWQtY29sb3I6ICMwMDAwMDBkZTtcbiR0YWJzLXRhYi1iZzogI2Y3ZjdmNztcbiR0YWJzLXRhYi1zZWxlY3RlZC1iZzogI2ZmZmZmZjtcbiR0YWJzLWJvcmRlci1jb2xvcjogI2UwZTBlMDtcbiR0YWJzLWZvY3VzZWQtYm9yZGVyLWNvbG9yOiAjNDU3OTg3O1xuJHRhYnMtdGFiLWhvdmVyLWJnOiAjZmZmZmZmO1xuJGJhZGdlLWNvbG9yOiAjZmZmZmZmO1xuJGJhZGdlLWJnOiAjNDU3OTg3O1xuJG92ZXJsYXktY29udGVudC1iZzogI2ZmZmZmZjtcbiRvdmVybGF5LXNoYWRlci1iZzogI2ZmZmZmZmNjO1xuJG92ZXJsYXktZm9jdXMtYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJHRleHRib3gtc2VhcmNoLWljb24tY29sb3I6ICM2NjY2NjZkZTtcbiR0ZXh0ZWRpdG9yLWNvbG9yOiAjMDAwMDAwZGU7XG4kdGV4dGVkaXRvci1wbGFjZWhvbGRlci1jb2xvcjogIzY2NjY2NmRlO1xuJHRleHRlZGl0b3ItYmc6ICNmZmZmZmY7XG4kdGV4dGVkaXRvci1maWxsZWQtYmc6ICMwMDAwMDAwZDtcbiR0ZXh0ZWRpdG9yLWJvcmRlci1jb2xvcjogI2UwZTBlMDtcbiR0ZXh0ZWRpdG9yLWZvY3VzZWQtYm9yZGVyLWNvbG9yOiAjNDU3OTg3O1xuJHRleHRlZGl0b3ItaG92ZXItYm9yZGVyLWNvbG9yOiAjNDU3OTg3NjY7XG4kZHJvcGRvd25lZGl0b3ItYnV0dG9uLWhvdmVyLWJnOiAjZTZlNmU2O1xuJGRyb3Bkb3duZWRpdG9yLWJ1dHRvbi1hY3RpdmUtYmc6ICNiM2IzYjM7XG4kZHJvcGRvd25lZGl0b3ItaWNvbi1jb2xvcjogIzAwMDAwMGRlO1xuJGRyb3Bkb3duZWRpdG9yLWljb24tYWN0aXZlLWNvbG9yOiAjMDAwMDAwZGU7XG4kbnVtYmVyYm94LXNwaW4tYWN0aXZlLWJnOiAjYjNiM2IzO1xuJG51bWJlcmJveC1zcGluLWhvdmVyLWJvcmRlci1jb2xvcjogdHJhbnNwYXJlbnQ7XG4kbnVtYmVyYm94LXNwaW4taWNvbi1jb2xvcjogIzAwMDAwMGRlO1xuJGNhbGVuZGFyLWNvbG9yOiAjMDAwMDAwZGU7XG4kY2FsZW5kYXItaGVhZGVyLWNvbG9yOiAjMDAwMDAwZGU7XG4kY2FsZW5kYXItY2VsbC1vdGhlci1jb2xvcjogIzAwMDAwMDYxO1xuJGNhbGVuZGFyLWJnOiAjZmZmZmZmO1xuJGNhbGVuZGFyLWhvdmVyLWJnOiAjNDU3OTg3M2Q7XG4kY2FsZW5kYXItY2VsbC1zZWxlY3RlZC1iZzogIzQ1Nzk4NztcbiRjYWxlbmRhci1jZWxsLWFjdGl2ZS1iZzogIzQ1Nzk4NzY2O1xuJGxvYWRpbmRpY2F0b3ItYmc6ICMyZDJkMmRkZTtcbiR0cmVldmlldy1mb2N1c2VkLWJnOiAjNDU3OTg3O1xuJHRyZWV2aWV3LWhvdmVyLWJnOiAjMDAwMDAwMGE7XG4kdHJlZXZpZXctYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJHRyZWV2aWV3LWl0ZW0tc2VsZWN0ZWQtY29sb3I6ICMwMDAwMDBkZTtcbiR0cmVldmlldy1zcGluLWljb24tY29sb3I6ICMwMDAwMDBkZTtcbiRtZW51LXBvcHVwLWJnOiAjZmZmZmZmO1xuJG1lbnUtaXRlbS1ob3Zlci1iZzogIzAwMDAwMDBhO1xuJG1lbnUtY29sb3I6ICMwMDAwMDBkZTtcbiRtZW51LWl0ZW0taG92ZXJlZC1jb2xvcjogIzAwMDAwMGRlO1xuJG1lbnUtaXRlbS1leHBhbmRlZC1jb2xvcjogIzAwMDAwMGRlO1xuJG1lbnUtaXRlbS1zZWxlY3RlZC1iZzogI2U2ZTZlNjtcbiRtZW51LXBvcHVwLWJvcmRlci1jb2xvcjogI2UwZTBlMDtcbiRzZWxlY3Rib3gtbGlzdC1iZzogI2ZmZmZmZjtcbiR0b29sYmFyLWJnOiAjZmZmZmZmO1xuJHBvcHVwLXRpdGxlLWJnOiB0cmFuc3BhcmVudDtcbiR0aWxldmlldy1jb2xvcjogIzAwMDAwMGRlO1xuJHRvYXN0LWNvbG9yOiAjZmZmZmZmO1xuJHRvYXN0LWluZm8tYmc6ICM0NTc5ODc7XG4kdG9hc3Qtd2FybmluZy1iZzogI2YwYWQ0ZTtcbiR0b2FzdC1lcnJvci1iZzogI2Q5NTM0ZjtcbiR0b2FzdC1zdWNjZXNzLWJnOiAjNWNiODVjO1xuJHByb2dyZXNzYmFyLWJnOiAjZGRkZGRkO1xuJHByb2dyZXNzYmFyLXJhbmdlLWJnOiAjNDU3OTg3O1xuJHRvb2x0aXAtY29sb3I6ICMwMDAwMDBkZTtcbiR0b29sdGlwLWJnOiAjZmZmZmZmO1xuJHRvb2x0aXAtYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJHNsaWRlci1iYXItYmc6ICNlMGUwZTA7XG4kc2xpZGVyLXJhbmdlLWJnOiAjNDU3OTg3O1xuJGdhbGxlcnktaW5kaWNhdG9yLWJnOiAjZmZmZmZmO1xuJGdhbGxlcnktaW5kaWNhdG9yLWl0ZW0tc2VsZWN0ZWQtYmc6ICM0NTc5ODc7XG4kZ2FsbGVyeS1pbmRpY2F0b3ItZm9jdXNlZC1iZzogIzJiNGM1NDtcbiRnYWxsZXJ5LW5hdmJ1dHRvbi1ob3Zlci1jb2xvcjogIzQ1Nzk4NzgwO1xuJGdhbGxlcnktbmF2LWFycm93LWNvbG9yOiAjZmZmZmZmO1xuJGdhbGxlcnktbmF2YnV0dG9uLWFjdGl2ZS1jb2xvcjogIzQ1Nzk4N2IzO1xuJGxvYWRwYW5lbC1jb250ZW50LWJnOiAjZmZmZmZmO1xuJHRhZ2JveC10YWctY29sb3I6ICMwMDAwMDBkZTtcbiR0YWdib3gtdGFnLWJnOiAjZGRkZGRkO1xuJHRhZ2JveC10YWctYnV0dG9uLXJlbW92ZS1iZzogI2FhYWFhYTtcbiRyYWRpb2dyb3VwLWNoZWNrZWQtYmc6ICM0NTc5ODc7XG4kcmFkaW9idXR0b24tYWN0aXZlLWJnOiAjNjA2MDYwMzM7XG4kYWNjb3JkaW9uLWNvbG9yOiAjMDAwMDAwZGU7XG4kYWNjb3JkaW9uLXRpdGxlLWNvbG9yOiAjMDAwMDAwZGU7XG4kYWNjb3JkaW9uLWl0ZW0tdGl0bGUtb3BlbmVkLWJnOiB0cmFuc3BhcmVudDtcbiRhY2NvcmRpb24taXRlbS1ib3JkZXItY29sb3I6ICNlMGUwZTA7XG4kYWNjb3JkaW9uLWl0ZW0tZm9jdXNlZC1ib3JkZXItY29sb3I6ICM0NTc5ODc7XG4kYWNjb3JkaW9uLXRpdGxlLWFjdGl2ZS1jb2xvcjogIzAwMDAwMGRlO1xuJGFjY29yZGlvbi1pdGVtLWhvdmVyLWJnOiAjMDAwMDAwMGE7XG4kYWNjb3JkaW9uLWljb24tYWN0aXZlLWNvbG9yOiAjMDAwMDAwZGU7XG4kY29sb3Jib3gtb3ZlcmxheS1iZzogI2ZmZmZmZjtcbiRkYXRhZ3JpZC1iYXNlLWNvbG9yOiAjMDAwMDAwZGU7XG4kZGF0YWdyaWQtYmFzZS1iYWNrZ3JvdW5kLWNvbG9yOiAjZmZmZmZmO1xuJGRhdGFncmlkLWJvcmRlci1jb2xvcjogI2UwZTBlMDtcbiRkYXRhZ3JpZC1jb2x1bW5jaG9vc2VyLWl0ZW0tY29sb3I6ICM2MjYyNjJkZTtcbiRkYXRhZ3JpZC1jb2x1bW5jaG9vc2VyLWZvbnQtd2VpZ2h0OiBub3JtYWw7XG4kZGF0YWdyaWQtZHJhZy1oZWFkZXItYm9yZGVyLWNvbG9yOiAjNDU3OTg3ODA7XG4kZGF0YWdyaWQtc2VsZWN0aW9uLWJnOiAjZTZlNmU2O1xuJGRhdGFncmlkLXJvdy1zZWxlY3RlZC1ib3JkZXItY29sb3I6ICNlMGUwZTA7XG4kZGF0YWdyaWQtcm93LXNlbGVjdGVkLWNvbG9yOiAjMDAwMDAwZGU7XG4kZGF0YWdyaWQtcm93LWZvY3VzZWQtY29sb3I6ICNmZmZmZmY7XG4kZGF0YWdyaWQtcm93LWZvY3VzZWQtYmc6ICM2YTk0OWY7XG4kZGF0YWdyaWQtaG92ZXItYmc6ICMwMDAwMDAwYTtcbiRkYXRhZ3JpZC1tZW51LWljb24tY29sb3I6ICM1NjU2NTZkZTtcbiRkYXRhZ3JpZC1jZWxsLW1vZGlmaWVkLWJvcmRlci1jb2xvcjogIzVjYjg1YzgwO1xuJGRhdGFncmlkLXJvdy1pbnZhbGlkLWZhZGVkLWJvcmRlci1jb2xvcjogI2Q5NTM0ZjY2O1xuJGRhdGFncmlkLW5vZGF0YS1jb2xvcjogIzY2NjY2NmRlO1xuJGRhdGFncmlkLWdyb3VwLXJvdy1jb2xvcjogIzYyNjI2MmRlO1xuJGRhdGFncmlkLWdyb3VwLXJvdy1iZzogI2Y3ZjdmNztcbiRkYXRhZ3JpZC1zZWFyY2gtY29sb3I6ICNmZmZmZmY7XG4kZGF0YWdyaWQtc3Bpbi1pY29uLWNvbG9yOiAjNjI2MjYyZGU7XG4kZGF0YWdyaWQtc2VhcmNoLWJnOiAjNDU3OTg3O1xuJGRhdGFncmlkLXJvdy1lcnJvci1jb2xvcjogI2ZmZmZmZjtcbiRkYXRhZ3JpZC1yb3ctZXJyb3ItYmc6ICNlODk4OTU7XG4kZGF0YWdyaWQtbGluay1jb2xvcjogIzQ1Nzk4NztcbiRkYXRhZ3JpZC1mb2N1c2VkLWJvcmRlci1jb2xvcjogIzZhOTQ5ZjtcbiRkYXRhZ3JpZC1lZGl0b3ItYmc6ICNmZmZmZmY7XG4kZGF0YWdyaWQtY29sdW1uY2hvb3Nlci1iZzogI2ZmZmZmZjtcbiRkYXRhZ3JpZC1yb3ctYWx0ZXJuYXRpb24tYmc6ICNmNWY1ZjU7XG4kZGF0YWdyaWQtc3VtbWFyeS1jb2xvcjogIzAwMDAwMGIzO1xuJHBhZ2VyLXBhZ2Utc2VsZWN0ZWQtY29sb3I6ICMwMDAwMDBkZTtcbiRwYWdlci1wYWdlLXNlbGVjdGVkLWJnOiAjZDRkNGQ0O1xuJHBpdm90Z3JpZC1hcmVhLWNvbG9yOiAjNjI2MjYyZGU7XG4kcGl2b3RncmlkLXRvdGFsY29sb3I6ICNkZGRkZGQzMztcbiRwaXZvdGdyaWQtZ3JhbmR0b3RhbGNvbG9yOiAjZjVmNWY1O1xuJHBpdm90Z3JpZC1maWVsZC1hcmVhLXRleHQtY29sb3I6ICMxZjFmMWZkZTtcbiRwaXZvdGdyaWQtc3Bpbi1pY29uLWNvbG9yOiAjNjI2MjYyZGU7XG4kZmlsZXVwbG9hZGVyLWZpbGVuYW1lLWNvbG9yOiAjMDAwMDAwZGU7XG4kZmlsZXVwbG9hZGVyLWZhbGVuYW1lLXN0YXR1cy1jb2xvcjogIzY2NjY2NmRlO1xuJGZpbGV1cGxvYWRlci1ib3JkZXItY29sb3I6ICNlMGUwZTA7XG4kc2NoZWR1bGVyLWJhc2UtYm9yZGVyLWNvbG9yOiAjZTBlMGUwOTk7XG4kc2NoZWR1bGVyLWFjY2VudC1ib3JkZXItY29sb3I6ICNjN2M3Yzc7XG4kc2NoZWR1bGVyLWFwcG9pbnRtZW50LXRleHQtY29sb3I6ICNmZmZmZmY7XG4kc2NoZWR1bGVyLWFwcG9pbnRtZW50LWJhc2UtY29sb3I6ICM0NTc5ODc7XG4kc2NoZWR1bGVyLWFwcG9pbnRtZW50LXN0YXJ0LWNvbG9yOiAjMDAwMDAwNGQ7XG4kc2NoZWR1bGVyLWZpcnN0LW1vbnRoLWJvcmRlci1jb2xvcjogI2FkYWRhZDtcbiRzY2hlZHVsZXItd29ya3NwYWNlLWZvY3VzZWQtY2VsbC1jb2xvcjogI2NjZTBlNTtcbiRzY2hlZHVsZXItY3VycmVudC10aW1lLWNlbGwtY29sb3I6ICM0NTc5ODc7XG4kc2NoZWR1bGVyLXRpbWUtaW5kaWNhdG9yLWNvbG9yOiAjNjdhMmIxO1xuJGZvcm0tZmllbGQtaXRlbS1jb2xvcjogIzAwMDAwMGRlO1xuJGZvcm0tZ3JvdXAtYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJGZpbHRlcmJ1aWxkZXItdGV4dC1jb2xvcjogIzAwMDAwMGRlO1xuJGZpbHRlcmJ1aWxkZXItdGV4dC1mb2N1cy1jb2xvcjogI2ZmZmZmZjtcbiRmaWx0ZXJidWlsZGVyLXBsdXMtaWNvbi1jb2xvcjogIzVjYjg1YzRkO1xuJGZpbHRlcmJ1aWxkZXItcmVtb3ZlLWljb24tY29sb3I6ICNkOTUzNGY0ZDtcbiRmaWx0ZXJidWlsZGVyLWdyb3VwLW9wZXJhdGlvbi1jb2xvcjogI2Q5NTM0ZjRkO1xuJGZpbHRlcmJ1aWxkZXItaXRlbS1maWVsZC1jb2xvcjogIzQ1Nzk4NzRkO1xuJGZpbHRlcmJ1aWxkZXItaXRlbS1vcGVyYXRvci1jb2xvcjogIzVjYjg1YzRkO1xuJGZpbHRlcmJ1aWxkZXItaXRlbS12YWx1ZS1jb2xvcjogI2UwZTBlMDgwO1xuJGZpbHRlcmJ1aWxkZXItbWVudS1pY29uLWNvbG9yOiAjNTY1NjU2ZGU7XG4kZHJhd2VyLXNoYWRlci1iYWNrZ3JvdW5kLWNvbG9yOiAjMDAwMDAwODA7XG4iXSwic291cmNlUm9vdCI6IiJ9 */"]
});
class CreateAccountFormModule {}
CreateAccountFormModule.ɵfac = function CreateAccountFormModule_Factory(t) {
  return new (t || CreateAccountFormModule)();
};
CreateAccountFormModule.ɵmod = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdefineNgModule"]({
  type: CreateAccountFormModule
});
CreateAccountFormModule.ɵinj = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdefineInjector"]({
  imports: [_angular_common__WEBPACK_IMPORTED_MODULE_5__.CommonModule, _angular_router__WEBPACK_IMPORTED_MODULE_4__.RouterModule, devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_6__.DxFormModule, devextreme_angular_ui_load_indicator__WEBPACK_IMPORTED_MODULE_9__.DxLoadIndicatorModule]
});
(function () {
  (typeof ngJitMode === "undefined" || ngJitMode) && _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵsetNgModuleScope"](CreateAccountFormModule, {
    declarations: [CreateAccountFormComponent],
    imports: [_angular_common__WEBPACK_IMPORTED_MODULE_5__.CommonModule, _angular_router__WEBPACK_IMPORTED_MODULE_4__.RouterModule, devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_6__.DxFormModule, devextreme_angular_ui_load_indicator__WEBPACK_IMPORTED_MODULE_9__.DxLoadIndicatorModule],
    exports: [CreateAccountFormComponent]
  });
})();

/***/ }),

/***/ 68014:
/*!**************************************************************!*\
  !*** ./src/app/shared/components/footer/footer.component.ts ***!
  \**************************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   FooterComponent: () => (/* binding */ FooterComponent),
/* harmony export */   FooterModule: () => (/* binding */ FooterModule)
/* harmony export */ });
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! @angular/core */ 61699);

const _c0 = ["*"];
class FooterComponent {}
FooterComponent.ɵfac = function FooterComponent_Factory(t) {
  return new (t || FooterComponent)();
};
FooterComponent.ɵcmp = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵdefineComponent"]({
  type: FooterComponent,
  selectors: [["app-footer"]],
  ngContentSelectors: _c0,
  decls: 2,
  vars: 0,
  template: function FooterComponent_Template(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵprojectionDef"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementStart"](0, "footer");
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵprojection"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementEnd"]();
    }
  },
  styles: ["[_nghost-%COMP%] {\n  display: block;\n  color: rgba(69, 121, 135, 0.7);\n  border-top: 1px solid rgba(0, 0, 0, 0.1);\n  padding-top: 20px;\n  padding-bottom: 24px;\n}\n/*# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbIndlYnBhY2s6Ly8uL3NyYy9hcHAvc2hhcmVkL2NvbXBvbmVudHMvZm9vdGVyL2Zvb3Rlci5jb21wb25lbnQuc2NzcyJdLCJuYW1lcyI6W10sIm1hcHBpbmdzIjoiQUFFQTtFQUNFLGNBQUE7RUFDQSw4QkFBQTtFQUNBLHdDQUFBO0VBQ0EsaUJBQUE7RUFDQSxvQkFBQTtBQURGIiwic291cmNlc0NvbnRlbnQiOlsiQGltcG9ydCBcIi4uLy4uLy4uLy4uL3RoZW1lcy9nZW5lcmF0ZWQvdmFyaWFibGVzLmJhc2UuZGFyay5zY3NzXCI7XG5cbjpob3N0IHtcbiAgZGlzcGxheTogYmxvY2s7XG4gIGNvbG9yOiByZ2JhKCRiYXNlLWFjY2VudCwgYWxwaGEoJGJhc2UtYWNjZW50KSAqIDAuNyk7XG4gIGJvcmRlci10b3A6IDFweCBzb2xpZCByZ2JhKDAsIDAsIDAsIDAuMSk7XG4gIHBhZGRpbmctdG9wOiAyMHB4O1xuICBwYWRkaW5nLWJvdHRvbTogMjRweDtcbn1cbiJdLCJzb3VyY2VSb290IjoiIn0= */"]
});
class FooterModule {}
FooterModule.ɵfac = function FooterModule_Factory(t) {
  return new (t || FooterModule)();
};
FooterModule.ɵmod = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵdefineNgModule"]({
  type: FooterModule
});
FooterModule.ɵinj = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵdefineInjector"]({});
(function () {
  (typeof ngJitMode === "undefined" || ngJitMode) && _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵsetNgModuleScope"](FooterModule, {
    declarations: [FooterComponent],
    exports: [FooterComponent]
  });
})();

/***/ }),

/***/ 10074:
/*!**************************************************************!*\
  !*** ./src/app/shared/components/header/header.component.ts ***!
  \**************************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   HeaderComponent: () => (/* binding */ HeaderComponent),
/* harmony export */   HeaderModule: () => (/* binding */ HeaderModule)
/* harmony export */ });
/* harmony import */ var C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./node_modules/@babel/runtime/helpers/esm/asyncToGenerator.js */ 71670);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var _angular_common__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! @angular/common */ 26575);
/* harmony import */ var _user_panel_user_panel_component__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../user-panel/user-panel.component */ 57387);
/* harmony import */ var devextreme_angular_ui_button__WEBPACK_IMPORTED_MODULE_9__ = __webpack_require__(/*! devextreme-angular/ui/button */ 64766);
/* harmony import */ var devextreme_angular_ui_toolbar__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(/*! devextreme-angular/ui/toolbar */ 70493);
/* harmony import */ var _services__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ../../services */ 57175);
/* harmony import */ var _angular_router__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! @angular/router */ 27947);
/* harmony import */ var devextreme_angular_core__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! devextreme-angular/core */ 65787);
/* harmony import */ var devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_8__ = __webpack_require__(/*! devextreme-angular/ui/nested */ 71723);













const _c0 = function (a2) {
  return {
    icon: "menu",
    stylingMode: "text",
    onClick: a2
  };
};
function HeaderComponent_dxi_item_2_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](0, "dxi-item", 3);
  }
  if (rf & 2) {
    const ctx_r0 = _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵnextContext"]();
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("options", _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵpureFunction1"](1, _c0, ctx_r0.toggleMenu));
  }
}
function HeaderComponent_dxi_item_3_div_1_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](0, "div")(1, "span", 6);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtext"](2);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]()();
  }
  if (rf & 2) {
    const ctx_r2 = _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵnextContext"](2);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](2);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtextInterpolate"](ctx_r2.title);
  }
}
function HeaderComponent_dxi_item_3_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](0, "dxi-item", 4);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplate"](1, HeaderComponent_dxi_item_3_div_1_Template, 3, 1, "div", 5);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]();
  }
  if (rf & 2) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("dxTemplateOf", "item");
  }
}
class HeaderComponent {
  constructor(authService, router) {
    this.authService = authService;
    this.router = router;
    this.menuToggle = new _angular_core__WEBPACK_IMPORTED_MODULE_3__.EventEmitter();
    this.menuToggleEnabled = false;
    this.user = {
      user: ''
    };
    this.userMenuItems = [{
      text: 'Profile',
      icon: 'user',
      onClick: () => {
        this.router.navigate(['/profile']);
      }
    }, {
      text: 'Logout',
      icon: 'runner',
      onClick: () => {
        this.authService.logOut();
      }
    }];
    this.toggleMenu = () => {
      this.menuToggle.emit();
    };
  }
  ngOnInit() {
    var _this = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      if (!_this.user) {
        var user = yield _this.authService.getUser();
        _this.user = user.data;
        _this.router.navigate(['/']);
      }
    })();
  }
}
HeaderComponent.ɵfac = function HeaderComponent_Factory(t) {
  return new (t || HeaderComponent)(_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdirectiveInject"](_services__WEBPACK_IMPORTED_MODULE_2__.AuthService), _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdirectiveInject"](_angular_router__WEBPACK_IMPORTED_MODULE_4__.Router));
};
HeaderComponent.ɵcmp = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdefineComponent"]({
  type: HeaderComponent,
  selectors: [["app-header"]],
  inputs: {
    menuToggleEnabled: "menuToggleEnabled",
    title: "title"
  },
  outputs: {
    menuToggle: "menuToggle"
  },
  decls: 4,
  vars: 2,
  consts: [[1, "header-toolbar"], ["location", "before", "widget", "dxButton", "cssClass", "menu-button", 3, "options", 4, "ngIf"], ["location", "before", 4, "ngIf"], ["location", "before", "widget", "dxButton", "cssClass", "menu-button", 3, "options"], ["location", "before"], [4, "dxTemplate", "dxTemplateOf"], ["translate", "", 2, "margin-left", "20px", "font-size", "20px", "color", "white"]],
  template: function HeaderComponent_Template(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](0, "header")(1, "dx-toolbar", 0);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplate"](2, HeaderComponent_dxi_item_2_Template, 1, 3, "dxi-item", 1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplate"](3, HeaderComponent_dxi_item_3_Template, 2, 1, "dxi-item", 2);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]()();
    }
    if (rf & 2) {
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("ngIf", ctx.menuToggleEnabled);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("ngIf", ctx.title);
    }
  },
  dependencies: [_angular_common__WEBPACK_IMPORTED_MODULE_5__.NgIf, devextreme_angular_core__WEBPACK_IMPORTED_MODULE_6__.DxTemplateDirective, devextreme_angular_ui_toolbar__WEBPACK_IMPORTED_MODULE_7__.DxToolbarComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_8__.DxiItemComponent],
  styles: ["html[_ngcontent-%COMP%], body[_ngcontent-%COMP%] {\n  margin: 0px;\n  min-height: 100%;\n  height: 100%;\n}\n\n*[_ngcontent-%COMP%] {\n  box-sizing: border-box;\n}\n\n.content[_ngcontent-%COMP%] {\n  line-height: 1.5;\n  flex-grow: 1;\n}\n.content[_ngcontent-%COMP%]   h2[_ngcontent-%COMP%] {\n  font-size: 30px;\n  margin-top: 20px;\n  margin-bottom: 20px;\n}\n\n.container[_ngcontent-%COMP%] {\n  height: 100%;\n  flex-direction: column;\n  display: flex;\n}\n\n.layout-body[_ngcontent-%COMP%] {\n  flex: 1;\n  min-height: 0;\n}\n\n.side-nav-outer-toolbar[_ngcontent-%COMP%]   .dx-drawer[_ngcontent-%COMP%] {\n  height: calc(100% - 56px);\n}\n\n.content-block[_ngcontent-%COMP%] {\n  margin-left: 40px;\n  margin-right: 40px;\n  margin-top: 20px;\n}\n.screen-x-small[_ngcontent-%COMP%]   .content-block[_ngcontent-%COMP%] {\n  margin-left: 20px;\n  margin-right: 20px;\n}\n\n.responsive-paddings[_ngcontent-%COMP%] {\n  padding: 20px;\n}\n.screen-large[_ngcontent-%COMP%]   .responsive-paddings[_ngcontent-%COMP%] {\n  padding: 40px;\n}\n\n.dx-card.wide-card[_ngcontent-%COMP%] {\n  border-radius: 0;\n  margin-left: 0;\n  margin-right: 0;\n  border-right: 0;\n  border-left: 0;\n}\n\n.with-footer[_ngcontent-%COMP%]    > .dx-scrollable-wrapper[_ngcontent-%COMP%]    > .dx-scrollable-container[_ngcontent-%COMP%]    > .dx-scrollable-content[_ngcontent-%COMP%] {\n  height: 100%;\n}\n.with-footer[_ngcontent-%COMP%]    > .dx-scrollable-wrapper[_ngcontent-%COMP%]    > .dx-scrollable-container[_ngcontent-%COMP%]    > .dx-scrollable-content[_ngcontent-%COMP%]    > .dx-scrollview-content[_ngcontent-%COMP%] {\n  display: flex;\n  flex-direction: column;\n  min-height: 100%;\n}\n\n[_nghost-%COMP%] {\n  flex: 0 0 auto;\n  z-index: 1;\n  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.12), 0 1px 2px rgba(0, 0, 0, 0.24);\n}\n[_nghost-%COMP%]     .dx-toolbar .dx-toolbar-item.menu-button > .dx-toolbar-item-content .dx-icon {\n  color: #457987;\n}\n\n  .dx-toolbar.header-toolbar .dx-toolbar-items-container .dx-toolbar-after {\n  padding: 0 40px;\n}\n.screen-x-small[_nghost-%COMP%]     .dx-toolbar.header-toolbar .dx-toolbar-items-container .dx-toolbar-after, .screen-x-small   [_nghost-%COMP%]     .dx-toolbar.header-toolbar .dx-toolbar-items-container .dx-toolbar-after {\n  padding: 0 20px;\n}\n\n  .dx-toolbar .dx-toolbar-item.menu-button {\n  width: 60px;\n  text-align: center;\n  padding: 0;\n}\n\n  .header-title .dx-item-content {\n  padding: 0;\n  margin: 0;\n}\n\n.dx-theme-generic[_nghost-%COMP%]   .dx-toolbar[_ngcontent-%COMP%], .dx-theme-generic   [_nghost-%COMP%]   .dx-toolbar[_ngcontent-%COMP%] {\n  padding: 10px 0;\n}\n.dx-theme-generic[_nghost-%COMP%]   .user-button[_ngcontent-%COMP%]    > .dx-button-content[_ngcontent-%COMP%], .dx-theme-generic   [_nghost-%COMP%]   .user-button[_ngcontent-%COMP%]    > .dx-button-content[_ngcontent-%COMP%] {\n  padding: 3px;\n}\n/*# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbIndlYnBhY2s6Ly8uL3NyYy9keC1zdHlsZXMuc2NzcyIsIndlYnBhY2s6Ly8uL3NyYy9hcHAvc2hhcmVkL2NvbXBvbmVudHMvaGVhZGVyL2hlYWRlci5jb21wb25lbnQuc2NzcyIsIndlYnBhY2s6Ly8uL3NyYy90aGVtZXMvZ2VuZXJhdGVkL3ZhcmlhYmxlcy5iYXNlLnNjc3MiXSwibmFtZXMiOltdLCJtYXBwaW5ncyI6IkFBQUE7RUFDRSxXQUFBO0VBQ0EsZ0JBQUE7RUFDQSxZQUFBO0FDQ0Y7O0FERUE7RUFDRSxzQkFBQTtBQ0NGOztBREVBO0VBQ0UsZ0JBQUE7RUFDQSxZQUFBO0FDQ0Y7QURDRTtFQUNFLGVBQUE7RUFDQSxnQkFBQTtFQUNBLG1CQUFBO0FDQ0o7O0FER0E7RUFDRSxZQUFBO0VBQ0Esc0JBQUE7RUFDQSxhQUFBO0FDQUY7O0FER0E7RUFDRSxPQUFBO0VBQ0EsYUFBQTtBQ0FGOztBREdBO0VBQ0UseUJBQUE7QUNBRjs7QURHQTtFQUNFLGlCQUFBO0VBQ0Esa0JBQUE7RUFDQSxnQkFBQTtBQ0FGO0FERUU7RUFDRSxpQkFBQTtFQUNBLGtCQUFBO0FDQUo7O0FESUE7RUFDRSxhQUFBO0FDREY7QURHRTtFQUNFLGFBQUE7QUNESjs7QURLQTtFQUNFLGdCQUFBO0VBQ0EsY0FBQTtFQUNBLGVBQUE7RUFDQSxlQUFBO0VBQ0EsY0FBQTtBQ0ZGOztBREtBO0VBRUUsWUFBQTtBQ0hGO0FES0U7RUFDRSxhQUFBO0VBQ0Esc0JBQUE7RUFDQSxnQkFBQTtBQ0hKOztBQWhFQTtFQUNFLGNBQUE7RUFDQSxVQUFBO0VBQ0Esd0VBQUE7QUFtRUY7QUFqRUU7RUFDRSxjQ1JVO0FEMkVkOztBQS9EQTtFQUNFLGVBQUE7QUFrRUY7QUFoRUU7RUFDRSxlQUFBO0FBa0VKOztBQTlEQTtFQUNFLFdEb0RxQjtFQ25EckIsa0JBQUE7RUFDQSxVQUFBO0FBaUVGOztBQTlEQTtFQUNFLFVBQUE7RUFDQSxTQUFBO0FBaUVGOztBQTdERTtFQUNFLGVBQUE7QUFnRUo7QUE3REU7RUFDRSxZQUFBO0FBK0RKIiwic291cmNlc0NvbnRlbnQiOlsiaHRtbCwgYm9keSB7XG4gIG1hcmdpbjogMHB4O1xuICBtaW4taGVpZ2h0OiAxMDAlO1xuICBoZWlnaHQ6IDEwMCU7XG59XG5cbioge1xuICBib3gtc2l6aW5nOiBib3JkZXItYm94O1xufVxuXG4uY29udGVudCB7XG4gIGxpbmUtaGVpZ2h0OiAxLjU7XG4gIGZsZXgtZ3JvdzogMTtcblxuICBoMiB7XG4gICAgZm9udC1zaXplOiAzMHB4O1xuICAgIG1hcmdpbi10b3A6IDIwcHg7XG4gICAgbWFyZ2luLWJvdHRvbTogMjBweDtcbiAgfVxufVxuXG4uY29udGFpbmVyIHtcbiAgaGVpZ2h0OiAxMDAlO1xuICBmbGV4LWRpcmVjdGlvbjogY29sdW1uO1xuICBkaXNwbGF5OiBmbGV4O1xufVxuXG4ubGF5b3V0LWJvZHkge1xuICBmbGV4OiAxO1xuICBtaW4taGVpZ2h0OiAwO1xufVxuXG4uc2lkZS1uYXYtb3V0ZXItdG9vbGJhciAuZHgtZHJhd2VyIHtcbiAgaGVpZ2h0OiBjYWxjKDEwMCUgLSA1NnB4KVxufVxuXG4uY29udGVudC1ibG9jayB7XG4gIG1hcmdpbi1sZWZ0OiA0MHB4O1xuICBtYXJnaW4tcmlnaHQ6IDQwcHg7XG4gIG1hcmdpbi10b3A6IDIwcHg7XG5cbiAgLnNjcmVlbi14LXNtYWxsICYge1xuICAgIG1hcmdpbi1sZWZ0OiAyMHB4O1xuICAgIG1hcmdpbi1yaWdodDogMjBweDtcbiAgfVxufVxuXG4ucmVzcG9uc2l2ZS1wYWRkaW5ncyB7XG4gIHBhZGRpbmc6IDIwcHg7XG5cbiAgLnNjcmVlbi1sYXJnZSAmIHtcbiAgICBwYWRkaW5nOiA0MHB4O1xuICB9XG59XG5cbi5keC1jYXJkLndpZGUtY2FyZCB7XG4gIGJvcmRlci1yYWRpdXM6IDA7XG4gIG1hcmdpbi1sZWZ0OiAwO1xuICBtYXJnaW4tcmlnaHQ6IDA7XG4gIGJvcmRlci1yaWdodDogMDtcbiAgYm9yZGVyLWxlZnQ6IDA7XG59XG5cbi53aXRoLWZvb3RlciA+IC5keC1zY3JvbGxhYmxlLXdyYXBwZXIgPlxuLmR4LXNjcm9sbGFibGUtY29udGFpbmVyID4gLmR4LXNjcm9sbGFibGUtY29udGVudCB7XG4gIGhlaWdodDogMTAwJTtcblxuICAmID4gLmR4LXNjcm9sbHZpZXctY29udGVudCB7XG4gICAgZGlzcGxheTogZmxleDtcbiAgICBmbGV4LWRpcmVjdGlvbjogY29sdW1uO1xuICAgIG1pbi1oZWlnaHQ6IDEwMCU7XG4gIH1cbn1cblxuJHNpZGUtcGFuZWwtbWluLXdpZHRoOiA2MHB4O1xuIiwiQGltcG9ydCBcIi4uLy4uLy4uLy4uL3RoZW1lcy9nZW5lcmF0ZWQvdmFyaWFibGVzLmJhc2Uuc2Nzc1wiO1xuQGltcG9ydCBcIi4uLy4uLy4uLy4uL2R4LXN0eWxlcy5zY3NzXCI7XG5cbjpob3N0IHtcbiAgZmxleDogMCAwIGF1dG87XG4gIHotaW5kZXg6IDE7XG4gIGJveC1zaGFkb3c6IDAgMXB4IDNweCByZ2JhKDAsIDAsIDAsIDAuMTIpLCAwIDFweCAycHggcmdiYSgwLCAwLCAwLCAwLjI0KTtcblxuICA6Om5nLWRlZXAgLmR4LXRvb2xiYXIgLmR4LXRvb2xiYXItaXRlbS5tZW51LWJ1dHRvbj4uZHgtdG9vbGJhci1pdGVtLWNvbnRlbnQgLmR4LWljb24ge1xuICAgIGNvbG9yOiAkYmFzZS1hY2NlbnQ7XG4gIH1cbn1cblxuOjpuZy1kZWVwIC5keC10b29sYmFyLmhlYWRlci10b29sYmFyIC5keC10b29sYmFyLWl0ZW1zLWNvbnRhaW5lciAuZHgtdG9vbGJhci1hZnRlciB7XG4gIHBhZGRpbmc6IDAgNDBweDtcblxuICA6aG9zdC1jb250ZXh0KC5zY3JlZW4teC1zbWFsbCkgJiB7XG4gICAgcGFkZGluZzogMCAyMHB4O1xuICB9XG59XG5cbjo6bmctZGVlcCAuZHgtdG9vbGJhciAuZHgtdG9vbGJhci1pdGVtLm1lbnUtYnV0dG9uIHtcbiAgd2lkdGg6ICRzaWRlLXBhbmVsLW1pbi13aWR0aDtcbiAgdGV4dC1hbGlnbjogY2VudGVyO1xuICBwYWRkaW5nOiAwO1xufVxuXG46Om5nLWRlZXAgLmhlYWRlci10aXRsZSAuZHgtaXRlbS1jb250ZW50IHtcbiAgcGFkZGluZzogMDtcbiAgbWFyZ2luOiAwO1xufVxuXG46aG9zdC1jb250ZXh0KC5keC10aGVtZS1nZW5lcmljKSB7XG4gIC5keC10b29sYmFyIHtcbiAgICBwYWRkaW5nOiAxMHB4IDA7XG4gIH1cblxuICAudXNlci1idXR0b24+LmR4LWJ1dHRvbi1jb250ZW50IHtcbiAgICBwYWRkaW5nOiAzcHg7XG4gIH1cbn1cbiIsIiRiYXNlLWZvbnQtZmFtaWx5OiBcIkhlbHZldGljYSBOZXVlXCIsIFwiU2Vnb2UgVUlcIiwgaGVsdmV0aWNhLCB2ZXJkYW5hLCBzYW5zLXNlcmlmO1xuJGJhc2UtYWNjZW50OiAjNDU3OTg3O1xuJGJhc2UtbGFiZWwtY29sb3I6ICMwMDAwMDBkZTtcbiRiYXNlLWxpbmstY29sb3I6ICM0NTc5ODc7XG4kYmFzZS1pY29uLWNvbG9yOiAjMDAwMDAwZGU7XG4kYmFzZS10ZXh0LWNvbG9yOiAjMDAwMDAwZGU7XG4kYmFzZS1iZzogI2ZmZmZmZjtcbiRiYXNlLWJvcmRlci1jb2xvcjogI2UwZTBlMDtcbiRiYXNlLWJvcmRlci1yYWRpdXM6IDRweDtcbiRiYXNlLXN1Y2Nlc3M6ICM1Y2I4NWM7XG4kYmFzZS13YXJuaW5nOiAjZjBhZDRlO1xuJGJhc2UtZGFuZ2VyOiAjZDk1MzRmO1xuJGJhc2UtaG92ZXItY29sb3I6ICMwMDAwMDBkZTtcbiRiYXNlLWhvdmVyLWJnOiAjMDAwMDAwMGE7XG4kYmFzZS1mb2N1cy1jb2xvcjogI2ZmZmZmZjtcbiRiYXNlLWZvY3VzLWJnOiAjNDU3OTg3O1xuJGJhc2UtaW52YWxpZC1jb2xvcjogI2Q5NTM0ZjtcbiRiYXNlLWludmFsaWQtZmFkZWQtYm9yZGVyLWNvbG9yOiAjZDk1MzRmNjY7XG4kYmFzZS1ib3JkZXItcmFkaXVzLXNtYWxsOiAzcHg7XG4kYmFzZS1ib3JkZXItcmFkaXVzLWxhcmdlOiA2cHg7XG4kYnV0dG9uLW5vcm1hbC1jb2xvcjogIzAwMDAwMGRlO1xuJGJ1dHRvbi1ub3JtYWwtYmc6ICNmZmZmZmY7XG4kYnV0dG9uLW5vcm1hbC1ib3JkZXItY29sb3I6ICNlMGUwZTA7XG4kYnV0dG9uLWRlZmF1bHQtY29sb3I6ICNmZmZmZmY7XG4kYnV0dG9uLWRlZmF1bHQtYmc6ICM0NTc5ODc7XG4kYnV0dG9uLWRlZmF1bHQtYm9yZGVyLWNvbG9yOiAjM2M2YTc2O1xuJGJ1dHRvbi1kYW5nZXItY29sb3I6ICNmZmZmZmY7XG4kYnV0dG9uLWRhbmdlci1iZzogI2Q5NTM0ZjtcbiRidXR0b24tZGFuZ2VyLWJvcmRlci1jb2xvcjogI2Q0M2YzYTtcbiRidXR0b24tc3VjY2Vzcy1jb2xvcjogI2ZmZmZmZjtcbiRidXR0b24tc3VjY2Vzcy1iZzogIzVjYjg1YztcbiRidXR0b24tc3VjY2Vzcy1ib3JkZXItY29sb3I6ICM0Y2FlNGM7XG4kbGlzdC1ib3JkZXItY29sb3I6ICNlMGUwZTA7XG4kbGlzdC1ncm91cC1jb2xvcjogIzAwMDAwMGRlO1xuJGxpc3QtZ3JvdXAtaGVhZGVyLWJnOiAjZWVlZWVlMGQ7XG4kbGlzdC1ub3JtYWwtY29sb3I6ICMwMDAwMDBkZTtcbiRsaXN0LWl0ZW0taG92ZXItYmc6ICMwMDAwMDAwYTtcbiRsaXN0LWl0ZW0tc2VsZWN0ZWQtYmc6ICMwMDAwMDAxYTtcbiRsaXN0LWl0ZW0tZm9jdXNlZC1zZWxlY3RlZC1iZzogIzQ1Nzk4N2IzO1xuJGxpc3QtZm9jdXNlZC1iZzogIzQ1Nzk4NztcbiRsaXN0LWl0ZW0tYWN0aXZlLWJnOiAjNDU3OTg3O1xuJGZpZWxkc2V0LWZpZWxkLWxhYmVsLWNvbG9yOiAjMDAwMDAwZGU7XG4kY2hlY2tib3gtYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJGNoZWNrYm94LWZvY3VzZWQtYm9yZGVyZC1jb2xvcjogIzQ1Nzk4NztcbiRjaGVja2JveC1iZzogI2ZmZmZmZjtcbiRjaGVja2JveC1ob3Zlci1ib3JkZXItY29sb3I6ICMzMDU1NWY7XG4kY2hlY2tib3gtYWN0aXZlLWljb24tYmc6ICM2MDYwNjAzMztcbiRidXR0b24tZ3JvdXAtbm9ybWFsLXNlbGVjdGVkLWNvbG9yOiAjMDAwMDAwZGU7XG4kYnV0dG9uLWdyb3VwLW5vcm1hbC1zZWxlY3RlZC1iZzogIzAwMDAwMDJiO1xuJGJ1dHRvbi1ncm91cC1kZWZhdWx0LXNlbGVjdGVkLWNvbG9yOiAjNDU3OTg3O1xuJGJ1dHRvbi1ncm91cC1kZWZhdWx0LXNlbGVjdGVkLWJnOiAjNDU3OTg3NGQ7XG4kYnV0dG9uLWdyb3VwLWRhbmdlci1zZWxlY3RlZC1jb2xvcjogI2Q5NTM0ZjtcbiRidXR0b24tZ3JvdXAtZGFuZ2VyLXNlbGVjdGVkLWJnOiAjZDk1MzRmNGQ7XG4kYnV0dG9uLWdyb3VwLXN1Y2Nlc3Mtc2VsZWN0ZWQtY29sb3I6ICM1Y2I4NWM7XG4kYnV0dG9uLWdyb3VwLXN1Y2Nlc3Mtc2VsZWN0ZWQtYmc6ICM1Y2I4NWM0ZDtcbiRzY3JvbGxhYmxlLXNjcm9sbC1iZzogI2JmYmZiZmIzO1xuJHNjcm9sbGFibGUtc2Nyb2xsYmFyLWFjdGl2ZS1iZzogI2JmYmZiZjMzO1xuJHN3aXRjaC1ib3JkZXItY29sb3I6ICNlMGUwZTA7XG4kc3dpdGNoLW9uLWNvbG9yOiAjMDAwMDAwZGU7XG4kc3dpdGNoLWNvbnRhaW5lci1hY3RpdmUtYmc6ICM2MDYwNjAzMztcbiRzd2l0Y2gtb2ZmLWNvbG9yOiAjNjY2NjY2ZGU7XG4kdGFicy10YWItY29sb3I6ICMwMDAwMDBkZTtcbiR0YWJzLXRhYi1zZWxlY3RlZC1jb2xvcjogIzAwMDAwMGRlO1xuJHRhYnMtdGFiLWJnOiAjZjdmN2Y3O1xuJHRhYnMtdGFiLXNlbGVjdGVkLWJnOiAjZmZmZmZmO1xuJHRhYnMtYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJHRhYnMtZm9jdXNlZC1ib3JkZXItY29sb3I6ICM0NTc5ODc7XG4kdGFicy10YWItaG92ZXItYmc6ICNmZmZmZmY7XG4kYmFkZ2UtY29sb3I6ICNmZmZmZmY7XG4kYmFkZ2UtYmc6ICM0NTc5ODc7XG4kb3ZlcmxheS1jb250ZW50LWJnOiAjZmZmZmZmO1xuJG92ZXJsYXktc2hhZGVyLWJnOiAjZmZmZmZmY2M7XG4kb3ZlcmxheS1mb2N1cy1ib3JkZXItY29sb3I6ICNlMGUwZTA7XG4kdGV4dGJveC1zZWFyY2gtaWNvbi1jb2xvcjogIzY2NjY2NmRlO1xuJHRleHRlZGl0b3ItY29sb3I6ICMwMDAwMDBkZTtcbiR0ZXh0ZWRpdG9yLXBsYWNlaG9sZGVyLWNvbG9yOiAjNjY2NjY2ZGU7XG4kdGV4dGVkaXRvci1iZzogI2ZmZmZmZjtcbiR0ZXh0ZWRpdG9yLWZpbGxlZC1iZzogIzAwMDAwMDBkO1xuJHRleHRlZGl0b3ItYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJHRleHRlZGl0b3ItZm9jdXNlZC1ib3JkZXItY29sb3I6ICM0NTc5ODc7XG4kdGV4dGVkaXRvci1ob3Zlci1ib3JkZXItY29sb3I6ICM0NTc5ODc2NjtcbiRkcm9wZG93bmVkaXRvci1idXR0b24taG92ZXItYmc6ICNlNmU2ZTY7XG4kZHJvcGRvd25lZGl0b3ItYnV0dG9uLWFjdGl2ZS1iZzogI2IzYjNiMztcbiRkcm9wZG93bmVkaXRvci1pY29uLWNvbG9yOiAjMDAwMDAwZGU7XG4kZHJvcGRvd25lZGl0b3ItaWNvbi1hY3RpdmUtY29sb3I6ICMwMDAwMDBkZTtcbiRudW1iZXJib3gtc3Bpbi1hY3RpdmUtYmc6ICNiM2IzYjM7XG4kbnVtYmVyYm94LXNwaW4taG92ZXItYm9yZGVyLWNvbG9yOiB0cmFuc3BhcmVudDtcbiRudW1iZXJib3gtc3Bpbi1pY29uLWNvbG9yOiAjMDAwMDAwZGU7XG4kY2FsZW5kYXItY29sb3I6ICMwMDAwMDBkZTtcbiRjYWxlbmRhci1oZWFkZXItY29sb3I6ICMwMDAwMDBkZTtcbiRjYWxlbmRhci1jZWxsLW90aGVyLWNvbG9yOiAjMDAwMDAwNjE7XG4kY2FsZW5kYXItYmc6ICNmZmZmZmY7XG4kY2FsZW5kYXItaG92ZXItYmc6ICM0NTc5ODczZDtcbiRjYWxlbmRhci1jZWxsLXNlbGVjdGVkLWJnOiAjNDU3OTg3O1xuJGNhbGVuZGFyLWNlbGwtYWN0aXZlLWJnOiAjNDU3OTg3NjY7XG4kbG9hZGluZGljYXRvci1iZzogIzJkMmQyZGRlO1xuJHRyZWV2aWV3LWZvY3VzZWQtYmc6ICM0NTc5ODc7XG4kdHJlZXZpZXctaG92ZXItYmc6ICMwMDAwMDAwYTtcbiR0cmVldmlldy1ib3JkZXItY29sb3I6ICNlMGUwZTA7XG4kdHJlZXZpZXctaXRlbS1zZWxlY3RlZC1jb2xvcjogIzAwMDAwMGRlO1xuJHRyZWV2aWV3LXNwaW4taWNvbi1jb2xvcjogIzAwMDAwMGRlO1xuJG1lbnUtcG9wdXAtYmc6ICNmZmZmZmY7XG4kbWVudS1pdGVtLWhvdmVyLWJnOiAjMDAwMDAwMGE7XG4kbWVudS1jb2xvcjogIzAwMDAwMGRlO1xuJG1lbnUtaXRlbS1ob3ZlcmVkLWNvbG9yOiAjMDAwMDAwZGU7XG4kbWVudS1pdGVtLWV4cGFuZGVkLWNvbG9yOiAjMDAwMDAwZGU7XG4kbWVudS1pdGVtLXNlbGVjdGVkLWJnOiAjZTZlNmU2O1xuJG1lbnUtcG9wdXAtYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJHNlbGVjdGJveC1saXN0LWJnOiAjZmZmZmZmO1xuJHRvb2xiYXItYmc6ICNmZmZmZmY7XG4kcG9wdXAtdGl0bGUtYmc6IHRyYW5zcGFyZW50O1xuJHRpbGV2aWV3LWNvbG9yOiAjMDAwMDAwZGU7XG4kdG9hc3QtY29sb3I6ICNmZmZmZmY7XG4kdG9hc3QtaW5mby1iZzogIzQ1Nzk4NztcbiR0b2FzdC13YXJuaW5nLWJnOiAjZjBhZDRlO1xuJHRvYXN0LWVycm9yLWJnOiAjZDk1MzRmO1xuJHRvYXN0LXN1Y2Nlc3MtYmc6ICM1Y2I4NWM7XG4kcHJvZ3Jlc3NiYXItYmc6ICNkZGRkZGQ7XG4kcHJvZ3Jlc3NiYXItcmFuZ2UtYmc6ICM0NTc5ODc7XG4kdG9vbHRpcC1jb2xvcjogIzAwMDAwMGRlO1xuJHRvb2x0aXAtYmc6ICNmZmZmZmY7XG4kdG9vbHRpcC1ib3JkZXItY29sb3I6ICNlMGUwZTA7XG4kc2xpZGVyLWJhci1iZzogI2UwZTBlMDtcbiRzbGlkZXItcmFuZ2UtYmc6ICM0NTc5ODc7XG4kZ2FsbGVyeS1pbmRpY2F0b3ItYmc6ICNmZmZmZmY7XG4kZ2FsbGVyeS1pbmRpY2F0b3ItaXRlbS1zZWxlY3RlZC1iZzogIzQ1Nzk4NztcbiRnYWxsZXJ5LWluZGljYXRvci1mb2N1c2VkLWJnOiAjMmI0YzU0O1xuJGdhbGxlcnktbmF2YnV0dG9uLWhvdmVyLWNvbG9yOiAjNDU3OTg3ODA7XG4kZ2FsbGVyeS1uYXYtYXJyb3ctY29sb3I6ICNmZmZmZmY7XG4kZ2FsbGVyeS1uYXZidXR0b24tYWN0aXZlLWNvbG9yOiAjNDU3OTg3YjM7XG4kbG9hZHBhbmVsLWNvbnRlbnQtYmc6ICNmZmZmZmY7XG4kdGFnYm94LXRhZy1jb2xvcjogIzAwMDAwMGRlO1xuJHRhZ2JveC10YWctYmc6ICNkZGRkZGQ7XG4kdGFnYm94LXRhZy1idXR0b24tcmVtb3ZlLWJnOiAjYWFhYWFhO1xuJHJhZGlvZ3JvdXAtY2hlY2tlZC1iZzogIzQ1Nzk4NztcbiRyYWRpb2J1dHRvbi1hY3RpdmUtYmc6ICM2MDYwNjAzMztcbiRhY2NvcmRpb24tY29sb3I6ICMwMDAwMDBkZTtcbiRhY2NvcmRpb24tdGl0bGUtY29sb3I6ICMwMDAwMDBkZTtcbiRhY2NvcmRpb24taXRlbS10aXRsZS1vcGVuZWQtYmc6IHRyYW5zcGFyZW50O1xuJGFjY29yZGlvbi1pdGVtLWJvcmRlci1jb2xvcjogI2UwZTBlMDtcbiRhY2NvcmRpb24taXRlbS1mb2N1c2VkLWJvcmRlci1jb2xvcjogIzQ1Nzk4NztcbiRhY2NvcmRpb24tdGl0bGUtYWN0aXZlLWNvbG9yOiAjMDAwMDAwZGU7XG4kYWNjb3JkaW9uLWl0ZW0taG92ZXItYmc6ICMwMDAwMDAwYTtcbiRhY2NvcmRpb24taWNvbi1hY3RpdmUtY29sb3I6ICMwMDAwMDBkZTtcbiRjb2xvcmJveC1vdmVybGF5LWJnOiAjZmZmZmZmO1xuJGRhdGFncmlkLWJhc2UtY29sb3I6ICMwMDAwMDBkZTtcbiRkYXRhZ3JpZC1iYXNlLWJhY2tncm91bmQtY29sb3I6ICNmZmZmZmY7XG4kZGF0YWdyaWQtYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJGRhdGFncmlkLWNvbHVtbmNob29zZXItaXRlbS1jb2xvcjogIzYyNjI2MmRlO1xuJGRhdGFncmlkLWNvbHVtbmNob29zZXItZm9udC13ZWlnaHQ6IG5vcm1hbDtcbiRkYXRhZ3JpZC1kcmFnLWhlYWRlci1ib3JkZXItY29sb3I6ICM0NTc5ODc4MDtcbiRkYXRhZ3JpZC1zZWxlY3Rpb24tYmc6ICNlNmU2ZTY7XG4kZGF0YWdyaWQtcm93LXNlbGVjdGVkLWJvcmRlci1jb2xvcjogI2UwZTBlMDtcbiRkYXRhZ3JpZC1yb3ctc2VsZWN0ZWQtY29sb3I6ICMwMDAwMDBkZTtcbiRkYXRhZ3JpZC1yb3ctZm9jdXNlZC1jb2xvcjogI2ZmZmZmZjtcbiRkYXRhZ3JpZC1yb3ctZm9jdXNlZC1iZzogIzZhOTQ5ZjtcbiRkYXRhZ3JpZC1ob3Zlci1iZzogIzAwMDAwMDBhO1xuJGRhdGFncmlkLW1lbnUtaWNvbi1jb2xvcjogIzU2NTY1NmRlO1xuJGRhdGFncmlkLWNlbGwtbW9kaWZpZWQtYm9yZGVyLWNvbG9yOiAjNWNiODVjODA7XG4kZGF0YWdyaWQtcm93LWludmFsaWQtZmFkZWQtYm9yZGVyLWNvbG9yOiAjZDk1MzRmNjY7XG4kZGF0YWdyaWQtbm9kYXRhLWNvbG9yOiAjNjY2NjY2ZGU7XG4kZGF0YWdyaWQtZ3JvdXAtcm93LWNvbG9yOiAjNjI2MjYyZGU7XG4kZGF0YWdyaWQtZ3JvdXAtcm93LWJnOiAjZjdmN2Y3O1xuJGRhdGFncmlkLXNlYXJjaC1jb2xvcjogI2ZmZmZmZjtcbiRkYXRhZ3JpZC1zcGluLWljb24tY29sb3I6ICM2MjYyNjJkZTtcbiRkYXRhZ3JpZC1zZWFyY2gtYmc6ICM0NTc5ODc7XG4kZGF0YWdyaWQtcm93LWVycm9yLWNvbG9yOiAjZmZmZmZmO1xuJGRhdGFncmlkLXJvdy1lcnJvci1iZzogI2U4OTg5NTtcbiRkYXRhZ3JpZC1saW5rLWNvbG9yOiAjNDU3OTg3O1xuJGRhdGFncmlkLWZvY3VzZWQtYm9yZGVyLWNvbG9yOiAjNmE5NDlmO1xuJGRhdGFncmlkLWVkaXRvci1iZzogI2ZmZmZmZjtcbiRkYXRhZ3JpZC1jb2x1bW5jaG9vc2VyLWJnOiAjZmZmZmZmO1xuJGRhdGFncmlkLXJvdy1hbHRlcm5hdGlvbi1iZzogI2Y1ZjVmNTtcbiRkYXRhZ3JpZC1zdW1tYXJ5LWNvbG9yOiAjMDAwMDAwYjM7XG4kcGFnZXItcGFnZS1zZWxlY3RlZC1jb2xvcjogIzAwMDAwMGRlO1xuJHBhZ2VyLXBhZ2Utc2VsZWN0ZWQtYmc6ICNkNGQ0ZDQ7XG4kcGl2b3RncmlkLWFyZWEtY29sb3I6ICM2MjYyNjJkZTtcbiRwaXZvdGdyaWQtdG90YWxjb2xvcjogI2RkZGRkZDMzO1xuJHBpdm90Z3JpZC1ncmFuZHRvdGFsY29sb3I6ICNmNWY1ZjU7XG4kcGl2b3RncmlkLWZpZWxkLWFyZWEtdGV4dC1jb2xvcjogIzFmMWYxZmRlO1xuJHBpdm90Z3JpZC1zcGluLWljb24tY29sb3I6ICM2MjYyNjJkZTtcbiRmaWxldXBsb2FkZXItZmlsZW5hbWUtY29sb3I6ICMwMDAwMDBkZTtcbiRmaWxldXBsb2FkZXItZmFsZW5hbWUtc3RhdHVzLWNvbG9yOiAjNjY2NjY2ZGU7XG4kZmlsZXVwbG9hZGVyLWJvcmRlci1jb2xvcjogI2UwZTBlMDtcbiRzY2hlZHVsZXItYmFzZS1ib3JkZXItY29sb3I6ICNlMGUwZTA5OTtcbiRzY2hlZHVsZXItYWNjZW50LWJvcmRlci1jb2xvcjogI2M3YzdjNztcbiRzY2hlZHVsZXItYXBwb2ludG1lbnQtdGV4dC1jb2xvcjogI2ZmZmZmZjtcbiRzY2hlZHVsZXItYXBwb2ludG1lbnQtYmFzZS1jb2xvcjogIzQ1Nzk4NztcbiRzY2hlZHVsZXItYXBwb2ludG1lbnQtc3RhcnQtY29sb3I6ICMwMDAwMDA0ZDtcbiRzY2hlZHVsZXItZmlyc3QtbW9udGgtYm9yZGVyLWNvbG9yOiAjYWRhZGFkO1xuJHNjaGVkdWxlci13b3Jrc3BhY2UtZm9jdXNlZC1jZWxsLWNvbG9yOiAjY2NlMGU1O1xuJHNjaGVkdWxlci1jdXJyZW50LXRpbWUtY2VsbC1jb2xvcjogIzQ1Nzk4NztcbiRzY2hlZHVsZXItdGltZS1pbmRpY2F0b3ItY29sb3I6ICM2N2EyYjE7XG4kZm9ybS1maWVsZC1pdGVtLWNvbG9yOiAjMDAwMDAwZGU7XG4kZm9ybS1ncm91cC1ib3JkZXItY29sb3I6ICNlMGUwZTA7XG4kZmlsdGVyYnVpbGRlci10ZXh0LWNvbG9yOiAjMDAwMDAwZGU7XG4kZmlsdGVyYnVpbGRlci10ZXh0LWZvY3VzLWNvbG9yOiAjZmZmZmZmO1xuJGZpbHRlcmJ1aWxkZXItcGx1cy1pY29uLWNvbG9yOiAjNWNiODVjNGQ7XG4kZmlsdGVyYnVpbGRlci1yZW1vdmUtaWNvbi1jb2xvcjogI2Q5NTM0ZjRkO1xuJGZpbHRlcmJ1aWxkZXItZ3JvdXAtb3BlcmF0aW9uLWNvbG9yOiAjZDk1MzRmNGQ7XG4kZmlsdGVyYnVpbGRlci1pdGVtLWZpZWxkLWNvbG9yOiAjNDU3OTg3NGQ7XG4kZmlsdGVyYnVpbGRlci1pdGVtLW9wZXJhdG9yLWNvbG9yOiAjNWNiODVjNGQ7XG4kZmlsdGVyYnVpbGRlci1pdGVtLXZhbHVlLWNvbG9yOiAjZTBlMGUwODA7XG4kZmlsdGVyYnVpbGRlci1tZW51LWljb24tY29sb3I6ICM1NjU2NTZkZTtcbiRkcmF3ZXItc2hhZGVyLWJhY2tncm91bmQtY29sb3I6ICMwMDAwMDA4MDtcbiJdLCJzb3VyY2VSb290IjoiIn0= */"]
});
class HeaderModule {}
HeaderModule.ɵfac = function HeaderModule_Factory(t) {
  return new (t || HeaderModule)();
};
HeaderModule.ɵmod = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdefineNgModule"]({
  type: HeaderModule
});
HeaderModule.ɵinj = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdefineInjector"]({
  imports: [_angular_common__WEBPACK_IMPORTED_MODULE_5__.CommonModule, devextreme_angular_ui_button__WEBPACK_IMPORTED_MODULE_9__.DxButtonModule, _user_panel_user_panel_component__WEBPACK_IMPORTED_MODULE_1__.UserPanelModule, devextreme_angular_ui_toolbar__WEBPACK_IMPORTED_MODULE_7__.DxToolbarModule]
});
(function () {
  (typeof ngJitMode === "undefined" || ngJitMode) && _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵsetNgModuleScope"](HeaderModule, {
    declarations: [HeaderComponent],
    imports: [_angular_common__WEBPACK_IMPORTED_MODULE_5__.CommonModule, devextreme_angular_ui_button__WEBPACK_IMPORTED_MODULE_9__.DxButtonModule, _user_panel_user_panel_component__WEBPACK_IMPORTED_MODULE_1__.UserPanelModule, devextreme_angular_ui_toolbar__WEBPACK_IMPORTED_MODULE_7__.DxToolbarModule],
    exports: [HeaderComponent]
  });
})();

/***/ }),

/***/ 94974:
/*!********************************************!*\
  !*** ./src/app/shared/components/index.ts ***!
  \********************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   ChangePasswordFormComponent: () => (/* reexport safe */ _change_password_form_change_password_form_component__WEBPACK_IMPORTED_MODULE_5__.ChangePasswordFormComponent),
/* harmony export */   ChangePasswordFormModule: () => (/* reexport safe */ _change_password_form_change_password_form_component__WEBPACK_IMPORTED_MODULE_5__.ChangePasswordFormModule),
/* harmony export */   CreateAccountFormComponent: () => (/* reexport safe */ _create_account_form_create_account_form_component__WEBPACK_IMPORTED_MODULE_4__.CreateAccountFormComponent),
/* harmony export */   CreateAccountFormModule: () => (/* reexport safe */ _create_account_form_create_account_form_component__WEBPACK_IMPORTED_MODULE_4__.CreateAccountFormModule),
/* harmony export */   FooterComponent: () => (/* reexport safe */ _footer_footer_component__WEBPACK_IMPORTED_MODULE_0__.FooterComponent),
/* harmony export */   FooterModule: () => (/* reexport safe */ _footer_footer_component__WEBPACK_IMPORTED_MODULE_0__.FooterModule),
/* harmony export */   HeaderComponent: () => (/* reexport safe */ _header_header_component__WEBPACK_IMPORTED_MODULE_1__.HeaderComponent),
/* harmony export */   HeaderModule: () => (/* reexport safe */ _header_header_component__WEBPACK_IMPORTED_MODULE_1__.HeaderModule),
/* harmony export */   LoginFormComponent: () => (/* reexport safe */ _login_form_login_form_component__WEBPACK_IMPORTED_MODULE_2__.LoginFormComponent),
/* harmony export */   LoginFormModule: () => (/* reexport safe */ _login_form_login_form_component__WEBPACK_IMPORTED_MODULE_2__.LoginFormModule),
/* harmony export */   ResetPasswordFormComponent: () => (/* reexport safe */ _reset_password_form_reset_password_form_component__WEBPACK_IMPORTED_MODULE_3__.ResetPasswordFormComponent),
/* harmony export */   ResetPasswordFormModule: () => (/* reexport safe */ _reset_password_form_reset_password_form_component__WEBPACK_IMPORTED_MODULE_3__.ResetPasswordFormModule),
/* harmony export */   SideNavigationMenuComponent: () => (/* reexport safe */ _side_navigation_menu_side_navigation_menu_component__WEBPACK_IMPORTED_MODULE_6__.SideNavigationMenuComponent),
/* harmony export */   SideNavigationMenuModule: () => (/* reexport safe */ _side_navigation_menu_side_navigation_menu_component__WEBPACK_IMPORTED_MODULE_6__.SideNavigationMenuModule),
/* harmony export */   UserPanelComponent: () => (/* reexport safe */ _user_panel_user_panel_component__WEBPACK_IMPORTED_MODULE_7__.UserPanelComponent),
/* harmony export */   UserPanelModule: () => (/* reexport safe */ _user_panel_user_panel_component__WEBPACK_IMPORTED_MODULE_7__.UserPanelModule)
/* harmony export */ });
/* harmony import */ var _footer_footer_component__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./footer/footer.component */ 68014);
/* harmony import */ var _header_header_component__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./header/header.component */ 10074);
/* harmony import */ var _login_form_login_form_component__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ./login-form/login-form.component */ 41918);
/* harmony import */ var _reset_password_form_reset_password_form_component__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ./reset-password-form/reset-password-form.component */ 35225);
/* harmony import */ var _create_account_form_create_account_form_component__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! ./create-account-form/create-account-form.component */ 18270);
/* harmony import */ var _change_password_form_change_password_form_component__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! ./change-password-form/change-password-form.component */ 68081);
/* harmony import */ var _side_navigation_menu_side_navigation_menu_component__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! ./side-navigation-menu/side-navigation-menu.component */ 5994);
/* harmony import */ var _user_panel_user_panel_component__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(/*! ./user-panel/user-panel.component */ 57387);









/***/ }),

/***/ 41918:
/*!**********************************************************************!*\
  !*** ./src/app/shared/components/login-form/login-form.component.ts ***!
  \**********************************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   LoginFormComponent: () => (/* binding */ LoginFormComponent),
/* harmony export */   LoginFormModule: () => (/* binding */ LoginFormModule)
/* harmony export */ });
/* harmony import */ var C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./node_modules/@babel/runtime/helpers/esm/asyncToGenerator.js */ 71670);
/* harmony import */ var _angular_common__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! @angular/common */ 26575);
/* harmony import */ var _angular_router__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! @angular/router */ 27947);
/* harmony import */ var devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! devextreme-angular/ui/form */ 93739);
/* harmony import */ var devextreme_angular_ui_load_indicator__WEBPACK_IMPORTED_MODULE_9__ = __webpack_require__(/*! devextreme-angular/ui/load-indicator */ 2040);
/* harmony import */ var devextreme_ui_notify__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! devextreme/ui/notify */ 72599);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var _services__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ../../services */ 57175);
/* harmony import */ var devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(/*! devextreme-angular/ui/nested */ 71723);
/* harmony import */ var devextreme_angular_core__WEBPACK_IMPORTED_MODULE_8__ = __webpack_require__(/*! devextreme-angular/core */ 65787);














function LoginFormComponent_ng_container_14_ng_container_3_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementContainerStart"](0);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](1, "dx-load-indicator", 15);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementContainerEnd"]();
  }
  if (rf & 2) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("visible", true);
  }
}
function LoginFormComponent_ng_container_14_ng_template_4_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](0, "span", 16);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtext"](1, "Login");
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]();
  }
}
function LoginFormComponent_ng_container_14_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementContainerStart"](0);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](1, "div")(2, "span", 12);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplate"](3, LoginFormComponent_ng_container_14_ng_container_3_Template, 2, 1, "ng-container", 13);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplate"](4, LoginFormComponent_ng_container_14_ng_template_4_Template, 2, 0, "ng-template", null, 14, _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplateRefExtractor"]);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]()();
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementContainerEnd"]();
  }
  if (rf & 2) {
    const _r3 = _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵreference"](5);
    const ctx_r0 = _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵnextContext"]();
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](3);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("ngIf", ctx_r0.loading)("ngIfElse", _r3);
  }
}
const _c0 = function () {
  return {
    stylingMode: "filled",
    placeholder: "User"
  };
};
const _c1 = function () {
  return {
    stylingMode: "filled",
    placeholder: "Password",
    mode: "password"
  };
};
const _c2 = function () {
  return {
    class: "form-text"
  };
};
const _c3 = function (a1) {
  return {
    text: "Remember me",
    elementAttr: a1
  };
};
class LoginFormComponent {
  constructor(authService, router) {
    this.authService = authService;
    this.router = router;
    this.loading = false;
    this.formData = {};
    this.onCreateAccountClick = () => {
      this.router.navigate(['/create-account']);
    };
  }
  onSubmit(e) {
    var _this = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      e.preventDefault();
      const {
        user,
        password
      } = _this.formData;
      _this.loading = true;
      const result = yield _this.authService.logIn(user, password);
      if (!result.isOk) {
        _this.loading = false;
        (0,devextreme_ui_notify__WEBPACK_IMPORTED_MODULE_1__["default"])(result.message, 'error', 2000);
      }
    })();
  }
}
LoginFormComponent.ɵfac = function LoginFormComponent_Factory(t) {
  return new (t || LoginFormComponent)(_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdirectiveInject"](_services__WEBPACK_IMPORTED_MODULE_2__.AuthService), _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdirectiveInject"](_angular_router__WEBPACK_IMPORTED_MODULE_4__.Router));
};
LoginFormComponent.ɵcmp = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdefineComponent"]({
  type: LoginFormComponent,
  selectors: [["app-login-form"]],
  decls: 15,
  vars: 16,
  consts: [[1, "login-form", 3, "submit"], [3, "formData", "disabled"], ["src", "/assets/images/Logo1.png", 1, "center_img"], ["dataField", "user", "editorType", "dxTextBox", 3, "editorOptions"], ["type", "required", "message", "User is required"], [3, "visible"], ["dataField", "password", "editorType", "dxTextBox", 3, "editorOptions"], ["type", "required", "message", "Password is required"], ["dataField", "rememberMe", "editorType", "dxCheckBox", 3, "editorOptions"], ["itemType", "button"], ["width", "100%", "type", "default", 3, "useSubmitBehavior", "template"], [4, "dxTemplate", "dxTemplateOf"], [1, "dx-button-text"], [4, "ngIf", "ngIfElse"], ["notLoading", ""], ["width", "24px", "height", "24px", 3, "visible"], [2, "color", "white"]],
  template: function LoginFormComponent_Template(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](0, "form", 0);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵlistener"]("submit", function LoginFormComponent_Template_form_submit_0_listener($event) {
        return ctx.onSubmit($event);
      });
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](1, "dx-form", 1)(2, "dxi-item");
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](3, "img", 2);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](4, "dxi-item", 3);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](5, "dxi-validation-rule", 4)(6, "dxo-label", 5);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](7, "dxi-item", 6);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](8, "dxi-validation-rule", 7)(9, "dxo-label", 5);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](10, "dxi-item", 8);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](11, "dxo-label", 5);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](12, "dxi-item", 9);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](13, "dxo-button-options", 10);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplate"](14, LoginFormComponent_ng_container_14_Template, 6, 2, "ng-container", 11);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]()();
    }
    if (rf & 2) {
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("formData", ctx.formData)("disabled", ctx.loading);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](3);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("editorOptions", _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵpureFunction0"](11, _c0));
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("visible", false);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("editorOptions", _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵpureFunction0"](12, _c1));
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("visible", false);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("editorOptions", _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵpureFunction1"](14, _c3, _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵpureFunction0"](13, _c2)));
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("visible", false);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("useSubmitBehavior", true)("template", "signInTemplate");
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("dxTemplateOf", "signInTemplate");
    }
  },
  dependencies: [_angular_common__WEBPACK_IMPORTED_MODULE_5__.NgIf, devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_6__.DxFormComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__.DxiItemComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__.DxoLabelComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__.DxiValidationRuleComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__.DxoButtonOptionsComponent, devextreme_angular_core__WEBPACK_IMPORTED_MODULE_8__.DxTemplateDirective, devextreme_angular_ui_load_indicator__WEBPACK_IMPORTED_MODULE_9__.DxLoadIndicatorComponent],
  styles: [".center_img[_ngcontent-%COMP%] {\n  display: block;\n  margin-left: auto;\n  margin-right: auto;\n  width: 100%;\n}\n\n.login-form[_ngcontent-%COMP%]   .link[_ngcontent-%COMP%] {\n  text-align: center;\n  font-size: 16px;\n  font-style: normal;\n}\n.login-form[_ngcontent-%COMP%]   .link[_ngcontent-%COMP%]   a[_ngcontent-%COMP%] {\n  text-decoration: none;\n}\n.login-form[_ngcontent-%COMP%]     .form-text {\n  margin: 10px 0;\n  color: rgba(255, 255, 255, 0.609);\n}\n/*# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbIndlYnBhY2s6Ly8uL3NyYy9hcHAvc2hhcmVkL2NvbXBvbmVudHMvbG9naW4tZm9ybS9sb2dpbi1mb3JtLmNvbXBvbmVudC5zY3NzIl0sIm5hbWVzIjpbXSwibWFwcGluZ3MiOiJBQUdBO0VBQ0UsY0FBQTtFQUNBLGlCQUFBO0VBQ0Esa0JBQUE7RUFDQSxXQUFBO0FBRkY7O0FBTUU7RUFDRSxrQkFBQTtFQUNBLGVBQUE7RUFDQSxrQkFBQTtBQUhKO0FBS0k7RUFDRSxxQkFBQTtBQUhOO0FBT0U7RUFDRSxjQUFBO0VBQ0EsaUNBQUE7QUFMSiIsInNvdXJjZXNDb250ZW50IjpbIkBpbXBvcnQgXCIuLi8uLi8uLi8uLi90aGVtZXMvZ2VuZXJhdGVkL3ZhcmlhYmxlcy5iYXNlLmRhcmsuc2Nzc1wiO1xuXG5cbi5jZW50ZXJfaW1nIHtcbiAgZGlzcGxheTogYmxvY2s7XG4gIG1hcmdpbi1sZWZ0OiBhdXRvO1xuICBtYXJnaW4tcmlnaHQ6IGF1dG87XG4gIHdpZHRoOiAxMDAlO1xufVxuXG4ubG9naW4tZm9ybSB7XG4gIC5saW5rIHtcbiAgICB0ZXh0LWFsaWduOiBjZW50ZXI7XG4gICAgZm9udC1zaXplOiAxNnB4O1xuICAgIGZvbnQtc3R5bGU6IG5vcm1hbDtcblxuICAgIGEge1xuICAgICAgdGV4dC1kZWNvcmF0aW9uOiBub25lO1xuICAgIH1cbiAgfVxuXG4gIDo6bmctZGVlcCAuZm9ybS10ZXh0IHtcbiAgICBtYXJnaW46IDEwcHggMDtcbiAgICBjb2xvcjogcmdiYSgkYmFzZS10ZXh0LWNvbG9yLCBhbHBoYSgkYmFzZS10ZXh0LWNvbG9yKSAqIDAuNyk7XG4gIH1cbn1cbiJdLCJzb3VyY2VSb290IjoiIn0= */"]
});
class LoginFormModule {}
LoginFormModule.ɵfac = function LoginFormModule_Factory(t) {
  return new (t || LoginFormModule)();
};
LoginFormModule.ɵmod = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdefineNgModule"]({
  type: LoginFormModule
});
LoginFormModule.ɵinj = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdefineInjector"]({
  imports: [_angular_common__WEBPACK_IMPORTED_MODULE_5__.CommonModule, _angular_router__WEBPACK_IMPORTED_MODULE_4__.RouterModule, devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_6__.DxFormModule, devextreme_angular_ui_load_indicator__WEBPACK_IMPORTED_MODULE_9__.DxLoadIndicatorModule]
});
(function () {
  (typeof ngJitMode === "undefined" || ngJitMode) && _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵsetNgModuleScope"](LoginFormModule, {
    declarations: [LoginFormComponent],
    imports: [_angular_common__WEBPACK_IMPORTED_MODULE_5__.CommonModule, _angular_router__WEBPACK_IMPORTED_MODULE_4__.RouterModule, devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_6__.DxFormModule, devextreme_angular_ui_load_indicator__WEBPACK_IMPORTED_MODULE_9__.DxLoadIndicatorModule],
    exports: [LoginFormComponent]
  });
})();

/***/ }),

/***/ 35225:
/*!****************************************************************************************!*\
  !*** ./src/app/shared/components/reset-password-form/reset-password-form.component.ts ***!
  \****************************************************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   ResetPasswordFormComponent: () => (/* binding */ ResetPasswordFormComponent),
/* harmony export */   ResetPasswordFormModule: () => (/* binding */ ResetPasswordFormModule)
/* harmony export */ });
/* harmony import */ var C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./node_modules/@babel/runtime/helpers/esm/asyncToGenerator.js */ 71670);
/* harmony import */ var _angular_common__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! @angular/common */ 26575);
/* harmony import */ var _angular_router__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! @angular/router */ 27947);
/* harmony import */ var devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! devextreme-angular/ui/form */ 93739);
/* harmony import */ var devextreme_angular_ui_load_indicator__WEBPACK_IMPORTED_MODULE_9__ = __webpack_require__(/*! devextreme-angular/ui/load-indicator */ 2040);
/* harmony import */ var devextreme_ui_notify__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! devextreme/ui/notify */ 72599);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var _services__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ../../services */ 57175);
/* harmony import */ var devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(/*! devextreme-angular/ui/nested */ 71723);
/* harmony import */ var devextreme_angular_core__WEBPACK_IMPORTED_MODULE_8__ = __webpack_require__(/*! devextreme-angular/core */ 65787);














function ResetPasswordFormComponent_ng_container_13_ng_container_3_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementContainerStart"](0);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](1, "dx-load-indicator", 14);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementContainerEnd"]();
  }
  if (rf & 2) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("visible", true);
  }
}
function ResetPasswordFormComponent_ng_container_13_ng_template_4_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtext"](0, "Reset my password");
  }
}
function ResetPasswordFormComponent_ng_container_13_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementContainerStart"](0);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](1, "div")(2, "span", 11);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplate"](3, ResetPasswordFormComponent_ng_container_13_ng_container_3_Template, 2, 1, "ng-container", 12);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplate"](4, ResetPasswordFormComponent_ng_container_13_ng_template_4_Template, 1, 0, "ng-template", null, 13, _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplateRefExtractor"]);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]()();
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtext"](6, "> ");
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementContainerEnd"]();
  }
  if (rf & 2) {
    const _r3 = _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵreference"](5);
    const ctx_r0 = _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵnextContext"]();
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](3);
    _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("ngIf", ctx_r0.loading)("ngIfElse", _r3);
  }
}
const _c0 = function () {
  return {
    stylingMode: "filled",
    placeholder: "Email",
    mode: "email"
  };
};
const _c1 = function () {
  return {
    class: "submit-button"
  };
};
const notificationText = 'We\'ve sent a link to reset your password. Check your inbox.';
class ResetPasswordFormComponent {
  constructor(authService, router) {
    this.authService = authService;
    this.router = router;
    this.loading = false;
    this.formData = {};
  }
  onSubmit(e) {
    var _this = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      e.preventDefault();
      const {
        email
      } = _this.formData;
      _this.loading = true;
      const result = yield _this.authService.resetPassword(email);
      _this.loading = false;
      if (result.isOk) {
        _this.router.navigate(['/login-form']);
        (0,devextreme_ui_notify__WEBPACK_IMPORTED_MODULE_1__["default"])(notificationText, 'success', 2500);
      } else {
        (0,devextreme_ui_notify__WEBPACK_IMPORTED_MODULE_1__["default"])(result.message, 'error', 2000);
      }
    })();
  }
}
ResetPasswordFormComponent.ɵfac = function ResetPasswordFormComponent_Factory(t) {
  return new (t || ResetPasswordFormComponent)(_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdirectiveInject"](_services__WEBPACK_IMPORTED_MODULE_2__.AuthService), _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdirectiveInject"](_angular_router__WEBPACK_IMPORTED_MODULE_4__.Router));
};
ResetPasswordFormComponent.ɵcmp = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdefineComponent"]({
  type: ResetPasswordFormComponent,
  selectors: [["app-reset-password-form"]],
  decls: 14,
  vars: 10,
  consts: [[1, "reset-password-form", 3, "submit"], [3, "formData", "disabled"], ["dataField", "email", "editorType", "dxTextBox", 3, "editorOptions"], ["type", "required", "message", "Email is required"], ["type", "email", "message", "Email is invalid"], [3, "visible"], ["itemType", "button"], ["width", "100%", "type", "default", 3, "useSubmitBehavior", "template", "elementAttr"], [1, "login-link"], ["routerLink", "/login"], [4, "dxTemplate", "dxTemplateOf"], [1, "dx-button-text"], [4, "ngIf", "ngIfElse"], ["notLoading", ""], ["width", "24px", "height", "24px", 3, "visible"]],
  template: function ResetPasswordFormComponent_Template(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](0, "form", 0);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵlistener"]("submit", function ResetPasswordFormComponent_Template_form_submit_0_listener($event) {
        return ctx.onSubmit($event);
      });
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](1, "dx-form", 1)(2, "dxi-item", 2);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](3, "dxi-validation-rule", 3)(4, "dxi-validation-rule", 4)(5, "dxo-label", 5);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](6, "dxi-item", 6);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelement"](7, "dxo-button-options", 7);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](8, "dxi-item")(9, "div", 8);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtext"](10, " Return to ");
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementStart"](11, "a", 9);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtext"](12, "Sign In");
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]()()();
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵtemplate"](13, ResetPasswordFormComponent_ng_container_13_Template, 7, 2, "ng-container", 10);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵelementEnd"]()();
    }
    if (rf & 2) {
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("formData", ctx.formData)("disabled", ctx.loading);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("editorOptions", _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵpureFunction0"](8, _c0));
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](3);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("visible", false);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("useSubmitBehavior", true)("template", "resetPasswordTemplate")("elementAttr", _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵpureFunction0"](9, _c1));
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵadvance"](6);
      _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵproperty"]("dxTemplateOf", "resetPasswordTemplate");
    }
  },
  dependencies: [_angular_common__WEBPACK_IMPORTED_MODULE_5__.NgIf, _angular_router__WEBPACK_IMPORTED_MODULE_4__.RouterLink, devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_6__.DxFormComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__.DxiItemComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__.DxoLabelComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__.DxiValidationRuleComponent, devextreme_angular_ui_nested__WEBPACK_IMPORTED_MODULE_7__.DxoButtonOptionsComponent, devextreme_angular_core__WEBPACK_IMPORTED_MODULE_8__.DxTemplateDirective, devextreme_angular_ui_load_indicator__WEBPACK_IMPORTED_MODULE_9__.DxLoadIndicatorComponent],
  styles: [".reset-password-form[_ngcontent-%COMP%]     .submit-button {\n  margin-top: 10px;\n}\n.reset-password-form[_ngcontent-%COMP%]   .login-link[_ngcontent-%COMP%] {\n  color: #457987;\n  font-size: 16px;\n  text-align: center;\n}\n/*# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbIndlYnBhY2s6Ly8uL3NyYy9hcHAvc2hhcmVkL2NvbXBvbmVudHMvcmVzZXQtcGFzc3dvcmQtZm9ybS9yZXNldC1wYXNzd29yZC1mb3JtLmNvbXBvbmVudC5zY3NzIiwid2VicGFjazovLy4vc3JjL3RoZW1lcy9nZW5lcmF0ZWQvdmFyaWFibGVzLmJhc2Uuc2NzcyJdLCJuYW1lcyI6W10sIm1hcHBpbmdzIjoiQUFHRTtFQUNFLGdCQUFBO0FBRko7QUFLRTtFQUNFLGNDUFU7RURRVixlQUFBO0VBQ0Esa0JBQUE7QUFISiIsInNvdXJjZXNDb250ZW50IjpbIkBpbXBvcnQgXCIuLi8uLi8uLi8uLi90aGVtZXMvZ2VuZXJhdGVkL3ZhcmlhYmxlcy5iYXNlLnNjc3NcIjtcblxuLnJlc2V0LXBhc3N3b3JkLWZvcm0ge1xuICA6Om5nLWRlZXAgLnN1Ym1pdC1idXR0b24ge1xuICAgIG1hcmdpbi10b3A6IDEwcHg7XG4gIH1cblxuICAubG9naW4tbGluayB7XG4gICAgY29sb3I6ICRiYXNlLWFjY2VudDtcbiAgICBmb250LXNpemU6IDE2cHg7XG4gICAgdGV4dC1hbGlnbjogY2VudGVyO1xuICB9XG59XG4iLCIkYmFzZS1mb250LWZhbWlseTogXCJIZWx2ZXRpY2EgTmV1ZVwiLCBcIlNlZ29lIFVJXCIsIGhlbHZldGljYSwgdmVyZGFuYSwgc2Fucy1zZXJpZjtcbiRiYXNlLWFjY2VudDogIzQ1Nzk4NztcbiRiYXNlLWxhYmVsLWNvbG9yOiAjMDAwMDAwZGU7XG4kYmFzZS1saW5rLWNvbG9yOiAjNDU3OTg3O1xuJGJhc2UtaWNvbi1jb2xvcjogIzAwMDAwMGRlO1xuJGJhc2UtdGV4dC1jb2xvcjogIzAwMDAwMGRlO1xuJGJhc2UtYmc6ICNmZmZmZmY7XG4kYmFzZS1ib3JkZXItY29sb3I6ICNlMGUwZTA7XG4kYmFzZS1ib3JkZXItcmFkaXVzOiA0cHg7XG4kYmFzZS1zdWNjZXNzOiAjNWNiODVjO1xuJGJhc2Utd2FybmluZzogI2YwYWQ0ZTtcbiRiYXNlLWRhbmdlcjogI2Q5NTM0ZjtcbiRiYXNlLWhvdmVyLWNvbG9yOiAjMDAwMDAwZGU7XG4kYmFzZS1ob3Zlci1iZzogIzAwMDAwMDBhO1xuJGJhc2UtZm9jdXMtY29sb3I6ICNmZmZmZmY7XG4kYmFzZS1mb2N1cy1iZzogIzQ1Nzk4NztcbiRiYXNlLWludmFsaWQtY29sb3I6ICNkOTUzNGY7XG4kYmFzZS1pbnZhbGlkLWZhZGVkLWJvcmRlci1jb2xvcjogI2Q5NTM0ZjY2O1xuJGJhc2UtYm9yZGVyLXJhZGl1cy1zbWFsbDogM3B4O1xuJGJhc2UtYm9yZGVyLXJhZGl1cy1sYXJnZTogNnB4O1xuJGJ1dHRvbi1ub3JtYWwtY29sb3I6ICMwMDAwMDBkZTtcbiRidXR0b24tbm9ybWFsLWJnOiAjZmZmZmZmO1xuJGJ1dHRvbi1ub3JtYWwtYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJGJ1dHRvbi1kZWZhdWx0LWNvbG9yOiAjZmZmZmZmO1xuJGJ1dHRvbi1kZWZhdWx0LWJnOiAjNDU3OTg3O1xuJGJ1dHRvbi1kZWZhdWx0LWJvcmRlci1jb2xvcjogIzNjNmE3NjtcbiRidXR0b24tZGFuZ2VyLWNvbG9yOiAjZmZmZmZmO1xuJGJ1dHRvbi1kYW5nZXItYmc6ICNkOTUzNGY7XG4kYnV0dG9uLWRhbmdlci1ib3JkZXItY29sb3I6ICNkNDNmM2E7XG4kYnV0dG9uLXN1Y2Nlc3MtY29sb3I6ICNmZmZmZmY7XG4kYnV0dG9uLXN1Y2Nlc3MtYmc6ICM1Y2I4NWM7XG4kYnV0dG9uLXN1Y2Nlc3MtYm9yZGVyLWNvbG9yOiAjNGNhZTRjO1xuJGxpc3QtYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJGxpc3QtZ3JvdXAtY29sb3I6ICMwMDAwMDBkZTtcbiRsaXN0LWdyb3VwLWhlYWRlci1iZzogI2VlZWVlZTBkO1xuJGxpc3Qtbm9ybWFsLWNvbG9yOiAjMDAwMDAwZGU7XG4kbGlzdC1pdGVtLWhvdmVyLWJnOiAjMDAwMDAwMGE7XG4kbGlzdC1pdGVtLXNlbGVjdGVkLWJnOiAjMDAwMDAwMWE7XG4kbGlzdC1pdGVtLWZvY3VzZWQtc2VsZWN0ZWQtYmc6ICM0NTc5ODdiMztcbiRsaXN0LWZvY3VzZWQtYmc6ICM0NTc5ODc7XG4kbGlzdC1pdGVtLWFjdGl2ZS1iZzogIzQ1Nzk4NztcbiRmaWVsZHNldC1maWVsZC1sYWJlbC1jb2xvcjogIzAwMDAwMGRlO1xuJGNoZWNrYm94LWJvcmRlci1jb2xvcjogI2UwZTBlMDtcbiRjaGVja2JveC1mb2N1c2VkLWJvcmRlcmQtY29sb3I6ICM0NTc5ODc7XG4kY2hlY2tib3gtYmc6ICNmZmZmZmY7XG4kY2hlY2tib3gtaG92ZXItYm9yZGVyLWNvbG9yOiAjMzA1NTVmO1xuJGNoZWNrYm94LWFjdGl2ZS1pY29uLWJnOiAjNjA2MDYwMzM7XG4kYnV0dG9uLWdyb3VwLW5vcm1hbC1zZWxlY3RlZC1jb2xvcjogIzAwMDAwMGRlO1xuJGJ1dHRvbi1ncm91cC1ub3JtYWwtc2VsZWN0ZWQtYmc6ICMwMDAwMDAyYjtcbiRidXR0b24tZ3JvdXAtZGVmYXVsdC1zZWxlY3RlZC1jb2xvcjogIzQ1Nzk4NztcbiRidXR0b24tZ3JvdXAtZGVmYXVsdC1zZWxlY3RlZC1iZzogIzQ1Nzk4NzRkO1xuJGJ1dHRvbi1ncm91cC1kYW5nZXItc2VsZWN0ZWQtY29sb3I6ICNkOTUzNGY7XG4kYnV0dG9uLWdyb3VwLWRhbmdlci1zZWxlY3RlZC1iZzogI2Q5NTM0ZjRkO1xuJGJ1dHRvbi1ncm91cC1zdWNjZXNzLXNlbGVjdGVkLWNvbG9yOiAjNWNiODVjO1xuJGJ1dHRvbi1ncm91cC1zdWNjZXNzLXNlbGVjdGVkLWJnOiAjNWNiODVjNGQ7XG4kc2Nyb2xsYWJsZS1zY3JvbGwtYmc6ICNiZmJmYmZiMztcbiRzY3JvbGxhYmxlLXNjcm9sbGJhci1hY3RpdmUtYmc6ICNiZmJmYmYzMztcbiRzd2l0Y2gtYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJHN3aXRjaC1vbi1jb2xvcjogIzAwMDAwMGRlO1xuJHN3aXRjaC1jb250YWluZXItYWN0aXZlLWJnOiAjNjA2MDYwMzM7XG4kc3dpdGNoLW9mZi1jb2xvcjogIzY2NjY2NmRlO1xuJHRhYnMtdGFiLWNvbG9yOiAjMDAwMDAwZGU7XG4kdGFicy10YWItc2VsZWN0ZWQtY29sb3I6ICMwMDAwMDBkZTtcbiR0YWJzLXRhYi1iZzogI2Y3ZjdmNztcbiR0YWJzLXRhYi1zZWxlY3RlZC1iZzogI2ZmZmZmZjtcbiR0YWJzLWJvcmRlci1jb2xvcjogI2UwZTBlMDtcbiR0YWJzLWZvY3VzZWQtYm9yZGVyLWNvbG9yOiAjNDU3OTg3O1xuJHRhYnMtdGFiLWhvdmVyLWJnOiAjZmZmZmZmO1xuJGJhZGdlLWNvbG9yOiAjZmZmZmZmO1xuJGJhZGdlLWJnOiAjNDU3OTg3O1xuJG92ZXJsYXktY29udGVudC1iZzogI2ZmZmZmZjtcbiRvdmVybGF5LXNoYWRlci1iZzogI2ZmZmZmZmNjO1xuJG92ZXJsYXktZm9jdXMtYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJHRleHRib3gtc2VhcmNoLWljb24tY29sb3I6ICM2NjY2NjZkZTtcbiR0ZXh0ZWRpdG9yLWNvbG9yOiAjMDAwMDAwZGU7XG4kdGV4dGVkaXRvci1wbGFjZWhvbGRlci1jb2xvcjogIzY2NjY2NmRlO1xuJHRleHRlZGl0b3ItYmc6ICNmZmZmZmY7XG4kdGV4dGVkaXRvci1maWxsZWQtYmc6ICMwMDAwMDAwZDtcbiR0ZXh0ZWRpdG9yLWJvcmRlci1jb2xvcjogI2UwZTBlMDtcbiR0ZXh0ZWRpdG9yLWZvY3VzZWQtYm9yZGVyLWNvbG9yOiAjNDU3OTg3O1xuJHRleHRlZGl0b3ItaG92ZXItYm9yZGVyLWNvbG9yOiAjNDU3OTg3NjY7XG4kZHJvcGRvd25lZGl0b3ItYnV0dG9uLWhvdmVyLWJnOiAjZTZlNmU2O1xuJGRyb3Bkb3duZWRpdG9yLWJ1dHRvbi1hY3RpdmUtYmc6ICNiM2IzYjM7XG4kZHJvcGRvd25lZGl0b3ItaWNvbi1jb2xvcjogIzAwMDAwMGRlO1xuJGRyb3Bkb3duZWRpdG9yLWljb24tYWN0aXZlLWNvbG9yOiAjMDAwMDAwZGU7XG4kbnVtYmVyYm94LXNwaW4tYWN0aXZlLWJnOiAjYjNiM2IzO1xuJG51bWJlcmJveC1zcGluLWhvdmVyLWJvcmRlci1jb2xvcjogdHJhbnNwYXJlbnQ7XG4kbnVtYmVyYm94LXNwaW4taWNvbi1jb2xvcjogIzAwMDAwMGRlO1xuJGNhbGVuZGFyLWNvbG9yOiAjMDAwMDAwZGU7XG4kY2FsZW5kYXItaGVhZGVyLWNvbG9yOiAjMDAwMDAwZGU7XG4kY2FsZW5kYXItY2VsbC1vdGhlci1jb2xvcjogIzAwMDAwMDYxO1xuJGNhbGVuZGFyLWJnOiAjZmZmZmZmO1xuJGNhbGVuZGFyLWhvdmVyLWJnOiAjNDU3OTg3M2Q7XG4kY2FsZW5kYXItY2VsbC1zZWxlY3RlZC1iZzogIzQ1Nzk4NztcbiRjYWxlbmRhci1jZWxsLWFjdGl2ZS1iZzogIzQ1Nzk4NzY2O1xuJGxvYWRpbmRpY2F0b3ItYmc6ICMyZDJkMmRkZTtcbiR0cmVldmlldy1mb2N1c2VkLWJnOiAjNDU3OTg3O1xuJHRyZWV2aWV3LWhvdmVyLWJnOiAjMDAwMDAwMGE7XG4kdHJlZXZpZXctYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJHRyZWV2aWV3LWl0ZW0tc2VsZWN0ZWQtY29sb3I6ICMwMDAwMDBkZTtcbiR0cmVldmlldy1zcGluLWljb24tY29sb3I6ICMwMDAwMDBkZTtcbiRtZW51LXBvcHVwLWJnOiAjZmZmZmZmO1xuJG1lbnUtaXRlbS1ob3Zlci1iZzogIzAwMDAwMDBhO1xuJG1lbnUtY29sb3I6ICMwMDAwMDBkZTtcbiRtZW51LWl0ZW0taG92ZXJlZC1jb2xvcjogIzAwMDAwMGRlO1xuJG1lbnUtaXRlbS1leHBhbmRlZC1jb2xvcjogIzAwMDAwMGRlO1xuJG1lbnUtaXRlbS1zZWxlY3RlZC1iZzogI2U2ZTZlNjtcbiRtZW51LXBvcHVwLWJvcmRlci1jb2xvcjogI2UwZTBlMDtcbiRzZWxlY3Rib3gtbGlzdC1iZzogI2ZmZmZmZjtcbiR0b29sYmFyLWJnOiAjZmZmZmZmO1xuJHBvcHVwLXRpdGxlLWJnOiB0cmFuc3BhcmVudDtcbiR0aWxldmlldy1jb2xvcjogIzAwMDAwMGRlO1xuJHRvYXN0LWNvbG9yOiAjZmZmZmZmO1xuJHRvYXN0LWluZm8tYmc6ICM0NTc5ODc7XG4kdG9hc3Qtd2FybmluZy1iZzogI2YwYWQ0ZTtcbiR0b2FzdC1lcnJvci1iZzogI2Q5NTM0ZjtcbiR0b2FzdC1zdWNjZXNzLWJnOiAjNWNiODVjO1xuJHByb2dyZXNzYmFyLWJnOiAjZGRkZGRkO1xuJHByb2dyZXNzYmFyLXJhbmdlLWJnOiAjNDU3OTg3O1xuJHRvb2x0aXAtY29sb3I6ICMwMDAwMDBkZTtcbiR0b29sdGlwLWJnOiAjZmZmZmZmO1xuJHRvb2x0aXAtYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJHNsaWRlci1iYXItYmc6ICNlMGUwZTA7XG4kc2xpZGVyLXJhbmdlLWJnOiAjNDU3OTg3O1xuJGdhbGxlcnktaW5kaWNhdG9yLWJnOiAjZmZmZmZmO1xuJGdhbGxlcnktaW5kaWNhdG9yLWl0ZW0tc2VsZWN0ZWQtYmc6ICM0NTc5ODc7XG4kZ2FsbGVyeS1pbmRpY2F0b3ItZm9jdXNlZC1iZzogIzJiNGM1NDtcbiRnYWxsZXJ5LW5hdmJ1dHRvbi1ob3Zlci1jb2xvcjogIzQ1Nzk4NzgwO1xuJGdhbGxlcnktbmF2LWFycm93LWNvbG9yOiAjZmZmZmZmO1xuJGdhbGxlcnktbmF2YnV0dG9uLWFjdGl2ZS1jb2xvcjogIzQ1Nzk4N2IzO1xuJGxvYWRwYW5lbC1jb250ZW50LWJnOiAjZmZmZmZmO1xuJHRhZ2JveC10YWctY29sb3I6ICMwMDAwMDBkZTtcbiR0YWdib3gtdGFnLWJnOiAjZGRkZGRkO1xuJHRhZ2JveC10YWctYnV0dG9uLXJlbW92ZS1iZzogI2FhYWFhYTtcbiRyYWRpb2dyb3VwLWNoZWNrZWQtYmc6ICM0NTc5ODc7XG4kcmFkaW9idXR0b24tYWN0aXZlLWJnOiAjNjA2MDYwMzM7XG4kYWNjb3JkaW9uLWNvbG9yOiAjMDAwMDAwZGU7XG4kYWNjb3JkaW9uLXRpdGxlLWNvbG9yOiAjMDAwMDAwZGU7XG4kYWNjb3JkaW9uLWl0ZW0tdGl0bGUtb3BlbmVkLWJnOiB0cmFuc3BhcmVudDtcbiRhY2NvcmRpb24taXRlbS1ib3JkZXItY29sb3I6ICNlMGUwZTA7XG4kYWNjb3JkaW9uLWl0ZW0tZm9jdXNlZC1ib3JkZXItY29sb3I6ICM0NTc5ODc7XG4kYWNjb3JkaW9uLXRpdGxlLWFjdGl2ZS1jb2xvcjogIzAwMDAwMGRlO1xuJGFjY29yZGlvbi1pdGVtLWhvdmVyLWJnOiAjMDAwMDAwMGE7XG4kYWNjb3JkaW9uLWljb24tYWN0aXZlLWNvbG9yOiAjMDAwMDAwZGU7XG4kY29sb3Jib3gtb3ZlcmxheS1iZzogI2ZmZmZmZjtcbiRkYXRhZ3JpZC1iYXNlLWNvbG9yOiAjMDAwMDAwZGU7XG4kZGF0YWdyaWQtYmFzZS1iYWNrZ3JvdW5kLWNvbG9yOiAjZmZmZmZmO1xuJGRhdGFncmlkLWJvcmRlci1jb2xvcjogI2UwZTBlMDtcbiRkYXRhZ3JpZC1jb2x1bW5jaG9vc2VyLWl0ZW0tY29sb3I6ICM2MjYyNjJkZTtcbiRkYXRhZ3JpZC1jb2x1bW5jaG9vc2VyLWZvbnQtd2VpZ2h0OiBub3JtYWw7XG4kZGF0YWdyaWQtZHJhZy1oZWFkZXItYm9yZGVyLWNvbG9yOiAjNDU3OTg3ODA7XG4kZGF0YWdyaWQtc2VsZWN0aW9uLWJnOiAjZTZlNmU2O1xuJGRhdGFncmlkLXJvdy1zZWxlY3RlZC1ib3JkZXItY29sb3I6ICNlMGUwZTA7XG4kZGF0YWdyaWQtcm93LXNlbGVjdGVkLWNvbG9yOiAjMDAwMDAwZGU7XG4kZGF0YWdyaWQtcm93LWZvY3VzZWQtY29sb3I6ICNmZmZmZmY7XG4kZGF0YWdyaWQtcm93LWZvY3VzZWQtYmc6ICM2YTk0OWY7XG4kZGF0YWdyaWQtaG92ZXItYmc6ICMwMDAwMDAwYTtcbiRkYXRhZ3JpZC1tZW51LWljb24tY29sb3I6ICM1NjU2NTZkZTtcbiRkYXRhZ3JpZC1jZWxsLW1vZGlmaWVkLWJvcmRlci1jb2xvcjogIzVjYjg1YzgwO1xuJGRhdGFncmlkLXJvdy1pbnZhbGlkLWZhZGVkLWJvcmRlci1jb2xvcjogI2Q5NTM0ZjY2O1xuJGRhdGFncmlkLW5vZGF0YS1jb2xvcjogIzY2NjY2NmRlO1xuJGRhdGFncmlkLWdyb3VwLXJvdy1jb2xvcjogIzYyNjI2MmRlO1xuJGRhdGFncmlkLWdyb3VwLXJvdy1iZzogI2Y3ZjdmNztcbiRkYXRhZ3JpZC1zZWFyY2gtY29sb3I6ICNmZmZmZmY7XG4kZGF0YWdyaWQtc3Bpbi1pY29uLWNvbG9yOiAjNjI2MjYyZGU7XG4kZGF0YWdyaWQtc2VhcmNoLWJnOiAjNDU3OTg3O1xuJGRhdGFncmlkLXJvdy1lcnJvci1jb2xvcjogI2ZmZmZmZjtcbiRkYXRhZ3JpZC1yb3ctZXJyb3ItYmc6ICNlODk4OTU7XG4kZGF0YWdyaWQtbGluay1jb2xvcjogIzQ1Nzk4NztcbiRkYXRhZ3JpZC1mb2N1c2VkLWJvcmRlci1jb2xvcjogIzZhOTQ5ZjtcbiRkYXRhZ3JpZC1lZGl0b3ItYmc6ICNmZmZmZmY7XG4kZGF0YWdyaWQtY29sdW1uY2hvb3Nlci1iZzogI2ZmZmZmZjtcbiRkYXRhZ3JpZC1yb3ctYWx0ZXJuYXRpb24tYmc6ICNmNWY1ZjU7XG4kZGF0YWdyaWQtc3VtbWFyeS1jb2xvcjogIzAwMDAwMGIzO1xuJHBhZ2VyLXBhZ2Utc2VsZWN0ZWQtY29sb3I6ICMwMDAwMDBkZTtcbiRwYWdlci1wYWdlLXNlbGVjdGVkLWJnOiAjZDRkNGQ0O1xuJHBpdm90Z3JpZC1hcmVhLWNvbG9yOiAjNjI2MjYyZGU7XG4kcGl2b3RncmlkLXRvdGFsY29sb3I6ICNkZGRkZGQzMztcbiRwaXZvdGdyaWQtZ3JhbmR0b3RhbGNvbG9yOiAjZjVmNWY1O1xuJHBpdm90Z3JpZC1maWVsZC1hcmVhLXRleHQtY29sb3I6ICMxZjFmMWZkZTtcbiRwaXZvdGdyaWQtc3Bpbi1pY29uLWNvbG9yOiAjNjI2MjYyZGU7XG4kZmlsZXVwbG9hZGVyLWZpbGVuYW1lLWNvbG9yOiAjMDAwMDAwZGU7XG4kZmlsZXVwbG9hZGVyLWZhbGVuYW1lLXN0YXR1cy1jb2xvcjogIzY2NjY2NmRlO1xuJGZpbGV1cGxvYWRlci1ib3JkZXItY29sb3I6ICNlMGUwZTA7XG4kc2NoZWR1bGVyLWJhc2UtYm9yZGVyLWNvbG9yOiAjZTBlMGUwOTk7XG4kc2NoZWR1bGVyLWFjY2VudC1ib3JkZXItY29sb3I6ICNjN2M3Yzc7XG4kc2NoZWR1bGVyLWFwcG9pbnRtZW50LXRleHQtY29sb3I6ICNmZmZmZmY7XG4kc2NoZWR1bGVyLWFwcG9pbnRtZW50LWJhc2UtY29sb3I6ICM0NTc5ODc7XG4kc2NoZWR1bGVyLWFwcG9pbnRtZW50LXN0YXJ0LWNvbG9yOiAjMDAwMDAwNGQ7XG4kc2NoZWR1bGVyLWZpcnN0LW1vbnRoLWJvcmRlci1jb2xvcjogI2FkYWRhZDtcbiRzY2hlZHVsZXItd29ya3NwYWNlLWZvY3VzZWQtY2VsbC1jb2xvcjogI2NjZTBlNTtcbiRzY2hlZHVsZXItY3VycmVudC10aW1lLWNlbGwtY29sb3I6ICM0NTc5ODc7XG4kc2NoZWR1bGVyLXRpbWUtaW5kaWNhdG9yLWNvbG9yOiAjNjdhMmIxO1xuJGZvcm0tZmllbGQtaXRlbS1jb2xvcjogIzAwMDAwMGRlO1xuJGZvcm0tZ3JvdXAtYm9yZGVyLWNvbG9yOiAjZTBlMGUwO1xuJGZpbHRlcmJ1aWxkZXItdGV4dC1jb2xvcjogIzAwMDAwMGRlO1xuJGZpbHRlcmJ1aWxkZXItdGV4dC1mb2N1cy1jb2xvcjogI2ZmZmZmZjtcbiRmaWx0ZXJidWlsZGVyLXBsdXMtaWNvbi1jb2xvcjogIzVjYjg1YzRkO1xuJGZpbHRlcmJ1aWxkZXItcmVtb3ZlLWljb24tY29sb3I6ICNkOTUzNGY0ZDtcbiRmaWx0ZXJidWlsZGVyLWdyb3VwLW9wZXJhdGlvbi1jb2xvcjogI2Q5NTM0ZjRkO1xuJGZpbHRlcmJ1aWxkZXItaXRlbS1maWVsZC1jb2xvcjogIzQ1Nzk4NzRkO1xuJGZpbHRlcmJ1aWxkZXItaXRlbS1vcGVyYXRvci1jb2xvcjogIzVjYjg1YzRkO1xuJGZpbHRlcmJ1aWxkZXItaXRlbS12YWx1ZS1jb2xvcjogI2UwZTBlMDgwO1xuJGZpbHRlcmJ1aWxkZXItbWVudS1pY29uLWNvbG9yOiAjNTY1NjU2ZGU7XG4kZHJhd2VyLXNoYWRlci1iYWNrZ3JvdW5kLWNvbG9yOiAjMDAwMDAwODA7XG4iXSwic291cmNlUm9vdCI6IiJ9 */"]
});
class ResetPasswordFormModule {}
ResetPasswordFormModule.ɵfac = function ResetPasswordFormModule_Factory(t) {
  return new (t || ResetPasswordFormModule)();
};
ResetPasswordFormModule.ɵmod = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdefineNgModule"]({
  type: ResetPasswordFormModule
});
ResetPasswordFormModule.ɵinj = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵdefineInjector"]({
  imports: [_angular_common__WEBPACK_IMPORTED_MODULE_5__.CommonModule, _angular_router__WEBPACK_IMPORTED_MODULE_4__.RouterModule, devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_6__.DxFormModule, devextreme_angular_ui_load_indicator__WEBPACK_IMPORTED_MODULE_9__.DxLoadIndicatorModule]
});
(function () {
  (typeof ngJitMode === "undefined" || ngJitMode) && _angular_core__WEBPACK_IMPORTED_MODULE_3__["ɵɵsetNgModuleScope"](ResetPasswordFormModule, {
    declarations: [ResetPasswordFormComponent],
    imports: [_angular_common__WEBPACK_IMPORTED_MODULE_5__.CommonModule, _angular_router__WEBPACK_IMPORTED_MODULE_4__.RouterModule, devextreme_angular_ui_form__WEBPACK_IMPORTED_MODULE_6__.DxFormModule, devextreme_angular_ui_load_indicator__WEBPACK_IMPORTED_MODULE_9__.DxLoadIndicatorModule],
    exports: [ResetPasswordFormComponent]
  });
})();

/***/ }),

/***/ 5994:
/*!******************************************************************************************!*\
  !*** ./src/app/shared/components/side-navigation-menu/side-navigation-menu.component.ts ***!
  \******************************************************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   SideNavigationMenuComponent: () => (/* binding */ SideNavigationMenuComponent),
/* harmony export */   SideNavigationMenuModule: () => (/* binding */ SideNavigationMenuModule)
/* harmony export */ });
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var devextreme_angular_ui_tree_view__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! devextreme-angular/ui/tree-view */ 14932);
/* harmony import */ var _app_navigation__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../../../app-navigation */ 8739);
/* harmony import */ var devextreme_events__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! devextreme/events */ 14918);






const _c0 = ["*"];
class SideNavigationMenuComponent {
  set selectedItem(value) {
    this._selectedItem = value;
    if (!this.menu.instance) {
      return;
    }
    this.menu.instance.selectItem(value);
  }
  get items() {
    if (!this._items) {
      this._items = _app_navigation__WEBPACK_IMPORTED_MODULE_0__.navigation.map(item => {
        if (item.path && !/^\//.test(item.path)) {
          item.path = `/${item.path}`;
        }
        return {
          ...item,
          expanded: !this._compactMode
        };
      });
    }
    return this._items;
  }
  get compactMode() {
    return this._compactMode;
  }
  set compactMode(val) {
    this._compactMode = val;
    if (!this.menu.instance) {
      return;
    }
    if (val) {
      this.menu.instance.collapseAll();
    } else {
      this.menu.instance.expandItem(this._selectedItem);
    }
  }
  constructor(elementRef) {
    this.elementRef = elementRef;
    this.selectedItemChanged = new _angular_core__WEBPACK_IMPORTED_MODULE_2__.EventEmitter();
    this.openMenu = new _angular_core__WEBPACK_IMPORTED_MODULE_2__.EventEmitter();
    this._compactMode = false;
  }
  onItemClick(event) {
    this.selectedItemChanged.emit(event);
  }
  ngAfterViewInit() {
    devextreme_events__WEBPACK_IMPORTED_MODULE_1__.on(this.elementRef.nativeElement, 'dxclick', e => {
      this.openMenu.next(e);
    });
  }
  ngOnDestroy() {
    devextreme_events__WEBPACK_IMPORTED_MODULE_1__.off(this.elementRef.nativeElement, 'dxclick');
  }
}
SideNavigationMenuComponent.ɵfac = function SideNavigationMenuComponent_Factory(t) {
  return new (t || SideNavigationMenuComponent)(_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵdirectiveInject"](_angular_core__WEBPACK_IMPORTED_MODULE_2__.ElementRef));
};
SideNavigationMenuComponent.ɵcmp = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵdefineComponent"]({
  type: SideNavigationMenuComponent,
  selectors: [["app-side-navigation-menu"]],
  viewQuery: function SideNavigationMenuComponent_Query(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵviewQuery"](devextreme_angular_ui_tree_view__WEBPACK_IMPORTED_MODULE_3__.DxTreeViewComponent, 7);
    }
    if (rf & 2) {
      let _t;
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵqueryRefresh"](_t = _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵloadQuery"]()) && (ctx.menu = _t.first);
    }
  },
  inputs: {
    selectedItem: "selectedItem",
    compactMode: "compactMode"
  },
  outputs: {
    selectedItemChanged: "selectedItemChanged",
    openMenu: "openMenu"
  },
  ngContentSelectors: _c0,
  decls: 3,
  vars: 2,
  consts: [[1, "menu-container"], ["keyExpr", "path", "selectionMode", "single", "expandEvent", "click", "width", "100%", 2, "background-color", "#363640", 3, "items", "focusStateEnabled", "onItemClick"]],
  template: function SideNavigationMenuComponent_Template(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵprojectionDef"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵprojection"](0);
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementStart"](1, "div", 0)(2, "dx-tree-view", 1);
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵlistener"]("onItemClick", function SideNavigationMenuComponent_Template_dx_tree_view_onItemClick_2_listener($event) {
        return ctx.onItemClick($event);
      });
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementEnd"]()();
    }
    if (rf & 2) {
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵadvance"](2);
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵproperty"]("items", ctx.items)("focusStateEnabled", false);
    }
  },
  dependencies: [devextreme_angular_ui_tree_view__WEBPACK_IMPORTED_MODULE_3__.DxTreeViewComponent],
  styles: ["html[_ngcontent-%COMP%], body[_ngcontent-%COMP%] {\n  margin: 0px;\n  min-height: 100%;\n  height: 100%;\n}\n\n*[_ngcontent-%COMP%] {\n  box-sizing: border-box;\n}\n\n.content[_ngcontent-%COMP%] {\n  line-height: 1.5;\n  flex-grow: 1;\n}\n.content[_ngcontent-%COMP%]   h2[_ngcontent-%COMP%] {\n  font-size: 30px;\n  margin-top: 20px;\n  margin-bottom: 20px;\n}\n\n.container[_ngcontent-%COMP%] {\n  height: 100%;\n  flex-direction: column;\n  display: flex;\n}\n\n.layout-body[_ngcontent-%COMP%] {\n  flex: 1;\n  min-height: 0;\n}\n\n.side-nav-outer-toolbar[_ngcontent-%COMP%]   .dx-drawer[_ngcontent-%COMP%] {\n  height: calc(100% - 56px);\n}\n\n.content-block[_ngcontent-%COMP%] {\n  margin-left: 40px;\n  margin-right: 40px;\n  margin-top: 20px;\n}\n.screen-x-small[_ngcontent-%COMP%]   .content-block[_ngcontent-%COMP%] {\n  margin-left: 20px;\n  margin-right: 20px;\n}\n\n.responsive-paddings[_ngcontent-%COMP%] {\n  padding: 20px;\n}\n.screen-large[_ngcontent-%COMP%]   .responsive-paddings[_ngcontent-%COMP%] {\n  padding: 40px;\n}\n\n.dx-card.wide-card[_ngcontent-%COMP%] {\n  border-radius: 0;\n  margin-left: 0;\n  margin-right: 0;\n  border-right: 0;\n  border-left: 0;\n}\n\n.with-footer[_ngcontent-%COMP%]    > .dx-scrollable-wrapper[_ngcontent-%COMP%]    > .dx-scrollable-container[_ngcontent-%COMP%]    > .dx-scrollable-content[_ngcontent-%COMP%] {\n  height: 100%;\n}\n.with-footer[_ngcontent-%COMP%]    > .dx-scrollable-wrapper[_ngcontent-%COMP%]    > .dx-scrollable-container[_ngcontent-%COMP%]    > .dx-scrollable-content[_ngcontent-%COMP%]    > .dx-scrollview-content[_ngcontent-%COMP%] {\n  display: flex;\n  flex-direction: column;\n  min-height: 100%;\n}\n\n[_nghost-%COMP%] {\n  display: flex;\n  flex-direction: column;\n  min-height: 100%;\n  height: 100%;\n  width: 250px !important;\n}\n[_nghost-%COMP%]   .menu-container[_ngcontent-%COMP%] {\n  min-height: 100%;\n  display: flex;\n  flex: 1;\n}\n[_nghost-%COMP%]   .menu-container[_ngcontent-%COMP%]     .dx-treeview {\n  white-space: nowrap;\n}\n[_nghost-%COMP%]   .menu-container[_ngcontent-%COMP%]     .dx-treeview .dx-treeview-item {\n  padding-left: 0;\n  padding-right: 0;\n}\n[_nghost-%COMP%]   .menu-container[_ngcontent-%COMP%]     .dx-treeview .dx-treeview-item .dx-icon {\n  width: 60px !important;\n  margin: 0 !important;\n}\n[_nghost-%COMP%]   .menu-container[_ngcontent-%COMP%]     .dx-treeview .dx-treeview-node {\n  padding: 0 0 !important;\n}\n[_nghost-%COMP%]   .menu-container[_ngcontent-%COMP%]     .dx-treeview .dx-treeview-toggle-item-visibility {\n  right: 10px;\n  left: auto;\n}\n[_nghost-%COMP%]   .menu-container[_ngcontent-%COMP%]     .dx-treeview .dx-rtl .dx-treeview-toggle-item-visibility {\n  left: 10px;\n  right: auto;\n}\n[_nghost-%COMP%]   .menu-container[_ngcontent-%COMP%]     .dx-treeview .dx-treeview-node[aria-level=\"1\"] {\n  font-weight: bold;\n  border-bottom: 1px solid #515159;\n}\n[_nghost-%COMP%]   .menu-container[_ngcontent-%COMP%]     .dx-treeview .dx-treeview-node[aria-level=\"2\"] .dx-treeview-item-content {\n  font-weight: normal;\n  padding: 0 60px;\n}\n[_nghost-%COMP%]   .menu-container[_ngcontent-%COMP%]     .dx-treeview .dx-treeview-node-container .dx-treeview-node.dx-state-selected:not(.dx-state-focused) > .dx-treeview-item {\n  background: transparent;\n}\n[_nghost-%COMP%]   .menu-container[_ngcontent-%COMP%]     .dx-treeview .dx-treeview-node-container .dx-treeview-node.dx-state-selected > .dx-treeview-item * {\n  color: #457987;\n}\n[_nghost-%COMP%]   .menu-container[_ngcontent-%COMP%]     .dx-treeview .dx-treeview-node-container .dx-treeview-node:not(.dx-state-focused) > .dx-treeview-item.dx-state-hover {\n  background-color: #3f3f4b;\n}\n[_nghost-%COMP%]   .menu-container[_ngcontent-%COMP%]   .dx-theme-generic[_nghost-%COMP%]    .dx-treeview .dx-treeview-node-container .dx-treeview-node.dx-state-selected.dx-state-focused > .dx-treeview-item *, .dx-theme-generic   [_nghost-%COMP%]    .dx-treeview .dx-treeview-node-container .dx-treeview-node.dx-state-selected.dx-state-focused > .dx-treeview-item * {\n  color: inherit;\n}\n/*# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbIndlYnBhY2s6Ly8uL3NyYy9keC1zdHlsZXMuc2NzcyIsIndlYnBhY2s6Ly8uL3NyYy9hcHAvc2hhcmVkL2NvbXBvbmVudHMvc2lkZS1uYXZpZ2F0aW9uLW1lbnUvc2lkZS1uYXZpZ2F0aW9uLW1lbnUuY29tcG9uZW50LnNjc3MiLCJ3ZWJwYWNrOi8vLi9zcmMvdGhlbWVzL2dlbmVyYXRlZC92YXJpYWJsZXMuYWRkaXRpb25hbC5zY3NzIl0sIm5hbWVzIjpbXSwibWFwcGluZ3MiOiJBQUFBO0VBQ0UsV0FBQTtFQUNBLGdCQUFBO0VBQ0EsWUFBQTtBQ0NGOztBREVBO0VBQ0Usc0JBQUE7QUNDRjs7QURFQTtFQUNFLGdCQUFBO0VBQ0EsWUFBQTtBQ0NGO0FEQ0U7RUFDRSxlQUFBO0VBQ0EsZ0JBQUE7RUFDQSxtQkFBQTtBQ0NKOztBREdBO0VBQ0UsWUFBQTtFQUNBLHNCQUFBO0VBQ0EsYUFBQTtBQ0FGOztBREdBO0VBQ0UsT0FBQTtFQUNBLGFBQUE7QUNBRjs7QURHQTtFQUNFLHlCQUFBO0FDQUY7O0FER0E7RUFDRSxpQkFBQTtFQUNBLGtCQUFBO0VBQ0EsZ0JBQUE7QUNBRjtBREVFO0VBQ0UsaUJBQUE7RUFDQSxrQkFBQTtBQ0FKOztBRElBO0VBQ0UsYUFBQTtBQ0RGO0FER0U7RUFDRSxhQUFBO0FDREo7O0FES0E7RUFDRSxnQkFBQTtFQUNBLGNBQUE7RUFDQSxlQUFBO0VBQ0EsZUFBQTtFQUNBLGNBQUE7QUNGRjs7QURLQTtFQUVFLFlBQUE7QUNIRjtBREtFO0VBQ0UsYUFBQTtFQUNBLHNCQUFBO0VBQ0EsZ0JBQUE7QUNISjs7QUFoRUE7RUFDRSxhQUFBO0VBQ0Esc0JBQUE7RUFDQSxnQkFBQTtFQUNBLFlBQUE7RUFDQSx1QkFBQTtBQW1FRjtBQWpFRTtFQUNFLGdCQUFBO0VBQ0EsYUFBQTtFQUNBLE9BQUE7QUFtRUo7QUFqRUk7RUFFRSxtQkFBQTtBQWtFTjtBQTlETTtFQUNFLGVBQUE7RUFDQSxnQkFBQTtBQWdFUjtBQTlEUTtFQUNFLHNCQUFBO0VBQ0Esb0JBQUE7QUFnRVY7QUExRE07RUFDRSx1QkFBQTtBQTREUjtBQXpETTtFQUNFLFdBQUE7RUFDQSxVQUFBO0FBMkRSO0FBeERNO0VBQ0UsVUFBQTtFQUNBLFdBQUE7QUEwRFI7QUFwRFE7RUFDRSxpQkFBQTtFQUNBLGdDQUFBO0FBc0RWO0FBbkRRO0VBQ0UsbUJBQUE7RUFDQSxlQUFBO0FBcURWO0FBM0NVO0VBQ0UsdUJBQUE7QUE2Q1o7QUExQ1U7RUFDRSxjQ3hFRTtBRG9IZDtBQXpDVTtFQUNFLHlCQUFBO0FBMkNaO0FBcENNO0VBQ0UsY0FBQTtBQXNDUiIsInNvdXJjZXNDb250ZW50IjpbImh0bWwsIGJvZHkge1xuICBtYXJnaW46IDBweDtcbiAgbWluLWhlaWdodDogMTAwJTtcbiAgaGVpZ2h0OiAxMDAlO1xufVxuXG4qIHtcbiAgYm94LXNpemluZzogYm9yZGVyLWJveDtcbn1cblxuLmNvbnRlbnQge1xuICBsaW5lLWhlaWdodDogMS41O1xuICBmbGV4LWdyb3c6IDE7XG5cbiAgaDIge1xuICAgIGZvbnQtc2l6ZTogMzBweDtcbiAgICBtYXJnaW4tdG9wOiAyMHB4O1xuICAgIG1hcmdpbi1ib3R0b206IDIwcHg7XG4gIH1cbn1cblxuLmNvbnRhaW5lciB7XG4gIGhlaWdodDogMTAwJTtcbiAgZmxleC1kaXJlY3Rpb246IGNvbHVtbjtcbiAgZGlzcGxheTogZmxleDtcbn1cblxuLmxheW91dC1ib2R5IHtcbiAgZmxleDogMTtcbiAgbWluLWhlaWdodDogMDtcbn1cblxuLnNpZGUtbmF2LW91dGVyLXRvb2xiYXIgLmR4LWRyYXdlciB7XG4gIGhlaWdodDogY2FsYygxMDAlIC0gNTZweClcbn1cblxuLmNvbnRlbnQtYmxvY2sge1xuICBtYXJnaW4tbGVmdDogNDBweDtcbiAgbWFyZ2luLXJpZ2h0OiA0MHB4O1xuICBtYXJnaW4tdG9wOiAyMHB4O1xuXG4gIC5zY3JlZW4teC1zbWFsbCAmIHtcbiAgICBtYXJnaW4tbGVmdDogMjBweDtcbiAgICBtYXJnaW4tcmlnaHQ6IDIwcHg7XG4gIH1cbn1cblxuLnJlc3BvbnNpdmUtcGFkZGluZ3Mge1xuICBwYWRkaW5nOiAyMHB4O1xuXG4gIC5zY3JlZW4tbGFyZ2UgJiB7XG4gICAgcGFkZGluZzogNDBweDtcbiAgfVxufVxuXG4uZHgtY2FyZC53aWRlLWNhcmQge1xuICBib3JkZXItcmFkaXVzOiAwO1xuICBtYXJnaW4tbGVmdDogMDtcbiAgbWFyZ2luLXJpZ2h0OiAwO1xuICBib3JkZXItcmlnaHQ6IDA7XG4gIGJvcmRlci1sZWZ0OiAwO1xufVxuXG4ud2l0aC1mb290ZXIgPiAuZHgtc2Nyb2xsYWJsZS13cmFwcGVyID5cbi5keC1zY3JvbGxhYmxlLWNvbnRhaW5lciA+IC5keC1zY3JvbGxhYmxlLWNvbnRlbnQge1xuICBoZWlnaHQ6IDEwMCU7XG5cbiAgJiA+IC5keC1zY3JvbGx2aWV3LWNvbnRlbnQge1xuICAgIGRpc3BsYXk6IGZsZXg7XG4gICAgZmxleC1kaXJlY3Rpb246IGNvbHVtbjtcbiAgICBtaW4taGVpZ2h0OiAxMDAlO1xuICB9XG59XG5cbiRzaWRlLXBhbmVsLW1pbi13aWR0aDogNjBweDtcbiIsIkBpbXBvcnQgXCIuLi8uLi8uLi8uLi9keC1zdHlsZXMuc2Nzc1wiO1xuQGltcG9ydCBcIi4uLy4uLy4uLy4uL3RoZW1lcy9nZW5lcmF0ZWQvdmFyaWFibGVzLmFkZGl0aW9uYWwuc2Nzc1wiO1xuXG46aG9zdCB7XG4gIGRpc3BsYXk6IGZsZXg7XG4gIGZsZXgtZGlyZWN0aW9uOiBjb2x1bW47XG4gIG1pbi1oZWlnaHQ6IDEwMCU7XG4gIGhlaWdodDogMTAwJTtcbiAgd2lkdGg6IDI1MHB4ICFpbXBvcnRhbnQ7XG5cbiAgLm1lbnUtY29udGFpbmVyIHtcbiAgICBtaW4taGVpZ2h0OiAxMDAlO1xuICAgIGRpc3BsYXk6IGZsZXg7XG4gICAgZmxleDogMTtcblxuICAgIDo6bmctZGVlcCAuZHgtdHJlZXZpZXcge1xuICAgICAgLy8gIyMgTG9uZyB0ZXh0IHBvc2l0aW9uaW5nXG4gICAgICB3aGl0ZS1zcGFjZTogbm93cmFwO1xuICAgICAgLy8gIyNcblxuICAgICAgLy8gIyMgSWNvbiB3aWR0aCBjdXN0b21pemF0aW9uXG4gICAgICAuZHgtdHJlZXZpZXctaXRlbSB7XG4gICAgICAgIHBhZGRpbmctbGVmdDogMDtcbiAgICAgICAgcGFkZGluZy1yaWdodDogMDtcblxuICAgICAgICAuZHgtaWNvbiB7XG4gICAgICAgICAgd2lkdGg6ICRzaWRlLXBhbmVsLW1pbi13aWR0aCAhaW1wb3J0YW50O1xuICAgICAgICAgIG1hcmdpbjogMCAhaW1wb3J0YW50O1xuICAgICAgICB9XG4gICAgICB9XG4gICAgICAvLyAjI1xuICAgICAgXG4gICAgICAvLyAjIyBBcnJvdyBjdXN0b21pemF0aW9uXG4gICAgICAuZHgtdHJlZXZpZXctbm9kZSB7XG4gICAgICAgIHBhZGRpbmc6IDAgMCAhaW1wb3J0YW50O1xuICAgICAgfVxuXG4gICAgICAuZHgtdHJlZXZpZXctdG9nZ2xlLWl0ZW0tdmlzaWJpbGl0eSB7XG4gICAgICAgIHJpZ2h0OiAxMHB4O1xuICAgICAgICBsZWZ0OiBhdXRvO1xuICAgICAgfVxuXG4gICAgICAuZHgtcnRsIC5keC10cmVldmlldy10b2dnbGUtaXRlbS12aXNpYmlsaXR5IHtcbiAgICAgICAgbGVmdDogMTBweDtcbiAgICAgICAgcmlnaHQ6IGF1dG87XG4gICAgICB9XG4gICAgICAvLyAjI1xuXG4gICAgICAvLyAjIyBJdGVtIGxldmVscyBjdXN0b21pemF0aW9uXG4gICAgICAuZHgtdHJlZXZpZXctbm9kZSB7XG4gICAgICAgICZbYXJpYS1sZXZlbD0nMSddIHtcbiAgICAgICAgICBmb250LXdlaWdodDogYm9sZDtcbiAgICAgICAgICBib3JkZXItYm90dG9tOiAxcHggc29saWQgJGJhc2UtYm9yZGVyLWNvbG9yO1xuICAgICAgICB9XG4gIFxuICAgICAgICAmW2FyaWEtbGV2ZWw9JzInXSAuZHgtdHJlZXZpZXctaXRlbS1jb250ZW50IHtcbiAgICAgICAgICBmb250LXdlaWdodDogbm9ybWFsO1xuICAgICAgICAgIHBhZGRpbmc6IDAgJHNpZGUtcGFuZWwtbWluLXdpZHRoO1xuICAgICAgICB9XG4gICAgICB9XG4gICAgICAvLyAjI1xuICAgIH1cblxuICAgIC8vICMjIFNlbGVjdGVkICYgRm9jdWNlZCBpdGVtcyBjdXN0b21pemF0aW9uXG4gICAgOjpuZy1kZWVwIC5keC10cmVldmlldyB7XG4gICAgICAuZHgtdHJlZXZpZXctbm9kZS1jb250YWluZXIge1xuICAgICAgICAuZHgtdHJlZXZpZXctbm9kZSB7XG4gICAgICAgICAgJi5keC1zdGF0ZS1zZWxlY3RlZDpub3QoLmR4LXN0YXRlLWZvY3VzZWQpPiAuZHgtdHJlZXZpZXctaXRlbSB7XG4gICAgICAgICAgICBiYWNrZ3JvdW5kOiB0cmFuc3BhcmVudDtcbiAgICAgICAgICB9XG5cbiAgICAgICAgICAmLmR4LXN0YXRlLXNlbGVjdGVkID4gLmR4LXRyZWV2aWV3LWl0ZW0gKiB7XG4gICAgICAgICAgICBjb2xvcjogJGJhc2UtYWNjZW50O1xuICAgICAgICAgIH1cblxuICAgICAgICAgICY6bm90KC5keC1zdGF0ZS1mb2N1c2VkKT4uZHgtdHJlZXZpZXctaXRlbS5keC1zdGF0ZS1ob3ZlciB7XG4gICAgICAgICAgICBiYWNrZ3JvdW5kLWNvbG9yOiBsaWdodGVuKCRiYXNlLWJnLCA0LjAwKTtcbiAgICAgICAgICB9XG4gICAgICAgIH1cbiAgICAgIH1cbiAgICB9XG5cbiAgICA6aG9zdC1jb250ZXh0KC5keC10aGVtZS1nZW5lcmljKSA6Om5nLWRlZXAuZHgtdHJlZXZpZXcge1xuICAgICAgLmR4LXRyZWV2aWV3LW5vZGUtY29udGFpbmVyIC5keC10cmVldmlldy1ub2RlLmR4LXN0YXRlLXNlbGVjdGVkLmR4LXN0YXRlLWZvY3VzZWQgPiAuZHgtdHJlZXZpZXctaXRlbSAqIHtcbiAgICAgICAgY29sb3I6IGluaGVyaXQ7XG4gICAgICB9XG4gICAgfVxuICAgIC8vICMjXG4gIH1cbn1cbiIsIiRiYXNlLWFjY2VudDogIzQ1Nzk4NztcbiRiYXNlLXRleHQtY29sb3I6ICNmZmZmZmY7XG4kYmFzZS1iZzogIzM2MzY0MDtcbiRiYXNlLWJvcmRlci1jb2xvcjogIzUxNTE1OTtcbiRiYXNlLWJvcmRlci1yYWRpdXM6IDRweDtcbiJdLCJzb3VyY2VSb290IjoiIn0= */"]
});
class SideNavigationMenuModule {}
SideNavigationMenuModule.ɵfac = function SideNavigationMenuModule_Factory(t) {
  return new (t || SideNavigationMenuModule)();
};
SideNavigationMenuModule.ɵmod = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵdefineNgModule"]({
  type: SideNavigationMenuModule
});
SideNavigationMenuModule.ɵinj = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵdefineInjector"]({
  imports: [devextreme_angular_ui_tree_view__WEBPACK_IMPORTED_MODULE_3__.DxTreeViewModule]
});
(function () {
  (typeof ngJitMode === "undefined" || ngJitMode) && _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵsetNgModuleScope"](SideNavigationMenuModule, {
    declarations: [SideNavigationMenuComponent],
    imports: [devextreme_angular_ui_tree_view__WEBPACK_IMPORTED_MODULE_3__.DxTreeViewModule],
    exports: [SideNavigationMenuComponent]
  });
})();

/***/ }),

/***/ 57387:
/*!**********************************************************************!*\
  !*** ./src/app/shared/components/user-panel/user-panel.component.ts ***!
  \**********************************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   UserPanelComponent: () => (/* binding */ UserPanelComponent),
/* harmony export */   UserPanelModule: () => (/* binding */ UserPanelModule)
/* harmony export */ });
/* harmony import */ var _angular_common__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! @angular/common */ 26575);
/* harmony import */ var devextreme_angular_ui_list__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! devextreme-angular/ui/list */ 75730);
/* harmony import */ var devextreme_angular_ui_context_menu__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! devextreme-angular/ui/context-menu */ 84505);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! @angular/core */ 61699);







const _c0 = function () {
  return {
    my: "top",
    at: "bottom"
  };
};
function UserPanelComponent_dx_context_menu_6_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelement"](0, "dx-context-menu", 7);
  }
  if (rf & 2) {
    const ctx_r0 = _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵnextContext"]();
    _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵproperty"]("items", ctx_r0.menuItems)("position", _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵpureFunction0"](2, _c0));
  }
}
function UserPanelComponent_dx_list_7_Template(rf, ctx) {
  if (rf & 1) {
    _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelement"](0, "dx-list", 8);
  }
  if (rf & 2) {
    const ctx_r1 = _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵnextContext"]();
    _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵproperty"]("items", ctx_r1.menuItems);
  }
}
class UserPanelComponent {
  constructor() {}
}
UserPanelComponent.ɵfac = function UserPanelComponent_Factory(t) {
  return new (t || UserPanelComponent)();
};
UserPanelComponent.ɵcmp = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵdefineComponent"]({
  type: UserPanelComponent,
  selectors: [["app-user-panel"]],
  inputs: {
    menuItems: "menuItems",
    menuMode: "menuMode",
    user: "user"
  },
  decls: 8,
  vars: 3,
  consts: [[1, "user-panel"], [1, "user-info"], [1, "image-container"], [1, "user-image"], [1, "user-name"], ["target", ".user-button", "showEvent", "dxclick", "width", "210px", "cssClass", "user-menu", 3, "items", "position", 4, "ngIf"], ["class", "dx-toolbar-menu-action", 3, "items", 4, "ngIf"], ["target", ".user-button", "showEvent", "dxclick", "width", "210px", "cssClass", "user-menu", 3, "items", "position"], [1, "dx-toolbar-menu-action", 3, "items"]],
  template: function UserPanelComponent_Template(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementStart"](0, "div", 0)(1, "div", 1)(2, "div", 2);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelement"](3, "div", 3);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementEnd"]();
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementStart"](4, "div", 4);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵtext"](5);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementEnd"]()();
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵtemplate"](6, UserPanelComponent_dx_context_menu_6_Template, 1, 3, "dx-context-menu", 5);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵtemplate"](7, UserPanelComponent_dx_list_7_Template, 1, 1, "dx-list", 6);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵelementEnd"]();
    }
    if (rf & 2) {
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵadvance"](5);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵtextInterpolate"](ctx.user == null ? null : ctx.user.user);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵproperty"]("ngIf", ctx.menuMode === "context");
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵadvance"](1);
      _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵproperty"]("ngIf", ctx.menuMode === "list");
    }
  },
  dependencies: [devextreme_angular_ui_list__WEBPACK_IMPORTED_MODULE_1__.DxListComponent, devextreme_angular_ui_context_menu__WEBPACK_IMPORTED_MODULE_2__.DxContextMenuComponent, _angular_common__WEBPACK_IMPORTED_MODULE_3__.NgIf],
  styles: [".user-info[_ngcontent-%COMP%] {\n  display: flex;\n  align-items: center;\n}\n.dx-toolbar-menu-section[_nghost-%COMP%]   .user-info[_ngcontent-%COMP%], .dx-toolbar-menu-section   [_nghost-%COMP%]   .user-info[_ngcontent-%COMP%] {\n  padding: 10px 6px;\n  border-bottom: 1px solid rgba(0, 0, 0, 0.1);\n}\n.user-info[_ngcontent-%COMP%]   .image-container[_ngcontent-%COMP%] {\n  overflow: hidden;\n  border-radius: 50%;\n  height: 30px;\n  width: 30px;\n  margin: 0 4px;\n  border: 1px solid rgba(0, 0, 0, 0.1);\n  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.15);\n}\n.user-info[_ngcontent-%COMP%]   .image-container[_ngcontent-%COMP%]   .user-image[_ngcontent-%COMP%] {\n  width: 100%;\n  height: 100%;\n  background: url(\"https://js.devexpress.com/Demos/WidgetsGallery/JSDemos/images/employees/06.png\") no-repeat #fff;\n  background-size: cover;\n}\n.user-info[_ngcontent-%COMP%]   .user-name[_ngcontent-%COMP%] {\n  font-size: 14px;\n  margin: 0 9px;\n}\n\n.user-panel[_ngcontent-%COMP%]     .dx-list-item .dx-icon {\n  vertical-align: middle;\n  color: rgba(255, 255, 255, 0.87);\n  margin-right: 16px;\n}\n.user-panel[_ngcontent-%COMP%]     .dx-rtl .dx-list-item .dx-icon {\n  margin-right: 0;\n  margin-left: 16px;\n}\n\n  .dx-context-menu.user-menu.dx-rtl .dx-submenu .dx-menu-items-container .dx-icon {\n  margin-left: 16px;\n}\n  .dx-context-menu.user-menu .dx-submenu .dx-menu-items-container .dx-icon {\n  margin-right: 16px;\n}\n  .dx-context-menu.user-menu .dx-menu-item .dx-menu-item-content {\n  padding: 3px 15px 4px;\n}\n\n  .dx-theme-generic .user-menu .dx-menu-item-content .dx-menu-item-text {\n  padding-left: 4px;\n  padding-right: 4px;\n}\n/*# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbIndlYnBhY2s6Ly8uL3NyYy9hcHAvc2hhcmVkL2NvbXBvbmVudHMvdXNlci1wYW5lbC91c2VyLXBhbmVsLmNvbXBvbmVudC5zY3NzIiwid2VicGFjazovLy4vc3JjL3RoZW1lcy9nZW5lcmF0ZWQvdmFyaWFibGVzLmJhc2UuZGFyay5zY3NzIl0sIm5hbWVzIjpbXSwibWFwcGluZ3MiOiJBQUVBO0VBQ0UsYUFBQTtFQUNBLG1CQUFBO0FBREY7QUFHRTtFQUNFLGlCQUFBO0VBQ0EsMkNBQUE7QUFESjtBQUlFO0VBQ0UsZ0JBQUE7RUFDQSxrQkFBQTtFQUNBLFlBQUE7RUFDQSxXQUFBO0VBQ0EsYUFBQTtFQUNBLG9DQUFBO0VBQ0EseUNBQUE7QUFGSjtBQUlJO0VBQ0UsV0FBQTtFQUNBLFlBQUE7RUFDQSxnSEFBQTtFQUNBLHNCQUFBO0FBRk47QUFNRTtFQUNFLGVBQUE7RUFDQSxhQUFBO0FBSko7O0FBU0U7RUFDRSxzQkFBQTtFQUNBLGdDQ3BDYztFRHFDZCxrQkFBQTtBQU5KO0FBUUU7RUFDRSxlQUFBO0VBQ0EsaUJBQUE7QUFOSjs7QUFZSTtFQUNFLGlCQUFBO0FBVE47QUFZRTtFQUNFLGtCQUFBO0FBVko7QUFZRTtFQUNJLHFCQUFBO0FBVk47O0FBY0E7RUFDRSxpQkFBQTtFQUNBLGtCQUFBO0FBWEYiLCJzb3VyY2VzQ29udGVudCI6WyJAaW1wb3J0IFwiLi4vLi4vLi4vLi4vdGhlbWVzL2dlbmVyYXRlZC92YXJpYWJsZXMuYmFzZS5kYXJrLnNjc3NcIjtcblxuLnVzZXItaW5mbyB7XG4gIGRpc3BsYXk6IGZsZXg7XG4gIGFsaWduLWl0ZW1zOiBjZW50ZXI7XG5cbiAgOmhvc3QtY29udGV4dCguZHgtdG9vbGJhci1tZW51LXNlY3Rpb24pICYge1xuICAgIHBhZGRpbmc6IDEwcHggNnB4O1xuICAgIGJvcmRlci1ib3R0b206IDFweCBzb2xpZCByZ2JhKDAsIDAsIDAsIDAuMSk7XG4gIH1cblxuICAuaW1hZ2UtY29udGFpbmVyIHtcbiAgICBvdmVyZmxvdzogaGlkZGVuO1xuICAgIGJvcmRlci1yYWRpdXM6IDUwJTtcbiAgICBoZWlnaHQ6IDMwcHg7XG4gICAgd2lkdGg6IDMwcHg7XG4gICAgbWFyZ2luOiAwIDRweDtcbiAgICBib3JkZXI6IDFweCBzb2xpZCByZ2JhKDAsIDAsIDAsIDAuMSk7XG4gICAgYm94LXNoYWRvdzogMCAxcHggM3B4IHJnYmEoMCwwLDAsMC4xNSk7XG5cbiAgICAudXNlci1pbWFnZSB7XG4gICAgICB3aWR0aDogMTAwJTtcbiAgICAgIGhlaWdodDogMTAwJTtcbiAgICAgIGJhY2tncm91bmQ6IHVybChcImh0dHBzOi8vanMuZGV2ZXhwcmVzcy5jb20vRGVtb3MvV2lkZ2V0c0dhbGxlcnkvSlNEZW1vcy9pbWFnZXMvZW1wbG95ZWVzLzA2LnBuZ1wiKSBuby1yZXBlYXQgI2ZmZjtcbiAgICAgIGJhY2tncm91bmQtc2l6ZTogY292ZXI7XG4gICAgfVxuICB9XG5cbiAgLnVzZXItbmFtZSB7XG4gICAgZm9udC1zaXplOiAxNHB4O1xuICAgIG1hcmdpbjogMCA5cHg7XG4gIH1cbn1cblxuLnVzZXItcGFuZWwgOjpuZy1kZWVwIHtcbiAgLmR4LWxpc3QtaXRlbSAuZHgtaWNvbiB7XG4gICAgdmVydGljYWwtYWxpZ246IG1pZGRsZTtcbiAgICBjb2xvcjogJGJhc2UtdGV4dC1jb2xvcjtcbiAgICBtYXJnaW4tcmlnaHQ6IDE2cHg7XG4gIH1cbiAgLmR4LXJ0bCAuZHgtbGlzdC1pdGVtIC5keC1pY29uIHtcbiAgICBtYXJnaW4tcmlnaHQ6IDA7XG4gICAgbWFyZ2luLWxlZnQ6IDE2cHg7XG4gIH1cbn1cblxuOjpuZy1kZWVwIC5keC1jb250ZXh0LW1lbnUudXNlci1tZW51IHtcbiAgJi5keC1ydGwge1xuICAgIC5keC1zdWJtZW51IC5keC1tZW51LWl0ZW1zLWNvbnRhaW5lciAuZHgtaWNvbiB7XG4gICAgICBtYXJnaW4tbGVmdDogMTZweDtcbiAgICB9XG4gIH1cbiAgLmR4LXN1Ym1lbnUgLmR4LW1lbnUtaXRlbXMtY29udGFpbmVyIC5keC1pY29uIHtcbiAgICBtYXJnaW4tcmlnaHQ6IDE2cHg7XG4gIH1cbiAgLmR4LW1lbnUtaXRlbSAuZHgtbWVudS1pdGVtLWNvbnRlbnQge1xuICAgICAgcGFkZGluZzogM3B4IDE1cHggNHB4O1xuICB9XG59XG5cbjo6bmctZGVlcCAuZHgtdGhlbWUtZ2VuZXJpYyAudXNlci1tZW51IC5keC1tZW51LWl0ZW0tY29udGVudCAuZHgtbWVudS1pdGVtLXRleHQge1xuICBwYWRkaW5nLWxlZnQ6IDRweDtcbiAgcGFkZGluZy1yaWdodDogNHB4O1xufVxuIiwiJGJhc2UtYmc6ICMyYTJhMmE7XG4kYmFzZS10ZXh0LWNvbG9yOiByZ2JhKDI1NSwgMjU1LCAyNTUsIDAuODcpO1xuJGJhc2UtYm9yZGVyLWNvbG9yOiAjNGQ0ZDRkO1xuJGJhc2UtYm9yZGVyLXJhZGl1czogNHB4O1xuJGJhc2UtYWNjZW50OiAjNDU3OTg3O1xuJGJhc2UtYWNjZW50MjogIzMyNTg2MjtcbiJdLCJzb3VyY2VSb290IjoiIn0= */"]
});
class UserPanelModule {}
UserPanelModule.ɵfac = function UserPanelModule_Factory(t) {
  return new (t || UserPanelModule)();
};
UserPanelModule.ɵmod = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵdefineNgModule"]({
  type: UserPanelModule
});
UserPanelModule.ɵinj = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵdefineInjector"]({
  imports: [devextreme_angular_ui_list__WEBPACK_IMPORTED_MODULE_1__.DxListModule, devextreme_angular_ui_context_menu__WEBPACK_IMPORTED_MODULE_2__.DxContextMenuModule, _angular_common__WEBPACK_IMPORTED_MODULE_3__.CommonModule]
});
(function () {
  (typeof ngJitMode === "undefined" || ngJitMode) && _angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵsetNgModuleScope"](UserPanelModule, {
    declarations: [UserPanelComponent],
    imports: [devextreme_angular_ui_list__WEBPACK_IMPORTED_MODULE_1__.DxListModule, devextreme_angular_ui_context_menu__WEBPACK_IMPORTED_MODULE_2__.DxContextMenuModule, _angular_common__WEBPACK_IMPORTED_MODULE_3__.CommonModule],
    exports: [UserPanelComponent]
  });
})();

/***/ }),

/***/ 61328:
/*!*****************************************************!*\
  !*** ./src/app/shared/services/app-info.service.ts ***!
  \*****************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   AppInfoService: () => (/* binding */ AppInfoService)
/* harmony export */ });
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! @angular/core */ 61699);

class AppInfoService {
  constructor() {}
  get title() {
    return 'Automatica.Satellite';
  }
  get currentYear() {
    return new Date().getFullYear();
  }
}
AppInfoService.ɵfac = function AppInfoService_Factory(t) {
  return new (t || AppInfoService)();
};
AppInfoService.ɵprov = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵdefineInjectable"]({
  token: AppInfoService,
  factory: AppInfoService.ɵfac
});

/***/ }),

/***/ 19483:
/*!*************************************************!*\
  !*** ./src/app/shared/services/auth.service.ts ***!
  \*************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   AuthGuardService: () => (/* binding */ AuthGuardService),
/* harmony export */   AuthService: () => (/* binding */ AuthService)
/* harmony export */ });
/* harmony import */ var C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./node_modules/@babel/runtime/helpers/esm/asyncToGenerator.js */ 71670);
/* harmony import */ var _base_service__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./base.service */ 127);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var _angular_router__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! @angular/router */ 27947);
/* harmony import */ var _angular_common_http__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! @angular/common/http */ 54860);





const defaultPath = '/';
const defaultUser = {
  email: 'sandra@example.com',
  avatarUrl: 'https://js.devexpress.com/Demos/WidgetsGallery/JSDemos/images/employees/06.png'
};
class AuthService extends _base_service__WEBPACK_IMPORTED_MODULE_1__.BaseService {
  get loggedIn() {
    return !!this._user;
  }
  set lastAuthenticatedPath(value) {
    this._lastAuthenticatedPath = value;
  }
  get isReady() {
    return this._isReady;
  }
  constructor(router, http) {
    super(http);
    this.router = router;
    this._user = null;
    this._lastAuthenticatedPath = defaultPath;
    this._isReady = undefined;
  }
  loadIsReady() {
    var _this = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      const result = yield _this.http.get("/webapi/ready", {
        headers: _this.headers()
      }).toPromise();
      _this._isReady = result.ready;
      _this.router.navigate(['/login-form']);
      return _this.isReady;
    })();
  }
  logIn(user, password) {
    var _this2 = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      try {
        // Send request
        // this.router.navigate([this._lastAuthenticatedPath]);
        const response = yield _this2.http.post("/webapi/login", {
          username: user,
          password: password
        }, {
          headers: _this2.headers()
        }).toPromise();
        _this2._user = {
          ...defaultUser,
          user
        };
        console.log(response);
        localStorage.setItem("token", response.toString());
        _this2.router.navigate(['/']);
        return {
          isOk: true,
          data: _this2._user
        };
      } catch {
        return {
          isOk: false,
          message: "Authentication failed"
        };
      }
    })();
  }
  getUser() {
    var _this3 = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      try {
        // Send request
        if (_this3._user) {
          return {
            isOk: true,
            data: _this3._user
          };
        }
        const result = yield _this3.http.get("/webapi/user", {
          headers: _this3.headers()
        }).toPromise();
        _this3._user = {
          user: result.user
        };
        _this3.router.navigate(['/']);
        return {
          isOk: true,
          data: _this3._user
        };
      } catch {
        return {
          isOk: false,
          data: null
        };
      }
    })();
  }
  setupAccount(username, password) {
    var _this4 = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      try {
        // Send request
        const response = yield _this4.http.post("/webapi/setup", {
          username,
          password
        }, {
          headers: _this4.headers()
        }).toPromise();
        _this4.router.navigate(['/login-form']);
        _this4._isReady = true;
        return {
          isOk: true
        };
      } catch {
        return {
          isOk: false,
          message: "Failed to create account"
        };
      }
    })();
  }
  changePassword(email, recoveryCode) {
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      try {
        // Send request
        return {
          isOk: true
        };
      } catch {
        return {
          isOk: false,
          message: "Failed to change password"
        };
      }
    })();
  }
  resetPassword(email) {
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      try {
        // Send request
        return {
          isOk: true
        };
      } catch {
        return {
          isOk: false,
          message: "Failed to reset password"
        };
      }
    })();
  }
  logOut() {
    var _this5 = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      _this5._user = null;
      _this5.router.navigate(['/login-form']);
    })();
  }
}
AuthService.ɵfac = function AuthService_Factory(t) {
  return new (t || AuthService)(_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵinject"](_angular_router__WEBPACK_IMPORTED_MODULE_3__.Router), _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵinject"](_angular_common_http__WEBPACK_IMPORTED_MODULE_4__.HttpClient));
};
AuthService.ɵprov = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵdefineInjectable"]({
  token: AuthService,
  factory: AuthService.ɵfac
});
class AuthGuardService {
  constructor(router, authService) {
    this.router = router;
    this.authService = authService;
  }
  canActivate(route) {
    const isLoggedIn = this.authService.loggedIn;
    const isAuthForm = ['login-form', 'reset-password', 'create-account', 'change-password/:recoveryCode'].includes(route.routeConfig?.path || defaultPath);
    const token = localStorage.getItem("token");
    if (!!token) {
      this.authService.getUser();
    }
    if (isLoggedIn && isAuthForm) {
      this.authService.lastAuthenticatedPath = defaultPath;
      this.router.navigate([defaultPath]);
      return false;
    }
    const isReady = this.authService.isReady;
    if (isReady == false) {
      if (route.routeConfig?.path == "create-account") return true;
      this.router.navigate(['/create-account']);
      return false;
    }
    if (!isLoggedIn && !isAuthForm) {
      this.router.navigate(['/login-form']);
    }
    if (isLoggedIn) {
      this.authService.lastAuthenticatedPath = route.routeConfig?.path || defaultPath;
    }
    return isLoggedIn || isAuthForm;
  }
}
AuthGuardService.ɵfac = function AuthGuardService_Factory(t) {
  return new (t || AuthGuardService)(_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵinject"](_angular_router__WEBPACK_IMPORTED_MODULE_3__.Router), _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵinject"](AuthService));
};
AuthGuardService.ɵprov = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵdefineInjectable"]({
  token: AuthGuardService,
  factory: AuthGuardService.ɵfac
});

/***/ }),

/***/ 127:
/*!*************************************************!*\
  !*** ./src/app/shared/services/base.service.ts ***!
  \*************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   BaseService: () => (/* binding */ BaseService)
/* harmony export */ });
/* harmony import */ var _angular_common_http__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! @angular/common/http */ 54860);

class BaseService {
  constructor(http) {
    this.http = http;
  }
  headers() {
    let headers = new _angular_common_http__WEBPACK_IMPORTED_MODULE_0__.HttpHeaders();
    headers = headers.append("Content-Type", "application/json");
    headers = headers.append("Accept", "application/json");
    headers = headers.append("Access-Control-Allow-Origin", "*");
    const jwt = localStorage.getItem("token");
    if (jwt) {
      headers = headers.append("Authorization", "Bearer " + jwt);
    }
    return headers;
  }
}

/***/ }),

/***/ 57175:
/*!******************************************!*\
  !*** ./src/app/shared/services/index.ts ***!
  \******************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   AppInfoService: () => (/* reexport safe */ _app_info_service__WEBPACK_IMPORTED_MODULE_0__.AppInfoService),
/* harmony export */   AuthGuardService: () => (/* reexport safe */ _auth_service__WEBPACK_IMPORTED_MODULE_1__.AuthGuardService),
/* harmony export */   AuthService: () => (/* reexport safe */ _auth_service__WEBPACK_IMPORTED_MODULE_1__.AuthService),
/* harmony export */   ScreenService: () => (/* reexport safe */ _screen_service__WEBPACK_IMPORTED_MODULE_2__.ScreenService)
/* harmony export */ });
/* harmony import */ var _app_info_service__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./app-info.service */ 61328);
/* harmony import */ var _auth_service__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./auth.service */ 19483);
/* harmony import */ var _screen_service__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ./screen.service */ 32395);




/***/ }),

/***/ 32395:
/*!***************************************************!*\
  !*** ./src/app/shared/services/screen.service.ts ***!
  \***************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   ScreenService: () => (/* binding */ ScreenService)
/* harmony export */ });
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var _angular_cdk_layout__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! @angular/cdk/layout */ 39743);




class ScreenService {
  constructor(breakpointObserver) {
    this.breakpointObserver = breakpointObserver;
    this.changed = new _angular_core__WEBPACK_IMPORTED_MODULE_0__.EventEmitter();
    this.breakpointObserver.observe([_angular_cdk_layout__WEBPACK_IMPORTED_MODULE_1__.Breakpoints.XSmall, _angular_cdk_layout__WEBPACK_IMPORTED_MODULE_1__.Breakpoints.Small, _angular_cdk_layout__WEBPACK_IMPORTED_MODULE_1__.Breakpoints.Medium, _angular_cdk_layout__WEBPACK_IMPORTED_MODULE_1__.Breakpoints.Large]).subscribe(() => this.changed.next(true));
  }
  isLargeScreen() {
    const isLarge = this.breakpointObserver.isMatched(_angular_cdk_layout__WEBPACK_IMPORTED_MODULE_1__.Breakpoints.Large);
    const isXLarge = this.breakpointObserver.isMatched(_angular_cdk_layout__WEBPACK_IMPORTED_MODULE_1__.Breakpoints.XLarge);
    return isLarge || isXLarge;
  }
  get sizes() {
    return {
      'screen-x-small': this.breakpointObserver.isMatched(_angular_cdk_layout__WEBPACK_IMPORTED_MODULE_1__.Breakpoints.XSmall),
      'screen-small': this.breakpointObserver.isMatched(_angular_cdk_layout__WEBPACK_IMPORTED_MODULE_1__.Breakpoints.Small),
      'screen-medium': this.breakpointObserver.isMatched(_angular_cdk_layout__WEBPACK_IMPORTED_MODULE_1__.Breakpoints.Medium),
      'screen-large': this.isLargeScreen()
    };
  }
}
ScreenService.ɵfac = function ScreenService_Factory(t) {
  return new (t || ScreenService)(_angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵinject"](_angular_cdk_layout__WEBPACK_IMPORTED_MODULE_1__.BreakpointObserver));
};
ScreenService.ɵprov = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_0__["ɵɵdefineInjectable"]({
  token: ScreenService,
  factory: ScreenService.ɵfac
});

/***/ }),

/***/ 761:
/*!**************************************************!*\
  !*** ./src/app/shared/services/statu.service.ts ***!
  \**************************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   StatusService: () => (/* binding */ StatusService)
/* harmony export */ });
/* harmony import */ var C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./node_modules/@babel/runtime/helpers/esm/asyncToGenerator.js */ 71670);
/* harmony import */ var _base_service__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./base.service */ 127);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var _angular_common_http__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! @angular/common/http */ 54860);




class StatusService extends _base_service__WEBPACK_IMPORTED_MODULE_1__.BaseService {
  constructor(http) {
    super(http);
  }
  getStatus() {
    var _this = this;
    return (0,C_dev_automatica_core_automatica_src_automatica_core_slave_src_Automatica_Core_Satellite_Web_node_modules_babel_runtime_helpers_esm_asyncToGenerator_js__WEBPACK_IMPORTED_MODULE_0__["default"])(function* () {
      const result = yield _this.http.get("/webapi/status", {
        headers: _this.headers()
      }).toPromise();
      return result;
    })();
  }
}
StatusService.ɵfac = function StatusService_Factory(t) {
  return new (t || StatusService)(_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵinject"](_angular_common_http__WEBPACK_IMPORTED_MODULE_3__.HttpClient));
};
StatusService.ɵprov = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵdefineInjectable"]({
  token: StatusService,
  factory: StatusService.ɵfac
});

/***/ }),

/***/ 40203:
/*!********************************************!*\
  !*** ./src/app/unauthenticated-content.ts ***!
  \********************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   UnauthenticatedContentComponent: () => (/* binding */ UnauthenticatedContentComponent),
/* harmony export */   UnauthenticatedContentModule: () => (/* binding */ UnauthenticatedContentModule)
/* harmony export */ });
/* harmony import */ var _angular_common__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! @angular/common */ 26575);
/* harmony import */ var _angular_router__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! @angular/router */ 27947);
/* harmony import */ var src_app_layouts__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! src/app/layouts */ 81447);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var _layouts_single_card_single_card_component__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./layouts/single-card/single-card.component */ 61010);






class UnauthenticatedContentComponent {
  constructor(router) {
    this.router = router;
  }
  get title() {
    const path = this.router.url.split('/')[1];
    switch (path) {
      case 'login-form':
        return 'Automatica.Satellite Login';
      case 'reset-password':
        return 'Reset Password';
      case 'create-account':
        return 'Setup Automatica.Satellite';
      case 'change-password':
        return 'Change Password';
      default:
        return '';
    }
  }
  get description() {
    const path = this.router.url.split('/')[1];
    switch (path) {
      case 'reset-password':
        return 'Please enter the email address that you used to register, and we will send you a link to reset your password via Email.';
      default:
        return '';
    }
  }
}
UnauthenticatedContentComponent.ɵfac = function UnauthenticatedContentComponent_Factory(t) {
  return new (t || UnauthenticatedContentComponent)(_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵdirectiveInject"](_angular_router__WEBPACK_IMPORTED_MODULE_3__.Router));
};
UnauthenticatedContentComponent.ɵcmp = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵdefineComponent"]({
  type: UnauthenticatedContentComponent,
  selectors: [["app-unauthenticated-content"]],
  decls: 2,
  vars: 2,
  consts: [[3, "title", "description"]],
  template: function UnauthenticatedContentComponent_Template(rf, ctx) {
    if (rf & 1) {
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementStart"](0, "app-single-card", 0);
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelement"](1, "router-outlet");
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵelementEnd"]();
    }
    if (rf & 2) {
      _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵproperty"]("title", ctx.title)("description", ctx.description);
    }
  },
  dependencies: [_angular_router__WEBPACK_IMPORTED_MODULE_3__.RouterOutlet, _layouts_single_card_single_card_component__WEBPACK_IMPORTED_MODULE_1__.SingleCardComponent],
  styles: ["[_nghost-%COMP%] {\n  width: 100%;\n  height: 100%;\n}\n/*# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbIndlYnBhY2s6Ly8uL3NyYy9hcHAvdW5hdXRoZW50aWNhdGVkLWNvbnRlbnQudHMiXSwibmFtZXMiOltdLCJtYXBwaW5ncyI6IkFBQ0k7RUFDRSxXQUFBO0VBQ0EsWUFBQTtBQUFOIiwic291cmNlc0NvbnRlbnQiOlsiXG4gICAgOmhvc3Qge1xuICAgICAgd2lkdGg6IDEwMCU7XG4gICAgICBoZWlnaHQ6IDEwMCU7XG4gICAgfVxuICAiXSwic291cmNlUm9vdCI6IiJ9 */"]
});
class UnauthenticatedContentModule {}
UnauthenticatedContentModule.ɵfac = function UnauthenticatedContentModule_Factory(t) {
  return new (t || UnauthenticatedContentModule)();
};
UnauthenticatedContentModule.ɵmod = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵdefineNgModule"]({
  type: UnauthenticatedContentModule
});
UnauthenticatedContentModule.ɵinj = /*@__PURE__*/_angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵdefineInjector"]({
  imports: [_angular_common__WEBPACK_IMPORTED_MODULE_4__.CommonModule, _angular_router__WEBPACK_IMPORTED_MODULE_3__.RouterModule, src_app_layouts__WEBPACK_IMPORTED_MODULE_0__.SingleCardModule]
});
(function () {
  (typeof ngJitMode === "undefined" || ngJitMode) && _angular_core__WEBPACK_IMPORTED_MODULE_2__["ɵɵsetNgModuleScope"](UnauthenticatedContentModule, {
    declarations: [UnauthenticatedContentComponent],
    imports: [_angular_common__WEBPACK_IMPORTED_MODULE_4__.CommonModule, _angular_router__WEBPACK_IMPORTED_MODULE_3__.RouterModule, src_app_layouts__WEBPACK_IMPORTED_MODULE_0__.SingleCardModule],
    exports: [UnauthenticatedContentComponent]
  });
})();

/***/ }),

/***/ 20553:
/*!*****************************************!*\
  !*** ./src/environments/environment.ts ***!
  \*****************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   environment: () => (/* binding */ environment)
/* harmony export */ });
// This file can be replaced during build by using the `fileReplacements` array.
// `ng build` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.
const environment = {
  production: false
};
/*
 * For easier debugging in development mode, you can import the following file
 * to ignore zone related error stack frames such as `zone.run`, `zoneDelegate.invokeTask`.
 *
 * This import should be commented out in production mode because it will have a negative impact
 * on performance if an error is thrown.
 */
// import 'zone.js/plugins/zone-error';  // Included with Angular CLI.

/***/ }),

/***/ 14913:
/*!*********************!*\
  !*** ./src/main.ts ***!
  \*********************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony import */ var _angular_platform_browser__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! @angular/platform-browser */ 36480);
/* harmony import */ var devextreme_ui_themes__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! devextreme/ui/themes */ 56978);
/* harmony import */ var _angular_core__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! @angular/core */ 61699);
/* harmony import */ var _app_app_module__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./app/app.module */ 78629);
/* harmony import */ var _environments_environment__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ./environments/environment */ 20553);





if (_environments_environment__WEBPACK_IMPORTED_MODULE_2__.environment.production) {
  (0,_angular_core__WEBPACK_IMPORTED_MODULE_3__.enableProdMode)();
}
devextreme_ui_themes__WEBPACK_IMPORTED_MODULE_0__["default"].initialized(() => {
  _angular_platform_browser__WEBPACK_IMPORTED_MODULE_4__.platformBrowser().bootstrapModule(_app_app_module__WEBPACK_IMPORTED_MODULE_1__.AppModule).catch(err => console.error(err));
});

/***/ })

},
/******/ __webpack_require__ => { // webpackRuntimeModules
/******/ var __webpack_exec__ = (moduleId) => (__webpack_require__(__webpack_require__.s = moduleId))
/******/ __webpack_require__.O(0, ["vendor"], () => (__webpack_exec__(14913)));
/******/ var __webpack_exports__ = __webpack_require__.O();
/******/ }
]);
//# sourceMappingURL=main.js.map