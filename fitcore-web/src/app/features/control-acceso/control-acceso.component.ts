import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IdentityApiService } from '../../core/api/identity-api.service';
import { AccesoResult } from '../../shared/models/api.models';
import { mensajeError } from '../../shared/utils/errores';
import { IconComponent } from '../../shared/ui/icon.component';

// BarcodeDetector existe en Chrome/Edge/Android pero aún no está en los tipos de TypeScript.
interface DetectorCodigos {
  detect(fuente: HTMLVideoElement): Promise<{ rawValue: string }[]>;
}
declare const BarcodeDetector:
  | (new (opciones: { formats: string[] }) => DetectorCodigos)
  | undefined;

interface Validacion extends AccesoResult {
  hora: Date;
}

const PAUSA_TRAS_LECTURA_MS = 3000;

@Component({
  selector: 'app-control-acceso',
  imports: [FormsModule, DatePipe, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './control-acceso.component.html',
  styleUrl: './control-acceso.component.css',
})
export class ControlAccesoComponent {
  private readonly api = inject(IdentityApiService);
  private readonly video = viewChild<ElementRef<HTMLVideoElement>>('video');

  readonly soportaCamara =
    typeof BarcodeDetector !== 'undefined' && !!navigator.mediaDevices?.getUserMedia;
  readonly camaraActiva = signal(false);
  readonly errorCamara = signal('');
  readonly validando = signal(false);
  readonly resultado = signal<Validacion | null>(null);
  readonly error = signal('');
  readonly historial = signal<Validacion[]>([]);
  tokenManual = '';

  private stream: MediaStream | null = null;
  private detector: DetectorCodigos | null = null;
  private temporizador: ReturnType<typeof setTimeout> | null = null;
  private pausado = false;

  constructor() {
    inject(DestroyRef).onDestroy(() => this.detenerCamara());
  }

  async iniciarCamara(): Promise<void> {
    this.errorCamara.set('');
    try {
      this.stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'environment' },
      });
      this.detector = new BarcodeDetector!({ formats: ['qr_code'] });
      this.camaraActiva.set(true);
      // El <video> aparece cuando camaraActiva = true.
      setTimeout(() => {
        const video = this.video()?.nativeElement;
        if (!video || !this.stream) return;
        video.srcObject = this.stream;
        void video.play();
        this.escanear();
      });
    } catch {
      this.errorCamara.set('No se pudo abrir la cámara. Revisa los permisos del navegador o pega el código.');
      this.detenerCamara();
    }
  }

  detenerCamara(): void {
    if (this.temporizador) clearTimeout(this.temporizador);
    this.temporizador = null;
    this.stream?.getTracks().forEach((t) => t.stop());
    this.stream = null;
    this.camaraActiva.set(false);
  }

  validarManual(): void {
    const token = this.tokenManual.trim();
    if (token) this.validar(token);
  }

  private escanear(): void {
    if (!this.camaraActiva()) return;
    const video = this.video()?.nativeElement;
    if (video && this.detector && !this.pausado && video.readyState >= 2) {
      this.detector
        .detect(video)
        .then((codigos) => {
          const valor = codigos[0]?.rawValue;
          if (valor) {
            this.pausado = true;
            this.validar(valor);
            setTimeout(() => (this.pausado = false), PAUSA_TRAS_LECTURA_MS);
          }
        })
        .catch(() => undefined);
    }
    this.temporizador = setTimeout(() => this.escanear(), 250);
  }

  private validar(token: string): void {
    this.validando.set(true);
    this.error.set('');
    this.api.validarAcceso(token).subscribe({
      next: (r) => {
        const validacion = { ...r, hora: new Date() };
        this.resultado.set(validacion);
        this.historial.update((items) => [validacion, ...items].slice(0, 15));
        this.validando.set(false);
        this.tokenManual = '';
      },
      error: (e) => {
        this.validando.set(false);
        this.error.set(mensajeError(e, 'No se pudo validar el código.'));
      },
    });
  }
}
