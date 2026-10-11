import { HttpClient } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Box, LucideAngularModule } from 'lucide-angular';
import { environment } from '../../../../../environments/environment';
import { ResponseModel } from '../../../../core/types/response.model';
import { apiErrorMessage } from '../../../../core/utils/api-error';

/** forgot: pedir el enlace. reset / invitation: definir la contraseña con el enlace recibido. */
export type AccountMode = 'forgot' | 'reset' | 'invitation';

const MIN_PASSWORD_LENGTH = 10;

const TEXTS: Record<AccountMode, { title: string; intro: string; action: string }> = {
  forgot: {
    title: 'Recuperar contraseña',
    intro: 'Escriba su correo y le enviaremos un enlace para definir una contraseña nueva.',
    action: 'Enviar enlace',
  },
  reset: {
    title: 'Nueva contraseña',
    intro: 'Defina la contraseña con la que seguirá entrando. Sus otras sesiones se cerrarán.',
    action: 'Guardar contraseña',
  },
  invitation: {
    title: 'Bienvenido a Zentryal',
    intro: 'Defina su contraseña para activar la cuenta que le creó la administración.',
    action: 'Activar mi cuenta',
  },
};

/** Páginas públicas de la cuenta: recuperación de contraseña y aceptación de invitación. */
@Component({
  selector: 'app-account',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    LucideAngularModule,
  ],
  templateUrl: './account.component.html',
  styleUrl: './account.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountComponent {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);

  readonly mode = this.route.snapshot.data['mode'] as AccountMode;
  readonly texts = TEXTS[this.mode];
  private readonly token = this.route.snapshot.queryParamMap.get('token');
  /** Un enlace para definir contraseña sin token no sirve. */
  readonly missingToken = this.mode !== 'forgot' && !this.token;
  readonly minLength = MIN_PASSWORD_LENGTH;

  readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', this.mode === 'forgot' ? [Validators.required, Validators.email] : []],
    password: [
      '',
      this.mode === 'forgot'
        ? []
        : [Validators.required, Validators.minLength(MIN_PASSWORD_LENGTH)],
    ],
    confirm: [''],
  });

  readonly sending = signal(false);
  readonly errorMessage = signal('');
  /** Mensaje final; al mostrarlo se oculta el formulario. */
  readonly doneMessage = signal('');
  readonly Box = Box;

  onSubmit(): void {
    if (this.form.invalid || this.sending()) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password, confirm } = this.form.getRawValue();
    if (this.mode !== 'forgot' && password !== confirm) {
      this.errorMessage.set('Las dos contraseñas no coinciden.');
      return;
    }

    const endpoint = {
      forgot: 'forgot-password',
      reset: 'reset-password',
      invitation: 'accept-invitation',
    }[this.mode];
    const body =
      this.mode === 'forgot' ? { email } : { token: this.token, password };

    this.sending.set(true);
    this.errorMessage.set('');

    this.http
      .post<ResponseModel<null>>(`${environment.apiUrl}/auth/${endpoint}`, body)
      .subscribe({
        next: (response) => {
          this.sending.set(false);
          this.doneMessage.set(response.message);
        },
        error: (err) => {
          this.sending.set(false);
          this.errorMessage.set(
            apiErrorMessage(err, 'No se pudo completar la solicitud.')
          );
        },
      });
  }
}
