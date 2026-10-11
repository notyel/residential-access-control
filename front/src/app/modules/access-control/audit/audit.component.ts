import { DatePipe } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { ChevronDown, ChevronRight, LucideAngularModule } from 'lucide-angular';
import { map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { PaginatedResultDto } from '../../../core/types/paginated-result.dto';
import { ResponseModel } from '../../../core/types/response.model';
import { DataTableComponent } from '../../../shared/components/data-table/data-table.component';
import {
  PageChange,
  PaginatorComponent,
} from '../../../shared/components/paginator/paginator.component';

interface AuditEntry {
  id: string;
  occurredAt: string;
  userId?: string | null;
  userName?: string | null;
  action: string;
  entityType?: string | null;
  entityId?: string | null;
  changes?: string | null;
  ipAddress?: string | null;
}

interface ChangeLine {
  field: string;
  old: string;
  new: string;
}

const ACTION_LABELS: Record<string, string> = {
  created: 'Creación',
  updated: 'Cambio',
  deleted: 'Eliminación',
  'auth.login': 'Inicio de sesión',
  'auth.login_failed': 'Inicio de sesión fallido',
  'auth.invitation_accepted': 'Invitación aceptada',
  'auth.password_reset_requested': 'Recuperación solicitada',
  'auth.password_reset': 'Contraseña restablecida',
  'user.invitation_sent': 'Invitación reenviada',
  'user.access_revoked': 'Accesos revocados',
  'visit.checkout': 'Salida de visita',
};

const ENTITY_LABELS: Record<string, string> = {
  User: 'Usuario',
  Role: 'Rol',
  RolePermission: 'Permiso de rol',
  UserRole: 'Rol de usuario',
  Complex: 'Conjunto',
  Visit: 'Visita',
  Person: 'Persona',
  Residence: 'Residencia',
};

/** Bitácora de auditoría: solo consulta. Cada fila se abre para ver qué cambió. */
@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [DatePipe, LucideAngularModule, DataTableComponent, PaginatorComponent],
  templateUrl: './audit.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuditComponent implements OnInit {
  private readonly http = inject(HttpClient);

  readonly entries = signal<AuditEntry[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(25);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly expandedId = signal<string | null>(null);

  readonly entityType = signal('');
  readonly action = signal('');
  readonly startDate = signal('');
  readonly endDate = signal('');

  readonly entityOptions = Object.entries(ENTITY_LABELS);
  readonly actionOptions = Object.entries(ACTION_LABELS);
  readonly OpenIcon = ChevronDown;
  readonly ClosedIcon = ChevronRight;

  ngOnInit(): void {
    this.load();
  }

  actionLabel(entry: AuditEntry): string {
    return ACTION_LABELS[entry.action] ?? entry.action;
  }

  entityLabel(entry: AuditEntry): string {
    return entry.entityType
      ? ENTITY_LABELS[entry.entityType] ?? entry.entityType
      : '—';
  }

  /** Campos que cambiaron, listos para mostrar como "antes → después". */
  changeLines(entry: AuditEntry): ChangeLine[] {
    if (!entry.changes) {
      return [];
    }
    try {
      const parsed = JSON.parse(entry.changes) as Record<string, unknown>;
      return Object.entries(parsed).map(([field, value]) => {
        const change =
          value !== null && typeof value === 'object'
            ? (value as { old?: unknown; new?: unknown })
            : { new: value };
        return {
          field,
          old: this.show(change.old),
          new: this.show(change.new),
        };
      });
    } catch {
      return [{ field: 'detalle', old: '', new: entry.changes }];
    }
  }

  toggle(entry: AuditEntry): void {
    this.expandedId.update((id) => (id === entry.id ? null : entry.id));
  }

  setFilter(target: 'entityType' | 'action' | 'startDate' | 'endDate', event: Event): void {
    this[target].set((event.target as HTMLInputElement | HTMLSelectElement).value);
    this.pageIndex.set(0);
    this.load();
  }

  onPageChange(change: PageChange): void {
    this.pageIndex.set(change.pageIndex);
    this.pageSize.set(change.pageSize);
    this.load();
  }

  load(): void {
    let params = new HttpParams()
      .set('pageNumber', this.pageIndex() + 1)
      .set('pageSize', this.pageSize());
    if (this.entityType()) {
      params = params.set('entityType', this.entityType());
    }
    if (this.action()) {
      params = params.set('action', this.action());
    }
    if (this.startDate()) {
      params = params.set('startDate', `${this.startDate()}T00:00:00`);
    }
    if (this.endDate()) {
      // el filtro "hasta" incluye el día completo
      params = params.set('endDate', `${this.endDate()}T23:59:59`);
    }

    this.loading.set(true);
    this.error.set(null);
    this.expandedId.set(null);

    this.http
      .get<ResponseModel<PaginatedResultDto<AuditEntry>>>(
        `${environment.apiUrl}/audit`,
        { params }
      )
      .pipe(map((response) => response.data ?? { items: [], totalCount: 0 }))
      .subscribe({
        next: (page) => {
          this.entries.set(page.items);
          this.totalCount.set(page.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Revisa tu conexión e inténtalo de nuevo.');
          this.loading.set(false);
        },
      });
  }

  private show(value: unknown): string {
    if (value === undefined || value === null || value === '') {
      return '';
    }
    return typeof value === 'object' ? JSON.stringify(value) : String(value);
  }
}
