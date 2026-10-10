import { DatePipe, formatDate } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  LOCALE_ID,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import {
  ListFilter,
  LogOut,
  LucideAngularModule,
  Plus,
  X,
} from 'lucide-angular';
import { Visit } from '../../../../../core/models/visit.model';
import { AuthService } from '../../../../../core/services/auth.service';
import { apiErrorMessage } from '../../../../../core/utils/api-error';
import { SidePanelService } from '../../../../../core/services/side-panel.service';
import { DataTableComponent } from '../../../../../shared/components/data-table/data-table.component';
import {
  PageChange,
  PaginatorComponent,
} from '../../../../../shared/components/paginator/paginator.component';
import { StatusBadgeComponent } from '../../../../../shared/components/status-badge/status-badge.component';
import { IfPermissionDirective } from '../../../../../shared/directives/if-permission.directive';
import { P } from '../../../../../core/security/permissions';
import { PageActionsDirective } from '../../../../../shared/directives/page-actions.directive';
import { PageToolsDirective } from '../../../../../shared/directives/page-tools.directive';
import { initials } from '../../../../../shared/utils/initials';
import {
  EMPTY_VISIT_FILTERS,
  VISIT_STATUS_LABELS,
  VisitFilters,
  VisitFiltersComponent,
} from '../../components/visit-filters/visit-filters.component';
import { VisitsService } from '../../services/visits.service';

interface FilterChip {
  key: keyof VisitFilters;
  label: string;
}

@Component({
  selector: 'app-visits',
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    MatButtonModule,
    LucideAngularModule,
    DataTableComponent,
    PaginatorComponent,
    StatusBadgeComponent,
    IfPermissionDirective,
    PageActionsDirective,
    PageToolsDirective,
  ],
  templateUrl: './visits.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VisitsComponent implements OnInit {
  private readonly visitsService = inject(VisitsService);
  private readonly sidePanel = inject(SidePanelService);
  private readonly locale = inject(LOCALE_ID);
  private readonly authService = inject(AuthService);

  readonly P = P;
  readonly canCheckout = this.authService.hasPermission(P.visitsCheckout);

  readonly visits = signal<Visit[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly filters = signal<VisitFilters>({ ...EMPTY_VISIT_FILTERS });
  /** Visita cuya salida está esperando confirmación. */
  readonly confirmingId = signal<string | null>(null);
  readonly checkingOutId = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  readonly filterChips = computed<FilterChip[]>(() => {
    const { startDate, endDate, status } = this.filters();
    const chips: FilterChip[] = [];
    if (startDate) {
      chips.push({
        key: 'startDate',
        label: `Desde: ${this.formatDay(startDate)}`,
      });
    }
    if (endDate) {
      chips.push({ key: 'endDate', label: `Hasta: ${this.formatDay(endDate)}` });
    }
    if (status) {
      chips.push({
        key: 'status',
        label: `Estado: ${VISIT_STATUS_LABELS[status]}`,
      });
    }
    return chips;
  });

  readonly FilterIcon = ListFilter;
  readonly PlusIcon = Plus;
  readonly RemoveIcon = X;
  readonly CheckoutIcon = LogOut;
  readonly initials = initials;

  ngOnInit(): void {
    this.loadVisits();
  }

  openFilters(): void {
    this.sidePanel
      .open<VisitFilters, VisitFilters, VisitFiltersComponent>(
        VisitFiltersComponent,
        { ariaLabel: 'Filtros', data: this.filters() }
      )
      .closed.subscribe((filters) => {
        if (filters) {
          this.applyFilters(filters);
        }
      });
  }

  removeFilter(key: keyof VisitFilters): void {
    this.applyFilters({ ...this.filters(), [key]: null });
  }

  onPageChange(change: PageChange): void {
    this.pageIndex.set(change.pageIndex);
    this.pageSize.set(change.pageSize);
    this.loadVisits();
  }

  loadVisits(): void {
    const { startDate, endDate, status } = this.filters();
    this.confirmingId.set(null);
    this.loading.set(true);
    this.error.set(null);

    this.visitsService
      .getVisits({
        pageNumber: this.pageIndex() + 1,
        pageSize: this.pageSize(),
        startDate: startDate ?? undefined,
        endDate: endDate ? this.endOfDay(endDate) : undefined,
        status: status ?? undefined,
      })
      .subscribe({
        next: (page) => {
          this.visits.set(page.items);
          this.totalCount.set(page.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Revisa tu conexión e inténtalo de nuevo.');
          this.loading.set(false);
        },
      });
  }

  /** Primer paso: pide confirmación en la misma fila. */
  askCheckout(visit: Visit): void {
    this.actionError.set(null);
    this.confirmingId.set(visit.id);
  }

  cancelCheckout(): void {
    this.confirmingId.set(null);
  }

  confirmCheckout(visit: Visit): void {
    if (this.checkingOutId()) {
      return;
    }
    this.checkingOutId.set(visit.id);
    this.actionError.set(null);

    this.visitsService.checkout(visit.id).subscribe({
      next: (updated) => {
        this.checkingOutId.set(null);
        this.confirmingId.set(null);
        if (this.filters().status === 'Inside') {
          // con el filtro "En sitio" la visita deja de pertenecer a la lista
          this.loadVisits();
        } else {
          this.visits.update((visits) =>
            visits.map((item) => (item.id === updated.id ? updated : item))
          );
        }
      },
      error: (err) => {
        this.checkingOutId.set(null);
        this.confirmingId.set(null);
        this.actionError.set(
          apiErrorMessage(err, 'No se pudo registrar la salida.')
        );
        // 409: otra persona ya registró la salida; se refresca para mostrar el estado real
        this.loadVisits();
      },
    });
  }

  private applyFilters(filters: VisitFilters): void {
    this.filters.set(filters);
    this.pageIndex.set(0);
    this.loadVisits();
  }

  /** El filtro "hasta" incluye el día completo. */
  private endOfDay(date: Date): Date {
    const end = new Date(date);
    end.setHours(23, 59, 59, 999);
    return end;
  }

  private formatDay(date: Date): string {
    return formatDate(date, 'd MMM y', this.locale);
  }
}
