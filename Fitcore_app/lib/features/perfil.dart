import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../core/api_cliente.dart';
import '../core/apis.dart';
import '../core/formato.dart';
import '../core/sesion.dart';
import '../models/modelos.dart';
import '../theme/tema.dart';
import '../ui/componentes.dart';

class PerfilPantalla extends StatefulWidget {
  const PerfilPantalla({super.key});

  @override
  State<PerfilPantalla> createState() => _PerfilPantallaState();
}

class _PerfilPantallaState extends State<PerfilPantalla> {
  bool _cargando = true;
  String _error = '';
  String? _nombre;
  List<(String, Object)> _datos = [];
  Membresia? _membresia;

  Rol? get _rol => sesion.rol;

  @override
  void initState() {
    super.initState();
    _cargar();
  }

  Future<void> _cargar() async {
    final id = sesion.usuario?.id;
    if (id == null) {
      setState(() => _cargando = false);
      return;
    }
    if (_rol == Rol.usuario) {
      oNulo(MembresiasApi.miMembresia()).then((m) {
        if (mounted) setState(() => _membresia = m);
      });
    }
    try {
      switch (_rol) {
        case Rol.usuario:
          final u = await IdentityApi.usuario(id);
          _nombre = u.nombreCompleto;
          _datos = [('Correo', u.email), ('Teléfono', '${u.telefono}')];
        case Rol.entrenador:
          final e = await IdentityApi.entrenador(id);
          _nombre = e.nombre;
          _datos = [('Correo', e.email), ('Teléfono', '${e.telefono}'), ('Especialidad', e.especialidad), ('Horario', e.horario)];
        default:
          final a = await IdentityApi.administrador(id);
          _nombre = '${a.nombre} ${a.lastName}';
          _datos = [('Correo', a.email), ('Teléfono', '${a.telefono}'), ('Nivel de acceso', a.nivelAcceso)];
      }
    } catch (e) {
      _error = mensajeError(e, 'No se pudo cargar tu perfil.');
    }
    if (mounted) setState(() => _cargando = false);
  }

  String get _iniciales => (_nombre ?? '?')
      .split(' ')
      .where((p) => p.isNotEmpty)
      .map((p) => p[0])
      .take(2)
      .join()
      .toUpperCase();

  @override
  Widget build(BuildContext context) {
    final m = _membresia;
    return Pagina(children: [
      const Encabezado(
        eyebrow: 'Cuenta',
        titulo: 'Mi perfil',
        descripcion: 'Tus datos en FitCore. Si necesitas cambiarlos, pídeselo a un administrador.',
      ),
      if (_cargando)
        const Cargando()
      else if (_error.isNotEmpty)
        Alerta(_error)
      else if (_nombre != null)
        Dividido(
          principal: Tarjeta(
            child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
              Row(children: [
                Avatar(_iniciales, size: 64, fondo: C.accent, tinta: C.accentInk),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Text(_nombre!, style: T.display(size: 29)),
                    const SizedBox(height: 6),
                    Etiqueta(_rol?.texto ?? '', tono: Tono.accent),
                  ]),
                ),
              ]),
              const SizedBox(height: 20),
              Detalles(_datos),
            ]),
          ),
          lateral: Bloques(children: [
            if (_rol == Rol.usuario)
              Tarjeta(
                child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                  CabeceraTarjeta(
                    titulo: 'Membresía',
                    trailing: m == null ? null : Etiqueta(m.estado, tono: m.activa ? Tono.ok : Tono.danger),
                  ),
                  if (m != null) ...[
                    Detalles([('Plan', m.planNombre), ('Vence', fechaLarga(m.fechaVencimiento))]),
                    const SizedBox(height: 12),
                    Align(
                      alignment: Alignment.centerLeft,
                      child: Boton('Ver mi QR', icono: 'qr', chico: true, onPressed: () => context.go('/mi-acceso')),
                    ),
                  ] else ...[
                    Text('Aún no tienes membresía. Regístrala en recepción.', style: T.texto(color: C.muted)),
                    const SizedBox(height: 12),
                    Align(
                      alignment: Alignment.centerLeft,
                      child: Boton('Ver planes', chico: true, onPressed: () => context.go('/planes')),
                    ),
                  ],
                ]),
              ),
            Tarjeta(
              child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                const CabeceraTarjeta(titulo: 'Sesión'),
                Text(
                  'Por seguridad, la sesión se guarda solo en esta pestaña y el token vence a las 2 horas.',
                  style: T.texto(color: C.muted),
                ),
                const SizedBox(height: 14),
                Align(
                  alignment: Alignment.centerLeft,
                  child: Boton.peligro('Cerrar sesión', icono: 'salir', onPressed: sesion.cerrar),
                ),
              ]),
            ),
          ]),
        ),
    ]);
  }
}
