import 'dart:async';

import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../core/api_cliente.dart';
import '../core/apis.dart';
import '../core/formato.dart';
import '../models/modelos.dart';
import '../theme/tema.dart';
import '../ui/componentes.dart';
import '../ui/icono.dart';

const _pausaTrasLectura = Duration(seconds: 3);

class ControlAccesoPantalla extends StatefulWidget {
  const ControlAccesoPantalla({super.key});

  @override
  State<ControlAccesoPantalla> createState() => _ControlAccesoPantallaState();
}

class _ControlAccesoPantallaState extends State<ControlAccesoPantalla> {
  final _tokenManual = TextEditingController();
  MobileScannerController? _camara;
  bool _validando = false;
  AccesoResult? _resultado;
  String _error = '';
  List<AccesoResult> _historial = [];
  bool _pausado = false;

  @override
  void initState() {
    super.initState();
    _tokenManual.addListener(() => setState(() {}));
  }

  @override
  void dispose() {
    _camara?.dispose();
    _tokenManual.dispose();
    super.dispose();
  }

  void _iniciarCamara() {
    setState(() {
      _camara = MobileScannerController(
        formats: const [BarcodeFormat.qrCode],
        facing: CameraFacing.back,
      );
    });
  }

  void _detenerCamara() {
    final c = _camara;
    setState(() => _camara = null);
    c?.dispose();
  }

  void _alDetectar(BarcodeCapture captura) {
    if (_pausado) return;
    final valor = captura.barcodes.isEmpty ? null : captura.barcodes.first.rawValue;
    if (valor == null || valor.isEmpty) return;
    _pausado = true;
    _validar(valor);
    Timer(_pausaTrasLectura, () => _pausado = false);
  }

  void _validarManual() {
    final token = _tokenManual.text.trim();
    if (token.isNotEmpty && !_validando) _validar(token);
  }

  Future<void> _validar(String token) async {
    setState(() {
      _validando = true;
      _error = '';
    });
    try {
      final r = await IdentityApi.validarAcceso(token);
      if (!mounted) return;
      setState(() {
        _resultado = r;
        _historial = [r, ..._historial].take(15).toList();
        _validando = false;
        _tokenManual.clear();
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _validando = false;
        _error = mensajeError(e, 'No se pudo validar el código.');
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Pagina(children: [
      const Encabezado(
        eyebrow: 'Recepción',
        titulo: 'Control de acceso',
        descripcion: 'Escanea el QR que muestra el usuario en “Mi acceso”. Cada código dura un minuto.',
      ),
      Dividido(
        principal: Bloques(children: [
          Tarjeta(child: _escaner()),
          Tarjeta(
            child: Bloques(espacio: 14.4, children: [
              Campo(
                etiqueta: 'Código leído (texto del QR)',
                controller: _tokenManual,
                hint: 'Pega aquí el contenido del QR',
                lineas: 3,
                mono: true,
              ),
              Boton.primario(
                _validando ? 'Validando…' : 'Validar acceso',
                onPressed: _tokenManual.text.trim().isEmpty || _validando ? null : _validarManual,
              ),
            ]),
          ),
        ]),
        lateral: Bloques(children: [
          if (_resultado != null) _Veredicto(_resultado!) else _esperando(),
          if (_error.isNotEmpty) Alerta(_error),
          if (_historial.isNotEmpty)
            Tarjeta(
              child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                const CabeceraTarjeta(titulo: 'Últimas validaciones'),
                ListaSeparada(espacio: 8.8, children: [
                  for (final h in _historial)
                    Row(children: [
                      Etiqueta(h.permitido ? 'Permitido' : 'Denegado', tono: h.permitido ? Tono.ok : Tono.danger),
                      const SizedBox(width: 12),
                      Expanded(child: Text(h.nombre ?? h.motivo, overflow: TextOverflow.ellipsis, style: T.texto(size: 14.5))),
                      const SizedBox(width: 12),
                      Text(hora(h.hora), style: T.mono(color: C.muted)),
                    ]),
                ]),
              ]),
            ),
        ]),
      ),
    ]);
  }

  Widget _escaner() {
    final camara = _camara;
    if (camara != null) {
      return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(R.radiusSm),
          child: AspectRatio(
            aspectRatio: 4 / 3,
            child: Stack(fit: StackFit.expand, children: [
              ColoredBox(
                color: Colors.black,
                child: MobileScanner(
                  controller: camara,
                  onDetect: _alDetectar,
                  errorBuilder: (context, error) => Center(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Text(
                        'No se pudo abrir la cámara. Revisa los permisos del navegador o pega el código.',
                        textAlign: TextAlign.center,
                        style: T.texto(color: C.danger),
                      ),
                    ),
                  ),
                ),
              ),
              // Marco de lectura con el exterior oscurecido.
              LayoutBuilder(builder: (context, c) {
                final mx = c.maxWidth * 0.18;
                final my = c.maxHeight * 0.18;
                return Stack(children: [
                  Positioned.fill(
                    child: ColorFiltered(
                      colorFilter: const ColorFilter.mode(Color(0x59000000), BlendMode.srcOut),
                      child: Stack(children: [
                        const Positioned.fill(child: ColoredBox(color: Colors.transparent)),
                        Positioned(
                          left: mx,
                          right: mx,
                          top: my,
                          bottom: my,
                          child: Container(decoration: BoxDecoration(color: Colors.black, borderRadius: BorderRadius.circular(16))),
                        ),
                      ]),
                    ),
                  ),
                  Positioned(
                    left: mx,
                    right: mx,
                    top: my,
                    bottom: my,
                    child: Container(
                      decoration: BoxDecoration(
                        border: Border.all(color: C.accent, width: 3),
                        borderRadius: BorderRadius.circular(16),
                      ),
                    ),
                  ),
                ]);
              }),
            ]),
          ),
        ),
        const SizedBox(height: 16),
        Center(child: Boton('Detener cámara', icono: 'cerrar', onPressed: _detenerCamara)),
      ]);
    }
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 32, horizontal: 16),
      child: Column(children: [
        const Icono('camara', size: 40, color: C.accent),
        const SizedBox(height: 12),
        ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 340),
          child: Text('Usa la cámara de este equipo para leer el código.', textAlign: TextAlign.center, style: T.texto(color: C.muted)),
        ),
        const SizedBox(height: 12),
        Boton.primario('Activar cámara', onPressed: _iniciarCamara),
      ]),
    );
  }

  Widget _esperando() => Container(
        constraints: const BoxConstraints(minHeight: 220),
        alignment: Alignment.center,
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: C.surface,
          borderRadius: BorderRadius.circular(R.radius),
          border: Border.all(color: C.borderStrong),
        ),
        child: Text('El resultado de la validación aparecerá aquí.', textAlign: TextAlign.center, style: T.texto(color: C.muted)),
      );
}

class _Veredicto extends StatelessWidget {
  const _Veredicto(this.r);
  final AccesoResult r;

  @override
  Widget build(BuildContext context) {
    final color = r.permitido ? C.ok : C.danger;
    final suave = r.permitido ? C.okSoft : C.dangerSoft;
    return Tarjeta(
      padding: const EdgeInsets.fromLTRB(24, 32, 24, 20),
      borde: color,
      gradiente: degradado(suave, desde: Alignment.topCenter, hasta: Alignment.bottomCenter, corte: 0.55),
      child: Column(children: [
        Container(
          width: 72,
          height: 72,
          alignment: Alignment.center,
          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
          child: Text(r.permitido ? '✓' : '✕', style: T.texto(size: 32, weight: FontWeight.w800, color: C.accentInk)),
        ),
        const SizedBox(height: 8),
        Text((r.permitido ? 'Acceso permitido' : 'Acceso denegado').toUpperCase(), style: T.display(size: 32, color: color)),
        if (r.nombre != null) ...[
          const SizedBox(height: 6),
          Text(r.nombre!, style: T.texto(size: 17.6, weight: FontWeight.w600)),
        ],
        const SizedBox(height: 16),
        Detalles([
          if (r.plan != null) ('Plan', r.plan!),
          if (r.fechaVencimiento != null) ('Vence', fechaMedia(r.fechaVencimiento!)),
          ('Motivo', r.motivo),
        ]),
      ]),
    );
  }
}
