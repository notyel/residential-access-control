import { Person } from './person.model';

export enum AccessEventType {
  Entry = 0,
  Exit = 1,
  Rejection = 2,
  Void = 3,
}

export enum PersonKind {
  Visitor = 0,
  Resident = 1,
  Owner = 2,
}

/** Cómo se verificó un ingreso. */
export enum Verification {
  PriorAuthorization = 0,
  ResidentConfirmation = 1,
  GuardApproval = 2,
  Exception = 3,
}

export const VERIFICATION_LABELS: Record<Verification, string> = {
  [Verification.PriorAuthorization]: 'Autorización previa',
  [Verification.ResidentConfirmation]: 'Confirmó un residente',
  [Verification.GuardApproval]: 'Aprobó portería',
  [Verification.Exception]: 'Acceso excepcional',
};

export enum AuthorizationKind {
  Single = 0,
  Period = 1,
  Recurring = 2,
}

export const AUTHORIZATION_KIND_LABELS: Record<AuthorizationKind, string> = {
  [AuthorizationKind.Single]: 'Un solo ingreso',
  [AuthorizationKind.Period]: 'Por periodo',
  [AuthorizationKind.Recurring]: 'Recurrente',
};

/** Expired y NotStarted los calcula el servidor por fecha. */
export type AuthorizationState =
  | 'Active'
  | 'NotStarted'
  | 'Expired'
  | 'Revoked'
  | 'Consumed';

export const AUTHORIZATION_STATE_LABELS: Record<AuthorizationState, string> = {
  Active: 'Vigente',
  NotStarted: 'Por empezar',
  Expired: 'Vencida',
  Revoked: 'Revocada',
  Consumed: 'Usada',
};

/** 0 = domingo … 6 = sábado, como en el servidor. */
export const WEEK_DAYS: readonly { value: number; short: string; label: string }[] = [
  { value: 1, short: 'Lu', label: 'Lunes' },
  { value: 2, short: 'Ma', label: 'Martes' },
  { value: 3, short: 'Mi', label: 'Miércoles' },
  { value: 4, short: 'Ju', label: 'Jueves' },
  { value: 5, short: 'Vi', label: 'Viernes' },
  { value: 6, short: 'Sá', label: 'Sábado' },
  { value: 0, short: 'Do', label: 'Domingo' },
];

export interface Authorization {
  id: string;
  unitId: string;
  unitLabel: string;
  authorizedByPersonId: string;
  authorizedByName: string;
  visitorPersonId?: string | null;
  visitorName: string;
  visitorDocument?: string | null;
  reason: string;
  notes?: string | null;
  kind: AuthorizationKind;
  startDate: string;
  endDate: string;
  /** HH:mm:ss */
  timeFrom?: string | null;
  timeTo?: string | null;
  daysOfWeek: number[];
  state: AuthorizationState;
  revokeReason?: string | null;
  /** Veredicto del servidor en el instante de la consulta. */
  canEnterNow: boolean;
  cannotEnterReason?: string | null;
  createdAt: string;
}

export interface CreateAuthorization {
  unitId: string;
  authorizedByPersonId?: string | null;
  visitorName: string;
  visitorDocument?: string | null;
  reason?: string | null;
  notes?: string | null;
  kind: AuthorizationKind;
  startDate: string;
  endDate?: string | null;
  timeFrom?: string | null;
  timeTo?: string | null;
  daysOfWeek?: number[];
}

export interface ResidentMatch {
  personId: string;
  name: string;
  unitId: string;
  unitLabel: string;
  kind: PersonKind;
}

export interface LookupResult {
  authorizations: Authorization[];
  residents: ResidentMatch[];
  people: Person[];
}

/** Reglas del conjunto que orientan el formulario de portería; el servidor es quien decide. */
export interface GateContext {
  withoutAuthorization: 'not_allowed' | 'resident_confirmation' | 'guard_discretion';
  documentRequired: boolean;
}

export interface VisitorPerson {
  firstName: string;
  lastName: string;
  documentType?: string | null;
  documentNumber?: string | null;
}

export interface CheckIn {
  unitId: string;
  personId?: string | null;
  newPerson?: VisitorPerson | null;
  reason?: string | null;
  vehiclePlate?: string | null;
  vehicleType?: string | null;
  vehicleBrand?: string | null;
  vehicleColor?: string | null;
  /** Parqueadero de visitantes en el que se estaciona; se libera solo al salir el vehículo. */
  parkingSpaceId?: string | null;
  authorizationId?: string | null;
  confirmedByPersonId?: string | null;
  confirmationChannel?: string | null;
  isException?: boolean;
  exceptionReason?: string | null;
  notes?: string | null;
}

export interface Reject {
  unitId?: string | null;
  personId?: string | null;
  personName: string;
  personDocument?: string | null;
  reasonCode: string;
  notes?: string | null;
  vehiclePlate?: string | null;
}

export interface Visit {
  id: string;
  person: Person;
  unitId: string;
  unitLabel?: string | null;
  reason: string;
  reasonNote?: string | null;
  vehicleId?: string | null;
  vehiclePlate?: string | null;
  /** El visitante ya salió pero su vehículo sigue dentro. */
  vehicleStillInside?: boolean;
  /** Parqueadero que ocupa su vehículo mientras está dentro. */
  parkingSpaceCode?: string | null;
  verification: Verification;
  authorizationId?: string | null;
  authorizedByName?: string | null;
  exceptionReason?: string | null;
  entryEventId: string;
  entryAt: string;
  entryOperator?: string | null;
  exitEventId?: string | null;
  exitAt?: string | null;
  exitOperator?: string | null;
  exitIsAdministrative: boolean;
  isVoided: boolean;
  /** Sigue dentro y superó las horas que el conjunto definió para revisarla. */
  needsReview: boolean;
}

/** Inside: sin salida registrada. CheckedOut: con salida. */
export type VisitStatus = 'Inside' | 'CheckedOut';

export interface VisitFilter {
  pageNumber: number;
  pageSize: number;
  startDate?: Date;
  endDate?: Date;
  status?: VisitStatus;
  unitId?: string;
  search?: string;
  plate?: string;
  verification?: Verification | null;
}

export interface AccessEvent {
  id: string;
  type: AccessEventType;
  occurredAt: string;
  personName: string;
  personDocument?: string | null;
  personKind: PersonKind;
  unitLabel?: string | null;
  visitId?: string | null;
  vehiclePlate?: string | null;
  operatorName: string;
  reason?: string | null;
  notes?: string | null;
  isVoided: boolean;
}

export function visitorName(visit: Visit): string {
  return `${visit.person.firstName} ${visit.person.lastName}`;
}

/** "NA" es el tipo interno de quien ingresó sin documento. */
export function visitorDocument(person: Person): string {
  return person.documentType === 'NA'
    ? 'Sin documento'
    : `${person.documentType} ${person.documentNumber}`;
}

/** Resumen legible de cuándo vale una autorización. */
export function authorizationWindow(a: Authorization): string {
  const hours =
    a.timeFrom && a.timeTo ? `, ${a.timeFrom.slice(0, 5)}–${a.timeTo.slice(0, 5)}` : '';
  if (a.kind === AuthorizationKind.Recurring) {
    const days = WEEK_DAYS.filter((d) => a.daysOfWeek.includes(d.value))
      .map((d) => d.short)
      .join(' ');
    return `${days}${hours}`;
  }
  return hours ? hours.slice(2) : 'Todo el día';
}
