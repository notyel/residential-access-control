import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { VisitsService } from '../visits.service';

@Component({
  selector: 'app-register-visit',
  templateUrl: './register-visit.component.html',
  styleUrls: ['./register-visit.component.scss']
})
export class RegisterVisitComponent {
  visitForm: FormGroup;
  errorMessage: string = '';

  constructor(
    private fb: FormBuilder,
    private visitsService: VisitsService,
    private router: Router
  ) {
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
