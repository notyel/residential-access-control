import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
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
  BulkUnitsResult,
  CatalogItem,
  Structure,
} from '../../../../../core/models/unit.model';
import { UnitsApiService } from '../../../../../core/services/units-api.service';
import { apiErrorMessage } from '../../../../../core/utils/api-error';
import { FormSectionComponent } from '../../../../../shared/components/form-section/form-section.component';
import { PageActionsDirective } from '../../../../../shared/directives/page-actions.directive';

const UNITS_URL = '/access-control/units';

/** Alta y edición de una unidad, y creación en lote por rango de pisos (con vista previa). */
@Component({
  selector: 'app-unit-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    LucideAngularModule,
    FormSectionComponent,
    PageActionsDirective,
  ],
  templateUrl: './unit-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnitFormComponent implements OnInit {
  private readonly api = inject(UnitsApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);

  readonly unitId = this.route.snapshot.paramMap.get('id');
  readonly bulk =
    !this.unitId && this.route.snapshot.queryParamMap.get('modo') === 'lote';

  readonly form = this.fb.nonNullable.group({
    code: ['', this.bulk ? [] : [Validators.required, Validators.maxLength(50)]],
    type: ['', Validators.required],
    structureId: [''],
    area: [null as number | null],
    notes: [''],
    // creación en lote
    prefix: [''],
    floorFrom: [1, [Validators.min(0), Validators.max(200)]],
    floorTo: [1, [Validators.min(0), Validators.max(200)]],
    unitsPerFloor: [4, [Validators.min(1), Validators.max(99)]],
  });

  readonly types = signal<CatalogItem[]>([]);
  readonly structures = signal<Structure[]>([]);
  readonly structureTypes = signal<CatalogItem[]>([]);
  readonly preview = signal<BulkUnitsResult | null>(null);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly SaveIcon = Check;
  readonly cancelUrl = this.unitId ? `${UNITS_URL}/${this.unitId}` : UNITS_URL;

  // alta rápida de una estructura sin salir del formulario
  readonly newStructure = this.fb.nonNullable.group({
    type: ['', Validators.required],
    name: ['', [Validators.required, Validators.maxLength(100)]],
    parentId: [''],
  });
  readonly addingStructure = signal(false);

  ngOnInit(): void {
    forkJoin({
      types: this.api.catalog('unit-type'),
      structureTypes: this.api.catalog('structure-type'),
      structures: this.api.structures(),
      unit: this.unitId ? this.api.unit(this.unitId) : of(null),
    }).subscribe({
      next: ({ types, structureTypes, structures, unit }) => {
        this.types.set(types);
        this.structureTypes.set(structureTypes);
        this.structures.set(structures);
        if (unit) {
          this.form.patchValue({
            code: unit.code,
            type: unit.type,
            structureId: unit.structureId ?? '',
            area: unit.area ?? null,
            notes: unit.notes ?? '',
          });
        } else if (types.length) {
          this.form.controls.type.setValue(types[0].code);
        }
      },
      error: () => this.errorMessage.set('No se pudieron cargar los datos.'),
    });

    // cualquier cambio invalida la vista previa: hay que volver a pedirla antes de crear
    this.form.valueChanges.subscribe(() => this.preview.set(null));
  }

  saveStructure(): void {
    if (this.newStructure.invalid) {
      this.newStructure.markAllAsTouched();
      return;
    }
    const { type, name, parentId } = this.newStructure.getRawValue();
    this.api.saveStructure({ type, name, parentId: parentId || null }).subscribe({
      next: (structure) => {
        this.structures.update((list) =>
          [...list, structure].sort((a, b) => a.path.localeCompare(b.path))
        );
        this.form.controls.structureId.setValue(structure.id);
        this.newStructure.reset();
        this.addingStructure.set(false);
      },
      error: (err) =>
        this.errorMessage.set(
          apiErrorMessage(err, 'No se pudo crear la estructura.')
        ),
    });
  }

  onSubmit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    this.saving.set(true);
    this.errorMessage.set('');

    if (this.bulk) {
      // primer clic: vista previa; segundo clic (sin cambios): crea
      const isPreview = this.preview() === null;
      this.api
        .bulkUnits({
          type: value.type,
          structureId: value.structureId || null,
          prefix: value.prefix,
          floorFrom: value.floorFrom,
          floorTo: value.floorTo,
          unitsPerFloor: value.unitsPerFloor,
          preview: isPreview,
        })
        .subscribe({
          next: (result) => {
            this.saving.set(false);
            if (isPreview) {
              this.preview.set(result);
            } else {
              this.router.navigate([UNITS_URL]);
            }
          },
          error: (err) => this.fail(err),
        });
      return;
    }

    this.api
      .saveUnit(
        {
          code: value.code,
          type: value.type,
          structureId: value.structureId || null,
          area: value.area,
          notes: value.notes,
        },
        this.unitId ?? undefined
      )
      .subscribe({
        next: (unit) => this.router.navigate([UNITS_URL, unit.id]),
        error: (err) => this.fail(err),
      });
  }

  private fail(err: unknown): void {
    this.saving.set(false);
    this.errorMessage.set(apiErrorMessage(err, 'No se pudo guardar.'));
  }
}
