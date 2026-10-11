import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LucideAngularModule, Search, X } from 'lucide-angular';
import {
  Subject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  of,
  switchMap,
} from 'rxjs';
import { PersonListItem } from '../../../core/models/unit.model';
import { UnitsApiService } from '../../../core/services/units-api.service';

/**
 * Busca una persona del conjunto por nombre o documento y emite la elegida. Se usa donde hay
 * que vincular a alguien que ya existe (propietario, ocupante); para crear una persona nueva
 * está su propio formulario.
 */
@Component({
  selector: 'app-person-picker',
  standalone: true,
  imports: [LucideAngularModule],
  template: `
    @if (selected(); as person) {
    <span class="app-chip person-picker__chosen">
      {{ person.firstName }} {{ person.lastName }}
      <span class="text-muted text-mono"
        >{{ person.documentType }} {{ person.documentNumber }}</span
      >
      <button type="button" aria-label="Elegir otra persona" (click)="clear()">
        <lucide-icon [img]="ClearIcon" [size]="12"></lucide-icon>
      </button>
    </span>
    } @else {
    <label class="app-toolbar-search person-picker__search">
      <lucide-icon [img]="SearchIcon" [size]="16"></lucide-icon>
      <input
        type="search"
        autocomplete="off"
        [attr.aria-label]="label()"
        [placeholder]="label()"
        [value]="term()"
        (input)="onInput($event)"
      />
    </label>
    @if (term().trim().length >= 2) {
    <ul class="person-picker__results" role="listbox" [attr.aria-label]="label()">
      @for (person of results(); track person.id) {
      <li>
        <button type="button" role="option" (click)="choose(person)">
          <span>{{ person.firstName }} {{ person.lastName }}</span>
          <span class="text-muted text-mono"
            >{{ person.documentType }} {{ person.documentNumber }}</span
          >
        </button>
      </li>
      } @empty {
      <li class="text-muted person-picker__empty" role="status">
        {{ searching() ? 'Buscando…' : 'Nadie coincide. Regístrela primero en Propietarios y residentes.' }}
      </li>
      }
    </ul>
    } }
  `,
  styles: `
    :host {
      display: block;
      min-width: 0;
    }

    .person-picker__search {
      width: 100%;
      height: 40px;

      input {
        flex: 1;
        width: auto;
        max-width: none;
      }
    }

    .person-picker__results {
      display: flex;
      flex-direction: column;
      margin: var(--app-space-1) 0 0;
      padding: var(--app-space-1);
      border: 1px solid var(--app-border);
      border-radius: var(--app-radius);
      background: var(--app-surface);
      list-style: none;

      button {
        display: flex;
        justify-content: space-between;
        gap: var(--app-space-3);
        width: 100%;
        min-height: 40px;
        padding: 0 var(--app-space-2);
        border: 0;
        border-radius: var(--app-radius);
        background: transparent;
        color: var(--app-text);
        font: inherit;
        text-align: left;
        align-items: center;
        cursor: pointer;

        &:hover,
        &:focus-visible {
          background: var(--app-surface-2);
        }
      }
    }

    .person-picker__empty {
      padding: var(--app-space-2);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonPickerComponent implements OnInit {
  private readonly api = inject(UnitsApiService);
  private readonly destroyRef = inject(DestroyRef);

  readonly label = input('Buscar persona por nombre o documento');
  readonly picked = output<PersonListItem | null>();

  readonly term = signal('');
  readonly results = signal<PersonListItem[]>([]);
  readonly selected = signal<PersonListItem | null>(null);
  readonly searching = signal(false);

  private readonly terms = new Subject<string>();
  readonly SearchIcon = Search;
  readonly ClearIcon = X;

  ngOnInit(): void {
    this.terms
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap((term) => {
          if (term.length < 2) {
            return of([] as PersonListItem[]);
          }
          this.searching.set(true);
          return this.api.people(term, false, 1, 8).pipe(
            catchError(() => of({ items: [] as PersonListItem[], totalCount: 0 })),
            switchMap((page) => of(page.items))
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((items) => {
        this.results.set(items);
        this.searching.set(false);
      });
  }

  onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.term.set(value);
    this.terms.next(value.trim());
  }

  choose(person: PersonListItem): void {
    this.selected.set(person);
    this.picked.emit(person);
  }

  clear(): void {
    this.selected.set(null);
    this.term.set('');
    this.results.set([]);
    this.picked.emit(null);
  }
}
