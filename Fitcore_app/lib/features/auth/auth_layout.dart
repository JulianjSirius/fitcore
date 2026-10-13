import 'package:flutter/material.dart';

import '../../theme/tema.dart';
import '../../ui/componentes.dart';

/// Estructura de login y registro: portada con el mensaje a la izquierda y el formulario a la derecha.
class AuthLayout extends StatelessWidget {
  const AuthLayout({
    super.key,
    required this.arriba,
    required this.eyebrow,
    required this.tituloLinea1,
    required this.tituloResaltado,
    required this.descripcion,
    required this.abajo,
    required this.titulo,
    required this.subtitulo,
    required this.formulario,
  });

  final Widget arriba;
  final String eyebrow;
  final String tituloLinea1;
  final String tituloResaltado;
  final String descripcion;
  final Widget abajo;
  final String titulo;
  final String subtitulo;
  final Widget formulario;

  @override
  Widget build(BuildContext context) {
    final ancho = MediaQuery.sizeOf(context).width;
    final apilado = ancho < 860;
    final pad = (ancho * 0.04).clamp(24.0, 56.0);
    final tamTitulo = apilado ? (ancho * 0.13).clamp(45.0, 64.0) : (ancho * 0.08).clamp(54.0, 104.0);

    final portada = Container(
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF15181B), Color(0xFF0D0F11)],
          stops: [0, 0.7],
        ),
        border: Border(
          right: apilado ? BorderSide.none : const BorderSide(color: C.border),
          bottom: apilado ? const BorderSide(color: C.border) : BorderSide.none,
        ),
      ),
      child: Stack(
        children: [
          // Brillo lima arriba a la derecha.
          const Positioned.fill(
            child: DecoratedBox(
              decoration: BoxDecoration(
                gradient: RadialGradient(
                  center: Alignment(0.7, -0.7),
                  radius: 0.9,
                  colors: [Color(0x29C8FF3D), Color(0x00C8FF3D)],
                ),
              ),
            ),
          ),
          // Anillo decorativo.
          Positioned(
            right: -120,
            bottom: -120,
            child: Container(
              width: 420,
              height: 420,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                border: Border.all(color: const Color(0x0DC8FF3D), width: 60),
              ),
            ),
          ),
          Padding(
            padding: EdgeInsets.all(pad),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              mainAxisSize: apilado ? MainAxisSize.min : MainAxisSize.max,
              children: [
                arriba,
                if (apilado) const SizedBox(height: 40),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Eyebrow(eyebrow),
                    const SizedBox(height: 13),
                    Text.rich(
                      TextSpan(children: [
                        TextSpan(text: '${tituloLinea1.toUpperCase()}\n'),
                        TextSpan(text: tituloResaltado.toUpperCase(), style: const TextStyle(color: C.accent)),
                      ]),
                      style: T.display(size: tamTitulo).copyWith(height: 0.92),
                    ),
                    const SizedBox(height: 16),
                    ConstrainedBox(
                      constraints: const BoxConstraints(maxWidth: 460),
                      child: Text(descripcion, style: T.texto(size: 16.8, color: C.muted)),
                    ),
                  ],
                ),
                if (apilado) const SizedBox(height: 40),
                abajo,
              ],
            ),
          ),
        ],
      ),
    );

    final panel = Center(
      child: SingleChildScrollView(
        padding: EdgeInsets.all(pad),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 430),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(titulo.toUpperCase(), style: T.display(size: 35)),
              const SizedBox(height: 6),
              Text(subtitulo, style: T.texto(color: C.muted)),
              const SizedBox(height: 24),
              formulario,
            ],
          ),
        ),
      ),
    );

    return Scaffold(
      body: apilado
          ? SingleChildScrollView(child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [portada, panel]))
          : Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [Expanded(flex: 11, child: portada), Expanded(flex: 10, child: panel)],
            ),
    );
  }
}
