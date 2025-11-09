import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ResidentsService } from '../../residents.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-edit-resident',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './edit-resident.component.html',
  styleUrls: ['./edit-resident.component.scss']
})
export class EditResidentComponent implements OnInit {
  residentForm: FormGroup;
  errorMessage: string = '';
  residentId: string = '';

  private fb = inject(FormBuilder);
  private residentsService = inject(ResidentsService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  constructor() {
    this.residentForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      apartmentNumber: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.residentId = id;
    }
    // Here you would typically fetch the resident's data and patch the form
  }

  onSubmit() {
    if (this.residentForm.valid) {
      this.residentsService.updateResident(this.residentId, this.residentForm.value).subscribe({
        next: () => this.router.navigate(['/residents']),
        error: (err) => {
          this.errorMessage = 'Error updating resident';
          console.error(err);
        }
      });
    }
  }
}
