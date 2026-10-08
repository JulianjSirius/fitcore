import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';
import { Role } from './shared/models/api.models';

const EQUIPO: Role[] = ['Entrenador', 'Administrador'];

// Rutas con sesión; `roles` limita quién puede entrar (el resto vuelve al inicio).
const privada = (path: string, loadComponent: Routes[number]['loadComponent'], roles?: Role[]) => ({
  path,
  canActivate: roles ? [authGuard, roleGuard] : [authGuard],
  ...(roles && { data: { roles } }),
  loadComponent,
});

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'registro',
    loadComponent: () =>
      import('./features/auth/register.component').then((m) => m.RegisterComponent),
  },
  privada('dashboard', () =>
    import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
  ),
  privada('rutinas', () =>
    import('./features/rutinas/rutinas.component').then((m) => m.RutinasComponent),
  ),
  privada('ejercicios', () =>
    import('./features/ejercicios/ejercicios.component').then((m) => m.EjerciciosComponent),
  ),
  privada('planes', () =>
    import('./features/planes/planes.component').then((m) => m.PlanesComponent),
  ),
  privada('perfil', () =>
    import('./features/perfil/perfil.component').then((m) => m.PerfilComponent),
  ),
  privada(
    'mi-acceso',
    () => import('./features/mi-acceso/mi-acceso.component').then((m) => m.MiAccesoComponent),
    ['Usuario'],
  ),
  privada(
    'logros',
    () => import('./features/logros/logros.component').then((m) => m.LogrosComponent),
    ['Usuario'],
  ),
  privada(
    'control-acceso',
    () =>
      import('./features/control-acceso/control-acceso.component').then(
        (m) => m.ControlAccesoComponent,
      ),
    EQUIPO,
  ),
  privada(
    'usuarios',
    () => import('./features/usuarios/usuarios.component').then((m) => m.UsuariosComponent),
    EQUIPO,
  ),
  privada(
    'entrenadores',
    () =>
      import('./features/entrenadores/entrenadores.component').then((m) => m.EntrenadoresComponent),
    EQUIPO,
  ),
  privada(
    'membresias',
    () => import('./features/membresias/membresias.component').then((m) => m.MembresiasComponent),
    ['Administrador'],
  ),
  privada(
    'codigos',
    () => import('./features/codigos/codigos.component').then((m) => m.CodigosComponent),
    ['Administrador'],
  ),
  privada(
    'administradores',
    () =>
      import('./features/administradores/administradores.component').then(
        (m) => m.AdministradoresComponent,
      ),
    ['Administrador'],
  ),
  // Ruta anterior: el panel se dividió en Membresías, Planes, Códigos y Administradores.
  { path: 'administracion', redirectTo: 'membresias' },
  { path: '**', redirectTo: 'dashboard' },
];
