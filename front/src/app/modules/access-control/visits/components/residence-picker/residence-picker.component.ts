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
import { Residence } from '../../../../../core/models/residence.model';
import { SidePanelComponent } from '../../../../../shared/components/side-panel/side-panel.component';
import { ResidencesService } from '../../services/residences.service';

export interface ResidencePickerData {
  selectedId: string | null;
}

/** Selector de residencia; se abre en el panel lateral, busca en el servidor y devuelve la elegida. */
@Component({
  selector: 'app-residence-picker',
  standalone: true,
  imports: [LucideAngularModule, SidePanelComponent],
  templateUrl: './residence-picker.component.html',
  styleUrl: './residence-picker.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResidencePickerComponent implements OnInit {
  private readonly dialogRef = inject<DialogRef<Residence>>(DialogRef);
  private readonly residencesService = inject(ResidencesService);
  private readonly destroyRef = inject(DestroyRef);
  readonly data = inject<ResidencePickerData>(DIALOG_DATA);

  readonly SearchIcon = Search;
  readonly CheckIcon = Check;

  readonly term = signal('');
  readonly results = signal<Residence[]>([]);
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
          this.residencesService.search(term).pipe(
            catchError(() => {
              this.failed.set(true);
              return of({ items: [] as Residence[], totalCount: 0 });
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

  select(residence: Residence): void {
    this.dialogRef.close(residence);
  }
}
