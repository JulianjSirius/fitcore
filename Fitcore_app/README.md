# FitCore App (Flutter web)

Versión en Flutter del frontend de FitCore. Replica el diseño y las pantallas de
`fitcore-web` (Angular): mismos colores, tipografías (Barlow Condensed, Inter,
JetBrains Mono), íconos, menú por rol y reglas de permisos.

## Ejecutar

Con Identity, Workouts y el Gateway (`http://localhost:5100`) corriendo:

```powershell
flutter run -d chrome --web-port 4300
```

El puerto **4300** es el que admiten los servicios en CORS (4200 es el de Angular).
Para apuntar a otro Gateway: `--dart-define=API_URL=http://servidor:5100/api`.

Compilar para publicar: `flutter build web` (sale en `build/web`; el servidor debe
devolver `index.html` en cualquier ruta, porque se usan URLs limpias).

## Estructura

- `lib/core/`: configuración, sesión JWT (sessionStorage), cliente HTTP, APIs y formatos es-CO.
- `lib/models/`: contratos de la API.
- `lib/theme/` y `lib/ui/`: tokens de diseño y componentes (tarjetas, botones, campos, tablas, rejillas).
- `lib/app/`: rutas protegidas por rol y la barra lateral.
- `lib/features/`: una pantalla por archivo (inicio, rutinas, ejercicios, planes, perfil,
  mi acceso con QR, logros, control de acceso con cámara, usuarios, entrenadores,
  membresías y pagos, códigos de registro y administradores).
