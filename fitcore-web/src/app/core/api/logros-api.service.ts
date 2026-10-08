import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ResumenAsistencia, ResumenProgreso } from '../../shared/models/api.models';

// Retos y logros del usuario: la asistencia vive en Identity y el progreso en Workouts.
@Injectable({ providedIn: 'root' })
export class LogrosApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  getMiAsistencia(): Observable<ResumenAsistencia> {
    return this.http.get<ResumenAsistencia>(`${this.baseUrl}/Usuario/me/asistencia`);
  }

  getMiProgreso(): Observable<ResumenProgreso> {
    return this.http.get<ResumenProgreso>(`${this.baseUrl}/Progreso/mio`);
  }
}
