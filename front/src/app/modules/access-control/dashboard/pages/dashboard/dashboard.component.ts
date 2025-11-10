import { Component, OnInit, inject, signal } from '@angular/core';
import { Visit } from '../../../../../core/models/visit.model';
import { DashboardService } from '../../services/dashboard.service';
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
  latestVisits = signal<Visit[]>([]);
  totalVisitsThisMonth = signal<number>(0);

  // Icons
  UsersIcon = Users;
  CalendarIcon = Calendar;

  private dashboardService = inject(DashboardService);

  ngOnInit(): void {
    this.dashboardService
      .getLatestVisits()
      .subscribe((items) => this.latestVisits.set(items));
    this.dashboardService
      .getTotalVisitsThisMonth()
      .subscribe((result) => this.totalVisitsThisMonth.set(result.count));
  }
}
