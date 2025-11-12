import { Component, OnInit, inject, signal } from '@angular/core';
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
} from 'lucide-angular';
import { StatsCardComponent } from '../../../components/stats-card/stats-card.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, StatsCardComponent, DatePipe],
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

  private dashboardService = inject(DashboardService);

  ngOnInit(): void {
    this.dashboardService.getDashboardData().subscribe((data) => {
      this.latestVisits.set(data.latestVisits);
      this.totalVisitsThisMonth.set(data.totalVisitsThisMonth);
    });
  }
}
