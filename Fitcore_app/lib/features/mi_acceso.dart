import 'dart:async';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:qr_flutter/qr_flutter.dart';

import '../core/api_cliente.dart';
import '../core/apis.dart';
import '../core/formato.dart';
import '../models/modelos.dart';
import '../theme/tema.dart';
import '../ui/componentes.dart';

// El token dura 60 s en el servidor; se pide uno nuevo cuando quedan 10 s.
const _vidaTokenSegundos = 60;
const _renovarConSegundos = 10;

class MiAccesoPantalla extends StatefulWidget {
  const MiAccesoPantalla({super.key});

  @override
  State<MiAccesoPantalla> createState() => _MiAccesoPantallaState();
}

class _MiAccesoPantallaState extends State<MiAccesoPantalla> {
  bool _cargando = true;
  Membresia? _membresia;
  String _sinMembresia = '';
  String _token = '';
  int _segundos = 0;
  String _errorQr = '';
  Timer? _intervalo;
  bool _pidiendo = false;
  late final AppLifecycleListener _ciclo;

  bool get _activa => _membresia?.activa ?? false;

  @override
  void initState() {
    super.initState();
    // Con la pestaña oculta no se piden tokens; al volver se genera uno nuevo.
    _ciclo = AppLifecycleListener(
      onShow: () {
        if (_activa) _pedirToken();
      },
      onHide: _detener,
    );
    _cargar();
  }

  @override
  void dispose() {
    _ciclo.dispose();
    _detener();
    super.dispose();
  }

  Future<void> _cargar() async {
    try {
      final m = await MembresiasApi.miMembresia();
      if (!mounted) return;
      setState(() {
        _membresia = m;
        _cargando = false;
        if (!m.activa) _sinMembresia = 'Tu membresía está vencida. Acércate a recepción para renovarla.';
      });
      if (m.activa) _pedirToken();
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _cargando = false;
        _sinMembresia = e is ApiException && e.estado == 404
            ? 'Todavía no tienes una membresía. Acércate a recepción para activarla.'
            : 'No pudimos consultar tu membresía. Inténtalo de nuevo en un momento.';
      });
    }
  }

  Future<void> _pedirToken() async {
    if (_pidiendo) return;
    _pidiendo = true;
    setState(() => _errorQr = '');
    try {
      final token = await MembresiasApi.tokenAccesoQr();
      _pidiendo = false;
      if (!mounted) return;
      setState(() => _token = token);
      _iniciarCuentaRegresiva();
    } catch (e) {
      _pidiendo = false;
      _detener();
      if (!mounted) return;
      setState(() {
        _token = '';
        _errorQr = mensajeError(e, 'No se pudo generar tu código de acceso. Toca "Generar código".');
      });
    }
  }

  void _iniciarCuentaRegresiva() {
    _detener();
    setState(() => _segundos = _vidaTokenSegundos);
    _intervalo = Timer.periodic(const Duration(seconds: 1), (_) {
      if (!mounted) return;
      final restantes = _segundos - 1;
      setState(() => _segundos = restantes < 0 ? 0 : restantes);
      if (restantes <= _renovarConSegundos) _pedirToken();
    });
  }

  void _detener() {
    _intervalo?.cancel();
    _intervalo = null;
  }

  @override
  Widget build(BuildContext context) {
    return Pagina(children: [
      const Encabezado(
        eyebrow: 'Mi acceso',
        titulo: 'Tu pase de entrada',
        descripcion: 'Muestra este código en recepción. Se renueva solo cada minuto.',
      ),
      if (_cargando)
        const Cargando()
      else
        LayoutBuilder(builder: (context, c) {
          final qr = _tarjetaQr(context);
          final m = _membresia;
          if (m == null) return qr;
          final datos = _tarjetaMembresia(m);
          if (c.maxWidth < 760) return Bloques(children: [qr, datos]);
          return Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Expanded(child: qr),
            const SizedBox(width: 16),
            Expanded(child: datos),
          ]);
        }),
    ]);
  }

  Widget _tarjetaQr(BuildContext context) {
    final Widget contenido;
    if (_token.isNotEmpty) {
      final bajo = _segundos <= 15;
      contenido = Column(children: [
        Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(18),
            boxShadow: const [BoxShadow(color: C.accentSoft, spreadRadius: 6)],
          ),
          child: QrImageView(
            data: _token,
            size: 260,
            padding: EdgeInsets.zero,
            errorCorrectionLevel: QrErrorCorrectLevel.M,
            backgroundColor: Colors.white,
            eyeStyle: const QrEyeStyle(eyeShape: QrEyeShape.square, color: C.bg),
            dataModuleStyle: const QrDataModuleStyle(dataModuleShape: QrDataModuleShape.square, color: C.bg),
            semanticsLabel: 'Código QR de acceso',
          ),
        ),
        const SizedBox(height: 16),
        ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 280),
          child: TweenAnimationBuilder<double>(
            tween: Tween(end: _segundos / _vidaTokenSegundos),
            duration: const Duration(seconds: 1),
            builder: (context, v, _) => BarraProgreso(v, alto: 6, color: bajo ? C.warn : C.accent),
          ),
        ),
        const SizedBox(height: 16),
        Text('Válido por $_segundos s', style: T.mono(size: 13.6, color: C.muted)),
      ]);
    } else if (_errorQr.isNotEmpty) {
      contenido = Column(children: [
        Alerta(_errorQr),
        if (_activa) ...[const SizedBox(height: 16), Boton.primario('Generar código', onPressed: _pedirToken)],
      ]);
    } else if (_sinMembresia.isNotEmpty) {
      contenido = Column(children: [
        Container(
          width: 64,
          height: 64,
          alignment: Alignment.center,
          decoration: const BoxDecoration(color: C.dangerSoft, shape: BoxShape.circle),
          child: Text('✕', style: T.texto(size: 24, weight: FontWeight.w700, color: C.danger)),
        ),
        const SizedBox(height: 12),
        ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 320),
          child: Text(_sinMembresia, textAlign: TextAlign.center, style: T.texto(color: C.muted)),
        ),
        const SizedBox(height: 12),
        Boton('Ver planes', onPressed: () => context.go('/planes')),
      ]);
    } else {
      contenido = const Cargando();
    }
    return Tarjeta(
      padding: const EdgeInsets.symmetric(vertical: 32, horizontal: 24),
      gradiente: halo(const Color(0x1FC8FF3D), centro: Alignment.topCenter, radio: 1.2),
      child: Center(child: contenido),
    );
  }

  Widget _tarjetaMembresia(Membresia m) {
    return Tarjeta(
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        CabeceraTarjeta(titulo: 'Tu membresía', trailing: Etiqueta(m.estado, tono: m.activa ? Tono.ok : Tono.danger)),
        Detalles([
          ('Plan', m.planNombre),
          ('Inicio', fechaLarga(m.fechaInicio)),
          ('Vence', fechaLarga(m.fechaVencimiento)),
          ('Titular', m.titularNombre),
          if (m.acompananteNombre != null) ('Acompañante (Duo)', m.acompananteNombre!),
        ]),
        const SizedBox(height: 16),
        Text(
          'Para renovar, registra tu pago en recepción antes de la fecha de vencimiento.',
          style: T.texto(size: 13.1, color: C.muted),
        ),
      ]),
    );
  }
}
