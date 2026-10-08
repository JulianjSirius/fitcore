import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { WorkoutsApiService } from '../../core/api/workouts-api.service';
import { Ejercicio, EjercicioRequest } from '../../shared/models/api.models';
import { mensajeError } from '../../shared/utils/errores';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { IconComponent } from '../../shared/ui/icon.component';
import { LoadingStateComponent } from '../../shared/ui/loading-state.component';

const POR_PAGINA = 24;

@Component({
  selector: 'app-ejercicios',
  imports: [FormsModule, EmptyStateComponent, LoadingStateComponent, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './ejercicios.component.html',
  styleUrl: './ejercicios.component.css',
})
export class EjerciciosComponent implements OnInit {
  private readonly api = inject(WorkoutsApiService);
  readonly auth = inject(AuthService);

  readonly ejercicios = signal<Ejercicio[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly aviso = signal('');
  readonly formError = signal('');
  readonly guardando = signal(false);

  readonly busqueda = signal('');
  readonly grupo = signal('');
  readonly visibles = signal(POR_PAGINA);

  readonly editorAbierto = signal(false);
  editandoId: string | null = null;
  draft: EjercicioRequest = this.vacio();

  readonly puedeGestionar = computed(() => this.auth.hasAnyRole(['Entrenador', 'Administrador']));

  readonly grupos = computed(() =>
    [...new Set(this.ejercicios().map((e) => e.grupoMuscular))].sort((a, b) => a.localeCompare(b)),
  );

  readonly filtrados = computed(() => {
    const texto = this.busqueda().trim().toLowerCase();
    const grupo = this.grupo();
    return this.ejercicios()
      .filter((e) => !grupo || e.grupoMuscular === grupo)
      .filter(
        (e) =>
          !texto ||
          e.nombre.toLowerCase().includes(texto) ||
          e.descripcionOrientativa.toLowerCase().includes(texto),
      )
      .sort((a, b) => a.nombre.localeCompare(b.nombre));
  });

  readonly pagina = computed(() => this.filtrados().slice(0, this.visibles()));

  ngOnInit(): void {
    this.api.getEjercicios().subscribe({
      next: (items) => {
        this.ejercicios.set(items);
        this.loading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(mensajeError(error, 'No se pudo cargar la biblioteca de ejercicios.'));
        this.loading.set(false);
      },
    });
  }

  filtrar(texto: string): void {
    this.busqueda.set(texto);
    this.visibles.set(POR_PAGINA);
  }

  elegirGrupo(grupo: string): void {
    this.grupo.set(grupo);
    this.visibles.set(POR_PAGINA);
  }

  verMas(): void {
    this.visibles.update((n) => n + POR_PAGINA);
  }

  nuevo(): void {
    this.editandoId = null;
    this.draft = this.vacio();
    this.abrir();
  }

  editar(e: Ejercicio): void {
    this.editandoId = e.id;
    this.draft = {
      nombre: e.nombre,
      grupoMuscular: e.grupoMuscular,
      descripcionOrientativa: e.descripcionOrientativa,
    };
    this.abrir();
  }

  cerrar(): void {
    this.editorAbierto.set(false);
  }

  guardar(form: NgForm): void {
    if (form.invalid) return;
    this.formError.set('');
    this.guardando.set(true);
    const request: EjercicioRequest = {
      nombre: this.draft.nombre.trim(),
      grupoMuscular: this.draft.grupoMuscular.trim(),
      descripcionOrientativa: this.draft.descripcionOrientativa.trim(),
    };
    const id = this.editandoId;
    const peticion = id ? this.api.updateEjercicio(id, request) : this.api.createEjercicio(request);

    peticion.subscribe({
      next: (ejercicio) => {
        this.ejercicios.update((items) =>
          id ? items.map((e) => (e.id === id ? ejercicio : e)) : [ejercicio, ...items],
        );
        this.guardando.set(false);
        this.editorAbierto.set(false);
        this.aviso.set(id ? `“${ejercicio.nombre}” actualizado.` : `“${ejercicio.nombre}” agregado a la biblioteca.`);
      },
      error: (error) => {
        this.guardando.set(false);
        this.formError.set(mensajeError(error, 'No se pudo guardar el ejercicio.'));
      },
    });
  }

  eliminar(e: Ejercicio): void {
    // La API borra en cascada: el ejercicio también desaparece de las rutinas que lo usan.
    const ok = confirm(
      `¿Eliminar “${e.nombre}”?\n\nTambién se quitará de todas las rutinas que lo incluyan.`,
    );
    if (!ok) return;
    this.api.deleteEjercicio(e.id).subscribe({
      next: () => {
        this.ejercicios.update((items) => items.filter((x) => x.id !== e.id));
        this.aviso.set(`“${e.nombre}” eliminado.`);
      },
      error: (error) => this.errorMessage.set(mensajeError(error, 'No se pudo eliminar el ejercicio.')),
    });
  }

  private abrir(): void {
    this.formError.set('');
    this.aviso.set('');
    this.editorAbierto.set(true);
    setTimeout(() => document.getElementById('editor-ejercicio')?.scrollIntoView({ behavior: 'smooth' }));
  }

  private vacio(): EjercicioRequest {
    return { nombre: '', grupoMuscular: '', descripcionOrientativa: '' };
  }
}
