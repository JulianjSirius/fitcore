import 'package:fitcore_app/core/api_cliente.dart';
import 'package:fitcore_app/models/modelos.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('mensajeError usa el message de la API', () {
    expect(mensajeError(ApiException(400, {'message': 'Código vencido'}), 'x'), 'Código vencido');
  });

  test('mensajeError toma el primer error de validación', () {
    final e = ApiException(400, {
      'errors': {
        'Email': ['El correo no es válido'],
      },
    });
    expect(mensajeError(e, 'x'), 'El correo no es válido');
  });

  test('mensajeError sin conexión', () {
    expect(mensajeError(ApiException(0, null), 'x'), contains('Gateway'));
  });

  test('Membresia vencida no tiene días restantes', () {
    final m = Membresia.fromJson({
      'estado': 'Vencida',
      'fechaInicio': '2026-01-01T00:00:00Z',
      'fechaVencimiento': '2026-02-01T00:00:00Z',
    });
    expect(m.diasRestantes, 0);
  });
}
