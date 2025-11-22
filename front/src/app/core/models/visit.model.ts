import { Person } from "./person.model";

export interface Visit {
  id: string;
  person: Person;
  vehiclePlate?: string;
  checkOut?: Date;
  residenceId: string;
  residenceIdentifier?: string;
  registeredById: string;
  registeredByFullName?: string;
  createdAt: Date;
}
