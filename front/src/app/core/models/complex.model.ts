export enum ComplexType {
  Conjunto = 0,
  Edificio = 1,
  PropiedadConPorteria = 2,
}

export const COMPLEX_TYPE_LABELS: Record<ComplexType, string> = {
  [ComplexType.Conjunto]: 'Conjunto residencial',
  [ComplexType.Edificio]: 'Edificio',
  [ComplexType.PropiedadConPorteria]: 'Propiedad con portería',
};

export interface Complex {
  id: string;
  name: string;
  type: ComplexType;
  address?: string | null;
  timeZoneId: string;
  isActive: boolean;
  createdAt: string;
}

export interface SaveComplex {
  name: string;
  type: ComplexType;
  address?: string | null;
  timeZoneId: string;
}

export interface CreateComplex extends SaveComplex {
  adminEmail: string;
  adminFirstName: string;
  adminLastName: string;
  adminPassword: string;
}
