export interface Visit {
  id: string;
  visitorName: string;
  visitorId: string;
  vehiclePlate?: string;
  createdAt: Date;
  checkOut?: Date;
  residenceId: string;
}
