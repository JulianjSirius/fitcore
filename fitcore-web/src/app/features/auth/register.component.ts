import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { IdentityApiService } from '../../core/api/identity-api.service';
import { UsuarioRequest } from '../../shared/models/api.models';
import { mensajeError } from '../../shared/utils/errores';

// El registro público es solo para usuarios y exige el código que entrega un
// administrador. Entrenadores y administradores se crean desde /entrenadores y /administradores.
@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './register.component.html',
  styleUrl: './auth.css',
})
export class RegisterComponent {
  private readonly api = inject(IdentityApiService);
  private readonly router = inject(Router);
  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');
  form: UsuarioRequest = {
    firstName: '',
    lastName: '',
    email: '',
    contrasena: '',
    telefono: 0,
    codigoRegistro: '',
  };

  submit(): void {
    this.loading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.api.registerUsuario(this.form).subscribe({
      next: () => {
        this.loading.set(false);
        this.successMessage.set('Cuenta creada correctamente. Ahora puedes iniciar sesión.');
        setTimeout(() => void this.router.navigate(['/login']), 1200);
      },
      error: (error) => {
        this.loading.set(false);
        this.errorMessage.set(
          mensajeError(error, 'No se pudo crear la cuenta. Verifica los datos e inténtalo de nuevo.'),
        );
      },
    });
  }
}
