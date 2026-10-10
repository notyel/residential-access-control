import { Injectable, signal } from '@angular/core';

export interface SelectedComplex {
  id: string;
  name: string;
}

const STORAGE_KEY = 'selected-complex';

/**
 * Conjunto sobre el que trabaja el superadministrador. Se envía en cada petición como
 * X-Complex-Id; el servidor lo ignora si quien llama no es superadministrador.
 */
@Injectable({ providedIn: 'root' })
export class ComplexContextService {
  readonly selected = signal<SelectedComplex | null>(this.read());

  select(complex: SelectedComplex): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(complex));
    this.selected.set(complex);
  }

  clear(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.selected.set(null);
  }

  private read(): SelectedComplex | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as SelectedComplex) : null;
    } catch {
      return null;
    }
  }
}
