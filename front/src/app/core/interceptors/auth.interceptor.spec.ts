import {
  HttpClient,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let auth: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    auth = jasmine.createSpyObj<AuthService>('AuthService', [
      'getToken',
      'logout',
    ]);
    auth.getToken.and.returnValue('token-de-prueba');
    router = jasmine.createSpyObj<Router>('Router', ['navigate'], {
      url: '/access-control/visits',
    });

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('añade el token a las peticiones', () => {
    http.get('/api/visits').subscribe();

    const request = backend.expectOne('/api/visits');
    expect(request.request.headers.get('Authorization')).toBe(
      'Bearer token-de-prueba'
    );
    request.flush({});
  });

  it('ante un 401 cierra la sesión y lleva al login recordando la ruta', () => {
    http.get('/api/visits').subscribe({ error: () => undefined });

    backend
      .expectOne('/api/visits')
      .flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(auth.logout).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/login'], {
      queryParams: { returnUrl: '/access-control/visits' },
    });
  });

  it('un 403 no cierra la sesión', () => {
    http.get('/api/users').subscribe({ error: () => undefined });

    backend
      .expectOne('/api/users')
      .flush({}, { status: 403, statusText: 'Forbidden' });

    expect(auth.logout).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('un 401 del propio login no redirige: son credenciales incorrectas', () => {
    http.post('/api/auth/login', {}).subscribe({ error: () => undefined });

    const request = backend.expectOne('/api/auth/login');
    expect(request.request.headers.has('Authorization')).toBeFalse();
    request.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(auth.logout).not.toHaveBeenCalled();
  });
});
