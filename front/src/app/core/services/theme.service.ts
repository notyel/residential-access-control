import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';

const STORAGE_KEY = 'isDarkTheme';

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  private readonly document = inject(DOCUMENT);

  readonly isDarkTheme = signal<boolean>(this.readPreference());

  constructor() {
    this.applyTheme();
  }

  toggleTheme(): void {
    this.isDarkTheme.update((dark) => !dark);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.isDarkTheme()));
    } catch {
      // Sin almacenamiento disponible el tema solo dura la sesión
    }
    this.applyTheme();
  }

  private readPreference(): boolean {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored !== null) {
        return JSON.parse(stored) === true;
      }
    } catch {
      // Se usa la preferencia del sistema
    }
    return (
      this.document.defaultView?.matchMedia('(prefers-color-scheme: dark)')
        .matches ?? false
    );
  }

  private applyTheme(): void {
    this.document.documentElement.setAttribute(
      'data-theme',
      this.isDarkTheme() ? 'dark' : 'light'
    );
  }
}
