import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  DashboardService,
  LatestVisit,
} from '../../services/dashboard.service';
import { DatePipe } from '@angular/common';
import {
  LucideAngularModule,
  Users,
  Calendar,
  TrendingUp,
  Plus,
  ArrowRight,
} from 'lucide-angular';
import { StatsCardComponent } from '../../../components/stats-card/stats-card.component';
import { StatusBadgeComponent } from '../../../../../shared/components/status-badge/status-badge.component';
import { IfPermissionDirective } from '../../../../../shared/directives/if-permission.directive';
import { P } from '../../../../../core/security/permissions';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    RouterLink,
    LucideAngularModule,
    StatsCardComponent,
    StatusBadgeComponent,
    IfPermissionDirective,
    DatePipe,
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent implements OnInit {
  latestVisits = signal<LatestVisit[]>([]);
  totalVisitsThisMonth = signal<number>(0);
  visitorsInside = signal<number>(0);
  visitsToday = signal<number>(0);

  readonly P = P;

  // Icons
  UsersIcon = Users;
  CalendarIcon = Calendar;

  TrendingUpIcon = TrendingUp;
  PlusIcon = Plus;
  ArrowRightIcon = ArrowRight;

  private dashboardService = inject(DashboardService);

  ngOnInit(): void {
    this.dashboardService.getDashboardData().subscribe((data) => {
      this.latestVisits.set(data.latestVisits);
      this.totalVisitsThisMonth.set(data.totalVisitsThisMonth);
      this.visitorsInside.set(data.visitorsInside);
      this.visitsToday.set(data.visitsToday);
    });
  }
}
