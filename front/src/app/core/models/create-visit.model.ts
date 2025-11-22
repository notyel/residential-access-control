import { CreatePerson } from "./person.model";

export interface CreateVisit {
  personId?: string;
  newPerson?: CreatePerson;
  vehiclePlate?: string;
  residenceId: string;
}
