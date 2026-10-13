import 'package:flutter/material.dart';

import '../core/api_cliente.dart';
import '../core/apis.dart';
import '../core/sesion.dart';
import '../models/modelos.dart';
import '../theme/tema.dart';
import '../ui/componentes.dart';

const _porPagina = 24;

class EjerciciosPantalla extends StatefulWidget {
  const EjerciciosPantalla({super.key});

  @override
  State<EjerciciosPantalla> createState() => _EjerciciosPantallaState();
}

class _EjerciciosPantallaState extends State<EjerciciosPantalla> {
  List<Ejercicio> _ejercicios = [];
  bool _cargando = true;
  String _error = '';
  String _aviso = '';
  String _errorForm = '';
  bool _guardando = false;

  String _busqueda = '';
  String _grupo = '';
  int _visibles = _porPagina;

  final _claveEditor = GlobalKey();
  bool _editorAbierto = false;
  String? _editandoId;
  final _nombre = TextEditingController();
  final _grupoMuscular = TextEditingController();
  final _descripcion = TextEditingController();

  bool get _puedeGestionar => sesion.tieneRol(const [Rol.entrenador, Rol.administrador]);

  @override
  void initState() {
    super.initState();
    for (final c in [_nombre, _grupoMuscular, _descripcion]) {
      c.addListener(() => setState(() {}));
    }
    WorkoutsApi.ejercicios().then((items) {
      if (!mounted) return;
      setState(() {
        _ejercicios = items;
        _cargando = false;
      });
    }).catchError((Object e) {
      if (!mounted) return;
      setState(() {
        _error = mensajeError(e, 'No se pudo cargar la biblioteca de ejercicios.');
        _cargando = false;
      });
    });
  }

  @override
  void dispose() {
    _nombre.dispose();
    _grupoMuscular.dispose();
    _descripcion.dispose();
    super.dispose();
  }

  List<String> get _grupos => _ejercicios.map((e) => e.grupoMuscular).toSet().toList()..sort();

  List<Ejercicio> get _filtrados {
    final texto = _busqueda.trim().toLowerCase();
    return _ejercicios
        .where((e) => _grupo.isEmpty || e.grupoMuscular == _grupo)
        .where((e) => texto.isEmpty || e.nombre.toLowerCase().contains(texto) || e.descripcionOrientativa.toLowerCase().contains(texto))
        .toList()
      ..sort((a, b) => a.nombre.toLowerCase().compareTo(b.nombre.toLowerCase()));
  }

  bool get _valido => _nombre.text.trim().isNotEmpty && _grupoMuscular.text.trim().isNotEmpty && _descripcion.text.trim().isNotEmpty;

  void _abrir(Ejercicio? e) {
    _editandoId = e?.id;
    _nombre.text = e?.nombre ?? '';
    _grupoMuscular.text = e?.grupoMuscular ?? '';
    _descripcion.text = e?.descripcionOrientativa ?? '';
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
      _errorForm = '';
      _guardando = true;
    });
    final datos = {
      'nombre': _nombre.text.trim(),
      'grupoMuscular': _grupoMuscular.text.trim(),
      'descripcionOrientativa': _descripcion.text.trim(),
    };
    final id = _editandoId;
    try {
      final ejercicio = id != null ? await WorkoutsApi.actualizarEjercicio(id, datos) : await WorkoutsApi.crearEjercicio(datos);
      if (!mounted) return;
      setState(() {
        _ejercicios = id != null ? [for (final e in _ejercicios) e.id == id ? ejercicio : e] : [ejercicio, ..._ejercicios];
        _guardando = false;
        _editorAbierto = false;
        _aviso = id != null ? '“${ejercicio.nombre}” actualizado.' : '“${ejercicio.nombre}” agregado a la biblioteca.';
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _guardando = false;
        _errorForm = mensajeError(e, 'No se pudo guardar el ejercicio.');
      });
    }
  }

  Future<void> _eliminar(Ejercicio e) async {
    // La API borra en cascada: el ejercicio también desaparece de las rutinas que lo usan.
    if (!await confirmar(context, '¿Eliminar “${e.nombre}”?\n\nTambién se quitará de todas las rutinas que lo incluyan.')) return;
    try {
      await WorkoutsApi.eliminarEjercicio(e.id);
      if (!mounted) return;
      setState(() {
        _ejercicios = _ejercicios.where((x) => x.id != e.id).toList();
        _aviso = '“${e.nombre}” eliminado.';
      });
    } catch (err) {
      if (mounted) setState(() => _error = mensajeError(err, 'No se pudo eliminar el ejercicio.'));
    }
  }

  @override
  Widget build(BuildContext context) {
    final filtrados = _filtrados;
    final pagina = filtrados.take(_visibles).toList();
    return Pagina(children: [
      Encabezado(
        eyebrow: 'Biblioteca',
        titulo: 'Ejercicios',
        descripcion: '${_ejercicios.length} movimientos disponibles para armar rutinas.',
        accion: _puedeGestionar ? Boton.primario('Nuevo ejercicio', icono: 'mas', onPressed: () => _abrir(null)) : null,
      ),
      if (_aviso.isNotEmpty) Alerta.ok(_aviso),
      if (_editorAbierto)
        Tarjeta(
          key: _claveEditor,
          borde: C.accent,
          sombraAcento: true,
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            CabeceraTarjeta(
              titulo: _editandoId != null ? 'Editar ejercicio' : 'Nuevo ejercicio',
              trailing: Boton.fantasma('Cancelar', icono: 'cerrar', chico: true, onPressed: () => setState(() => _editorAbierto = false)),
            ),
            Bloques(espacio: 14.4, children: [
              FormularioRejilla(completos: const {2}, children: [
                Campo(etiqueta: 'Nombre', controller: _nombre, hint: 'Press de banca'),
                Campo(etiqueta: 'Grupo muscular', controller: _grupoMuscular, hint: 'Pecho', sugerencias: _grupos),
                Campo(etiqueta: 'Descripción orientativa', controller: _descripcion, hint: 'Cómo se ejecuta y qué cuidar', lineas: 3),
              ]),
              if (_errorForm.isNotEmpty) Alerta(_errorForm),
              Align(
                alignment: Alignment.centerLeft,
                child: Boton.primario(
                  _guardando ? 'Guardando…' : (_editandoId != null ? 'Guardar cambios' : 'Agregar ejercicio'),
                  icono: 'check',
                  onPressed: !_valido || _guardando ? null : _guardar,
                ),
              ),
            ]),
          ]),
        ),
      if (_cargando)
        const Cargando()
      else if (_error.isNotEmpty)
        Alerta(_error)
      else if (_ejercicios.isEmpty)
        const EstadoVacio(titulo: 'Biblioteca vacía', mensaje: 'Todavía no hay ejercicios registrados.')
      else ...[
        Wrap(spacing: 12, runSpacing: 12, children: [
          SizedBox(
            width: 320,
            child: Busqueda(
              hint: 'Buscar ejercicio',
              onChanged: (v) => setState(() {
                _busqueda = v;
                _visibles = _porPagina;
              }),
            ),
          ),
          SizedBox(
            width: 240,
            child: Selector<String>(
              valor: _grupo,
              opciones: [('', 'Todos los grupos (${_ejercicios.length})'), for (final g in _grupos) (g, g)],
              onChanged: (v) => setState(() {
                _grupo = v;
                _visibles = _porPagina;
              }),
            ),
          ),
        ]),
        if (filtrados.isEmpty)
          const EstadoVacio(titulo: 'Sin coincidencias', mensaje: 'Prueba con otra búsqueda o grupo muscular.')
        else ...[
          Text('Mostrando ${pagina.length} de ${filtrados.length}', style: T.texto(size: 13.6, color: C.muted)),
          Rejilla(minAncho: 260, maxColumnas: 4, children: [for (final e in pagina) _tarjeta(e)]),
          if (pagina.length < filtrados.length)
            Center(child: Boton('Ver más ejercicios', onPressed: () => setState(() => _visibles += _porPagina))),
        ],
      ],
    ]);
  }

  Widget _tarjeta(Ejercicio e) {
    return Tarjeta(
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Etiqueta(e.grupoMuscular, tono: Tono.accent),
        const SizedBox(height: 10),
        Text(e.nombre, style: T.texto(size: 16.8, weight: FontWeight.w700)),
        const SizedBox(height: 6),
        Text(e.descripcionOrientativa.isEmpty ? 'Sin descripción.' : e.descripcionOrientativa, style: T.texto(size: 14, color: C.muted)),
        if (_puedeGestionar) ...[
          const Spacer(),
          const SizedBox(height: 14),
          Wrap(spacing: 8, runSpacing: 8, children: [
            Boton('Editar', icono: 'editar', chico: true, onPressed: () => _abrir(e)),
            Boton.peligro('Eliminar', icono: 'borrar', chico: true, onPressed: () => _eliminar(e)),
          ]),
        ],
      ]),
    );
  }
}
