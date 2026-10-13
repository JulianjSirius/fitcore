import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter/services.dart';

import '../theme/tema.dart';
import 'icono.dart';

// Componentes visuales de fitcore-web (src/styles.css) llevados a Flutter.

enum Tono { neutro, ok, warn, danger, accent, info }

(Color, Color) _coloresTono(Tono t) => switch (t) {
  Tono.neutro => (C.muted, C.surface3),
  Tono.ok => (C.ok, C.okSoft),
  Tono.warn => (C.warn, C.warnSoft),
  Tono.danger => (C.danger, C.dangerSoft),
  Tono.accent => (C.accent, C.accentSoft),
  Tono.info => (C.info, C.infoSoft),
};

/// Separa una lista de widgets con un espacio fijo.
List<Widget> separar(
  Iterable<Widget> hijos,
  double espacio, {
  bool horizontal = false,
}) {
  final lista = <Widget>[];
  for (final h in hijos) {
    if (lista.isNotEmpty) {
      lista.add(
        SizedBox(
          width: horizontal ? espacio : 0,
          height: horizontal ? 0 : espacio,
        ),
      );
    }
    lista.add(h);
  }
  return lista;
}

// ---------------------------------------------------------------- Página

/// Contenedor de cada pantalla: ancho máximo 1180 y separación de 24 entre bloques.
class Pagina extends StatelessWidget {
  const Pagina({super.key, required this.children, this.controller});

  final List<Widget> children;
  final ScrollController? controller;

  @override
  Widget build(BuildContext context) {
    final ancho = MediaQuery.sizeOf(context).width;
    final lateral = (ancho * 0.03).clamp(16.0, 40.0);
    return SingleChildScrollView(
      controller: controller,
      child: Align(
        alignment: Alignment.topCenter,
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 1180),
          child: Padding(
            padding: EdgeInsets.fromLTRB(lateral, 32, lateral, 64),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: separar(children, 24),
            ),
          ),
        ),
      ),
    );
  }
}

class Eyebrow extends StatelessWidget {
  const Eyebrow(this.texto, {super.key});
  final String texto;

  @override
  Widget build(BuildContext context) =>
      Text(texto.toUpperCase(), style: T.rotulo());
}

class Encabezado extends StatelessWidget {
  const Encabezado({
    super.key,
    required this.eyebrow,
    required this.titulo,
    this.descripcion,
    this.accion,
  });

  final String eyebrow;
  final String titulo;
  final String? descripcion;
  final Widget? accion;

  @override
  Widget build(BuildContext context) {
    final ancho = MediaQuery.sizeOf(context).width;
    final texto = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Eyebrow(eyebrow),
        const SizedBox(height: 6),
        Text(
          titulo.toUpperCase(),
          style: T.display(size: (ancho * 0.05).clamp(35.0, 51.0)),
        ),
        if (descripcion != null) ...[
          const SizedBox(height: 6),
          ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 560),
            child: Text(descripcion!, style: T.texto(color: C.muted)),
          ),
        ],
      ],
    );
    if (accion == null) return texto;
    return Wrap(
      alignment: WrapAlignment.spaceBetween,
      crossAxisAlignment: WrapCrossAlignment.end,
      spacing: 16,
      runSpacing: 16,
      children: [texto, accion!],
    );
  }
}

// ---------------------------------------------------------------- Tarjetas

class Tarjeta extends StatefulWidget {
  const Tarjeta({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(21.6),
    this.borde,
    this.fondo,
    this.gradiente,
    this.sombraAcento = false,
    this.onTap,
    this.opacidad = 1,
  });

  final Widget child;
  final EdgeInsets padding;
  final Color? borde;
  final Color? fondo;
  final Gradient? gradiente;

  /// Halo de acento (editores abiertos).
  final bool sombraAcento;

  /// Si hay acción, la tarjeta reacciona al pasar el mouse (tarjetas de acceso rápido).
  final VoidCallback? onTap;
  final double opacidad;

  @override
  State<Tarjeta> createState() => _TarjetaState();
}

class _TarjetaState extends State<Tarjeta> {
  bool _hover = false;

  @override
  Widget build(BuildContext context) {
    final w = widget;
    Widget caja = AnimatedContainer(
      duration: const Duration(milliseconds: 150),
      transform: Matrix4.translationValues(
        0,
        _hover && w.onTap != null ? -2 : 0,
        0,
      ),
      padding: w.padding,
      decoration: BoxDecoration(
        color: w.gradiente == null ? (w.fondo ?? C.surface) : null,
        gradient: w.gradiente,
        borderRadius: BorderRadius.circular(R.radius),
        border: Border.all(
          color: _hover && w.onTap != null ? C.accent : (w.borde ?? C.border),
        ),
        boxShadow: w.sombraAcento
            ? const [BoxShadow(color: C.accentSoft, spreadRadius: 4)]
            : null,
      ),
      child: w.child,
    );
    if (w.opacidad < 1) caja = Opacity(opacity: w.opacidad, child: caja);
    if (w.onTap == null) return caja;
    return MouseRegion(
      cursor: SystemMouseCursors.click,
      onEnter: (_) => setState(() => _hover = true),
      onExit: (_) => setState(() => _hover = false),
      child: GestureDetector(
        onTap: w.onTap,
        behavior: HitTestBehavior.opaque,
        child: caja,
      ),
    );
  }
}

/// Fondo con un degradado que se desvanece hacia la superficie (avisos, veredictos, destacados).
Gradient degradado(
  Color color, {
  Alignment desde = Alignment.centerLeft,
  Alignment hasta = Alignment.centerRight,
  double corte = 0.6,
}) => LinearGradient(
  begin: desde,
  end: hasta,
  colors: [color, C.surface],
  stops: [0, corte],
);

Gradient halo(
  Color color, {
  Alignment centro = Alignment.topRight,
  double radio = 1.1,
}) => RadialGradient(
  center: centro,
  radius: radio,
  colors: [color, C.surface],
  stops: const [0, 0.55],
);

class CabeceraTarjeta extends StatelessWidget {
  const CabeceraTarjeta({
    super.key,
    required this.titulo,
    this.subtitulo,
    this.trailing,
    this.eyebrow,
  });

  final String titulo;
  final String? subtitulo;
  final String? eyebrow;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 17.6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (eyebrow != null) ...[
                  Eyebrow(eyebrow!),
                  const SizedBox(height: 4),
                ],
                Text(
                  titulo,
                  style: T.texto(size: 16.8, weight: FontWeight.w700),
                ),
                if (subtitulo != null) ...[
                  const SizedBox(height: 4),
                  Text(subtitulo!, style: T.texto(size: 13.5, color: C.muted)),
                ],
              ],
            ),
          ),
          if (trailing != null) ...[const SizedBox(width: 16), trailing!],
        ],
      ),
    );
  }
}

/// Métrica grande (tarjeta .stat). La versión con acento usa fondo lima.
class Metrica extends StatelessWidget {
  const Metrica({
    super.key,
    required this.etiqueta,
    this.valor,
    this.valorWidget,
    this.pie,
    this.acento = false,
    this.colorValor,
  });

  final String etiqueta;
  final String? valor;
  final Widget? valorWidget;
  final String? pie;
  final bool acento;
  final Color? colorValor;

  @override
  Widget build(BuildContext context) {
    final tinta = acento ? C.accentInk : C.text;
    final suave = acento ? C.accentInk.withValues(alpha: 0.7) : C.muted;
    return Tarjeta(
      fondo: acento ? C.accent : null,
      borde: acento ? C.accent : null,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            etiqueta.toUpperCase(),
            style: T.rotulo(size: 12, color: suave, espaciado: 0.08),
          ),
          const SizedBox(height: 6),
          DefaultTextStyle(
            style: T.display(
              size: 38,
              weight: FontWeight.w700,
              color: colorValor ?? tinta,
            ),
            child: IconTheme(
              data: IconThemeData(color: colorValor ?? tinta),
              child: valorWidget ?? Text(valor ?? ''),
            ),
          ),
          if (pie != null) ...[
            const SizedBox(height: 6),
            Text(
              pie!,
              style: T.texto(size: 13, color: acento ? suave : C.faint),
            ),
          ],
        ],
      ),
    );
  }
}

// ---------------------------------------------------------------- Insignias y alertas

class Etiqueta extends StatelessWidget {
  const Etiqueta(this.texto, {super.key, this.tono = Tono.neutro, this.icono});

  final String texto;
  final Tono tono;
  final String? icono;

  @override
  Widget build(BuildContext context) {
    final (fg, bg) = _coloresTono(tono);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9.6, vertical: 3.2),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icono != null) ...[
            Icono(icono!, size: 13, color: fg),
            const SizedBox(width: 5),
          ],
          Text(
            texto,
            style: T
                .texto(size: 11.5, weight: FontWeight.w700, color: fg)
                .copyWith(letterSpacing: 0.46),
          ),
        ],
      ),
    );
  }
}

class Alerta extends StatelessWidget {
  const Alerta(this.texto, {super.key, this.tono = Tono.danger});

  final String texto;
  final Tono tono;

  const Alerta.ok(this.texto, {super.key}) : tono = Tono.ok;
  const Alerta.info(this.texto, {super.key}) : tono = Tono.info;

  @override
  Widget build(BuildContext context) {
    final (fg, bg) = _coloresTono(tono);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14.4, vertical: 11.2),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(R.radiusSm),
        border: Border.all(color: fg.withValues(alpha: 0.35)),
      ),
      child: Text(texto, style: T.texto(size: 14, color: fg)),
    );
  }
}

// ---------------------------------------------------------------- Botones

enum Variante { normal, primario, fantasma, peligro }

class Boton extends StatelessWidget {
  const Boton(
    this.texto, {
    super.key,
    this.icono,
    this.onPressed,
    this.variante = Variante.normal,
    this.chico = false,
    this.bloque = false,
    this.grande = false,
    this.tooltip,
  });

  const Boton.primario(
    this.texto, {
    super.key,
    this.icono,
    this.onPressed,
    this.chico = false,
    this.bloque = false,
    this.grande = false,
    this.tooltip,
  }) : variante = Variante.primario;

  const Boton.fantasma(
    this.texto, {
    super.key,
    this.icono,
    this.onPressed,
    this.chico = false,
    this.bloque = false,
    this.grande = false,
    this.tooltip,
  }) : variante = Variante.fantasma;

  const Boton.peligro(
    this.texto, {
    super.key,
    this.icono,
    this.onPressed,
    this.chico = false,
    this.bloque = false,
    this.grande = false,
    this.tooltip,
  }) : variante = Variante.peligro;

  final String texto;
  final String? icono;
  final VoidCallback? onPressed;
  final Variante variante;
  final bool chico;
  final bool bloque;
  final bool grande;
  final String? tooltip;

  @override
  Widget build(BuildContext context) {
    final (
      fondo,
      fondoHover,
      borde,
      bordeHover,
      tinta,
      tintaHover,
    ) = switch (variante) {
      Variante.normal => (
        C.surface2,
        C.surface3,
        C.borderStrong,
        C.faint,
        C.text,
        C.text,
      ),
      Variante.primario => (
        C.accent,
        C.accentHover,
        C.accent,
        C.accentHover,
        C.accentInk,
        C.accentInk,
      ),
      Variante.fantasma => (
        Colors.transparent,
        Colors.transparent,
        Colors.transparent,
        Colors.transparent,
        C.muted,
        C.text,
      ),
      Variante.peligro => (
        Colors.transparent,
        C.dangerSoft,
        C.danger.withValues(alpha: 0.4),
        C.danger,
        C.danger,
        C.danger,
      ),
    };
    final alto = chico ? 32.0 : (grande ? 48.0 : 40.0);
    final tamTexto = chico ? 12.8 : (grande ? 15.2 : 14.1);
    final activo = onPressed != null;

    Widget boton = Opacity(
      opacity: activo ? 1 : 0.45,
      child: TextButton(
        onPressed: onPressed,
        style: ButtonStyle(
          minimumSize: WidgetStatePropertyAll(Size(chico ? 32 : 40, alto)),
          padding: WidgetStatePropertyAll(
            EdgeInsets.symmetric(
              horizontal: texto.isEmpty
                  ? (chico ? 8 : 10)
                  : (chico ? 11.2 : 16),
            ),
          ),
          tapTargetSize: MaterialTapTargetSize.shrinkWrap,
          visualDensity: VisualDensity.standard,
          overlayColor: const WidgetStatePropertyAll(Colors.transparent),
          shape: WidgetStatePropertyAll(
            RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(R.radiusSm),
            ),
          ),
          backgroundColor: WidgetStateProperty.resolveWith(
            (s) =>
                s.contains(WidgetState.hovered) && activo ? fondoHover : fondo,
          ),
          side: WidgetStateProperty.resolveWith(
            (s) => BorderSide(
              color: s.contains(WidgetState.hovered) && activo
                  ? bordeHover
                  : borde,
            ),
          ),
          foregroundColor: WidgetStateProperty.resolveWith(
            (s) =>
                s.contains(WidgetState.hovered) && activo ? tintaHover : tinta,
          ),
          textStyle: WidgetStatePropertyAll(
            T.texto(size: tamTexto, weight: FontWeight.w600),
          ),
          mouseCursor: WidgetStatePropertyAll(
            activo ? SystemMouseCursors.click : SystemMouseCursors.forbidden,
          ),
        ),
        child: Builder(
          builder: (context) {
            final color = DefaultTextStyle.of(context).style.color;
            return Row(
              mainAxisSize: bloque ? MainAxisSize.max : MainAxisSize.min,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                if (icono != null)
                  Icono(icono!, size: chico ? 15 : 18, color: color),
                if (icono != null && texto.isNotEmpty) const SizedBox(width: 7),
                if (texto.isNotEmpty)
                  Flexible(
                    child: Text(
                      texto,
                      overflow: TextOverflow.ellipsis,
                      maxLines: 1,
                    ),
                  ),
              ],
            );
          },
        ),
      ),
    );
    if (bloque) boton = SizedBox(width: double.infinity, child: boton);
    if (tooltip != null) boton = Tooltip(message: tooltip!, child: boton);
    return boton;
  }
}

// ---------------------------------------------------------------- Formularios

TextStyle get _estiloEtiqueta => T
    .texto(size: 12.5, weight: FontWeight.w600, color: C.muted)
    .copyWith(letterSpacing: 0.25);

class ConEtiqueta extends StatelessWidget {
  const ConEtiqueta({super.key, required this.etiqueta, required this.child});
  final String etiqueta;
  final Widget child;

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    mainAxisSize: MainAxisSize.min,
    children: [
      Text(etiqueta, style: _estiloEtiqueta),
      const SizedBox(height: 6),
      child,
    ],
  );
}

class Campo extends StatelessWidget {
  const Campo({
    super.key,
    this.etiqueta,
    this.controller,
    this.hint,
    this.oculto = false,
    this.teclado,
    this.lineas = 1,
    this.mono = false,
    this.onChanged,
    this.onSubmitted,
    this.habilitado = true,
    this.soloDigitos = false,
    this.decimal = false,
    this.autofill,
    this.prefijo,
    this.maxLength,
    this.sugerencias,
    this.compacto = false,
  });

  /// Campo bajo (34 px) para cifras dentro de listas.
  final bool compacto;

  final String? etiqueta;
  final TextEditingController? controller;
  final String? hint;
  final bool oculto;
  final TextInputType? teclado;
  final int lineas;
  final bool mono;
  final ValueChanged<String>? onChanged;
  final ValueChanged<String>? onSubmitted;
  final bool habilitado;
  final bool soloDigitos;
  final bool decimal;
  final Iterable<String>? autofill;
  final Widget? prefijo;
  final int? maxLength;

  /// Opciones para autocompletar (equivalente a un <datalist>).
  final List<String>? sugerencias;

  @override
  Widget build(BuildContext context) {
    final estilo = mono
        ? T.mono(size: 14.5)
        : T.texto(size: 14.7, weight: FontWeight.w500);
    final formatos = <TextInputFormatter>[
      if (soloDigitos) FilteringTextInputFormatter.digitsOnly,
      if (decimal) FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]')),
      if (maxLength != null) LengthLimitingTextInputFormatter(maxLength),
    ];
    final decoracion = InputDecoration(
      hintText: hint,
      contentPadding: compacto
          ? const EdgeInsets.symmetric(horizontal: 8, vertical: 9)
          : null,
      prefixIcon: prefijo,
      prefixIconConstraints: const BoxConstraints(minWidth: 36, minHeight: 20),
    );
    final teclas =
        teclado ??
        (soloDigitos
            ? TextInputType.number
            : (decimal
                  ? const TextInputType.numberWithOptions(decimal: true)
                  : null));

    Widget campo;
    if (sugerencias != null && controller != null) {
      campo = _CampoSugerencias(
        controller: controller!,
        opciones: sugerencias!,
        estilo: estilo,
        decoracion: decoracion,
        onChanged: onChanged,
      );
    } else {
      campo = TextField(
        controller: controller,
        obscureText: oculto,
        keyboardType: lineas > 1 ? TextInputType.multiline : teclas,
        minLines: lineas > 1 ? lineas : null,
        maxLines: lineas > 1 ? null : 1,
        style: estilo,
        enabled: habilitado,
        inputFormatters: formatos,
        onChanged: onChanged,
        onSubmitted: onSubmitted,
        autofillHints: autofill,
        decoration: decoracion,
      );
    }
    return etiqueta == null
        ? campo
        : ConEtiqueta(etiqueta: etiqueta!, child: campo);
  }
}

class _CampoSugerencias extends StatefulWidget {
  const _CampoSugerencias({
    required this.controller,
    required this.opciones,
    required this.estilo,
    required this.decoracion,
    this.onChanged,
  });

  final TextEditingController controller;
  final List<String> opciones;
  final TextStyle estilo;
  final InputDecoration decoracion;
  final ValueChanged<String>? onChanged;

  @override
  State<_CampoSugerencias> createState() => _CampoSugerenciasState();
}

class _CampoSugerenciasState extends State<_CampoSugerencias> {
  final _foco = FocusNode();

  @override
  void dispose() {
    _foco.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return RawAutocomplete<String>(
      textEditingController: widget.controller,
      focusNode: _foco,
      optionsBuilder: (v) {
        final t = v.text.trim().toLowerCase();
        return widget.opciones.where(
          (o) => o.toLowerCase().contains(t) && o.toLowerCase() != t,
        );
      },
      fieldViewBuilder: (context, c, foco, enviar) => TextField(
        controller: c,
        focusNode: foco,
        style: widget.estilo,
        decoration: widget.decoracion,
        onChanged: widget.onChanged,
      ),
      optionsViewBuilder: (context, elegir, opciones) => Align(
        alignment: Alignment.topLeft,
        child: Material(
          color: C.surface2,
          elevation: 8,
          borderRadius: BorderRadius.circular(R.radiusSm),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxHeight: 220, maxWidth: 320),
            child: ListView(
              padding: const EdgeInsets.symmetric(vertical: 4),
              shrinkWrap: true,
              children: [
                for (final o in opciones)
                  InkWell(
                    onTap: () => elegir(o),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 9,
                      ),
                      child: Text(o, style: T.texto(size: 14)),
                    ),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class Busqueda extends StatelessWidget {
  const Busqueda({
    super.key,
    required this.hint,
    required this.onChanged,
    this.controller,
  });

  final String hint;
  final ValueChanged<String> onChanged;
  final TextEditingController? controller;

  @override
  Widget build(BuildContext context) => Campo(
    hint: hint,
    controller: controller,
    onChanged: onChanged,
    prefijo: const Padding(
      padding: EdgeInsets.only(left: 10, right: 4),
      child: Icon(Icons.search, size: 18, color: C.faint),
    ),
  );
}

class Selector<V> extends StatelessWidget {
  const Selector({
    super.key,
    this.etiqueta,
    required this.valor,
    required this.opciones,
    required this.onChanged,
    this.hint,
  });

  final String? etiqueta;
  final V? valor;
  final List<(V, String)> opciones;
  final ValueChanged<V>? onChanged;
  final String? hint;

  @override
  Widget build(BuildContext context) {
    final existe = opciones.any((o) => o.$1 == valor);
    final campo = InputDecorator(
      decoration: InputDecoration(
        enabled: onChanged != null,
        contentPadding: const EdgeInsets.symmetric(horizontal: 13, vertical: 3),
      ),
      child: DropdownButtonHideUnderline(
        child: DropdownButton<V>(
          value: existe ? valor : null,
          isExpanded: true,
          dropdownColor: C.surface2,
          borderRadius: BorderRadius.circular(R.radiusSm),
          iconEnabledColor: C.muted,
          iconDisabledColor: C.faint,
          style: T.texto(size: 14.7, weight: FontWeight.w500),
          hint: hint == null
              ? null
              : Text(hint!, style: T.texto(size: 14.7, color: C.faint)),
          onChanged: onChanged == null
              ? null
              : (v) => v == null ? null : onChanged!(v),
          items: [
            for (final (v, texto) in opciones)
              DropdownMenuItem(
                value: v,
                child: Text(texto, overflow: TextOverflow.ellipsis),
              ),
          ],
        ),
      ),
    );
    return etiqueta == null
        ? campo
        : ConEtiqueta(etiqueta: etiqueta!, child: campo);
  }
}

class Casilla extends StatelessWidget {
  const Casilla({
    super.key,
    required this.etiqueta,
    required this.valor,
    required this.onChanged,
  });

  final String etiqueta;
  final bool valor;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
      cursor: SystemMouseCursors.click,
      child: GestureDetector(
        onTap: () => onChanged(!valor),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            SizedBox(
              width: 22,
              height: 22,
              child: Checkbox(
                value: valor,
                onChanged: (v) => onChanged(v ?? false),
              ),
            ),
            const SizedBox(width: 9),
            Flexible(child: Text(etiqueta, style: T.texto(size: 14))),
          ],
        ),
      ),
    );
  }
}

/// Grupo de pestañas segmentado (.tabs).
class Pestanas<V> extends StatelessWidget {
  const Pestanas({
    super.key,
    required this.opciones,
    required this.seleccion,
    required this.onChanged,
    this.completo = false,
  });

  final List<(V, String)> opciones;
  final V seleccion;
  final ValueChanged<V> onChanged;
  final bool completo;

  @override
  Widget build(BuildContext context) {
    Widget boton((V, String) o) {
      final activo = o.$1 == seleccion;
      return MouseRegion(
        cursor: SystemMouseCursors.click,
        child: GestureDetector(
          onTap: () => onChanged(o.$1),
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 150),
            height: 34,
            alignment: Alignment.center,
            padding: const EdgeInsets.symmetric(horizontal: 14.4),
            decoration: BoxDecoration(
              color: activo ? C.surface3 : Colors.transparent,
              borderRadius: BorderRadius.circular(7),
            ),
            child: Text(
              o.$2,
              style: T.texto(
                size: 13.6,
                weight: FontWeight.w600,
                color: activo ? C.text : C.muted,
              ),
              maxLines: 1,
            ),
          ),
        ),
      );
    }

    final contenido = Row(
      mainAxisSize: completo ? MainAxisSize.max : MainAxisSize.min,
      children: separar(
        opciones.map((o) => completo ? Expanded(child: boton(o)) : boton(o)),
        4,
        horizontal: true,
      ),
    );
    final caja = Container(
      padding: const EdgeInsets.all(4),
      decoration: BoxDecoration(
        color: C.surface,
        borderRadius: BorderRadius.circular(R.radiusSm),
        border: Border.all(color: C.border),
      ),
      child: contenido,
    );
    if (completo) return caja;
    return Align(
      alignment: Alignment.centerLeft,
      widthFactor: 1,
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: caja,
      ),
    );
  }
}

// ---------------------------------------------------------------- Listas y estados

/// Lista de detalle (dt/dd) con separadores.
class Detalles extends StatelessWidget {
  const Detalles(this.filas, {super.key});

  /// (etiqueta, valor): el valor puede ser String o Widget.
  final List<(String, Object)> filas;

  @override
  Widget build(BuildContext context) {
    return ListaSeparada(
      espacio: 10.4,
      children: [
        for (final (etiqueta, valor) in filas)
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(etiqueta, style: T.texto(size: 13.6, color: C.muted)),
              const SizedBox(width: 16),
              Expanded(
                child: Align(
                  alignment: Alignment.centerRight,
                  child: valor is Widget
                      ? valor
                      : Text(
                          '$valor',
                          textAlign: TextAlign.right,
                          style: T.texto(weight: FontWeight.w600),
                        ),
                ),
              ),
            ],
          ),
      ],
    );
  }
}

/// Filas separadas por una línea superior (excepto la primera).
class ListaSeparada extends StatelessWidget {
  const ListaSeparada({super.key, required this.children, this.espacio = 12});

  final List<Widget> children;
  final double espacio;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        for (var i = 0; i < children.length; i++)
          Container(
            padding: EdgeInsets.symmetric(vertical: espacio),
            decoration: BoxDecoration(
              border: i == 0
                  ? null
                  : const Border(top: BorderSide(color: C.border)),
            ),
            child: children[i],
          ),
      ],
    );
  }
}

/// Título y subtítulo de una celda (.cell-title).
class TituloCelda extends StatelessWidget {
  const TituloCelda(this.titulo, {super.key, this.subtitulo, this.trailing});

  final String titulo;
  final String? subtitulo;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Row(
          children: [
            Flexible(
              child: Text(
                titulo,
                style: T.texto(size: 14.5, weight: FontWeight.w600),
                overflow: TextOverflow.ellipsis,
              ),
            ),
            if (trailing != null) ...[const SizedBox(width: 8), trailing!],
          ],
        ),
        if (subtitulo != null && subtitulo!.isNotEmpty)
          Text(
            subtitulo!,
            style: T.texto(size: 13, color: C.muted),
            overflow: TextOverflow.ellipsis,
            maxLines: 1,
          ),
      ],
    );
  }
}

class EstadoVacio extends StatelessWidget {
  const EstadoVacio({
    super.key,
    this.titulo = 'Sin resultados',
    this.mensaje = 'Todavía no hay información para mostrar.',
  });

  final String titulo;
  final String mensaje;

  @override
  Widget build(BuildContext context) {
    return CustomPaint(
      painter: _BordePunteado(),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 40, horizontal: 24),
        child: Column(
          children: [
            Container(
              width: 34,
              height: 4,
              decoration: BoxDecoration(
                color: C.accent,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
            const SizedBox(height: 12),
            Text(
              titulo,
              style: T.texto(size: 16, weight: FontWeight.w700),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 6),
            Text(
              mensaje,
              style: T.texto(size: 14.4, color: C.muted),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }
}

class _BordePunteado extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final pintura = Paint()
      ..color = C.borderStrong
      ..style = PaintingStyle.stroke;
    final ruta = Path()
      ..addRRect(
        RRect.fromRectAndRadius(
          Offset.zero & size,
          const Radius.circular(R.radius),
        ),
      );
    for (final m in ruta.computeMetrics()) {
      for (double d = 0; d < m.length; d += 9) {
        canvas.drawPath(m.extractPath(d, d + 5), pintura);
      }
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

/// Tres puntos que pulsan (.loading-state).
class Cargando extends StatefulWidget {
  const Cargando({super.key});

  @override
  State<Cargando> createState() => _CargandoState();
}

class _CargandoState extends State<Cargando>
    with SingleTickerProviderStateMixin {
  late final AnimationController _c = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 1000),
  )..repeat(reverse: true);

  @override
  void dispose() {
    _c.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(40),
      child: AnimatedBuilder(
        animation: _c,
        builder: (context, _) => Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            for (var i = 0; i < 3; i++)
              Builder(
                builder: (context) {
                  final t = ((_c.value - i * 0.2) % 1.0).abs();
                  return Container(
                    margin: const EdgeInsets.symmetric(horizontal: 3),
                    transform: Matrix4.translationValues(0, -5 * t, 0),
                    width: 8,
                    height: 8,
                    decoration: BoxDecoration(
                      color: C.accent.withValues(alpha: 1 - 0.75 * t),
                      shape: BoxShape.circle,
                    ),
                  );
                },
              ),
          ],
        ),
      ),
    );
  }
}

class Avatar extends StatelessWidget {
  const Avatar(
    this.texto, {
    super.key,
    this.size = 36,
    this.fondo = C.surface3,
    this.tinta = C.accent,
  });

  final String texto;
  final double size;
  final Color fondo;
  final Color tinta;

  @override
  Widget build(BuildContext context) => Container(
    width: size,
    height: size,
    alignment: Alignment.center,
    decoration: BoxDecoration(color: fondo, shape: BoxShape.circle),
    child: Text(
      texto,
      style: T.texto(size: size * 0.34, weight: FontWeight.w700, color: tinta),
    ),
  );
}

class BarraProgreso extends StatelessWidget {
  const BarraProgreso(
    this.valor, {
    super.key,
    this.alto = 10,
    this.color = C.accent,
    this.ancho,
  });

  final double valor;
  final double alto;
  final Color color;
  final double? ancho;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: ancho,
      height: alto,
      child: ClipRRect(
        borderRadius: BorderRadius.circular(999),
        child: Stack(
          children: [
            const Positioned.fill(child: ColoredBox(color: C.surface3)),
            FractionallySizedBox(
              widthFactor: valor.clamp(0, 1).toDouble(),
              heightFactor: 1,
              child: DecoratedBox(
                decoration: BoxDecoration(
                  color: color,
                  borderRadius: BorderRadius.circular(999),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ---------------------------------------------------------------- Disposición

/// Rejilla responsiva: tantas columnas como quepan con [minAncho], hasta [maxColumnas].
/// Las tarjetas de una misma fila quedan de igual alto.
class Rejilla extends StatelessWidget {
  const Rejilla({
    super.key,
    required this.children,
    this.minAncho = 220,
    this.maxColumnas = 4,
    this.espacio = 16,
    this.igualarAlto = true,
  });

  final List<Widget> children;
  final double minAncho;
  final int maxColumnas;
  final double espacio;
  final bool igualarAlto;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, c) {
        final columnas = math.max(
          1,
          math.min(
            maxColumnas,
            ((c.maxWidth + espacio) / (minAncho + espacio)).floor(),
          ),
        );
        final filas = <Widget>[];
        for (var i = 0; i < children.length; i += columnas) {
          final celdas = [
            for (var j = i; j < i + columnas; j++)
              j < children.length ? children[j] : const SizedBox(),
          ];
          filas.add(
            _FilaIgual(
              espacio: espacio,
              igualarAlto: igualarAlto,
              children: celdas,
            ),
          );
        }
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: separar(filas, espacio),
        );
      },
    );
  }
}

/// Fila de celdas de igual ancho. Mide cada celda con su alto natural y luego
/// las vuelve a acomodar todas con el alto de la más alta (sin depender de alturas intrínsecas).
class _FilaIgual extends MultiChildRenderObjectWidget {
  const _FilaIgual({
    required this.espacio,
    required this.igualarAlto,
    required super.children,
  });

  final double espacio;
  final bool igualarAlto;

  @override
  RenderObject createRenderObject(BuildContext context) =>
      _RenderFilaIgual(espacio, igualarAlto);

  @override
  void updateRenderObject(BuildContext context, _RenderFilaIgual renderObject) {
    renderObject
      ..espacio = espacio
      ..igualarAlto = igualarAlto;
  }
}

class _DatosFila extends ContainerBoxParentData<RenderBox> {}

class _RenderFilaIgual extends RenderBox
    with
        ContainerRenderObjectMixin<RenderBox, _DatosFila>,
        RenderBoxContainerDefaultsMixin<RenderBox, _DatosFila> {
  _RenderFilaIgual(this._espacio, this._igualarAlto);

  double _espacio;
  set espacio(double v) {
    if (v != _espacio) {
      _espacio = v;
      markNeedsLayout();
    }
  }

  bool _igualarAlto;
  set igualarAlto(bool v) {
    if (v != _igualarAlto) {
      _igualarAlto = v;
      markNeedsLayout();
    }
  }

  @override
  void setupParentData(RenderBox child) {
    if (child.parentData is! _DatosFila) child.parentData = _DatosFila();
  }

  @override
  void performLayout() {
    final n = childCount;
    final ancho = (constraints.maxWidth - _espacio * (n - 1)) / n;
    var alto = 0.0;
    var hijo = firstChild;
    while (hijo != null) {
      hijo.layout(BoxConstraints.tightFor(width: ancho), parentUsesSize: true);
      if (hijo.size.height > alto) alto = hijo.size.height;
      hijo = childAfter(hijo);
    }
    var x = 0.0;
    hijo = firstChild;
    while (hijo != null) {
      if (_igualarAlto && hijo.size.height != alto) {
        hijo.layout(
          BoxConstraints.tightFor(width: ancho, height: alto),
          parentUsesSize: true,
        );
      }
      (hijo.parentData! as _DatosFila).offset = Offset(x, 0);
      x += ancho + _espacio;
      hijo = childAfter(hijo);
    }
    size = constraints.constrain(Size(constraints.maxWidth, alto));
  }

  @override
  void paint(PaintingContext context, Offset offset) =>
      defaultPaint(context, offset);

  @override
  bool hitTestChildren(BoxHitTestResult result, {required Offset position}) =>
      defaultHitTestChildren(result, position: position);
}

/// Dos columnas (1.6fr / 1fr) que se apilan en pantallas angostas (.split).
class Dividido extends StatelessWidget {
  const Dividido({
    super.key,
    required this.principal,
    required this.lateral,
    this.corte = 760,
    this.flexPrincipal = 16,
    this.flexLateral = 10,
  });

  final Widget principal;
  final Widget lateral;
  final double corte;
  final int flexPrincipal;
  final int flexLateral;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, c) {
        if (c.maxWidth < corte) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [principal, const SizedBox(height: 16), lateral],
          );
        }
        return Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(flex: flexPrincipal, child: principal),
            const SizedBox(width: 16),
            Expanded(flex: flexLateral, child: lateral),
          ],
        );
      },
    );
  }
}

/// Columna de bloques con separación de 16 (.grid).
class Bloques extends StatelessWidget {
  const Bloques({super.key, required this.children, this.espacio = 16});
  final List<Widget> children;
  final double espacio;

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: separar(children, espacio),
  );
}

/// Campos en dos columnas que se apilan en pantallas angostas (.form-grid).
class FormularioRejilla extends StatelessWidget {
  const FormularioRejilla({
    super.key,
    required this.children,
    this.completos = const {},
  });

  final List<Widget> children;

  /// Índices de campos que ocupan todo el ancho.
  final Set<int> completos;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, c) {
        if (c.maxWidth < 440) return Bloques(espacio: 14.4, children: children);
        final filas = <Widget>[];
        var i = 0;
        while (i < children.length) {
          if (completos.contains(i) ||
              i + 1 >= children.length ||
              completos.contains(i + 1)) {
            filas.add(children[i]);
            i++;
          } else {
            filas.add(
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(child: children[i]),
                  const SizedBox(width: 14.4),
                  Expanded(child: children[i + 1]),
                ],
              ),
            );
            i += 2;
          }
        }
        return Bloques(espacio: 14.4, children: filas);
      },
    );
  }
}

// ---------------------------------------------------------------- Tablas

class Columna {
  const Columna(this.texto, {this.flex = 1, this.numerica = false});
  final String texto;
  final int flex;
  final bool numerica;
}

class FilaTabla {
  const FilaTabla(this.celdas, {this.onTap, this.seleccionada = false});
  final List<Widget> celdas;
  final VoidCallback? onTap;
  final bool seleccionada;
}

class Tabla extends StatelessWidget {
  const Tabla({
    super.key,
    required this.columnas,
    required this.filas,
    this.anchoMinimo = 560,
  });

  final List<Columna> columnas;
  final List<FilaTabla> filas;
  final double anchoMinimo;

  @override
  Widget build(BuildContext context) {
    Widget celdas(List<Widget> hijos, {bool cabecera = false}) => Row(
      children: [
        for (var i = 0; i < columnas.length; i++)
          Expanded(
            flex: columnas[i].flex,
            child: Padding(
              padding: EdgeInsets.symmetric(
                horizontal: 14.4,
                vertical: cabecera ? 11.2 : 12,
              ),
              child: Align(
                alignment: columnas[i].numerica
                    ? Alignment.centerRight
                    : Alignment.centerLeft,
                child: hijos[i],
              ),
            ),
          ),
      ],
    );

    final contenido = Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        ColoredBox(
          color: C.surface2,
          child: celdas([
            for (final c in columnas)
              Text(
                c.texto.toUpperCase(),
                style: T.rotulo(size: 11.2, color: C.muted, espaciado: 0.08),
              ),
          ], cabecera: true),
        ),
        for (final f in filas) _FilaHover(fila: f, child: celdas(f.celdas)),
      ],
    );

    return Container(
      decoration: BoxDecoration(
        border: Border.all(color: C.border),
        borderRadius: BorderRadius.circular(R.radiusSm),
      ),
      clipBehavior: Clip.antiAlias,
      child: LayoutBuilder(
        builder: (context, c) {
          if (c.maxWidth >= anchoMinimo) return contenido;
          return SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: SizedBox(width: anchoMinimo, child: contenido),
          );
        },
      ),
    );
  }
}

class _FilaHover extends StatefulWidget {
  const _FilaHover({required this.fila, required this.child});
  final FilaTabla fila;
  final Widget child;

  @override
  State<_FilaHover> createState() => _FilaHoverState();
}

class _FilaHoverState extends State<_FilaHover> {
  bool _hover = false;

  @override
  Widget build(BuildContext context) {
    final f = widget.fila;
    return MouseRegion(
      cursor: f.onTap == null ? MouseCursor.defer : SystemMouseCursors.click,
      onEnter: (_) => setState(() => _hover = true),
      onExit: (_) => setState(() => _hover = false),
      child: GestureDetector(
        onTap: f.onTap,
        behavior: HitTestBehavior.opaque,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 120),
          decoration: BoxDecoration(
            color: f.seleccionada
                ? C.accentSoft
                : (_hover ? const Color(0x05FFFFFF) : Colors.transparent),
            border: const Border(top: BorderSide(color: C.border)),
          ),
          child: widget.child,
        ),
      ),
    );
  }
}

// ---------------------------------------------------------------- Diálogos

Future<bool> confirmar(
  BuildContext context,
  String mensaje, {
  String accion = 'Eliminar',
}) async {
  final r = await showDialog<bool>(
    context: context,
    builder: (context) => AlertDialog(
      content: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 420),
        child: Text(mensaje, style: T.texto()),
      ),
      actions: [
        Boton.fantasma(
          'Cancelar',
          onPressed: () => Navigator.pop(context, false),
        ),
        Boton.peligro(accion, onPressed: () => Navigator.pop(context, true)),
      ],
    ),
  );
  return r ?? false;
}

/// Mueve la vista para que [clave] quede visible (equivalente a scrollIntoView).
void llevarAVista(GlobalKey clave) {
  WidgetsBinding.instance.addPostFrameCallback((_) {
    final ctx = clave.currentContext;
    if (ctx != null) {
      Scrollable.ensureVisible(
        ctx,
        duration: const Duration(milliseconds: 300),
        alignmentPolicy: ScrollPositionAlignmentPolicy.keepVisibleAtStart,
      );
    }
  });
}
