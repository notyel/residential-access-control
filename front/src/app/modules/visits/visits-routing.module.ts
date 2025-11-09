import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { VisitsComponent } from './visits/visits.component';

const routes: Routes = [
  {
    path: '',
    component: VisitsComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class VisitsRoutingModule { }
