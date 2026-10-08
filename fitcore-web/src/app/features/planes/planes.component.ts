import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { MembresiasApiService } from '../../core/api/membresias-api.service';
import { Plan } from '../../shared/models/api.models';
import { mensajeError } from '../../shared/utils/errores';
import { IconComponent } from '../../shared/ui/icon.component';
import { LoadingStateComponent } from '../../shared/ui/loading-state.component';

const BENEFICIOS: Record<string, string[]> = {
  Mensual: ['Acceso ilimitado con QR', 'Rutinas personalizadas', 'Asesoría de entrenadores'],
  Duo: ['Todo lo del plan Mensual', 'Para dos personas', 'Un solo pago'],
  Anual: ['Todo lo del plan Mensual', '12 meses de acceso', 'El mejor precio por mes'],
};

@Component({
  selector: 'app-planes',
  imports: [FormsModule, CurrencyPipe, DatePipe, IconComponent, LoadingStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './planes.component.html',
  styleUrl: './planes.component.css',
})
export class PlanesComponent implements OnInit {
  private readonly api = inject(MembresiasApiService);
  readonly auth = inject(AuthService);

  readonly planes = signal<Plan[]>([]);
  readonly cargando = signal(true);
  readonly error = signal('');
  readonly aviso = signal('');
  readonly editando = signal<string | null>(null);
  readonly guardando = signal(false);
  borrador: Plan | null = null;

  readonly esAdmin = computed(() => this.auth.role() === 'Administrador');
  readonly visibles = computed(() =>
    this.esAdmin() ? this.planes() : this.planes().filter((p) => p.activo),
  );

  ngOnInit(): void {
    this.api.getPlanes().subscribe({
      next: (p) => {
        this.planes.set(p);
        this.cargando.set(false);
      },
      error: (e) => {
        this.error.set(mensajeError(e, 'No se pudieron cargar los planes.'));
        this.cargando.set(false);
      },
    });
  }

  beneficios(plan: Plan): string[] {
    return BENEFICIOS[plan.codigo] ?? [];
  }

  precioMensual(plan: Plan): number {
    return Math.round(plan.precio / plan.duracionMeses / (plan.maxBeneficiarios || 1));
  }

  editar(plan: Plan): void {
    this.borrador = { ...plan };
    this.aviso.set('');
    this.error.set('');
    this.editando.set(plan.id);
  }

  cancelar(): void {
    this.editando.set(null);
    this.borrador = null;
  }

  guardar(): void {
    const b = this.borrador;
    if (!b) return;
    this.guardando.set(true);
    this.api
      .updatePlan(b.id, {
        nombre: b.nombre.trim(),
        precio: b.precio,
        duracionMeses: b.duracionMeses,
        activo: b.activo,
      })
      .subscribe({
        next: (actualizado) => {
          this.planes.update((items) => items.map((p) => (p.id === actualizado.id ? actualizado : p)));
          this.guardando.set(false);
          this.cancelar();
          this.aviso.set(`Plan ${actualizado.nombre} actualizado. Aplica a los pagos nuevos.`);
        },
        error: (e) => {
          this.guardando.set(false);
          this.error.set(mensajeError(e, 'No se pudo guardar el plan.'));
        },
      });
  }
}
