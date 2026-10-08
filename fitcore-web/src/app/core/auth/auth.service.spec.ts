import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';

// JWT de prueba sin firma válida: el frontend solo lee el payload.
// Se codifica en UTF-8 como lo hace la API (así "Dueño" viaja como bytes C3 B1).
function token(payload: object): string {
  const b64 = (o: object) =>
    btoa(String.fromCharCode(...new TextEncoder().encode(JSON.stringify(o)))).replace(/=+$/, '');
  return `${b64({ alg: 'none' })}.${b64(payload)}.firma`;
}

describe('AuthService', () => {
  let auth: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'login', children: [] }]),
      ],
    });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it.each(['administrador', 'usuario', 'entrenador'] as const)(
    'POST /Auth/login/%s por el gateway y guarda la sesión',
    (tipo) => {
      const jwt = token({
        sub: 'id-1',
        email: 'a@b.co',
        Rol: 'Administrador',
        NivelAcceso: 'Dueño',
      });
      auth.login(tipo, { email: 'a@b.co', contrasena: 'x' }).subscribe();

      const req = http.expectOne(`http://localhost:5100/api/Auth/login/${tipo}`);
      expect(req.request.method).toBe('POST');
      req.flush({ token: jwt, role: 'Administrador' });

      expect(auth.isAuthenticated()).toBe(true);
      expect(auth.currentUser()).toEqual({
        id: 'id-1',
        email: 'a@b.co',
        role: 'Administrador',
        nivelAcceso: 'Dueño',
      });
      expect(sessionStorage.getItem('fitcore.access_token')).toBe(jwt);
    },
  );

  it('logout borra la sesión', () => {
    auth.login('usuario', { email: 'a@b.co', contrasena: 'x' }).subscribe();
    http
      .expectOne(() => true)
      .flush({ token: token({ sub: '1', Rol: 'Usuario' }), role: 'Usuario' });

    auth.logout();
    expect(auth.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem('fitcore.access_token')).toBeNull();
  });

  it('hasAnyRole compara contra el rol de la sesión', () => {
    auth.login('usuario', { email: 'a@b.co', contrasena: 'x' }).subscribe();
    http
      .expectOne(() => true)
      .flush({ token: token({ sub: '1', Rol: 'Usuario' }), role: 'Usuario' });

    expect(auth.hasAnyRole(['Usuario'])).toBe(true);
    expect(auth.hasAnyRole(['Administrador', 'Entrenador'])).toBe(false);
  });
});
