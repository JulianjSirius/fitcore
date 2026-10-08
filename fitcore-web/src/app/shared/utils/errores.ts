import { HttpErrorResponse } from '@angular/common/http';

// Convierte la respuesta de error de la API en un texto para mostrar.
// Cubre { message } de los controladores y los ProblemDetails de validación de ASP.NET.
export function mensajeError(error: unknown, porDefecto: string): string {
  if (!(error instanceof HttpErrorResponse)) return porDefecto;
  const cuerpo = error.error;

  if (typeof cuerpo?.message === 'string' && cuerpo.message) return cuerpo.message;

  if (cuerpo?.errors && typeof cuerpo.errors === 'object') {
    const primero = Object.values(cuerpo.errors as Record<string, string[]>).flat()[0];
    if (primero) return primero;
  }

  if (error.status === 0) return 'No hay conexión con el servidor. Revisa que el Gateway esté corriendo.';
  if (error.status === 403) return 'No tienes permiso para esta acción.';
  if (error.status === 404) return 'No se encontró el registro.';
  return porDefecto;
}

