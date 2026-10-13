import 'package:flutter/material.dart';

import '../core/api_cliente.dart';
import '../core/apis.dart';
import '../core/sesion.dart';
import '../models/modelos.dart';
import '../theme/tema.dart';
import '../ui/componentes.dart';

const _niveles = ['Recepcionista', 'Contador', 'Administrador', 'Dueño'];

class AdministradoresPantalla extends StatefulWidget {
  const AdministradoresPantalla({super.key});

  @override
  State<AdministradoresPantalla> createState() => _AdministradoresPantallaState();
}

class _AdministradoresPantallaState extends State<AdministradoresPantalla> {
  List<Administrador> _administradores = [];
  bool _cargando = true;
  String _error = '';
  String _aviso = '';

  final _claveEditor = GlobalKey();
  bool _editorAbierto = false;
  bool _guardando = false;
  String _errorForm = '';
  String? _editandoId;
  final _nombre = TextEditingController();
  final _apellido = TextEditingController();
  final _email = TextEditingController();
  final _telefono = TextEditingController();
  final _contrasena = TextEditingController();
  String _nivel = '';

  List<TextEditingController> get _campos => [_nombre, _apellido, _email, _telefono, _contrasena];

  String? get _miId => sesion.usuario?.id;
  bool get _esDueno => sesion.esDueno;

  @override
  void initState() {
    super.initState();
    for (final c in _campos) {
      c.addListener(() => setState(() {}));
    }
    IdentityApi.administradores().then((items) {
      if (!mounted) return;
      setState(() {
        _administradores = items;
        _cargando = false;
      });
    }).catchError((Object e) {
      if (!mounted) return;
      setState(() {
        _error = mensajeError(e, 'No se pudo cargar la lista de administradores.');
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

  List<Administrador> get _ordenados => [..._administradores]
    ..sort((a, b) {
      final dueno = (b.nivelAcceso == 'Dueño' ? 1 : 0) - (a.nivelAcceso == 'Dueño' ? 1 : 0);
      return dueno != 0 ? dueno : a.nombre.toLowerCase().compareTo(b.nombre.toLowerCase());
    });

  bool get _valido {
    final tel = int.tryParse(_telefono.text) ?? 0;
    return _nombre.text.trim().isNotEmpty &&
        _apellido.text.trim().isNotEmpty &&
        _email.text.contains('@') &&
        tel >= 1000000000 &&
        tel <= 9999999999 &&
        _nivel.isNotEmpty &&
        _contrasena.text.length >= 6;
  }

  void _abrir(Administrador? a) {
    _editandoId = a?.id;
    _nombre.text = a?.nombre ?? '';
    _apellido.text = a?.lastName ?? '';
    _email.text = a?.email ?? '';
    _telefono.text = a == null ? '' : '${a.telefono}';
    _contrasena.text = '';
    setState(() {
      _nivel = a?.nivelAcceso ?? '';
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
      'lastName': _apellido.text.trim(),
      'email': _email.text.trim(),
      'contrasena': _contrasena.text,
      'telefono': int.parse(_telefono.text),
      'nivelAcceso': _nivel,
    };
    final id = _editandoId;
    try {
      String mensaje;
      if (id != null) {
        await IdentityApi.actualizarAdministrador(id, datos);
        final actualizado = Administrador(
          id: id,
          nombre: datos['nombre'] as String,
          lastName: datos['lastName'] as String,
          email: datos['email'] as String,
          telefono: datos['telefono'] as int,
          nivelAcceso: _nivel,
        );
        _administradores = [for (final a in _administradores) a.id == id ? actualizado : a];
        mensaje = 'Administrador ${actualizado.nombre} actualizado.';
      } else {
        final creado = await IdentityApi.crearAdministrador(datos);
        _administradores = [..._administradores, creado];
        mensaje = 'Administrador ${creado.nombre} creado.';
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
        _errorForm = mensajeError(e, id != null ? 'No se pudo actualizar el administrador.' : 'No se pudo crear el administrador.');
      });
    }
  }

  Future<void> _eliminar(Administrador a) async {
    if (a.id == _miId) return;
    if (!await confirmar(context, '¿Eliminar a ${a.nombre} ${a.lastName}? Perderá el acceso de inmediato.')) return;
    try {
      await IdentityApi.eliminarAdministrador(a.id);
      if (!mounted) return;
      setState(() {
        _administradores = _administradores.where((x) => x.id != a.id).toList();
        _aviso = 'Administrador ${a.nombre} eliminado.';
      });
    } catch (e) {
      if (mounted) setState(() => _error = mensajeError(e, 'No se pudo eliminar el administrador.'));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Pagina(children: [
      Encabezado(
        eyebrow: 'Administración',
        titulo: 'Administradores',
        descripcion: _esDueno
            ? 'Como Dueño puedes crear, editar y eliminar administradores.'
            : 'Solo el Dueño puede crear, editar o eliminar administradores.',
        accion: _esDueno ? Boton.primario('Nuevo administrador', icono: 'mas', onPressed: () => _abrir(null)) : null,
      ),
      if (_aviso.isNotEmpty) Alerta.ok(_aviso),
      if (_error.isNotEmpty) Alerta(_error),
      if (_editorAbierto) _editor(),
      if (_cargando)
        const Cargando()
      else if (_administradores.isEmpty)
        const EstadoVacio(titulo: 'Sin administradores', mensaje: 'No hay administradores registrados.')
      else
        Tarjeta(
          child: Tabla(
            anchoMinimo: 620,
            columnas: [
              const Columna('Administrador', flex: 6),
              const Columna('Nivel', flex: 3),
              const Columna('Teléfono', flex: 3),
              if (_esDueno) const Columna('', flex: 3, numerica: true),
            ],
            filas: [
              for (final a in _ordenados)
                FilaTabla([
                  TituloCelda(
                    '${a.nombre} ${a.lastName}',
                    subtitulo: a.email,
                    trailing: a.id == _miId ? const Etiqueta('Tú', tono: Tono.accent) : null,
                  ),
                  Etiqueta(a.nivelAcceso, tono: a.nivelAcceso == 'Dueño' ? Tono.accent : Tono.neutro),
                  Text('${a.telefono}', style: T.mono()),
                  if (_esDueno)
                    Wrap(spacing: 6, children: [
                      Boton('Editar', icono: 'editar', chico: true, onPressed: () => _abrir(a)),
                      Boton.peligro(
                        '',
                        icono: 'borrar',
                        chico: true,
                        tooltip: a.id == _miId ? 'No puedes eliminar tu propia cuenta' : 'Eliminar',
                        onPressed: a.id == _miId ? null : () => _eliminar(a),
                      ),
                    ]),
                ]),
            ],
          ),
        ),
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
          titulo: editando ? 'Editar administrador' : 'Nuevo administrador',
          trailing: Boton.fantasma('Cancelar', icono: 'cerrar', chico: true, onPressed: () => setState(() => _editorAbierto = false)),
        ),
        Bloques(espacio: 14.4, children: [
          FormularioRejilla(children: [
            Campo(etiqueta: 'Nombre', controller: _nombre),
            Campo(etiqueta: 'Apellido', controller: _apellido),
            Campo(etiqueta: 'Correo', controller: _email, teclado: TextInputType.emailAddress),
            Campo(etiqueta: 'Teléfono', controller: _telefono, soloDigitos: true, maxLength: 10),
            Selector<String>(
              etiqueta: 'Nivel de acceso',
              valor: _nivel.isEmpty ? null : _nivel,
              hint: 'Selecciona un nivel',
              opciones: [for (final n in _niveles) (n, n)],
              onChanged: (v) => setState(() => _nivel = v),
            ),
            Campo(etiqueta: editando ? 'Nueva contraseña' : 'Contraseña inicial', controller: _contrasena, oculto: true),
          ]),
          Text(
            'El nivel “Dueño” da control total (incluido administrar a otros administradores).'
            '${editando ? ' La API reemplaza la contraseña en cada actualización.' : ''}',
            style: T.texto(size: 13.6, color: C.muted),
          ),
          if (_errorForm.isNotEmpty) Alerta(_errorForm),
          Align(
            alignment: Alignment.centerLeft,
            child: Boton.primario(
              _guardando ? 'Guardando…' : (editando ? 'Guardar cambios' : 'Crear administrador'),
              icono: 'check',
              onPressed: !_valido || _guardando ? null : _guardar,
            ),
          ),
        ]),
      ]),
    );
  }
}
