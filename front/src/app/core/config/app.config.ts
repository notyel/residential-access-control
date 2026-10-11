import { registerLocaleData } from '@angular/common';
import localeEsCo from '@angular/common/locales/es-CO';
import {
  APP_INITIALIZER,
  ApplicationConfig,
  LOCALE_ID,
  inject,
  provideZoneChangeDetection,
} from '@angular/core';
import { TitleStrategy, provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import {
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import {
  DateAdapter,
  provideNativeDateAdapter,
} from '@angular/material/core';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS } from '@angular/material/form-field';
import { firstValueFrom } from 'rxjs';
import { routes } from '../../app.routes';
import { AuthService } from '../services/auth.service';
import { authInterceptor } from '../interceptors/auth.interceptor';
import { AppDateAdapter } from './app-date-adapter';
import { ActionsSearchProvider } from '../search/actions-search.provider';
import { PersonsSearchProvider } from '../search/persons-search.provider';
import { SEARCH_PROVIDERS } from '../search/search-provider';
import { AppTitleStrategy } from '../services/app-title.strategy';
import { UnitsSearchProvider } from '../search/units-search.provider';

registerLocaleData(localeEsCo);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimations(),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideNativeDateAdapter(),
    { provide: DateAdapter, useClass: AppDateAdapter },
    {
      // Con un token guardado, el usuario y sus permisos se cargan antes de la primera ruta
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: () => {
        const authService = inject(AuthService);
        return () => firstValueFrom(authService.refresh());
      },
    },
    { provide: LOCALE_ID, useValue: 'es-CO' },
    { provide: TitleStrategy, useClass: AppTitleStrategy },
    // Fuentes de la búsqueda por comando, en el orden en que se muestran
    { provide: SEARCH_PROVIDERS, useClass: PersonsSearchProvider, multi: true },
    {
      provide: SEARCH_PROVIDERS,
      useClass: UnitsSearchProvider,
      multi: true,
    },
    { provide: SEARCH_PROVIDERS, useClass: ActionsSearchProvider, multi: true },
    {
      provide: MAT_FORM_FIELD_DEFAULT_OPTIONS,
      useValue: {
        appearance: 'outline',
        subscriptSizing: 'dynamic',
      },
    },
  ],
};
