import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import {
  Check,
  CircleCheck,
  Info,
  LucideAngularModule,
  Search,
} from 'lucide-angular';
import { map, startWith } from 'rxjs';
import { CreateVisit } from '../../../../../core/models/create-visit.model';
import { Person } from '../../../../../core/models/person.model';
import { User as Resident } from '../../../../../core/models/user.model';
import { AuthService } from '../../../../../core/services/auth.service';
import { PersonService } from '../../../../../core/services/person.service';
import { SidePanelService } from '../../../../../core/services/side-panel.service';
import { FormSectionComponent } from '../../../../../shared/components/form-section/form-section.component';
import { PageActionsDirective } from '../../../../../shared/directives/page-actions.directive';
import { ResidentsService } from '../../../residents/services/residents.service';
import {
  ResidencePickerComponent,
  ResidencePickerData,
} from '../../components/residence-picker/residence-picker.component';
import { VisitsService } from '../../services/visits.service';

@Component({
  selector: 'app-register-visit',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    LucideAngularModule,
    FormSectionComponent,
    PageActionsDirective,
  ],
  templateUrl: './register-visit.component.html',
  styleUrl: './register-visit.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterVisitComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly visitsService = inject(VisitsService);
  private readonly residentsService = inject(ResidentsService);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly personService = inject(PersonService);
  private readonly sidePanel = inject(SidePanelService);

  // Documento recibido desde la búsqueda por comando
  private readonly initialDocument: unknown =
    this.router.getCurrentNavigation()?.extras.state?.['documentNumber'];

  readonly visitForm = this.fb.nonNullable.group({
    documentNumber: ['', Validators.required],
    newPerson: this.fb.nonNullable.group({
      firstName: [''],
      lastName: [''],
      documentType: [''],
      phone: [''],
      email: ['', Validators.email],
      personType: [0],
    }),
    vehiclePlate: [''],
    residenceId: ['', Validators.required],
  });

  readonly canRegister =
    this.authService.hasRole('Admin') || this.authService.hasRole('Guard');

  readonly errorMessage = signal('');
  readonly residents = signal<Resident[]>([]);
  readonly selectedResident = signal<Resident | null>(null);
  readonly foundPerson = signal<Person | null>(null);
  readonly showNewPersonForm = signal(false);
  readonly searching = signal(false);
  readonly saving = signal(false);

  private readonly formValue = toSignal(
    this.visitForm.valueChanges.pipe(
      startWith(null),
      map(() => this.visitForm.getRawValue())
    ),
    { initialValue: this.visitForm.getRawValue() }
  );
  private readonly formValid = toSignal(
    this.visitForm.statusChanges.pipe(map((status) => status === 'VALID')),
    { initialValue: this.visitForm.valid }
  );

  readonly canSave = computed(
    () =>
      this.canRegister &&
      this.formValid() &&
      !this.saving() &&
      (this.foundPerson() !== null || this.showNewPersonForm())
  );

  readonly summary = computed(() => {
    const value = this.formValue();
    const person = this.foundPerson();
    const resident = this.selectedResident();
    const visitor = person
      ? `${person.firstName} ${person.lastName}`
      : `${value.newPerson.firstName} ${value.newPerson.lastName}`.trim();
    const documentType = person?.documentType ?? value.newPerson.documentType;

    return {
      visitor: visitor || '—',
      document:
        `${documentType} ${value.documentNumber}`.trim() || '—',
      residence: resident
        ? `Apartamento ${resident.apartmentNumber}`
        : '—',
      plate: value.vehiclePlate || '—',
    };
  });

  readonly SearchIcon = Search;
  readonly SaveIcon = Check;
  readonly FoundIcon = CircleCheck;
  readonly InfoIcon = Info;

  ngOnInit(): void {
    if (!this.canRegister) {
      this.visitForm.disable();
    }

    this.residentsService
      .getResidents()
      .subscribe((residents) => this.residents.set(residents));

    if (typeof this.initialDocument === 'string' && this.initialDocument) {
      this.visitForm.controls.documentNumber.setValue(this.initialDocument);
      this.onSearch();
    }
  }

  onSearch(): void {
    const documentNumber = this.visitForm.controls.documentNumber.value.trim();
    if (!documentNumber || this.searching()) {
      return;
    }

    this.searching.set(true);
    this.errorMessage.set('');

    this.personService.searchPersons(documentNumber).subscribe({
      next: (persons) => {
        this.searching.set(false);
        if (persons.length > 0) {
          this.foundPerson.set(persons[0]);
          this.showNewPersonForm.set(false);
          this.visitForm.controls.newPerson.reset();
          this.setNewPersonValidators(false);
        } else {
          this.foundPerson.set(null);
          this.showNewPersonForm.set(true);
          this.setNewPersonValidators(true);
        }
      },
      error: () => {
        this.searching.set(false);
        this.errorMessage.set('No se pudo buscar la persona. Inténtalo de nuevo.');
      },
    });
  }

  onClear(): void {
    this.foundPerson.set(null);
    this.showNewPersonForm.set(false);
    this.visitForm.controls.documentNumber.reset();
    this.visitForm.controls.newPerson.reset();
    this.setNewPersonValidators(false);
  }

  openResidencePicker(): void {
    this.sidePanel
      .open<Resident, ResidencePickerData, ResidencePickerComponent>(
        ResidencePickerComponent,
        {
          ariaLabel: 'Buscar residencia',
          data: {
            residents: this.residents(),
            selectedId: this.selectedResident()?.id ?? null,
          },
        }
      )
      .closed.subscribe((resident) => {
        if (resident) {
          this.selectedResident.set(resident);
          this.visitForm.controls.residenceId.setValue(resident.id);
        }
      });
  }

  onSubmit(): void {
    if (!this.canSave()) {
      this.visitForm.markAllAsTouched();
      return;
    }

    const formValue = this.visitForm.getRawValue();
    const visit: CreateVisit = {
      residenceId: formValue.residenceId,
      vehiclePlate: formValue.vehiclePlate,
    };

    const person = this.foundPerson();
    if (person) {
      visit.personId = person.id;
    } else {
      visit.newPerson = {
        ...formValue.newPerson,
        documentNumber: formValue.documentNumber,
      };
    }

    this.saving.set(true);
    this.errorMessage.set('');

    this.visitsService.createVisit(visit).subscribe({
      next: () => this.router.navigate(['/access-control/visits']),
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set('Error al crear la visita');
        console.error(err);
      },
    });
  }

  private setNewPersonValidators(enable: boolean): void {
    const { firstName, lastName, documentType } =
      this.visitForm.controls.newPerson.controls;

    for (const control of [firstName, lastName, documentType]) {
      control.setValidators(enable ? Validators.required : null);
      control.updateValueAndValidity();
    }
  }
}
