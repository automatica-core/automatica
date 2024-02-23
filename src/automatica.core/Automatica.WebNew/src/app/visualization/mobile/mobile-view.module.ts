import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { DndModule } from "p3root-angular-dnd";

import { Routes, RouterModule } from "@angular/router";
import { MobileModule } from "./mobile.module";
import { DxLoadPanelModule, DxButtonModule } from "devextreme-angular";
import { HasRoleGuard } from "../../services/login.service";
import { Role } from "src/app/base/model/user/role";
import { ControlsModule } from "src/app/base/visu/controls/controls.module";
import { MobileContainerComponent } from "./mobile-container/mobile-container.component";
import { MobileContainerModule } from "./mobile-container/mobile-container.module";

const routes: Routes = [
  {
    path: "",
    component: MobileContainerComponent,
    data: {
      title: "Visualization",
      loadHomepage: true,
      loadFavorites: false,
      editable: false,
      requiresRole: Role.VISU_ROLE
    },
     canActivate: [HasRoleGuard]
  }, {
    path: "home",
    component: MobileContainerComponent,
    data: {
      title: "Visualization",
      loadHomepage: false,
      loadFavorites: true,
      editable: false,
      requiresRole: Role.VISU_ROLE
    },
     canActivate: [HasRoleGuard]
  },
  {
    path: ":type/:id",
    component: MobileContainerComponent,
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
    ControlsModule,
    DndModule,
    MobileViewRoutingModule,
    MobileModule,
    DxLoadPanelModule,
    MobileContainerModule
  ],
  declarations: [],
  providers: [
  ]
})
export class MobileViewModule { }
