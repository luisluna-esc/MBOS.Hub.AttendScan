#!/bin/bash
# Crea la base de AttendScan en el PostgreSQL del servidor y la deja lista con todo:
# tablas, roles, menú, admin, los 4 registradores y las 240 personas con sus códigos de carnet.
# Solo para la PRIMERA vez (dataBase.sql crea las tablas desde cero y falla si ya existen).
# Uso, en el servidor:  bash /opt/attendscan/backend/deploy/setup-database.sh
set -euo pipefail

SCRIPTS=/opt/attendscan/backend/Scripts
# Datos personales y usuarios: NO están en Git (repositorio público). Se copian aparte a esta carpeta.
DATA=/root/attendscan-data
ENV_FILE=/root/attendscan-backend.env

PASSWORD=$(grep ^ConnectionStrings__DefaultConnection "$ENV_FILE" | sed 's/.*Password=//')
if [ -z "$PASSWORD" ] || [ "$PASSWORD" = "CAMBIAR" ]; then
  echo "Primero pon la contraseña de la base en $ENV_FILE (ver DEPLOY.md, paso 2)." >&2
  exit 1
fi

for f in seedUsers.sql seedPersons.sql; do
  if [ ! -f "$DATA/$f" ]; then
    echo "Falta $DATA/$f (se copia aparte, no viene de Git; ver DEPLOY.md)." >&2
    exit 1
  fi
done

if sudo -u postgres psql -tAc "SELECT 1 FROM pg_database WHERE datname='attendscan'" | grep -q 1; then
  echo "La base attendscan ya existe: no se toca. (Este script es solo para la primera vez.)" >&2
  exit 1
fi

echo "== Creando usuario y base"
sudo -u postgres psql -v ON_ERROR_STOP=1 -q <<SQL
DO \$\$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'attendscan') THEN
    CREATE ROLE attendscan LOGIN PASSWORD '$PASSWORD';
  ELSE
    ALTER ROLE attendscan WITH LOGIN PASSWORD '$PASSWORD';
  END IF;
END \$\$;
CREATE DATABASE attendscan OWNER attendscan ENCODING 'UTF8' TEMPLATE template0;
SQL

export PGPASSWORD="$PASSWORD"
run() {
  echo "== $(basename "$1")"
  psql -h 127.0.0.1 -U attendscan -d attendscan -v ON_ERROR_STOP=1 -q -f "$1" > /dev/null
}
run "$SCRIPTS/dataBase.sql"
run "$SCRIPTS/seedData.sql"
run "$DATA/seedUsers.sql"
run "$DATA/seedPersons.sql"

echo "== Revisando"
psql -h 127.0.0.1 -U attendscan -d attendscan -tAc \
  "SELECT 'personas: ' || (SELECT count(*) FROM person) || ' | usuarios: ' || (SELECT string_agg(username, ', ' ORDER BY app_user_id) FROM app_user)"
# Que el contenedor (red Docker 172.16.0.0/12) pueda entrar a esta base, igual que los demás sistemas.
HBA=$(sudo -u postgres psql -tAc "show hba_file")
if ! grep -qE "^host\s+attendscan\s+attendscan\s" "$HBA"; then
  echo "host    attendscan    attendscan    172.16.0.0/12    scram-sha-256" >> "$HBA"
  sudo -u postgres psql -tAc "select pg_reload_conf()" > /dev/null
  echo "== pg_hba: acceso desde Docker agregado"
fi

echo "Listo. Siguiente: bash /opt/attendscan/backend/deploy.sh"
