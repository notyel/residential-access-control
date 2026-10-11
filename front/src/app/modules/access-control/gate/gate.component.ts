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
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { LucideAngularModule, Search, UserPlus, X } from 'lucide-angular';
import {
  Subject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  forkJoin,
  of,
  switchMap,
} from 'rxjs';
import {
  AccessEventType,
  Authorization,
  GateContext,
  LookupResult,
  PersonKind,
  ResidentMatch,
  Visit,
  authorizationWindow,
  visitorDocument,
  visitorName,
} from '../../../core/models/access.model';
import { Person } from '../../../core/models/person.model';
import { AccessApiService } from '../../../core/services/access-api.service';
import { SidePanelService } from '../../../core/services/side-panel.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { NotificationService } from '../../../shared/services/notifications/notification.service';
import {
  GateEntryComponent,
  GateEntryData,
  GateEntryResult,
} from './gate-entry.component';

const EMPTY_LOOKUP: LookupResult = { authorizations: [], residents: [], people: [] };
const DEFAULT_CONTEXT: GateContext = {
  withoutAuthorization: 'resident_confirmation',
  documentRequired: true,
};

/**
 * Pantalla de trabajo de portería: una sola búsqueda (documento, nombre o unidad) que muestra
 * si la persona puede ingresar, y desde ahí se registra el ingreso, la salida o el rechazo.
 * Sin búsqueda muestra a quién se espera hoy y quién sigue dentro.
 */
@Component({
  selector: 'app-gate',
  standalone: true,
  imports: [DatePipe, MatButtonModule, LucideAngularModule, StatusBadgeComponent],
  templateUrl: './gate.component.html',
  styleUrl: './gate.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GateComponent implements OnInit {
  private readonly api = inject(AccessApiService);
  private readonly sidePanel = inject(SidePanelService);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  // Documento recibido desde la búsqueda por comando
  private readonly initialDocument: unknown =
    inject(Router).getCurrentNavigation()?.extras.state?.['documentNumber'];

  readonly term = signal('');
  readonly searching = signal(false);
  readonly results = signal<LookupResult>(EMPTY_LOOKUP);
  readonly expected = signal<Authorization[]>([]);
  readonly inside = signal<Visit[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal('');
  /** Visita cuya salida espera confirmación. */
  readonly confirmingId = signal<string | null>(null);
  readonly busy = signal(false);

  private context = DEFAULT_CONTEXT;
  private readonly terms = new Subject<string>();

  readonly SearchIcon = Search;
  readonly ClearIcon = X;
  readonly NewIcon = UserPlus;
  readonly Kind = PersonKind;
  readonly Movement = AccessEventType;
  readonly window = authorizationWindow;
  readonly visitorName = visitorName;
  readonly visitorDocument = visitorDocument;

  ngOnInit(): void {
    this.api.context().subscribe({
      next: (context) => (this.context = context),
      // sin las reglas se usa lo más estricto; el servidor decide igual
      error: () => undefined,
    });

    this.terms
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap((term) => {
          if (term.length < 2) {
            return of(EMPTY_LOOKUP);
          }
          this.searching.set(true);
          return this.api.lookup(term).pipe(catchError(() => of(EMPTY_LOOKUP)));
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((results) => {
        this.results.set(results);
        this.searching.set(false);
      });

    this.refresh();

    if (typeof this.initialDocument === 'string' && this.initialDocument) {
      this.term.set(this.initialDocument);
      this.terms.next(this.initialDocument);
    }
  }

  get hasTerm(): boolean {
    return this.term().trim().length >= 2;
  }

  onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.term.set(value);
    this.terms.next(value.trim());
  }

  clearSearch(): void {
    this.term.set('');
    this.terms.next('');
  }

  refresh(): void {
    forkJoin({
      expected: this.api.expected(),
      inside: this.api.visits({ pageNumber: 1, pageSize: 100, status: 'Inside' }),
    }).subscribe({
      next: ({ expected, inside }) => {
        this.expected.set(expected);
        this.inside.set(inside.items);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(
          apiErrorMessage(err, 'No se pudo cargar la información de portería.')
        );
      },
    });
  }

  enterWith(authorization: Authorization): void {
    this.openEntry({ authorization });
  }

  enterPerson(person: Person): void {
    this.openEntry({ person });
  }

  /** Visitante que no aparece en la búsqueda: si lo buscado parece un documento, se conserva. */
  enterNew(): void {
    const term = this.term().trim();
    this.openEntry({ document: /^[\w-]*\d[\w-]*$/.test(term) ? term : undefined });
  }

  residentMovement(
    resident: ResidentMatch,
    type: AccessEventType.Entry | AccessEventType.Exit
  ): void {
    if (this.busy()) {
      return;
    }
    this.busy.set(true);
    this.errorMessage.set('');
    this.api.residentMovement(resident.personId, resident.unitId, type).subscribe({
      next: () => {
        this.busy.set(false);
        this.notifications.success(
          `${type === AccessEventType.Entry ? 'Entrada' : 'Salida'} de ${resident.name} registrada.`
        );
      },
      error: (err) => {
        this.busy.set(false);
        this.errorMessage.set(apiErrorMessage(err, 'No se pudo registrar el movimiento.'));
      },
    });
  }

  askCheckOut(visit: Visit): void {
    this.errorMessage.set('');
    this.confirmingId.set(visit.id);
  }

  checkOut(visit: Visit): void {
    if (this.busy()) {
      return;
    }
    this.busy.set(true);
    this.api.checkOut(visit.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.confirmingId.set(null);
        this.inside.update((visits) => visits.filter((item) => item.id !== visit.id));
        this.notifications.success(`Salida de ${visitorName(visit)} registrada.`);
      },
      error: (err) => {
        this.busy.set(false);
        this.confirmingId.set(null);
        this.errorMessage.set(apiErrorMessage(err, 'No se pudo registrar la salida.'));
        // 409: alguien más ya la registró; se muestra el estado real
        this.refresh();
      },
    });
  }

  private openEntry(data: Omit<GateEntryData, 'context'>): void {
    this.errorMessage.set('');
    this.sidePanel
      .open<GateEntryResult, GateEntryData, GateEntryComponent>(GateEntryComponent, {
        ariaLabel: 'Registrar ingreso',
        data: { ...data, context: this.context },
      })
      .closed.subscribe((result) => {
        if (!result) {
          return;
        }
        if ('visit' in result) {
          this.notifications.success(
            `Ingreso de ${visitorName(result.visit)} a ${result.visit.unitLabel} registrado.`
          );
        } else {
          this.notifications.info(`Rechazo de ${result.rejected} registrado.`);
        }
        this.clearSearch();
        this.refresh();
      });
  }
}
