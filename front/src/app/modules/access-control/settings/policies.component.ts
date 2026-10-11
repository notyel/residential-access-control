import { HttpClient } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { Check, LucideAngularModule } from 'lucide-angular';
import { map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { P } from '../../../core/security/permissions';
import { AuthService } from '../../../core/services/auth.service';
import { ResponseModel } from '../../../core/types/response.model';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { FormSectionComponent } from '../../../shared/components/form-section/form-section.component';
import { PageActionsDirective } from '../../../shared/directives/page-actions.directive';

interface Policy {
  key: string;
  group: string;
  label: string;
  description: string;
  kind: 'Boolean' | 'Number' | 'Choice';
  value: string;
  defaultValue: string;
  isDefault: boolean;
  min?: number | null;
  max?: number | null;
  options?: { value: string; label: string }[] | null;
}

interface PolicyGroup {
  name: string;
  policies: Policy[];
}

/** Reglas de operación del conjunto. Quien las aplica es el servidor; aquí solo se ajustan. */
@Component({
  selector: 'app-policies',
  standalone: true,
  imports: [
    MatButtonModule,
    LucideAngularModule,
    FormSectionComponent,
    PageActionsDirective,
  ],
  templateUrl: './policies.component.html',
  styleUrl: './policies.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PoliciesComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/policies`;

  readonly canManage = inject(AuthService).hasPermission(P.settingsManage);

  readonly policies = signal<Policy[]>([]);
  /** Valores cambiados y aún sin guardar, por clave. */
  readonly draft = signal<Readonly<Record<string, string>>>({});
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly message = signal<{ text: string; isError: boolean } | null>(null);
  readonly SaveIcon = Check;

  readonly groups = computed<PolicyGroup[]>(() => {
    const groups: PolicyGroup[] = [];
    for (const policy of this.policies()) {
      const group = groups.find((g) => g.name === policy.group);
      if (group) {
        group.policies.push(policy);
      } else {
        groups.push({ name: policy.group, policies: [policy] });
      }
    }
    return groups;
  });

  readonly dirty = computed(() => Object.keys(this.draft()).length > 0);

  ngOnInit(): void {
    this.http
      .get<ResponseModel<Policy[]>>(this.apiUrl)
      .pipe(map((response) => response.data ?? []))
      .subscribe({
        next: (policies) => {
          this.policies.set(policies);
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
          this.message.set({
            text: apiErrorMessage(err, 'No se pudieron cargar las políticas.'),
            isError: true,
          });
        },
      });
  }

  valueOf(policy: Policy): string {
    return this.draft()[policy.key] ?? policy.value;
  }

  defaultLabel(policy: Policy): string {
    if (policy.kind === 'Boolean') {
      return policy.defaultValue === 'true' ? 'Sí' : 'No';
    }
    return (
      policy.options?.find((o) => o.value === policy.defaultValue)?.label ??
      policy.defaultValue
    );
  }

  set(policy: Policy, event: Event): void {
    const value = (event.target as HTMLInputElement | HTMLSelectElement).value;
    this.message.set(null);
    this.draft.update((draft) => {
      const next = { ...draft };
      if (value === policy.value) {
        delete next[policy.key];
      } else {
        next[policy.key] = value;
      }
      return next;
    });
  }

  save(): void {
    if (!this.dirty() || this.saving()) {
      return;
    }
    this.saving.set(true);
    this.message.set(null);

    this.http
      .put<ResponseModel<Policy[]>>(this.apiUrl, this.draft())
      .pipe(map((response) => response.data ?? []))
      .subscribe({
        next: (policies) => {
          this.policies.set(policies);
          this.draft.set({});
          this.saving.set(false);
          this.message.set({ text: 'Políticas guardadas.', isError: false });
        },
        error: (err) => {
          this.saving.set(false);
          this.message.set({
            text: apiErrorMessage(err, 'No se pudieron guardar las políticas.'),
            isError: true,
          });
        },
      });
  }
}
