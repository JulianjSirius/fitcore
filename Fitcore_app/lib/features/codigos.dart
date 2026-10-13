import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../core/api_cliente.dart';
import '../core/apis.dart';
import '../core/formato.dart';
import '../models/modelos.dart';
import '../theme/tema.dart';
import '../ui/componentes.dart';

enum _Estado { todos, disponible, usado, vencido }

extension on _Estado {
  String get texto => switch (this) {
        _Estado.todos => 'Todos',
        _Estado.disponible => 'Disponible',
        _Estado.usado => 'Usado',
        _Estado.vencido => 'Vencido',
      };
}

class CodigosPantalla extends StatefulWidget {
  const CodigosPantalla({super.key});

  @override
  State<CodigosPantalla> createState() => _CodigosPantallaState();
}

class _CodigosPantallaState extends State<CodigosPantalla> {
  List<CodigoRegistro> _codigos = [];
  List<UsuarioResumen> _usuarios = [];
  List<Administrador> _administradores = [];
  bool _cargando = true;
  bool _generando = false;
  String _error = '';
  CodigoRegistro? _ultimo;
  bool _copiado = false;
  _Estado _filtro = _Estado.todos;
  final _dias = TextEditingController(text: '7');

  @override
  void initState() {
    super.initState();
    _dias.addListener(() => setState(() {}));
    IdentityApi.codigos().then((items) {
      if (!mounted) return;
      setState(() {
        _codigos = items;
        _cargando = false;
      });
    }).catchError((Object e) {
      if (!mounted) return;
      setState(() {
        _error = mensajeError(e, 'No se pudieron cargar los códigos.');
        _cargando = false;
      });
    });
    oVacio(IdentityApi.usuarios()).then((u) {
      if (mounted) setState(() => _usuarios = u);
    });
    oVacio(IdentityApi.administradores()).then((a) {
      if (mounted) setState(() => _administradores = a);
    });
  }

  @override
  void dispose() {
    _dias.dispose();
    super.dispose();
  }

  int get _diasVigencia => int.tryParse(_dias.text) ?? 0;
  bool get _diasValidos => _diasVigencia >= 1 && _diasVigencia <= 90;

  _Estado _estado(CodigoRegistro c) {
    if (c.usadoPorUsuarioId != null) return _Estado.usado;
    return c.fechaExpiracion.isAfter(DateTime.now()) ? _Estado.disponible : _Estado.vencido;
  }

  Future<void> _generar() async {
    if (!_diasValidos || _generando) return;
    setState(() {
      _error = '';
      _copiado = false;
      _generando = true;
    });
    try {
      final c = await IdentityApi.generarCodigo(_diasVigencia);
      if (!mounted) return;
      setState(() {
        _ultimo = c;
        _codigos = [c, ..._codigos];
        _generando = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _generando = false;
        _error = mensajeError(e, 'No se pudo generar el código.');
      });
    }
  }

  Future<void> _copiar(String codigo) async {
    await Clipboard.setData(ClipboardData(text: codigo));
    if (!mounted) return;
    setState(() => _copiado = true);
    Timer(const Duration(seconds: 2), () {
      if (mounted) setState(() => _copiado = false);
    });
  }

  String _nombreUsuario(String? id) {
    for (final u in _usuarios) {
      if (u.id == id) return u.nombreCompleto;
    }
    return '';
  }

  String _nombreAdmin(String id) {
    for (final a in _administradores) {
      if (a.id == id) return '${a.nombre} ${a.lastName}';
    }
    return '';
  }

  @override
  Widget build(BuildContext context) {
    final conteo = {for (final e in _Estado.values) e: 0};
    for (final c in _codigos) {
      conteo[_estado(c)] = conteo[_estado(c)]! + 1;
    }
    conteo[_Estado.todos] = _codigos.length;
    final filtrados = _codigos.where((c) => _filtro == _Estado.todos || _estado(c) == _filtro).toList()
      ..sort((a, b) => b.fechaCreacion.compareTo(a.fechaCreacion));

    return Pagina(children: [
      const Encabezado(
        eyebrow: 'Administración',
        titulo: 'Códigos de registro',
        descripcion: 'Cada código permite crear una sola cuenta de usuario. Entrégalo al cliente para que se registre.',
      ),
      Dividido(
        flexPrincipal: 10,
        flexLateral: 24,
        corte: 820,
        principal: Bloques(children: [
          Tarjeta(
            child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
              const CabeceraTarjeta(titulo: 'Generar código'),
              Bloques(espacio: 14.4, children: [
                Campo(etiqueta: 'Vigencia (días)', controller: _dias, soloDigitos: true, maxLength: 2),
                Wrap(spacing: 8, children: [
                  for (final d in [1, 7, 30])
                    Boton(
                      '$d ${plural(d, 'día', 'días')}',
                      chico: true,
                      variante: _diasVigencia == d ? Variante.primario : Variante.normal,
                      onPressed: () => _dias.text = '$d',
                    ),
                ]),
                Boton.primario(
                  _generando ? 'Generando…' : 'Generar código',
                  icono: 'mas',
                  bloque: true,
                  onPressed: _generando || !_diasValidos ? null : _generar,
                ),
                if (_error.isNotEmpty) Alerta(_error),
              ]),
            ]),
          ),
          if (_ultimo != null)
            Tarjeta(
              borde: C.accent,
              gradiente: halo(const Color(0x24C8FF3D)),
              child: Column(children: [
                const Eyebrow('Código nuevo'),
                const SizedBox(height: 8),
                SelectableText(_ultimo!.codigo, style: T.mono(size: 30, weight: FontWeight.w500).copyWith(letterSpacing: 4)),
                const SizedBox(height: 6),
                Text('Vence el ${fechaHora(_ultimo!.fechaExpiracion)}', style: T.texto(size: 13, color: C.muted)),
                const SizedBox(height: 14),
                Boton(
                  _copiado ? 'Copiado' : 'Copiar código',
                  icono: _copiado ? 'check' : 'copiar',
                  onPressed: () => _copiar(_ultimo!.codigo),
                ),
              ]),
            ),
        ]),
        lateral: Tarjeta(
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            Pestanas<_Estado>(
              opciones: [for (final e in _Estado.values) (e, '${e.texto}  ${conteo[e]}')],
              seleccion: _filtro,
              onChanged: (e) => setState(() => _filtro = e),
            ),
            const SizedBox(height: 16),
            if (_cargando)
              const Cargando()
            else if (filtrados.isEmpty)
              const EstadoVacio(titulo: 'Sin códigos', mensaje: 'No hay códigos con este estado.')
            else
              Tabla(
                anchoMinimo: 640,
                columnas: const [
                  Columna('Código', flex: 4),
                  Columna('Estado', flex: 3),
                  Columna('Creado', flex: 4),
                  Columna('Vence', flex: 4),
                  Columna('Usado por', flex: 4),
                ],
                filas: [
                  for (final c in filtrados)
                    FilaTabla([
                      Tooltip(
                        message: 'Copiar',
                        child: MouseRegion(
                          cursor: SystemMouseCursors.click,
                          child: GestureDetector(
                            onTap: () => _copiar(c.codigo),
                            child: Text(c.codigo, style: T.mono(color: C.accent)),
                          ),
                        ),
                      ),
                      Etiqueta(
                        _estado(c).texto,
                        tono: switch (_estado(c)) {
                          _Estado.disponible => Tono.ok,
                          _Estado.usado => Tono.info,
                          _ => Tono.danger,
                        },
                      ),
                      TituloCelda(fechaCorta(c.fechaCreacion), subtitulo: _nombreAdmin(c.creadoPorAdministradorId)),
                      Text(fechaCorta(c.fechaExpiracion), style: T.texto(size: 14)),
                      c.usadoPorUsuarioId != null
                          ? TituloCelda(
                              _nombreUsuario(c.usadoPorUsuarioId).isEmpty ? 'Usuario' : _nombreUsuario(c.usadoPorUsuarioId),
                              subtitulo: c.fechaUso == null ? null : fechaCorta(c.fechaUso!),
                            )
                          : Text('—', style: T.texto(color: C.muted)),
                    ]),
                ],
              ),
          ]),
        ),
      ),
    ]);
  }
}
