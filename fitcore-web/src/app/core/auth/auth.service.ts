import { computed, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AccountType,
  LoginRequest,
  LoginResponse,
  Role,
  SessionUser,
} from '../../shared/models/api.models';

const TOKEN_KEY = 'fitcore.access_token';
const ROLE_KEY = 'fitcore.role';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenState = signal<string | null>(sessionStorage.getItem(TOKEN_KEY));
  private readonly roleState = signal<Role | null>(sessionStorage.getItem(ROLE_KEY) as Role | null);

  readonly token = this.tokenState.asReadonly();
  readonly role = this.roleState.asReadonly();
  readonly isAuthenticated = computed(() => Boolean(this.tokenState()));
  readonly currentUser = computed<SessionUser | null>(() => {
    const token = this.tokenState();
    if (!token) return null;
    return this.decodeToken(token);
  });

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router,
  ) {
    const roleFromToken = this.currentUser()?.role;
    if (roleFromToken) {
      this.roleState.set(roleFromToken);
      sessionStorage.setItem(ROLE_KEY, roleFromToken);
    }
  }

  login(accountType: AccountType, credentials: LoginRequest): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/Auth/login/${accountType}`, credentials)
      .pipe(tap((response) => this.setSession(response)));
  }

  logout(): void {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(ROLE_KEY);
    this.tokenState.set(null);
    this.roleState.set(null);
    void this.router.navigate(['/login']);
  }

  hasAnyRole(roles: Role[]): boolean {
    const currentRole = this.currentUser()?.role ?? this.roleState();
    return currentRole !== null && roles.includes(currentRole);
  }

  private setSession(response: LoginResponse): void {
    sessionStorage.setItem(TOKEN_KEY, response.token);
    sessionStorage.setItem(ROLE_KEY, response.role);
    this.tokenState.set(response.token);
    this.roleState.set(response.role);
  }

  private decodeToken(token: string): SessionUser | null {
    try {
      // atob devuelve bytes; se decodifican como UTF-8 para que "Dueño" no llegue como "DueÃ±o".
      const binario = atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/'));
      const bytes = Uint8Array.from(binario, (c) => c.charCodeAt(0));
      const payload = JSON.parse(new TextDecoder().decode(bytes));
      return {
        id: payload.sub,
        email: payload.email,
        role: payload.Rol ?? this.roleState()!,
        nivelAcceso: payload.NivelAcceso,
      };
    } catch {
      return null;
    }
  }
}
