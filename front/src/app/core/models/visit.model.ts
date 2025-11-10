export interface Visit {
  id: string;
  visitorName: string;
  visitorId: string;
  vehiclePlate?: string;
  checkOut?: Date;
  residenceId: string;
  residenceIdentifier?: string;
  registeredById: string;
  registeredByFullName?: string;
  createdAt: Date;
}
