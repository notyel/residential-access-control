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
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { LucideAngularModule, Plus, Search, X } from 'lucide-angular';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  AUTHORIZATION_KIND_LABELS,
  AUTHORIZATION_STATE_LABELS,
  Authorization,
  AuthorizationKind,
  AuthorizationState,
  WEEK_DAYS,
  authorizationWindow,
} from '../../../core/models/access.model';
import { CatalogItem, Unit, todayIso } from '../../../core/models/unit.model';
import { P } from '../../../core/security/permissions';
import { AccessApiService } from '../../../core/services/access-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { SidePanelService } from '../../../core/services/side-panel.service';
import { UnitsApiService } from '../../../core/services/units-api.service';
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
import {
  UnitPickerComponent,
  UnitPickerData,
} from '../../../shared/components/unit-picker/unit-picker.component';
import { NotificationService } from '../../../shared/services/notifications/notification.service';

interface Option {
  id: string;
  label: string;
}

const STATE_VARIANTS: Record<AuthorizationState, StatusVariant> = {
  Active: 'success',
  NotStarted: 'neutral',
  Expired: 'neutral',
  Revoked: 'danger',
  Consumed: 'neutral',
};

/**
 * Autorizaciones de visitantes. Modo "portal": el propietario o residente autoriza visitas a sus
 * unidades. Modo "admin": la administración las consulta todas y puede crearlas en nombre de
 * alguien de la unidad. Las reglas (vínculo vigente, duración máxima) las aplica el servidor.
 */
@Component({
  selector: 'app-authorizations',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    LucideAngularModule,
    DataTableComponent,
    PaginatorComponent,
    StatusBadgeComponent,
    PageActionsDirective,
  ],
  templateUrl: './authorizations.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthorizationsComponent implements OnInit {
  private readonly api = inject(AccessApiService);
  private readonly unitsApi = inject(UnitsApiService);
  private readonly sidePanel = inject(SidePanelService);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly portal = inject(ActivatedRoute).snapshot.data['mode'] === 'portal';
  readonly canWrite =
    this.portal || inject(AuthService).hasPermission(P.authorizationsWrite);

  readonly items = signal<Authorization[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly state = signal('Active');
  readonly search = signal('');

  readonly creating = signal(false);
  readonly busy = signal(false);
  readonly formError = signal('');
  readonly actionError = signal('');
  /** Unidades en las que se puede autorizar: en el portal, las propias. */
  readonly unitOptions = signal<Option[]>([]);
  /** Modo admin: quién de la unidad elegida pide la autorización. */
  readonly authorizers = signal<Option[]>([]);
  readonly reasons = signal<CatalogItem[]>([]);
  readonly days = signal<number[]>([]);
  readonly revokingId = signal<string | null>(null);

  readonly form = inject(FormBuilder).nonNullable.group({
    unitId: ['', Validators.required],
    authorizedByPersonId: [''],
    visitorName: ['', Validators.required],
    visitorDocument: [''],
    reason: ['otro'],
    kind: [AuthorizationKind.Single],
    startDate: [todayIso(), Validators.required],
    endDate: [''],
    timeFrom: [''],
    timeTo: [''],
    notes: [''],
  });

  private readonly searches = new Subject<string>();

  readonly Kind = AuthorizationKind;
  readonly kinds = [
    AuthorizationKind.Single,
    AuthorizationKind.Period,
    AuthorizationKind.Recurring,
  ];
  readonly kindLabels = AUTHORIZATION_KIND_LABELS;
  readonly stateLabels = AUTHORIZATION_STATE_LABELS;
  readonly stateVariants = STATE_VARIANTS;
  readonly weekDays = WEEK_DAYS;
  readonly window = authorizationWindow;
  readonly today = todayIso();
  readonly PlusIcon = Plus;
  readonly SearchIcon = Search;
  readonly CancelIcon = X;

  ngOnInit(): void {
    this.searches
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((term) => {
        this.search.set(term);
        this.pageIndex.set(0);
        this.load();
      });
    this.load();

    if (!this.canWrite) {
      return;
    }
    this.unitsApi.catalog('visit-reason').subscribe((reasons) => this.reasons.set(reasons));
    if (this.portal) {
      this.unitsApi.myUnits().subscribe((units) => {
        this.unitOptions.set(units.map((unit) => ({ id: unit.id, label: unit.label })));
        if (units.length === 1) {
          this.form.controls.unitId.setValue(units[0].id);
        }
      });
    }
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.revokingId.set(null);
    this.api
      .authorizations(
        {
          pageNumber: this.pageIndex() + 1,
          pageSize: this.pageSize(),
          state: this.state() || undefined,
          search: this.search(),
        },
        this.portal
      )
      .subscribe({
        next: (page) => {
          this.items.set(page.items);
          this.totalCount.set(page.totalCount);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(apiErrorMessage(err, 'Revisa tu conexión e inténtalo de nuevo.'));
          this.loading.set(false);
        },
      });
  }

  setState(event: Event): void {
    this.state.set((event.target as HTMLSelectElement).value);
    this.pageIndex.set(0);
    this.load();
  }

  onSearch(event: Event): void {
    this.searches.next((event.target as HTMLInputElement).value.trim());
  }

  onPageChange(change: PageChange): void {
    this.pageIndex.set(change.pageIndex);
    this.pageSize.set(change.pageSize);
    this.load();
  }

  open(): void {
    this.creating.set(true);
    this.formError.set('');
  }

  cancel(): void {
    this.creating.set(false);
  }

  /** Modo admin: la unidad se busca en todo el conjunto. */
  pickUnit(): void {
    this.sidePanel
      .open<Unit, UnitPickerData, UnitPickerComponent>(UnitPickerComponent, {
        ariaLabel: 'Buscar unidad',
        data: { selectedId: this.form.controls.unitId.value || null },
      })
      .closed.subscribe((unit) => {
        if (!unit) {
          return;
        }
        this.unitOptions.set([{ id: unit.id, label: unit.label }]);
        this.form.patchValue({ unitId: unit.id, authorizedByPersonId: '' });
        this.unitsApi.unitSummary(unit.id).subscribe((summary) => {
          const people = new Map<string, string>();
          for (const link of [...summary.owners, ...summary.occupants]) {
            people.set(link.personId, link.personName);
          }
          this.authorizers.set([...people].map(([id, label]) => ({ id, label })));
        });
      });
  }

  toggleDay(day: number): void {
    this.days.update((days) =>
      days.includes(day) ? days.filter((d) => d !== day) : [...days, day]
    );
  }

  save(): void {
    if (this.form.invalid || this.busy()) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    const single = value.kind === AuthorizationKind.Single;
    if (!this.portal && !value.authorizedByPersonId) {
      this.formError.set('Indique quién de la unidad autoriza.');
      return;
    }

    this.busy.set(true);
    this.formError.set('');
    this.api
      .createAuthorization(
        {
          unitId: value.unitId,
          authorizedByPersonId: this.portal ? null : value.authorizedByPersonId,
          visitorName: value.visitorName,
          visitorDocument: value.visitorDocument || null,
          reason: value.reason,
          notes: value.notes || null,
          kind: value.kind,
          startDate: value.startDate,
          endDate: single ? null : value.endDate || null,
          // el servidor espera HH:mm:ss
          timeFrom: value.timeFrom ? `${value.timeFrom}:00` : null,
          timeTo: value.timeTo ? `${value.timeTo}:00` : null,
          daysOfWeek: value.kind === AuthorizationKind.Recurring ? this.days() : [],
        },
        this.portal
      )
      .subscribe({
        next: (created) => {
          this.busy.set(false);
          this.creating.set(false);
          this.notifications.success(`${created.visitorName} quedó autorizado.`);
          this.form.patchValue({ visitorName: '', visitorDocument: '', notes: '' });
          this.days.set([]);
          this.load();
        },
        error: (err) => {
          this.busy.set(false);
          this.formError.set(apiErrorMessage(err, 'No se pudo crear la autorización.'));
        },
      });
  }

  canRevoke(item: Authorization): boolean {
    return this.canWrite && (item.state === 'Active' || item.state === 'NotStarted');
  }

  revoke(item: Authorization): void {
    if (this.busy()) {
      return;
    }
    this.busy.set(true);
    this.actionError.set('');
    this.api
      .revokeAuthorization(
        item.id,
        this.portal ? 'Revocada por el residente' : 'Revocada por la administración',
        this.portal
      )
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.notifications.success(`Autorización de ${item.visitorName} revocada.`);
          this.load();
        },
        error: (err) => {
          this.busy.set(false);
          this.revokingId.set(null);
          this.actionError.set(apiErrorMessage(err, 'No se pudo revocar la autorización.'));
        },
      });
  }
}
