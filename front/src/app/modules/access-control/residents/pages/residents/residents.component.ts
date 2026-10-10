import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { LucideAngularModule, Plus, SquarePen } from 'lucide-angular';
import { User } from '../../../../../core/models/user.model';
import { DataTableComponent } from '../../../../../shared/components/data-table/data-table.component';
import {
  PageChange,
  PaginatorComponent,
} from '../../../../../shared/components/paginator/paginator.component';
import { PageActionsDirective } from '../../../../../shared/directives/page-actions.directive';
import { initials } from '../../../../../shared/utils/initials';
import { ResidentsService } from '../../services/residents.service';

@Component({
  selector: 'app-residents',
  standalone: true,
  imports: [
    RouterLink,
    MatButtonModule,
    LucideAngularModule,
    DataTableComponent,
    PaginatorComponent,
    PageActionsDirective,
  ],
  templateUrl: './residents.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResidentsComponent implements OnInit {
  private readonly residentsService = inject(ResidentsService);

  readonly residents = signal<User[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);
  readonly isLoading = signal(true);
  readonly error = signal<string | null>(null);

  readonly PlusIcon = Plus;
  readonly EditIcon = SquarePen;
  readonly initials = initials;

  ngOnInit(): void {
    this.loadResidents();
  }

  onPageChange(change: PageChange): void {
    this.pageIndex.set(change.pageIndex);
    this.pageSize.set(change.pageSize);
    this.loadResidents();
  }

  loadResidents(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.residentsService
      .getResidentsPage(this.pageIndex() + 1, this.pageSize())
      .subscribe({
        next: (page) => {
          this.residents.set(page.items);
          this.totalCount.set(page.totalCount);
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading residents:', error);
          this.error.set('Revisa tu conexión e inténtalo de nuevo.');
          this.isLoading.set(false);
        },
      });
  }
}
