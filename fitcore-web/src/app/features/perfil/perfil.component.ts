import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, map, Observable, of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { IdentityApiService } from '../../core/api/identity-api.service';
import { MembresiasApiService } from '../../core/api/membresias-api.service';
import { Membresia } from '../../shared/models/api.models';
import { mensajeError } from '../../shared/utils/errores';
import { IconComponent } from '../../shared/ui/icon.component';
import { LoadingStateComponent } from '../../shared/ui/loading-state.component';

interface DatoPerfil {
  etiqueta: string;
  valor: string;
}

interface Perfil {
  nombre: string;
  datos: DatoPerfil[];
}

@Component({
  selector: 'app-perfil',
  imports: [DatePipe, RouterLink, IconComponent, LoadingStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './perfil.component.html',
  styleUrl: './perfil.component.css',
})
export class PerfilComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly identity = inject(IdentityApiService);
  private readonly membresiasApi = inject(MembresiasApiService);

  readonly cargando = signal(true);
  readonly error = signal('');
  readonly perfil = signal<Perfil | null>(null);
  readonly membresia = signal<Membresia | null>(null);

  readonly rol = computed(() => this.auth.role());
  readonly iniciales = computed(() =>
    (this.perfil()?.nombre ?? '?')
      .split(' ')
      .map((p) => p.charAt(0))
      .slice(0, 2)
      .join('')
      .toUpperCase(),
  );

  ngOnInit(): void {
    const id = this.auth.currentUser()?.id;
    if (!id) {
      this.cargando.set(false);
      return;
    }

    this.cargarPerfil(id).subscribe({
      next: (p) => {
        this.perfil.set(p);
        this.cargando.set(false);
      },
      error: (e) => {
        this.error.set(mensajeError(e, 'No se pudo cargar tu perfil.'));
        this.cargando.set(false);
      },
    });

    if (this.rol() === 'Usuario') {
      this.membresiasApi
        .getMiMembresia()
        .pipe(catchError(() => of(null)))
        .subscribe((m) => this.membresia.set(m));
    }
  }

  private cargarPerfil(id: string): Observable<Perfil> {
    switch (this.rol()) {
      case 'Usuario':
        return this.identity.getUsuario(id).pipe(
          map((u) => ({
            nombre: `${u.firstName} ${u.lastName}`,
            datos: [
              { etiqueta: 'Correo', valor: u.email },
              { etiqueta: 'Teléfono', valor: String(u.telefono) },
            ],
          })),
        );
      case 'Entrenador':
        return this.identity.getEntrenador(id).pipe(
          map((e) => ({
            nombre: e.nombre,
            datos: [
              { etiqueta: 'Correo', valor: e.email },
              { etiqueta: 'Teléfono', valor: String(e.telefono) },
              { etiqueta: 'Especialidad', valor: e.especialidad },
              { etiqueta: 'Horario', valor: e.horario },
            ],
          })),
        );
      default:
        return this.identity.getAdministrador(id).pipe(
          map((a) => ({
            nombre: `${a.nombre} ${a.lastName}`,
            datos: [
              { etiqueta: 'Correo', valor: a.email },
              { etiqueta: 'Teléfono', valor: String(a.telefono) },
              { etiqueta: 'Nivel de acceso', valor: a.nivelAcceso },
            ],
          })),
        );
    }
  }
}
