// Modelos de la API (mismos contratos que fitcore-web/src/app/shared/models/api.models.ts).

typedef Json = Map<String, dynamic>;

enum Rol {
  administrador('Administrador'),
  usuario('Usuario'),
  entrenador('Entrenador');

  const Rol(this.texto);
  final String texto;

  static Rol? desde(String? valor) {
    for (final r in Rol.values) {
      if (r.texto == valor) return r;
    }
    return null;
  }
}

/// Tipo de cuenta en la ruta de login: /Auth/login/{tipo}.
enum TipoCuenta {
  usuario('usuario', 'Usuario'),
  entrenador('entrenador', 'Entrenador'),
  administrador('administrador', 'Administrador');

  const TipoCuenta(this.valor, this.texto);
  final String valor;
  final String texto;
}

String _s(dynamic v) => v?.toString() ?? '';
int _i(dynamic v) => v is num ? v.toInt() : int.tryParse('$v') ?? 0;
double _d(dynamic v) => v is num ? v.toDouble() : double.tryParse('$v') ?? 0;
DateTime _f(dynamic v) => DateTime.tryParse(_s(v))?.toLocal() ?? DateTime.fromMillisecondsSinceEpoch(0);
DateTime? _fOpc(dynamic v) => v == null ? null : DateTime.tryParse(_s(v))?.toLocal();
List<T> _lista<T>(dynamic v, T Function(Json) desde) =>
    v is List ? v.map((e) => desde(e as Json)).toList() : <T>[];

class SesionUsuario {
  const SesionUsuario({required this.id, required this.email, required this.rol, this.nivelAcceso});
  final String id;
  final String email;
  final Rol rol;
  final String? nivelAcceso;
}

class CodigoRegistro {
  CodigoRegistro.fromJson(Json j)
      : id = _s(j['id']),
        codigo = _s(j['codigo']),
        creadoPorAdministradorId = _s(j['creadoPorAdministradorId']),
        fechaCreacion = _f(j['fechaCreacion']),
        fechaExpiracion = _f(j['fechaExpiracion']),
        usadoPorUsuarioId = j['usadoPorUsuarioId'] as String?,
        fechaUso = _fOpc(j['fechaUso']);

  final String id;
  final String codigo;
  final String creadoPorAdministradorId;
  final DateTime fechaCreacion;
  final DateTime fechaExpiracion;
  final String? usadoPorUsuarioId;
  final DateTime? fechaUso;
}

class Ejercicio {
  Ejercicio.fromJson(Json j)
      : id = _s(j['id']),
        nombre = _s(j['nombre']),
        grupoMuscular = _s(j['grupoMuscular']),
        descripcionOrientativa = _s(j['descripcionOrientativa']);

  final String id;
  final String nombre;
  final String grupoMuscular;
  final String descripcionOrientativa;
}

class RutinaEjercicio {
  RutinaEjercicio.fromJson(Json j)
      : ejercicioId = _s(j['ejercicioId']),
        nombre = _s(j['nombre']),
        grupoMuscular = _s(j['grupoMuscular']),
        series = _i(j['series']),
        repeticiones = _i(j['repeticiones']),
        pesoKg = _d(j['pesoKg']),
        tiempoDescansoSegundos = _i(j['tiempoDescansoSegundos']),
        ordenAparicion = _i(j['ordenAparicion']);

  final String ejercicioId;
  final String nombre;
  final String grupoMuscular;
  final int series;
  final int repeticiones;
  final double pesoKg;
  final int tiempoDescansoSegundos;
  final int ordenAparicion;
}

/// Mejora de un ejercicio al editar la rutina (subieron repeticiones o peso).
class ProgresoEjercicio {
  ProgresoEjercicio.fromJson(Json j)
      : id = _s(j['id']),
        nombreEjercicio = _s(j['nombreEjercicio']),
        repeticionesAnteriores = _i(j['repeticionesAnteriores']),
        repeticionesNuevas = _i(j['repeticionesNuevas']),
        diferenciaRepeticiones = _i(j['diferenciaRepeticiones']),
        pesoAnteriorKg = _d(j['pesoAnteriorKg']),
        pesoNuevoKg = _d(j['pesoNuevoKg']),
        diferenciaPesoKg = _d(j['diferenciaPesoKg']),
        esRecordPersonal = j['esRecordPersonal'] == true,
        fecha = _f(j['fecha']);

  final String id;
  final String nombreEjercicio;
  final int repeticionesAnteriores;
  final int repeticionesNuevas;
  final int diferenciaRepeticiones;
  final double pesoAnteriorKg;
  final double pesoNuevoKg;
  final double diferenciaPesoKg;
  final bool esRecordPersonal;
  final DateTime fecha;
}

class Rutina {
  Rutina.fromJson(Json j)
      : id = _s(j['id']),
        nombre = _s(j['nombre']),
        descripcion = _s(j['descripcion']),
        nivelDificultad = _s(j['nivelDificultad']),
        usuarioId = _s(j['usuarioId']),
        permitirEdicionEntrenador = j['permitirEdicionEntrenador'] == true,
        fechaCreacion = _f(j['fechaCreacion']),
        fechaActualizacion = _fOpc(j['fechaActualizacion']),
        rutinaEjercicios = _lista(j['rutinaEjercicios'], RutinaEjercicio.fromJson),
        progresos = _lista(j['progresos'], ProgresoEjercicio.fromJson);

  final String id;
  final String nombre;
  final String descripcion;
  final String nivelDificultad;
  final String usuarioId;
  final bool permitirEdicionEntrenador;
  final DateTime fechaCreacion;
  final DateTime? fechaActualizacion;
  final List<RutinaEjercicio> rutinaEjercicios;

  /// Solo viene lleno en la respuesta de actualizar.
  final List<ProgresoEjercicio> progresos;
}

/// Insignia (permanente) o reto (se reinicia cada mes).
class Logro {
  Logro.fromJson(Json j)
      : codigo = _s(j['codigo']),
        nombre = _s(j['nombre']),
        descripcion = _s(j['descripcion']),
        meta = _i(j['meta']),
        progreso = _i(j['progreso']),
        obtenido = j['obtenido'] == true,
        fechaObtencion = _fOpc(j['fechaObtencion']);

  final String codigo;
  final String nombre;
  final String descripcion;
  final int meta;
  final int progreso;
  final bool obtenido;
  final DateTime? fechaObtencion;

  double get avance => meta <= 0 ? 0 : (progreso / meta).clamp(0, 1).toDouble();
}

class ResumenAsistencia {
  ResumenAsistencia.fromJson(Json j)
      : rachaActual = _i(j['rachaActual']),
        mejorRacha = _i(j['mejorRacha']),
        totalAsistencias = _i(j['totalAsistencias']),
        asistenciasEsteMes = _i(j['asistenciasEsteMes']),
        diasRecientes = (j['diasRecientes'] as List? ?? []).map((e) {
          final t = _s(e);
          return t.length > 10 ? t.substring(0, 10) : t;
        }).toSet(),
        insignias = _lista(j['insignias'], Logro.fromJson),
        retos = _lista(j['retos'], Logro.fromJson);

  final int rachaActual;
  final int mejorRacha;
  final int totalAsistencias;
  final int asistenciasEsteMes;

  /// Fechas yyyy-MM-dd en que el usuario asistió.
  final Set<String> diasRecientes;
  final List<Logro> insignias;
  final List<Logro> retos;
}

class ResumenProgreso {
  ResumenProgreso.fromJson(Json j)
      : recordsPersonales = _i(j['recordsPersonales']),
        kgGanados = _d(j['kgGanados']),
        repeticionesGanadas = _i(j['repeticionesGanadas']),
        historial = _lista(j['historial'], ProgresoEjercicio.fromJson),
        insignias = _lista(j['insignias'], Logro.fromJson),
        retos = _lista(j['retos'], Logro.fromJson);

  final int recordsPersonales;
  final double kgGanados;
  final int repeticionesGanadas;
  final List<ProgresoEjercicio> historial;
  final List<Logro> insignias;
  final List<Logro> retos;
}

class Plan {
  Plan.fromJson(Json j)
      : id = _s(j['id']),
        codigo = _s(j['codigo']),
        nombre = _s(j['nombre']),
        precio = _d(j['precio']),
        duracionMeses = _i(j['duracionMeses']),
        maxBeneficiarios = _i(j['maxBeneficiarios']),
        activo = j['activo'] == true,
        fechaActualizacion = _f(j['fechaActualizacion']);

  final String id;

  /// Mensual, Duo o Anual.
  final String codigo;
  final String nombre;
  final double precio;
  final int duracionMeses;
  final int maxBeneficiarios;
  final bool activo;
  final DateTime fechaActualizacion;
}

class Membresia {
  Membresia.fromJson(Json j)
      : id = _s(j['id']),
        planId = _s(j['planId']),
        planNombre = _s(j['planNombre']),
        estado = _s(j['estado']),
        fechaInicio = _f(j['fechaInicio']),
        fechaVencimiento = _f(j['fechaVencimiento']),
        titularNombre = _s(j['titularNombre']),
        acompananteNombre = j['acompananteNombre'] as String?;

  final String id;
  final String planId;
  final String planNombre;

  /// Activa o Vencida.
  final String estado;
  final DateTime fechaInicio;
  final DateTime fechaVencimiento;
  final String titularNombre;
  final String? acompananteNombre;

  bool get activa => estado == 'Activa';

  int get diasRestantes {
    if (!activa) return 0;
    final ms = fechaVencimiento.difference(DateTime.now()).inMilliseconds;
    return ms <= 0 ? 0 : (ms / Duration.millisecondsPerDay).ceil();
  }
}

class Pago {
  Pago.fromJson(Json j)
      : id = _s(j['id']),
        usuarioId = _s(j['usuarioId']),
        planNombre = _s(j['planNombre']),
        monto = _d(j['monto']),
        metodo = _s(j['metodo']),
        referencia = _s(j['referencia']),
        fecha = _f(j['fecha']),
        acompananteUsuarioId = j['acompananteUsuarioId'] as String?;

  final String id;
  final String usuarioId;
  final String planNombre;
  final double monto;
  final String metodo;
  final String referencia;
  final DateTime fecha;
  final String? acompananteUsuarioId;
}

class UsuarioResumen {
  UsuarioResumen({
    required this.id,
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.telefono,
  });

  UsuarioResumen.fromJson(Json j)
      : id = _s(j['id']),
        firstName = _s(j['firstName']),
        lastName = _s(j['lastName']),
        email = _s(j['email']),
        telefono = _i(j['telefono']);

  final String id;
  final String firstName;
  final String lastName;
  final String email;
  final int telefono;

  String get nombreCompleto => '$firstName $lastName';
  String get iniciales =>
      '${firstName.isEmpty ? '' : firstName[0]}${lastName.isEmpty ? '' : lastName[0]}'.toUpperCase();
}

class Entrenador {
  Entrenador({
    required this.id,
    required this.nombre,
    required this.especialidad,
    required this.horario,
    required this.email,
    required this.telefono,
  });

  Entrenador.fromJson(Json j)
      : id = _s(j['id']),
        nombre = _s(j['nombre']),
        especialidad = _s(j['especialidad']),
        horario = _s(j['horario']),
        email = _s(j['email']),
        telefono = _i(j['telefono']);

  final String id;
  final String nombre;
  final String especialidad;
  final String horario;
  final String email;
  final int telefono;
}

class Administrador {
  Administrador({
    required this.id,
    required this.nombre,
    required this.lastName,
    required this.email,
    required this.telefono,
    required this.nivelAcceso,
  });

  Administrador.fromJson(Json j)
      : id = _s(j['id']),
        nombre = _s(j['nombre']),
        lastName = _s(j['lastName']),
        email = _s(j['email']),
        telefono = _i(j['telefono']),
        nivelAcceso = _s(j['nivelAcceso']);

  final String id;
  final String nombre;
  final String lastName;
  final String email;
  final int telefono;
  final String nivelAcceso;
}

class AccesoResult {
  AccesoResult.fromJson(Json j)
      : permitido = j['permitido'] == true,
        motivo = _s(j['motivo']),
        nombre = j['nombre'] as String?,
        plan = j['plan'] as String?,
        fechaVencimiento = _fOpc(j['fechaVencimiento']),
        hora = DateTime.now();

  final bool permitido;
  final String motivo;
  final String? nombre;
  final String? plan;
  final DateTime? fechaVencimiento;

  /// Momento en que se validó en esta pantalla.
  final DateTime hora;
}
