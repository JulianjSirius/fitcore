import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../auth/auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;

  // La sesión se abre con un login simulado para cada caso.
  function iniciarCon(token: string | null) {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    if (token) {
      TestBed.inject(AuthService)
        .login('usuario', { email: 'a@b.co', contrasena: 'x' })
        .subscribe();
      backend
        .expectOne((req) => req.url.endsWith('/Auth/login/usuario'))
        .flush({ token, role: 'Usuario' });
    }
  }

  afterEach(() => backend.verify());

  it('agrega Authorization: Bearer cuando hay sesión', () => {
    iniciarCon('abc.def.ghi');
    http.get('/api/Plan').subscribe();
    expect(backend.expectOne('/api/Plan').request.headers.get('Authorization')).toBe(
      'Bearer abc.def.ghi',
    );
  });

  it('no agrega cabecera sin sesión', () => {
    iniciarCon(null);
    http.get('/api/Plan').subscribe();
    expect(backend.expectOne('/api/Plan').request.headers.has('Authorization')).toBe(false);
  });

  it('un 401 con sesión cierra la sesión', () => {
    iniciarCon('abc.def.ghi');
    const logout = vi
      .spyOn(TestBed.inject(AuthService), 'logout')
      .mockImplementation(() => undefined);

    http.get('/api/Membresia/mia').subscribe({ error: () => undefined });
    backend
      .expectOne('/api/Membresia/mia')
      .flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(logout).toHaveBeenCalled();
  });

  it('un 403 llega a la pantalla sin redirigir ni cerrar sesión', () => {
    iniciarCon('abc.def.ghi');
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const logout = vi
      .spyOn(TestBed.inject(AuthService), 'logout')
      .mockImplementation(() => undefined);
    let status = 0;

    http.get('/api/Usuario').subscribe({ error: (e) => (status = e.status) });
    backend.expectOne('/api/Usuario').flush(null, { status: 403, statusText: 'Forbidden' });
    expect(status).toBe(403);
    expect(navigate).not.toHaveBeenCalled();
    expect(logout).not.toHaveBeenCalled();
  });
});
