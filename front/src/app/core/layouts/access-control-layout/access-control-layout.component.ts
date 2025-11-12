import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from '../../../modules/access-control/components/header/header.component';
import { SidebarComponent } from '../../../modules/access-control/components/sidebar/sidebar.component';

@Component({
  selector: 'app-access-control-layout',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, SidebarComponent],
  templateUrl: './access-control-layout.component.html',
  styleUrl: './access-control-layout.component.scss',
})
export class AccessControlLayoutComponent {}
