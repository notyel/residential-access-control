import { DatePipe, formatDate } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  LOCALE_ID,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import {
  ChevronRight,
  ListFilter,
  LucideAngularModule,
  ScanLine,
  Search,
  X,
} from 'lucide-angular';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  Authorization,
  VERIFICATION_LABELS,
  Verification,
  Visit,
  authorizationWindow,
  visitorDocument,
} from '../../../../../core/models/access.model';
import { P } from '../../../../../core/security/permissions';
import { AccessApiService } from '../../../../../core/services/access-api.service';
import { AuthService } from '../../../../../core/services/auth.service';
import { SidePanelService } from '../../../../../core/services/side-panel.service';
import { DataTableComponent } from '../../../../../shared/components/data-table/data-table.component';
import {
  PageChange,
  PaginatorComponent,
} from '../../../../../shared/components/paginator/paginator.component';
import { StatusBadgeComponent } from '../../../../../shared/components/status-badge/status-badge.component';
import { IfPermissionDirective } from '../../../../../shared/directives/if-permission.directive';
import { PageActionsDirective } from '../../../../../shared/directives/page-actions.directive';
import { PageToolsDirective } from '../../../../../shared/directives/page-tools.directive';
import { initials } from '../../../../../shared/utils/initials';
import { VisitDetailComponent } from '../../components/visit-detail/visit-detail.component';
import {
  EMPTY_VISIT_FILTERS,
  VISIT_STATUS_LABELS,
  VisitFilters,
  VisitFiltersComponent,
} from '../../components/visit-filters/visit-filters.component';

/** Esperados: autorizados para hoy. Presentes: sin salida. Historial: todo, con filtros. */
type Tab = 'expected' | 'inside' | 'history';

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
  private readonly api = inject(AccessApiService);
  private readonly sidePanel = inject(SidePanelService);
  private readonly locale = inject(LOCALE_ID);
  private readonly destroyRef = inject(DestroyRef);

  readonly P = P;
  /** Los esperados son información de portería; el portal ve los suyos en "Autorizar visitantes". */
  readonly canGate = inject(AuthService).hasPermission(P.accessRegister);

  readonly tabs: { id: Tab; label: string }[] = [
    ...(this.canGate ? [{ id: 'expected' as Tab, label: 'Esperados' }] : []),
    { id: 'inside', label: 'Presentes' },
    { id: 'history', label: 'Historial' },
  ];
  readonly tab = signal<Tab>('inside');

  readonly visits = signal<Visit[]>([]);
  readonly expected = signal<Authorization[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly search = signal('');
  readonly filters = signal<VisitFilters>({ ...EMPTY_VISIT_FILTERS });

  readonly isEmpty = computed(() =>
    this.tab() === 'expected' ? this.expected().length === 0 : this.visits().length === 0
  );

  readonly filterChips = computed<FilterChip[]>(() => {
    const { startDate, endDate, status, plate, verification } = this.filters();
    const chips: FilterChip[] = [];
    if (startDate) {
      chips.push({ key: 'startDate', label: `Desde: ${this.formatDay(startDate)}` });
    }
    if (endDate) {
      chips.push({ key: 'endDate', label: `Hasta: ${this.formatDay(endDate)}` });
    }
    if (status) {
      chips.push({ key: 'status', label: `Estado: ${VISIT_STATUS_LABELS[status]}` });
    }
    if (verification !== null) {
      chips.push({ key: 'verification', label: VERIFICATION_LABELS[verification] });
    }
    if (plate) {
      chips.push({ key: 'plate', label: `Placa: ${plate}` });
    }
    return chips;
  });

  private readonly searches = new Subject<string>();

  readonly FilterIcon = ListFilter;
  readonly GateIcon = ScanLine;
  readonly SearchIcon = Search;
  readonly RemoveIcon = X;
  readonly DetailIcon = ChevronRight;
  readonly Exception = Verification.Exception;
  readonly verificationLabels = VERIFICATION_LABELS;
  readonly initials = initials;
  readonly window = authorizationWindow;
  readonly visitorDocument = visitorDocument;

  ngOnInit(): void {
    this.searches
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((term) => {
        this.search.set(term);
        this.pageIndex.set(0);
        this.load();
      });
    this.load();
  }

  select(tab: Tab): void {
    if (tab === this.tab()) {
      return;
    }
    this.tab.set(tab);
    this.pageIndex.set(0);
    this.load();
  }

  onSearch(event: Event): void {
    this.searches.next((event.target as HTMLInputElement).value.trim());
  }

  openFilters(): void {
    this.sidePanel
      .open<VisitFilters, VisitFilters, VisitFiltersComponent>(VisitFiltersComponent, {
        ariaLabel: 'Filtros',
        data: this.filters(),
      })
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
    this.load();
  }

  openDetail(visit: Visit): void {
    this.sidePanel
      .open<boolean, Visit, VisitDetailComponent>(VisitDetailComponent, {
        ariaLabel: 'Detalle de la visita',
        data: visit,
      })
      .closed.subscribe((changed) => {
        if (changed) {
          this.load();
        }
      });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    if (this.tab() === 'expected') {
      this.api.expected().subscribe({
        next: (expected) => {
          this.expected.set(expected);
          this.totalCount.set(expected.length);
          this.loading.set(false);
        },
        error: () => this.fail(),
      });
      return;
    }

    const history = this.tab() === 'history';
    const { startDate, endDate, status, plate, verification } = this.filters();
    this.api
      .visits({
        pageNumber: this.pageIndex() + 1,
        pageSize: this.pageSize(),
        search: this.search(),
        // los filtros solo aplican al historial; "Presentes" siempre es quien no ha salido
        status: history ? status ?? undefined : 'Inside',
        startDate: history ? startDate ?? undefined : undefined,
        endDate: history && endDate ? this.endOfDay(endDate) : undefined,
        plate: history ? plate ?? undefined : undefined,
        verification: history ? verification : null,
      })
      .subscribe({
        next: (page) => {
          this.visits.set(page.items);
          this.totalCount.set(page.totalCount);
          this.loading.set(false);
        },
        error: () => this.fail(),
      });
  }

  private fail(): void {
    this.error.set('Revisa tu conexión e inténtalo de nuevo.');
    this.loading.set(false);
  }

  private applyFilters(filters: VisitFilters): void {
    this.filters.set(filters);
    this.pageIndex.set(0);
    this.load();
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
