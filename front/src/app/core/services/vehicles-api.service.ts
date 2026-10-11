import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AccessEvent } from '../models/access.model';
import {
  CreateVehicle,
  PlateCheck,
  SaveVehicle,
  Vehicle,
  VehicleDetail,
  VehicleFilter,
  VehicleInside,
  normalizePlate,
} from '../models/vehicle.model';
import { PaginatedResultDto } from '../types/paginated-result.dto';
import { ResponseModel } from '../types/response.model';

/** Inventario de vehículos, su verificación en portería y "Mis vehículos". */
@Injectable({ providedIn: 'root' })
export class VehiclesApiService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  search(filter: VehicleFilter): Observable<PaginatedResultDto<Vehicle>> {
    let params = new HttpParams()
      .set('pageNumber', filter.pageNumber)
      .set('pageSize', filter.pageSize);
    if (filter.search?.trim()) {
      params = params.set('search', filter.search.trim());
    }
    if (filter.type) {
      params = params.set('type', filter.type);
    }
    if (filter.class !== null && filter.class !== undefined) {
      params = params.set('class', filter.class);
    }
    if (filter.status !== null && filter.status !== undefined) {
      params = params.set('status', filter.status);
    }
    return this.http
      .get<ResponseModel<PaginatedResultDto<Vehicle>>>(`${this.api}/vehicles`, { params })
      .pipe(map((r) => r.data ?? { items: [], totalCount: 0 }));
  }

  detail(id: string): Observable<VehicleDetail> {
    return this.get<VehicleDetail>(`vehicles/${id}`).pipe(map((d) => d!));
  }

  pending(): Observable<Vehicle[]> {
    return this.get<Vehicle[]>('vehicles/pending').pipe(map((d) => d ?? []));
  }

  create(vehicle: CreateVehicle): Observable<Vehicle> {
    return this.post<Vehicle>('vehicles', vehicle);
  }

  update(id: string, vehicle: SaveVehicle): Observable<Vehicle> {
    return this.http
      .put<ResponseModel<Vehicle>>(`${this.api}/vehicles/${id}`, vehicle)
      .pipe(map((r) => r.data!));
  }

  reassign(id: string, unitId: string, responsiblePersonId: string): Observable<Vehicle> {
    return this.post<Vehicle>(`vehicles/${id}/reassign`, { unitId, responsiblePersonId });
  }

  suspend(id: string, reason: string): Observable<Vehicle> {
    return this.post<Vehicle>(`vehicles/${id}/suspend`, { reason });
  }

  reactivate(id: string): Observable<Vehicle> {
    return this.post<Vehicle>(`vehicles/${id}/reactivate`, {});
  }

  revoke(id: string, reason: string): Observable<Vehicle> {
    return this.post<Vehicle>(`vehicles/${id}/revoke`, { reason });
  }

  decide(id: string, approve: boolean, reason?: string): Observable<Vehicle> {
    return this.post<Vehicle>(`vehicles/${id}/decide`, { approve, reason });
  }

  addNote(id: string, text: string): Observable<Vehicle> {
    return this.post<Vehicle>(`vehicles/${id}/notes`, { text });
  }

  // ---- portería ----

  checkPlate(plate: string): Observable<PlateCheck> {
    return this.get<PlateCheck>(
      `vehicles/by-plate/${encodeURIComponent(normalizePlate(plate))}`
    ).pipe(map((d) => d!));
  }

  inside(): Observable<VehicleInside[]> {
    return this.get<VehicleInside[]>('vehicles/inside').pipe(map((d) => d ?? []));
  }

  /** Salida de un vehículo que quedó dentro sin su conductor. */
  vehicleExit(vehicleId: string): Observable<AccessEvent> {
    return this.post<AccessEvent>(`access/vehicles/${vehicleId}/exit`, {});
  }

  // ---- portal ----

  /** Devuelve además la política del conjunto: no, approval o direct. */
  mine(): Observable<{ policy: string; vehicles: Vehicle[] }> {
    return this.http
      .get<ResponseModel<Vehicle[]>>(`${this.api}/my/vehicles`)
      .pipe(map((r) => ({ policy: r.message, vehicles: r.data ?? [] })));
  }

  createMine(vehicle: CreateVehicle): Observable<{ message: string; vehicle: Vehicle }> {
    return this.http
      .post<ResponseModel<Vehicle>>(`${this.api}/my/vehicles`, vehicle)
      .pipe(map((r) => ({ message: r.message, vehicle: r.data! })));
  }

  revokeMine(id: string): Observable<Vehicle> {
    return this.post<Vehicle>(`my/vehicles/${id}/revoke`, {});
  }

  private get<T>(path: string): Observable<T | undefined> {
    return this.http
      .get<ResponseModel<T>>(`${this.api}/${path}`)
      .pipe(map((r) => r.data));
  }

  private post<T>(path: string, body: unknown): Observable<T> {
    return this.http
      .post<ResponseModel<T>>(`${this.api}/${path}`, body)
      .pipe(map((r) => r.data!));
  }
}
