import { ApplicationConfig, provideZoneChangeDetection, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { LucideAngularModule, Sun, Moon, Search, Box, LogOut, Home, LayoutGrid, Users, Settings, DollarSign, BarChart, Activity } from 'lucide-angular';
import { authInterceptor } from './core/interceptors/auth.interceptor';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimationsAsync(),
    importProvidersFrom(LucideAngularModule.pick({
      Sun, Moon, Search, Box, LogOut, Home, LayoutGrid, Users, Settings, DollarSign, BarChart, Activity
    }))
  ]
};
