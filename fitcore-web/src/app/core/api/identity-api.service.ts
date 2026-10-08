import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AccesoResult,
  Administrador,
  AdministradorRequest,
  CodigoRegistro,
  Entrenador,
  EntrenadorRequest,
  UsuarioRequest,
  UsuarioResumen,
  UsuarioUpdateRequest,
} from '../../shared/models/api.models';

// El login vive en AuthService, que además guarda la sesión.
@Injectable({ providedIn: 'root' })
export class IdentityApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  // ---------- Usuarios ----------
  registerUsuario(request: UsuarioRequest): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/Usuario`, request);
  }

  getUsuarios(): Observable<UsuarioResumen[]> {
    return this.http.get<UsuarioResumen[]>(`${this.baseUrl}/Usuario`);
  }

  getUsuario(id: string): Observable<UsuarioResumen> {
    return this.http.get<UsuarioResumen>(`${this.baseUrl}/Usuario/${id}`);
  }

  updateUsuario(id: string, request: UsuarioUpdateRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/Usuario/${id}`, request);
  }

  // ---------- Entrenadores ----------
  getEntrenadores(): Observable<Entrenador[]> {
    return this.http.get<Entrenador[]>(`${this.baseUrl}/Entrenador`);
  }

  getEntrenador(id: string): Observable<Entrenador> {
    return this.http.get<Entrenador>(`${this.baseUrl}/Entrenador/${id}`);
  }

  createEntrenador(request: EntrenadorRequest): Observable<Entrenador> {
    return this.http.post<Entrenador>(`${this.baseUrl}/Entrenador`, request);
  }

  updateEntrenador(id: string, request: EntrenadorRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/Entrenador/${id}`, request);
  }

  // ---------- Administradores ----------
  getAdministradores(): Observable<Administrador[]> {
    return this.http.get<Administrador[]>(`${this.baseUrl}/Administrador`);
  }

  getAdministrador(id: string): Observable<Administrador> {
    return this.http.get<Administrador>(`${this.baseUrl}/Administrador/${id}`);
  }

  createAdministrador(request: AdministradorRequest): Observable<Administrador> {
    return this.http.post<Administrador>(`${this.baseUrl}/Administrador`, request);
  }

  updateAdministrador(id: string, request: AdministradorRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/Administrador/${id}`, request);
  }

  deleteAdministrador(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/Administrador/${id}`);
  }

  // ---------- Códigos de registro ----------
  getCodigosRegistro(): Observable<CodigoRegistro[]> {
    return this.http.get<CodigoRegistro[]>(`${this.baseUrl}/CodigoRegistro`);
  }

  generarCodigoRegistro(diasVigencia: number): Observable<CodigoRegistro> {
    return this.http.post<CodigoRegistro>(`${this.baseUrl}/CodigoRegistro`, { diasVigencia });
  }

  // ---------- Control de acceso ----------
  validarAcceso(token: string): Observable<AccesoResult> {
    return this.http.post<AccesoResult>(`${this.baseUrl}/Acceso/validar`, { token });
  }
}
