import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { AvailabilityRow } from '../../../../core/models/parking.model';
import { Incident } from '../../../../core/services/reports-api.service';
import { ResponseModel } from '../../../../core/types/response.model';

export interface LatestVisit {
  visitorFullName: string;
  unitLabel: string;
  entryTime: Date;
  checkOut?: Date | null;
}

/** Bloque de portería (access.register). */
export interface GateBlock {
  expectedToday: number;
  inside: number;
  vehiclesInside: number;
  visitorParking: AvailabilityRow;
  latestIncidents: Incident[];
}

export interface DashboardAlert {
  occurredAt: string;
  kind: 'rejection' | 'exception';
  personName: string;
  unitLabel?: string | null;
  reason?: string | null;
}

/** Bloque de administración y auditoría (reports.read). */
export interface ManagementBlock {
  unitsVacant: number;
  unitsOwnerOccupied: number;
  unitsRented: number;
  accessesToday: number;
  accessesWeek: number;
  inside: number;
  recentAlerts: DashboardAlert[];
  parking: AvailabilityRow[];
  pendingOccupancies: number;
  pendingVehicles: number;
  expiringLinks: number;
}

/** Bloque del propietario o residente (units.read_own). */
export interface PortalBlock {
  units: string[];
  activeAuthorizations: number;
  vehicles: number;
}

/** El servidor solo envía los bloques que los permisos del usuario habilitan; los demás llegan en null. */
export interface DashboardData {
  totalVisitsThisMonth: number;
  visitorsInside: number;
  visitsToday: number;
  latestVisits: LatestVisit[];
  gate?: GateBlock | null;
  management?: ManagementBlock | null;
  portal?: PortalBlock | null;
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
