import 'package:web/web.dart' as web;

String? leer(String clave) => web.window.sessionStorage.getItem(clave);

void escribir(String clave, String valor) => web.window.sessionStorage.setItem(clave, valor);

void borrar(String clave) => web.window.sessionStorage.removeItem(clave);
