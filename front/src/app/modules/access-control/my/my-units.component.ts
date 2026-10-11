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
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { forkJoin, of } from 'rxjs';
import {
  CatalogItem,
  MyUnit,
  OCCUPANCY_STATE_LABELS,
  OCCUPANCY_STATUS_LABELS,
  Occupancy,
  OccupancyRelation,
  OccupancyStatus,
  RELATION_LABELS,
  todayIso,
} from '../../../core/models/unit.model';
import { UnitsApiService } from '../../../core/services/units-api.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

/**
 * Portal. Modo "owner": mis propiedades y quienes viven en ellas, con el registro de ocupantes
 * que permita la política del conjunto. Modo "resident": mi residencia y mi vinculación.
 * El servidor solo entrega las unidades con vínculo vigente.
 */
@Component({
  selector: 'app-my-units',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    StatusBadgeComponent,
  ],
  templateUrl: './my-units.component.html',
  styleUrl: '../units/pages/unit-detail/unit-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyUnitsComponent implements OnInit {
  private readonly api = inject(UnitsApiService);
  readonly ownerMode = inject(ActivatedRoute).snapshot.data['mode'] === 'owner';

  private readonly allUnits = signal<MyUnit[]>([]);
  readonly units = computed(() =>
    this.allUnits().filter((unit) =>
      this.ownerMode ? unit.isOwner : unit.relation !== null && unit.relation !== undefined
    )
  );
  readonly occupants = signal<Occupancy[]>([]);
  /** no, approval o direct. */
  readonly policy = signal('no');
  readonly documentTypes = signal<CatalogItem[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly notice = signal('');
  readonly addingTo = signal<string | null>(null);
  readonly busy = signal(false);

  readonly form = inject(FormBuilder).nonNullable.group({
    relation: [OccupancyRelation.Tenant],
    startDate: [todayIso(), Validators.required],
    endDate: [''],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    documentType: ['CC', Validators.required],
    documentNumber: ['', Validators.required],
    phone: [''],
    email: ['', Validators.email],
  });

  readonly Status = OccupancyStatus;
  readonly stateLabels = OCCUPANCY_STATE_LABELS;
  readonly relationLabels = RELATION_LABELS;
  readonly statusLabels = OCCUPANCY_STATUS_LABELS;
  readonly relations = [
    OccupancyRelation.Tenant,
    OccupancyRelation.Family,
    OccupancyRelation.Other,
  ];

  ngOnInit(): void {
    this.load();
  }

  occupantsOf(unit: MyUnit): Occupancy[] {
    return this.occupants().filter((o) => o.unitId === unit.id);
  }

  load(): void {
    forkJoin({
      units: this.api.myUnits(),
      mine: this.ownerMode
        ? this.api.myOccupants()
        : of({ policy: 'no', occupants: [] as Occupancy[] }),
      documentTypes: this.ownerMode ? this.api.catalog('document-type') : of([]),
    }).subscribe({
      next: ({ units, mine, documentTypes }) => {
        this.allUnits.set(units);
        this.occupants.set(mine.occupants);
        this.policy.set(mine.policy);
        this.documentTypes.set(documentTypes);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(apiErrorMessage(err, 'No se pudo cargar la información.'));
      },
    });
  }

  open(unit: MyUnit): void {
    this.addingTo.set(unit.id);
    this.errorMessage.set('');
    this.notice.set('');
    this.form.reset();
  }

  add(): void {
    const unitId = this.addingTo();
    if (!unitId || this.form.invalid || this.busy()) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    this.busy.set(true);

    this.api
      .addMyOccupant({ ...value, unitId, endDate: value.endDate || null })
      .subscribe({
        next: ({ message }) => {
          this.busy.set(false);
          this.addingTo.set(null);
          this.notice.set(message);
          this.load();
        },
        error: (err) => {
          this.busy.set(false);
          this.errorMessage.set(
            apiErrorMessage(err, 'No se pudo registrar el ocupante.')
          );
        },
      });
  }

  end(occupancy: Occupancy): void {
    if (this.busy()) {
      return;
    }
    this.busy.set(true);
    this.api.endMyOccupancy(occupancy.id, { endDate: todayIso() }).subscribe({
      next: () => {
        this.busy.set(false);
        this.notice.set('Ocupación finalizada.');
        this.load();
      },
      error: (err) => {
        this.busy.set(false);
        this.errorMessage.set(
          apiErrorMessage(err, 'No se pudo finalizar la ocupación.')
        );
      },
    });
  }
}
