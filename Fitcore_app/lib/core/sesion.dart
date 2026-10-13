import 'dart:convert';

import 'package:flutter/foundation.dart';

import '../models/modelos.dart';
import 'almacen.dart' as almacen;
import 'api_cliente.dart';

const _claveToken = 'fitcore.access_token';
const _claveRol = 'fitcore.role';

/// Sesión del usuario: guarda el JWT y expone el rol para el menú y las rutas.
class Sesion extends ChangeNotifier {
  Sesion() {
    _token = almacen.leer(_claveToken);
    _rolGuardado = Rol.desde(almacen.leer(_claveRol));
    _usuario = _token == null ? null : _decodificar(_token!);
  }

  late final ApiCliente api = ApiCliente(token: () => _token, alExpirarSesion: cerrar);

  String? _token;
  Rol? _rolGuardado;
  SesionUsuario? _usuario;

  bool get autenticado => _token != null;
  SesionUsuario? get usuario => _usuario;
  Rol? get rol => _usuario?.rol ?? _rolGuardado;
  bool get esDueno => _usuario?.nivelAcceso == 'Dueño';

  bool tieneRol(List<Rol> roles) => rol != null && roles.contains(rol);

  Future<void> iniciar(TipoCuenta tipo, String email, String contrasena) async {
    final respuesta = await api.post('/Auth/login/${tipo.valor}', {
      'email': email,
      'contrasena': contrasena,
    }) as Json;
    _token = respuesta['token'] as String;
    _rolGuardado = Rol.desde(respuesta['role'] as String?);
    almacen.escribir(_claveToken, _token!);
    if (_rolGuardado != null) almacen.escribir(_claveRol, _rolGuardado!.texto);
    _usuario = _decodificar(_token!);
    notifyListeners();
  }

  void cerrar() {
    almacen.borrar(_claveToken);
    almacen.borrar(_claveRol);
    _token = null;
    _rolGuardado = null;
    _usuario = null;
    notifyListeners();
  }

  // El frontend solo lee el payload; la firma la valida la API.
  SesionUsuario? _decodificar(String token) {
    try {
      final partes = token.split('.');
      final bytes = base64Url.decode(base64Url.normalize(partes[1]));
      final payload = jsonDecode(utf8.decode(bytes)) as Json;
      final rol = Rol.desde(payload['Rol'] as String?) ?? _rolGuardado;
      if (rol == null) return null;
      return SesionUsuario(
        id: '${payload['sub']}',
        email: '${payload['email'] ?? ''}',
        rol: rol,
        nivelAcceso: payload['NivelAcceso'] as String?,
      );
    } catch (_) {
      return null;
    }
  }
}

/// Única sesión de la aplicación.
final sesion = Sesion();
