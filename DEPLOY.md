# Despliegue de AttendScan

AttendScan va en el mismo servidor que InvenTrack, FundTrack y ClockMetrics (`148.230.73.226`, Ubuntu,
Docker) y se abre en **https://attendscan.iasdmbos.cloud**.

> **HTTPS es obligatorio.** Sin HTTPS el celular no deja usar la cámara y no se puede escanear.

## Cómo está armado

| Pieza | Dónde | Detalle |
|---|---|---|
| Frontend | contenedor `attendscan-frontend` | Angular (PWA) servido por nginx; pasa `/api` al backend. Puerto local `127.0.0.1:5131`. |
| Backend | contenedor `attendscan-backend` | .NET 10. Puerto local `127.0.0.1:5130`. Red Docker `attendscan-net`. |
| Base de datos | PostgreSQL del servidor | Base `attendscan`, usuario `attendscan`. El contenedor entra por `host.docker.internal`. |
| Configuración | `/root/attendscan-backend.env` | Cadena de conexión, clave JWT y CORS. Solo root. **No va a Git.** Plantilla: `deploy/attendscan-backend.env.example`. |
| Entrada pública | `/etc/nginx/sites-available/attendscan.conf` | Copia de `deploy/attendscan.nginx.conf`. HTTPS en el 443, pasa todo a `127.0.0.1:5131`. |
| Certificado | Let's Encrypt (certbot) | Se renueva solo. |
| Código | `/opt/attendscan/backend` y `/opt/attendscan/frontend` | Clones de GitHub. |

Puertos locales del servidor (revisado el 2026-11-27): InvenTrack 5080-5081, FundTrack 5090-5091,
Música 5092, ClockMetrics 5100-5101, TransactionMatching 5110-5111, BackToSchool 5120-5121,
**AttendScan 5130-5131**. Antes de agregar otro sistema: `ss -ltn | grep 127.0.0.1:51`.

## Primera instalación

1. **DNS:** registro A `attendscan` → `148.230.73.226` en `iasdmbos.cloud`.

2. **Código y configuración** (en el servidor, como root):

   ```bash
   mkdir -p /opt/attendscan
   git clone https://github.com/luisluna-esc/MBOS.Hub.AttendScan.git     /opt/attendscan/backend
   git clone https://github.com/luisluna-esc/MBOS.Hub.AttendScan.Web.git /opt/attendscan/frontend

   cp /opt/attendscan/backend/deploy/attendscan-backend.env.example /root/attendscan-backend.env
   chmod 600 /root/attendscan-backend.env
   nano /root/attendscan-backend.env     # poner Password=<nueva> y Jwt__Key=$(openssl rand -base64 48)
   ```

3. **Datos que no están en Git.** El repositorio es público, así que la lista de personas
   (`seedPersons.sql`, con nombres, edades, etc.) y los usuarios (`seedUsers.sql`) no se suben.
   Se copian desde la PC donde están, a `/root/attendscan-data/` (solo root):

   ```bash
   scp Scripts/seedPersons.sql Scripts/seedUsers.sql root@148.230.73.226:/root/attendscan-data/
   ```

   **Base de datos** (crea todo: tablas, roles, admin, los 4 registradores, las 240 personas con sus
   códigos de carnet, y el acceso desde Docker en `pg_hba.conf`):

   ```bash
   bash /opt/attendscan/backend/deploy/setup-database.sh
   ```


4. **Contenedores:**

   ```bash
   bash /opt/attendscan/backend/deploy.sh
   ```

5. **Entrada pública y certificado:**

   ```bash
   cp /opt/attendscan/backend/deploy/attendscan.nginx.conf /etc/nginx/sites-available/attendscan.conf
   ln -s /etc/nginx/sites-available/attendscan.conf /etc/nginx/sites-enabled/attendscan.conf
   nginx -t && systemctl reload nginx
   certbot --nginx -d attendscan.iasdmbos.cloud --redirect
   ```

6. **Revisar:** entrar a https://attendscan.iasdmbos.cloud con `admin`, cambiar su contraseña
   (Administración > Usuarios) y probar un escaneo con un registrador.

## Los carnets

Las 240 personas se cargan con `Scripts/seedPersons.sql`, **con los mismos códigos de carnet** que se
generaron y verificaron. No volver a importar el Excel en el servidor para "cargar a las personas":
ya están (saldrían como "Ya existe"). Personas → *Descargar todos los QR* da los PNG para imprimir.

## Actualizar a una nueva versión

1. Sube los cambios a GitHub (backend y frontend).
2. Si hay cambios de base de datos, primero respaldo y después el script:

   ```bash
   sudo -u postgres pg_dump -Fc attendscan > /root/attendscan_antes_$(date +%Y%m%d_%H%M).dump
   export PGPASSWORD=$(grep ^ConnectionStrings /root/attendscan-backend.env | sed 's/.*Password=//')
   psql -h 127.0.0.1 -U attendscan -d attendscan -v ON_ERROR_STOP=1 -f /opt/attendscan/backend/Scripts/<script>.sql
   ```

   `seedData.sql`, `seedUsers.sql` y `seedPersons.sql` se pueden volver a correr sin problema;
   `dataBase.sql` no (crea las tablas desde cero).

3. `bash /opt/attendscan/backend/deploy.sh`

4. Revisar:

   ```bash
   docker ps | grep attendscan
   docker logs --tail 30 attendscan-backend
   curl -s -o /dev/null -w "%{http_code}\n" https://attendscan.iasdmbos.cloud/
   ```

## Respaldos

```bash
sudo -u postgres pg_dump -Fc attendscan > /root/attendscan_$(date +%Y%m%d_%H%M).dump
```

La asistencia solo vive en la base: respaldar antes y después de cada evento.
