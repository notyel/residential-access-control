import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { LucideAngularModule, LogOut, Box } from 'lucide-angular';
import { MenuService } from '../../../../core/services/menu.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, LucideAngularModule],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss'],
})
export class SidebarComponent implements OnInit {
  menuService = inject(MenuService);
  readonly LogOut = LogOut;
  readonly Box = Box;

  ngOnInit(): void {
    this.menuService.getMenuForCurrentUser().subscribe();
  }
}
