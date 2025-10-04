import { ApplicationConfig, provideZoneChangeDetection, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { LucideAngularModule, Sun, Moon, Search, Box, LogOut, Home, LayoutGrid, Users, Settings, DollarSign, BarChart, Activity, Shield, Mail, Lock } from 'lucide-angular';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    importProvidersFrom(LucideAngularModule.pick({
      Sun, Moon, Search, Box, LogOut, Home, LayoutGrid, Users, Settings, DollarSign, BarChart, Activity, Shield, Mail, Lock
    }))
  ]
};
