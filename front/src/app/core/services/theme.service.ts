import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  isDarkTheme = signal<boolean>(false);

  constructor() {
    this.initializeTheme();
  }

  initializeTheme() {
    if (typeof window !== 'undefined') {
      const storedTheme = localStorage.getItem('isDarkTheme');
      if (storedTheme) {
        this.isDarkTheme.set(JSON.parse(storedTheme));
      }
      this.updateTheme();
    }
  }

  toggleTheme() {
    this.isDarkTheme.set(!this.isDarkTheme());
    if (typeof window !== 'undefined') {
      localStorage.setItem('isDarkTheme', JSON.stringify(this.isDarkTheme()));
    }
    this.updateTheme();
  }

  private updateTheme() {
    if (typeof window !== 'undefined') {
      if (this.isDarkTheme()) {
        document.body.setAttribute('data-theme', 'dark');
      } else {
        document.body.setAttribute('data-theme', 'light');
      }
    }
  }
}
