import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { AdminComponent } from "./admin.component";
import { SideNavOuterToolbarModule, SideNavInnerToolbarModule } from "../layouts";
import { FooterModule } from "../shared/components/footer/footer.component";
import { RouterModule } from "@angular/router";

@NgModule({
  declarations: [AdminComponent],
  imports: [
    CommonModule,
    SideNavOuterToolbarModule,
    SideNavInnerToolbarModule,
    FooterModule,
    RouterModule
  ],
  exports: [
    AdminComponent
  ]
})
export class AdminModule { }
