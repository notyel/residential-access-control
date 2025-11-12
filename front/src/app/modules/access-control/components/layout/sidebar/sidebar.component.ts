import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { LucideAngularModule, LogOut, Box } from 'lucide-angular';
import { MenuService } from '../../../../../core/services/menu.service';
import { AuthService } from '../../../../../core/services/auth.service';
import { IconService } from '../../../../../core/services/icon.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, LucideAngularModule],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss'],
})
export class SidebarComponent implements OnInit {
  menuService = inject(MenuService);
  authService = inject(AuthService);
  iconService = inject(IconService);
  router = inject(Router);

  readonly LogOut = LogOut;
  readonly Box = Box;

  ngOnInit(): void {
    this.menuService.getMenuForCurrentUser().subscribe();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  getMenuIcon(iconName: string) {
    return this.iconService.getIcon(iconName);
  }
}
