import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { MiAccesoComponent } from './mi-acceso.component';

const base = 'http://localhost:5100/api';
const membresiaActiva = {
  id: 'm1',
  planId: 'p1',
  planCodigo: 'Mensual',
  planNombre: 'Mensual',
  estado: 'Activa',
  fechaInicio: '2026-10-01T00:00:00Z',
  fechaVencimiento: '2026-11-01T00:00:00Z',
  usuarioTitularId: 'u1',
  titularNombre: 'Ana P',
  acompananteUsuarioId: null,
  acompananteNombre: null,
};

describe('MiAccesoComponent', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [MiAccesoComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    vi.useRealTimers();
    http.verify();
  });

  function crear() {
    const fixture = TestBed.createComponent(MiAccesoComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('con membresía activa pide el token y pinta el QR', async () => {
    const fixture = crear();
    http.expectOne(`${base}/Membresia/mia`).flush(membresiaActiva);
    http.expectOne(`${base}/Usuario/me/qr-token`).flush({ token: 'token-qr', expiraEn: '' });
    fixture.detectChanges();
    await fixture.whenStable();

    const html = fixture.nativeElement as HTMLElement;
    expect(html.querySelector('qrcode')).not.toBeNull();
    expect(html.textContent).toContain('Válido por 60 s');
    expect(fixture.componentInstance.token()).toBe('token-qr');
    fixture.destroy();
  });

  it('sin membresía (404) muestra el aviso y no pide token', () => {
    const fixture = crear();
    http
      .expectOne(`${base}/Membresia/mia`)
      .flush({ message: 'x' }, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'Todavía no tienes una membresía',
    );
    http.expectNone(`${base}/Usuario/me/qr-token`);
  });

  it('si el servidor niega el QR (403) muestra su mensaje', () => {
    const fixture = crear();
    http.expectOne(`${base}/Membresia/mia`).flush(membresiaActiva);
    http
      .expectOne(`${base}/Usuario/me/qr-token`)
      .flush({ message: 'No tienes una membresía activa.' }, { status: 403, statusText: 'Forbidden' });
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'No tienes una membresía activa.',
    );
  });

  it('pide un token nuevo cuando quedan 10 segundos', () => {
    vi.useFakeTimers();
    const fixture = crear();
    http.expectOne(`${base}/Membresia/mia`).flush(membresiaActiva);
    http.expectOne(`${base}/Usuario/me/qr-token`).flush({ token: 'primero', expiraEn: '' });

    vi.advanceTimersByTime(49_000);
    http.expectNone(`${base}/Usuario/me/qr-token`);

    vi.advanceTimersByTime(1_000);
    http.expectOne(`${base}/Usuario/me/qr-token`).flush({ token: 'segundo', expiraEn: '' });
    expect(fixture.componentInstance.token()).toBe('segundo');
    fixture.destroy();
  });
});
