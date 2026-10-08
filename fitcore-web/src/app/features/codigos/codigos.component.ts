import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { catchError, of } from 'rxjs';
import { IdentityApiService } from '../../core/api/identity-api.service';
import { Administrador, CodigoRegistro, UsuarioResumen } from '../../shared/models/api.models';
import { mensajeError } from '../../shared/utils/errores';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { IconComponent } from '../../shared/ui/icon.component';
import { LoadingStateComponent } from '../../shared/ui/loading-state.component';

type EstadoCodigo = 'Disponible' | 'Usado' | 'Vencido';

@Component({
  selector: 'app-codigos',
  imports: [FormsModule, DatePipe, EmptyStateComponent, LoadingStateComponent, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './codigos.component.html',
  styleUrl: './codigos.component.css',
})
export class CodigosComponent implements OnInit {
  private readonly api = inject(IdentityApiService);

  readonly estados: (EstadoCodigo | '')[] = ['', 'Disponible', 'Usado', 'Vencido'];
  readonly codigos = signal<CodigoRegistro[]>([]);
  readonly usuarios = signal<UsuarioResumen[]>([]);
  readonly administradores = signal<Administrador[]>([]);
  readonly cargando = signal(true);
  readonly generando = signal(false);
  readonly error = signal('');
  readonly ultimo = signal<CodigoRegistro | null>(null);
  readonly copiado = signal(false);
  readonly filtro = signal<EstadoCodigo | ''>('');
  diasVigencia = 7;

  readonly filtrados = computed(() => {
    const f = this.filtro();
    return this.codigos()
      .filter((c) => !f || this.estado(c) === f)
      .sort((a, b) => b.fechaCreacion.localeCompare(a.fechaCreacion));
  });

  readonly conteo = computed(() => {
    const c: Record<string, number> = { '': this.codigos().length, Disponible: 0, Usado: 0, Vencido: 0 };
    this.codigos().forEach((x) => c[this.estado(x)]++);
    return c;
  });

  ngOnInit(): void {
    this.api.getCodigosRegistro().subscribe({
      next: (items) => {
        this.codigos.set(items);
        this.cargando.set(false);
      },
      error: (e) => {
        this.error.set(mensajeError(e, 'No se pudieron cargar los códigos.'));
        this.cargando.set(false);
      },
    });
    this.api.getUsuarios().pipe(catchError(() => of([]))).subscribe((u) => this.usuarios.set(u));
    this.api.getAdministradores().pipe(catchError(() => of([]))).subscribe((a) => this.administradores.set(a));
  }

  generar(): void {
    this.error.set('');
    this.copiado.set(false);
    this.generando.set(true);
    this.api.generarCodigoRegistro(this.diasVigencia).subscribe({
      next: (codigo) => {
        this.ultimo.set(codigo);
        this.codigos.update((items) => [codigo, ...items]);
        this.generando.set(false);
      },
      error: (e) => {
        this.generando.set(false);
        this.error.set(mensajeError(e, 'No se pudo generar el código.'));
      },
    });
  }

  copiar(codigo: string): void {
    void navigator.clipboard?.writeText(codigo).then(() => {
      this.copiado.set(true);
      setTimeout(() => this.copiado.set(false), 2000);
    });
  }

  estado(c: CodigoRegistro): EstadoCodigo {
    if (c.usadoPorUsuarioId) return 'Usado';
    return new Date(c.fechaExpiracion) <= new Date() ? 'Vencido' : 'Disponible';
  }

  nombreUsuario(id: string | null | undefined): string {
    const u = this.usuarios().find((x) => x.id === id);
    return u ? `${u.firstName} ${u.lastName}` : '';
  }

  nombreAdmin(id: string): string {
    const a = this.administradores().find((x) => x.id === id);
    return a ? `${a.nombre} ${a.lastName}` : '';
  }
}
