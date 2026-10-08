import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { LogrosApiService } from './logros-api.service';

describe('LogrosApiService', () => {
  let api: LogrosApiService;
  let http: HttpTestingController;
  const base = 'http://localhost:5100/api';

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(LogrosApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('GET /Usuario/me/asistencia', () => {
    api.getMiAsistencia().subscribe();
    expect(http.expectOne(`${base}/Usuario/me/asistencia`).request.method).toBe('GET');
  });

  it('GET /Progreso/mio', () => {
    api.getMiProgreso().subscribe();
    expect(http.expectOne(`${base}/Progreso/mio`).request.method).toBe('GET');
  });
});
