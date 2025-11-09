import { Component, OnInit, inject, signal } from '@angular/core';
import { Visit } from '../../../../core/models/visit.model';
import { DashboardService } from '../../dashboard.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  latestVisits = signal<Visit[]>([]);
  totalVisitsThisMonth = signal<number>(0);

  private dashboardService = inject(DashboardService);

  ngOnInit(): void {
    this.dashboardService.getLatestVisits().subscribe(items => this.latestVisits.set(items));
    this.dashboardService.getTotalVisitsThisMonth().subscribe(result => this.totalVisitsThisMonth.set(result.count));
  }
}
