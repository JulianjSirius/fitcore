export type AccountType = 'administrador' | 'usuario' | 'entrenador';
export type Role = 'Administrador' | 'Usuario' | 'Entrenador';

export interface LoginRequest {
  email: string;
  contrasena: string;
}

export interface LoginResponse {
  token: string;
  role: Role;
}

export interface UsuarioRequest {
  firstName: string;
  lastName: string;
  email: string;
  contrasena: string;
  telefono: number;
  codigoRegistro: string;
}

export interface CodigoRegistro {
  id: string;
  codigo: string;
  creadoPorAdministradorId: string;
  fechaCreacion: string;
  fechaExpiracion: string;
  usadoPorUsuarioId?: string | null;
  fechaUso?: string | null;
}

export interface EntrenadorRequest {
  nombre: string;
  especialidad: string;
  horario: string;
  email: string;
  contrasena: string;
  telefono: number;
}

export interface AdministradorRequest {
  nombre: string;
  lastName: string;
  email: string;
  contrasena: string;
  telefono: number;
  nivelAcceso: string;
}

export interface SessionUser {
  id: string;
  email: string;
  role: Role;
  nivelAcceso?: string;
}

export interface Ejercicio {
  id: string;
  nombre: string;
  grupoMuscular: string;
  descripcionOrientativa: string;
}

export interface EjercicioRequest {
  nombre: string;
  grupoMuscular: string;
  descripcionOrientativa: string;
}

export interface RutinaEjercicio {
  ejercicioId: string;
  nombre: string;
  grupoMuscular: string;
  series: number;
  repeticiones: number;
  pesoKg: number;
  tiempoDescansoSegundos: number;
  ordenAparicion: number;
}

// Mejora de un ejercicio al editar la rutina (subieron repeticiones o peso).
export interface ProgresoEjercicio {
  id: string;
  rutinaId: string;
  ejercicioId: string;
  nombreEjercicio: string;
  repeticionesAnteriores: number;
  repeticionesNuevas: number;
  diferenciaRepeticiones: number;
  pesoAnteriorKg: number;
  pesoNuevoKg: number;
  diferenciaPesoKg: number;
  esRecordPersonal: boolean;
  fecha: string;
}

export interface Rutina {
  id: string;
  nombre: string;
  descripcion: string;
  nivelDificultad: string;
  usuarioId: string;
  creadorId: string;
  permitirEdicionEntrenador: boolean;
  fechaCreacion: string;
  fechaActualizacion?: string;
  rutinaEjercicios: RutinaEjercicio[];
  // Solo viene lleno en la respuesta de actualizar.
  progresos?: ProgresoEjercicio[];
}

export interface RutinaEjercicioRequest {
  ejercicioId: string;
  series: number;
  repeticiones: number;
  pesoKg: number;
  tiempoDescansoSegundos: number;
  ordenAparicion: number;
}

// Insignia (permanente) o reto (se reinicia cada mes).
export interface Logro {
  codigo: string;
  nombre: string;
  descripcion: string;
  meta: number;
  progreso: number;
  obtenido: boolean;
  fechaObtencion?: string | null;
}

export interface ResumenAsistencia {
  rachaActual: number;
  mejorRacha: number;
  totalAsistencias: number;
  asistenciasEsteMes: number;
  ultimaAsistencia?: string | null;
  diasRecientes: string[];
  insignias: Logro[];
  retos: Logro[];
}

export interface ResumenProgreso {
  totalMejoras: number;
  recordsPersonales: number;
  kgGanados: number;
  repeticionesGanadas: number;
  mejorasEsteMes: number;
  historial: ProgresoEjercicio[];
  insignias: Logro[];
  retos: Logro[];
}

export interface RutinaRequest {
  nombre: string;
  descripcion: string;
  nivelDificultad: string;
  usuarioId: string;
  permitirEdicionEntrenador: boolean;
  rutinaEjercicios: RutinaEjercicioRequest[];
}

export type CodigoPlan = 'Mensual' | 'Duo' | 'Anual';
export type MetodoPagoManual = 'Efectivo' | 'Transferencia' | 'Datafono';

export interface Plan {
  id: string;
  codigo: CodigoPlan;
  nombre: string;
  precio: number;
  duracionMeses: number;
  maxBeneficiarios: number;
  activo: boolean;
  fechaActualizacion: string;
}

export interface PlanUpdateRequest {
  nombre: string;
  precio: number;
  duracionMeses: number;
  activo: boolean;
}

export interface Membresia {
  id: string;
  planId: string;
  planCodigo: CodigoPlan;
  planNombre: string;
  estado: 'Activa' | 'Vencida';
  fechaInicio: string;
  fechaVencimiento: string;
  usuarioTitularId: string;
  titularNombre: string;
  acompananteUsuarioId?: string | null;
  acompananteNombre?: string | null;
}

export interface Pago {
  id: string;
  usuarioId: string;
  planId: string;
  planNombre: string;
  monto: number;
  moneda: string;
  metodo: string;
  estado: string;
  referencia: string;
  fecha: string;
  membresiaId?: string | null;
  acompananteUsuarioId?: string | null;
}

export interface RegistrarPagoRequest {
  usuarioId: string;
  planId: string;
  metodo: MetodoPagoManual;
  referencia?: string;
  acompananteEmail?: string;
}

export interface PagoRegistrado {
  pago: Pago;
  membresia: Membresia;
}

export interface UsuarioResumen {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  telefono: number;
}

export interface TokenAccesoQr {
  token: string;
  expiraEn: string;
}

export interface UsuarioUpdateRequest {
  firstName: string;
  lastName: string;
  email: string;
  contrasena: string;
  telefono: number;
}

export interface Entrenador {
  id: string;
  nombre: string;
  especialidad: string;
  horario: string;
  email: string;
  telefono: number;
}

export interface Administrador {
  id: string;
  nombre: string;
  lastName: string;
  email: string;
  telefono: number;
  nivelAcceso: string;
}

export interface AccesoResult {
  permitido: boolean;
  motivo: string;
  usuarioId?: string | null;
  nombre?: string | null;
  plan?: string | null;
  fechaVencimiento?: string | null;
}
