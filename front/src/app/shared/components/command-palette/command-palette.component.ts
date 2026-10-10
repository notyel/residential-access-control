import { DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { LucideAngularModule, Search } from 'lucide-angular';
import {
  catchError,
  combineLatest,
  debounce,
  map,
  of,
  startWith,
  switchMap,
  tap,
  timer,
} from 'rxjs';
import {
  SEARCH_PROVIDERS,
  SearchResult,
  normalizeSearchText,
} from '../../../core/search/search-provider';

const DEBOUNCE_MS = 250;

interface TitleParts {
  before: string;
  match: string;
  after: string;
}

interface PaletteItem {
  result: SearchResult;
  index: number;
  parts: TitleParts;
}

interface ProviderState {
  results: SearchResult[];
  pending: boolean;
}

interface PaletteGroup {
  name: string;
  items: PaletteItem[];
}

@Component({
  selector: 'app-command-palette',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './command-palette.component.html',
  styleUrl: './command-palette.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommandPaletteComponent {
  private readonly dialogRef = inject(DialogRef);
  private readonly providers = inject(SEARCH_PROVIDERS, { optional: true }) ?? [];
  private readonly list = viewChild<ElementRef<HTMLElement>>('list');

  readonly SearchIcon = Search;

  readonly term = signal('');
  readonly loading = signal(false);
  readonly activeIndex = signal(0);
  private readonly results = signal<SearchResult[]>([]);

  /** Resultados agrupados, conservando el orden en que llegan los grupos. */
  readonly groups = computed<PaletteGroup[]>(() => {
    const term = this.term();
    const groups: PaletteGroup[] = [];

    this.results().forEach((result, index) => {
      let group = groups.find((g) => g.name === result.group);
      if (!group) {
        group = { name: result.group, items: [] };
        groups.push(group);
      }
      group.items.push({
        result,
        index,
        parts: this.splitMatch(result.title, term),
      });
    });

    return groups;
  });

  readonly activeId = computed(() => {
    const active = this.results()[this.activeIndex()];
    return active ? this.optionId(active) : null;
  });

  constructor() {
    toObservable(this.term)
      .pipe(
        // La consulta vacía (estado inicial) se resuelve sin espera
        debounce((term) => timer(term ? DEBOUNCE_MS : 0)),
        tap(() => this.loading.set(true)),
        switchMap((term) =>
          // Cada proveedor publica sus resultados en cuanto los tiene
          combineLatest(
            this.providers.map((provider) =>
              provider.search(term).pipe(
                map((results) => ({ results, pending: false })),
                catchError(() => of({ results: [], pending: false })),
                startWith<ProviderState>({ results: [], pending: true })
              )
            )
          ).pipe(
            // combineLatest no emite si no hay proveedores registrados
            startWith<ProviderState[]>([])
          )
        ),
        takeUntilDestroyed()
      )
      .subscribe((states) => {
        this.results.set(states.flatMap((state) => state.results));
        this.loading.set(states.some((state) => state.pending));
        this.activeIndex.set(0);
      });
  }

  optionId(result: SearchResult): string {
    return `palette-option-${result.id}`;
  }

  onInput(event: Event): void {
    this.term.set((event.target as HTMLInputElement).value);
  }

  onKeydown(event: KeyboardEvent): void {
    const count = this.results().length;
    if (!count) {
      return;
    }

    switch (event.key) {
      case 'ArrowDown':
        this.setActive((this.activeIndex() + 1) % count);
        break;
      case 'ArrowUp':
        this.setActive((this.activeIndex() - 1 + count) % count);
        break;
      case 'Home':
        this.setActive(0);
        break;
      case 'End':
        this.setActive(count - 1);
        break;
      case 'Enter':
        this.run(this.results()[this.activeIndex()]);
        break;
      default:
        return;
    }
    event.preventDefault();
  }

  run(result: SearchResult): void {
    this.dialogRef.close();
    result.run();
  }

  close(): void {
    this.dialogRef.close();
  }

  private setActive(index: number): void {
    this.activeIndex.set(index);
    const id = this.optionId(this.results()[index]);
    // Tras el render, para que la opción recién activada exista y esté al día
    queueMicrotask(() =>
      this.list()
        ?.nativeElement.querySelector(`#${CSS.escape(id)}`)
        ?.scrollIntoView({ block: 'nearest' })
    );
  }

  private splitMatch(title: string, term: string): TitleParts {
    const query = normalizeSearchText(term);
    const start = query ? normalizeSearchText(title).indexOf(query) : -1;

    // La normalización conserva la longitud salvo en caracteres compuestos
    // poco habituales; si no coincide, se muestra el título sin resaltar.
    if (start < 0 || normalizeSearchText(title).length !== title.length) {
      return { before: title, match: '', after: '' };
    }

    return {
      before: title.slice(0, start),
      match: title.slice(start, start + query.length),
      after: title.slice(start + query.length),
    };
  }
}
