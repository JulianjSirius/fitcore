import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../app/marco.dart';
import '../../core/api_cliente.dart';
import '../../core/apis.dart';
import '../../theme/tema.dart';
import '../../ui/componentes.dart';
import 'auth_layout.dart';

// El registro público es solo para usuarios y exige el código que entrega un
// administrador. Entrenadores y administradores se crean desde /entrenadores y /administradores.
class RegistroPantalla extends StatefulWidget {
  const RegistroPantalla({super.key});

  @override
  State<RegistroPantalla> createState() => _RegistroPantallaState();
}

class _RegistroPantallaState extends State<RegistroPantalla> {
  final _codigo = TextEditingController();
  final _nombre = TextEditingController();
  final _apellido = TextEditingController();
  final _email = TextEditingController();
  final _telefono = TextEditingController();
  final _contrasena = TextEditingController();
  bool _cargando = false;
  String _error = '';
  String _exito = '';

  List<TextEditingController> get _campos => [_codigo, _nombre, _apellido, _email, _telefono, _contrasena];

  @override
  void initState() {
    super.initState();
    for (final c in _campos) {
      c.addListener(() => setState(() {}));
    }
  }

  @override
  void dispose() {
    for (final c in _campos) {
      c.dispose();
    }
    super.dispose();
  }

  bool get _valido {
    final tel = int.tryParse(_telefono.text) ?? 0;
    return _codigo.text.trim().isNotEmpty &&
        _nombre.text.trim().isNotEmpty &&
        _apellido.text.trim().isNotEmpty &&
        _email.text.contains('@') &&
        tel >= 1000000000 &&
        tel <= 9999999999 &&
        _contrasena.text.length >= 6;
  }

  Future<void> _registrar() async {
    if (!_valido || _cargando) return;
    setState(() {
      _cargando = true;
      _error = '';
      _exito = '';
    });
    try {
      await IdentityApi.registrarUsuario({
        'firstName': _nombre.text.trim(),
        'lastName': _apellido.text.trim(),
        'email': _email.text.trim(),
        'contrasena': _contrasena.text,
        'telefono': int.parse(_telefono.text),
        'codigoRegistro': _codigo.text.trim(),
      });
      if (!mounted) return;
      setState(() {
        _cargando = false;
        _exito = 'Cuenta creada correctamente. Ahora puedes iniciar sesión.';
      });
      await Future<void>.delayed(const Duration(milliseconds: 1200));
      if (mounted) context.go('/login');
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _cargando = false;
        _error = mensajeError(e, 'No se pudo crear la cuenta. Verifica los datos e inténtalo de nuevo.');
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return AuthLayout(
      arriba: MouseRegion(
        cursor: SystemMouseCursors.click,
        child: GestureDetector(
          onTap: () => context.go('/login'),
          child: Text('← Volver a iniciar sesión', style: T.texto(size: 14, color: C.muted)),
        ),
      ),
      eyebrow: 'Registro de usuarios',
      tituloLinea1: 'Empieza',
      tituloResaltado: 'hoy.',
      descripcion:
          'Pide en recepción tu código de registro. Con él creas tu cuenta, y cuando registren tu pago tendrás tu acceso con QR y tus rutinas.',
      abajo: const Marca(size: 28.8),
      titulo: 'Crear cuenta',
      subtitulo: 'El código es de un solo uso y lo entrega un administrador.',
      formulario: AutofillGroup(
        child: Bloques(
          espacio: 14.4,
          children: [
            Campo(etiqueta: 'Código de registro', controller: _codigo, hint: 'K7M2QX9PLA', mono: true),
            FormularioRejilla(children: [
              Campo(etiqueta: 'Nombre', controller: _nombre, autofill: const [AutofillHints.givenName]),
              Campo(etiqueta: 'Apellido', controller: _apellido, autofill: const [AutofillHints.familyName]),
            ]),
            Campo(
              etiqueta: 'Correo electrónico',
              controller: _email,
              hint: 'tu@correo.com',
              teclado: TextInputType.emailAddress,
              autofill: const [AutofillHints.email],
            ),
            Campo(
              etiqueta: 'Teléfono (10 dígitos)',
              controller: _telefono,
              hint: '3001234567',
              soloDigitos: true,
              maxLength: 10,
              autofill: const [AutofillHints.telephoneNumber],
            ),
            Campo(
              etiqueta: 'Contraseña',
              controller: _contrasena,
              hint: 'Mínimo 6 caracteres',
              oculto: true,
              autofill: const [AutofillHints.newPassword],
              onSubmitted: (_) => _registrar(),
            ),
            if (_error.isNotEmpty) Alerta(_error),
            if (_exito.isNotEmpty) Alerta.ok(_exito),
            Boton.primario(
              _cargando ? 'Registrando…' : 'Crear cuenta',
              bloque: true,
              grande: true,
              onPressed: _valido && !_cargando ? _registrar : null,
            ),
          ],
        ),
      ),
    );
  }
}
