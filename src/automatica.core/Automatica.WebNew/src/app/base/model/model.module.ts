import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BoardInterface } from './board-interface';
import { BoardType } from './board-type';
import { InterfaceType } from './interface-type';
import { NodeDataType } from './node-data-type';
import { NodeInstance } from './node-instance';
import { NodeTemplate } from './node-template';
import { PropertyInstance } from './property-instance';
import { PropertyTemplate } from './property-template';
import { PropertyType } from './property-type';

@NgModule({
  imports: [
    CommonModule
  ],
  declarations: [
   
  ],
  exports: [
  /*  BoardInterface,
    BoardType,
    InterfaceType,
    NodeDataType,
    NodeInstance,
    NodeTemplate,
    PropertyInstance,
    PropertyTemplate,
    PropertyType*/
  ]
})
export class ModelModule { }
