import { ApplicationConfig, provideZoneChangeDetection, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { LucideAngularModule, Sun, Moon, Search, Box, LogOut, Home, LayoutGrid, Users, Settings, DollarSign, BarChart, Activity } from 'lucide-angular';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    importProvidersFrom(LucideAngularModule.pick({
      Sun, Moon, Search, Box, LogOut, Home, LayoutGrid, Users, Settings, DollarSign, BarChart, Activity
    }))
  ]
};
