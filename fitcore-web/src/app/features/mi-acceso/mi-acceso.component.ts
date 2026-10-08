import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { QRCodeComponent } from 'angularx-qrcode';
import { MembresiasApiService } from '../../core/api/membresias-api.service';
import { Membresia } from '../../shared/models/api.models';
import { LoadingStateComponent } from '../../shared/ui/loading-state.component';

// El token dura 60 s en el servidor; se pide uno nuevo cuando quedan 10 s.
const VIDA_TOKEN_SEGUNDOS = 60;
const RENOVAR_CON_SEGUNDOS = 10;

@Component({
  selector: 'app-mi-acceso',
  imports: [DatePipe, RouterLink, QRCodeComponent, LoadingStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './mi-acceso.component.html',
  styleUrl: './mi-acceso.component.css',
})
export class MiAccesoComponent implements OnInit {
  private readonly api = inject(MembresiasApiService);
  private readonly destroyRef = inject(DestroyRef);

  readonly cargando = signal(true);
  readonly membresia = signal<Membresia | null>(null);
  readonly sinMembresia = signal('');
  readonly token = signal('');
  readonly segundosRestantes = signal(0);
  readonly errorQr = signal('');

  private intervalo: ReturnType<typeof setInterval> | null = null;
  private pidiendo = false;

  ngOnInit(): void {
    const alCambiarVisibilidad = () => {
      if (document.visibilityState === 'visible' && this.membresiaActiva()) {
        this.pedirToken();
      } else {
        this.detener();
      }
    };
    document.addEventListener('visibilitychange', alCambiarVisibilidad);
    this.destroyRef.onDestroy(() => {
      document.removeEventListener('visibilitychange', alCambiarVisibilidad);
      this.detener();
    });

    this.api.getMiMembresia().subscribe({
      next: (membresia) => {
        this.membresia.set(membresia);
        this.cargando.set(false);
        if (membresia.estado === 'Activa') {
          this.pedirToken();
        } else {
          this.sinMembresia.set('Tu membresía está vencida. Acércate a recepción para renovarla.');
        }
      },
      error: (error: HttpErrorResponse) => {
        this.cargando.set(false);
        this.sinMembresia.set(
          error.status === 404
            ? 'Todavía no tienes una membresía. Acércate a recepción para activarla.'
            : 'No pudimos consultar tu membresía. Inténtalo de nuevo en un momento.',
        );
      },
    });
  }

  membresiaActiva(): boolean {
    return this.membresia()?.estado === 'Activa';
  }

  pedirToken(): void {
    if (this.pidiendo) return;
    this.pidiendo = true;
    this.errorQr.set('');

    this.api.getTokenAccesoQr().subscribe({
      next: ({ token }) => {
        this.pidiendo = false;
        this.token.set(token);
        this.iniciarCuentaRegresiva();
      },
      error: (error: HttpErrorResponse) => {
        this.pidiendo = false;
        this.token.set('');
        this.detener();
        this.errorQr.set(
          error.error?.message ?? 'No se pudo generar tu código de acceso. Toca "Generar código".',
        );
      },
    });
  }

  private iniciarCuentaRegresiva(): void {
    this.detener();
    this.segundosRestantes.set(VIDA_TOKEN_SEGUNDOS);
    this.intervalo = setInterval(() => {
      const restantes = this.segundosRestantes() - 1;
      this.segundosRestantes.set(Math.max(restantes, 0));
      if (restantes <= RENOVAR_CON_SEGUNDOS) {
        this.pedirToken();
      }
    }, 1000);
  }

  private detener(): void {
    if (this.intervalo) {
      clearInterval(this.intervalo);
      this.intervalo = null;
    }
  }
}
