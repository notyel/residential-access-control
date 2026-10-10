import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import {
  Copy,
  LucideAngularModule,
  Plus,
  Power,
  SquarePen,
  Trash2,
} from 'lucide-angular';
import { Role } from '../../../../../core/models/role.model';
import { P } from '../../../../../core/security/permissions';
import { AuthService } from '../../../../../core/services/auth.service';
import { apiErrorMessage } from '../../../../../core/utils/api-error';
import { DataTableComponent } from '../../../../../shared/components/data-table/data-table.component';
import { StatusBadgeComponent } from '../../../../../shared/components/status-badge/status-badge.component';
import { IfPermissionDirective } from '../../../../../shared/directives/if-permission.directive';
import { PageActionsDirective } from '../../../../../shared/directives/page-actions.directive';
import { RolesService } from '../../services/roles.service';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [
    RouterLink,
    MatButtonModule,
    LucideAngularModule,
    DataTableComponent,
    StatusBadgeComponent,
    IfPermissionDirective,
    PageActionsDirective,
  ],
  templateUrl: './roles.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RolesComponent implements OnInit {
  private readonly rolesService = inject(RolesService);
  private readonly authService = inject(AuthService);

  readonly roles = signal<Role[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly busyId = signal<string | null>(null);
  /** Rol propio cuyo borrado espera confirmación. */
  readonly deletingId = signal<string | null>(null);

  readonly P = P;
  readonly PlusIcon = Plus;
  readonly EditIcon = SquarePen;
  readonly CopyIcon = Copy;
  readonly PowerIcon = Power;
  readonly DeleteIcon = Trash2;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.deletingId.set(null);

    this.rolesService.getAll().subscribe({
      next: (roles) => {
        this.roles.set(roles);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Revisa tu conexión e inténtalo de nuevo.');
        this.loading.set(false);
      },
    });
  }

  toggleActive(role: Role): void {
    this.run(role, this.rolesService.setActive(role.id, !role.isActive), (updated) =>
      this.roles.update((list) =>
        list.map((item) => (item.id === updated.id ? updated : item))
      )
    );
  }

  askDelete(role: Role): void {
    this.actionError.set(null);
    this.deletingId.set(role.id);
  }

  confirmDelete(role: Role): void {
    this.run(role, this.rolesService.delete(role.id), () =>
      this.roles.update((list) => list.filter((item) => item.id !== role.id))
    );
  }

  private run<T>(
    role: Role,
    request: import('rxjs').Observable<T>,
    onDone: (result: T) => void
  ): void {
    if (this.busyId()) {
      return;
    }
    this.busyId.set(role.id);
    this.actionError.set(null);

    request.subscribe({
      next: (result) => {
        this.busyId.set(null);
        this.deletingId.set(null);
        onDone(result);
        // si cambió un rol propio, los permisos de la sesión pueden haber cambiado
        this.authService.refresh().subscribe();
      },
      error: (err) => {
        this.busyId.set(null);
        this.deletingId.set(null);
        this.actionError.set(
          apiErrorMessage(err, 'No se pudo completar la acción.')
        );
      },
    });
  }
}
