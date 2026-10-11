import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { DashboardData, DashboardService } from '../../services/dashboard.service';
import { DatePipe } from '@angular/common';
import {
  LucideAngularModule,
  Users,
  Calendar,
  TrendingUp,
  ArrowRight,
  Building2,
  Car,
  CircleAlert,
  ClipboardList,
  DoorOpen,
  ScanSearch,
  Search,
  SquareParking,
  Ticket,
} from 'lucide-angular';
import { fromEvent, interval, merge } from 'rxjs';
import { filter } from 'rxjs/operators';
import { StatsCardComponent } from '../../../components/stats-card/stats-card.component';
import { StatusBadgeComponent } from '../../../../../shared/components/status-badge/status-badge.component';
import { PARKING_TYPE_LABELS } from '../../../../../core/models/parking.model';

const REFRESH_MS = 30_000;

/**
 * Panel armado con bloques: portería, administración y portal. El servidor envía solo los
 * bloques que los permisos del usuario habilitan, con las cifras calculadas en el momento; quien
 * tiene varios roles ve la unión. Se actualiza solo cada 30 segundos y al volver a la pestaña.
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    RouterLink,
    LucideAngularModule,
    StatsCardComponent,
    StatusBadgeComponent,
    DatePipe,
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly data = signal<DashboardData | null>(null);
  readonly failed = signal(false);
  private focused = false;

  readonly parkingLabels = PARKING_TYPE_LABELS;

  readonly UsersIcon = Users;
  readonly CalendarIcon = Calendar;
  readonly TrendingUpIcon = TrendingUp;
  readonly ArrowRightIcon = ArrowRight;
  readonly UnitsIcon = Building2;
  readonly CarIcon = Car;
  readonly AlertIcon = CircleAlert;
  readonly IncidentIcon = ClipboardList;
  readonly GateIcon = DoorOpen;
  readonly PlateIcon = ScanSearch;
  readonly SearchIcon = Search;
  readonly ParkingIcon = SquareParking;
  readonly TicketIcon = Ticket;

  ngOnInit(): void {
    this.load();
    merge(
      interval(REFRESH_MS),
      fromEvent(document, 'visibilitychange').pipe(
        filter(() => document.visibilityState === 'visible')
      )
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        if (document.visibilityState === 'visible') {
          this.load();
        }
      });
  }

  /** El buscador de portería lleva lo escrito a la validación: se puede registrar un ingreso sin usar el ratón. */
  search(event: Event): void {
    event.preventDefault();
    const input = (event.target as HTMLFormElement).querySelector('input');
    const term = input?.value.trim();
    // el término viaja en el estado de navegación, no en la URL: puede ser un documento
    this.router.navigate(['/access-control/gate'], {
      state: term ? { documentNumber: term } : undefined,
    });
  }

  private load(): void {
    this.dashboardService.getDashboardData().subscribe({
      next: (data) => {
        this.data.set(data);
        this.failed.set(false);
        if (data.gate && !this.focused) {
          // portería empieza a escribir sin tocar el ratón; solo la primera vez, para no robar el foco al refrescar
          this.focused = true;
          setTimeout(() =>
            document.querySelector<HTMLInputElement>('.dashboard-search input')?.focus()
          );
        }
      },
      error: () => this.failed.set(true),
    });
  }
}
