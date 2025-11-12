import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule, LucideAngularModule],
  templateUrl: './page-header.component.html',
  styleUrls: ['./page-header.component.scss'],
})
export class PageHeaderComponent {
  @Input() title: string = '';
  @Input() subtitle?: string;
  @Input() showButton: boolean = false;
  @Input() buttonText?: string;
  @Input() buttonIcon?: any;
  @Input() buttonColor: 'primary' | 'accent' | 'warn' = 'primary';
  @Input() buttonRoute?: string | string[];
  @Input() showBadge: boolean = false;
  @Input() badgeText?: string;
  @Input() badgeColor: string = 'primary';
}
