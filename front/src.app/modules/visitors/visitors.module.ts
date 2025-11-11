import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

import { VisitorsComponent } from './visitors.component';
import { VisitorFormComponent } from './visitor-form.component';
import routes from './visitors.routes';

@NgModule({
  declarations: [
    VisitorsComponent,
    VisitorFormComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule.forChild(routes)
  ]
})
export class VisitorsModule { }
