import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [RouterOutlet],
  template: `
    <div class="dashboard-layout">
      <router-outlet></router-outlet>
    </div>
  `,
  styles: [
    `
      .dashboard-layout {
        display: flex;
        flex-direction: column;
        min-height: 100vh;
        padding: 2rem;
        background-color: #f8fafc;
      }
    `,
  ],
})
export class DashboardLayoutComponent {}
