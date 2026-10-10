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
import { PermissionDefinition, Role } from '../../../../../core/models/role.model';
import { P } from '../../../../../core/security/permissions';
import { AuthService } from '../../../../../core/services/auth.service';
import { apiErrorMessage } from '../../../../../core/utils/api-error';
import { FormSectionComponent } from '../../../../../shared/components/form-section/form-section.component';
import { PageActionsDirective } from '../../../../../shared/directives/page-actions.directive';
import { RolesService } from '../../services/roles.service';

const ROLES_URL = '/access-control/roles';

interface PermissionGroup {
  module: string;
  permissions: PermissionDefinition[];
}

/** Alta, duplicado y edición de un rol: datos y matriz de permisos agrupada por módulo. */
@Component({
  selector: 'app-role-form',
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
  templateUrl: './role-form.component.html',
  styleUrl: './role-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoleFormComponent implements OnInit {
  private readonly rolesService = inject(RolesService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly roleId = this.route.snapshot.paramMap.get('id');
  /** Rol del que se copian los permisos al duplicar. */
  private readonly cloneFromId = this.route.snapshot.queryParamMap.get('from');

  readonly canManage = this.authService.hasPermission(P.rolesManage);

  readonly form = inject(FormBuilder).nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', Validators.maxLength(300)],
  });

  readonly role = signal<Role | null>(null);
  readonly groups = signal<PermissionGroup[]>([]);
  readonly selected = signal<ReadonlySet<string>>(new Set());
  /** Permisos que el rol ya tenía al abrirlo: se pueden conservar aunque quien edita no los tenga. */
  private readonly original = signal<ReadonlySet<string>>(new Set());

  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly saving = signal(false);
  readonly SaveIcon = Check;
  readonly cancelUrl = ROLES_URL;

  private readonly formValid = toSignal(
    this.form.statusChanges.pipe(map((status) => status === 'VALID')),
    { initialValue: this.form.valid }
  );
  readonly canSave = computed(
    () => this.canManage && this.formValid() && !this.saving() && !this.loading()
  );

  ngOnInit(): void {
    const sourceId = this.roleId ?? this.cloneFromId;

    forkJoin({
      catalog: this.rolesService.getCatalog(),
      source: sourceId ? this.rolesService.getById(sourceId) : of(null),
    }).subscribe({
      next: ({ catalog, source }) => {
        this.groups.set(this.group(catalog));
        if (source) {
          this.selected.set(new Set(source.permissions));
          if (this.roleId) {
            this.role.set(source);
            this.original.set(new Set(source.permissions));
            this.form.patchValue({
              name: source.name,
              description: source.description ?? '',
            });
            if (source.isSystem) {
              // otras partes del sistema identifican los roles de sistema por su nombre
              this.form.controls.name.disable();
            }
          } else {
            this.form.patchValue({ name: `${source.name} (copia)` });
          }
        }
        if (!this.canManage) {
          this.form.disable();
        }
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudo cargar el rol.');
        this.loading.set(false);
      },
    });
  }

  isChecked(key: string): boolean {
    return this.selected().has(key);
  }

  /** Nadie concede un permiso que no tiene; sí puede quitarlo o dejar el que el rol ya tenía. */
  isDisabled(key: string): boolean {
    if (!this.canManage) {
      return true;
    }
    return (
      !this.authService.hasPermission(key) &&
      !this.original().has(key) &&
      !this.selected().has(key)
    );
  }

  toggle(key: string, checked: boolean): void {
    this.selected.update((current) => {
      const next = new Set(current);
      if (checked) {
        next.add(key);
      } else {
        next.delete(key);
      }
      return next;
    });
  }

  selectedIn(group: PermissionGroup): number {
    return group.permissions.filter((p) => this.selected().has(p.key)).length;
  }

  onSubmit(): void {
    if (!this.canSave()) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, description } = this.form.getRawValue();
    const body = { name, description, permissions: [...this.selected()] };
    const request = this.roleId
      ? this.rolesService.update(this.roleId, body)
      : this.rolesService.create(body);

    this.saving.set(true);
    this.errorMessage.set('');

    request.subscribe({
      next: () => {
        // el rol editado puede ser uno de los del propio usuario
        this.authService.refresh().subscribe();
        this.router.navigate([ROLES_URL]);
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(apiErrorMessage(err, 'No se pudo guardar el rol.'));
      },
    });
  }

  private group(catalog: PermissionDefinition[]): PermissionGroup[] {
    const groups = new Map<string, PermissionDefinition[]>();
    for (const permission of catalog) {
      const list = groups.get(permission.module) ?? [];
      list.push(permission);
      groups.set(permission.module, list);
    }
    return [...groups.entries()].map(([module, permissions]) => ({
      module,
      permissions,
    }));
  }
}
