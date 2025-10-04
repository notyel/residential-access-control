import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-auth-layout',
  standalone: true,
  imports: [RouterOutlet],
  template: `
    <div class="auth-layout">
      <router-outlet></router-outlet>
    </div>
  `,
  styles: [
    `
      .auth-layout {
        min-height: 100vh;
        width: 100%;
      }
    `,
  ],
})
export class AuthLayoutComponent {}
