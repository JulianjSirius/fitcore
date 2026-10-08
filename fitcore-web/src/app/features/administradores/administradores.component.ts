import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { IdentityApiService } from '../../core/api/identity-api.service';
import { Administrador, AdministradorRequest } from '../../shared/models/api.models';
import { mensajeError } from '../../shared/utils/errores';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { IconComponent } from '../../shared/ui/icon.component';
import { LoadingStateComponent } from '../../shared/ui/loading-state.component';

@Component({
  selector: 'app-administradores',
  imports: [FormsModule, EmptyStateComponent, LoadingStateComponent, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './administradores.component.html',
  styleUrl: './administradores.component.css',
})
export class AdministradoresComponent implements OnInit {
  private readonly api = inject(IdentityApiService);
  readonly auth = inject(AuthService);

  readonly niveles = ['Recepcionista', 'Contador', 'Administrador', 'Dueño'];
  readonly administradores = signal<Administrador[]>([]);
  readonly cargando = signal(true);
  readonly error = signal('');
  readonly aviso = signal('');

  readonly editorAbierto = signal(false);
  readonly guardando = signal(false);
  readonly errorForm = signal('');
  editandoId: string | null = null;
  form: AdministradorRequest = this.vacio();

  readonly miId = computed(() => this.auth.currentUser()?.id);
  readonly esDueno = computed(() => this.auth.currentUser()?.nivelAcceso === 'Dueño');

  readonly ordenados = computed(() =>
    [...this.administradores()].sort(
      (a, b) =>
        Number(b.nivelAcceso === 'Dueño') - Number(a.nivelAcceso === 'Dueño') ||
        a.nombre.localeCompare(b.nombre),
    ),
  );

  ngOnInit(): void {
    this.api.getAdministradores().subscribe({
      next: (items) => {
        this.administradores.set(items);
        this.cargando.set(false);
      },
      error: (e) => {
        this.error.set(mensajeError(e, 'No se pudo cargar la lista de administradores.'));
        this.cargando.set(false);
      },
    });
  }

  nuevo(): void {
    this.editandoId = null;
    this.form = this.vacio();
    this.abrir();
  }

  editar(a: Administrador): void {
    this.editandoId = a.id;
    this.form = { ...a, contrasena: '' };
    this.abrir();
  }

  cerrar(): void {
    this.editorAbierto.set(false);
  }

  guardar(f: NgForm): void {
    if (f.invalid) return;
    this.guardando.set(true);
    this.errorForm.set('');
    const id = this.editandoId;

    if (id) {
      this.api.updateAdministrador(id, this.form).subscribe({
        next: () => {
          const { contrasena: _, ...datos } = this.form;
          this.administradores.update((items) => items.map((a) => (a.id === id ? { id, ...datos } : a)));
          this.terminar(`Administrador ${this.form.nombre} actualizado.`);
        },
        error: (e) => this.fallo(e, 'No se pudo actualizar el administrador.'),
      });
    } else {
      this.api.createAdministrador(this.form).subscribe({
        next: (creado) => {
          this.administradores.update((items) => [...items, creado]);
          this.terminar(`Administrador ${creado.nombre} creado.`);
        },
        error: (e) => this.fallo(e, 'No se pudo crear el administrador.'),
      });
    }
  }

  eliminar(a: Administrador): void {
    if (a.id === this.miId()) return;
    if (!confirm(`¿Eliminar a ${a.nombre} ${a.lastName}? Perderá el acceso de inmediato.`)) return;
    this.api.deleteAdministrador(a.id).subscribe({
      next: () => {
        this.administradores.update((items) => items.filter((x) => x.id !== a.id));
        this.aviso.set(`Administrador ${a.nombre} eliminado.`);
      },
      error: (e) => this.error.set(mensajeError(e, 'No se pudo eliminar el administrador.')),
    });
  }

  private abrir(): void {
    this.errorForm.set('');
    this.aviso.set('');
    this.editorAbierto.set(true);
    setTimeout(() => document.getElementById('editor-admin')?.scrollIntoView({ behavior: 'smooth' }));
  }

  private terminar(mensaje: string): void {
    this.guardando.set(false);
    this.editorAbierto.set(false);
    this.aviso.set(mensaje);
  }

  private fallo(error: unknown, porDefecto: string): void {
    this.guardando.set(false);
    this.errorForm.set(mensajeError(error, porDefecto));
  }

  private vacio(): AdministradorRequest {
    return { nombre: '', lastName: '', email: '', contrasena: '', telefono: 0, nivelAcceso: '' };
  }
}
