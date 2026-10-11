import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AccessEvent } from '../models/access.model';
import { PaginatedResultDto } from '../types/paginated-result.dto';
import { ResponseModel } from '../types/response.model';

export interface AccessHistoryFilter {
  /** yyyy-MM-dd; el rango es obligatorio. */
  startDate: string;
  endDate: string;
  search?: string;
  plate?: string;
  type?: string;
  personKind?: string;
  onlyExceptions?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

export interface ReportDefinition {
  key: string;
  title: string;
  description: string;
  usesDates: boolean;
}

export interface Report {
  key: string;
  title: string;
  columns: string[];
  rows: string[][];
}

export interface Incident {
  id: string;
  occurredAt: string;
  category: string;
  categoryLabel: string;
  text: string;
  unitLabel?: string | null;
  personName?: string | null;
  vehiclePlate?: string | null;
  authorName: string;
}

/** Historial de accesos, reportes con su exportación y novedades del turno. */
@Injectable({ providedIn: 'root' })
export class ReportsApiService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  history(filter: AccessHistoryFilter): Observable<PaginatedResultDto<AccessEvent>> {
    return this.http
      .get<ResponseModel<PaginatedResultDto<AccessEvent>>>(`${this.api}/access-history`, {
        params: this.historyParams(filter),
      })
      .pipe(map((r) => r.data ?? { items: [], totalCount: 0 }));
  }

  exportHistory(filter: AccessHistoryFilter): Observable<void> {
    return this.download('access-history/export', this.historyParams(filter), 'historial-de-accesos.csv');
  }

  definitions(): Observable<ReportDefinition[]> {
    return this.http
      .get<ResponseModel<ReportDefinition[]>>(`${this.api}/reports`)
      .pipe(map((r) => r.data ?? []));
  }

  run(key: string, startDate: string, endDate: string): Observable<Report> {
    return this.http
      .get<ResponseModel<Report>>(`${this.api}/reports/${key}`, {
        params: this.range(startDate, endDate),
      })
      .pipe(map((r) => r.data!));
  }

  exportReport(key: string, startDate: string, endDate: string): Observable<void> {
    return this.download(`reports/${key}/export`, this.range(startDate, endDate), `${key}.csv`);
  }

  incidents(
    category: string,
    pageNumber: number,
    pageSize: number
  ): Observable<PaginatedResultDto<Incident>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (category) {
      params = params.set('category', category);
    }
    return this.http
      .get<ResponseModel<PaginatedResultDto<Incident>>>(`${this.api}/incidents`, { params })
      .pipe(map((r) => r.data ?? { items: [], totalCount: 0 }));
  }

  addIncident(body: {
    category: string;
    text: string;
    vehiclePlate?: string | null;
  }): Observable<Incident> {
    return this.http
      .post<ResponseModel<Incident>>(`${this.api}/incidents`, body)
      .pipe(map((r) => r.data!));
  }

  /** El día completo, en la hora del navegador: desde las 00:00 de inicio hasta las 23:59 de fin. */
  private range(startDate: string, endDate: string): HttpParams {
    return new HttpParams()
      .set('startDate', new Date(`${startDate}T00:00:00`).toISOString())
      .set('endDate', new Date(`${endDate}T23:59:59.999`).toISOString());
  }

  private historyParams(filter: AccessHistoryFilter): HttpParams {
    let params = this.range(filter.startDate, filter.endDate)
      .set('pageNumber', filter.pageNumber ?? 1)
      .set('pageSize', filter.pageSize ?? 25);
    if (filter.search?.trim()) {
      params = params.set('search', filter.search.trim());
    }
    if (filter.plate?.trim()) {
      params = params.set('plate', filter.plate.trim());
    }
    if (filter.type) {
      params = params.set('type', filter.type);
    }
    if (filter.personKind) {
      params = params.set('personKind', filter.personKind);
    }
    if (filter.onlyExceptions) {
      params = params.set('onlyExceptions', true);
    }
    return params;
  }

  /** El CSV se pide con la sesión (no es un enlace público) y se entrega al navegador como descarga. */
  private download(path: string, params: HttpParams, fileName: string): Observable<void> {
    return this.http.get(`${this.api}/${path}`, { params, responseType: 'blob' }).pipe(
      tap((blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.click();
        URL.revokeObjectURL(url);
      }),
      map(() => undefined)
    );
  }
}

/** yyyy-MM-dd de hace N días, para los rangos por defecto. */
export function daysAgoIso(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() - days);
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}
