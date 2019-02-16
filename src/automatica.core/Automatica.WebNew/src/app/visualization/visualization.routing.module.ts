import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";
import { VisualizationComponent } from "./visualization.component";

const routes: Routes = [
  {
    path: "",
    component: VisualizationComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class VisualizationRoutingModule { }
