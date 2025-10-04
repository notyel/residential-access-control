import { Component } from '@angular/core';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [],
  template: `
    <div class="reports-page">
      <h1>Reportes</h1>
      <p>Aquí irán los reportes del sistema.</p>
    </div>
  `,
  styles: [
    `
      .reports-page {
        padding: 2rem;
      }

      h1 {
        margin-bottom: 1rem;
        color: #374151;
      }
    `,
  ],
})
export class ReportsComponent {}
