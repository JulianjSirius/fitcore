// Guarda la sesión: en web usa sessionStorage (igual que fitcore-web); en otras
// plataformas queda en memoria.
export 'almacen_memoria.dart' if (dart.library.js_interop) 'almacen_web.dart';
