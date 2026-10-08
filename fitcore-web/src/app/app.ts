import { Component, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { AuthService } from './core/auth/auth.service';
import { Role } from './shared/models/api.models';
import { IconComponent } from './shared/ui/icon.component';

interface EnlaceMenu {
  ruta: string;
  texto: string;
  icono: string;
  roles: Role[];
}

interface GrupoMenu {
  titulo: string;
  enlaces: EnlaceMenu[];
}

const TODOS: Role[] = ['Usuario', 'Entrenador', 'Administrador'];
const EQUIPO: Role[] = ['Entrenador', 'Administrador'];

const MENU: GrupoMenu[] = [
  {
    titulo: 'General',
    enlaces: [
      { ruta: '/dashboard', texto: 'Inicio', icono: 'inicio', roles: TODOS },
      { ruta: '/mi-acceso', texto: 'Mi acceso', icono: 'qr', roles: ['Usuario'] },
      { ruta: '/control-acceso', texto: 'Control de acceso', icono: 'escanear', roles: EQUIPO },
    ],
  },
  {
    titulo: 'Entrenamiento',
    enlaces: [
      { ruta: '/rutinas', texto: 'Rutinas', icono: 'rutinas', roles: TODOS },
      { ruta: '/ejercicios', texto: 'Ejercicios', icono: 'ejercicios', roles: TODOS },
      { ruta: '/logros', texto: 'Retos y logros', icono: 'trofeo', roles: ['Usuario'] },
    ],
  },
  {
    titulo: 'Gimnasio',
    enlaces: [
      { ruta: '/usuarios', texto: 'Usuarios', icono: 'usuarios', roles: EQUIPO },
      { ruta: '/entrenadores', texto: 'Entrenadores', icono: 'entrenadores', roles: EQUIPO },
      { ruta: '/membresias', texto: 'Membresías y pagos', icono: 'membresias', roles: ['Administrador'] },
      { ruta: '/planes', texto: 'Planes', icono: 'planes', roles: TODOS },
      { ruta: '/codigos', texto: 'Códigos de registro', icono: 'codigos', roles: ['Administrador'] },
      { ruta: '/administradores', texto: 'Administradores', icono: 'administradores', roles: ['Administrador'] },
    ],
  },
  {
    titulo: 'Cuenta',
    enlaces: [{ ruta: '/perfil', texto: 'Mi perfil', icono: 'perfil', roles: TODOS }],
  },
];

@Component({
  imports: [RouterLink, RouterLinkActive, RouterOutlet, IconComponent],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  readonly auth = inject(AuthService);
  readonly menuAbierto = signal(false);

  readonly menu = computed(() => {
    const rol = this.auth.role();
    if (!rol) return [];
    return MENU.map((grupo) => ({
      ...grupo,
      enlaces: grupo.enlaces.filter((enlace) => enlace.roles.includes(rol)),
    })).filter((grupo) => grupo.enlaces.length > 0);
  });

  readonly iniciales = computed(() =>
    (this.auth.currentUser()?.email ?? '?').slice(0, 2).toUpperCase(),
  );

  constructor() {
    // En móvil el menú se cierra al navegar.
    inject(Router)
      .events.pipe(
        filter((evento) => evento instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.menuAbierto.set(false));
  }
}
