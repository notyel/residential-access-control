import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { Visit } from '../../core/models/visit.model';
import { DashboardService } from '../dashboard.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  latestVisits$: Observable<Visit[]>;
  totalVisitsThisMonth$: Observable<{ count: number }>;

  constructor(private dashboardService: DashboardService) { }

  ngOnInit(): void {
    this.latestVisits$ = this.dashboardService.getLatestVisits();
    this.totalVisitsThisMonth$ = this.dashboardService.getTotalVisitsThisMonth();
  }
}
