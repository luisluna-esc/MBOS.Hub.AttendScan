-- =====================================================================
-- ATTENDSCAN - DATOS INICIALES (correr después de dataBase.sql)
-- Se puede volver a correr: todo usa ON CONFLICT DO NOTHING.
-- =====================================================================

-- ---------------------------------------------------------------------
-- Permisos: los mismos dos códigos que el resto de MBOS.Hub
-- (ver AttendScan.Infrastructure/Authorization/PermissionAuthorizationHandler.cs):
--   "visita"  -> solo ver (cumple cualquier policy que termine en ".view").
--   "trabajo" -> acceso completo (cumple todas, incluida "attendance.scan").
-- ---------------------------------------------------------------------
INSERT INTO permission (code, name, description, module) VALUES
('visita', 'Visita (solo ver)', 'Puede ver todo el sistema, pero no crear, editar ni escanear.', 'General'),
('trabajo', 'Trabajo (acceso completo)', 'Acceso completo: puede ver, crear, editar y escanear según su rol.', 'General')
ON CONFLICT (code) DO NOTHING;

-- ---------------------------------------------------------------------
-- Roles
-- ---------------------------------------------------------------------
INSERT INTO role (name, description, is_active) VALUES
('M-BOS', 'Superusuario: personas, eventos, asistencia y administración', TRUE),
('Registrador', 'Escanea carnets en la puerta y ve la lista de asistencia', TRUE),
('Consulta', 'Solo ve la lista de asistencia', TRUE)
ON CONFLICT (name) DO NOTHING;

INSERT INTO role_permission (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM role r CROSS JOIN permission p
WHERE (r.name IN ('M-BOS', 'Registrador') AND p.code = 'trabajo')
   OR (r.name = 'Consulta' AND p.code = 'visita')
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- ---------------------------------------------------------------------
-- Menú lateral (translation_key -> public/i18n/*.json, "nav.*")
-- ---------------------------------------------------------------------
INSERT INTO menu (name, translation_key, route, icon, parent_menu_id, display_order, is_active) VALUES
('Inicio', 'nav.home', '/', 'home', NULL, 1, TRUE),
('Asistencia', 'nav.attendance', '/attendance', 'report', NULL, 2, TRUE),
('Personas', 'nav.persons', '/persons', 'client', NULL, 3, TRUE),
('Eventos', 'nav.events', '/events', 'catalog', NULL, 4, TRUE),
('Administración', 'nav.admin', NULL, 'admin', NULL, 90, TRUE)
ON CONFLICT (name) DO NOTHING;

INSERT INTO menu (name, translation_key, route, icon, parent_menu_id, display_order, is_active)
SELECT v.name, v.translation_key, v.route, NULL, parent.menu_id, v.display_order, TRUE
FROM (VALUES
    ('Usuarios', 'nav.users', '/admin/users', 1),
    ('Roles', 'nav.roles', '/admin/roles', 2),
    ('Permisos', 'nav.permissions', '/admin/permissions', 3)
) AS v(name, translation_key, route, display_order)
JOIN menu parent ON parent.name = 'Administración'
ON CONFLICT (name) DO NOTHING;

-- Qué ve cada rol. Escanear no está aquí: el Shell lo muestra siempre arriba del menú.
INSERT INTO menu_role (menu_id, role_id)
SELECT m.menu_id, r.role_id
FROM menu m JOIN role r ON
       (r.name = 'M-BOS'       AND m.route IN ('/', '/attendance', '/persons', '/events', '/admin/users', '/admin/roles', '/admin/permissions'))
    OR (r.name = 'Registrador' AND m.route IN ('/', '/attendance'))
    OR (r.name = 'Consulta'    AND m.route IN ('/', '/attendance'))
ON CONFLICT (menu_id, role_id) DO NOTHING;

-- ---------------------------------------------------------------------
-- Usuario administrador inicial (rol M-BOS). La contraseña NO está en este archivo: se entrega
-- aparte y hay que cambiarla después del primer ingreso (Administración > Usuarios).
-- ---------------------------------------------------------------------
INSERT INTO app_user (first_name, last_name, email, username, password_hash, is_active) VALUES
('Administrador', 'M-BOS', 'admin@mbos.local', 'admin', '$2a$11$/FVUHUOujgz7Z0jaP8ab4.b.RMApzLUBQW0VbmJONSgyIdrro47ce', TRUE)
ON CONFLICT (username) DO NOTHING;

INSERT INTO app_user_role (app_user_id, role_id)
SELECT u.app_user_id, r.role_id
FROM app_user u JOIN role r ON r.name = 'M-BOS'
WHERE u.username = 'admin'
ON CONFLICT (app_user_id, role_id) DO NOTHING;
