export interface Person {
  id: string;
  firstName: string;
  lastName: string;
  documentType: string;
  documentNumber: string;
  phone?: string;
  email?: string;
}

export interface CreatePerson {
  firstName: string;
  lastName:string;
  documentType: string;
  documentNumber: string;
  phone?: string;
  email?: string;
}

export interface UpdatePerson {
  firstName: string;
  lastName: string;
  documentType: string;
  phone?: string;
  email?: string;
}
