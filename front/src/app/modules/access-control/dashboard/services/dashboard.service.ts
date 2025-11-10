import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { Visit } from '../../../../core/models/visit.model';

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  constructor(private http: HttpClient) {}

  getLatestVisits(): Observable<Visit[]> {
    return this.http.get<Visit[]>(
      `${environment.apiUrl}/dashboard/latest-visits`
    );
  }

  getTotalVisitsThisMonth(): Observable<{ count: number }> {
    return this.http.get<{ count: number }>(
      `${environment.apiUrl}/dashboard/total-visits-this-month`
    );
  }
}
