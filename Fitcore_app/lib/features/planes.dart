import 'package:flutter/material.dart';

import '../core/api_cliente.dart';
import '../core/apis.dart';
import '../core/formato.dart';
import '../core/sesion.dart';
import '../models/modelos.dart';
import '../theme/tema.dart';
import '../ui/componentes.dart';
import '../ui/icono.dart';

const _beneficios = <String, List<String>>{
  'Mensual': ['Acceso ilimitado con QR', 'Rutinas personalizadas', 'Asesoría de entrenadores'],
  'Duo': ['Todo lo del plan Mensual', 'Para dos personas', 'Un solo pago'],
  'Anual': ['Todo lo del plan Mensual', '12 meses de acceso', 'El mejor precio por mes'],
};

class PlanesPantalla extends StatefulWidget {
  const PlanesPantalla({super.key});

  @override
  State<PlanesPantalla> createState() => _PlanesPantallaState();
}

class _PlanesPantallaState extends State<PlanesPantalla> {
  List<Plan> _planes = [];
  bool _cargando = true;
  String _error = '';
  String _aviso = '';
  String? _editando;
  bool _guardando = false;

  final _nombre = TextEditingController();
  final _precio = TextEditingController();
  final _duracion = TextEditingController();
  bool _activo = true;

  bool get _esAdmin => sesion.rol == Rol.administrador;
  List<Plan> get _visibles => _esAdmin ? _planes : _planes.where((p) => p.activo).toList();

  @override
  void initState() {
    super.initState();
    MembresiasApi.planes().then((p) {
      if (!mounted) return;
      setState(() {
        _planes = p;
        _cargando = false;
      });
    }).catchError((Object e) {
      if (!mounted) return;
      setState(() {
        _error = mensajeError(e, 'No se pudieron cargar los planes.');
        _cargando = false;
      });
    });
  }

  @override
  void dispose() {
    _nombre.dispose();
    _precio.dispose();
    _duracion.dispose();
    super.dispose();
  }

  int _precioMensual(Plan p) =>
      (p.precio / p.duracionMeses / (p.maxBeneficiarios == 0 ? 1 : p.maxBeneficiarios)).round();

  void _editar(Plan p) {
    _nombre.text = p.nombre;
    _precio.text = p.precio.round().toString();
    _duracion.text = '${p.duracionMeses}';
    setState(() {
      _activo = p.activo;
      _aviso = '';
      _error = '';
      _editando = p.id;
    });
  }

  Future<void> _guardar(Plan p) async {
    final precio = int.tryParse(_precio.text) ?? 0;
    final duracion = int.tryParse(_duracion.text) ?? 0;
    if (_nombre.text.trim().isEmpty || precio < 1 || duracion < 1 || duracion > 24) {
      setState(() => _error = 'Revisa el nombre, el precio (mayor que 0) y la duración (1 a 24 meses).');
      return;
    }
    setState(() => _guardando = true);
    try {
      final actualizado = await MembresiasApi.actualizarPlan(p.id, {
        'nombre': _nombre.text.trim(),
        'precio': precio,
        'duracionMeses': duracion,
        'activo': _activo,
      });
      if (!mounted) return;
      setState(() {
        _planes = [for (final x in _planes) x.id == actualizado.id ? actualizado : x];
        _guardando = false;
        _editando = null;
        _error = '';
        _aviso = 'Plan ${actualizado.nombre} actualizado. Aplica a los pagos nuevos.';
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _guardando = false;
        _error = mensajeError(e, 'No se pudo guardar el plan.');
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Pagina(children: [
      Encabezado(
        eyebrow: 'Membresías',
        titulo: 'Planes',
        descripcion: _esAdmin
            ? 'Ajusta precios, duración y disponibilidad. Los cambios aplican a los pagos nuevos; las membresías ya pagadas no cambian.'
            : 'Elige tu plan y registra el pago en recepción.',
      ),
      if (_aviso.isNotEmpty) Alerta.ok(_aviso),
      if (_error.isNotEmpty) Alerta(_error),
      if (_cargando)
        const Cargando()
      else
        Rejilla(minAncho: 260, maxColumnas: 3, children: [for (final p in _visibles) _tarjeta(p)]),
    ]);
  }

  Widget _tarjeta(Plan p) {
    final destacado = p.codigo == 'Anual';
    return Tarjeta(
      padding: const EdgeInsets.all(25.6),
      borde: destacado ? C.accent : null,
      gradiente: destacado ? halo(const Color(0x24C8FF3D)) : null,
      opacidad: p.activo ? 1 : 0.6,
      child: _editando == p.id ? _formulario(p) : _vista(p),
    );
  }

  Widget _formulario(Plan p) {
    return Bloques(espacio: 14.4, children: [
      Campo(etiqueta: 'Nombre', controller: _nombre),
      Campo(etiqueta: 'Precio (COP)', controller: _precio, soloDigitos: true),
      Campo(etiqueta: 'Duración (meses)', controller: _duracion, soloDigitos: true, maxLength: 2),
      Casilla(etiqueta: 'Disponible para nuevos pagos', valor: _activo, onChanged: (v) => setState(() => _activo = v)),
      Wrap(spacing: 8, children: [
        Boton.primario(_guardando ? 'Guardando…' : 'Guardar', chico: true, onPressed: _guardando ? null : () => _guardar(p)),
        Boton.fantasma('Cancelar', chico: true, onPressed: () => setState(() => _editando = null)),
      ]),
    ]);
  }

  Widget _vista(Plan p) {
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Wrap(spacing: 8, runSpacing: 6, crossAxisAlignment: WrapCrossAlignment.center, children: [
        Text(p.nombre.toUpperCase(), style: T.rotulo(size: 12.8, color: C.muted, espaciado: 0.12)),
        if (p.codigo == 'Anual') const Etiqueta('Mejor precio', tono: Tono.accent),
        if (!p.activo) const Etiqueta('Inactivo', tono: Tono.danger),
      ]),
      const SizedBox(height: 14),
      Text(moneda(p.precio), style: T.display(size: 45)),
      const SizedBox(height: 8),
      Text(
        '${p.duracionMeses} ${plural(p.duracionMeses, 'mes', 'meses')} · '
        '${p.maxBeneficiarios} ${plural(p.maxBeneficiarios, 'persona', 'personas')}',
        style: T.texto(color: C.muted),
      ),
      if (p.duracionMeses > 1 || p.maxBeneficiarios > 1) ...[
        const SizedBox(height: 6),
        Text('≈ ${moneda(_precioMensual(p))} por persona al mes', style: T.texto(size: 13.1, weight: FontWeight.w600, color: C.accent)),
      ],
      const SizedBox(height: 13),
      Container(
        width: double.infinity,
        padding: const EdgeInsets.only(top: 16),
        decoration: const BoxDecoration(border: Border(top: BorderSide(color: C.border))),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          for (final b in _beneficios[p.codigo] ?? const <String>[])
            Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: Row(children: [
                const Icono('check', size: 15, color: C.accent),
                const SizedBox(width: 8),
                Expanded(child: Text(b, style: T.texto(size: 14.4))),
              ]),
            ),
        ]),
      ),
      if (_esAdmin) ...[
        const Spacer(),
        const SizedBox(height: 16),
        Row(children: [
          Expanded(child: Text('Actualizado ${fechaMedia(p.fechaActualizacion)}', style: T.texto(size: 13, color: C.muted))),
          Boton('Editar', icono: 'editar', chico: true, onPressed: () => _editar(p)),
        ]),
      ],
    ]);
  }
}
