import { Component } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { Home, LayoutGrid, Users, Settings, Box, LogOut } from 'lucide-angular';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  homeIcon = Home;
  dashboardIcon = LayoutGrid;
  usersIcon = Users;
  settingsIcon = Settings;
  boxIcon = Box;
  logOutIcon = LogOut;
}
