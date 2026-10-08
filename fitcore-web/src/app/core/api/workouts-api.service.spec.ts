import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { WorkoutsApiService } from './workouts-api.service';

describe('WorkoutsApiService', () => {
  let api: WorkoutsApiService;
  let http: HttpTestingController;
  const base = 'http://localhost:5100/api';

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(WorkoutsApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('GET y POST /Ejercicios', () => {
    api.getEjercicios().subscribe();
    expect(http.expectOne(`${base}/Ejercicios`).request.method).toBe('GET');
    api.createEjercicio({ nombre: 'Remo', grupoMuscular: 'Espalda', descripcionOrientativa: '...' }).subscribe();
    expect(http.expectOne(`${base}/Ejercicios`).request.method).toBe('POST');
  });

  it('GET y POST /Rutinas', () => {
    api.getRutinas().subscribe();
    expect(http.expectOne(`${base}/Rutinas`).request.method).toBe('GET');
    api.createRutina({ nombre: 'R', descripcion: 'D', nivelDificultad: 'Media', usuarioId: 'u1', permitirEdicionEntrenador: false, rutinaEjercicios: [] }).subscribe();
    expect(http.expectOne(`${base}/Rutinas`).request.method).toBe('POST');
  });

  it('PUT y DELETE /Ejercicios/{id}', () => {
    api.updateEjercicio('e1', { nombre: 'Remo', grupoMuscular: 'Espalda', descripcionOrientativa: '...' }).subscribe();
    expect(http.expectOne(`${base}/Ejercicios/e1`).request.method).toBe('PUT');
    api.deleteEjercicio('e1').subscribe();
    expect(http.expectOne(`${base}/Ejercicios/e1`).request.method).toBe('DELETE');
  });

  it('PUT y DELETE /Rutinas/{id}', () => {
    api.updateRutina('r1', { nombre: 'R', descripcion: 'D', nivelDificultad: 'Avanzado', usuarioId: 'u1', permitirEdicionEntrenador: true, rutinaEjercicios: [] }).subscribe();
    const put = http.expectOne(`${base}/Rutinas/r1`);
    expect(put.request.method).toBe('PUT');
    expect(put.request.body.permitirEdicionEntrenador).toBe(true);
    api.deleteRutina('r1').subscribe();
    expect(http.expectOne(`${base}/Rutinas/r1`).request.method).toBe('DELETE');
  });
});
