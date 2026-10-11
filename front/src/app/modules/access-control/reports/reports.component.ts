import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { Download, LucideAngularModule } from 'lucide-angular';
import { todayIso } from '../../../core/models/unit.model';
import { P } from '../../../core/security/permissions';
import { AuthService } from '../../../core/services/auth.service';
import {
  Report,
  ReportDefinition,
  ReportsApiService,
  daysAgoIso,
} from '../../../core/services/reports-api.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { PageActionsDirective } from '../../../shared/directives/page-actions.directive';

/**
 * Reportes del conjunto. Son consultas de solo lectura calculadas en el momento; exportar exige
 * su propio permiso y queda en la bitácora con los filtros usados.
 */
@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [MatButtonModule, LucideAngularModule, PageActionsDirective],
  template: `
    @if (canExport && report()) {
    <ng-template appPageActions>
      <button mat-stroked-button type="button" [disabled]="exporting()" (click)="export()">
        <lucide-icon [img]="ExportIcon" [size]="18"></lucide-icon>
        {{ exporting() ? 'Exportando…' : 'Exportar CSV' }}
      </button>
    </ng-template>
    }

    <div class="app-page app-page--scroll">
      <div class="row-confirm" style="flex-wrap: wrap; margin-bottom: var(--app-space-3)">
        <select class="app-toolbar-select" aria-label="Reporte" (change)="choose($event)">
          @for (item of definitions(); track item.key) {
          <option [value]="item.key" [selected]="item.key === selectedKey()">{{ item.title }}</option>
          }
        </select>
        @if (selected()?.usesDates) {
        <input
          class="app-toolbar-select"
          type="date"
          aria-label="Desde"
          [value]="startDate()"
          [max]="endDate()"
          (change)="setDate('startDate', $event)"
        />
        <input
          class="app-toolbar-select"
          type="date"
          aria-label="Hasta"
          [value]="endDate()"
          [min]="startDate()"
          (change)="setDate('endDate', $event)"
        />
        }
      </div>
      @if (selected(); as definition) {
      <p class="text-muted" style="margin: 0 0 var(--app-space-3)">{{ definition.description }}</p>
      } @if (errorMessage()) {
      <p class="app-alert" role="alert">{{ errorMessage() }}</p>
      }

      @if (report(); as r) {
      <section class="app-card" style="overflow-x: auto">
        <table class="app-table">
          <caption class="sr-only">
            {{
              r.title
            }}
          </caption>
          <thead>
            <tr>
              @for (column of r.columns; track column) {
              <th scope="col">{{ column }}</th>
              }
            </tr>
          </thead>
          <tbody>
            @for (row of r.rows; track $index) {
            <tr>
              @for (cell of row; track $index) {
              <td>{{ cell || '—' }}</td>
              }
            </tr>
            } @empty {
            <tr>
              <td class="text-muted" [attr.colspan]="r.columns.length">
                No hay datos para ese periodo.
              </td>
            </tr>
            }
          </tbody>
        </table>
      </section>
      } @else if (loading()) {
      <p class="text-muted" role="status">Generando…</p>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportsComponent implements OnInit {
  private readonly api = inject(ReportsApiService);

  readonly canExport = inject(AuthService).hasPermission(P.reportsExport);

  readonly definitions = signal<ReportDefinition[]>([]);
  readonly selectedKey = signal('');
  readonly selected = computed(() =>
    this.definitions().find((d) => d.key === this.selectedKey())
  );
  readonly report = signal<Report | null>(null);
  readonly startDate = signal(daysAgoIso(29));
  readonly endDate = signal(todayIso());
  readonly loading = signal(false);
  readonly exporting = signal(false);
  readonly errorMessage = signal('');

  readonly ExportIcon = Download;

  ngOnInit(): void {
    this.api.definitions().subscribe({
      next: (definitions) => {
        this.definitions.set(definitions);
        this.selectedKey.set(definitions[0]?.key ?? '');
        this.run();
      },
      error: (err) =>
        this.errorMessage.set(apiErrorMessage(err, 'No se pudieron cargar los reportes.')),
    });
  }

  choose(event: Event): void {
    this.selectedKey.set((event.target as HTMLSelectElement).value);
    this.run();
  }

  setDate(name: 'startDate' | 'endDate', event: Event): void {
    this[name].set((event.target as HTMLInputElement).value);
    this.run();
  }

  run(): void {
    const key = this.selectedKey();
    if (!key || !this.startDate() || !this.endDate()) {
      return;
    }
    this.loading.set(true);
    this.errorMessage.set('');
    this.report.set(null);
    this.api.run(key, this.startDate(), this.endDate()).subscribe({
      next: (report) => {
        this.report.set(report);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(apiErrorMessage(err, 'No se pudo generar el reporte.'));
      },
    });
  }

  export(): void {
    if (this.exporting()) {
      return;
    }
    this.exporting.set(true);
    this.api.exportReport(this.selectedKey(), this.startDate(), this.endDate()).subscribe({
      next: () => this.exporting.set(false),
      error: (err) => {
        this.exporting.set(false);
        this.errorMessage.set(apiErrorMessage(err, 'No se pudo exportar el reporte.'));
      },
    });
  }
}
