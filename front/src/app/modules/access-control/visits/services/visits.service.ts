import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { Visit } from '../../../../core/models/visit.model';
import { AuthService } from '../../../../core/services/auth.service';
import { ResponseModel } from '../../../../core/types/response.model';
import { PaginatedResultDto } from '../../../../core/types/paginated-result.dto';

export interface VisitFilter {
  pageNumber: number;
  pageSize: number;
  startDate?: Date;
  endDate?: Date;
}

@Injectable({
  providedIn: 'root',
})
export class VisitsService {
  private apiUrl = `${environment.apiUrl}/visits`;
  private ownersApiUrl = `${environment.apiUrl}/owners`;
  private authService = inject(AuthService);

  constructor(private http: HttpClient) {}

  getVisits(filter: VisitFilter): Observable<PaginatedResultDto<Visit>> {
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
      return this.http.get<ResponseModel<PaginatedResultDto<Visit>>>(`${this.ownersApiUrl}/visits`, {
        params,
      }).pipe(
        map((response) => response.data!)
      );
    }

    return this.http.get<ResponseModel<PaginatedResultDto<Visit>>>(this.apiUrl, { params }).pipe(
      map((response) => response.data!)
    );
  }

  createVisit(visit: Partial<Visit>): Observable<Visit> {
    return this.http.post<ResponseModel<Visit>>(this.apiUrl, visit).pipe(
      map((response) => response.data!)
    );
  }
}
