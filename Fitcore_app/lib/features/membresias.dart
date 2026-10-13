import 'package:flutter/material.dart';

import '../core/api_cliente.dart';
import '../core/apis.dart';
import '../core/formato.dart';
import '../models/modelos.dart';
import '../theme/tema.dart';
import '../ui/componentes.dart';
import '../ui/icono.dart';

const _metodos = [('Efectivo', 'Efectivo'), ('Transferencia', 'Transferencia'), ('Datafono', 'Datáfono')];

class MembresiasPantalla extends StatefulWidget {
  const MembresiasPantalla({super.key, this.usuarioInicial});

  /// Llegada desde la ficha de un usuario: `/membresias?usuario=<id>`
  final String? usuarioInicial;

  @override
  State<MembresiasPantalla> createState() => _MembresiasPantallaState();
}

class _MembresiasPantallaState extends State<MembresiasPantalla> {
  List<Plan> _planes = [];
  List<UsuarioResumen> _usuarios = [];
  List<Pago> _pagos = [];
  bool _cargando = true;
  String _error = '';

  // Formulario de pago
  String _usuarioId = '';
  String _planId = '';
  String _metodo = 'Efectivo';
  final _referencia = TextEditingController();
  final _acompanante = TextEditingController();
  String _filtroUsuarioForm = '';
  bool _registrando = false;
  String _errorPago = '';
  Membresia? _confirmacion;
  Membresia? _membresiaActual;

  // Historial
  String _filtroUsuario = '';
  String _filtroMetodo = '';

  @override
  void initState() {
    super.initState();
    _acompanante.addListener(() => setState(() {}));
    MembresiasApi.planes().then((p) {
      if (mounted) setState(() => _planes = p);
    }).catchError((Object e) {
      if (mounted) setState(() => _error = mensajeError(e, 'No se pudieron cargar los planes.'));
    });
    IdentityApi.usuarios().then((u) {
      if (!mounted) return;
      setState(() => _usuarios = u);
      final pre = widget.usuarioInicial;
      if (pre != null && u.any((x) => x.id == pre)) _alCambiarUsuario(pre);
    }).catchError((Object e) {
      if (mounted) setState(() => _error = mensajeError(e, 'No se pudo cargar la lista de usuarios.'));
    });
    _cargarPagos();
  }

  @override
  void dispose() {
    _referencia.dispose();
    _acompanante.dispose();
    super.dispose();
  }

  Future<void> _cargarPagos() async {
    try {
      final p = await MembresiasApi.pagos();
      if (mounted) setState(() => _pagos = p);
    } catch (e) {
      if (mounted) setState(() => _error = mensajeError(e, 'No se pudo cargar el historial de pagos.'));
    }
    if (mounted) setState(() => _cargando = false);
  }

  List<Plan> get _planesActivos => _planes.where((p) => p.activo).toList();
  Plan? get _plan => _planes.where((p) => p.id == _planId).firstOrNull;
  bool get _esDuo => (_plan?.maxBeneficiarios ?? 1) > 1;

  List<UsuarioResumen> get _usuariosParaElegir {
    final texto = _filtroUsuarioForm.trim().toLowerCase();
    return _usuarios
        .where((u) => texto.isEmpty || u.id == _usuarioId || '${u.nombreCompleto} ${u.email}'.toLowerCase().contains(texto))
        .toList()
      ..sort((a, b) => a.nombreCompleto.toLowerCase().compareTo(b.nombreCompleto.toLowerCase()));
  }

  List<Pago> get _pagosFiltrados => _pagos
      .where((p) => _filtroUsuario.isEmpty || p.usuarioId == _filtroUsuario)
      .where((p) => _filtroMetodo.isEmpty || p.metodo == _filtroMetodo)
      .toList()
    ..sort((a, b) => b.fecha.compareTo(a.fecha));

  bool get _valido => _usuarioId.isNotEmpty && _planId.isNotEmpty && (!_esDuo || _acompanante.text.contains('@'));

  Future<void> _alCambiarUsuario(String id) async {
    setState(() {
      _usuarioId = id;
      _membresiaActual = null;
      _confirmacion = null;
    });
    final m = await oNulo(MembresiasApi.membresiaDeUsuario(id));
    if (mounted && _usuarioId == id) setState(() => _membresiaActual = m);
  }

  DateTime _sumarMeses(DateTime f, int meses) => DateTime(f.year, f.month + meses, f.day, f.hour, f.minute);

  // Qué pasará con este pago, siguiendo las reglas de MembresiaService.
  String get _resumenPago {
    final plan = _plan;
    if (plan == null) return '';
    final actual = _membresiaActual;
    final vigente = actual != null && actual.activa && actual.fechaVencimiento.isAfter(DateTime.now());
    if (vigente && actual.planId != plan.id) {
      return 'El usuario tiene ${actual.planNombre} vigente: solo puede renovar ese plan hasta que venza.';
    }
    if (vigente) {
      return 'Renovación: la membresía pasará a vencer el ${fechaLarga(_sumarMeses(actual.fechaVencimiento, plan.duracionMeses))}.';
    }
    return 'Nueva membresía ${plan.nombre} hasta el ${fechaLarga(_sumarMeses(DateTime.now(), plan.duracionMeses))}.';
  }

  Future<void> _registrar() async {
    if (!_valido || _registrando) return;
    setState(() {
      _errorPago = '';
      _confirmacion = null;
      _registrando = true;
    });
    final referencia = _referencia.text.trim();
    try {
      final m = await MembresiasApi.registrarPago({
        'usuarioId': _usuarioId,
        'planId': _planId,
        'metodo': _metodo,
        if (referencia.isNotEmpty) 'referencia': referencia,
        if (_esDuo) 'acompananteEmail': _acompanante.text.trim(),
      });
      if (!mounted) return;
      _referencia.clear();
      _acompanante.clear();
      setState(() {
        _registrando = false;
        _confirmacion = m;
        _membresiaActual = m;
        _planId = '';
        _metodo = 'Efectivo';
      });
      _cargarPagos();
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _registrando = false;
        _errorPago = mensajeError(e, 'No se pudo registrar el pago.');
      });
    }
  }

  String _nombreUsuario(String? id) {
    for (final u in _usuarios) {
      if (u.id == id) return u.nombreCompleto;
    }
    return '—';
  }

  @override
  Widget build(BuildContext context) {
    return Pagina(children: [
      const Encabezado(
        eyebrow: 'Administración',
        titulo: 'Membresías y pagos',
        descripcion: 'Registra pagos en efectivo, transferencia o datáfono para activar o renovar membresías.',
      ),
      if (_error.isNotEmpty) Alerta(_error),
      Dividido(flexPrincipal: 10, flexLateral: 10, principal: _formulario(), lateral: _historial()),
    ]);
  }

  // ================= REGISTRAR PAGO =================
  Widget _formulario() {
    final m = _membresiaActual;
    final c = _confirmacion;
    final plan = _plan;
    final usuarios = _usuariosParaElegir;
    return Tarjeta(
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        const CabeceraTarjeta(titulo: 'Registrar pago', subtitulo: 'El pago queda aprobado y la membresía se activa al instante.'),
        Bloques(espacio: 14.4, children: [
          Campo(etiqueta: 'Buscar usuario', hint: 'Nombre o correo', onChanged: (v) => setState(() => _filtroUsuarioForm = v)),
          Selector<String>(
            etiqueta: 'Usuario',
            valor: _usuarioId.isEmpty ? null : _usuarioId,
            hint: 'Selecciona un usuario (${usuarios.length})',
            opciones: [for (final u in usuarios) (u.id, '${u.nombreCompleto} · ${u.email}')],
            onChanged: _alCambiarUsuario,
          ),
          if (m != null)
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
              decoration: BoxDecoration(color: C.surface2, borderRadius: BorderRadius.circular(R.radiusSm), border: Border.all(color: C.border)),
              child: Row(children: [
                Etiqueta(m.estado, tono: m.activa ? Tono.ok : Tono.danger),
                const SizedBox(width: 10),
                Expanded(child: Text('${m.planNombre} · vence ${fechaMedia(m.fechaVencimiento)}', style: T.texto(size: 14))),
              ]),
            ),
          ConEtiqueta(
            etiqueta: 'Plan',
            child: Bloques(espacio: 8, children: [
              for (final p in _planesActivos) _OpcionPlan(plan: p, elegido: _planId == p.id, onTap: () => setState(() => _planId = p.id)),
            ]),
          ),
          if (_esDuo)
            Campo(
              etiqueta: 'Correo del acompañante',
              controller: _acompanante,
              hint: 'Debe estar registrado en FitCore',
              teclado: TextInputType.emailAddress,
            ),
          FormularioRejilla(children: [
            Selector<String>(etiqueta: 'Método', valor: _metodo, opciones: _metodos, onChanged: (v) => setState(() => _metodo = v)),
            Campo(etiqueta: 'Referencia (opcional)', controller: _referencia, hint: 'N.º de comprobante', maxLength: 64),
          ]),
          if (_resumenPago.isNotEmpty) Alerta.info(_resumenPago),
          if (_errorPago.isNotEmpty) Alerta(_errorPago),
          if (c != null)
            Alerta.ok(
              'Pago registrado. ${c.titularNombre} tiene ${c.planNombre} activo hasta el ${fechaLarga(c.fechaVencimiento)}'
              '${c.acompananteNombre != null ? ', junto con ${c.acompananteNombre}' : ''}.',
            ),
          Boton.primario(
            plan == null ? 'Registrar pago' : (_registrando ? 'Registrando…' : 'Registrar ${moneda(plan.precio)}'),
            bloque: true,
            onPressed: _valido && !_registrando ? _registrar : null,
          ),
        ]),
      ]),
    );
  }

  // ================= HISTORIAL =================
  Widget _historial() {
    final pagos = _pagosFiltrados;
    final total = pagos.fold<double>(0, (t, p) => t + p.monto);
    return Tarjeta(
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        CabeceraTarjeta(titulo: 'Historial de pagos', subtitulo: '${pagos.length} pagos · ${moneda(total)}'),
        FormularioRejilla(children: [
          Selector<String>(
            valor: _filtroUsuario,
            opciones: [('', 'Todos los usuarios'), for (final u in _usuarios) (u.id, u.nombreCompleto)],
            onChanged: (v) => setState(() => _filtroUsuario = v),
          ),
          Selector<String>(
            valor: _filtroMetodo,
            opciones: [('', 'Todos los métodos'), ..._metodos],
            onChanged: (v) => setState(() => _filtroMetodo = v),
          ),
        ]),
        const SizedBox(height: 12),
        if (_cargando)
          const Cargando()
        else if (pagos.isEmpty)
          const EstadoVacio(titulo: 'Sin pagos', mensaje: 'No hay pagos con esos filtros.')
        else
          ListaSeparada(children: [
            for (final p in pagos)
              Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Container(
                  width: 34,
                  height: 34,
                  alignment: Alignment.center,
                  decoration: BoxDecoration(color: C.accentSoft, borderRadius: BorderRadius.circular(9)),
                  child: const Icono('membresias', size: 16, color: C.accent),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Text(_nombreUsuario(p.usuarioId), style: T.texto(size: 14.5, weight: FontWeight.w600)),
                    Text(
                      '${p.planNombre} · ${p.metodo} · ${fechaCorta(p.fecha)}'
                      '${p.acompananteUsuarioId != null ? ' · con ${_nombreUsuario(p.acompananteUsuarioId)}' : ''}',
                      style: T.texto(size: 13, color: C.muted),
                    ),
                    if (p.referencia.isNotEmpty) Text(p.referencia, style: T.mono(size: 12, color: C.muted)),
                  ]),
                ),
                const SizedBox(width: 12),
                Text(moneda(p.monto), style: T.mono(size: 14, weight: FontWeight.w700)),
              ]),
          ]),
      ]),
    );
  }
}

class _OpcionPlan extends StatelessWidget {
  const _OpcionPlan({required this.plan, required this.elegido, required this.onTap});

  final Plan plan;
  final bool elegido;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final p = plan;
    return MouseRegion(
      cursor: SystemMouseCursors.click,
      child: GestureDetector(
        onTap: onTap,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 150),
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
          decoration: BoxDecoration(
            color: elegido ? C.accentSoft : C.bg,
            borderRadius: BorderRadius.circular(R.radiusSm),
            border: Border.all(color: elegido ? C.accent : C.border),
          ),
          child: Row(children: [
            Container(
              width: 18,
              height: 18,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                border: Border.all(color: elegido ? C.accent : C.borderStrong, width: elegido ? 5 : 1.5),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(p.nombre.toUpperCase(), style: T.rotulo(size: 12, color: elegido ? C.accent : C.muted, espaciado: 0.1)),
                Text(
                  '${p.duracionMeses} ${plural(p.duracionMeses, 'mes', 'meses')}${p.maxBeneficiarios > 1 ? ' · ${p.maxBeneficiarios} personas' : ''}',
                  style: T.texto(size: 13, color: C.faint),
                ),
              ]),
            ),
            Text(moneda(p.precio), style: T.display(size: 22, weight: FontWeight.w700)),
          ]),
        ),
      ),
    );
  }
}
