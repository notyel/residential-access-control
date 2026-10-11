import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AccessEvent,
  AccessEventType,
  Authorization,
  CheckIn,
  CreateAuthorization,
  GateContext,
  LookupResult,
  Reject,
  Visit,
  VisitFilter,
} from '../models/access.model';
import { PaginatedResultDto } from '../types/paginated-result.dto';
import { ResponseModel } from '../types/response.model';

export interface AuthorizationFilter {
  pageNumber: number;
  pageSize: number;
  /** Active, Expired, Revoked o Consumed. */
  state?: string;
  search?: string;
}

const EMPTY_PAGE = { items: [], totalCount: 0 };

/** Llamadas al API de portería: validación, ingresos, salidas, visitas y autorizaciones. */
@Injectable({ providedIn: 'root' })
export class AccessApiService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  // ---- portería ----

  context(): Observable<GateContext> {
    return this.get<GateContext>('access/context').pipe(map((d) => d!));
  }

  lookup(term: string): Observable<LookupResult> {
    return this.get<LookupResult>(
      'access/lookup',
      new HttpParams().set('term', term)
    ).pipe(map((d) => d ?? { authorizations: [], residents: [], people: [] }));
  }

  expected(): Observable<Authorization[]> {
    return this.get<Authorization[]>('access/expected').pipe(map((d) => d ?? []));
  }

  checkIn(body: CheckIn): Observable<Visit> {
    return this.post<Visit>('access/check-in', body);
  }

  /** El servidor responde 409 si la visita ya tenía salida. */
  checkOut(
    visitId: string,
    options: { administrativeReason?: string; withoutVehicle?: boolean } = {}
  ): Observable<Visit> {
    return this.post<Visit>(`access/visits/${visitId}/check-out`, {
      administrative: !!options.administrativeReason,
      reason: options.administrativeReason,
      // el visitante sale y deja el vehículo: sigue contando como dentro
      withoutVehicle: !!options.withoutVehicle,
    });
  }

  reject(body: Reject): Observable<AccessEvent> {
    return this.post<AccessEvent>('access/reject', body);
  }

  residentMovement(
    personId: string,
    unitId: string,
    type: AccessEventType.Entry | AccessEventType.Exit,
    vehicle?: { vehicleId: string; isException?: boolean; exceptionReason?: string }
  ): Observable<AccessEvent> {
    return this.post<AccessEvent>('access/residents', {
      personId,
      unitId,
      type,
      ...vehicle,
    });
  }

  /** Los eventos no se editan: un error se corrige anulándolo, con motivo. */
  voidEvent(eventId: string, reason: string): Observable<AccessEvent> {
    return this.post<AccessEvent>(`access/events/${eventId}/void`, { reason });
  }

  // ---- visitas ----

  /** Sin permiso sobre todo el conjunto, el servidor solo entrega las de las unidades con vínculo vigente. */
  visits(filter: VisitFilter): Observable<PaginatedResultDto<Visit>> {
    let params = new HttpParams()
      .set('pageNumber', filter.pageNumber)
      .set('pageSize', filter.pageSize);
    if (filter.startDate) {
      params = params.set('startDate', filter.startDate.toISOString());
    }
    if (filter.endDate) {
      params = params.set('endDate', filter.endDate.toISOString());
    }
    if (filter.status) {
      params = params.set('status', filter.status);
    }
    if (filter.unitId) {
      params = params.set('unitId', filter.unitId);
    }
    if (filter.search?.trim()) {
      params = params.set('search', filter.search.trim());
    }
    if (filter.plate?.trim()) {
      params = params.set('plate', filter.plate.trim());
    }
    if (filter.verification !== null && filter.verification !== undefined) {
      params = params.set('verification', filter.verification);
    }
    return this.get<PaginatedResultDto<Visit>>('visits', params).pipe(
      map((d) => d ?? EMPTY_PAGE)
    );
  }

  // ---- autorizaciones ----

  /** portal = las de mis unidades; si no, las de todo el conjunto. */
  authorizations(
    filter: AuthorizationFilter,
    portal: boolean
  ): Observable<PaginatedResultDto<Authorization>> {
    let params = new HttpParams()
      .set('pageNumber', filter.pageNumber)
      .set('pageSize', filter.pageSize);
    if (filter.state) {
      params = params.set('state', filter.state);
    }
    if (filter.search?.trim()) {
      params = params.set('search', filter.search.trim());
    }
    return this.get<PaginatedResultDto<Authorization>>(
      this.authorizationsPath(portal),
      params
    ).pipe(map((d) => d ?? EMPTY_PAGE));
  }

  createAuthorization(
    body: CreateAuthorization,
    portal: boolean
  ): Observable<Authorization> {
    return this.post<Authorization>(this.authorizationsPath(portal), body);
  }

  revokeAuthorization(
    id: string,
    reason: string,
    portal: boolean
  ): Observable<Authorization> {
    return this.post<Authorization>(
      `${this.authorizationsPath(portal)}/${id}/revoke`,
      { reason }
    );
  }

  private authorizationsPath(portal: boolean): string {
    return portal ? 'my/authorizations' : 'authorizations';
  }

  private get<T>(path: string, params?: HttpParams): Observable<T | undefined> {
    return this.http
      .get<ResponseModel<T>>(`${this.api}/${path}`, { params })
      .pipe(map((r) => r.data));
  }

  private post<T>(path: string, body: unknown): Observable<T> {
    return this.http
      .post<ResponseModel<T>>(`${this.api}/${path}`, body)
      .pipe(map((r) => r.data!));
  }
}
