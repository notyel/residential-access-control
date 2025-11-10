import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { Visit } from '../../../../core/models/visit.model';
import { AuthService } from '../../../../core/services/auth.service';

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
  pageSize: number;
}

@Injectable({
  providedIn: 'root',
})
export class VisitsService {
  private apiUrl = `${environment.apiUrl}/visits`;
  private authService = inject(AuthService);

  constructor(private http: HttpClient) {}

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

    if (this.authService.hasRole('Owner')) {
      return this.http.get<PagedVisits>(`${this.apiUrl}/my-visits`, {
        params,
      });
    }

    return this.http.get<PagedVisits>(this.apiUrl, { params });
  }

  createVisit(visit: Partial<Visit>): Observable<Visit> {
    return this.http.post<Visit>(this.apiUrl, visit);
  }
}
