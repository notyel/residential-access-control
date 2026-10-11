import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { Download, LucideAngularModule, Search } from 'lucide-angular';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  AccessEvent,
  AccessEventType,
  PersonKind,
} from '../../../core/models/access.model';
import { todayIso } from '../../../core/models/unit.model';
import { P } from '../../../core/security/permissions';
import { AuthService } from '../../../core/services/auth.service';
import {
  ReportsApiService,
  daysAgoIso,
} from '../../../core/services/reports-api.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { DataTableComponent } from '../../../shared/components/data-table/data-table.component';
import {
  PageChange,
  PaginatorComponent,
} from '../../../shared/components/paginator/paginator.component';
import {
  StatusBadgeComponent,
  StatusVariant,
} from '../../../shared/components/status-badge/status-badge.component';
import { PageActionsDirective } from '../../../shared/directives/page-actions.directive';

const TYPE_LABELS: Record<AccessEventType, string> = {
  [AccessEventType.Entry]: 'Entrada',
  [AccessEventType.Exit]: 'Salida',
  [AccessEventType.Rejection]: 'Rechazo',
  [AccessEventType.Void]: 'Anulación',
};

const TYPE_VARIANTS: Record<AccessEventType, StatusVariant> = {
  [AccessEventType.Entry]: 'success',
  [AccessEventType.Exit]: 'neutral',
  [AccessEventType.Rejection]: 'danger',
  [AccessEventType.Void]: 'warning',
};

const KIND_LABELS: Record<PersonKind, string> = {
  [PersonKind.Visitor]: 'Visitante',
  [PersonKind.Resident]: 'Residente',
  [PersonKind.Owner]: 'Propietario',
};

/**
 * Historial de accesos: todos los eventos (entradas, salidas, rechazos, excepciones y
 * anulaciones). Qué alcanza a ver cada quien —todo el conjunto, los últimos días o solo sus
 * unidades— lo decide el servidor; el documento llega ya enmascarado si no hay permiso.
 */
@Component({
  selector: 'app-access-history',
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    LucideAngularModule,
    DataTableComponent,
    PaginatorComponent,
    StatusBadgeComponent,
    PageActionsDirective,
  ],
  templateUrl: './access-history.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessHistoryComponent implements OnInit {
  private readonly api = inject(ReportsApiService);
  private readonly destroyRef = inject(DestroyRef);

  readonly canExport = inject(AuthService).hasPermission(P.reportsExport);

  readonly events = signal<AccessEvent[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(25);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly exporting = signal(false);

  readonly startDate = signal(daysAgoIso(6));
  readonly endDate = signal(todayIso());
  readonly search = signal('');
  readonly plate = signal('');
  readonly type = signal('');
  readonly personKind = signal('');

  private readonly texts = new Subject<void>();

  readonly Type = AccessEventType;
  readonly typeLabels = TYPE_LABELS;
  readonly typeVariants = TYPE_VARIANTS;
  readonly kindLabels = KIND_LABELS;
  readonly types = [
    AccessEventType.Entry,
    AccessEventType.Exit,
    AccessEventType.Rejection,
    AccessEventType.Void,
  ];
  readonly kinds = [PersonKind.Visitor, PersonKind.Resident, PersonKind.Owner];
  readonly SearchIcon = Search;
  readonly ExportIcon = Download;

  ngOnInit(): void {
    this.texts
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.reload());
    this.load();
  }

  setFilter(
    name: 'startDate' | 'endDate' | 'type' | 'personKind',
    event: Event
  ): void {
    this[name].set((event.target as HTMLInputElement | HTMLSelectElement).value);
    this.reload();
  }

  setText(name: 'search' | 'plate', event: Event): void {
    this[name].set((event.target as HTMLInputElement).value);
    this.texts.next();
  }

  onPageChange(change: PageChange): void {
    this.pageIndex.set(change.pageIndex);
    this.pageSize.set(change.pageSize);
    this.load();
  }

  load(): void {
    if (!this.startDate() || !this.endDate()) {
      this.error.set('Indique el rango de fechas.');
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.api
      .history({ ...this.filter(), pageNumber: this.pageIndex() + 1, pageSize: this.pageSize() })
      .subscribe({
        next: (page) => {
          this.events.set(page.items);
          this.totalCount.set(page.totalCount);
          this.loading.set(false);
        },
        error: (err) => {
          this.events.set([]);
          this.totalCount.set(0);
          this.loading.set(false);
          this.error.set(apiErrorMessage(err, 'No se pudo consultar el historial.'));
        },
      });
  }

  /** Exporta lo que se está viendo, con los mismos filtros. */
  export(): void {
    if (this.exporting()) {
      return;
    }
    this.exporting.set(true);
    this.api.exportHistory(this.filter()).subscribe({
      next: () => this.exporting.set(false),
      error: (err) => {
        this.exporting.set(false);
        this.error.set(apiErrorMessage(err, 'No se pudo exportar el historial.'));
      },
    });
  }

  private reload(): void {
    this.pageIndex.set(0);
    this.load();
  }

  private filter() {
    return {
      startDate: this.startDate(),
      endDate: this.endDate(),
      search: this.search(),
      plate: this.plate(),
      type: this.type(),
      personKind: this.personKind(),
    };
  }
}
