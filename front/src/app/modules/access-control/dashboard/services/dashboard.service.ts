import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { ResponseModel } from '../../../../core/types/response.model';

export interface LatestVisit {
  visitorFullName: string;
  residentFullName: string;
  entryTime: Date;
}

export interface DashboardData {
  totalVisitsThisMonth: number;
  latestVisits: LatestVisit[];
}

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  constructor(private http: HttpClient) {}

  getDashboardData(): Observable<DashboardData> {
    return this.http.get<ResponseModel<DashboardData>>(
      `${environment.apiUrl}/dashboard`
    ).pipe(
      map((response) => response.data!)
    );
  }
}
