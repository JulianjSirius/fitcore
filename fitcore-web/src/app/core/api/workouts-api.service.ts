import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Ejercicio, EjercicioRequest, Rutina, RutinaRequest } from '../../shared/models/api.models';

@Injectable({ providedIn: 'root' })
export class WorkoutsApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  // ---------- Ejercicios ----------
  getEjercicios(): Observable<Ejercicio[]> {
    return this.http.get<Ejercicio[]>(`${this.baseUrl}/Ejercicios`);
  }

  createEjercicio(request: EjercicioRequest): Observable<Ejercicio> {
    return this.http.post<Ejercicio>(`${this.baseUrl}/Ejercicios`, request);
  }

  updateEjercicio(id: string, request: EjercicioRequest): Observable<Ejercicio> {
    return this.http.put<Ejercicio>(`${this.baseUrl}/Ejercicios/${id}`, request);
  }

  deleteEjercicio(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/Ejercicios/${id}`);
  }

  // ---------- Rutinas ----------
  getRutinas(): Observable<Rutina[]> {
    return this.http.get<Rutina[]>(`${this.baseUrl}/Rutinas`);
  }

  createRutina(request: RutinaRequest): Observable<Rutina> {
    return this.http.post<Rutina>(`${this.baseUrl}/Rutinas`, request);
  }

  updateRutina(id: string, request: RutinaRequest): Observable<Rutina> {
    return this.http.put<Rutina>(`${this.baseUrl}/Rutinas/${id}`, request);
  }

  deleteRutina(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/Rutinas/${id}`);
  }
}
