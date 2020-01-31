import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { MobileContainerComponent } from "./mobile-container/mobile-container.component";
import { GridsterModule } from "ngx-gridster";
import { DndModule } from "p3root-angular-dnd";

import { Routes, RouterModule } from "@angular/router";
import { MobileModule } from "./mobile.module";
import { DxLoadPanelModule, DxButtonModule } from "devextreme-angular";
import { HasRoleGuard } from "../../services/login.service";
import { Role } from "src/app/base/model/user/role";
import { ControlsModule } from "src/app/base/visu/controls/controls.module";
import { MobileContainer2Component } from "./mobile-container2/mobile-container2.component";
import { MobileContainer2Module } from "./mobile-container2/mobile-container2.module";

const routes: Routes = [
  {
    path: "",
    component: MobileContainer2Component,
    data: {
      title: "Visualization",
      loadHomepage: true,
      editable: false,
      requiresRole: Role.VISU_ROLE
    },
     canActivate: [HasRoleGuard]
  },
  {
    path: ":id",
    component: MobileContainer2Component,
    data: {
      title: "Visualization",
      loadHomepage: false,
      editable: false,
      requiresRole: Role.VISU_ROLE
    },
     canActivate: [HasRoleGuard]
  }

];

@NgModule({
  imports: [
    DxButtonModule,
    RouterModule.forChild(routes)
  ],
  exports: [RouterModule],
  declarations: [

  ]
})
export class MobileViewRoutingModule { }


@NgModule({
  imports: [
    CommonModule,
    RouterModule,
    GridsterModule,
    ControlsModule,
    DndModule,
    MobileViewRoutingModule,
    MobileModule,
    DxLoadPanelModule,
    MobileContainer2Module
  ],
  declarations: [],
  providers: [
  ]
})
export class MobileViewModule { }
