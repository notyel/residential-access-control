import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { MenuService } from '../../../../../core/services/menu.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, LucideAngularModule],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss'],
})
export class SidebarComponent implements OnInit {
  menuService = inject(MenuService);

  ngOnInit(): void {
    this.menuService.getMenuForCurrentUser().subscribe({
      next: () => {},
      error: (err) =>
        console.error('Error loading menu in sidebar:', err),
    });
  }
}
