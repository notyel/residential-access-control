import { Component, OnInit, inject } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ResidentsService } from '../../services/residents.service';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { LucideAngularModule, User, Save, ArrowLeft } from 'lucide-angular';
import { AuthService } from '../../../../../core/services/auth.service';
import { PageHeaderComponent } from '../../../components/page-header/page-header.component';

@Component({
  selector: 'app-edit-resident',
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
    PageHeaderComponent,
  ],
  templateUrl: './edit-resident.component.html',
  styleUrls: ['./edit-resident.component.scss'],
})
export class EditResidentComponent implements OnInit {
  residentForm: FormGroup;
  errorMessage: string = '';
  residentId: string = '';
  isAdmin = false;

  // Icons
  UserIcon = User;
  SaveIcon = Save;
  ArrowLeftIcon = ArrowLeft;

  private fb = inject(FormBuilder);
  private residentsService = inject(ResidentsService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private authService = inject(AuthService);

  constructor() {
    this.residentForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      apartmentNumber: ['', Validators.required],
    });
  }

  ngOnInit(): void {
    this.isAdmin = this.authService.hasRole('Admin');
    if (!this.isAdmin) {
      this.residentForm.disable();
    }

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.residentId = id;
      this.residentsService.getResident(id).subscribe((resident) => {
        this.residentForm.patchValue(resident);
      });
    }
  }

  onSubmit() {
    if (this.residentForm.valid) {
      this.residentsService
        .updateResident(this.residentId, this.residentForm.value)
        .subscribe({
          next: () => this.router.navigate(['/access-control/residents']),
          error: (err) => {
            this.errorMessage = 'Error al actualizar el residente';
            console.error(err);
          },
        });
    }
  }
}
