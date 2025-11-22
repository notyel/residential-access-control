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
import { LucideAngularModule, User, Save, ArrowLeft, Search } from 'lucide-angular';
import { ResidentsService } from '../../../residents/services/residents.service';
import { User as Resident } from '../../../../../core/models/user.model';
import { AuthService } from '../../../../../core/services/auth.service';
import { PageHeaderComponent } from '../../../components/page-header/page-header.component';
import { PersonService } from '../../../../../core/services/person.service';
import { Person } from '../../../../../core/models/person.model';
import { CreateVisit } from '../../../../../core/models/create-visit.model';

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
    PageHeaderComponent,
  ],
  templateUrl: './register-visit.component.html',
  styleUrls: ['./register-visit.component.scss'],
})
export class RegisterVisitComponent implements OnInit {
  visitForm: FormGroup;
  errorMessage: string = '';
  residents: Resident[] = [];
  canRegister = false;
  foundPerson: Person | null = null;
  showNewPersonForm = false;

  // Icons
  UserIcon = User;
  SaveIcon = Save;
  ArrowLeftIcon = ArrowLeft;
  SearchIcon = Search;

  private fb = inject(FormBuilder);
  private visitsService = inject(VisitsService);
  private residentsService = inject(ResidentsService);
  private router = inject(Router);
  private authService = inject(AuthService);
  private personService = inject(PersonService);

  constructor() {
    this.visitForm = this.fb.group({
      documentNumber: ['', Validators.required],
      newPerson: this.fb.group({
        firstName: [''],
        lastName: [''],
        documentType: [''],
        phone: [''],
        email: [''],
        personType: [0],
      }),
      vehiclePlate: [''],
      residenceId: ['', Validators.required],
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

  onSearch() {
    const documentNumber = this.visitForm.get('documentNumber')?.value;
    if (documentNumber) {
      this.personService.searchPersons(documentNumber).subscribe((persons) => {
        if (persons.length > 0) {
          this.foundPerson = persons[0];
          this.showNewPersonForm = false;
          this.visitForm.get('newPerson')?.reset();
        } else {
          this.foundPerson = null;
          this.showNewPersonForm = true;
          this.setNewPersonValidators(true);
        }
      });
    }
  }

  onClear() {
    this.foundPerson = null;
    this.showNewPersonForm = false;
    this.visitForm.get('documentNumber')?.reset();
    this.visitForm.get('newPerson')?.reset();
    this.setNewPersonValidators(false);
  }

  private setNewPersonValidators(enable: boolean) {
    const newPersonForm = this.visitForm.get('newPerson');
    if (enable) {
      newPersonForm?.get('firstName')?.setValidators(Validators.required);
      newPersonForm?.get('lastName')?.setValidators(Validators.required);
      newPersonForm?.get('documentType')?.setValidators(Validators.required);
    } else {
      newPersonForm?.get('firstName')?.clearValidators();
      newPersonForm?.get('lastName')?.clearValidators();
      newPersonForm?.get('documentType')?.clearValidators();
    }
    newPersonForm?.get('firstName')?.updateValueAndValidity();
    newPersonForm?.get('lastName')?.updateValueAndValidity();
    newPersonForm?.get('documentType')?.updateValueAndValidity();
  }

  onSubmit() {
    if (this.visitForm.valid) {
      const formValue = this.visitForm.value;
      const visit: CreateVisit = {
        residenceId: formValue.residenceId,
        vehiclePlate: formValue.vehiclePlate,
      };

      if (this.foundPerson) {
        visit.personId = this.foundPerson.id;
      } else {
        visit.newPerson = formValue.newPerson;
      }

      this.visitsService.createVisit(visit).subscribe({
        next: () => this.router.navigate(['/access-control/visits']),
        error: (err) => {
          this.errorMessage = 'Error al crear la visita';
          console.error(err);
        },
      });
    }
  }
}
