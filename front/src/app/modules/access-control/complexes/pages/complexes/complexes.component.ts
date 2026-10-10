import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import {
  LogIn,
  LucideAngularModule,
  Plus,
  Power,
  SquarePen,
} from 'lucide-angular';
import {
  COMPLEX_TYPE_LABELS,
  Complex,
} from '../../../../../core/models/complex.model';
import { ComplexContextService } from '../../../../../core/services/complex-context.service';
import { MenuService } from '../../../../../core/services/menu.service';
import { apiErrorMessage } from '../../../../../core/utils/api-error';
import { DataTableComponent } from '../../../../../shared/components/data-table/data-table.component';
import { StatusBadgeComponent } from '../../../../../shared/components/status-badge/status-badge.component';
import { PageActionsDirective } from '../../../../../shared/directives/page-actions.directive';
import { ComplexesService } from '../../services/complexes.service';

@Component({
  selector: 'app-complexes',
  standalone: true,
  imports: [
    RouterLink,
    MatButtonModule,
    LucideAngularModule,
    DataTableComponent,
    StatusBadgeComponent,
    PageActionsDirective,
  ],
  templateUrl: './complexes.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ComplexesComponent implements OnInit {
  private readonly complexesService = inject(ComplexesService);
  private readonly menuService = inject(MenuService);
  private readonly router = inject(Router);
  readonly context = inject(ComplexContextService);

  readonly complexes = signal<Complex[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly busyId = signal<string | null>(null);

  readonly typeLabels = COMPLEX_TYPE_LABELS;
  readonly PlusIcon = Plus;
  readonly EditIcon = SquarePen;
  readonly PowerIcon = Power;
  readonly EnterIcon = LogIn;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.complexesService.getAll().subscribe({
      next: (complexes) => {
        this.complexes.set(complexes);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Revisa tu conexión e inténtalo de nuevo.');
        this.loading.set(false);
      },
    });
  }

  /** Deja al superadministrador trabajando sobre el conjunto y lo lleva a su panel. */
  workOn(complex: Complex): void {
    this.context.select({ id: complex.id, name: complex.name });
    this.menuService.getMenuForCurrentUser().subscribe();
    this.router.navigate(['/access-control/dashboard']);
  }

  toggleActive(complex: Complex): void {
    if (this.busyId()) {
      return;
    }
    this.busyId.set(complex.id);
    this.actionError.set(null);

    this.complexesService.setActive(complex.id, !complex.isActive).subscribe({
      next: (updated) => {
        this.busyId.set(null);
        this.complexes.update((list) =>
          list.map((item) => (item.id === updated.id ? updated : item))
        );
      },
      error: (err) => {
        this.busyId.set(null);
        this.actionError.set(
          apiErrorMessage(err, 'No se pudo cambiar el estado del conjunto.')
        );
      },
    });
  }
}
