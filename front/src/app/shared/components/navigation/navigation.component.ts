import { Component, OnInit, inject, signal } from '@angular/core';
import { MenuItem, MenuService } from '../../../core/services/menu.service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { IfRoleDirective } from '../../directives/if-role.directive';

@Component({
  selector: 'app-navigation',
  standalone: true,
  imports: [CommonModule, RouterModule, IfRoleDirective],
  templateUrl: './navigation.component.html',
  styleUrls: ['./navigation.component.scss']
})
export class NavigationComponent implements OnInit {
  menu = signal<MenuItem[]>([]);

  private menuService = inject(MenuService);

  ngOnInit(): void {
    this.menuService.getMenu().subscribe(items => this.menu.set(items));
  }
}
