import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Check, LucideAngularModule } from 'lucide-angular';
import { forkJoin, map, of } from 'rxjs';
import { Role } from '../../../../../core/models/role.model';
import { AuthService } from '../../../../../core/services/auth.service';
import { apiErrorMessage } from '../../../../../core/utils/api-error';
import { FormSectionComponent } from '../../../../../shared/components/form-section/form-section.component';
import { PageActionsDirective } from '../../../../../shared/directives/page-actions.directive';
import { RolesService } from '../../../roles/services/roles.service';
import { UsersService } from '../../services/users.service';

const USERS_URL = '/access-control/users';

/** Alta (invitación) y edición de un usuario: datos y roles. */
@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    LucideAngularModule,
    FormSectionComponent,
    PageActionsDirective,
  ],
  templateUrl: './user-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserFormComponent implements OnInit {
  private readonly usersService = inject(UsersService);
  private readonly rolesService = inject(RolesService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly userId = inject(ActivatedRoute).snapshot.paramMap.get('id');
  readonly isNew = this.userId === null;

  readonly form = inject(FormBuilder).nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
  });

  readonly roles = signal<Role[]>([]);
  readonly selected = signal<ReadonlySet<string>>(new Set());
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly SaveIcon = Check;
  readonly cancelUrl = USERS_URL;

  private readonly formValid = toSignal(
    this.form.statusChanges.pipe(map((status) => status === 'VALID')),
    { initialValue: this.form.valid }
  );
  readonly canSave = computed(
    () =>
      this.formValid() &&
      this.selected().size > 0 &&
      !this.saving() &&
      !this.loading()
  );

  ngOnInit(): void {
    forkJoin({
      roles: this.rolesService.getAll(),
      user: this.userId ? this.usersService.getById(this.userId) : of(null),
    }).subscribe({
      next: ({ roles, user }) => {
        const current = new Set(user?.roles.map((role) => role.id) ?? []);
        // un rol inactivo solo se muestra si el usuario ya lo tiene, para poder quitárselo
        this.roles.set(roles.filter((role) => role.isActive || current.has(role.id)));
        this.selected.set(current);
        if (user) {
          this.form.patchValue(user);
          // el correo identifica la cuenta: no se cambia
          this.form.controls.email.disable();
        }
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudieron cargar los datos.');
        this.loading.set(false);
      },
    });
  }

  /** Nadie asigna un rol con permisos que él mismo no tiene; sí puede quitarlo. */
  isDisabled(role: Role): boolean {
    if (this.selected().has(role.id)) {
      return false;
    }
    return (
      !role.isActive ||
      role.permissions.some((p) => !this.authService.hasPermission(p))
    );
  }

  toggle(roleId: string, checked: boolean): void {
    this.selected.update((current) => {
      const next = new Set(current);
      if (checked) {
        next.add(roleId);
      } else {
        next.delete(roleId);
      }
      return next;
    });
  }

  onSubmit(): void {
    if (!this.canSave()) {
      this.form.markAllAsTouched();
      return;
    }

    const { firstName, lastName, email } = this.form.getRawValue();
    const roleIds = [...this.selected()];
    const request = this.userId
      ? this.usersService.update(this.userId, { firstName, lastName, roleIds })
      : this.usersService.create({ firstName, lastName, email, roleIds });

    this.saving.set(true);
    this.errorMessage.set('');

    request.subscribe({
      next: () => this.router.navigate([USERS_URL]),
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(
          apiErrorMessage(err, 'No se pudo guardar el usuario.')
        );
      },
    });
  }
}
