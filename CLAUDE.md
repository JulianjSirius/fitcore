# FitCore

## Pruebas de Postman (obligatorio al cambiar la API)

- La colección `postman/FitCore.postman_collection.json` se genera desde `postman/generar-coleccion.mjs`. Edita el generador, nunca el JSON.
- Todo cambio en controladores, DTOs, commands/queries o rutas del Gateway actualiza el generador en el mismo cambio: peticiones nuevas, cuerpos y `pm.test` con los códigos y campos esperados. Luego ejecuta `node postman/generar-coleccion.mjs`.
- Validar (con Identity, Workouts y Gateway corriendo en Development): `.\scripts\ejecutar-postman.ps1`. Corre newman y `scripts/verificar-postman.ps1`, que falla si algún endpoint del Swagger no tiene petición.
- La colección no necesita configuración: Identity siembra en Development el Dueño `postman.dueno@fitcore.com` / `Postman123!` (`FitCore.Identity.API/Desarrollo/SemillaDesarrollo.cs`, activado por `SemillaPostman:Habilitada`). Cada ejecución crea datos con correos únicos y restaura lo que modifica.
- Código nuevo que deba arrancar con la API va en el proyecto API cuando sea posible: Smart App Control bloquea DLLs recién compiladas de otros proyectos (Infrastructure, Application).
