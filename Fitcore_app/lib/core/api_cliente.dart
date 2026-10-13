import 'dart:convert';

import 'package:http/http.dart' as http;

import 'config.dart';

/// Error HTTP de la API. [estado] 0 significa que no hubo conexión.
class ApiException implements Exception {
  ApiException(this.estado, this.cuerpo);
  final int estado;
  final dynamic cuerpo;

  @override
  String toString() => 'ApiException($estado)';
}

/// Cliente del Gateway: agrega el token y avisa cuando la sesión venció (401).
class ApiCliente {
  ApiCliente({required this.token, required this.alExpirarSesion});

  final String? Function() token;
  final void Function() alExpirarSesion;
  final http.Client _http = http.Client();

  Future<dynamic> get(String ruta) => _enviar('GET', ruta);
  Future<dynamic> post(String ruta, [Object? cuerpo]) => _enviar('POST', ruta, cuerpo);
  Future<dynamic> put(String ruta, [Object? cuerpo]) => _enviar('PUT', ruta, cuerpo);
  Future<dynamic> delete(String ruta) => _enviar('DELETE', ruta);

  Future<dynamic> _enviar(String metodo, String ruta, [Object? cuerpo]) async {
    final peticion = http.Request(metodo, Uri.parse('$apiUrl$ruta'));
    final t = token();
    peticion.headers['Accept'] = 'application/json';
    if (t != null) peticion.headers['Authorization'] = 'Bearer $t';
    if (cuerpo != null) {
      peticion.headers['Content-Type'] = 'application/json';
      peticion.body = jsonEncode(cuerpo);
    }

    final http.Response respuesta;
    try {
      respuesta = await http.Response.fromStream(await _http.send(peticion));
    } catch (_) {
      throw ApiException(0, null);
    }

    final texto = utf8.decode(respuesta.bodyBytes);
    dynamic datos;
    if (texto.isNotEmpty) {
      try {
        datos = jsonDecode(texto);
      } catch (_) {
        datos = texto;
      }
    }

    if (respuesta.statusCode >= 200 && respuesta.statusCode < 300) return datos;

    // Un 401 con sesión significa token vencido: se cierra la sesión.
    // Los 403 los maneja cada pantalla (son reglas de negocio).
    if (respuesta.statusCode == 401 && t != null) alExpirarSesion();
    throw ApiException(respuesta.statusCode, datos);
  }
}

/// Convierte la respuesta de error de la API en un texto para mostrar.
/// Cubre { message } de los controladores y los ProblemDetails de validación de ASP.NET.
String mensajeError(Object error, String porDefecto) {
  if (error is! ApiException) return porDefecto;
  final cuerpo = error.cuerpo;
  if (cuerpo is Map) {
    final mensaje = cuerpo['message'];
    if (mensaje is String && mensaje.isNotEmpty) return mensaje;
    final errores = cuerpo['errors'];
    if (errores is Map) {
      for (final lista in errores.values) {
        if (lista is List && lista.isNotEmpty) return '${lista.first}';
      }
    }
  }
  if (error.estado == 0) return 'No hay conexión con el servidor. Revisa que el Gateway esté corriendo.';
  if (error.estado == 403) return 'No tienes permiso para esta acción.';
  if (error.estado == 404) return 'No se encontró el registro.';
  return porDefecto;
}
