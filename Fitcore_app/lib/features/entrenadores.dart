import 'package:flutter/material.dart';

import '../core/api_cliente.dart';
import '../core/apis.dart';
import '../core/sesion.dart';
import '../models/modelos.dart';
import '../theme/tema.dart';
import '../ui/componentes.dart';

class EntrenadoresPantalla extends StatefulWidget {
  const EntrenadoresPantalla({super.key});

  @override
  State<EntrenadoresPantalla> createState() => _EntrenadoresPantallaState();
}

class _EntrenadoresPantallaState extends State<EntrenadoresPantalla> {
  List<Entrenador> _entrenadores = [];
  bool _cargando = true;
  String _error = '';
  String _aviso = '';
  String _busqueda = '';

  final _claveEditor = GlobalKey();
  bool _editorAbierto = false;
  bool _guardando = false;
  String _errorForm = '';
  String? _editandoId;
  final _nombre = TextEditingController();
  final _especialidad = TextEditingController();
  final _horario = TextEditingController();
  final _telefono = TextEditingController();
  final _email = TextEditingController();
  final _contrasena = TextEditingController();

  List<TextEditingController> get _campos => [_nombre, _especialidad, _horario, _telefono, _email, _contrasena];

  String? get _miId => sesion.usuario?.id;
  bool get _esAdmin => sesion.rol == Rol.administrador;

  @override
  void initState() {
    super.initState();
    for (final c in _campos) {
      c.addListener(() => setState(() {}));
    }
    IdentityApi.entrenadores().then((items) {
      if (!mounted) return;
      setState(() {
        _entrenadores = items;
        _cargando = false;
      });
    }).catchError((Object e) {
      if (!mounted) return;
      setState(() {
        _error = mensajeError(e, 'No se pudo cargar el equipo de entrenadores.');
        _cargando = false;
      });
    });
  }

  @override
  void dispose() {
    for (final c in _campos) {
      c.dispose();
    }
    super.dispose();
  }

  List<Entrenador> get _filtrados {
    final texto = _busqueda.trim().toLowerCase();
    return _entrenadores
        .where((e) =>
            texto.isEmpty ||
            e.nombre.toLowerCase().contains(texto) ||
            e.especialidad.toLowerCase().contains(texto) ||
            e.email.toLowerCase().contains(texto))
        .toList()
      ..sort((a, b) => a.nombre.toLowerCase().compareTo(b.nombre.toLowerCase()));
  }

  bool get _valido {
    final tel = int.tryParse(_telefono.text) ?? 0;
    return _nombre.text.trim().isNotEmpty &&
        _especialidad.text.trim().isNotEmpty &&
        _horario.text.trim().isNotEmpty &&
        tel >= 1000000000 &&
        tel <= 9999999999 &&
        _email.text.contains('@') &&
        _contrasena.text.length >= 6;
  }

  void _abrir(Entrenador? e) {
    _editandoId = e?.id;
    _nombre.text = e?.nombre ?? '';
    _especialidad.text = e?.especialidad ?? '';
    _horario.text = e?.horario ?? '';
    _telefono.text = e == null ? '' : '${e.telefono}';
    _email.text = e?.email ?? '';
    _contrasena.text = '';
    setState(() {
      _errorForm = '';
      _aviso = '';
      _editorAbierto = true;
    });
    llevarAVista(_claveEditor);
  }

  Future<void> _guardar() async {
    if (!_valido) return;
    setState(() {
      _guardando = true;
      _errorForm = '';
    });
    final datos = {
      'nombre': _nombre.text.trim(),
      'especialidad': _especialidad.text.trim(),
      'horario': _horario.text.trim(),
      'email': _email.text.trim(),
      'contrasena': _contrasena.text,
      'telefono': int.parse(_telefono.text),
    };
    final id = _editandoId;
    try {
      String mensaje;
      if (id != null) {
        await IdentityApi.actualizarEntrenador(id, datos);
        final actualizado = Entrenador(
          id: id,
          nombre: datos['nombre'] as String,
          especialidad: datos['especialidad'] as String,
          horario: datos['horario'] as String,
          email: datos['email'] as String,
          telefono: datos['telefono'] as int,
        );
        _entrenadores = [for (final e in _entrenadores) e.id == id ? actualizado : e];
        mensaje = 'Entrenador ${actualizado.nombre} actualizado.';
      } else {
        final creado = await IdentityApi.crearEntrenador(datos);
        _entrenadores = [creado, ..._entrenadores];
        mensaje = 'Entrenador ${creado.nombre} creado. Ya puede iniciar sesión con su correo.';
      }
      if (!mounted) return;
      setState(() {
        _guardando = false;
        _editorAbierto = false;
        _aviso = mensaje;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _guardando = false;
        _errorForm = mensajeError(e, id != null ? 'No se pudo actualizar el entrenador.' : 'No se pudo crear el entrenador.');
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final filtrados = _filtrados;
    return Pagina(children: [
      Encabezado(
        eyebrow: 'Equipo',
        titulo: 'Entrenadores',
        descripcion: '${_entrenadores.length} entrenadores en el gimnasio.',
        accion: _esAdmin ? Boton.primario('Nuevo entrenador', icono: 'mas', onPressed: () => _abrir(null)) : null,
      ),
      if (_aviso.isNotEmpty) Alerta.ok(_aviso),
      if (_editorAbierto) _editor(),
      if (_cargando)
        const Cargando()
      else if (_error.isNotEmpty)
        Alerta(_error)
      else if (_entrenadores.isEmpty)
        const EstadoVacio(titulo: 'Sin entrenadores', mensaje: 'Todavía no hay entrenadores registrados.')
      else ...[
        Align(
          alignment: Alignment.centerLeft,
          child: SizedBox(
            width: 380,
            child: Busqueda(hint: 'Buscar por nombre, especialidad o correo', onChanged: (v) => setState(() => _busqueda = v)),
          ),
        ),
        if (filtrados.isEmpty)
          const EstadoVacio(titulo: 'Sin coincidencias', mensaje: 'Prueba con otra búsqueda.')
        else
          Rejilla(minAncho: 280, maxColumnas: 3, children: [for (final e in filtrados) _tarjeta(e)]),
      ],
    ]);
  }

  Widget _editor() {
    final editando = _editandoId != null;
    return Tarjeta(
      key: _claveEditor,
      borde: C.accent,
      sombraAcento: true,
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        CabeceraTarjeta(
          titulo: editando ? 'Editar entrenador' : 'Nuevo entrenador',
          subtitulo: editando ? 'Solo el Dueño puede editar entrenadores.' : 'Podrá iniciar sesión con este correo y contraseña.',
          trailing: Boton.fantasma('Cancelar', icono: 'cerrar', chico: true, onPressed: () => setState(() => _editorAbierto = false)),
        ),
        Bloques(espacio: 14.4, children: [
          FormularioRejilla(children: [
            Campo(etiqueta: 'Nombre completo', controller: _nombre),
            Campo(etiqueta: 'Especialidad', controller: _especialidad, hint: 'Fuerza, funcional, yoga…'),
            Campo(etiqueta: 'Horario', controller: _horario, hint: 'Lunes a viernes 6:00–12:00'),
            Campo(etiqueta: 'Teléfono', controller: _telefono, soloDigitos: true, maxLength: 10),
            Campo(etiqueta: 'Correo', controller: _email, teclado: TextInputType.emailAddress),
            Campo(etiqueta: editando ? 'Nueva contraseña' : 'Contraseña inicial', controller: _contrasena, oculto: true),
          ]),
          if (editando)
            Text(
              'La API reemplaza la contraseña en cada actualización: comunícale la nueva al entrenador.',
              style: T.texto(size: 13.6, color: C.muted),
            ),
          if (_errorForm.isNotEmpty) Alerta(_errorForm),
          Align(
            alignment: Alignment.centerLeft,
            child: Boton.primario(
              _guardando ? 'Guardando…' : (editando ? 'Guardar cambios' : 'Crear entrenador'),
              icono: 'check',
              onPressed: !_valido || _guardando ? null : _guardar,
            ),
          ),
        ]),
      ]),
    );
  }

  Widget _tarjeta(Entrenador e) {
    final soyYo = e.id == _miId;
    return Tarjeta(
      borde: soyYo ? C.accent : null,
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(children: [
          Avatar(e.nombre.isEmpty ? '?' : e.nombre[0].toUpperCase()),
          const SizedBox(width: 12),
          Expanded(child: TituloCelda(e.nombre, subtitulo: e.email)),
          if (soyYo) const Etiqueta('Tú', tono: Tono.accent),
        ]),
        const SizedBox(height: 12),
        Etiqueta(e.especialidad, tono: Tono.info),
        const SizedBox(height: 6),
        Detalles([('Horario', e.horario), ('Teléfono', Text('${e.telefono}', style: T.mono(weight: FontWeight.w600)))]),
        if (sesion.esDueno) ...[
          const Spacer(),
          const SizedBox(height: 8),
          Boton('Editar', icono: 'editar', chico: true, onPressed: () => _abrir(e)),
        ],
      ]),
    );
  }
}
