import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  LucideAngularModule,
  User as UserIconLucide,
  SquarePen as EditIconLucide,
  Building2,
  Mail,
} from 'lucide-angular';
import { User } from '../../../../../core/models/user.model';

@Component({
  selector: 'app-resident-card',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    LucideAngularModule,
  ],
  templateUrl: './resident-card.component.html',
  styleUrls: ['./resident-card.component.scss'],
})
export class ResidentCardComponent {
  @Input({ required: true }) resident!: User;

  // Icons
  UserIcon = UserIconLucide;
  EditIcon = EditIconLucide;
  BuildingIcon = Building2;
  MailIcon = Mail;
}
