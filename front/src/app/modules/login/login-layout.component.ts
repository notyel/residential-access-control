import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-login-layout',
  standalone: true,
  imports: [RouterOutlet],
  template: `
    <div class="login-layout-container">
      <router-outlet></router-outlet>
    </div>
  `,
  styleUrls: ['./login-layout.component.scss'],
})
export class LoginLayoutComponent {}
