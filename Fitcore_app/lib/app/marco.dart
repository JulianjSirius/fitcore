import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../core/sesion.dart';
import '../models/modelos.dart';
import '../theme/tema.dart';
import '../ui/componentes.dart';
import '../ui/icono.dart';

class _Enlace {
  const _Enlace(this.ruta, this.texto, this.icono, this.roles);
  final String ruta;
  final String texto;
  final String icono;
  final List<Rol> roles;
}

const _todos = Rol.values;
const _equipo = [Rol.entrenador, Rol.administrador];
const _admin = [Rol.administrador];

const _menu = <(String, List<_Enlace>)>[
  ('General', [
    _Enlace('/dashboard', 'Inicio', 'inicio', _todos),
    _Enlace('/mi-acceso', 'Mi acceso', 'qr', [Rol.usuario]),
    _Enlace('/control-acceso', 'Control de acceso', 'escanear', _equipo),
  ]),
  ('Entrenamiento', [
    _Enlace('/rutinas', 'Rutinas', 'rutinas', _todos),
    _Enlace('/ejercicios', 'Ejercicios', 'ejercicios', _todos),
    _Enlace('/logros', 'Retos y logros', 'trofeo', [Rol.usuario]),
  ]),
  ('Gimnasio', [
    _Enlace('/usuarios', 'Usuarios', 'usuarios', _equipo),
    _Enlace('/entrenadores', 'Entrenadores', 'entrenadores', _equipo),
    _Enlace('/membresias', 'Membresías y pagos', 'membresias', _admin),
    _Enlace('/planes', 'Planes', 'planes', _todos),
    _Enlace('/codigos', 'Códigos de registro', 'codigos', _admin),
    _Enlace('/administradores', 'Administradores', 'administradores', _admin),
  ]),
  ('Cuenta', [_Enlace('/perfil', 'Mi perfil', 'perfil', _todos)]),
];

/// Rutas que exigen un rol concreto (el resto solo pide sesión).
final rolesPorRuta = <String, List<Rol>>{
  for (final (_, enlaces) in _menu)
    for (final e in enlaces)
      if (e.roles.length < Rol.values.length) e.ruta: e.roles,
};

class Marca extends StatelessWidget {
  const Marca({super.key, this.size = 25.6});
  final double size;

  @override
  Widget build(BuildContext context) {
    final estilo = T.display(size: size).copyWith(letterSpacing: size * 0.06);
    return Text.rich(TextSpan(children: [
      TextSpan(text: 'FIT', style: estilo),
      TextSpan(text: 'CORE', style: estilo.copyWith(color: C.accent)),
    ]));
  }
}

/// Estructura con sesión: barra lateral fija en escritorio y menú desplegable en móvil.
class Marco extends StatelessWidget {
  const Marco({super.key, required this.ruta, required this.child});

  final String ruta;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final movil = MediaQuery.sizeOf(context).width < 900;
    if (!movil) {
      return Scaffold(
        body: Row(
          children: [
            SizedBox(width: 260, child: _BarraLateral(ruta: ruta)),
            Expanded(child: child),
          ],
        ),
      );
    }
    return Scaffold(
      drawer: Drawer(
        width: 290,
        backgroundColor: C.surface,
        shape: const RoundedRectangleBorder(),
        child: _BarraLateral(ruta: ruta, alNavegar: () => Navigator.of(context).maybePop()),
      ),
      drawerScrimColor: Colors.black.withValues(alpha: 0.6),
      body: Column(
        children: [
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 11),
            decoration: const BoxDecoration(color: C.surface, border: Border(bottom: BorderSide(color: C.border))),
            child: SafeArea(
              bottom: false,
              child: Row(
                children: [
                  GestureDetector(onTap: () => context.go('/dashboard'), child: const Marca()),
                  const Spacer(),
                  Builder(
                    builder: (context) => Boton.fantasma('', icono: 'menu', chico: true, tooltip: 'Menú', onPressed: () => Scaffold.of(context).openDrawer()),
                  ),
                ],
              ),
            ),
          ),
          Expanded(child: child),
        ],
      ),
    );
  }
}

class _BarraLateral extends StatelessWidget {
  const _BarraLateral({required this.ruta, this.alNavegar});

  final String ruta;
  final VoidCallback? alNavegar;

  @override
  Widget build(BuildContext context) {
    final rol = sesion.rol;
    final usuario = sesion.usuario;
    final grupos = [
      for (final (titulo, enlaces) in _menu)
        (titulo, enlaces.where((e) => rol != null && e.roles.contains(rol)).toList()),
    ].where((g) => g.$2.isNotEmpty);
    final email = usuario?.email ?? '?';

    return Container(
      decoration: const BoxDecoration(color: C.surface, border: Border(right: BorderSide(color: C.border))),
      padding: const EdgeInsets.fromLTRB(16, 24, 16, 16),
      child: SafeArea(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 9.6),
              child: Align(
                alignment: Alignment.centerLeft,
                child: GestureDetector(
                  onTap: () {
                    alNavegar?.call();
                    context.go('/dashboard');
                  },
                  child: const MouseRegion(cursor: SystemMouseCursors.click, child: Marca()),
                ),
              ),
            ),
            const SizedBox(height: 24),
            Expanded(
              child: ListView(
                padding: EdgeInsets.zero,
                children: [
                  for (final (titulo, enlaces) in grupos) ...[
                    Padding(
                      padding: const EdgeInsets.fromLTRB(11.2, 0, 11.2, 5.6),
                      child: Text(titulo.toUpperCase(), style: T.rotulo(size: 10.9, color: C.faint, espaciado: 0.14)),
                    ),
                    for (final e in enlaces)
                      _EnlaceMenu(
                        enlace: e,
                        activo: ruta == e.ruta || (e.ruta != '/dashboard' && ruta.startsWith('${e.ruta}/')),
                        onTap: () {
                          alNavegar?.call();
                          context.go(e.ruta);
                        },
                      ),
                    const SizedBox(height: 20),
                  ],
                ],
              ),
            ),
            Container(
              padding: const EdgeInsets.fromLTRB(9.6, 12, 9.6, 0),
              decoration: const BoxDecoration(border: Border(top: BorderSide(color: C.border))),
              child: Row(
                children: [
                  Avatar(email.substring(0, email.length < 2 ? email.length : 2).toUpperCase()),
                  const SizedBox(width: 10.4),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(email, style: T.texto(size: 12.8, weight: FontWeight.w600), overflow: TextOverflow.ellipsis, maxLines: 1),
                        Text(
                          '${rol?.texto ?? ''}${usuario?.nivelAcceso != null ? ' · ${usuario!.nivelAcceso}' : ''}',
                          style: T.texto(size: 11.5, color: C.muted),
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                    ),
                  ),
                  Boton.fantasma('', icono: 'salir', chico: true, tooltip: 'Cerrar sesión', onPressed: sesion.cerrar),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _EnlaceMenu extends StatefulWidget {
  const _EnlaceMenu({required this.enlace, required this.activo, required this.onTap});

  final _Enlace enlace;
  final bool activo;
  final VoidCallback onTap;

  @override
  State<_EnlaceMenu> createState() => _EnlaceMenuState();
}

class _EnlaceMenuState extends State<_EnlaceMenu> {
  bool _hover = false;

  @override
  Widget build(BuildContext context) {
    final activo = widget.activo;
    final color = activo ? C.accentInk : (_hover ? C.text : C.muted);
    return Padding(
      padding: const EdgeInsets.only(bottom: 2.4),
      child: MouseRegion(
        cursor: SystemMouseCursors.click,
        onEnter: (_) => setState(() => _hover = true),
        onExit: (_) => setState(() => _hover = false),
        child: GestureDetector(
          onTap: widget.onTap,
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 150),
            padding: const EdgeInsets.symmetric(horizontal: 11.2, vertical: 8.8),
            decoration: BoxDecoration(
              color: activo ? C.accent : (_hover ? C.surface2 : Colors.transparent),
              borderRadius: BorderRadius.circular(R.radiusSm),
            ),
            child: Row(
              children: [
                Icono(widget.enlace.icono, color: color),
                const SizedBox(width: 11.2),
                Expanded(
                  child: Text(
                    widget.enlace.texto,
                    style: T.texto(size: 14.4, weight: activo ? FontWeight.w700 : FontWeight.w500, color: color),
                    overflow: TextOverflow.ellipsis,
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
