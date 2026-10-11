import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Check, LucideAngularModule } from 'lucide-angular';
import { forkJoin, of } from 'rxjs';
import {
  CatalogItem,
  OCCUPANCY_STATUS_LABELS,
  RELATION_LABELS,
} from '../../../core/models/unit.model';
import { P } from '../../../core/security/permissions';
import { AuthService } from '../../../core/services/auth.service';
import {
  PersonDetail,
  UnitsApiService,
} from '../../../core/services/units-api.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { FormSectionComponent } from '../../../shared/components/form-section/form-section.component';
import { IfPermissionDirective } from '../../../shared/directives/if-permission.directive';
import { PageActionsDirective } from '../../../shared/directives/page-actions.directive';

const PEOPLE_URL = '/access-control/people';

/**
 * Ficha de persona: sus datos (editables con permiso), todas sus unidades con la relación que
 * tiene o tuvo con cada una, y su cuenta en la plataforma. Sin id, es el alta.
 */
@Component({
  selector: 'app-person',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    LucideAngularModule,
    FormSectionComponent,
    IfPermissionDirective,
    PageActionsDirective,
  ],
  templateUrl: './person.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonComponent implements OnInit {
  private readonly api = inject(UnitsApiService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  readonly canWrite = inject(AuthService).hasPermission(P.peopleWrite);

  readonly personId = inject(ActivatedRoute).snapshot.paramMap.get('id');

  readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    documentType: ['CC', Validators.required],
    documentNumber: ['', [Validators.required, Validators.maxLength(30)]],
    phone: [''],
    email: ['', Validators.email],
  });

  readonly access = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    roleKey: ['owner' as 'owner' | 'resident'],
  });

  readonly detail = signal<PersonDetail | null>(null);
  readonly documentTypes = signal<CatalogItem[]>([]);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly notice = signal('');

  /** El documento provisional que dejó la migración de usuarios antiguos. */
  readonly provisionalDocument = computed(
    () => this.detail()?.person.documentType === 'NA'
  );

  readonly P = P;
  readonly relationLabels = RELATION_LABELS;
  readonly statusLabels = OCCUPANCY_STATUS_LABELS;
  readonly SaveIcon = Check;
  readonly cancelUrl = PEOPLE_URL;

  ngOnInit(): void {
    forkJoin({
      documentTypes: this.api.catalog('document-type'),
      detail: this.personId ? this.api.person(this.personId) : of(null),
    }).subscribe({
      next: ({ documentTypes, detail }) => {
        this.documentTypes.set(documentTypes);
        if (detail) {
          this.show(detail);
        }
        if (!this.canWrite) {
          this.form.disable();
        }
      },
      error: (err) =>
        this.errorMessage.set(apiErrorMessage(err, 'No se pudo cargar la persona.')),
    });
  }

  onSubmit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.errorMessage.set('');
    this.notice.set('');

    this.api
      .savePerson(this.form.getRawValue(), this.personId ?? undefined)
      .subscribe({
        next: (person) => {
          this.saving.set(false);
          if (this.personId) {
            this.notice.set('Datos guardados.');
          } else {
            this.router.navigate([PEOPLE_URL, person.id]);
          }
        },
        error: (err) => {
          this.saving.set(false);
          this.errorMessage.set(apiErrorMessage(err, 'No se pudo guardar.'));
        },
      });
  }

  grantAccess(): void {
    if (this.access.invalid || !this.personId) {
      this.access.markAllAsTouched();
      return;
    }
    const { email, roleKey } = this.access.getRawValue();
    this.errorMessage.set('');

    this.api.grantAccess(this.personId, email, roleKey).subscribe({
      next: () => {
        this.notice.set(`Se envió la invitación a ${email}.`);
        this.api.person(this.personId!).subscribe((detail) => this.show(detail));
      },
      error: (err) =>
        this.errorMessage.set(apiErrorMessage(err, 'No se pudo crear la cuenta.')),
    });
  }

  private show(detail: PersonDetail): void {
    this.detail.set(detail);
    this.form.patchValue({
      ...detail.person,
      phone: detail.person.phone ?? '',
      email: detail.person.email ?? '',
    });
    this.access.patchValue({ email: detail.person.email ?? '' });
  }
}
