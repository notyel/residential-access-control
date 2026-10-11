export interface CatalogItem {
  code: string;
  label: string;
  order: number;
  isActive: boolean;
}

export interface Structure {
  id: string;
  parentId?: string | null;
  type: string;
  name: string;
  /** Nombre con sus ancestros: "Torre A · Piso 3". */
  path: string;
  isActive: boolean;
  unitCount: number;
}

/** Estado de ocupación; lo calcula el servidor de las ocupaciones vigentes. */
export enum UnitOccupancyState {
  Vacant = 0,
  OwnerOccupied = 1,
  Rented = 2,
}

export const OCCUPANCY_STATE_LABELS: Record<UnitOccupancyState, string> = {
  [UnitOccupancyState.Vacant]: 'Desocupada',
  [UnitOccupancyState.OwnerOccupied]: 'Ocupada por propietario',
  [UnitOccupancyState.Rented]: 'Arrendada',
};

export interface Unit {
  id: string;
  code: string;
  type: string;
  structureId?: string | null;
  label: string;
  area?: number | null;
  notes?: string | null;
  isActive: boolean;
  occupancyState: UnitOccupancyState;
  owners: string[];
}

export interface SaveUnit {
  code: string;
  type: string;
  structureId?: string | null;
  area?: number | null;
  notes?: string | null;
}

export interface BulkUnits {
  type: string;
  structureId?: string | null;
  prefix?: string | null;
  floorFrom: number;
  floorTo: number;
  unitsPerFloor: number;
  preview: boolean;
}

export interface BulkUnitsResult {
  created: string[];
  skipped: string[];
}

export interface Ownership {
  id: string;
  unitId: string;
  unitLabel: string;
  personId: string;
  personName: string;
  startDate: string;
  endDate?: string | null;
  percentage?: number | null;
  isCurrent: boolean;
}

export enum OccupancyRelation {
  OwnerResident = 0,
  Tenant = 1,
  Family = 2,
  Other = 3,
}

export const RELATION_LABELS: Record<OccupancyRelation, string> = {
  [OccupancyRelation.OwnerResident]: 'Propietario residente',
  [OccupancyRelation.Tenant]: 'Arrendatario',
  [OccupancyRelation.Family]: 'Familiar',
  [OccupancyRelation.Other]: 'Otro ocupante autorizado',
};

export enum OccupancyStatus {
  Pending = 0,
  Active = 1,
  Ended = 2,
  Rejected = 3,
}

export const OCCUPANCY_STATUS_LABELS: Record<OccupancyStatus, string> = {
  [OccupancyStatus.Pending]: 'Pendiente de aprobación',
  [OccupancyStatus.Active]: 'Vigente',
  [OccupancyStatus.Ended]: 'Finalizada',
  [OccupancyStatus.Rejected]: 'Rechazada',
};

export interface Occupancy {
  id: string;
  unitId: string;
  unitLabel: string;
  personId: string;
  personName: string;
  relation: OccupancyRelation;
  startDate: string;
  endDate?: string | null;
  status: OccupancyStatus;
  statusReason?: string | null;
  /** Vigente hoy: aprobada y con hoy dentro de su periodo. */
  isCurrent: boolean;
}

export interface UnitSummary {
  unit: Unit;
  owners: Ownership[];
  occupants: Occupancy[];
  pendingOccupancies: Occupancy[];
  /** null cuando quien consulta no puede ver el historial (vista de portería). */
  pastOwners?: Ownership[] | null;
  pastOccupants?: Occupancy[] | null;
  /** Vehículos con autorización en curso en la unidad. */
  vehicles?: import('./vehicle.model').Vehicle[];
  /** Parqueaderos privados de los que la unidad es titular. */
  parking?: import('./parking.model').ParkingSpace[];
}

export interface PersonListItem {
  id: string;
  firstName: string;
  lastName: string;
  documentType: string;
  documentNumber: string;
  phone?: string | null;
  email?: string | null;
  ownedUnits: string[];
  occupiedUnits: string[];
  hasUser: boolean;
}

/** Unidad del usuario del portal, con su vínculo. */
export interface MyUnit {
  id: string;
  label: string;
  type: string;
  isOwner: boolean;
  relation?: OccupancyRelation | null;
  startDate?: string | null;
  endDate?: string | null;
  occupancyState: UnitOccupancyState;
  occupants: string[];
}

/** Fecha de hoy en formato yyyy-MM-dd, para los campos de fecha. */
export function todayIso(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}
