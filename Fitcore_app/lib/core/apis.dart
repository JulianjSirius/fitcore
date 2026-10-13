import '../models/modelos.dart';
import 'api_cliente.dart';
import 'sesion.dart';

ApiCliente get _api => sesion.api;

List<T> _lista<T>(dynamic datos, T Function(Json) desde) =>
    (datos as List? ?? []).map((e) => desde(e as Json)).toList();

/// Identity: usuarios, entrenadores, administradores, códigos y acceso.
class IdentityApi {
  const IdentityApi._();

  // ---------- Usuarios ----------
  static Future<void> registrarUsuario(Json datos) => _api.post('/Usuario', datos);

  static Future<List<UsuarioResumen>> usuarios() async =>
      _lista(await _api.get('/Usuario'), UsuarioResumen.fromJson);

  static Future<UsuarioResumen> usuario(String id) async =>
      UsuarioResumen.fromJson(await _api.get('/Usuario/$id') as Json);

  static Future<void> actualizarUsuario(String id, Json datos) => _api.put('/Usuario/$id', datos);

  // ---------- Entrenadores ----------
  static Future<List<Entrenador>> entrenadores() async =>
      _lista(await _api.get('/Entrenador'), Entrenador.fromJson);

  static Future<Entrenador> entrenador(String id) async =>
      Entrenador.fromJson(await _api.get('/Entrenador/$id') as Json);

  static Future<Entrenador> crearEntrenador(Json datos) async =>
      Entrenador.fromJson(await _api.post('/Entrenador', datos) as Json);

  static Future<void> actualizarEntrenador(String id, Json datos) => _api.put('/Entrenador/$id', datos);

  // ---------- Administradores ----------
  static Future<List<Administrador>> administradores() async =>
      _lista(await _api.get('/Administrador'), Administrador.fromJson);

  static Future<Administrador> administrador(String id) async =>
      Administrador.fromJson(await _api.get('/Administrador/$id') as Json);

  static Future<Administrador> crearAdministrador(Json datos) async =>
      Administrador.fromJson(await _api.post('/Administrador', datos) as Json);

  static Future<void> actualizarAdministrador(String id, Json datos) =>
      _api.put('/Administrador/$id', datos);

  static Future<void> eliminarAdministrador(String id) => _api.delete('/Administrador/$id');

  // ---------- Códigos de registro ----------
  static Future<List<CodigoRegistro>> codigos() async =>
      _lista(await _api.get('/CodigoRegistro'), CodigoRegistro.fromJson);

  static Future<CodigoRegistro> generarCodigo(int diasVigencia) async =>
      CodigoRegistro.fromJson(await _api.post('/CodigoRegistro', {'diasVigencia': diasVigencia}) as Json);

  // ---------- Control de acceso ----------
  static Future<AccesoResult> validarAcceso(String token) async =>
      AccesoResult.fromJson(await _api.post('/Acceso/validar', {'token': token}) as Json);
}

/// Planes, membresías, pagos y token QR.
class MembresiasApi {
  const MembresiasApi._();

  static Future<List<Plan>> planes() async => _lista(await _api.get('/Plan'), Plan.fromJson);

  static Future<Plan> actualizarPlan(String id, Json datos) async =>
      Plan.fromJson(await _api.put('/Plan/$id', datos) as Json);

  static Future<Membresia> miMembresia() async =>
      Membresia.fromJson(await _api.get('/Membresia/mia') as Json);

  static Future<Membresia> membresiaDeUsuario(String usuarioId) async =>
      Membresia.fromJson(await _api.get('/Membresia/usuario/$usuarioId') as Json);

  static Future<List<Pago>> pagos([String? usuarioId]) async {
    final query = usuarioId == null ? '' : '?usuarioId=${Uri.encodeComponent(usuarioId)}';
    return _lista(await _api.get('/Membresia/pagos$query'), Pago.fromJson);
  }

  /// Devuelve la membresía resultante del pago.
  static Future<Membresia> registrarPago(Json datos) async {
    final r = await _api.post('/Membresia/pagos', datos) as Json;
    return Membresia.fromJson(r['membresia'] as Json);
  }

  static Future<String> tokenAccesoQr() async =>
      (await _api.get('/Usuario/me/qr-token') as Json)['token'] as String;
}

/// Ejercicios y rutinas.
class WorkoutsApi {
  const WorkoutsApi._();

  static Future<List<Ejercicio>> ejercicios() async =>
      _lista(await _api.get('/Ejercicios'), Ejercicio.fromJson);

  static Future<Ejercicio> crearEjercicio(Json datos) async =>
      Ejercicio.fromJson(await _api.post('/Ejercicios', datos) as Json);

  static Future<Ejercicio> actualizarEjercicio(String id, Json datos) async =>
      Ejercicio.fromJson(await _api.put('/Ejercicios/$id', datos) as Json);

  static Future<void> eliminarEjercicio(String id) => _api.delete('/Ejercicios/$id');

  static Future<List<Rutina>> rutinas() async => _lista(await _api.get('/Rutinas'), Rutina.fromJson);

  static Future<Rutina> crearRutina(Json datos) async =>
      Rutina.fromJson(await _api.post('/Rutinas', datos) as Json);

  static Future<Rutina> actualizarRutina(String id, Json datos) async =>
      Rutina.fromJson(await _api.put('/Rutinas/$id', datos) as Json);

  static Future<void> eliminarRutina(String id) => _api.delete('/Rutinas/$id');
}

/// Retos y logros: la asistencia vive en Identity y el progreso en Workouts.
class LogrosApi {
  const LogrosApi._();

  static Future<ResumenAsistencia> miAsistencia() async =>
      ResumenAsistencia.fromJson(await _api.get('/Usuario/me/asistencia') as Json);

  static Future<ResumenProgreso> miProgreso() async =>
      ResumenProgreso.fromJson(await _api.get('/Progreso/mio') as Json);
}

/// Si una consulta falla, la pantalla se muestra igual con esa sección vacía.
Future<List<T>> oVacio<T>(Future<List<T>> consulta) async => await oNulo(consulta) ?? <T>[];

Future<T?> oNulo<T>(Future<T> consulta) async {
  try {
    return await consulta;
  } catch (_) {
    return null;
  }
}
