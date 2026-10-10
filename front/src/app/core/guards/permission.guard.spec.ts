import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AuthService } from '../services/auth.service';
import { permissionGuard } from './permission.guard';

@Component({ standalone: true, template: 'pantalla' })
class StubComponent {}

describe('permissionGuard', () => {
  let permissions: string[];
  let superAdmin: boolean;

  beforeEach(() => {
    permissions = [];
    superAdmin = false;
    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthService,
          useValue: {
            isSuperAdmin: () => superAdmin,
            hasPermission: (rule: string | string[]) =>
              superAdmin ||
              (typeof rule === 'string' ? [rule] : rule).some((p) =>
                permissions.includes(p)
              ),
          },
        },
        provideRouter([
          {
            path: 'access-control',
            canActivateChild: [permissionGuard],
            children: [
              {
                path: 'visits',
                // contenedor como los módulos lazy: el permiso va en la ruta hoja
                children: [
                  {
                    path: '',
                    component: StubComponent,
                    data: { permission: ['visits.read', 'visits.read_own'] },
                  },
                ],
              },
              {
                path: 'complexes',
                component: StubComponent,
                data: { superAdminOnly: true },
              },
              { path: 'sin-permiso', component: StubComponent },
              {
                path: 'forbidden',
                component: StubComponent,
                data: { public: true },
              },
            ],
          },
        ]),
      ],
    });
  });

  async function navigate(url: string): Promise<string> {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url).catch(() => undefined);
    return TestBed.inject(Router).url;
  }

  it('deja pasar con cualquiera de los permisos declarados', async () => {
    permissions = ['visits.read_own'];
    expect(await navigate('/access-control/visits')).toBe(
      '/access-control/visits'
    );
  });

  it('lleva a "Sin acceso" a quien escribe la URL de una pantalla sin tener el permiso', async () => {
    permissions = ['dashboard.read'];
    expect(await navigate('/access-control/visits')).toBe(
      '/access-control/forbidden'
    );
  });

  it('reserva las pantallas de plataforma al superadministrador', async () => {
    permissions = ['visits.read'];
    expect(await navigate('/access-control/complexes')).toBe(
      '/access-control/forbidden'
    );
  });

  it('el superadministrador abre las pantallas de plataforma', async () => {
    superAdmin = true;
    expect(await navigate('/access-control/complexes')).toBe(
      '/access-control/complexes'
    );
  });

  it('bloquea una ruta que no declara permiso', async () => {
    permissions = ['visits.read'];
    spyOn(console, 'error');
    expect(await navigate('/access-control/sin-permiso')).toBe(
      '/access-control/forbidden'
    );
  });
});
