#!/bin/bash
# Actualiza AttendScan en el servidor con lo último de GitHub (backend y frontend).
# Uso, en el servidor:  bash /opt/attendscan/backend/deploy.sh
# No aplica cambios de base de datos: esos se corren antes, a mano (ver DEPLOY.md).
set -euo pipefail

ROOT=/opt/attendscan
DOMAIN=attendscan.iasdmbos.cloud

echo "== Bajando el código"
git -C "$ROOT/backend" pull --ff-only
git -C "$ROOT/frontend" pull --ff-only
echo "   backend:  $(git -C "$ROOT/backend" log --oneline -1)"
echo "   frontend: $(git -C "$ROOT/frontend" log --oneline -1)"

echo "== Construyendo las imágenes (la versión actual sigue funcionando mientras tanto)"
docker build -q -t attendscan-backend:latest "$ROOT/backend" > /dev/null
docker build -q -t attendscan-frontend:latest "$ROOT/frontend" > /dev/null

echo "== Cambiando los contenedores"
docker network inspect attendscan-net > /dev/null 2>&1 || docker network create attendscan-net > /dev/null
docker rm -f attendscan-backend attendscan-frontend > /dev/null 2>&1 || true

docker run -d --name attendscan-backend --network attendscan-net --restart unless-stopped \
  --env-file /root/attendscan-backend.env --add-host host.docker.internal:host-gateway \
  -p 127.0.0.1:5130:8080 attendscan-backend:latest > /dev/null

docker run -d --name attendscan-frontend --network attendscan-net --restart unless-stopped \
  -p 127.0.0.1:5131:80 attendscan-frontend:latest > /dev/null

docker image prune -f > /dev/null

echo "== Revisando"
sleep 8
docker ps --format "   {{.Names}}: {{.Status}}" | grep attendscan
echo "   sitio: $(curl -s -o /dev/null -w "%{http_code}" "https://$DOMAIN/")"
echo "Listo: https://$DOMAIN"
