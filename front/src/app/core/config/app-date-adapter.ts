import { Injectable } from '@angular/core';
import { NativeDateAdapter } from '@angular/material/core';

const DAY_MONTH_YEAR = /^(\d{1,2})[/.-](\d{1,2})[/.-](\d{4})$/;

/**
 * El adaptador nativo interpreta las fechas escritas como mes/día/año.
 * Aquí se leen como día/mes/año, que es como se muestran.
 */
@Injectable()
export class AppDateAdapter extends NativeDateAdapter {
  override parse(value: unknown, parseFormat?: unknown): Date | null {
    if (typeof value === 'string') {
      const match = DAY_MONTH_YEAR.exec(value.trim());
      if (match) {
        const [, day, month, year] = match.map(Number);
        const date = new Date(year, month - 1, day);
        // Rechaza fechas inexistentes como 31/02, que Date desbordaría
        return date.getMonth() === month - 1 && date.getDate() === day
          ? date
          : this.invalid();
      }
    }
    return super.parse(value, parseFormat);
  }
}
