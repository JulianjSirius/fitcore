import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { IdentityApiService } from './identity-api.service';

describe('IdentityApiService', () => {
  let api: IdentityApiService;
  let http: HttpTestingController;
  const base = 'http://localhost:5100/api';

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(IdentityApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('POST /Usuario incluye el código de registro', () => {
    const registro = {
      firstName: 'Ana', lastName: 'P', email: 'a@b.co', contrasena: 'x', telefono: 3001234567,
      codigoRegistro: 'ABCDEFGH23',
    };
    api.registerUsuario(registro).subscribe();
    const req = http.expectOne(`${base}/Usuario`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.codigoRegistro).toBe('ABCDEFGH23');
  });

  it('POST /Entrenador', () => {
    api.createEntrenador({ nombre: 'Leo', especialidad: 'F', horario: 'H', email: 'e@b.co', contrasena: 'x', telefono: 3001234567 }).subscribe();
    expect(http.expectOne(`${base}/Entrenador`).request.method).toBe('POST');
  });

  it('POST /Administrador', () => {
    api.createAdministrador({ nombre: 'R', lastName: 'R', email: 'r@b.co', contrasena: 'x', telefono: 3001234567, nivelAcceso: 'Recepcionista' }).subscribe();
    expect(http.expectOne(`${base}/Administrador`).request.method).toBe('POST');
  });

  it('GET y POST /CodigoRegistro', () => {
    api.getCodigosRegistro().subscribe();
    expect(http.expectOne(`${base}/CodigoRegistro`).request.method).toBe('GET');

    api.generarCodigoRegistro(5).subscribe();
    const req = http.expectOne(`${base}/CodigoRegistro`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ diasVigencia: 5 });
  });

  it('Usuarios: GET lista, GET por id y PUT', () => {
    api.getUsuarios().subscribe();
    expect(http.expectOne(`${base}/Usuario`).request.method).toBe('GET');
    api.getUsuario('u1').subscribe();
    expect(http.expectOne(`${base}/Usuario/u1`).request.method).toBe('GET');
    api.updateUsuario('u1', { firstName: 'A', lastName: 'P', email: 'a@b.co', contrasena: 'nueva1', telefono: 3001234567 }).subscribe();
    expect(http.expectOne(`${base}/Usuario/u1`).request.method).toBe('PUT');
  });

  it('Entrenadores: GET lista, GET por id y PUT', () => {
    api.getEntrenadores().subscribe();
    expect(http.expectOne(`${base}/Entrenador`).request.method).toBe('GET');
    api.getEntrenador('e1').subscribe();
    expect(http.expectOne(`${base}/Entrenador/e1`).request.method).toBe('GET');
    api.updateEntrenador('e1', { nombre: 'Leo', especialidad: 'F', horario: 'H', email: 'e@b.co', contrasena: 'x', telefono: 3001234567 }).subscribe();
    expect(http.expectOne(`${base}/Entrenador/e1`).request.method).toBe('PUT');
  });

  it('Administradores: GET, PUT y DELETE', () => {
    api.getAdministradores().subscribe();
    expect(http.expectOne(`${base}/Administrador`).request.method).toBe('GET');
    api.getAdministrador('a1').subscribe();
    expect(http.expectOne(`${base}/Administrador/a1`).request.method).toBe('GET');
    api.updateAdministrador('a1', { nombre: 'R', lastName: 'R', email: 'r@b.co', contrasena: 'x', telefono: 3001234567, nivelAcceso: 'Contador' }).subscribe();
    expect(http.expectOne(`${base}/Administrador/a1`).request.method).toBe('PUT');
    api.deleteAdministrador('a1').subscribe();
    expect(http.expectOne(`${base}/Administrador/a1`).request.method).toBe('DELETE');
  });

  it('POST /Acceso/validar con el token del QR', () => {
    api.validarAcceso('qr-token').subscribe();
    const req = http.expectOne(`${base}/Acceso/validar`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ token: 'qr-token' });
  });
});
