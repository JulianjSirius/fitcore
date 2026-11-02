# FitCore

Plataforma de gestión para gimnasios desarrollada con **.NET 10**, **Angular 22** y **Flutter**. Cubre el ciclo completo del socio: se registra con un código, paga su membresía, entra con un QR, entrena con rutinas personalizadas y se mantiene motivado con rachas e insignias.

Está construida como **microservicios** (Identity y Workouts) detrás de un **API Gateway**, cada uno con su propia base de datos PostgreSQL y aplicando **Clean Architecture** con **CQRS** (MediatR).


## Inicio rápido

```bash
cp .env.example .env        # crea .env con POSTGRES_PASSWORD
docker compose up -d


# 1. Servicios (una terminal cada uno)
dotnet run --project Services/Identity/FitCore.Identity.API
dotnet run --project Services/Workouts/FitCore.Workouts.API
dotnet run --project Services/Gateway/FitCore.Gateway

# 2. Cliente web
cd fitcore-web && npm install && npm start
```

Abre `http://localhost:4200`.

## ¿Qué es la arquitectura de FitCore?

Cada servicio se divide en cuatro proyectos con dependencias que apuntan hacia el dominio:

```
API  ──►  Application  ──►  Domain
 │             ▲
 └──►  Infrastructure ─────┘
```

| Capa | Responsabilidad |
|---|---|
| **API** | Controladores REST, autenticación JWT, Swagger, workers en segundo plano |
| **Application** | Casos de uso: *commands* y *queries* con MediatR, DTOs, reglas de negocio, interfaces |
| **Domain** | Entidades y constantes del dominio, sin dependencias externas |
| **Infrastructure** | EF Core + PostgreSQL, migraciones, hash de contraseñas, clientes de servicios externos |

El código de cada servicio se organiza **por funcionalidad** (`Features/Membresias`, `Features/Acceso`, …), con sus `Commands`, `Queries`, `Handlers` y `DTOs` juntos.

### Vista general del sistema

```
Angular (4200) ──┐
                 ├─►  Gateway YARP (5100) ─┬─►  Identity API (5231) ─►  PostgreSQL :5432
Flutter web(4300)┘                         └─►  Workouts API (5000) ─►  PostgreSQL :5433
                                                        │
                                                        └─►  Wger (catálogo externo de ejercicios)

RabbitMQ (provisionado en docker-compose, todavía sin uso)
```

### Flujo de una solicitud

```
Angular / Flutter
      │  POST /api/Membresia/pagos
      ▼
Gateway (YARP)              ← Enruta por prefijo de ruta
      ▼
MembresiaController         ← Valida el JWT y el rol, delega con MediatR
      ▼
RegistrarPagoManualHandler  ← Reglas de negocio, transacción
      ▼
MembresiaService            ← Aplica el pago y crea/renueva la membresía
      ▼
AppDbContext (EF Core)      → PostgreSQL
      ▼
HTTP 200 + JSON
```

## Estructura del proyecto

```
FitCore
├── Services/
│   ├── Gateway/FitCore.Gateway              ← Punto de entrada único (YARP)
│   ├── Identity/                            ← Cuentas, membresías, pagos, acceso, asistencia
│   │   ├── FitCore.Identity.API
│   │   ├── FitCore.Identity.Application
│   │   ├── FitCore.Identity.Domain
│   │   └── FitCore.Identity.Infrastructure
│   └── Workouts/                            ← Ejercicios, rutinas, progreso
│       ├── FitCore.Workouts.API
│       ├── FitCore.Workouts.Application
│       ├── FitCore.Workouts.Domain
│       └── FitCore.Workouts.Infrastructure
├── fitcore-web/                             ← Cliente Angular
├── Fitcore_app/                             ← Cliente Flutter (web, listo para Android/iOS)
├── tests/                                   ← xUnit: Identity, Workouts, Gateway
└── docker-compose.yml                       ← PostgreSQL x2
```

## Detalle de cada servicio

### Gateway
Enruta cada `/api/...` al microservicio correspondiente y expone `/health`. CORS y JWT los resuelve cada servicio.

| Prefijo | Destino |
|---|---|
| `/api/Auth`, `/api/Usuario`, `/api/Entrenador`, `/api/Administrador`, `/api/CodigoRegistro`, `/api/Plan`, `/api/Membresia`, `/api/Acceso` | Identity |
| `/api/Ejercicios`, `/api/Rutinas`, `/api/Progreso` | Workouts |

### Identity

| Carpeta (`Features/`) | Contenido |
|---|---|
| `Autenticacion` | Login por tipo de cuenta y emisión de JWT |
| `Usuarios` | Registro con código, consulta, edición, token de QR |
| `Entrenadores`, `Administradores` | CRUD de personal |
| `CodigosRegistro` | Códigos de un solo uso con vigencia |
| `Planes` | Tres planes fijos con precio y duración editables |
| `Membresias` | Pagos manuales, `MembresiaService`, renovaciones |
| `Acceso` | Validación del QR en recepción |
| `Logros` | Rachas, insignias y reto mensual de asistencia |

Entidades: `User`, `Entrenador`, `Administrador`, `CodigoRegistro`, `Plan`, `Membresia`, `Pago`, `Asistencia`.

### Workouts

| Carpeta (`Features/`) | Contenido |
|---|---|
| `Ejercicios` | Biblioteca poblada automáticamente desde Wger |
| `Rutinas` | Rutinas por usuario con series, repeticiones, peso y descanso |
| `Progreso` | Mejoras, récords personales e insignias de progreso |

Entidades: `Ejercicio`, `Rutina`, `RutinaEjercicio`, `ProgresoEjercicio`.

### Clientes
Angular y Flutter tienen las mismas pantallas: login, registro, dashboard, usuarios, entrenadores, administradores, códigos de registro, planes, membresías y pagos, ejercicios, rutinas, mi acceso (QR), control de acceso (escáner), logros y perfil. El menú y los permisos cambian según el rol.

## Tecnologías

| Tecnología | Versión | Uso |
|---|---|---|
| .NET / ASP.NET Core | 10.0 | APIs y Gateway |
| Entity Framework Core + Npgsql | 10.0 | Acceso a datos y migraciones |
| PostgreSQL | 15 | Una base por servicio |
| MediatR | 12.4 | CQRS |
| Mapster | 7.4 | Mapeo entre DTOs |
| YARP | 2.3 | API Gateway (reverse proxy) |
| JWT Bearer | 10.0 | Autenticación |
| Swashbuckle | 10.2 | Swagger (Development) |
| Angular | 22 | Cliente web (standalone, rutas perezosas) |
| TypeScript / RxJS / Vitest | 6.0 / 7.8 / 4 | Lógica y pruebas del cliente web |
| Flutter + go_router | — | Cliente multiplataforma |
| qr_flutter / mobile_scanner / angularx-qrcode | — | Generación y lectura de QR |
| Docker Compose | — | Infraestructura local |
| Wger API | — | Catálogo externo de ejercicios |
| Wompi | — | Pagos en línea (modelo preparado, sin activar) |
| xUnit + WebApplicationFactory | — | Pruebas de backend |

## Roles y permisos

| Capacidad | Usuario | Entrenador | Administrador | Dueño |
|---|:-:|:-:|:-:|:-:|
| Registrarse con código | ✅ | | | |
| Ver su membresía, QR, asistencia, logros y progreso | ✅ | | | |
| Listar usuarios y entrenadores | | ✅ | ✅ | ✅ |
| Validar QR en recepción | | ✅ | ✅ | ✅ |
| Gestionar ejercicios | | ✅ | ✅ | ✅ |
| Crear rutinas | propias | ✅ | | |
| Editar o borrar rutinas | propias | si el usuario lo permite | | |
| Emitir códigos de registro | | | ✅ | ✅ |
| Registrar pagos y ver historial | | | ✅ | ✅ |
| Modificar planes | | | ✅ | ✅ |
| Crear entrenadores | | | ✅ | ✅ |
| Editar usuarios y entrenadores | | | | ✅ |
| Crear, editar y eliminar administradores | | | | ✅ |

El **Dueño** es un administrador con el claim `NivelAcceso = Dueño`, que se emite únicamente desde la base de datos.

## Reglas de negocio

### Registro y autenticación
1. Un cliente no puede registrarse sin un **código de registro** entregado por un administrador.
2. El código es de **un solo uso** y tiene fecha de expiración; se reclama de forma atómica.
3. El login es **separado por tipo de cuenta** (usuario, entrenador, administrador).
4. Las contraseñas se guardan con **PBKDF2-SHA256 (600 000 iteraciones)**; las antiguas se migran al iniciar sesión.
5. El JWT dura **2 horas**. `Jwt:Key` y `QrAcceso:Key` deben ser distintas y de al menos 32 bytes, o la API no arranca.

### Planes, membresías y pagos
1. Existen **tres planes fijos**: Mensual ($120.000 COP, 1 mes), Duo ($200.000, 1 mes, titular + acompañante) y Anual ($1.200.000, 12 meses). Solo se editan precio y duración.
2. Métodos de pago manual: **efectivo, transferencia y datáfono**, aprobados al instante.
3. Cada pago guarda el **precio vigente** y una **referencia única**.
4. La **renovación anticipada** empieza donde termina el periodo actual.
5. Con una membresía vigente solo se puede renovar **el mismo plan**.
6. **Duo exige acompañante**; los demás planes no lo admiten. El acompañante no puede tener otra membresía vigente.
7. Un proceso **horario** marca como vencidas las membresías pasadas de fecha; las consultas además calculan el estado real.

### Acceso y asistencia
1. Solo con **membresía vigente** se puede generar el QR, que dura **1 minuto**.
2. Recepción escanea el QR y el sistema **revalida la membresía** en ese momento.
3. Un acceso permitido registra **una asistencia por usuario y día**, en hora de Colombia (UTC-5).
4. La racha sigue viva si la última asistencia fue hoy o ayer.
5. Insignias de asistencia: primera visita, racha de 7 y 30 días, 50 y 100 asistencias, y reto mensual de 12 días.

### Rutinas y progreso
1. Una rutina requiere **al menos un ejercicio**, sin repetirlos; series y repeticiones mayores a 0 y peso entre 0 y 1000 kg.
2. Un usuario solo crea rutinas **para sí mismo**; el entrenador las crea para un cliente.
3. El entrenador solo edita o borra si la rutina tiene activado **«permitir edición del entrenador»**.
4. Al editar, si suben repeticiones o peso se registra una **mejora**; si supera la mejor marca (más peso o, con igual peso, más repeticiones) es un **récord personal**.
5. El historial de progreso sobrevive al borrado de la rutina o del ejercicio.
6. Insignias de progreso: primera mejora, primer récord, 10 récords, +50 kg, +100 repeticiones y reto mensual de 4 mejoras.

## Cómo ejecutar y probar la aplicación

### Requisitos previos
- .NET 10 SDK
- Node.js y npm (versión compatible con Angular 22)
- Docker (para PostgreSQL y RabbitMQ)
- Flutter SDK (opcional, solo para `Fitcore_app`)

### Paso 1 — Infraestructura
Crea un archivo `.env` en la raíz

```env
POSTGRES_PASSWORD=tu_password
```

```bash
docker compose up -d
```

| Servicio | Puerto | Base de datos |
|---|---|---|
| PostgreSQL (Identity) | 5432 | `fitcore_identity` |
| PostgreSQL (Workouts) | 5433 | `fitcore_workouts` |

### Paso 2 — Configuración de secretos
Las APIs **no arrancan** sin estas claves. Usa `dotnet user-secrets

```bash
# Identity
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=fitcore_identity;Username=postgres;Password=tu_password" --project Services/Identity/FitCore.Identity.API
dotnet user-secrets set "Jwt:Key"       "<cadena aleatoria de 32+ bytes>" --project Services/Identity/FitCore.Identity.API
dotnet user-secrets set "QrAcceso:Key"  "<otra cadena distinta de 32+ bytes>" --project Services/Identity/FitCore.Identity.API

# Workouts (la misma Jwt:Key que Identity)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=fitcore_workouts;Username=postgres;Password=tu_password" --project Services/Workouts/FitCore.Workouts.API
dotnet user-secrets set "Jwt:Key"       "<la misma clave que Identity>" --project Services/Workouts/FitCore.Workouts.API
```

### Paso 3 — Ejecutar los servicios
Una terminal por servicio:

```bash
dotnet run --project Services/Identity/FitCore.Identity.API
dotnet run --project Services/Workouts/FitCore.Workouts.API
dotnet run --project Services/Gateway/FitCore.Gateway
```

| URL | Descripción |
|---|---|
| `http://localhost:5100` | Gateway (punto de entrada de los clientes) |
| `http://localhost:5231/swagger` | Swagger de Identity (solo en Development) |
| `http://localhost:5000/swagger` | Swagger de Workouts |
| `…/health` | Estado del servicio y de su base de datos |

Al arrancar, cada servicio **aplica sus migraciones**. Workouts además **descarga el catálogo de ejercicios de Wger** si la tabla está vacía (necesita internet).

### Paso 4 — Ejecutar un cliente

**Angular** (puerto 4200):
```bash
cd fitcore-web
npm install
npm start
```

**Flutter** (puerto 4300, el que admiten los servicios en CORS):
```bash
cd Fitcore_app
flutter run -d chrome --web-port 4300
# Otro Gateway: --dart-define=API_URL=http://servidor:5100/api
```

### Paso 5 — Probar el flujo completo

En **Development**, Identity siembra un Dueño de pruebas (`SemillaPostman:Habilitada`):

| Correo | Contraseña | Rol |
|---|---|---|
| `postman.dueno@fitcore.com` | `Postman123!` | Administrador (Dueño) |

> Solo para desarrollo. No actives esta semilla fuera de ese entorno.

1. **Administrador**: inicia sesión → *Códigos de registro* → crea un código.
2. **Cliente**: *Registro* → introduce el código → inicia sesión como usuario.
3. **Administrador**: *Membresías* → registra un pago manual para el cliente (plan Mensual).
4. **Cliente**: *Mi acceso* → se muestra el QR renovable cada minuto.
5. **Recepción** (administrador o entrenador): *Control de acceso* → escanea el QR → «Acceso permitido».
6. **Cliente**: *Logros* → aparece la primera visita y la racha.
7. **Cliente**: *Rutinas* → crea una rutina con ejercicios de la biblioteca, edítala subiendo el peso y revisa el progreso y el récord.

### Pruebas automáticas

```bash
dotnet test        # xUnit: Identity, Workouts y Gateway
cd fitcore-web && npm test
```

## Endpoints principales (vía Gateway `http://localhost:5100`)

| Método | Ruta | Descripción | Acceso |
|---|---|---|---|
| POST | `/api/Auth/login/usuario` · `/entrenador` · `/administrador` | Iniciar sesión, devuelve JWT | Público |
| POST | `/api/Usuario` | Registrar usuario con código | Público |
| GET | `/api/Usuario` | Listar usuarios | Entrenador, Administrador |
| GET | `/api/Usuario/me/qr-token` | Token del QR (1 min) | Usuario |
| GET | `/api/Usuario/me/asistencia` | Rachas, insignias, reto del mes | Usuario |
| POST | `/api/Acceso/validar` | Validar QR en recepción | Entrenador, Administrador |
| GET / POST | `/api/CodigoRegistro` | Listar / crear códigos | Administrador |
| GET / PUT | `/api/Plan` | Ver / modificar planes | Público / Administrador |
| GET | `/api/Membresia/mia` | Mi membresía actual | Usuario |
| GET / POST | `/api/Membresia/pagos` | Historial / registrar pago manual | Administrador |
| GET / POST / PUT / DELETE | `/api/Ejercicios` | Biblioteca de ejercicios | Lectura: todos · Escritura: personal |
| GET / POST / PUT / DELETE | `/api/Rutinas` | Rutinas | Según reglas de propiedad |
| GET | `/api/Progreso/mio` | Mi progreso e insignias | Usuario |

La lista completa y los cuerpos de cada petición están en Swagger.

## Estado del proyecto

| Área | Estado |
|---|---|
| Microservicios Identity y Workouts, Gateway | ✅ Funcionando |
| Registro por código, planes, membresías, pagos manuales | ✅ Completado |
| Acceso por QR, asistencia, rachas e insignias | ✅ Completado |
| Rutinas, biblioteca (Wger), progreso y récords | ✅ Completado |
| Clientes Angular y Flutter | ✅ Mismas pantallas y permisos |
| Pagos en línea (Wompi) | 🔜 Modelo preparado |
| Mensajería con RabbitMQ (recordatorios de vencimiento) | 🔜 Infraestructura lista |
| CI/CD, Dockerfiles, observabilidad | 🔜 Pendiente |
