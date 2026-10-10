import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { Visit } from '../../../../core/models/visit.model';
import { ResponseModel } from '../../../../core/types/response.model';
import { PaginatedResultDto } from '../../../../core/types/paginated-result.dto';
import { CreateVisit } from '../../../../core/models/create-visit.model';

/** Inside: sin salida registrada. CheckedOut: con salida. */
export type VisitStatus = 'Inside' | 'CheckedOut';

export interface VisitFilter {
  pageNumber: number;
  pageSize: number;
  startDate?: Date;
  endDate?: Date;
  status?: VisitStatus;
}

@Injectable({
  providedIn: 'root',
})
export class VisitsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/visits`;

  /** El servidor limita el resultado según el rol: un propietario solo recibe las de sus residencias. */
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
    if (filter.status) {
      params = params.set('status', filter.status);
    }

    return this.http
      .get<ResponseModel<PaginatedResultDto<Visit>>>(this.apiUrl, { params })
      .pipe(map((response) => response.data!));
  }

  createVisit(visit: CreateVisit): Observable<Visit> {
    return this.http
      .post<ResponseModel<Visit>>(this.apiUrl, visit)
      .pipe(map((response) => response.data!));
  }

  /** Registra la salida. El servidor responde 409 si la visita ya la tenía. */
  checkout(visitId: string): Observable<Visit> {
    return this.http
      .post<ResponseModel<Visit>>(`${this.apiUrl}/${visitId}/checkout`, null)
      .pipe(map((response) => response.data!));
  }
}
