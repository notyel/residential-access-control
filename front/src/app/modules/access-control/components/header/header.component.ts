import {
  Component,
  EventEmitter,
  Output,
  OnInit,
  OnDestroy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule, Search, Menu } from 'lucide-angular';
import { AuthService } from '../../../../core/services/auth.service';
import { User } from '../../../../core/models/user.model';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [LucideAngularModule, CommonModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
})
export class HeaderComponent implements OnInit, OnDestroy {
  readonly SearchIcon = Search;
  readonly MenuIcon = Menu;

  currentUser: User | null = null;
  userRole: string | null = null;
  private subscription!: Subscription;

  @Output() toggleSidebar = new EventEmitter<void>();

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.subscription = this.authService.user$.subscribe((user) => {
      this.currentUser = user;
      this.userRole = this.authService.getUserRole();
    });
  }

  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }

  get displayName(): string {
    if (!this.currentUser) return 'Usuario';

    if (this.currentUser.firstName || this.currentUser.lastName) {
      return `${this.currentUser.firstName || ''} ${
        this.currentUser.lastName || ''
      }`.trim();
    }

    return this.currentUser.email.split('@')[0];
  }

  onToggleSidebar() {
    this.toggleSidebar.emit();
  }
}
