import { StatusVariant } from '../../shared/components/status-badge/status-badge.component';

export enum VehicleClass {
  Permanent = 0,
  Visitor = 1,
}

export const VEHICLE_CLASS_LABELS: Record<VehicleClass, string> = {
  [VehicleClass.Permanent]: 'Permanente',
  [VehicleClass.Visitor]: 'Visitante',
};

export enum VehicleStatus {
  Pending = 0,
  Active = 1,
  Suspended = 2,
  Revoked = 3,
  Ended = 4,
  Rejected = 5,
}

export const VEHICLE_STATUS_LABELS: Record<VehicleStatus, string> = {
  [VehicleStatus.Pending]: 'Pendiente de aprobación',
  [VehicleStatus.Active]: 'Vigente',
  [VehicleStatus.Suspended]: 'Suspendida',
  [VehicleStatus.Revoked]: 'Revocada',
  [VehicleStatus.Ended]: 'Finalizada',
  [VehicleStatus.Rejected]: 'Rechazada',
};

export const VEHICLE_STATUS_VARIANTS: Record<VehicleStatus, StatusVariant> = {
  [VehicleStatus.Pending]: 'warning',
  [VehicleStatus.Active]: 'success',
  [VehicleStatus.Suspended]: 'warning',
  [VehicleStatus.Revoked]: 'danger',
  [VehicleStatus.Ended]: 'neutral',
  [VehicleStatus.Rejected]: 'danger',
};

export interface Vehicle {
  id: string;
  /** Normalizada: mayúsculas, sin espacios ni guiones. */
  plate: string;
  hasPlate: boolean;
  type: string;
  brand?: string | null;
  color?: string | null;
  model?: string | null;
  notes?: string | null;
  class: VehicleClass;
  authorizationId?: string | null;
  unitId?: string | null;
  unitLabel?: string | null;
  responsiblePersonId?: string | null;
  responsibleName?: string | null;
  startDate?: string | null;
  endDate?: string | null;
  status?: VehicleStatus | null;
  statusReason?: string | null;
  isAuthorizedToday: boolean;
}

export interface VehicleAuthorization {
  id: string;
  unitLabel: string;
  responsibleName: string;
  startDate: string;
  endDate?: string | null;
  status: VehicleStatus;
  statusReason?: string | null;
}

export interface VehicleNote {
  createdAt: string;
  authorName?: string | null;
  text: string;
}

export interface VehicleDetail {
  vehicle: Vehicle;
  history: VehicleAuthorization[];
  notes: VehicleNote[];
  isInside: boolean;
}

export interface SaveVehicle {
  plate?: string | null;
  type: string;
  brand?: string | null;
  color?: string | null;
  model?: string | null;
  notes?: string | null;
}

export interface CreateVehicle extends SaveVehicle {
  unitId: string;
  responsiblePersonId?: string | null;
}

export interface VehicleFilter {
  pageNumber: number;
  pageSize: number;
  search?: string;
  type?: string;
  class?: VehicleClass | null;
  status?: VehicleStatus | null;
}

export enum PlateResult {
  Authorized = 0,
  NotValid = 1,
  VisitInProgress = 2,
  Unknown = 3,
}

export interface PlateCheck {
  plate: string;
  result: PlateResult;
  message: string;
  vehicle?: Vehicle | null;
  isInside: boolean;
  drivers: { personId: string; name: string }[];
  visitId?: string | null;
  visitorName?: string | null;
  visitUnitLabel?: string | null;
}

export interface VehicleInside {
  vehicleId: string;
  plate: string;
  type: string;
  class: VehicleClass;
  unitLabel?: string | null;
  personName: string;
  since: string;
  visitId?: string | null;
  /** La visita con la que entró ya salió: el vehículo quedó dentro. */
  visitorLeft: boolean;
}

/** La misma normalización del servidor, para mostrar la placa como quedará guardada. */
export function normalizePlate(value: string): string {
  return value.replace(/[^a-zA-Z0-9]/g, '').toUpperCase();
}

/** "Mazda rojo" o lo que haya. */
export function vehicleDescription(v: Pick<Vehicle, 'brand' | 'color' | 'model'>): string {
  return [v.brand, v.model, v.color].filter(Boolean).join(' · ');
}
