import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../app/marco.dart';
import '../../core/apis.dart';
import '../../core/formato.dart';
import '../../core/sesion.dart';
import '../../models/modelos.dart';
import '../../theme/tema.dart';
import '../../ui/componentes.dart';
import 'auth_layout.dart';

class LoginPantalla extends StatefulWidget {
  const LoginPantalla({super.key});

  @override
  State<LoginPantalla> createState() => _LoginPantallaState();
}

class _LoginPantallaState extends State<LoginPantalla> {
  final _email = TextEditingController();
  final _contrasena = TextEditingController();
  TipoCuenta _tipo = TipoCuenta.usuario;
  bool _cargando = false;
  String _error = '';
  List<Plan> _planes = [];

  @override
  void initState() {
    super.initState();
    // Los precios son públicos; si la API no responde, el login funciona igual.
    oVacio(MembresiasApi.planes()).then((p) {
      if (mounted) setState(() => _planes = p.where((x) => x.activo).toList());
    });
    _email.addListener(() => setState(() {}));
    _contrasena.addListener(() => setState(() {}));
  }

  @override
  void dispose() {
    _email.dispose();
    _contrasena.dispose();
    super.dispose();
  }

  bool get _valido => _email.text.trim().contains('@') && _contrasena.text.isNotEmpty;

  Future<void> _entrar() async {
    if (!_valido || _cargando) return;
    setState(() {
      _cargando = true;
      _error = '';
    });
    try {
      // Al iniciar sesión el enrutador lleva al inicio (o a returnUrl).
      await sesion.iniciar(_tipo, _email.text.trim(), _contrasena.text);
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _cargando = false;
        _error = 'No pudimos validar tus credenciales. Revisa el tipo de cuenta, el correo y la contraseña.';
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return AuthLayout(
      arriba: const Marca(size: 28.8),
      eyebrow: 'Tu gimnasio, en un solo lugar',
      tituloLinea1: 'Entrena\nsin',
      tituloResaltado: 'límites.',
      descripcion: 'Rutinas, membresías y acceso con QR para usuarios, entrenadores y administradores.',
      abajo: _planes.isEmpty
          ? const SizedBox()
          : Rejilla(
              minAncho: 150,
              maxColumnas: 3,
              espacio: 12,
              children: [for (final p in _planes) _PlanMini(p)],
            ),
      titulo: 'Iniciar sesión',
      subtitulo: 'Elige tu tipo de cuenta e ingresa tus credenciales.',
      formulario: AutofillGroup(
        child: Bloques(
          espacio: 14.4,
          children: [
            Pestanas<TipoCuenta>(
              completo: true,
              opciones: [for (final t in TipoCuenta.values) (t, t.texto)],
              seleccion: _tipo,
              onChanged: (t) => setState(() => _tipo = t),
            ),
            Campo(
              etiqueta: 'Correo electrónico',
              controller: _email,
              hint: 'tu@correo.com',
              teclado: TextInputType.emailAddress,
              autofill: const [AutofillHints.email],
              onSubmitted: (_) => _entrar(),
            ),
            Campo(
              etiqueta: 'Contraseña',
              controller: _contrasena,
              hint: '••••••••',
              oculto: true,
              autofill: const [AutofillHints.password],
              onSubmitted: (_) => _entrar(),
            ),
            if (_error.isNotEmpty) Alerta(_error),
            Boton.primario(
              _cargando ? 'Validando…' : 'Entrar',
              bloque: true,
              grande: true,
              onPressed: _valido && !_cargando ? _entrar : null,
            ),
            const SizedBox(height: 10),
            Center(
              child: Wrap(
                crossAxisAlignment: WrapCrossAlignment.center,
                children: [
                  Text('¿Tienes un código de registro? ', style: T.texto(size: 14.4, color: C.muted)),
                  MouseRegion(
                    cursor: SystemMouseCursors.click,
                    child: GestureDetector(
                      onTap: () => context.go('/registro'),
                      child: Text('Crea tu cuenta', style: T.texto(size: 14.4, color: C.accent).copyWith(decoration: TextDecoration.underline, decorationColor: C.accent)),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _PlanMini extends StatelessWidget {
  const _PlanMini(this.plan);
  final Plan plan;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: C.surface.withValues(alpha: 0.75),
        borderRadius: BorderRadius.circular(R.radius),
        border: Border.all(color: C.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(plan.nombre.toUpperCase(), style: T.rotulo(size: 12, color: C.muted, espaciado: 0.1)),
          const SizedBox(height: 3),
          Text(moneda(plan.precio), style: T.display(size: 27, weight: FontWeight.w700)),
          const SizedBox(height: 3),
          Text(
            '${plan.duracionMeses} ${plural(plan.duracionMeses, 'mes', 'meses')}'
            '${plan.maxBeneficiarios > 1 ? ' · ${plan.maxBeneficiarios} personas' : ''}',
            style: T.texto(size: 13, color: C.faint),
          ),
        ],
      ),
    );
  }
}
