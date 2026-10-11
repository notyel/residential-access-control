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
import {
  LucideAngularModule,
  MailPlus,
  Plus,
  Power,
  Search,
  ShieldOff,
  SquarePen,
} from 'lucide-angular';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  USER_STATUS_LABELS,
  User,
  UserStatus,
} from '../../../../../core/models/user.model';
import { P } from '../../../../../core/security/permissions';
import { AuthService } from '../../../../../core/services/auth.service';
import { apiErrorMessage } from '../../../../../core/utils/api-error';
import { DataTableComponent } from '../../../../../shared/components/data-table/data-table.component';
import {
  PageChange,
  PaginatorComponent,
} from '../../../../../shared/components/paginator/paginator.component';
import { StatusBadgeComponent } from '../../../../../shared/components/status-badge/status-badge.component';
import { IfPermissionDirective } from '../../../../../shared/directives/if-permission.directive';
import { PageActionsDirective } from '../../../../../shared/directives/page-actions.directive';
import { initials } from '../../../../../shared/utils/initials';
import { UserAction, UsersService } from '../../services/users.service';

/** Acción que espera confirmación en la fila. */
interface PendingAction {
  userId: string;
  action: UserAction;
  label: string;
}

@Component({
  selector: 'app-users',
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
  templateUrl: './users.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersComponent implements OnInit {
  private readonly usersService = inject(UsersService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly users = signal<User[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly search = signal('');
  readonly status = signal<UserStatus | ''>('');
  private readonly searchTerms = new Subject<string>();

  readonly pending = signal<PendingAction | null>(null);
  readonly busyId = signal<string | null>(null);
  readonly notice = signal<{ text: string; isError: boolean } | null>(null);

  readonly P = P;
  readonly statuses: UserStatus[] = ['Active', 'Invited', 'Disabled'];
  readonly statusLabels = USER_STATUS_LABELS;
  readonly initials = initials;
  readonly PlusIcon = Plus;
  readonly SearchIcon = Search;
  readonly EditIcon = SquarePen;
  readonly PowerIcon = Power;
  readonly RevokeIcon = ShieldOff;
  readonly ResendIcon = MailPlus;

  ngOnInit(): void {
    this.searchTerms
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
      });
    this.load();
  }

  isSelf(user: User): boolean {
    return user.id === this.authService.currentUser()?.id;
  }

  roleNames(user: User): string {
    return user.roles.map((role) => role.name).join(', ') || 'Sin rol';
  }

  onSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.search.set(value);
    this.searchTerms.next(value.trim());
  }

  onStatus(event: Event): void {
    this.status.set((event.target as HTMLSelectElement).value as UserStatus | '');
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
    this.pending.set(null);

    this.usersService
      .getPage({
        pageNumber: this.pageIndex() + 1,
        pageSize: this.pageSize(),
        search: this.search(),
        status: this.status() || undefined,
      })
      .subscribe({
        next: (page) => {
          this.users.set(page.items);
          this.totalCount.set(page.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Revisa tu conexión e inténtalo de nuevo.');
          this.loading.set(false);
        },
      });
  }

  ask(user: User, action: UserAction): void {
    const labels: Record<UserAction, string> = {
      activate: 'Activar',
      deactivate: 'Desactivar',
      revoke: 'Revocar accesos',
    };
    this.notice.set(null);
    this.pending.set({ userId: user.id, action, label: labels[action] });
  }

  confirm(user: User): void {
    const pending = this.pending();
    if (!pending || this.busyId()) {
      return;
    }
    this.busyId.set(user.id);

    this.usersService.run(user.id, pending.action).subscribe({
      next: (updated) => {
        this.busyId.set(null);
        this.pending.set(null);
        this.users.update((list) =>
          list.map((item) => (item.id === updated.id ? updated : item))
        );
      },
      error: (err) => this.fail(err),
    });
  }

  resend(user: User): void {
    if (this.busyId()) {
      return;
    }
    this.busyId.set(user.id);
    this.notice.set(null);

    this.usersService.resendInvitation(user.id).subscribe({
      next: () => {
        this.busyId.set(null);
        this.notice.set({
          text: `Invitación enviada a ${user.email}.`,
          isError: false,
        });
      },
      error: (err) => this.fail(err),
    });
  }

  private fail(err: unknown): void {
    this.busyId.set(null);
    this.pending.set(null);
    this.notice.set({
      text: apiErrorMessage(err, 'No se pudo completar la acción.'),
      isError: true,
    });
  }
}
