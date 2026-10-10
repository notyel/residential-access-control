import { registerLocaleData } from '@angular/common';
import localeEsCo from '@angular/common/locales/es-CO';
import {
  ApplicationConfig,
  LOCALE_ID,
  provideZoneChangeDetection,
} from '@angular/core';
import { TitleStrategy, provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import {
  provideHttpClient,
  withInterceptorsFromDi,
  HTTP_INTERCEPTORS,
} from '@angular/common/http';
import {
  DateAdapter,
  provideNativeDateAdapter,
} from '@angular/material/core';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS } from '@angular/material/form-field';
import { routes } from '../../app.routes';
import { AuthInterceptor } from '../interceptors/auth.interceptor';
import { AppDateAdapter } from './app-date-adapter';
import { ActionsSearchProvider } from '../search/actions-search.provider';
import { PersonsSearchProvider } from '../search/persons-search.provider';
import { SEARCH_PROVIDERS } from '../search/search-provider';
import { AppTitleStrategy } from '../services/app-title.strategy';
import { ResidentsSearchProvider } from '../../modules/access-control/residents/services/residents-search.provider';

registerLocaleData(localeEsCo);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimations(),
    provideHttpClient(withInterceptorsFromDi()),
    provideNativeDateAdapter(),
    { provide: DateAdapter, useClass: AppDateAdapter },
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: LOCALE_ID, useValue: 'es-CO' },
    { provide: TitleStrategy, useClass: AppTitleStrategy },
    // Fuentes de la búsqueda por comando, en el orden en que se muestran
    { provide: SEARCH_PROVIDERS, useClass: PersonsSearchProvider, multi: true },
    {
      provide: SEARCH_PROVIDERS,
      useClass: ResidentsSearchProvider,
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
