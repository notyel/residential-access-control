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
import { Occupancy, PersonListItem, RELATION_LABELS } from '../../../core/models/unit.model';
import { P } from '../../../core/security/permissions';
import { AuthService } from '../../../core/services/auth.service';
import { UnitsApiService } from '../../../core/services/units-api.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { DataTableComponent } from '../../../shared/components/data-table/data-table.component';
import {
  PageChange,
  PaginatorComponent,
} from '../../../shared/components/paginator/paginator.component';
import { IfPermissionDirective } from '../../../shared/directives/if-permission.directive';
import { PageActionsDirective } from '../../../shared/directives/page-actions.directive';
import { initials } from '../../../shared/utils/initials';

/** Directorio de propietarios y residentes, con la bandeja de solicitudes de los propietarios. */
@Component({
  selector: 'app-people',
  standalone: true,
  imports: [
    RouterLink,
    MatButtonModule,
    LucideAngularModule,
    DataTableComponent,
    PaginatorComponent,
    IfPermissionDirective,
    PageActionsDirective,
  ],
  templateUrl: './people.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PeopleComponent implements OnInit {
  private readonly api = inject(UnitsApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly canDecide = inject(AuthService).hasPermission(P.occupancyWrite);

  readonly people = signal<PersonListItem[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly search = signal('');
  /** Solo quienes tienen un vínculo vigente; apagado incluye a quienes solo han sido visitantes. */
  readonly onlyLinked = signal(true);
  readonly pending = signal<Occupancy[]>([]);
  readonly notice = signal('');
  private readonly searchTerms = new Subject<string>();

  readonly P = P;
  readonly relationLabels = RELATION_LABELS;
  readonly initials = initials;
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
    this.loadPending();
  }

  onSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.search.set(value);
    this.searchTerms.next(value.trim());
  }

  onScope(event: Event): void {
    this.onlyLinked.set((event.target as HTMLSelectElement).value === 'linked');
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
      .people(this.search(), this.onlyLinked(), this.pageIndex() + 1, this.pageSize())
      .subscribe({
        next: (page) => {
          this.people.set(page.items);
          this.totalCount.set(page.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Revisa tu conexión e inténtalo de nuevo.');
          this.loading.set(false);
        },
      });
  }

  decide(request: Occupancy, approve: boolean): void {
    const reason = approve ? undefined : window.prompt('Motivo del rechazo') ?? '';
    if (!approve && !reason) {
      return;
    }
    this.api.decideOccupancy(request.id, approve, reason).subscribe({
      next: () => {
        this.notice.set('');
        this.loadPending();
        this.load();
      },
      error: (err) =>
        this.notice.set(apiErrorMessage(err, 'No se pudo resolver la solicitud.')),
    });
  }

  private loadPending(): void {
    if (this.canDecide) {
      this.api.pendingOccupancies().subscribe((pending) => this.pending.set(pending));
    }
  }
}
