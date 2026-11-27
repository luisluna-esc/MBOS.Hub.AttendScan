# MBOS.Hub.AttendScan (API)

Registro de asistencia por QR impreso en carnets. Frontend: `MBOS.Hub.AttendScan.Web` (PWA Angular).
Misma arquitectura que `MBOS.Hub.CheckInvoice`: `core` → `Application` → `Infrastructure` → `Web`.

## Reglas que no se pueden romper

- **El QR del carnet solo lleva `CardCode`** (`AS1-XXXX-XXXX-C`, ver `AttendScan.Application/Services/CardCodes.cs`).
  Lo genera el servidor al crear la persona; no vence, no cambia y no se reutiliza.
- **Los carnets nunca se anulan**: no hay endpoint para borrar personas y la base de datos lo impide
  (triggers `trg_person_no_delete` y `trg_person_card_code_immutable`).
- **Una asistencia por persona por sesión**: índice único `uq_attendance_session_person`.
- `AttendScan.Tests` fija el formato y el dígito verificador. Si una prueba falla, los carnets ya
  impresos podrían dejar de leerse: revertir el cambio, no "arreglar" la prueba.

## Puesta en marcha (desarrollo)

1. Base de datos (PostgreSQL), nueva y vacía:
   ```
   CREATE DATABASE "AttendScan";
   psql -d AttendScan -f Scripts/dataBase.sql
   psql -d AttendScan -f Scripts/seedData.sql
   ```
   El seed crea el usuario `admin` (rol M-BOS). Su contraseña se entrega aparte; cambiarla al entrar.

2. Secretos (no van en `appsettings.json`):
   ```
   cd AttendScan.Web
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=AttendScan;Username=postgres;Password=..."
   dotnet user-secrets set "Jwt:Key" "<base64 de 48+ bytes al azar>"
   ```
   En el servidor: variables de entorno `ConnectionStrings__DefaultConnection` y `Jwt__Key`.

3. `dotnet run --project AttendScan.Web` → http://localhost:5030/swagger

4. Pruebas: `dotnet test` (incluye la lectura de la lista real si está en
   `X:\explicacion sistema\AttendScan\names_revisado.xlsx`; otra ruta con `ATTENDSCAN_NAMES_XLSX`).

## Importar la lista de personas

Personas → **Importar Excel** → elegir `names_revisado.xlsx` (debe tener las columnas
"Apellido paterno", "Apellido materno" y "Nombres"). Primero muestra una vista previa sin guardar;
al confirmar crea a todos con su código de carnet, todo o nada. Volver a subir el Excel no duplica a
nadie: la misma persona (aunque esté escrita en otro orden o sin tildes) sale como "Ya existe".

## Roles (Scripts/seedData.sql)

| Rol | Permiso | Ve |
|---|---|---|
| M-BOS | trabajo | todo |
| Registrador | trabajo | Escanear, Asistencia |
| Consulta | visita | Asistencia (solo lectura) |
