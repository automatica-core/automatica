import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { L10nTranslationModule } from "angular-l10n";
import { SkeletonComponent } from "./skeleton.component";

@NgModule({
  declarations: [SkeletonComponent],
  imports: [
    CommonModule,
    L10nTranslationModule
  ],
  exports: [
    SkeletonComponent
  ]
})
export class SkeletonModule { }
