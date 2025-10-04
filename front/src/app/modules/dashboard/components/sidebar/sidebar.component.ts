import { Component } from '@angular/core';
import {
  LucideAngularModule,
  Box,
  Home,
  LayoutGrid,
  Users,
  Settings,
  LogOut,
} from 'lucide-angular';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
})
export class SidebarComponent {
  readonly BoxIcon = Box;
  readonly HomeIcon = Home;
  readonly LayoutGridIcon = LayoutGrid;
  readonly UsersIcon = Users;
  readonly SettingsIcon = Settings;
  readonly LogOutIcon = LogOut;
}
