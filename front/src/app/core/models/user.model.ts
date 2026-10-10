export enum Role {
  Admin = 0,
  Guard = 1,
  Owner = 2,
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  apartmentNumber: string;
  role: number;
}
