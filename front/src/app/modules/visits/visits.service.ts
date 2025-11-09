import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Visit } from '../../core/models/visit.model';

export interface VisitFilter {
  pageNumber: number;
  pageSize: number;
  startDate?: Date;
  endDate?: Date;
}

export interface PagedVisits {
  visits: Visit[];
  totalCount: number;
  pageNumber: number;
}

@Injectable({
  providedIn: 'root'
})
export class VisitsService {

  constructor(private http: HttpClient) { }

  getVisits(filter: VisitFilter): Observable<PagedVisits> {
    let params = new HttpParams()
      .set('pageNumber', filter.pageNumber.toString())
      .set('pageSize', filter.pageSize.toString());

    if (filter.startDate) {
      params = params.set('startDate', filter.startDate.toISOString());
    }
    if (filter.endDate) {
      params = params.set('endDate', filter.endDate.toISOString());
    }

    return this.http.get<PagedVisits>(`${environment.apiUrl}/visits`, { params });
  }

  createVisit(visit: Partial<Visit>): Observable<Visit> {
    return this.http.post<Visit>(`${environment.apiUrl}/visits`, visit);
  }
}
