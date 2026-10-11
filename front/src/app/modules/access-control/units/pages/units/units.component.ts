import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { LucideAngularModule, Plus, Search } from 'lucide-angular';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  OCCUPANCY_STATE_LABELS,
  Unit,
  UnitOccupancyState,
} from '../../../../../core/models/unit.model';
import { P } from '../../../../../core/security/permissions';
import { UnitsApiService } from '../../../../../core/services/units-api.service';
import { DataTableComponent } from '../../../../../shared/components/data-table/data-table.component';
import {
  PageChange,
  PaginatorComponent,
} from '../../../../../shared/components/paginator/paginator.component';
import { StatusBadgeComponent } from '../../../../../shared/components/status-badge/status-badge.component';
import { IfPermissionDirective } from '../../../../../shared/directives/if-permission.directive';
import { PageActionsDirective } from '../../../../../shared/directives/page-actions.directive';

@Component({
  selector: 'app-units',
  standalone: true,
  imports: [
    RouterLink,
    MatButtonModule,
    LucideAngularModule,
    DataTableComponent,
    PaginatorComponent,
    StatusBadgeComponent,
    IfPermissionDirective,
    PageActionsDirective,
  ],
  templateUrl: './units.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnitsComponent implements OnInit {
  private readonly api = inject(UnitsApiService);
  private readonly destroyRef = inject(DestroyRef);

  readonly units = signal<Unit[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly search = signal('');
  readonly state = signal('');
  private readonly searchTerms = new Subject<string>();

  readonly P = P;
  readonly State = UnitOccupancyState;
  readonly stateLabels = OCCUPANCY_STATE_LABELS;
  readonly states = [
    UnitOccupancyState.OwnerOccupied,
    UnitOccupancyState.Rented,
    UnitOccupancyState.Vacant,
  ];
  readonly PlusIcon = Plus;
  readonly SearchIcon = Search;

  ngOnInit(): void {
    this.searchTerms
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
      });
    this.load();
  }

  onSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.search.set(value);
    this.searchTerms.next(value.trim());
  }

  onState(event: Event): void {
    this.state.set((event.target as HTMLSelectElement).value);
    this.pageIndex.set(0);
    this.load();
  }

  onPageChange(change: PageChange): void {
    this.pageIndex.set(change.pageIndex);
    this.pageSize.set(change.pageSize);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api
      .units({
        pageNumber: this.pageIndex() + 1,
        pageSize: this.pageSize(),
        search: this.search(),
        state: this.state() === '' ? null : (Number(this.state()) as UnitOccupancyState),
      })
      .subscribe({
        next: (page) => {
          this.units.set(page.items);
          this.totalCount.set(page.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Revisa tu conexión e inténtalo de nuevo.');
          this.loading.set(false);
        },
      });
  }
}
