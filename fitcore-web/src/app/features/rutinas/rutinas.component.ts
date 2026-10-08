import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { IdentityApiService } from '../../core/api/identity-api.service';
import { WorkoutsApiService } from '../../core/api/workouts-api.service';
import {
  Ejercicio,
  ProgresoEjercicio,
  Rutina,
  RutinaRequest,
  UsuarioResumen,
} from '../../shared/models/api.models';
import { mensajeError } from '../../shared/utils/errores';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { IconComponent } from '../../shared/ui/icon.component';
import { LoadingStateComponent } from '../../shared/ui/loading-state.component';

interface ItemRutina {
  ejercicioId: string;
  nombre: string;
  grupoMuscular: string;
  series: number;
  repeticiones: number;
  pesoKg: number;
  tiempoDescansoSegundos: number;
}

interface FormRutina {
  id: string | null;
  nombre: string;
  descripcion: string;
  nivelDificultad: string;
  usuarioId: string;
  permitirEdicionEntrenador: boolean;
  items: ItemRutina[];
}

const NIVELES = ['Principiante', 'Intermedio', 'Avanzado'];
const MAX_SUGERENCIAS = 40;
const PESO_MAXIMO_KG = 1000; // Mismo límite que la API

@Component({
  selector: 'app-rutinas',
  imports: [FormsModule, DatePipe, RouterLink, EmptyStateComponent, LoadingStateComponent, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './rutinas.component.html',
  styleUrl: './rutinas.component.css',
})
export class RutinasComponent implements OnInit {
  private readonly api = inject(WorkoutsApiService);
  private readonly identity = inject(IdentityApiService);
  readonly auth = inject(AuthService);

  readonly niveles = NIVELES;
  readonly rutinas = signal<Rutina[]>([]);
  readonly ejercicios = signal<Ejercicio[]>([]);
  readonly usuarios = signal<UsuarioResumen[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly formError = signal('');
  readonly aviso = signal('');
  // Mejoras de repeticiones o peso de la última edición guardada.
  readonly logrosGuardado = signal<ProgresoEjercicio[]>([]);
  readonly expandida = signal<string | null>(null);

  // Filtros de la lista
  readonly busqueda = signal('');
  readonly filtroNivel = signal('');

  // Editor
  readonly editorAbierto = signal(false);
  form: FormRutina = this.formVacio();
  readonly busquedaEjercicio = signal('');
  readonly grupoEjercicio = signal('');

  readonly rol = computed(() => this.auth.role());
  readonly miId = computed(() => this.auth.currentUser()?.id ?? '');
  readonly puedeCrear = computed(() => this.rol() === 'Usuario' || this.rol() === 'Entrenador');
  readonly esEquipo = computed(() => this.rol() === 'Entrenador' || this.rol() === 'Administrador');

  readonly rutinasFiltradas = computed(() => {
    const texto = this.busqueda().trim().toLowerCase();
    const nivel = this.filtroNivel();
    return this.rutinas()
      .filter((r) => !nivel || r.nivelDificultad === nivel)
      .filter(
        (r) =>
          !texto ||
          r.nombre.toLowerCase().includes(texto) ||
          r.descripcion.toLowerCase().includes(texto) ||
          this.nombreUsuario(r.usuarioId).toLowerCase().includes(texto),
      )
      .sort((a, b) => b.fechaCreacion.localeCompare(a.fechaCreacion));
  });

  readonly gruposMusculares = computed(() =>
    [...new Set(this.ejercicios().map((e) => e.grupoMuscular))].sort((a, b) => a.localeCompare(b)),
  );

  readonly sugerencias = computed(() => {
    const texto = this.busquedaEjercicio().trim().toLowerCase();
    const grupo = this.grupoEjercicio();
    return this.ejercicios()
      .filter((e) => !grupo || e.grupoMuscular === grupo)
      .filter((e) => !texto || e.nombre.toLowerCase().includes(texto))
      .slice(0, MAX_SUGERENCIAS);
  });

  ngOnInit(): void {
    forkJoin({
      rutinas: this.api.getRutinas(),
      ejercicios: this.api.getEjercicios().pipe(catchError(() => of([] as Ejercicio[]))),
      // Solo el equipo puede listar usuarios (para mostrar dueños y asignar rutinas).
      usuarios: this.esEquipo()
        ? this.identity.getUsuarios().pipe(catchError(() => of([] as UsuarioResumen[])))
        : of([] as UsuarioResumen[]),
    }).subscribe({
      next: ({ rutinas, ejercicios, usuarios }) => {
        this.rutinas.set(rutinas);
        this.ejercicios.set(ejercicios);
        this.usuarios.set(usuarios);
        this.loading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(mensajeError(error, 'No se pudieron cargar las rutinas.'));
        this.loading.set(false);
      },
    });
  }

  // ---------- Permisos (mismas reglas que RutinaReglas en la API) ----------
  puedeModificar(r: Rutina): boolean {
    return (
      (this.rol() === 'Usuario' && r.usuarioId === this.miId()) ||
      (this.rol() === 'Entrenador' && r.permitirEdicionEntrenador)
    );
  }

  nombreUsuario(id: string): string {
    if (id === this.miId()) return 'Tú';
    const u = this.usuarios().find((x) => x.id === id);
    return u ? `${u.firstName} ${u.lastName}` : '';
  }

  alternarDetalle(id: string): void {
    this.expandida.set(this.expandida() === id ? null : id);
  }

  // ---------- Editor ----------
  nueva(): void {
    this.form = this.formVacio();
    this.abrirEditor();
  }

  editar(r: Rutina): void {
    this.form = {
      id: r.id,
      nombre: r.nombre,
      descripcion: r.descripcion,
      nivelDificultad: r.nivelDificultad,
      usuarioId: r.usuarioId,
      permitirEdicionEntrenador: r.permitirEdicionEntrenador,
      items: [...r.rutinaEjercicios]
        .sort((a, b) => a.ordenAparicion - b.ordenAparicion)
        .map((e) => ({
          ejercicioId: e.ejercicioId,
          nombre: e.nombre,
          grupoMuscular: e.grupoMuscular,
          series: e.series,
          repeticiones: e.repeticiones,
          pesoKg: e.pesoKg ?? 0,
          tiempoDescansoSegundos: e.tiempoDescansoSegundos,
        })),
    };
    this.abrirEditor();
  }

  cerrarEditor(): void {
    if (!this.saving()) this.editorAbierto.set(false);
  }

  estaEnRutina(id: string): boolean {
    return this.form.items.some((i) => i.ejercicioId === id);
  }

  agregar(e: Ejercicio): void {
    if (this.estaEnRutina(e.id)) {
      this.form.items = this.form.items.filter((i) => i.ejercicioId !== e.id);
      return;
    }
    this.form.items = [
      ...this.form.items,
      {
        ejercicioId: e.id,
        nombre: e.nombre,
        grupoMuscular: e.grupoMuscular,
        series: 3,
        repeticiones: 10,
        pesoKg: 0,
        tiempoDescansoSegundos: 60,
      },
    ];
  }

  quitar(index: number): void {
    this.form.items = this.form.items.filter((_, i) => i !== index);
  }

  mover(index: number, delta: number): void {
    const destino = index + delta;
    if (destino < 0 || destino >= this.form.items.length) return;
    const items = [...this.form.items];
    [items[index], items[destino]] = [items[destino], items[index]];
    this.form.items = items;
  }

  guardar(): void {
    this.formError.set('');
    const f = this.form;
    if (!f.nombre.trim() || !f.descripcion.trim()) {
      this.formError.set('Completa el nombre y la descripción.');
      return;
    }
    if (this.rol() === 'Entrenador' && !f.usuarioId) {
      this.formError.set('Elige el usuario al que le asignas la rutina.');
      return;
    }
    if (f.items.length === 0) {
      this.formError.set('Agrega al menos un ejercicio.');
      return;
    }
    if (f.items.some((i) => i.series < 1 || i.repeticiones < 1 || i.tiempoDescansoSegundos < 0)) {
      this.formError.set('Series y repeticiones deben ser mayores que cero y el descanso no puede ser negativo.');
      return;
    }
    if (f.items.some((i) => (i.pesoKg ?? 0) < 0 || (i.pesoKg ?? 0) > PESO_MAXIMO_KG)) {
      this.formError.set(`El peso debe estar entre 0 y ${PESO_MAXIMO_KG} kg.`);
      return;
    }

    const request: RutinaRequest = {
      nombre: f.nombre.trim(),
      descripcion: f.descripcion.trim(),
      nivelDificultad: f.nivelDificultad,
      usuarioId: this.rol() === 'Usuario' ? this.miId() : f.usuarioId,
      permitirEdicionEntrenador: f.permitirEdicionEntrenador,
      rutinaEjercicios: f.items.map((i, index) => ({
        ejercicioId: i.ejercicioId,
        series: i.series,
        repeticiones: i.repeticiones,
        pesoKg: i.pesoKg ?? 0,
        tiempoDescansoSegundos: i.tiempoDescansoSegundos,
        ordenAparicion: index + 1,
      })),
    };

    this.saving.set(true);
    const peticion = f.id ? this.api.updateRutina(f.id, request) : this.api.createRutina(request);
    peticion.subscribe({
      next: (rutina) => {
        this.rutinas.update((items) =>
          f.id ? items.map((r) => (r.id === rutina.id ? rutina : r)) : [rutina, ...items],
        );
        this.saving.set(false);
        this.editorAbierto.set(false);
        this.aviso.set(f.id ? `Rutina “${rutina.nombre}” actualizada.` : `Rutina “${rutina.nombre}” creada.`);
        this.logrosGuardado.set(rutina.progresos ?? []);
      },
      error: (error) => {
        this.saving.set(false);
        this.formError.set(mensajeError(error, 'No se pudo guardar la rutina.'));
      },
    });
  }

  eliminar(r: Rutina): void {
    if (!confirm(`¿Eliminar la rutina “${r.nombre}”? Esta acción no se puede deshacer.`)) return;
    this.api.deleteRutina(r.id).subscribe({
      next: () => {
        this.rutinas.update((items) => items.filter((x) => x.id !== r.id));
        this.aviso.set(`Rutina “${r.nombre}” eliminada.`);
      },
      error: (error) => this.errorMessage.set(mensajeError(error, 'No se pudo eliminar la rutina.')),
    });
  }

  // "+2 reps (10 → 12)" / "+5 kg (40 → 45)"; vacío si ese valor no subió.
  subida(anterior: number, nuevo: number, unidad: string): string {
    const diferencia = Math.round((nuevo - anterior) * 100) / 100;
    return diferencia > 0 ? `+${diferencia} ${unidad} (${anterior} → ${nuevo})` : '';
  }

  private abrirEditor(): void {
    this.formError.set('');
    this.aviso.set('');
    this.logrosGuardado.set([]);
    this.busquedaEjercicio.set('');
    this.grupoEjercicio.set('');
    this.editorAbierto.set(true);
    setTimeout(() => document.getElementById('editor-rutina')?.scrollIntoView({ behavior: 'smooth' }));
  }

  private formVacio(): FormRutina {
    return {
      id: null,
      nombre: '',
      descripcion: '',
      nivelDificultad: 'Intermedio',
      usuarioId: '',
      // Un entrenador que crea la rutina normalmente quiere poder ajustarla después.
      permitirEdicionEntrenador: this.auth.role() === 'Entrenador',
      items: [],
    };
  }
}
