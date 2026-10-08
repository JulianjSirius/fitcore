import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../../environments/environment';
import { MembresiasApiService } from './membresias-api.service';

// Contrato Angular → gateway: cada método debe pegarle a la URL y con el verbo que espera la API.
describe('MembresiasApiService', () => {
  let api: MembresiasApiService;
  let http: HttpTestingController;
  const base = 'http://localhost:5100/api';

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(MembresiasApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('usa el API Gateway como base', () => {
    expect(environment.apiUrl).toBe(base);
  });

  it('GET /Plan', () => {
    api.getPlanes().subscribe();
    expect(http.expectOne(`${base}/Plan`).request.method).toBe('GET');
  });

  it('PUT /Plan/{id} con los valores editados', () => {
    const cambios = { nombre: 'Mensual', precio: 130000, duracionMeses: 1, activo: true };
    api.updatePlan('p1', cambios).subscribe();
    const req = http.expectOne(`${base}/Plan/p1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(cambios);
  });

  it('GET /Membresia/mia', () => {
    api.getMiMembresia().subscribe();
    expect(http.expectOne(`${base}/Membresia/mia`).request.method).toBe('GET');
  });

  it('GET /Membresia/usuario/{id}', () => {
    api.getMembresiaDeUsuario('u1').subscribe();
    expect(http.expectOne(`${base}/Membresia/usuario/u1`).request.method).toBe('GET');
  });

  it('GET /Membresia/pagos con y sin filtro de usuario', () => {
    api.getPagos().subscribe();
    http.expectOne(`${base}/Membresia/pagos`);
    api.getPagos('u 1').subscribe();
    http.expectOne(`${base}/Membresia/pagos?usuarioId=u%201`);
  });

  it('POST /Membresia/pagos con el pago', () => {
    const pago = { usuarioId: 'u1', planId: 'p1', metodo: 'Efectivo' as const };
    api.registrarPago(pago).subscribe();
    const req = http.expectOne(`${base}/Membresia/pagos`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(pago);
  });

  it('GET /Usuario para la lista de usuarios', () => {
    api.getUsuarios().subscribe();
    expect(http.expectOne(`${base}/Usuario`).request.method).toBe('GET');
  });

  it('GET /Usuario/me/qr-token para el QR', () => {
    api.getTokenAccesoQr().subscribe();
    expect(http.expectOne(`${base}/Usuario/me/qr-token`).request.method).toBe('GET');
  });
});
