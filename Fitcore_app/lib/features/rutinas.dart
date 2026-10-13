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

const _niveles = ['Principiante', 'Intermedio', 'Avanzado'];
const _maxSugerencias = 40;
const _pesoMaximoKg = 1000; // Mismo límite que la API

/// Ejercicio dentro del editor, con sus campos editables.
class _Item {
  _Item({required this.ejercicioId, required this.nombre, required this.grupoMuscular, int series = 3, int repeticiones = 10, double pesoKg = 0, int descanso = 60})
      : series = TextEditingController(text: '$series'),
        repeticiones = TextEditingController(text: '$repeticiones'),
        peso = TextEditingController(text: _textoPeso(pesoKg)),
        descanso = TextEditingController(text: '$descanso');

  final String ejercicioId;
  final String nombre;
  final String grupoMuscular;
  final TextEditingController series;
  final TextEditingController repeticiones;
  final TextEditingController peso;
  final TextEditingController descanso;

  static String _textoPeso(double kg) => kg == kg.roundToDouble() ? '${kg.round()}' : '$kg';

  int get valorSeries => int.tryParse(series.text) ?? 0;
  int get valorRepeticiones => int.tryParse(repeticiones.text) ?? 0;
  double get valorPeso => double.tryParse(peso.text.replaceAll(',', '.')) ?? 0;
  int get valorDescanso => int.tryParse(descanso.text) ?? -1;

  void dispose() {
    series.dispose();
    repeticiones.dispose();
    peso.dispose();
    descanso.dispose();
  }
}

class RutinasPantalla extends StatefulWidget {
  const RutinasPantalla({super.key});

  @override
  State<RutinasPantalla> createState() => _RutinasPantallaState();
}

class _RutinasPantallaState extends State<RutinasPantalla> {
  List<Rutina> _rutinas = [];
  List<Ejercicio> _ejercicios = [];
  List<UsuarioResumen> _usuarios = [];
  bool _cargando = true;
  bool _guardando = false;
  String _error = '';
  String _errorForm = '';
  String _aviso = '';

  /// Mejoras de repeticiones o peso de la última edición guardada.
  List<ProgresoEjercicio> _logrosGuardado = [];
  String? _expandida;

  // Filtros de la lista
  String _busqueda = '';
  String _filtroNivel = '';

  // Editor
  final _claveEditor = GlobalKey();
  bool _editorAbierto = false;
  String? _editandoId;
  final _nombre = TextEditingController();
  final _descripcion = TextEditingController();
  String _nivel = 'Intermedio';
  String _usuarioId = '';
  bool _permitirEdicion = false;
  List<_Item> _items = [];
  String _busquedaEjercicio = '';
  String _grupoEjercicio = '';

  Rol? get _rol => sesion.rol;
  String get _miId => sesion.usuario?.id ?? '';
  bool get _puedeCrear => _rol == Rol.usuario || _rol == Rol.entrenador;
  bool get _esEquipo => _rol == Rol.entrenador || _rol == Rol.administrador;

  @override
  void initState() {
    super.initState();
    _nombre.addListener(() => setState(() {}));
    _cargar();
  }

  @override
  void dispose() {
    _nombre.dispose();
    _descripcion.dispose();
    for (final i in _items) {
      i.dispose();
    }
    super.dispose();
  }

  Future<void> _cargar() async {
    final ejercicios = oVacio(WorkoutsApi.ejercicios());
    // Solo el equipo puede listar usuarios (para mostrar dueños y asignar rutinas).
    final usuarios = _esEquipo ? oVacio(IdentityApi.usuarios()) : Future.value(<UsuarioResumen>[]);
    try {
      _rutinas = await WorkoutsApi.rutinas();
    } catch (e) {
      _error = mensajeError(e, 'No se pudieron cargar las rutinas.');
    }
    _ejercicios = await ejercicios;
    _usuarios = await usuarios;
    if (mounted) setState(() => _cargando = false);
  }

  // ---------- Permisos (mismas reglas que RutinaReglas en la API) ----------
  bool _puedeModificar(Rutina r) =>
      (_rol == Rol.usuario && r.usuarioId == _miId) || (_rol == Rol.entrenador && r.permitirEdicionEntrenador);

  String _nombreUsuario(String id) {
    if (id == _miId) return 'Tú';
    for (final u in _usuarios) {
      if (u.id == id) return u.nombreCompleto;
    }
    return '';
  }

  List<Rutina> get _filtradas {
    final texto = _busqueda.trim().toLowerCase();
    return _rutinas
        .where((r) => _filtroNivel.isEmpty || r.nivelDificultad == _filtroNivel)
        .where((r) =>
            texto.isEmpty ||
            r.nombre.toLowerCase().contains(texto) ||
            r.descripcion.toLowerCase().contains(texto) ||
            _nombreUsuario(r.usuarioId).toLowerCase().contains(texto))
        .toList()
      ..sort((a, b) => b.fechaCreacion.compareTo(a.fechaCreacion));
  }

  List<String> get _grupos => _ejercicios.map((e) => e.grupoMuscular).toSet().toList()..sort();

  List<Ejercicio> get _sugerencias {
    final texto = _busquedaEjercicio.trim().toLowerCase();
    return _ejercicios
        .where((e) => _grupoEjercicio.isEmpty || e.grupoMuscular == _grupoEjercicio)
        .where((e) => texto.isEmpty || e.nombre.toLowerCase().contains(texto))
        .take(_maxSugerencias)
        .toList();
  }

  // ---------- Editor ----------
  void _reemplazarItems(List<_Item> nuevos) {
    for (final i in _items) {
      i.dispose();
    }
    _items = nuevos;
  }

  void _nueva() {
    _editandoId = null;
    _nombre.text = '';
    _descripcion.text = '';
    _nivel = 'Intermedio';
    _usuarioId = '';
    // Un entrenador que crea la rutina normalmente quiere poder ajustarla después.
    _permitirEdicion = _rol == Rol.entrenador;
    _reemplazarItems([]);
    _abrirEditor();
  }

  void _editar(Rutina r) {
    _editandoId = r.id;
    _nombre.text = r.nombre;
    _descripcion.text = r.descripcion;
    _nivel = _niveles.contains(r.nivelDificultad) ? r.nivelDificultad : 'Intermedio';
    _usuarioId = r.usuarioId;
    _permitirEdicion = r.permitirEdicionEntrenador;
    final ordenados = [...r.rutinaEjercicios]..sort((a, b) => a.ordenAparicion.compareTo(b.ordenAparicion));
    _reemplazarItems([
      for (final e in ordenados)
        _Item(
          ejercicioId: e.ejercicioId,
          nombre: e.nombre,
          grupoMuscular: e.grupoMuscular,
          series: e.series,
          repeticiones: e.repeticiones,
          pesoKg: e.pesoKg,
          descanso: e.tiempoDescansoSegundos,
        ),
    ]);
    _abrirEditor();
  }

  void _abrirEditor() {
    setState(() {
      _errorForm = '';
      _aviso = '';
      _logrosGuardado = [];
      _busquedaEjercicio = '';
      _grupoEjercicio = '';
      _editorAbierto = true;
    });
    llevarAVista(_claveEditor);
  }

  void _cerrarEditor() {
    if (!_guardando) setState(() => _editorAbierto = false);
  }

  bool _estaEnRutina(String id) => _items.any((i) => i.ejercicioId == id);

  void _alternar(Ejercicio e) {
    setState(() {
      final existente = _items.where((i) => i.ejercicioId == e.id).firstOrNull;
      if (existente != null) {
        _items = _items.where((i) => i != existente).toList();
        existente.dispose();
      } else {
        _items = [..._items, _Item(ejercicioId: e.id, nombre: e.nombre, grupoMuscular: e.grupoMuscular)];
      }
    });
  }

  void _quitar(int index) {
    setState(() {
      final quitado = _items[index];
      _items = [..._items]..removeAt(index);
      quitado.dispose();
    });
  }

  void _mover(int index, int delta) {
    final destino = index + delta;
    if (destino < 0 || destino >= _items.length) return;
    setState(() {
      final items = [..._items];
      final t = items[index];
      items[index] = items[destino];
      items[destino] = t;
      _items = items;
    });
  }

  Future<void> _guardar() async {
    setState(() => _errorForm = '');
    String? problema;
    if (_nombre.text.trim().isEmpty || _descripcion.text.trim().isEmpty) {
      problema = 'Completa el nombre y la descripción.';
    } else if (_rol == Rol.entrenador && _usuarioId.isEmpty) {
      problema = 'Elige el usuario al que le asignas la rutina.';
    } else if (_items.isEmpty) {
      problema = 'Agrega al menos un ejercicio.';
    } else if (_items.any((i) => i.valorSeries < 1 || i.valorRepeticiones < 1 || i.valorDescanso < 0)) {
      problema = 'Series y repeticiones deben ser mayores que cero y el descanso no puede ser negativo.';
    } else if (_items.any((i) => i.valorPeso < 0 || i.valorPeso > _pesoMaximoKg)) {
      problema = 'El peso debe estar entre 0 y $_pesoMaximoKg kg.';
    }
    if (problema != null) {
      setState(() => _errorForm = problema!);
      return;
    }

    final datos = {
      'nombre': _nombre.text.trim(),
      'descripcion': _descripcion.text.trim(),
      'nivelDificultad': _nivel,
      'usuarioId': _rol == Rol.usuario ? _miId : _usuarioId,
      'permitirEdicionEntrenador': _permitirEdicion,
      'rutinaEjercicios': [
        for (var i = 0; i < _items.length; i++)
          {
            'ejercicioId': _items[i].ejercicioId,
            'series': _items[i].valorSeries,
            'repeticiones': _items[i].valorRepeticiones,
            'pesoKg': _items[i].valorPeso,
            'tiempoDescansoSegundos': _items[i].valorDescanso,
            'ordenAparicion': i + 1,
          },
      ],
    };

    setState(() => _guardando = true);
    final id = _editandoId;
    try {
      final rutina = id != null ? await WorkoutsApi.actualizarRutina(id, datos) : await WorkoutsApi.crearRutina(datos);
      if (!mounted) return;
      setState(() {
        _rutinas = id != null ? [for (final r in _rutinas) r.id == rutina.id ? rutina : r] : [rutina, ..._rutinas];
        _guardando = false;
        _editorAbierto = false;
        _aviso = id != null ? 'Rutina “${rutina.nombre}” actualizada.' : 'Rutina “${rutina.nombre}” creada.';
        _logrosGuardado = rutina.progresos;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _guardando = false;
        _errorForm = mensajeError(e, 'No se pudo guardar la rutina.');
      });
    }
  }

  Future<void> _eliminar(Rutina r) async {
    if (!await confirmar(context, '¿Eliminar la rutina “${r.nombre}”? Esta acción no se puede deshacer.')) return;
    try {
      await WorkoutsApi.eliminarRutina(r.id);
      if (!mounted) return;
      setState(() {
        _rutinas = _rutinas.where((x) => x.id != r.id).toList();
        _aviso = 'Rutina “${r.nombre}” eliminada.';
      });
    } catch (e) {
      if (mounted) setState(() => _error = mensajeError(e, 'No se pudo eliminar la rutina.'));
    }
  }

  /// "+2 reps (10 → 12)" / "+5 kg (40 → 45)"; vacío si ese valor no subió.
  String _subida(num anterior, num nuevo, String unidad) {
    final diferencia = ((nuevo - anterior) * 100).round() / 100;
    return diferencia > 0 ? '+${decimal(diferencia)} $unidad (${decimal(anterior)} → ${decimal(nuevo)})' : '';
  }

  @override
  Widget build(BuildContext context) {
    return Pagina(children: [
      Encabezado(
        eyebrow: 'Entrenamiento',
        titulo: 'Rutinas',
        descripcion: switch (_rol) {
          Rol.usuario => 'Tus sesiones de entrenamiento. Puedes permitir que tu entrenador las ajuste.',
          Rol.entrenador => 'Crea rutinas para tus usuarios y edita las que te dieron permiso.',
          _ => 'Consulta todas las rutinas del gimnasio.',
        },
        accion: _puedeCrear ? Boton.primario('Nueva rutina', icono: 'mas', onPressed: _nueva) : null,
      ),
      if (_aviso.isNotEmpty) Alerta.ok(_aviso),
      if (_logrosGuardado.isNotEmpty) _avisoLogros(context),
      if (_rol == Rol.administrador)
        const Alerta.info('Los administradores pueden consultar las rutinas; las crean los usuarios y entrenadores.'),
      if (_editorAbierto) _editor(),
      ..._lista(),
    ]);
  }

  Widget _avisoLogros(BuildContext context) {
    final n = _logrosGuardado.length;
    return Tarjeta(
      borde: C.accent,
      gradiente: degradado(C.accentSoft, desde: Alignment.topLeft, hasta: Alignment.bottomRight),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(children: [
          Container(
            width: 42,
            height: 42,
            alignment: Alignment.center,
            decoration: const BoxDecoration(color: C.accent, shape: BoxShape.circle),
            child: const Icono('trofeo', size: 22, color: C.accentInk),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              const Eyebrow('¡Nuevo logro!'),
              Text('Subiste ${n == 1 ? 'un ejercicio' : '$n ejercicios'}', style: T.texto(size: 18.4, weight: FontWeight.w700)),
            ]),
          ),
          Boton.fantasma('', icono: 'cerrar', chico: true, tooltip: 'Cerrar', onPressed: () => setState(() => _logrosGuardado = [])),
        ]),
        const SizedBox(height: 14),
        for (final p in _logrosGuardado)
          Padding(
            padding: const EdgeInsets.only(bottom: 8),
            child: Wrap(spacing: 9.6, runSpacing: 6.4, crossAxisAlignment: WrapCrossAlignment.center, children: [
              Text(p.nombreEjercicio, style: T.texto(weight: FontWeight.w700)),
              if (_subida(p.repeticionesAnteriores, p.repeticionesNuevas, 'reps').isNotEmpty)
                Etiqueta(_subida(p.repeticionesAnteriores, p.repeticionesNuevas, 'reps'), tono: Tono.ok),
              if (_subida(p.pesoAnteriorKg, p.pesoNuevoKg, 'kg').isNotEmpty)
                Etiqueta(_subida(p.pesoAnteriorKg, p.pesoNuevoKg, 'kg'), tono: Tono.ok),
              if (p.esRecordPersonal) const Etiqueta('Récord personal', tono: Tono.accent),
            ]),
          ),
        if (_rol == Rol.usuario)
          MouseRegion(
            cursor: SystemMouseCursors.click,
            child: GestureDetector(
              onTap: () => context.go('/logros'),
              child: Text('Ver mis retos y logros →', style: T.texto(size: 13.6, color: C.accent)),
            ),
          ),
      ]),
    );
  }

  // ================= EDITOR =================
  Widget _editor() {
    return Tarjeta(
      key: _claveEditor,
      borde: C.accent,
      sombraAcento: true,
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Eyebrow(_editandoId != null ? 'Editar rutina' : 'Nueva rutina'),
              const SizedBox(height: 5),
              Text((_nombre.text.isEmpty ? 'Sin nombre' : _nombre.text).toUpperCase(), style: T.display(size: 29)),
            ]),
          ),
          Boton.fantasma('Cancelar', icono: 'cerrar', chico: true, onPressed: _cerrarEditor),
        ]),
        const SizedBox(height: 18),
        Dividido(
          corte: 720,
          flexPrincipal: 15,
          flexLateral: 10,
          principal: _formulario(),
          lateral: _biblioteca(),
        ),
      ]),
    );
  }

  Widget _formulario() {
    return Bloques(espacio: 14.4, children: [
      Campo(etiqueta: 'Nombre', controller: _nombre, hint: 'Fuerza tren superior'),
      Campo(etiqueta: 'Descripción', controller: _descripcion, hint: 'Objetivo de la sesión', lineas: 2),
      FormularioRejilla(children: [
        Selector<String>(
          etiqueta: 'Nivel',
          valor: _nivel,
          opciones: [for (final n in _niveles) (n, n)],
          onChanged: (v) => setState(() => _nivel = v),
        ),
        if (_rol == Rol.entrenador)
          Selector<String>(
            etiqueta: 'Usuario',
            valor: _usuarioId.isEmpty ? null : _usuarioId,
            hint: 'Elige un usuario',
            opciones: [for (final u in _usuarios) (u.id, u.nombreCompleto)],
            onChanged: _editandoId != null ? null : (v) => setState(() => _usuarioId = v),
          ),
      ]),
      Casilla(
        etiqueta: 'Permitir que los entrenadores editen esta rutina',
        valor: _permitirEdicion,
        onChanged: (v) => setState(() => _permitirEdicion = v),
      ),
      Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(color: C.bg, borderRadius: BorderRadius.circular(R.radiusSm), border: Border.all(color: C.border)),
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Row(children: [
            Text('Ejercicios de la rutina', style: T.texto(size: 14.4, weight: FontWeight.w700)),
            const SizedBox(width: 8),
            Etiqueta('${_items.length}', tono: Tono.accent),
          ]),
          const SizedBox(height: 10),
          if (_items.isEmpty)
            Text('Elige ejercicios de la biblioteca.', style: T.texto(size: 13.6, color: C.muted))
          else
            Bloques(espacio: 8, children: [
              for (var i = 0; i < _items.length; i++) _ItemEditor(
                key: ObjectKey(_items[i]),
                item: _items[i],
                orden: i + 1,
                primero: i == 0,
                ultimo: i == _items.length - 1,
                alSubir: () => _mover(i, -1),
                alBajar: () => _mover(i, 1),
                alQuitar: () => _quitar(i),
              ),
            ]),
        ]),
      ),
      if (_errorForm.isNotEmpty) Alerta(_errorForm),
      Wrap(spacing: 8, runSpacing: 8, children: [
        Boton.primario(
          _guardando ? 'Guardando…' : (_editandoId != null ? 'Guardar cambios' : 'Crear rutina'),
          icono: 'check',
          onPressed: _guardando ? null : _guardar,
        ),
        Boton.fantasma('Cancelar', onPressed: _cerrarEditor),
      ]),
    ]);
  }

  Widget _biblioteca() {
    final sugerencias = _sugerencias;
    return Bloques(espacio: 12, children: [
      Text('Biblioteca', style: T.texto(size: 14.4, weight: FontWeight.w700)),
      Busqueda(hint: 'Buscar ejercicio', onChanged: (v) => setState(() => _busquedaEjercicio = v)),
      Selector<String>(
        valor: _grupoEjercicio,
        opciones: [('', 'Todos los grupos'), for (final g in _grupos) (g, g)],
        onChanged: (v) => setState(() => _grupoEjercicio = v),
      ),
      ConstrainedBox(
        constraints: const BoxConstraints(maxHeight: 440),
        child: sugerencias.isEmpty
            ? Text('No hay ejercicios que coincidan.', style: T.texto(size: 13.6, color: C.muted))
            : ListView.separated(
                shrinkWrap: true,
                itemCount: sugerencias.length,
                separatorBuilder: (_, _) => const SizedBox(height: 5.6),
                itemBuilder: (context, i) {
                  final e = sugerencias[i];
                  return _ItemBiblioteca(ejercicio: e, elegido: _estaEnRutina(e.id), onTap: () => _alternar(e));
                },
              ),
      ),
    ]);
  }

  // ================= LISTA =================
  List<Widget> _lista() {
    if (_cargando) return const [Cargando()];
    if (_error.isNotEmpty) return [Alerta(_error)];
    if (_rutinas.isEmpty) {
      return [
        EstadoVacio(
          titulo: 'Sin rutinas',
          mensaje: _puedeCrear ? 'Usa “Nueva rutina” para crear la primera.' : 'Todavía no hay rutinas registradas.',
        ),
      ];
    }
    final filtradas = _filtradas;
    return [
      Wrap(
        alignment: WrapAlignment.spaceBetween,
        crossAxisAlignment: WrapCrossAlignment.center,
        spacing: 12,
        runSpacing: 12,
        children: [
          SizedBox(
            width: 320,
            child: Busqueda(
              hint: 'Buscar por nombre${_rol != Rol.usuario ? ' o usuario' : ''}',
              onChanged: (v) => setState(() => _busqueda = v),
            ),
          ),
          Pestanas<String>(
            opciones: [('', 'Todas'), for (final n in _niveles) (n, n)],
            seleccion: _filtroNivel,
            onChanged: (v) => setState(() => _filtroNivel = v),
          ),
        ],
      ),
      if (filtradas.isEmpty)
        const EstadoVacio(titulo: 'Sin coincidencias', mensaje: 'Prueba con otra búsqueda o nivel.')
      else
        Rejilla(minAncho: 300, maxColumnas: 3, children: [for (final r in filtradas) _tarjetaRutina(r)]),
    ];
  }

  Widget _tarjetaRutina(Rutina r) {
    final dueno = _nombreUsuario(r.usuarioId);
    final abierta = _expandida == r.id;
    final tono = switch (r.nivelDificultad) {
      'Principiante' => Tono.ok,
      'Intermedio' => Tono.warn,
      'Avanzado' => Tono.danger,
      _ => Tono.neutro,
    };
    return Tarjeta(
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Wrap(spacing: 6.4, runSpacing: 6.4, children: [
          Etiqueta(r.nivelDificultad, tono: tono),
          if (r.permitirEdicionEntrenador) const Etiqueta('Editable por entrenador', tono: Tono.info),
        ]),
        const SizedBox(height: 10),
        Text(r.nombre, style: T.texto(size: 18.4, weight: FontWeight.w700)),
        const SizedBox(height: 8),
        Text(r.descripcion, style: T.texto(size: 14, color: C.muted)),
        const SizedBox(height: 10),
        Wrap(spacing: 16, runSpacing: 6, children: [
          Text.rich(
            TextSpan(children: [
              TextSpan(text: '${r.rutinaEjercicios.length}', style: T.texto(size: 12.8, weight: FontWeight.w700)),
              const TextSpan(text: ' ejercicios'),
            ]),
            style: T.texto(size: 12.8, color: C.muted),
          ),
          if (dueno.isNotEmpty) Text(dueno, style: T.texto(size: 12.8, color: C.muted)),
          Text(fechaMedia(r.fechaActualizacion ?? r.fechaCreacion), style: T.texto(size: 12.8, color: C.muted)),
        ]),
        if (abierta) ...[
          const SizedBox(height: 8),
          Container(
            decoration: const BoxDecoration(border: Border(top: BorderSide(color: C.border))),
            child: Column(children: [
              for (final e in r.rutinaEjercicios)
                Container(
                  padding: const EdgeInsets.symmetric(vertical: 8.8),
                  decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: C.border))),
                  child: Row(children: [
                    Expanded(child: TituloCelda(e.nombre, subtitulo: e.grupoMuscular)),
                    const SizedBox(width: 12),
                    Text('${e.series}×${e.repeticiones}${e.pesoKg > 0 ? ' · ${decimal(e.pesoKg)} kg' : ''}', style: T.mono()),
                    const SizedBox(width: 12),
                    Text('${e.tiempoDescansoSegundos}s', style: T.mono(color: C.muted)),
                  ]),
                ),
            ]),
          ),
        ],
        const Spacer(),
        const SizedBox(height: 16),
        Wrap(spacing: 8, runSpacing: 8, children: [
          Boton(abierta ? 'Ocultar' : 'Ver ejercicios', chico: true, onPressed: () => setState(() => _expandida = abierta ? null : r.id)),
          if (_puedeModificar(r)) ...[
            Boton('Editar', icono: 'editar', chico: true, onPressed: () => _editar(r)),
            Boton.peligro('Eliminar', icono: 'borrar', chico: true, onPressed: () => _eliminar(r)),
          ],
        ]),
      ]),
    );
  }
}

class _ItemBiblioteca extends StatelessWidget {
  const _ItemBiblioteca({required this.ejercicio, required this.elegido, required this.onTap});

  final Ejercicio ejercicio;
  final bool elegido;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
      cursor: SystemMouseCursors.click,
      child: GestureDetector(
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 9.6),
          decoration: BoxDecoration(
            color: elegido ? C.accentSoft : C.surface2,
            borderRadius: BorderRadius.circular(R.radiusSm),
            border: Border.all(color: elegido ? C.accent : C.border),
          ),
          child: Row(children: [
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(ejercicio.nombre, style: T.texto(size: 14.5, weight: FontWeight.w600, color: elegido ? C.accent : C.text)),
                Text(ejercicio.grupoMuscular, style: T.texto(size: 13, color: elegido ? C.accent : C.muted)),
              ]),
            ),
            Icono(elegido ? 'check' : 'mas', size: 16, color: elegido ? C.accent : C.text),
          ]),
        ),
      ),
    );
  }
}

class _ItemEditor extends StatelessWidget {
  const _ItemEditor({
    super.key,
    required this.item,
    required this.orden,
    required this.primero,
    required this.ultimo,
    required this.alSubir,
    required this.alBajar,
    required this.alQuitar,
  });

  final _Item item;
  final int orden;
  final bool primero;
  final bool ultimo;
  final VoidCallback alSubir;
  final VoidCallback alBajar;
  final VoidCallback alQuitar;

  @override
  Widget build(BuildContext context) {
    final numero = Container(
      width: 28,
      height: 28,
      alignment: Alignment.center,
      decoration: const BoxDecoration(color: C.accent, shape: BoxShape.circle),
      child: Text('$orden', style: T.texto(size: 12.5, weight: FontWeight.w800, color: C.accentInk)),
    );
    final titulo = TituloCelda(item.nombre, subtitulo: item.grupoMuscular);
    final campos = [
      _Mini('Series', item.series),
      _Mini('Reps', item.repeticiones),
      _Mini('Peso (kg)', item.peso, decimal: true),
      _Mini('Descanso (s)', item.descanso),
    ];
    final acciones = Row(mainAxisSize: MainAxisSize.min, children: [
      Boton.fantasma('↑', chico: true, tooltip: 'Subir', onPressed: primero ? null : alSubir),
      Boton.fantasma('↓', chico: true, tooltip: 'Bajar', onPressed: ultimo ? null : alBajar),
      Boton.fantasma('', icono: 'cerrar', chico: true, tooltip: 'Quitar', onPressed: alQuitar),
    ]);

    return Container(
      padding: const EdgeInsets.all(9.6),
      decoration: BoxDecoration(color: C.surface, borderRadius: BorderRadius.circular(R.radiusSm), border: Border.all(color: C.border)),
      child: LayoutBuilder(builder: (context, c) {
        if (c.maxWidth < 600) {
          // Fila 1: orden + nombre · fila 2: series/reps/peso/descanso · fila 3: acciones
          return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            Row(children: [numero, const SizedBox(width: 9.6), Expanded(child: titulo)]),
            const SizedBox(height: 9.6),
            Row(children: separar(campos.map((m) => Expanded(child: m)), 9.6, horizontal: true)),
            Align(alignment: Alignment.centerRight, child: acciones),
          ]);
        }
        return Row(crossAxisAlignment: CrossAxisAlignment.end, children: [
          Padding(padding: const EdgeInsets.only(bottom: 3), child: numero),
          const SizedBox(width: 9.6),
          Expanded(child: Padding(padding: const EdgeInsets.only(bottom: 2), child: titulo)),
          for (final m in campos) ...[const SizedBox(width: 9.6), SizedBox(width: 76, child: m)],
          const SizedBox(width: 6),
          acciones,
        ]);
      }),
    );
  }
}

class _Mini extends StatelessWidget {
  const _Mini(this.etiqueta, this.controller, {this.decimal = false});

  final String etiqueta;
  final TextEditingController controller;
  final bool decimal;

  @override
  Widget build(BuildContext context) {
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, mainAxisSize: MainAxisSize.min, children: [
      Text(etiqueta, style: T.texto(size: 10.9, weight: FontWeight.w600, color: C.faint), maxLines: 1, overflow: TextOverflow.ellipsis),
      const SizedBox(height: 3),
      Campo(controller: controller, mono: true, compacto: true, soloDigitos: !decimal, decimal: decimal),
    ]);
  }
}
