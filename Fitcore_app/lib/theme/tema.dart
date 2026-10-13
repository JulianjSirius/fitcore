import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

/// Tokens de color de fitcore-web (src/styles.css).
abstract final class C {
  static const bg = Color(0xFF0D0F11);
  static const surface = Color(0xFF15181B);
  static const surface2 = Color(0xFF1C2024);
  static const surface3 = Color(0xFF242A2F);
  static const border = Color(0xFF2A3036);
  static const borderStrong = Color(0xFF3A424A);
  static const text = Color(0xFFF1F4F5);
  static const muted = Color(0xFF98A2AB);
  static const faint = Color(0xFF6B757E);
  static const accent = Color(0xFFC8FF3D);
  static const accentHover = Color(0xFFD6FF6B);
  static const accentInk = Color(0xFF0D0F11);
  static const accentSoft = Color(0x1FC8FF3D);
  static const ok = Color(0xFF4ADE80);
  static const okSoft = Color(0x1F4ADE80);
  static const warn = Color(0xFFFBBF24);
  static const warnSoft = Color(0x1FFBBF24);
  static const danger = Color(0xFFFF6B6B);
  static const dangerSoft = Color(0x1FFF6B6B);
  static const info = Color(0xFF60A5FA);
  static const infoSoft = Color(0x1F60A5FA);
}

abstract final class R {
  static const radius = 14.0;
  static const radiusSm = 9.0;
}

/// Tipografías: Inter (texto), Barlow Condensed (títulos) y JetBrains Mono (códigos y cifras).
abstract final class T {
  static TextStyle texto({double size = 15, FontWeight weight = FontWeight.w400, Color color = C.text}) =>
      GoogleFonts.inter(fontSize: size, fontWeight: weight, color: color, height: 1.45);

  static TextStyle display({double size = 32, FontWeight weight = FontWeight.w800, Color color = C.text}) =>
      GoogleFonts.barlowCondensed(fontSize: size, fontWeight: weight, color: color, height: 1.0);

  static TextStyle mono({double size = 13, FontWeight weight = FontWeight.w500, Color color = C.text}) =>
      GoogleFonts.jetBrainsMono(fontSize: size, fontWeight: weight, color: color);

  /// Rótulo en mayúsculas con espaciado (eyebrow, títulos de grupo, encabezados de tabla).
  static TextStyle rotulo({double size = 11.5, Color color = C.accent, double espaciado = 0.16}) =>
      GoogleFonts.inter(
        fontSize: size,
        fontWeight: FontWeight.w700,
        color: color,
        letterSpacing: size * espaciado,
      );
}

ThemeData temaFitCore() {
  final base = ThemeData(
    useMaterial3: true,
    brightness: Brightness.dark,
    scaffoldBackgroundColor: C.bg,
    colorScheme: const ColorScheme.dark(
      primary: C.accent,
      onPrimary: C.accentInk,
      secondary: C.accent,
      surface: C.surface,
      onSurface: C.text,
      error: C.danger,
    ),
  );
  return base.copyWith(
    textTheme: base.textTheme.apply(fontFamily: GoogleFonts.inter().fontFamily, bodyColor: C.text, displayColor: C.text),
    textSelectionTheme: const TextSelectionThemeData(
      cursorColor: C.accent,
      selectionColor: Color(0x66C8FF3D),
      selectionHandleColor: C.accent,
    ),
    dividerColor: C.border,
    scrollbarTheme: ScrollbarThemeData(
      thumbColor: WidgetStateProperty.all(C.borderStrong),
      radius: const Radius.circular(8),
    ),
    tooltipTheme: TooltipThemeData(
      decoration: BoxDecoration(color: C.surface3, borderRadius: BorderRadius.circular(6)),
      textStyle: T.texto(size: 12),
    ),
    dialogTheme: DialogThemeData(
      backgroundColor: C.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(R.radius),
        side: const BorderSide(color: C.border),
      ),
    ),
    inputDecorationTheme: InputDecorationTheme(
      isDense: true,
      filled: true,
      fillColor: C.bg,
      contentPadding: const EdgeInsets.symmetric(horizontal: 13, vertical: 13),
      hintStyle: T.texto(size: 14, color: C.faint),
      border: _borde(C.border),
      enabledBorder: _borde(C.border),
      hoverColor: Colors.transparent,
      focusedBorder: _borde(C.accent),
      disabledBorder: _borde(C.border.withValues(alpha: 0.5)),
      errorBorder: _borde(C.danger),
      focusedErrorBorder: _borde(C.danger),
    ),
    checkboxTheme: CheckboxThemeData(
      fillColor: WidgetStateProperty.resolveWith((s) => s.contains(WidgetState.selected) ? C.accent : Colors.transparent),
      checkColor: WidgetStateProperty.all(C.accentInk),
      side: const BorderSide(color: C.borderStrong, width: 1.5),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
    ),
    radioTheme: RadioThemeData(fillColor: WidgetStateProperty.all(C.accent)),
    progressIndicatorTheme: const ProgressIndicatorThemeData(color: C.accent),
  );
}

OutlineInputBorder _borde(Color color) => OutlineInputBorder(
      borderRadius: BorderRadius.circular(R.radiusSm),
      borderSide: BorderSide(color: color),
    );
