import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, forkJoin, Observable, of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { IdentityApiService } from '../../core/api/identity-api.service';
import { MembresiasApiService } from '../../core/api/membresias-api.service';
import { WorkoutsApiService } from '../../core/api/workouts-api.service';
import {
  CodigoRegistro,
  Ejercicio,
  Entrenador,
  Membresia,
  Pago,
  Rutina,
  UsuarioResumen,
} from '../../shared/models/api.models';
import { IconComponent } from '../../shared/ui/icon.component';
import { LoadingStateComponent } from '../../shared/ui/loading-state.component';

const DIA_MS = 86_400_000;

// Si una consulta falla, el panel se muestra igual con esa sección vacía.
const oVacio = <T>(fuente: Observable<T[]>): Observable<T[]> =>
  fuente.pipe(catchError(() => of([] as T[])));

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, DatePipe, CurrencyPipe, IconComponent, LoadingStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
})
export class DashboardComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly identity = inject(IdentityApiService);
  private readonly membresias = inject(MembresiasApiService);
  private readonly workouts = inject(WorkoutsApiService);

  readonly cargando = signal(true);
  readonly membresia = signal<Membresia | null>(null);
  readonly rutinas = signal<Rutina[]>([]);
  readonly ejercicios = signal<Ejercicio[]>([]);
  readonly usuarios = signal<UsuarioResumen[]>([]);
  readonly entrenadores = signal<Entrenador[]>([]);
  readonly pagos = signal<Pago[]>([]);
  readonly codigos = signal<CodigoRegistro[]>([]);

  readonly nombre = computed(() => this.auth.currentUser()?.email?.split('@')[0] ?? 'atleta');
  readonly rol = computed(() => this.auth.role());

  readonly diasRestantes = computed(() => {
    const m = this.membresia();
    if (!m || m.estado !== 'Activa') return 0;
    return Math.max(0, Math.ceil((new Date(m.fechaVencimiento).getTime() - Date.now()) / DIA_MS));
  });

  readonly rutinasEditables = computed(() =>
    this.rutinas().filter((r) => r.permitirEdicionEntrenador),
  );

  readonly ingresosMes = computed(() => {
    const ahora = new Date();
    return this.pagos()
      .filter((p) => {
        const fecha = new Date(p.fecha);
        return fecha.getMonth() === ahora.getMonth() && fecha.getFullYear() === ahora.getFullYear();
      })
      .reduce((total, p) => total + p.monto, 0);
  });

  readonly codigosDisponibles = computed(
    () =>
      this.codigos().filter((c) => !c.usadoPorUsuarioId && new Date(c.fechaExpiracion) > new Date())
        .length,
  );

  ngOnInit(): void {

    switch (this.rol()) {
      case 'Usuario':
        forkJoin({
          membresia: this.membresias.getMiMembresia().pipe(catchError(() => of(null))),
          rutinas: oVacio(this.workouts.getRutinas()),
        }).subscribe(({ membresia, rutinas }) => {
          this.membresia.set(membresia);
          this.rutinas.set(rutinas);
          this.cargando.set(false);
        });
        break;

      case 'Entrenador':
        forkJoin({
          usuarios: oVacio(this.identity.getUsuarios()),
          rutinas: oVacio(this.workouts.getRutinas()),
          ejercicios: oVacio(this.workouts.getEjercicios()),
        }).subscribe(({ usuarios, rutinas, ejercicios }) => {
          this.usuarios.set(usuarios);
          this.rutinas.set(rutinas);
          this.ejercicios.set(ejercicios);
          this.cargando.set(false);
        });
        break;

      case 'Administrador':
        forkJoin({
          usuarios: oVacio(this.identity.getUsuarios()),
          entrenadores: oVacio(this.identity.getEntrenadores()),
          pagos: oVacio(this.membresias.getPagos()),
          codigos: oVacio(this.identity.getCodigosRegistro()),
        }).subscribe(({ usuarios, entrenadores, pagos, codigos }) => {
          this.usuarios.set(usuarios);
          this.entrenadores.set(entrenadores);
          this.pagos.set(pagos);
          this.codigos.set(codigos);
          this.cargando.set(false);
        });
        break;

      default:
        this.cargando.set(false);
    }
  }

  nombreUsuario(id: string): string {
    const u = this.usuarios().find((x) => x.id === id);
    return u ? `${u.firstName} ${u.lastName}` : 'Usuario';
  }
}
