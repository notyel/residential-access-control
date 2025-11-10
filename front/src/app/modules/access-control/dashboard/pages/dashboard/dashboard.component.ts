import { Component, OnInit, inject, signal } from '@angular/core';
import { DashboardService, LatestVisit } from '../../services/dashboard.service';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { LucideAngularModule, Users, Calendar } from 'lucide-angular';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatListModule,
    MatIconModule,
    LucideAngularModule,
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

  private dashboardService = inject(DashboardService);

  ngOnInit(): void {
    this.dashboardService
      .getDashboardData()
      .subscribe((data) => {
        this.latestVisits.set(data.latestVisits);
        this.totalVisitsThisMonth.set(data.totalVisitsThisMonth);
      });
  }
}
