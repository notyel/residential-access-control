import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { VisitsComponent } from './visits/visits.component';
import { VisitsRoutingModule } from './visits-routing.module';

@NgModule({
  declarations: [
    VisitsComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    VisitsRoutingModule
  ]
})
export class VisitsModule { }
