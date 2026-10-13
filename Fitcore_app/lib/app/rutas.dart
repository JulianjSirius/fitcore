import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../core/sesion.dart';
import '../features/administradores.dart';
import '../features/auth/login.dart';
import '../features/auth/registro.dart';
import '../features/codigos.dart';
import '../features/control_acceso.dart';
import '../features/ejercicios.dart';
import '../features/entrenadores.dart';
import '../features/inicio.dart';
import '../features/logros.dart';
import '../features/membresias.dart';
import '../features/mi_acceso.dart';
import '../features/perfil.dart';
import '../features/planes.dart';
import '../features/rutinas.dart';
import '../features/usuarios.dart';
import 'marco.dart';

const _publicas = {'/login', '/registro'};

GoRoute _ruta(String path, Widget Function(GoRouterState s) pantalla) => GoRoute(
      path: path,
      pageBuilder: (context, state) => NoTransitionPage(key: state.pageKey, child: pantalla(state)),
    );

final enrutador = GoRouter(
  initialLocation: '/dashboard',
  refreshListenable: sesion,
  redirect: (context, state) {
    final ruta = state.matchedLocation;
    final publica = _publicas.contains(ruta);
    if (!sesion.autenticado) {
      if (publica) return null;
      return Uri(path: '/login', queryParameters: {'returnUrl': state.uri.toString()}).toString();
    }
    if (publica) return state.uri.queryParameters['returnUrl'] ?? '/dashboard';
    // Rutas con `roles`: el resto vuelve al inicio.
    final roles = rolesPorRuta[ruta];
    if (roles != null && !sesion.tieneRol(roles)) return '/dashboard';
    return null;
  },
  onException: (context, state, router) => router.go('/dashboard'),
  routes: [
    GoRoute(path: '/', redirect: (_, _) => '/dashboard'),
    // Ruta anterior: el panel se dividió en Membresías, Planes, Códigos y Administradores.
    GoRoute(path: '/administracion', redirect: (_, _) => '/membresias'),
    _ruta('/login', (_) => const LoginPantalla()),
    _ruta('/registro', (_) => const RegistroPantalla()),
    ShellRoute(
      builder: (context, state, child) => Marco(ruta: state.matchedLocation, child: child),
      routes: [
        _ruta('/dashboard', (_) => const InicioPantalla()),
        _ruta('/rutinas', (_) => const RutinasPantalla()),
        _ruta('/ejercicios', (_) => const EjerciciosPantalla()),
        _ruta('/planes', (_) => const PlanesPantalla()),
        _ruta('/perfil', (_) => const PerfilPantalla()),
        _ruta('/mi-acceso', (_) => const MiAccesoPantalla()),
        _ruta('/logros', (_) => const LogrosPantalla()),
        _ruta('/control-acceso', (_) => const ControlAccesoPantalla()),
        _ruta('/usuarios', (_) => const UsuariosPantalla()),
        _ruta('/entrenadores', (_) => const EntrenadoresPantalla()),
        _ruta('/membresias', (s) => MembresiasPantalla(usuarioInicial: s.uri.queryParameters['usuario'])),
        _ruta('/codigos', (_) => const CodigosPantalla()),
        _ruta('/administradores', (_) => const AdministradoresPantalla()),
      ],
    ),
  ],
);
