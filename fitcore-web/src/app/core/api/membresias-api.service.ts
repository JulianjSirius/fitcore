import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Membresia,
  Pago,
  PagoRegistrado,
  Plan,
  PlanUpdateRequest,
  RegistrarPagoRequest,
  TokenAccesoQr,
  UsuarioResumen,
} from '../../shared/models/api.models';

@Injectable({ providedIn: 'root' })
export class MembresiasApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  getPlanes(): Observable<Plan[]> {
    return this.http.get<Plan[]>(`${this.baseUrl}/Plan`);
  }

  updatePlan(id: string, request: PlanUpdateRequest): Observable<Plan> {
    return this.http.put<Plan>(`${this.baseUrl}/Plan/${id}`, request);
  }

  getMiMembresia(): Observable<Membresia> {
    return this.http.get<Membresia>(`${this.baseUrl}/Membresia/mia`);
  }

  getMembresiaDeUsuario(usuarioId: string): Observable<Membresia> {
    return this.http.get<Membresia>(`${this.baseUrl}/Membresia/usuario/${usuarioId}`);
  }

  getPagos(usuarioId?: string): Observable<Pago[]> {
    const query = usuarioId ? `?usuarioId=${encodeURIComponent(usuarioId)}` : '';
    return this.http.get<Pago[]>(`${this.baseUrl}/Membresia/pagos${query}`);
  }

  registrarPago(request: RegistrarPagoRequest): Observable<PagoRegistrado> {
    return this.http.post<PagoRegistrado>(`${this.baseUrl}/Membresia/pagos`, request);
  }

  getUsuarios(): Observable<UsuarioResumen[]> {
    return this.http.get<UsuarioResumen[]>(`${this.baseUrl}/Usuario`);
  }

  getTokenAccesoQr(): Observable<TokenAccesoQr> {
    return this.http.get<TokenAccesoQr>(`${this.baseUrl}/Usuario/me/qr-token`);
  }
}
