import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Check, LucideAngularModule, Search } from 'lucide-angular';
import {
  Subject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  of,
  startWith,
  switchMap,
  tap,
} from 'rxjs';
import { Unit } from '../../../core/models/unit.model';
import { UnitsApiService } from '../../../core/services/units-api.service';
import { SidePanelComponent } from '../side-panel/side-panel.component';

export interface UnitPickerData {
  selectedId: string | null;
}

/** Selector de la unidad de destino; se abre en el panel lateral, busca en el servidor y devuelve la elegida. */
@Component({
  selector: 'app-unit-picker',
  standalone: true,
  imports: [LucideAngularModule, SidePanelComponent],
  templateUrl: './unit-picker.component.html',
  styleUrl: './unit-picker.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnitPickerComponent implements OnInit {
  private readonly dialogRef = inject<DialogRef<Unit>>(DialogRef);
  private readonly api = inject(UnitsApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly data = inject<UnitPickerData>(DIALOG_DATA);

  readonly SearchIcon = Search;
  readonly CheckIcon = Check;

  readonly term = signal('');
  readonly results = signal<Unit[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly failed = signal(false);

  private readonly terms = new Subject<string>();

  ngOnInit(): void {
    this.terms
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        startWith(''),
        tap(() => {
          this.loading.set(true);
          this.failed.set(false);
        }),
        switchMap((term) =>
          // solo unidades activas: una inactiva no admite visitas
          this.api.units({ pageNumber: 1, pageSize: 30, search: term, isActive: true }).pipe(
            catchError(() => {
              this.failed.set(true);
              return of({ items: [] as Unit[], totalCount: 0 });
            })
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((page) => {
        this.results.set(page.items);
        this.totalCount.set(page.totalCount);
        this.loading.set(false);
      });
  }

  onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.term.set(value);
    this.terms.next(value.trim());
  }

  select(unit: Unit): void {
    this.dialogRef.close(unit);
  }
}
