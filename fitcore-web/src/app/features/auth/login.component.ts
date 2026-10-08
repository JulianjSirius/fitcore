import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AccountType, Plan } from '../../shared/models/api.models';
import { AuthService } from '../../core/auth/auth.service';
import { MembresiasApiService } from '../../core/api/membresias-api.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink, CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.component.html',
  styleUrl: './auth.css',
})
export class LoginComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly membresiasApi = inject(MembresiasApiService);

  readonly tipos: { valor: AccountType; texto: string }[] = [
    { valor: 'usuario', texto: 'Usuario' },
    { valor: 'entrenador', texto: 'Entrenador' },
    { valor: 'administrador', texto: 'Administrador' },
  ];

  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly planes = signal<Plan[]>([]);
  accountType: AccountType = 'usuario';
  email = '';
  password = '';

  ngOnInit(): void {
    // Los precios son públicos; si la API no responde, el login funciona igual.
    this.membresiasApi.getPlanes().subscribe({
      next: (planes) => this.planes.set(planes.filter((p) => p.activo)),
      error: () => this.planes.set([]),
    });
  }

  submit(): void {
    this.loading.set(true);
    this.errorMessage.set('');
    this.auth.login(this.accountType, { email: this.email, contrasena: this.password }).subscribe({
      next: () => {
        this.loading.set(false);
        void this.router.navigate(['/dashboard']);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set(
          'No pudimos validar tus credenciales. Revisa el tipo de cuenta, el correo y la contraseña.',
        );
      },
    });
  }
}
