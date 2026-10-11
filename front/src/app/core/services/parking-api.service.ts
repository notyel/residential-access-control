import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Availability,
  ParkingFilter,
  ParkingSpace,
  ParkingSpaceDetail,
  ParkingType,
  ParkingVehicleKind,
  SaveParkingSpace,
} from '../models/parking.model';
import { ResponseModel } from '../types/response.model';

export interface PlanParking {
  spaceId: string;
  unitId: string;
  /** ISO con zona horaria. */
  startsAt: string;
  endsAt: string;
}

/** Parqueaderos: inventario, disponibilidad calculada, asignaciones y ocupación. */
@Injectable({ providedIn: 'root' })
export class ParkingApiService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/parking`;

  spaces(filter: ParkingFilter = {}): Observable<ParkingSpace[]> {
    return this.http
      .get<ResponseModel<ParkingSpace[]>>(`${this.api}/spaces`, { params: this.params(filter) })
      .pipe(map((r) => r.data ?? []));
  }

  availability(from?: string, to?: string): Observable<Availability> {
    return this.http
      .get<ResponseModel<Availability>>(`${this.api}/availability`, {
        params: this.params({ from, to }),
      })
      .pipe(map((r) => r.data!));
  }

  /** Espacios de visitantes libres ahora para ese tipo de vehículo: lo que portería puede ofrecer. */
  visitorSpaces(vehicleType?: string): Observable<ParkingSpace[]> {
    const params = vehicleType ? new HttpParams().set('vehicleType', vehicleType) : undefined;
    return this.http
      .get<ResponseModel<ParkingSpace[]>>(`${this.api}/visitor-spaces`, { params })
      .pipe(map((r) => r.data ?? []));
  }

  detail(id: string): Observable<ParkingSpaceDetail> {
    return this.http
      .get<ResponseModel<ParkingSpaceDetail>>(`${this.api}/spaces/${id}`)
      .pipe(map((r) => r.data!));
  }

  mine(): Observable<ParkingSpace[]> {
    return this.http
      .get<ResponseModel<ParkingSpace[]>>(`${environment.apiUrl}/my/parking`)
      .pipe(map((r) => r.data ?? []));
  }

  create(space: SaveParkingSpace): Observable<ParkingSpace> {
    return this.post<ParkingSpace>('spaces', space);
  }

  createBulk(body: {
    prefix: string;
    from: number;
    to: number;
    location?: string | null;
    type: ParkingType;
    vehicleKind: ParkingVehicleKind;
  }): Observable<{ created: string[]; skipped: string[] }> {
    return this.post('spaces/bulk', body);
  }

  setActive(id: string, active: boolean): Observable<ParkingSpace> {
    return this.post<ParkingSpace>(`spaces/${id}/${active ? 'activate' : 'deactivate'}`, {});
  }

  setHolder(id: string, unitId: string): Observable<ParkingSpace> {
    return this.post<ParkingSpace>(`spaces/${id}/holder`, { unitId });
  }

  endHolder(id: string): Observable<ParkingSpace> {
    return this.post<ParkingSpace>(`spaces/${id}/holder/end`, {});
  }

  outOfService(id: string, reason: string): Observable<ParkingSpace> {
    return this.post<ParkingSpace>(`spaces/${id}/out-of-service`, { reason });
  }

  endOutOfService(outOfServiceId: string): Observable<ParkingSpace> {
    return this.post<ParkingSpace>(`out-of-service/${outOfServiceId}/end`, {});
  }

  assign(plan: PlanParking): Observable<ParkingSpace> {
    return this.post<ParkingSpace>('assignments', plan);
  }

  reserve(plan: PlanParking): Observable<ParkingSpace> {
    return this.post<ParkingSpace>('reservations', plan);
  }

  cancelAssignment(assignmentId: string): Observable<ParkingSpace> {
    return this.post<ParkingSpace>(`assignments/${assignmentId}/cancel`, {});
  }

  /** Corrección manual: lo normal es que la salida del vehículo libere el espacio. */
  release(assignmentId: string): Observable<ParkingSpace> {
    return this.post<ParkingSpace>(`occupancies/${assignmentId}/release`, {});
  }

  addNote(id: string, text: string): Observable<ParkingSpace> {
    return this.post<ParkingSpace>(`spaces/${id}/notes`, { text });
  }

  private params(values: object): HttpParams {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(values)) {
      if (value !== null && value !== undefined && value !== '') {
        params = params.set(key, value as string | number);
      }
    }
    return params;
  }

  private post<T>(path: string, body: unknown): Observable<T> {
    return this.http
      .post<ResponseModel<T>>(`${this.api}/${path}`, body)
      .pipe(map((r) => r.data!));
  }
}
