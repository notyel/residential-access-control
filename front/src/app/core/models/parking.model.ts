import { StatusVariant } from '../../shared/components/status-badge/status-badge.component';

/** A quién está destinado el espacio: un dato suyo, no su estado. */
export enum ParkingType {
  Private = 0,
  Community = 1,
  Visitor = 2,
}

export const PARKING_TYPE_LABELS: Record<ParkingType, string> = {
  [ParkingType.Private]: 'Privado',
  [ParkingType.Community]: 'Comunitario',
  [ParkingType.Visitor]: 'Visitantes',
};

export enum ParkingVehicleKind {
  Any = 0,
  Car = 1,
  Motorcycle = 2,
}

export const PARKING_KIND_LABELS: Record<ParkingVehicleKind, string> = {
  [ParkingVehicleKind.Any]: 'Cualquier vehículo',
  [ParkingVehicleKind.Car]: 'Carro',
  [ParkingVehicleKind.Motorcycle]: 'Moto',
};

/** Estado operativo; lo calcula el servidor en cada consulta. */
export enum ParkingState {
  Available = 0,
  Occupied = 1,
  Reserved = 2,
  OutOfService = 3,
}

export const PARKING_STATE_LABELS: Record<ParkingState, string> = {
  [ParkingState.Available]: 'Disponible',
  [ParkingState.Occupied]: 'Ocupado',
  [ParkingState.Reserved]: 'Reservado',
  [ParkingState.OutOfService]: 'Fuera de servicio',
};

export const PARKING_STATE_VARIANTS: Record<ParkingState, StatusVariant> = {
  [ParkingState.Available]: 'success',
  [ParkingState.Occupied]: 'danger',
  [ParkingState.Reserved]: 'warning',
  [ParkingState.OutOfService]: 'neutral',
};

export enum ParkingAssignmentClass {
  Holder = 0,
  Temporary = 1,
  Reservation = 2,
  Occupancy = 3,
}

export const PARKING_CLASS_LABELS: Record<ParkingAssignmentClass, string> = {
  [ParkingAssignmentClass.Holder]: 'Titular',
  [ParkingAssignmentClass.Temporary]: 'Asignación temporal',
  [ParkingAssignmentClass.Reservation]: 'Reserva',
  [ParkingAssignmentClass.Occupancy]: 'Ocupación',
};

export const PARKING_ASSIGNMENT_STATUS_LABELS = ['Vigente', 'Finalizada', 'Cancelada'];

export interface ParkingOccupant {
  assignmentId: string;
  plate?: string | null;
  unitLabel?: string | null;
  visitId?: string | null;
  since: string;
  isOverdue: boolean;
  isException: boolean;
}

export interface ParkingPlan {
  assignmentId: string;
  class: ParkingAssignmentClass;
  unitLabel?: string | null;
  plate?: string | null;
  startsAt: string;
  endsAt?: string | null;
}

export interface ParkingSpace {
  id: string;
  code: string;
  location?: string | null;
  type: ParkingType;
  vehicleKind: ParkingVehicleKind;
  notes?: string | null;
  isActive: boolean;
  state: ParkingState;
  holderUnitId?: string | null;
  holderUnitLabel?: string | null;
  holderPersonName?: string | null;
  occupant?: ParkingOccupant | null;
  plan?: ParkingPlan | null;
  outOfServiceId?: string | null;
  outOfServiceReason?: string | null;
  outOfServiceUntil?: string | null;
}

export interface ParkingAssignment {
  id: string;
  class: ParkingAssignmentClass;
  unitLabel?: string | null;
  personName?: string | null;
  plate?: string | null;
  startsAt: string;
  endsAt?: string | null;
  status: number;
  notes?: string | null;
  registeredBy?: string | null;
  isException: boolean;
  exceptionReason?: string | null;
}

export interface ParkingSpaceDetail {
  space: ParkingSpace;
  history: ParkingAssignment[];
  outOfService: { id: string; from: string; to?: string | null; reason: string }[];
  notes: { createdAt: string; authorName?: string | null; text: string }[];
}

export interface AvailabilityRow {
  type?: ParkingType | null;
  capacity: number;
  occupied: number;
  reserved: number;
  available: number;
  outOfService: number;
}

export interface Availability {
  byType: AvailabilityRow[];
  total: AvailabilityRow;
}

export interface SaveParkingSpace {
  code: string;
  location?: string | null;
  type: ParkingType;
  vehicleKind: ParkingVehicleKind;
  notes?: string | null;
}

export interface ParkingFilter {
  type?: ParkingType | null;
  state?: ParkingState | null;
  vehicleKind?: ParkingVehicleKind | null;
  location?: string;
  /** Periodo futuro para el que se consulta; sin él, ahora. */
  from?: string;
  to?: string;
}
