import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { CatalogItem } from '../../../core/models/unit.model';
import { P } from '../../../core/security/permissions';
import { AuthService } from '../../../core/services/auth.service';
import { Incident, ReportsApiService } from '../../../core/services/reports-api.service';
import { UnitsApiService } from '../../../core/services/units-api.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import {
  PageChange,
  PaginatorComponent,
} from '../../../shared/components/paginator/paginator.component';
import { NotificationService } from '../../../shared/services/notifications/notification.service';

/** Novedades del turno de portería: se agregan y se consultan; no se editan ni se borran. */
@Component({
  selector: 'app-incidents',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    PaginatorComponent,
  ],
  template: `
    <div class="app-page app-page--scroll">
      @if (errorMessage()) {
      <p class="app-alert" role="alert">{{ errorMessage() }}</p>
      }

      @if (canWrite) {
      <section class="app-card" style="padding: var(--app-space-4); margin-bottom: var(--app-space-4)">
        <form class="app-form-grid" [formGroup]="form" (ngSubmit)="add()">
          <mat-form-field>
            <mat-label>Categoría</mat-label>
            <mat-select formControlName="category">
              @for (item of categories(); track item.code) {
              <mat-option [value]="item.code">{{ item.label }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
          <mat-form-field>
            <mat-label>Placa relacionada (opcional)</mat-label>
            <input matInput class="text-mono" formControlName="vehiclePlate" autocomplete="off" />
          </mat-form-field>
          <mat-form-field class="span-2">
            <mat-label>Novedad</mat-label>
            <textarea matInput rows="2" formControlName="text"></textarea>
            <mat-error>Describa la novedad</mat-error>
          </mat-form-field>
          <div class="span-2">
            <button mat-flat-button type="submit" [disabled]="busy()">Registrar novedad</button>
          </div>
        </form>
      </section>
      }

      <section class="app-card" style="padding: var(--app-space-3) var(--app-space-4)">
        <div class="row-confirm" style="margin-bottom: var(--app-space-2)">
          <select class="app-toolbar-select" aria-label="Filtrar por categoría" (change)="setCategory($event)">
            <option value="">Todas las categorías</option>
            @for (item of categories(); track item.code) {
            <option [value]="item.code">{{ item.label }}</option>
            }
          </select>
          <span class="app-spacer"></span>
          <span class="text-muted">{{ totalCount() }}</span>
        </div>
        <ul style="margin: 0; padding: 0; list-style: none">
          @for (incident of incidents(); track incident.id) {
          <li class="cell-stack" style="padding: var(--app-space-2) 0; border-top: 1px solid var(--app-border)">
            <span>{{ incident.text }}</span>
            <span class="text-muted">
              {{ incident.occurredAt | date : 'd MMM y, h:mm a' }} · {{ incident.categoryLabel }} ·
              {{ incident.authorName }}
              @if (incident.unitLabel) { · <span class="text-mono">{{ incident.unitLabel }}</span> }
              @if (incident.vehiclePlate) { ·
              <span class="text-mono">{{ incident.vehiclePlate }}</span> }
            </span>
          </li>
          } @empty {
          <li class="text-muted">{{ loading() ? 'Cargando…' : 'No hay novedades registradas.' }}</li>
          }
        </ul>
        <app-paginator
          [length]="totalCount()"
          [pageIndex]="pageIndex()"
          [pageSize]="pageSize()"
          (page)="onPageChange($event)"
        ></app-paginator>
      </section>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IncidentsComponent implements OnInit {
  private readonly api = inject(ReportsApiService);
  private readonly unitsApi = inject(UnitsApiService);
  private readonly notifications = inject(NotificationService);

  readonly canWrite = inject(AuthService).hasPermission(P.incidentsWrite);

  readonly incidents = signal<Incident[]>([]);
  readonly categories = signal<CatalogItem[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(25);
  readonly category = signal('');
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly errorMessage = signal('');

  readonly form = inject(FormBuilder).nonNullable.group({
    category: ['otro', Validators.required],
    text: ['', [Validators.required, Validators.minLength(5)]],
    vehiclePlate: [''],
  });

  ngOnInit(): void {
    this.unitsApi
      .catalog('incident-category')
      .subscribe((categories) => this.categories.set(categories));
    this.load();
  }

  load(): void {
    this.api.incidents(this.category(), this.pageIndex() + 1, this.pageSize()).subscribe({
      next: (page) => {
        this.incidents.set(page.items);
        this.totalCount.set(page.totalCount);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(apiErrorMessage(err, 'No se pudieron cargar las novedades.'));
      },
    });
  }

  setCategory(event: Event): void {
    this.category.set((event.target as HTMLSelectElement).value);
    this.pageIndex.set(0);
    this.load();
  }

  onPageChange(change: PageChange): void {
    this.pageIndex.set(change.pageIndex);
    this.pageSize.set(change.pageSize);
    this.load();
  }

  add(): void {
    if (this.form.invalid || this.busy()) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    this.busy.set(true);
    this.errorMessage.set('');
    this.api
      .addIncident({ ...value, vehiclePlate: value.vehiclePlate || null })
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.form.reset({ category: value.category, text: '', vehiclePlate: '' });
          this.notifications.success('Novedad registrada.');
          this.pageIndex.set(0);
          this.load();
        },
        error: (err) => {
          this.busy.set(false);
          this.errorMessage.set(apiErrorMessage(err, 'No se pudo registrar la novedad.'));
        },
      });
  }
}
