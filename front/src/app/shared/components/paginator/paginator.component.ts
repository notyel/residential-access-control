import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { ChevronLeft, ChevronRight, LucideAngularModule } from 'lucide-angular';

export interface PageChange {
  /** Índice de página, desde 0. */
  pageIndex: number;
  pageSize: number;
}

@Component({
  selector: 'app-paginator',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './paginator.component.html',
  styleUrl: './paginator.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginatorComponent {
  readonly length = input.required<number>();
  readonly pageIndex = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly pageSizeOptions = input<number[]>([10, 25, 50]);

  readonly page = output<PageChange>();

  readonly PreviousIcon = ChevronLeft;
  readonly NextIcon = ChevronRight;

  readonly pageCount = computed(() =>
    Math.max(1, Math.ceil(this.length() / this.pageSize()))
  );

  readonly rangeLabel = computed(() => {
    const length = this.length();
    if (!length) {
      return '0 de 0';
    }
    const start = this.pageIndex() * this.pageSize() + 1;
    const end = Math.min(start + this.pageSize() - 1, length);
    return `${start}–${end} de ${length}`;
  });

  readonly hasPrevious = computed(() => this.pageIndex() > 0);
  readonly hasNext = computed(() => this.pageIndex() < this.pageCount() - 1);

  goTo(pageIndex: number): void {
    this.page.emit({ pageIndex, pageSize: this.pageSize() });
  }

  changePageSize(event: Event): void {
    const pageSize = Number((event.target as HTMLSelectElement).value);
    this.page.emit({ pageIndex: 0, pageSize });
  }
}
