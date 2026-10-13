import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../core/apis.dart';
import '../core/formato.dart';
import '../core/sesion.dart';
import '../models/modelos.dart';
import '../theme/tema.dart';
import '../ui/componentes.dart';
import '../ui/icono.dart';

class InicioPantalla extends StatefulWidget {
  const InicioPantalla({super.key});

  @override
  State<InicioPantalla> createState() => _InicioPantallaState();
}

class _InicioPantallaState extends State<InicioPantalla> {
  bool _cargando = true;
  Membresia? _membresia;
  List<Rutina> _rutinas = [];
  List<Ejercicio> _ejercicios = [];
  List<UsuarioResumen> _usuarios = [];
  List<Entrenador> _entrenadores = [];
  List<Pago> _pagos = [];
  List<CodigoRegistro> _codigos = [];

  Rol? get _rol => sesion.rol;

  @override
  void initState() {
    super.initState();
    _cargar();
  }

  Future<void> _cargar() async {
    switch (_rol) {
      case Rol.usuario:
        final r = await (oNulo(MembresiasApi.miMembresia()), oVacio(WorkoutsApi.rutinas())).wait;
        _membresia = r.$1;
        _rutinas = r.$2;
      case Rol.entrenador:
        final r = await (oVacio(IdentityApi.usuarios()), oVacio(WorkoutsApi.rutinas()), oVacio(WorkoutsApi.ejercicios())).wait;
        _usuarios = r.$1;
        _rutinas = r.$2;
        _ejercicios = r.$3;
      case Rol.administrador:
        final r = await (
          oVacio(IdentityApi.usuarios()),
          oVacio(IdentityApi.entrenadores()),
          oVacio(MembresiasApi.pagos()),
          oVacio(IdentityApi.codigos()),
        ).wait;
        _usuarios = r.$1;
        _entrenadores = r.$2;
        _pagos = r.$3;
        _codigos = r.$4;
      case null:
        break;
    }
    if (mounted) setState(() => _cargando = false);
  }

  String get _nombre => sesion.usuario?.email.split('@').first ?? 'atleta';

  List<Rutina> get _rutinasEditables => _rutinas.where((r) => r.permitirEdicionEntrenador).toList();

  double get _ingresosMes {
    final ahora = DateTime.now();
    return _pagos
        .where((p) => p.fecha.month == ahora.month && p.fecha.year == ahora.year)
        .fold(0, (total, p) => total + p.monto);
  }

  int get _codigosDisponibles =>
      _codigos.where((c) => c.usadoPorUsuarioId == null && c.fechaExpiracion.isAfter(DateTime.now())).length;

  String _nombreUsuario(String id) {
    for (final u in _usuarios) {
      if (u.id == id) return u.nombreCompleto;
    }
    return 'Usuario';
  }

  @override
  Widget build(BuildContext context) {
    final rol = _rol;
    return Pagina(children: [
      Encabezado(
        eyebrow: rol?.texto ?? '',
        titulo: 'Hola, $_nombre',
        descripcion: switch (rol) {
          Rol.usuario => 'Tu membresía, tu acceso y tus rutinas de un vistazo.',
          Rol.entrenador => 'Tus usuarios, sus rutinas y la biblioteca de ejercicios.',
          Rol.administrador => 'El pulso del gimnasio: personas, pagos y accesos.',
          null => null,
        },
      ),
      if (_cargando)
        const Cargando()
      else
        ...switch (rol) {
          Rol.usuario => _usuario(context),
          Rol.entrenador => _entrenador(context),
          Rol.administrador => _administrador(context),
          null => <Widget>[],
        },
    ]);
  }

  // ================= USUARIO =================
  List<Widget> _usuario(BuildContext context) {
    final m = _membresia;
    return [
      if (m != null)
        Rejilla(children: [
          Metrica(etiqueta: 'Plan', valor: m.planNombre, pie: m.acompananteNombre != null ? 'Con ${m.acompananteNombre}' : 'Individual', acento: true),
          Metrica(etiqueta: 'Estado', valor: m.estado, colorValor: m.activa ? C.ok : C.danger, pie: 'Desde ${fechaMedia(m.fechaInicio)}'),
          Metrica(etiqueta: 'Días restantes', valor: '${m.diasRestantes}', pie: 'Vence el ${fechaMedia(m.fechaVencimiento)}'),
          Metrica(etiqueta: 'Mis rutinas', valor: '${_rutinas.length}', pie: 'Creadas por ti o tu entrenador'),
        ])
      else
        Tarjeta(
          borde: C.warn.withValues(alpha: 0.35),
          gradiente: degradado(C.warnSoft),
          child: Wrap(
            alignment: WrapAlignment.spaceBetween,
            crossAxisAlignment: WrapCrossAlignment.center,
            spacing: 16,
            runSpacing: 16,
            children: [
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Todavía no tienes una membresía activa', style: T.texto(size: 17.6, weight: FontWeight.w700)),
                  const SizedBox(height: 5),
                  Text('Acércate a recepción para registrar tu pago. Revisa los planes disponibles.', style: T.texto(color: C.muted)),
                ],
              ),
              Boton('Ver planes', icono: 'flecha', onPressed: () => context.go('/planes')),
            ],
          ),
        ),
      Rejilla(minAncho: 300, maxColumnas: 2, children: [
        TarjetaAccion(icono: 'qr', titulo: 'Mi acceso', texto: 'Muestra tu código QR en la entrada. Se renueva cada minuto.', ruta: '/mi-acceso'),
        TarjetaAccion(icono: 'rutinas', titulo: 'Nueva rutina', texto: 'Arma tu sesión con series, repeticiones y descansos.', ruta: '/rutinas'),
      ]),
      if (_rutinas.isNotEmpty)
        Tarjeta(
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            CabeceraTarjeta(titulo: 'Mis rutinas', trailing: Boton('Ver todas', chico: true, onPressed: () => context.go('/rutinas'))),
            ListaSeparada(children: [
              for (final r in _rutinas.take(5)) _FilaRutina(titulo: r.nombre, subtitulo: r.descripcion, rutina: r),
            ]),
          ]),
        ),
    ];
  }

  // ================= ENTRENADOR =================
  List<Widget> _entrenador(BuildContext context) {
    final editables = _rutinasEditables;
    return [
      Rejilla(children: [
        Metrica(etiqueta: 'Usuarios', valor: '${_usuarios.length}', pie: 'Registrados en el gimnasio', acento: true),
        Metrica(etiqueta: 'Rutinas', valor: '${_rutinas.length}', pie: 'En total'),
        Metrica(etiqueta: 'Editables por ti', valor: '${editables.length}', pie: 'Con permiso del usuario'),
        Metrica(etiqueta: 'Ejercicios', valor: '${_ejercicios.length}', pie: 'En la biblioteca'),
      ]),
      Rejilla(minAncho: 280, maxColumnas: 3, children: const [
        TarjetaAccion(icono: 'escanear', titulo: 'Validar acceso', texto: 'Escanea el QR de un usuario en la entrada.', ruta: '/control-acceso'),
        TarjetaAccion(icono: 'rutinas', titulo: 'Crear rutina', texto: 'Asigna una rutina a uno de tus usuarios.', ruta: '/rutinas'),
        TarjetaAccion(icono: 'usuarios', titulo: 'Usuarios', texto: 'Consulta datos y membresía de cada usuario.', ruta: '/usuarios'),
      ]),
      if (editables.isNotEmpty)
        Tarjeta(
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            CabeceraTarjeta(
              titulo: 'Rutinas que puedes editar',
              subtitulo: 'Los usuarios te dieron permiso de ajustarlas.',
              trailing: Boton('Ir a rutinas', chico: true, onPressed: () => context.go('/rutinas')),
            ),
            ListaSeparada(children: [
              for (final r in editables.take(6)) _FilaRutina(titulo: r.nombre, subtitulo: _nombreUsuario(r.usuarioId), rutina: r),
            ]),
          ]),
        ),
    ];
  }

  // ================= ADMINISTRADOR =================
  List<Widget> _administrador(BuildContext context) {
    return [
      Rejilla(children: [
        Metrica(etiqueta: 'Ingresos del mes', valor: moneda(_ingresosMes), pie: 'Pagos registrados este mes', acento: true),
        Metrica(etiqueta: 'Usuarios', valor: '${_usuarios.length}', pie: 'Registrados'),
        Metrica(etiqueta: 'Entrenadores', valor: '${_entrenadores.length}', pie: 'En el equipo'),
        Metrica(etiqueta: 'Códigos disponibles', valor: '$_codigosDisponibles', pie: 'Sin usar y vigentes'),
      ]),
      Rejilla(minAncho: 280, maxColumnas: 3, children: const [
        TarjetaAccion(icono: 'membresias', titulo: 'Registrar pago', texto: 'Activa o renueva la membresía de un usuario.', ruta: '/membresias'),
        TarjetaAccion(icono: 'codigos', titulo: 'Generar código', texto: 'Entrega un código para que un cliente se registre.', ruta: '/codigos'),
        TarjetaAccion(icono: 'escanear', titulo: 'Validar acceso', texto: 'Escanea el QR de un usuario en la entrada.', ruta: '/control-acceso'),
      ]),
      Tarjeta(
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          CabeceraTarjeta(titulo: 'Últimos pagos', trailing: Boton('Ver todos', chico: true, onPressed: () => context.go('/membresias'))),
          if (_pagos.isEmpty)
            Text('Todavía no hay pagos registrados.', style: T.texto(color: C.muted))
          else
            Tabla(
              columnas: const [
                Columna('Fecha', flex: 3),
                Columna('Usuario', flex: 4),
                Columna('Plan', flex: 3),
                Columna('Método', flex: 3),
                Columna('Monto', flex: 3, numerica: true),
              ],
              filas: [
                for (final p in _pagos.take(6))
                  FilaTabla([
                    Text(fechaCorta(p.fecha), style: T.texto(size: 14)),
                    Text(_nombreUsuario(p.usuarioId), style: T.texto(size: 14), overflow: TextOverflow.ellipsis),
                    Etiqueta(p.planNombre, tono: Tono.accent),
                    Text(p.metodo, style: T.texto(size: 14)),
                    Text(moneda(p.monto), style: T.mono()),
                  ]),
              ],
            ),
        ]),
      ),
    ];
  }
}

class _FilaRutina extends StatelessWidget {
  const _FilaRutina({required this.titulo, required this.subtitulo, required this.rutina});

  final String titulo;
  final String subtitulo;
  final Rutina rutina;

  @override
  Widget build(BuildContext context) {
    return Row(children: [
      Expanded(child: TituloCelda(titulo, subtitulo: subtitulo)),
      const SizedBox(width: 16),
      Etiqueta(rutina.nivelDificultad),
      const SizedBox(width: 16),
      Text('${rutina.rutinaEjercicios.length} ejercicios', style: T.texto(size: 14, color: C.muted)),
    ]);
  }
}

/// Acceso rápido a otra pantalla (.action-card).
class TarjetaAccion extends StatefulWidget {
  const TarjetaAccion({super.key, required this.icono, required this.titulo, required this.texto, required this.ruta});

  final String icono;
  final String titulo;
  final String texto;
  final String ruta;

  @override
  State<TarjetaAccion> createState() => _TarjetaAccionState();
}

class _TarjetaAccionState extends State<TarjetaAccion> {
  bool _hover = false;

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
      onEnter: (_) => setState(() => _hover = true),
      onExit: (_) => setState(() => _hover = false),
      child: Tarjeta(
        onTap: () => context.go(widget.ruta),
        child: Row(children: [
          Container(
            width: 52,
            height: 52,
            alignment: Alignment.center,
            decoration: BoxDecoration(color: C.accentSoft, borderRadius: BorderRadius.circular(12)),
            child: Icono(widget.icono, size: 28, color: C.accent),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(widget.titulo, style: T.texto(size: 16, weight: FontWeight.w700)),
              const SizedBox(height: 4),
              Text(widget.texto, style: T.texto(size: 13.6, color: C.muted)),
            ]),
          ),
          const SizedBox(width: 16),
          AnimatedSlide(
            duration: const Duration(milliseconds: 150),
            offset: Offset(_hover ? 0.17 : 0, 0),
            child: Icono('flecha', color: _hover ? C.accent : C.faint),
          ),
        ]),
      ),
    );
  }
}
