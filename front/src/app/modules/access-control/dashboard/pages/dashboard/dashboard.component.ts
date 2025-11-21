import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  DashboardService,
  LatestVisit,
} from '../../services/dashboard.service';
import { CommonModule, DatePipe } from '@angular/common';
import {
  LucideAngularModule,
  Users,
  Calendar,
  Clock,
  TrendingUp,
  Plus,
  ArrowRight,
} from 'lucide-angular';
import { MatButtonModule } from '@angular/material/button';
import { StatsCardComponent } from '../../../components/stats-card/stats-card.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    LucideAngularModule,
    StatsCardComponent,
    DatePipe,
    MatButtonModule,
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit {
  latestVisits = signal<LatestVisit[]>([]);
  totalVisitsThisMonth = signal<number>(0);

  // Icons
  UsersIcon = Users;
  CalendarIcon = Calendar;
  ClockIcon = Clock;

  TrendingUpIcon = TrendingUp;
  PlusIcon = Plus;
  ArrowRightIcon = ArrowRight;

  private dashboardService = inject(DashboardService);
  private router = inject(Router);

  ngOnInit(): void {
    this.dashboardService.getDashboardData().subscribe((data) => {
      this.latestVisits.set(data.latestVisits);
      this.totalVisitsThisMonth.set(data.totalVisitsThisMonth);
    });
  }

  registerVisit() {
    this.router.navigate(['/access-control/visits/register']);
  }
}
