import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MembresiasApiService } from '../../core/api/membresias-api.service';
import { IdentityApiService } from '../../core/api/identity-api.service';
import {
  Membresia,
  MetodoPagoManual,
  Pago,
  Plan,
  RegistrarPagoRequest,
  UsuarioResumen,
} from '../../shared/models/api.models';
import { mensajeError } from '../../shared/utils/errores';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { IconComponent } from '../../shared/ui/icon.component';
import { LoadingStateComponent } from '../../shared/ui/loading-state.component';

@Component({
  selector: 'app-membresias',
  imports: [FormsModule, DatePipe, CurrencyPipe, EmptyStateComponent, LoadingStateComponent, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './membresias.component.html',
  styleUrl: './membresias.component.css',
})
export class MembresiasComponent implements OnInit {
  private readonly api = inject(MembresiasApiService);
  private readonly identity = inject(IdentityApiService);
  private readonly route = inject(ActivatedRoute);

  readonly metodos: { valor: MetodoPagoManual; texto: string }[] = [
    { valor: 'Efectivo', texto: 'Efectivo' },
    { valor: 'Transferencia', texto: 'Transferencia' },
    { valor: 'Datafono', texto: 'Datáfono' },
  ];

  readonly planes = signal<Plan[]>([]);
  readonly usuarios = signal<UsuarioResumen[]>([]);
  readonly pagos = signal<Pago[]>([]);
  readonly cargando = signal(true);
  readonly error = signal('');

  // Formulario de pago
  pago: RegistrarPagoRequest = this.nuevoPago();
  readonly filtroUsuarioForm = signal('');
  readonly registrando = signal(false);
  readonly errorPago = signal('');
  readonly confirmacion = signal<Membresia | null>(null);
  readonly membresiaActual = signal<Membresia | null>(null);

  // Historial
  readonly filtroUsuario = signal('');
  readonly filtroMetodo = signal('');

  readonly planesActivos = computed(() => this.planes().filter((p) => p.activo));

  readonly usuariosParaElegir = computed(() => {
    const texto = this.filtroUsuarioForm().trim().toLowerCase();
    return this.usuarios()
      .filter(
        (u) =>
          !texto ||
          u.id === this.pago.usuarioId ||
          `${u.firstName} ${u.lastName} ${u.email}`.toLowerCase().includes(texto),
      )
      .sort((a, b) => `${a.firstName} ${a.lastName}`.localeCompare(`${b.firstName} ${b.lastName}`));
  });

  readonly pagosFiltrados = computed(() => {
    const usuario = this.filtroUsuario();
    const metodo = this.filtroMetodo();
    return this.pagos()
      .filter((p) => !usuario || p.usuarioId === usuario)
      .filter((p) => !metodo || p.metodo === metodo)
      .sort((a, b) => b.fecha.localeCompare(a.fecha));
  });

  readonly totalFiltrado = computed(() => this.pagosFiltrados().reduce((t, p) => t + p.monto, 0));

  ngOnInit(): void {
    this.api.getPlanes().subscribe({
      next: (p) => this.planes.set(p),
      error: (e) => this.error.set(mensajeError(e, 'No se pudieron cargar los planes.')),
    });
    this.identity.getUsuarios().subscribe({
      next: (u) => {
        this.usuarios.set(u);
        // Llegada desde la ficha de un usuario: /membresias?usuario=<id>
        const preseleccionado = this.route.snapshot.queryParamMap.get('usuario');
        if (preseleccionado && u.some((x) => x.id === preseleccionado)) {
          this.pago.usuarioId = preseleccionado;
          this.alCambiarUsuario();
        }
      },
      error: (e) => this.error.set(mensajeError(e, 'No se pudo cargar la lista de usuarios.')),
    });
    this.cargarPagos();
  }

  planSeleccionado(): Plan | undefined {
    return this.planes().find((p) => p.id === this.pago.planId);
  }

  esDuo(): boolean {
    return (this.planSeleccionado()?.maxBeneficiarios ?? 1) > 1;
  }

  alCambiarUsuario(): void {
    this.membresiaActual.set(null);
    this.confirmacion.set(null);
    if (!this.pago.usuarioId) return;
    this.api.getMembresiaDeUsuario(this.pago.usuarioId).subscribe({
      next: (m) => this.membresiaActual.set(m),
      error: () => this.membresiaActual.set(null),
    });
  }

  // Qué pasará con este pago, siguiendo las reglas de MembresiaService.
  resumenPago(): string {
    const plan = this.planSeleccionado();
    if (!plan) return '';
    const actual = this.membresiaActual();
    const vigente = actual && actual.estado === 'Activa' && new Date(actual.fechaVencimiento) > new Date();
    if (vigente && actual.planId !== plan.id) {
      return `El usuario tiene ${actual.planNombre} vigente: solo puede renovar ese plan hasta que venza.`;
    }
    if (vigente) {
      const nueva = new Date(actual.fechaVencimiento);
      nueva.setMonth(nueva.getMonth() + plan.duracionMeses);
      return `Renovación: la membresía pasará a vencer el ${nueva.toLocaleDateString('es-CO')}.`;
    }
    const vence = new Date();
    vence.setMonth(vence.getMonth() + plan.duracionMeses);
    return `Nueva membresía ${plan.nombre} hasta el ${vence.toLocaleDateString('es-CO')}.`;
  }

  registrar(form: NgForm): void {
    if (form.invalid) return;
    this.errorPago.set('');
    this.confirmacion.set(null);
    this.registrando.set(true);
    const request: RegistrarPagoRequest = {
      usuarioId: this.pago.usuarioId,
      planId: this.pago.planId,
      metodo: this.pago.metodo,
      referencia: this.pago.referencia?.trim() || undefined,
      acompananteEmail: this.esDuo() ? this.pago.acompananteEmail?.trim() : undefined,
    };

    this.api.registrarPago(request).subscribe({
      next: ({ membresia }) => {
        this.registrando.set(false);
        this.confirmacion.set(membresia);
        this.membresiaActual.set(membresia);
        this.pago = { ...this.nuevoPago(), usuarioId: request.usuarioId };
        this.cargarPagos();
      },
      error: (e) => {
        this.registrando.set(false);
        this.errorPago.set(mensajeError(e, 'No se pudo registrar el pago.'));
      },
    });
  }

  nombreUsuario(id: string | null | undefined): string {
    const u = this.usuarios().find((x) => x.id === id);
    return u ? `${u.firstName} ${u.lastName}` : '—';
  }

  private cargarPagos(): void {
    this.api.getPagos().subscribe({
      next: (p) => {
        this.pagos.set(p);
        this.cargando.set(false);
      },
      error: (e) => {
        this.error.set(mensajeError(e, 'No se pudo cargar el historial de pagos.'));
        this.cargando.set(false);
      },
    });
  }

  private nuevoPago(): RegistrarPagoRequest {
    return { usuarioId: '', planId: '', metodo: 'Efectivo', referencia: '', acompananteEmail: '' };
  }
}
