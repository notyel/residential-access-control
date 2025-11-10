import { Component, OnInit, inject } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ResidentsService } from '../../services/residents.service';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { LucideAngularModule, User, Save } from 'lucide-angular';
import { AuthService } from '../../../../../core/services/auth.service';

@Component({
  selector: 'app-new-resident',
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
    LucideAngularModule,
  ],
  templateUrl: './new-resident.component.html',
  styleUrls: ['./new-resident.component.scss'],
})
export class NewResidentComponent implements OnInit {
  residentForm: FormGroup;
  errorMessage: string = '';
  isAdmin = false;

  // Icons
  UserIcon = User;
  SaveIcon = Save;

  private fb = inject(FormBuilder);
  private residentsService = inject(ResidentsService);
  private router = inject(Router);
  private authService = inject(AuthService);

  constructor() {
    this.residentForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      apartmentNumber: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
    });
  }

  ngOnInit(): void {
    this.isAdmin = this.authService.hasRole('Admin');
    if (!this.isAdmin) {
      this.residentForm.disable();
    }
  }

  onSubmit() {
    if (this.residentForm.valid) {
      this.residentsService.createResident(this.residentForm.value).subscribe({
        next: () => this.router.navigate(['/access-control/residents']),
        error: (err) => {
          this.errorMessage = 'Error al crear el residente';
          console.error(err);
        },
      });
    }
  }
}
