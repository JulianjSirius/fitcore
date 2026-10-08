import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { IdentityApiService } from '../../core/api/identity-api.service';
import { Entrenador, EntrenadorRequest } from '../../shared/models/api.models';
import { mensajeError } from '../../shared/utils/errores';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { IconComponent } from '../../shared/ui/icon.component';
import { LoadingStateComponent } from '../../shared/ui/loading-state.component';

@Component({
  selector: 'app-entrenadores',
  imports: [FormsModule, EmptyStateComponent, LoadingStateComponent, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './entrenadores.component.html',
  styleUrl: './entrenadores.component.css',
})
export class EntrenadoresComponent implements OnInit {
  private readonly api = inject(IdentityApiService);
  readonly auth = inject(AuthService);

  readonly entrenadores = signal<Entrenador[]>([]);
  readonly cargando = signal(true);
  readonly error = signal('');
  readonly aviso = signal('');
  readonly busqueda = signal('');

  readonly editorAbierto = signal(false);
  readonly guardando = signal(false);
  readonly errorForm = signal('');
  editandoId: string | null = null;
  form: EntrenadorRequest = this.vacio();

  readonly miId = computed(() => this.auth.currentUser()?.id);
  readonly esAdmin = computed(() => this.auth.role() === 'Administrador');
  readonly esDueno = computed(() => this.auth.currentUser()?.nivelAcceso === 'Dueño');

  readonly filtrados = computed(() => {
    const texto = this.busqueda().trim().toLowerCase();
    return this.entrenadores()
      .filter(
        (e) =>
          !texto ||
          e.nombre.toLowerCase().includes(texto) ||
          e.especialidad.toLowerCase().includes(texto) ||
          e.email.toLowerCase().includes(texto),
      )
      .sort((a, b) => a.nombre.localeCompare(b.nombre));
  });

  ngOnInit(): void {
    this.api.getEntrenadores().subscribe({
      next: (items) => {
        this.entrenadores.set(items);
        this.cargando.set(false);
      },
      error: (e) => {
        this.error.set(mensajeError(e, 'No se pudo cargar el equipo de entrenadores.'));
        this.cargando.set(false);
      },
    });
  }

  nuevo(): void {
    this.editandoId = null;
    this.form = this.vacio();
    this.abrir();
  }

  editar(e: Entrenador): void {
    this.editandoId = e.id;
    this.form = { ...e, contrasena: '' };
    this.abrir();
  }

  guardar(f: NgForm): void {
    if (f.invalid) return;
    this.guardando.set(true);
    this.errorForm.set('');
    const id = this.editandoId;

    if (id) {
      this.api.updateEntrenador(id, this.form).subscribe({
        next: () => {
          const { contrasena: _, ...datos } = this.form;
          this.entrenadores.update((items) => items.map((e) => (e.id === id ? { id, ...datos } : e)));
          this.terminar(`Entrenador ${this.form.nombre} actualizado.`);
        },
        error: (e) => this.fallo(e, 'No se pudo actualizar el entrenador.'),
      });
    } else {
      this.api.createEntrenador(this.form).subscribe({
        next: (creado) => {
          this.entrenadores.update((items) => [creado, ...items]);
          this.terminar(`Entrenador ${creado.nombre} creado. Ya puede iniciar sesión con su correo.`);
        },
        error: (e) => this.fallo(e, 'No se pudo crear el entrenador.'),
      });
    }
  }

  cerrar(): void {
    this.editorAbierto.set(false);
  }

  private abrir(): void {
    this.errorForm.set('');
    this.aviso.set('');
    this.editorAbierto.set(true);
    setTimeout(() => document.getElementById('editor-entrenador')?.scrollIntoView({ behavior: 'smooth' }));
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

  private vacio(): EntrenadorRequest {
    return { nombre: '', especialidad: '', horario: '', email: '', contrasena: '', telefono: 0 };
  }
}
