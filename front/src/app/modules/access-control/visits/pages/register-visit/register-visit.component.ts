import { Component, OnInit, inject } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { VisitsService } from '../../services/visits.service';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { LucideAngularModule, User, Save } from 'lucide-angular';
import { ResidentsService } from '../../../residents/services/residents.service';
import { User as Resident } from '../../../../../core/models/user.model';
import { AuthService } from '../../../../../core/services/auth.service';

@Component({
  selector: 'app-register-visit',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    LucideAngularModule,
  ],
  templateUrl: './register-visit.component.html',
  styleUrls: ['./register-visit.component.scss'],
})
export class RegisterVisitComponent implements OnInit {
  visitForm: FormGroup;
  errorMessage: string = '';
  residents: Resident[] = [];
  canRegister = false;

  // Icons
  UserIcon = User;
  SaveIcon = Save;

  private fb = inject(FormBuilder);
  private visitsService = inject(VisitsService);
  private residentsService = inject(ResidentsService);
  private router = inject(Router);
  private authService = inject(AuthService);

  constructor() {
    this.visitForm = this.fb.group({
      visitorName: ['', Validators.required],
      reason: ['', Validators.required],
      residentId: ['', Validators.required],
    });
  }

  ngOnInit(): void {
    this.canRegister =
      this.authService.hasRole('Admin') || this.authService.hasRole('Guard');
    if (!this.canRegister) {
      this.visitForm.disable();
    }
    this.residentsService
      .getResidents()
      .subscribe((residents) => (this.residents = residents));
  }

  onSubmit() {
    if (this.visitForm.valid) {
      this.visitsService.createVisit(this.visitForm.value).subscribe({
        next: () => this.router.navigate(['/access-control/visits']),
        error: (err) => {
          this.errorMessage = 'Error creating visit';
          console.error(err);
        },
      });
    }
  }
}
