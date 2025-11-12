import { Component, HostListener, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HeaderComponent } from '../../../modules/access-control/components/header/header.component';
import { SidebarComponent } from '../../../modules/access-control/components/sidebar/sidebar.component';

@Component({
  selector: 'app-access-control-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, HeaderComponent, SidebarComponent],
  templateUrl: './access-control-layout.component.html',
  styleUrl: './access-control-layout.component.scss',
})
export class AccessControlLayoutComponent implements OnInit {
  isMobile = false;
  sidebarVisible = true;

  ngOnInit() {
    this.checkScreenSize();
  }

  @HostListener('window:resize', ['$event'])
  onResize(event: any) {
    this.checkScreenSize();
  }

  private checkScreenSize() {
    this.isMobile = window.innerWidth <= 768;
    if (this.isMobile) {
      this.sidebarVisible = false;
    } else {
      this.sidebarVisible = true;
    }
  }

  toggleSidebar() {
    this.sidebarVisible = !this.sidebarVisible;
  }
}
