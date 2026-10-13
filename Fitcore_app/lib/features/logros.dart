import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../core/apis.dart';
import '../core/formato.dart';
import '../models/modelos.dart';
import '../theme/tema.dart';
import '../ui/componentes.dart';
import '../ui/icono.dart';

// Mismo tamaño que CalculadoraAsistencia.DiasRecientes en Identity.
const _diasCalendario = 42;
const _diasSemana = ['L', 'M', 'X', 'J', 'V', 'S', 'D'];

const _iconoInsignia = <String, String>{
  'primera-visita': 'check',
  'racha-7': 'fuego',
  'racha-30': 'fuego',
  'asistencias-50': 'estrella',
  'asistencias-100': 'estrella',
  'primera-mejora': 'rutinas',
  'primer-record': 'trofeo',
  'records-10': 'trofeo',
  'kg-50': 'rutinas',
  'repeticiones-100': 'rutinas',
};

String _iso(DateTime d) =>
    '${d.year}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

class LogrosPantalla extends StatefulWidget {
  const LogrosPantalla({super.key});

  @override
  State<LogrosPantalla> createState() => _LogrosPantallaState();
}

class _LogrosPantallaState extends State<LogrosPantalla> {
  bool _cargando = true;
  ResumenAsistencia? _asistencia;
  ResumenProgreso? _progreso;

  @override
  void initState() {
    super.initState();
    () async {
      final r = await (oNulo(LogrosApi.miAsistencia()), oNulo(LogrosApi.miProgreso())).wait;
      if (!mounted) return;
      setState(() {
        _asistencia = r.$1;
        _progreso = r.$2;
        _cargando = false;
      });
    }();
  }

  List<Logro> get _retos => [...?_asistencia?.retos, ...?_progreso?.retos];

  // Obtenidas primero (la más reciente arriba) y luego las que faltan, por avance.
  List<Logro> get _insignias {
    final lista = [...?_asistencia?.insignias, ...?_progreso?.insignias];
    lista.sort((a, b) {
      if (a.obtenido != b.obtenido) return a.obtenido ? -1 : 1;
      if (a.obtenido) {
        return (b.fechaObtencion ?? DateTime(0)).compareTo(a.fechaObtencion ?? DateTime(0));
      }
      return b.avance.compareTo(a.avance);
    });
    return lista;
  }

  @override
  Widget build(BuildContext context) {
    final a = _asistencia;
    final p = _progreso;
    final errorA = !_cargando && a == null;
    final errorP = !_cargando && p == null;
    final insignias = _insignias;

    return Pagina(children: [
      const Encabezado(
        eyebrow: 'Motivación',
        titulo: 'Retos y logros',
        descripcion: 'Tus rachas de asistencia, los récords que vas rompiendo y las insignias que desbloqueas.',
      ),
      if (_cargando)
        const Cargando()
      else ...[
        if (errorA || errorP)
          Alerta(
            'No se pudo cargar ${errorA && errorP ? 'tu información' : errorA ? 'tu asistencia' : 'tu progreso'}. '
            'Intenta de nuevo en un momento.',
          ),

        // ================= MÉTRICAS =================
        Rejilla(children: [
          Metrica(
            etiqueta: 'Racha actual',
            acento: true,
            valorWidget: Row(mainAxisSize: MainAxisSize.min, children: [
              const Icono('fuego', size: 30, color: C.accentInk),
              const SizedBox(width: 6),
              Text('${a?.rachaActual ?? 0}'),
            ]),
            pie: (a?.rachaActual ?? 0) == 1 ? 'día seguido' : 'días seguidos',
          ),
          Metrica(etiqueta: 'Mejor racha', valor: '${a?.mejorRacha ?? 0}', pie: 'días seguidos'),
          Metrica(etiqueta: 'Asistencias', valor: '${a?.totalAsistencias ?? 0}', pie: '${a?.asistenciasEsteMes ?? 0} este mes'),
          Metrica(
            etiqueta: 'Récords personales',
            valor: '${p?.recordsPersonales ?? 0}',
            pie: '+${decimal(p?.kgGanados ?? 0)} kg · +${p?.repeticionesGanadas ?? 0} reps',
          ),
        ]),

        Dividido(
          principal: Tarjeta(
            child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
              const CabeceraTarjeta(
                titulo: 'Últimas 6 semanas',
                subtitulo: 'Cada día que escaneas tu QR en la entrada suma a tu racha.',
              ),
              Align(alignment: Alignment.centerLeft, child: _Calendario(asistidos: a?.diasRecientes ?? const {})),
              const SizedBox(height: 14),
              Row(children: [
                const _Celda(asistio: true, tam: 14),
                const SizedBox(width: 6),
                Text('Asististe', style: T.texto(size: 13.6, color: C.muted)),
                const SizedBox(width: 12),
                const _Celda(hoy: true, tam: 14),
                const SizedBox(width: 6),
                Text('Hoy', style: T.texto(size: 13.6, color: C.muted)),
              ]),
            ]),
          ),
          lateral: Tarjeta(
            child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
              const CabeceraTarjeta(
                titulo: 'Retos del mes',
                subtitulo: 'Se reinician el primer día de cada mes.',
                trailing: Icono('diana', size: 22, color: C.text),
              ),
              if (_retos.isEmpty)
                Text('No hay retos disponibles.', style: T.texto(size: 13.6, color: C.muted))
              else
                Bloques(espacio: 17.6, children: [
                  for (final r in _retos)
                    Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                      Row(children: [
                        Expanded(child: Text(r.nombre, style: T.texto(weight: FontWeight.w700))),
                        if (r.obtenido)
                          const Etiqueta('¡Completado!', tono: Tono.ok)
                        else
                          Text('${r.progreso}/${r.meta}', style: T.mono(color: C.muted)),
                      ]),
                      const SizedBox(height: 4),
                      Text(r.descripcion, style: T.texto(size: 13, color: C.muted)),
                      const SizedBox(height: 6),
                      BarraProgreso(r.avance),
                    ]),
                ]),
            ]),
          ),
        ),

        // ================= INSIGNIAS =================
        Tarjeta(
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            CabeceraTarjeta(
              titulo: 'Insignias',
              subtitulo: '${insignias.where((i) => i.obtenido).length} de ${insignias.length} desbloqueadas.',
            ),
            Rejilla(minAncho: 230, maxColumnas: 4, espacio: 12.8, children: [for (final i in insignias) _Insignia(i)]),
          ]),
        ),

        // ================= HISTORIAL DE PROGRESO =================
        Tarjeta(
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            CabeceraTarjeta(
              titulo: 'Tus mejoras',
              subtitulo: 'Cada vez que subes repeticiones o peso en una rutina queda registrado aquí.',
              trailing: Boton('Ir a mis rutinas', chico: true, onPressed: () => context.go('/rutinas')),
            ),
            if ((p?.historial ?? []).isEmpty)
              Text(
                'Aún no hay mejoras. Edita una rutina y sube las repeticiones o el peso de un ejercicio.',
                style: T.texto(color: C.muted),
              )
            else
              ListaSeparada(espacio: 11.2, children: [
                for (final h in p!.historial)
                  Wrap(
                    alignment: WrapAlignment.spaceBetween,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    spacing: 16,
                    runSpacing: 8,
                    children: [
                      TituloCelda(h.nombreEjercicio, subtitulo: fechaMedia(h.fecha)),
                      Wrap(spacing: 6.4, runSpacing: 6.4, children: [
                        if (h.diferenciaRepeticiones > 0)
                          Etiqueta('+${h.diferenciaRepeticiones} reps (${h.repeticionesAnteriores} → ${h.repeticionesNuevas})', tono: Tono.ok),
                        if (h.diferenciaPesoKg > 0)
                          Etiqueta(
                            '+${decimal(h.diferenciaPesoKg)} kg (${decimal(h.pesoAnteriorKg)} → ${decimal(h.pesoNuevoKg)})',
                            tono: Tono.ok,
                          ),
                        if (h.esRecordPersonal) const Etiqueta('Récord', tono: Tono.accent, icono: 'trofeo'),
                      ]),
                    ],
                  ),
              ]),
          ]),
        ),
      ],
    ]);
  }
}

/// Últimas seis semanas; los huecos iniciales alinean el primer día con su columna (lunes primero).
class _Calendario extends StatelessWidget {
  const _Calendario({required this.asistidos});
  final Set<String> asistidos;

  @override
  Widget build(BuildContext context) {
    final hoy = DateTime.now();
    final inicio = DateTime(hoy.year, hoy.month, hoy.day - (_diasCalendario - 1));
    final huecos = inicio.weekday - 1;
    final celdas = <Widget>[
      for (final d in _diasSemana)
        Center(child: Text(d, style: T.texto(size: 11.2, weight: FontWeight.w600, color: C.faint))),
      for (var i = 0; i < huecos; i++) const SizedBox(),
      for (var i = 0; i < _diasCalendario; i++)
        Builder(builder: (context) {
          final fecha = DateTime(inicio.year, inicio.month, inicio.day + i);
          final asistio = asistidos.contains(_iso(fecha));
          return Tooltip(
            message: '${fechaCompleta(fecha)}${asistio ? ' · asististe' : ''}',
            child: _Celda(asistio: asistio, hoy: i == _diasCalendario - 1),
          );
        }),
    ];
    return ConstrainedBox(
      constraints: const BoxConstraints(maxWidth: 420),
      child: GridView.count(
        crossAxisCount: 7,
        shrinkWrap: true,
        physics: const NeverScrollableScrollPhysics(),
        mainAxisSpacing: 6.4,
        crossAxisSpacing: 6.4,
        children: celdas,
      ),
    );
  }
}

class _Celda extends StatelessWidget {
  const _Celda({this.asistio = false, this.hoy = false, this.tam});

  final bool asistio;
  final bool hoy;
  final double? tam;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: tam,
      height: tam,
      decoration: BoxDecoration(
        color: asistio ? C.accent : C.surface3,
        borderRadius: BorderRadius.circular(6),
        border: hoy ? Border.all(color: C.text, width: 2, strokeAlign: BorderSide.strokeAlignOutside) : null,
      ),
    );
  }
}

class _Insignia extends StatelessWidget {
  const _Insignia(this.logro);
  final Logro logro;

  @override
  Widget build(BuildContext context) {
    final i = logro;
    return Opacity(
      opacity: i.obtenido ? 1 : 0.75,
      child: Container(
        padding: const EdgeInsets.all(14.4),
        decoration: BoxDecoration(
          color: i.obtenido ? null : C.surface2,
          gradient: i.obtenido
              ? const LinearGradient(
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                  colors: [Color(0x1FC8FF3D), C.surface2],
                  stops: [0, 0.7],
                )
              : null,
          borderRadius: BorderRadius.circular(R.radiusSm),
          border: Border.all(color: i.obtenido ? C.accent : C.border),
        ),
        child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Container(
            width: 46,
            height: 46,
            alignment: Alignment.center,
            decoration: BoxDecoration(color: i.obtenido ? C.accent : C.surface3, shape: BoxShape.circle),
            child: Icono(
              i.obtenido ? (_iconoInsignia[i.codigo] ?? 'estrella') : 'candado',
              size: 24,
              color: i.obtenido ? C.accentInk : C.faint,
            ),
          ),
          const SizedBox(width: 12.8),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(i.nombre, style: T.texto(weight: FontWeight.w700)),
              const SizedBox(height: 3),
              Text(i.descripcion, style: T.texto(size: 13, color: C.muted)),
              const SizedBox(height: 3),
              if (i.obtenido)
                Text(
                  'Desbloqueada${i.fechaObtencion != null ? ' el ${fechaMedia(i.fechaObtencion!)}' : ''}',
                  style: T.texto(size: 13, weight: FontWeight.w600, color: C.accent),
                )
              else ...[
                const SizedBox(height: 4),
                BarraProgreso(i.avance, alto: 6),
                const SizedBox(height: 4),
                Text('${i.progreso}/${i.meta}', style: T.mono(size: 12, color: C.muted)),
              ],
            ]),
          ),
        ]),
      ),
    );
  }
}
