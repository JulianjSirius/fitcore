import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../core/api_cliente.dart';
import '../core/apis.dart';
import '../core/formato.dart';
import '../core/sesion.dart';
import '../models/modelos.dart';
import '../theme/tema.dart';
import '../ui/componentes.dart';
import '../ui/icono.dart';

class UsuariosPantalla extends StatefulWidget {
  const UsuariosPantalla({super.key});

  @override
  State<UsuariosPantalla> createState() => _UsuariosPantallaState();
}

class _UsuariosPantallaState extends State<UsuariosPantalla> {
  List<UsuarioResumen> _usuarios = [];
  List<Rutina> _rutinas = [];
  bool _cargando = true;
  String _error = '';
  String _busqueda = '';

  final _claveDetalle = GlobalKey();
  UsuarioResumen? _seleccionado;
  Membresia? _membresia;
  bool _cargandoDetalle = false;
  bool _sinMembresia = false;
  List<Pago> _pagos = [];

  bool _editando = false;
  bool _guardando = false;
  String _mensaje = '';
  String _errorEdicion = '';
  final _nombre = TextEditingController();
  final _apellido = TextEditingController();
  final _email = TextEditingController();
  final _telefono = TextEditingController();
  final _contrasena = TextEditingController();

  List<TextEditingController> get _campos => [_nombre, _apellido, _email, _telefono, _contrasena];

  bool get _esAdmin => sesion.rol == Rol.administrador;

  @override
  void initState() {
    super.initState();
    for (final c in _campos) {
      c.addListener(() => setState(() {}));
    }
    IdentityApi.usuarios().then((items) {
      if (!mounted) return;
      setState(() {
        _usuarios = items;
        _cargando = false;
      });
    }).catchError((Object e) {
      if (!mounted) return;
      setState(() {
        _error = mensajeError(e, 'No se pudo cargar la lista de usuarios.');
        _cargando = false;
      });
    });
    oVacio(WorkoutsApi.rutinas()).then((r) {
      if (mounted) setState(() => _rutinas = r);
    });
  }

  @override
  void dispose() {
    for (final c in _campos) {
      c.dispose();
    }
    super.dispose();
  }

  List<UsuarioResumen> get _filtrados {
    final texto = _busqueda.trim().toLowerCase();
    return _usuarios
        .where((u) =>
            texto.isEmpty ||
            u.nombreCompleto.toLowerCase().contains(texto) ||
            u.email.toLowerCase().contains(texto) ||
            '${u.telefono}'.contains(texto))
        .toList()
      ..sort((a, b) => a.nombreCompleto.toLowerCase().compareTo(b.nombreCompleto.toLowerCase()));
  }

  List<Rutina> get _rutinasDelUsuario {
    final id = _seleccionado?.id;
    return id == null ? [] : _rutinas.where((r) => r.usuarioId == id).toList();
  }

  Future<void> _seleccionar(UsuarioResumen u) async {
    setState(() {
      _seleccionado = u;
      _editando = false;
      _mensaje = '';
      _membresia = null;
      _sinMembresia = false;
      _pagos = [];
      _cargandoDetalle = true;
    });
    llevarAVista(_claveDetalle);

    if (_esAdmin) {
      oVacio(MembresiasApi.pagos(u.id)).then((p) {
        if (mounted && _seleccionado?.id == u.id) setState(() => _pagos = p);
      });
    }
    try {
      final m = await MembresiasApi.membresiaDeUsuario(u.id);
      if (mounted && _seleccionado?.id == u.id) setState(() => _membresia = m);
    } catch (e) {
      if (mounted && _seleccionado?.id == u.id) setState(() => _sinMembresia = e is ApiException && e.estado == 404);
    }
    if (mounted && _seleccionado?.id == u.id) setState(() => _cargandoDetalle = false);
  }

  void _empezarEdicion() {
    final u = _seleccionado;
    if (u == null) return;
    _nombre.text = u.firstName;
    _apellido.text = u.lastName;
    _email.text = u.email;
    _telefono.text = '${u.telefono}';
    _contrasena.text = '';
    setState(() {
      _errorEdicion = '';
      _mensaje = '';
      _editando = true;
    });
  }

  bool get _edicionValida {
    final tel = int.tryParse(_telefono.text) ?? 0;
    return _nombre.text.trim().isNotEmpty &&
        _apellido.text.trim().isNotEmpty &&
        _email.text.contains('@') &&
        tel >= 1000000000 &&
        tel <= 9999999999 &&
        _contrasena.text.length >= 6;
  }

  Future<void> _guardar() async {
    final u = _seleccionado;
    if (u == null || !_edicionValida) return;
    setState(() {
      _guardando = true;
      _errorEdicion = '';
    });
    final actualizado = UsuarioResumen(
      id: u.id,
      firstName: _nombre.text.trim(),
      lastName: _apellido.text.trim(),
      email: _email.text.trim(),
      telefono: int.parse(_telefono.text),
    );
    try {
      await IdentityApi.actualizarUsuario(u.id, {
        'firstName': actualizado.firstName,
        'lastName': actualizado.lastName,
        'email': actualizado.email,
        'telefono': actualizado.telefono,
        'contrasena': _contrasena.text,
      });
      if (!mounted) return;
      setState(() {
        _usuarios = [for (final x in _usuarios) x.id == u.id ? actualizado : x];
        _seleccionado = actualizado;
        _guardando = false;
        _editando = false;
        _mensaje = 'Datos del usuario actualizados.';
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _guardando = false;
        _errorEdicion = mensajeError(e, 'No se pudo actualizar el usuario.');
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Pagina(children: [
      Encabezado(
        eyebrow: 'Gimnasio',
        titulo: 'Usuarios',
        descripcion: '${_usuarios.length} usuarios registrados. Elige uno para ver su membresía y sus rutinas.',
        accion: _esAdmin ? Boton('Generar código de registro', icono: 'codigos', onPressed: () => context.go('/codigos')) : null,
      ),
      if (_cargando)
        const Cargando()
      else if (_error.isNotEmpty)
        Alerta(_error)
      else
        Dividido(principal: _lista(), lateral: KeyedSubtree(key: _claveDetalle, child: _detalle(context))),
    ]);
  }

  Widget _lista() {
    final filtrados = _filtrados;
    return Tarjeta(
      child: Bloques(children: [
        Busqueda(hint: 'Buscar por nombre, correo o teléfono', onChanged: (v) => setState(() => _busqueda = v)),
        if (filtrados.isEmpty)
          const EstadoVacio(titulo: 'Sin usuarios', mensaje: 'No hay usuarios que coincidan con la búsqueda.')
        else
          Tabla(
            anchoMinimo: 460,
            columnas: const [Columna('Usuario', flex: 6), Columna('Teléfono', flex: 3), Columna('', flex: 2, numerica: true)],
            filas: [
              for (final u in filtrados)
                FilaTabla(
                  seleccionada: _seleccionado?.id == u.id,
                  onTap: () => _seleccionar(u),
                  [
                    Row(children: [
                      Avatar(u.iniciales),
                      const SizedBox(width: 10),
                      Expanded(child: TituloCelda(u.nombreCompleto, subtitulo: u.email)),
                    ]),
                    Text('${u.telefono}', style: T.mono()),
                    Boton('Ver', chico: true, onPressed: () => _seleccionar(u)),
                  ],
                ),
            ],
          ),
      ]),
    );
  }

  Widget _detalle(BuildContext context) {
    final u = _seleccionado;
    if (u == null) {
      return Tarjeta(
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 40),
          child: Column(children: [
            const Icono('usuarios', size: 32, color: C.faint),
            const SizedBox(height: 10),
            Text('Selecciona un usuario para ver su detalle.', style: T.texto(color: C.muted), textAlign: TextAlign.center),
          ]),
        ),
      );
    }
    final m = _membresia;
    final rutinas = _rutinasDelUsuario;

    Widget titulo(String t) => Padding(
          padding: const EdgeInsets.only(top: 22, bottom: 10),
          child: Text(t.toUpperCase(), style: T.rotulo(size: 11.5, color: C.muted, espaciado: 0.1)),
        );

    return Tarjeta(
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Avatar(u.iniciales, size: 52),
          const SizedBox(width: 12),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(u.nombreCompleto, style: T.texto(size: 17.6, weight: FontWeight.w700)),
              Text(u.email, style: T.texto(size: 13, color: C.muted), overflow: TextOverflow.ellipsis),
            ]),
          ),
          Boton.fantasma('', icono: 'cerrar', chico: true, tooltip: 'Cerrar', onPressed: () => setState(() => _seleccionado = null)),
        ]),
        const SizedBox(height: 16),
        if (_mensaje.isNotEmpty) ...[Alerta.ok(_mensaje), const SizedBox(height: 12)],
        if (_editando)
          Bloques(espacio: 14.4, children: [
            FormularioRejilla(completos: const {2, 3, 4}, children: [
              Campo(etiqueta: 'Nombre', controller: _nombre),
              Campo(etiqueta: 'Apellido', controller: _apellido),
              Campo(etiqueta: 'Correo', controller: _email, teclado: TextInputType.emailAddress),
              Campo(etiqueta: 'Teléfono', controller: _telefono, soloDigitos: true, maxLength: 10),
              Campo(etiqueta: 'Nueva contraseña', controller: _contrasena, oculto: true),
            ]),
            Text(
              'La API reemplaza la contraseña en cada actualización: comunícale la nueva al usuario.',
              style: T.texto(size: 13.6, color: C.muted),
            ),
            if (_errorEdicion.isNotEmpty) Alerta(_errorEdicion),
            Wrap(spacing: 8, children: [
              Boton.primario(_guardando ? 'Guardando…' : 'Guardar', onPressed: !_edicionValida || _guardando ? null : _guardar),
              Boton.fantasma('Cancelar', onPressed: () => setState(() => _editando = false)),
            ]),
          ])
        else ...[
          Detalles([
            ('Teléfono', Text('${u.telefono}', style: T.mono(weight: FontWeight.w600))),
            ('Rutinas', '${rutinas.length}'),
          ]),
          if (sesion.esDueno) ...[
            const SizedBox(height: 10),
            Align(alignment: Alignment.centerLeft, child: Boton('Editar datos', icono: 'editar', chico: true, onPressed: _empezarEdicion)),
          ],
        ],
        titulo('Membresía'),
        if (_cargandoDetalle)
          const Cargando()
        else if (m != null)
          Container(
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              color: C.surface2,
              borderRadius: BorderRadius.circular(R.radiusSm),
              border: Border.all(color: m.activa ? C.border : C.danger.withValues(alpha: 0.35)),
            ),
            child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
              Row(children: [
                Etiqueta(m.estado, tono: m.activa ? Tono.ok : Tono.danger),
                const SizedBox(width: 10),
                Expanded(child: Text(m.planNombre, style: T.texto(weight: FontWeight.w700))),
              ]),
              const SizedBox(height: 6),
              Detalles([
                ('Vence', fechaMedia(m.fechaVencimiento)),
                if (m.activa) ('Días restantes', '${m.diasRestantes}'),
                ('Titular', m.titularNombre),
                if (m.acompananteNombre != null) ('Acompañante', m.acompananteNombre!),
              ]),
            ]),
          )
        else if (_sinMembresia)
          Text('Este usuario todavía no tiene membresía.', style: T.texto(size: 13.6, color: C.muted))
        else
          const Alerta('No se pudo consultar la membresía.'),
        if (_esAdmin) ...[
          const SizedBox(height: 12),
          Align(
            alignment: Alignment.centerLeft,
            child: Boton(
              'Registrar pago',
              icono: 'membresias',
              chico: true,
              onPressed: () => context.go(Uri(path: '/membresias', queryParameters: {'usuario': u.id}).toString()),
            ),
          ),
        ],
        if (rutinas.isNotEmpty) ...[
          titulo('Rutinas'),
          ListaSeparada(espacio: 8, children: [
            for (final r in rutinas)
              Row(children: [
                Expanded(child: Text(r.nombre, style: T.texto(size: 14.5), overflow: TextOverflow.ellipsis)),
                Etiqueta(r.nivelDificultad),
              ]),
          ]),
        ],
        if (_esAdmin && _pagos.isNotEmpty) ...[
          titulo('Pagos'),
          ListaSeparada(espacio: 8, children: [
            for (final p in _pagos)
              Row(children: [
                Expanded(child: Text('${fechaMedia(p.fecha)} · ${p.planNombre}', style: T.texto(size: 14.5), overflow: TextOverflow.ellipsis)),
                Text(moneda(p.monto), style: T.mono()),
              ]),
          ]),
        ],
      ]),
    );
  }
}
