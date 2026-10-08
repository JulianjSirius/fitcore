import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { IdentityApiService } from '../../core/api/identity-api.service';
import { MembresiasApiService } from '../../core/api/membresias-api.service';
import { WorkoutsApiService } from '../../core/api/workouts-api.service';
import {
  Membresia,
  Pago,
  Rutina,
  UsuarioResumen,
  UsuarioUpdateRequest,
} from '../../shared/models/api.models';
import { mensajeError } from '../../shared/utils/errores';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { IconComponent } from '../../shared/ui/icon.component';
import { LoadingStateComponent } from '../../shared/ui/loading-state.component';

@Component({
  selector: 'app-usuarios',
  imports: [
    FormsModule,
    DatePipe,
    CurrencyPipe,
    RouterLink,
    EmptyStateComponent,
    LoadingStateComponent,
    IconComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './usuarios.component.html',
  styleUrl: './usuarios.component.css',
})
export class UsuariosComponent implements OnInit {
  private readonly identity = inject(IdentityApiService);
  private readonly membresiasApi = inject(MembresiasApiService);
  private readonly workouts = inject(WorkoutsApiService);
  readonly auth = inject(AuthService);

  readonly usuarios = signal<UsuarioResumen[]>([]);
  readonly rutinas = signal<Rutina[]>([]);
  readonly cargando = signal(true);
  readonly error = signal('');
  readonly busqueda = signal('');

  readonly seleccionado = signal<UsuarioResumen | null>(null);
  readonly membresia = signal<Membresia | null>(null);
  readonly cargandoDetalle = signal(false);
  readonly sinMembresia = signal(false);
  readonly pagos = signal<Pago[]>([]);

  readonly editando = signal(false);
  readonly guardando = signal(false);
  readonly mensaje = signal('');
  readonly errorEdicion = signal('');
  edicion: UsuarioUpdateRequest = this.vacio();

  readonly esAdmin = computed(() => this.auth.role() === 'Administrador');
  readonly esDueno = computed(() => this.auth.currentUser()?.nivelAcceso === 'Dueño');

  readonly filtrados = computed(() => {
    const texto = this.busqueda().trim().toLowerCase();
    return this.usuarios()
      .filter(
        (u) =>
          !texto ||
          `${u.firstName} ${u.lastName}`.toLowerCase().includes(texto) ||
          u.email.toLowerCase().includes(texto) ||
          String(u.telefono).includes(texto),
      )
      .sort((a, b) => `${a.firstName} ${a.lastName}`.localeCompare(`${b.firstName} ${b.lastName}`));
  });

  readonly rutinasDelUsuario = computed(() => {
    const id = this.seleccionado()?.id;
    return id ? this.rutinas().filter((r) => r.usuarioId === id) : [];
  });

  ngOnInit(): void {
    this.identity.getUsuarios().subscribe({
      next: (items) => {
        this.usuarios.set(items);
        this.cargando.set(false);
      },
      error: (e) => {
        this.error.set(mensajeError(e, 'No se pudo cargar la lista de usuarios.'));
        this.cargando.set(false);
      },
    });
    this.workouts
      .getRutinas()
      .pipe(catchError(() => of([] as Rutina[])))
      .subscribe((items) => this.rutinas.set(items));
  }

  seleccionar(u: UsuarioResumen): void {
    this.seleccionado.set(u);
    this.editando.set(false);
    this.mensaje.set('');
    this.membresia.set(null);
    this.sinMembresia.set(false);
    this.pagos.set([]);
    this.cargandoDetalle.set(true);

    this.membresiasApi.getMembresiaDeUsuario(u.id).subscribe({
      next: (m) => {
        this.membresia.set(m);
        this.cargandoDetalle.set(false);
      },
      error: (e: HttpErrorResponse) => {
        this.sinMembresia.set(e.status === 404);
        this.cargandoDetalle.set(false);
      },
    });

    if (this.esAdmin()) {
      this.membresiasApi
        .getPagos(u.id)
        .pipe(catchError(() => of([] as Pago[])))
        .subscribe((items) => this.pagos.set(items));
    }
    setTimeout(() => document.getElementById('detalle-usuario')?.scrollIntoView({ behavior: 'smooth', block: 'nearest' }));
  }

  cerrarDetalle(): void {
    this.seleccionado.set(null);
  }

  empezarEdicion(): void {
    const u = this.seleccionado();
    if (!u) return;
    this.edicion = {
      firstName: u.firstName,
      lastName: u.lastName,
      email: u.email,
      telefono: u.telefono,
      contrasena: '',
    };
    this.errorEdicion.set('');
    this.mensaje.set('');
    this.editando.set(true);
  }

  guardar(form: NgForm): void {
    const u = this.seleccionado();
    if (!u || form.invalid) return;
    this.guardando.set(true);
    this.errorEdicion.set('');
    this.identity.updateUsuario(u.id, this.edicion).subscribe({
      next: () => {
        const actualizado: UsuarioResumen = {
          id: u.id,
          firstName: this.edicion.firstName,
          lastName: this.edicion.lastName,
          email: this.edicion.email,
          telefono: this.edicion.telefono,
        };
        this.usuarios.update((items) => items.map((x) => (x.id === u.id ? actualizado : x)));
        this.seleccionado.set(actualizado);
        this.guardando.set(false);
        this.editando.set(false);
        this.mensaje.set('Datos del usuario actualizados.');
      },
      error: (e) => {
        this.guardando.set(false);
        this.errorEdicion.set(mensajeError(e, 'No se pudo actualizar el usuario.'));
      },
    });
  }

  diasRestantes(m: Membresia): number {
    return Math.max(0, Math.ceil((new Date(m.fechaVencimiento).getTime() - Date.now()) / 86_400_000));
  }

  private vacio(): UsuarioUpdateRequest {
    return { firstName: '', lastName: '', email: '', telefono: 0, contrasena: '' };
  }
}
