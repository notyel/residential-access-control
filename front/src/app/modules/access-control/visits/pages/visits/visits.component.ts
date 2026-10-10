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
import { ListFilter, LucideAngularModule, Plus, X } from 'lucide-angular';
import { Visit } from '../../../../../core/models/visit.model';
import { SidePanelService } from '../../../../../core/services/side-panel.service';
import { DataTableComponent } from '../../../../../shared/components/data-table/data-table.component';
import {
  PageChange,
  PaginatorComponent,
} from '../../../../../shared/components/paginator/paginator.component';
import { StatusBadgeComponent } from '../../../../../shared/components/status-badge/status-badge.component';
import { IfRoleDirective } from '../../../../../shared/directives/if-role.directive';
import { PageActionsDirective } from '../../../../../shared/directives/page-actions.directive';
import { PageToolsDirective } from '../../../../../shared/directives/page-tools.directive';
import { initials } from '../../../../../shared/utils/initials';
import {
  EMPTY_VISIT_FILTERS,
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
    IfRoleDirective,
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

  readonly visits = signal<Visit[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly filters = signal<VisitFilters>({ ...EMPTY_VISIT_FILTERS });

  readonly filterChips = computed<FilterChip[]>(() => {
    const { startDate, endDate } = this.filters();
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
    return chips;
  });

  readonly FilterIcon = ListFilter;
  readonly PlusIcon = Plus;
  readonly RemoveIcon = X;
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
    const { startDate, endDate } = this.filters();
    this.loading.set(true);
    this.error.set(null);

    this.visitsService
      .getVisits({
        pageNumber: this.pageIndex() + 1,
        pageSize: this.pageSize(),
        startDate: startDate ?? undefined,
        endDate: endDate ? this.endOfDay(endDate) : undefined,
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
