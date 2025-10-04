import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from '../../../modules/dashboard/components/header/header.component';
import { SidebarComponent } from '../../../modules/dashboard/components/sidebar/sidebar.component';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, SidebarComponent],
  template: `
    <div class="dashboard-layout">
      <app-sidebar></app-sidebar>
      <main class="dashboard-main">
        <app-header></app-header>
        <div class="dashboard-content">
          <router-outlet></router-outlet>
        </div>
      </main>
    </div>
  `,
  styles: [
    `
      .dashboard-layout {
        display: flex;
        min-height: 100vh;
      }

      .dashboard-main {
        flex: 1;
        display: flex;
        flex-direction: column;
      }

      .dashboard-content {
        flex: 1;
        padding: 2rem;
        background-color: #f8fafc;
      }
    `,
  ],
})
export class DashboardLayoutComponent {}
