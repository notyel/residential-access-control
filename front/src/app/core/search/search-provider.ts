import { InjectionToken } from '@angular/core';
import { LucideIconData } from 'lucide-angular';
import { Observable } from 'rxjs';

export interface SearchResult {
  id: string;
  /** Encabezado bajo el que se agrupa el resultado. */
  group: string;
  title: string;
  subtitle?: string;
  icon: LucideIconData;
  run: () => void;
}

/** Fuente de resultados de la búsqueda por comando. */
export interface SearchProvider {
  search(term: string): Observable<SearchResult[]>;
}

export const SEARCH_PROVIDERS = new InjectionToken<SearchProvider[]>(
  'SEARCH_PROVIDERS'
);

/** Normaliza un texto para compararlo sin mayúsculas ni tildes. */
export function normalizeSearchText(value: string): string {
  return value
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .trim();
}
