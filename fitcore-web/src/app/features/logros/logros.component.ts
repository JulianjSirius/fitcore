import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { LogrosApiService } from '../../core/api/logros-api.service';
import { Logro, ResumenAsistencia, ResumenProgreso } from '../../shared/models/api.models';
import { IconComponent } from '../../shared/ui/icon.component';
import { LoadingStateComponent } from '../../shared/ui/loading-state.component';

// Mismo tamaño que CalculadoraAsistencia.DiasRecientes en Identity.
const DIAS_CALENDARIO = 42;

interface DiaCalendario {
  fecha: string; // yyyy-MM-dd
  asistio: boolean;
  esHoy: boolean;
}

interface InsigniaVista extends Logro {
  icono: string;
}

const ICONO_INSIGNIA: Record<string, string> = {
  'primera-visita': 'check',
  'racha-7': 'fuego',
  'racha-30': 'fuego',
  'asistencias-50': 'estrella',
  'asistencias-100': 'estrella',
  'primera-mejora': 'rutinas',
  'primer-record': 'trofeo',
  'records-10': 'trofeo',
  'kg-50': 'rutinas',
  'repeticiones-100': 'rutinas',
};

const aIso = (d: Date) =>
  `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;

@Component({
  selector: 'app-logros',
  imports: [DatePipe, DecimalPipe, RouterLink, IconComponent, LoadingStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './logros.component.html',
  styleUrl: './logros.component.css',
})
export class LogrosComponent implements OnInit {
  private readonly api = inject(LogrosApiService);

  readonly cargando = signal(true);
  readonly asistencia = signal<ResumenAsistencia | null>(null);
  readonly progreso = signal<ResumenProgreso | null>(null);
  readonly errorAsistencia = signal(false);
  readonly errorProgreso = signal(false);

  readonly diasSemana = ['L', 'M', 'X', 'J', 'V', 'S', 'D'];

  // Últimas seis semanas; los huecos iniciales alinean el primer día con su columna (lunes primero).
  readonly calendario = computed(() => {
    const asistidos = new Set(this.asistencia()?.diasRecientes ?? []);
    const hoy = new Date();
    const inicio = new Date(hoy.getFullYear(), hoy.getMonth(), hoy.getDate() - (DIAS_CALENDARIO - 1));
    const huecos = (inicio.getDay() + 6) % 7;
    const dias: (DiaCalendario | null)[] = Array.from({ length: huecos }, () => null);
    for (let i = 0; i < DIAS_CALENDARIO; i++) {
      const fecha = aIso(new Date(inicio.getFullYear(), inicio.getMonth(), inicio.getDate() + i));
      dias.push({ fecha, asistio: asistidos.has(fecha), esHoy: i === DIAS_CALENDARIO - 1 });
    }
    return dias;
  });

  readonly retos = computed(() => [...(this.asistencia()?.retos ?? []), ...(this.progreso()?.retos ?? [])]);

  // Obtenidas primero (la más reciente arriba) y luego las que faltan, por avance.
  readonly insignias = computed<InsigniaVista[]>(() =>
    [...(this.asistencia()?.insignias ?? []), ...(this.progreso()?.insignias ?? [])]
      .map((l) => ({ ...l, icono: ICONO_INSIGNIA[l.codigo] ?? 'estrella' }))
      .sort((a, b) =>
        a.obtenido !== b.obtenido
          ? Number(b.obtenido) - Number(a.obtenido)
          : a.obtenido
            ? (b.fechaObtencion ?? '').localeCompare(a.fechaObtencion ?? '')
            : b.progreso / b.meta - a.progreso / a.meta,
      ),
  );

  readonly obtenidas = computed(() => this.insignias().filter((i) => i.obtenido).length);

  ngOnInit(): void {
    forkJoin({
      asistencia: this.api.getMiAsistencia().pipe(
        catchError(() => {
          this.errorAsistencia.set(true);
          return of(null);
        }),
      ),
      progreso: this.api.getMiProgreso().pipe(
        catchError(() => {
          this.errorProgreso.set(true);
          return of(null);
        }),
      ),
    }).subscribe(({ asistencia, progreso }) => {
      this.asistencia.set(asistencia);
      this.progreso.set(progreso);
      this.cargando.set(false);
    });
  }

  porcentaje(l: Logro): number {
    return Math.round((l.progreso / l.meta) * 100);
  }
}
