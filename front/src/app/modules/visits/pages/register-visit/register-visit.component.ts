import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { VisitsService } from '../../visits.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-register-visit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './register-visit.component.html',
  styleUrls: ['./register-visit.component.scss']
})
export class RegisterVisitComponent {
  visitForm: FormGroup;
  errorMessage: string = '';

  private fb = inject(FormBuilder);
  private visitsService = inject(VisitsService);
  private router = inject(Router);

  constructor() {
    this.visitForm = this.fb.group({
      visitorName: ['', Validators.required],
      visitorId: ['', Validators.required],
      vehiclePlate: [''],
      residenceId: ['', Validators.required]
    });
  }

  onSubmit() {
    if (this.visitForm.valid) {
      this.visitsService.createVisit(this.visitForm.value).subscribe({
        next: () => this.router.navigate(['/visits']),
        error: (err) => {
          this.errorMessage = 'Error creating visit';
          console.error(err);
        }
      });
    }
  }
}
