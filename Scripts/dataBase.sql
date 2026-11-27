-- =====================================================================
-- ATTENDSCAN - SCRIPT CONSOLIDADO POSTGRESQL
-- Orden de creación respetando las Foreign Keys. Base nueva y vacía:
--   CREATE DATABASE "AttendScan";  y luego este script, después seedData.sql.
-- =====================================================================

-- =====================================================================
-- SEGURIDAD (igual que en los demás sistemas MBOS.Hub)
-- =====================================================================

CREATE TABLE permission (
    permission_id BIGSERIAL PRIMARY KEY,
    code VARCHAR(100) NOT NULL,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(255),
    module VARCHAR(50) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_permission_code UNIQUE (code)
);

CREATE TABLE app_user (
    app_user_id BIGSERIAL PRIMARY KEY,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL,
    username VARCHAR(50) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    last_login TIMESTAMP,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by BIGINT,
    updated_at TIMESTAMP,
    updated_by BIGINT,
    CONSTRAINT uq_app_user_email UNIQUE (email),
    CONSTRAINT uq_app_user_username UNIQUE (username),
    CONSTRAINT fk_app_user_created_by FOREIGN KEY (created_by) REFERENCES app_user(app_user_id),
    CONSTRAINT fk_app_user_updated_by FOREIGN KEY (updated_by) REFERENCES app_user(app_user_id)
);

CREATE TABLE role (
    role_id BIGSERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    description VARCHAR(255),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by BIGINT,
    updated_at TIMESTAMP,
    updated_by BIGINT,
    CONSTRAINT uq_role_name UNIQUE (name),
    CONSTRAINT fk_role_created_by FOREIGN KEY (created_by) REFERENCES app_user(app_user_id),
    CONSTRAINT fk_role_updated_by FOREIGN KEY (updated_by) REFERENCES app_user(app_user_id)
);

CREATE TABLE refresh_token (
    refresh_token_id BIGSERIAL PRIMARY KEY,
    app_user_id BIGINT NOT NULL,
    token VARCHAR(500) NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_ip VARCHAR(50),
    CONSTRAINT fk_refresh_token_app_user FOREIGN KEY (app_user_id) REFERENCES app_user(app_user_id) ON DELETE CASCADE,
    CONSTRAINT uq_refresh_token_token UNIQUE (token)
);

CREATE TABLE audit_log (
    audit_log_id BIGSERIAL PRIMARY KEY,
    app_user_id BIGINT,
    table_name VARCHAR(100) NOT NULL,
    record_id BIGINT,
    log_date TIMESTAMP NOT NULL DEFAULT NOW(),
    action VARCHAR(20) NOT NULL,
    CONSTRAINT fk_audit_log_app_user FOREIGN KEY (app_user_id) REFERENCES app_user(app_user_id)
);

CREATE INDEX idx_audit_log_app_user ON audit_log(app_user_id);
CREATE INDEX idx_audit_log_table_name ON audit_log(table_name);

CREATE TABLE app_user_role (
    app_user_role_id BIGSERIAL PRIMARY KEY,
    app_user_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by BIGINT,
    CONSTRAINT fk_app_user_role_app_user FOREIGN KEY (app_user_id) REFERENCES app_user(app_user_id) ON DELETE CASCADE,
    CONSTRAINT fk_app_user_role_role FOREIGN KEY (role_id) REFERENCES role(role_id) ON DELETE CASCADE,
    CONSTRAINT fk_app_user_role_created_by FOREIGN KEY (created_by) REFERENCES app_user(app_user_id),
    CONSTRAINT uq_app_user_role UNIQUE (app_user_id, role_id)
);

CREATE INDEX idx_app_user_role_app_user ON app_user_role(app_user_id);
CREATE INDEX idx_app_user_role_role ON app_user_role(role_id);

CREATE TABLE role_permission (
    role_permission_id BIGSERIAL PRIMARY KEY,
    role_id BIGINT NOT NULL,
    permission_id BIGINT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by BIGINT,
    CONSTRAINT fk_role_permission_role FOREIGN KEY (role_id) REFERENCES role(role_id) ON DELETE CASCADE,
    CONSTRAINT fk_role_permission_permission FOREIGN KEY (permission_id) REFERENCES permission(permission_id) ON DELETE CASCADE,
    CONSTRAINT fk_role_permission_created_by FOREIGN KEY (created_by) REFERENCES app_user(app_user_id),
    CONSTRAINT uq_role_permission UNIQUE (role_id, permission_id)
);

CREATE INDEX idx_role_permission_role ON role_permission(role_id);
CREATE INDEX idx_role_permission_permission ON role_permission(permission_id);

CREATE TABLE menu (
    menu_id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    translation_key VARCHAR(100),
    route VARCHAR(150),
    icon VARCHAR(50),
    parent_menu_id BIGINT,
    display_order INT NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    permission_id BIGINT,
    CONSTRAINT fk_menu_parent FOREIGN KEY (parent_menu_id) REFERENCES menu(menu_id) ON DELETE CASCADE,
    CONSTRAINT fk_menu_permission FOREIGN KEY (permission_id) REFERENCES permission(permission_id),
    CONSTRAINT uq_menu_name UNIQUE (name)
);

CREATE INDEX idx_menu_parent ON menu(parent_menu_id);
CREATE INDEX idx_menu_permission ON menu(permission_id);

CREATE TABLE menu_role (
    menu_role_id BIGSERIAL PRIMARY KEY,
    menu_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_menu_role_menu FOREIGN KEY (menu_id) REFERENCES menu(menu_id) ON DELETE CASCADE,
    CONSTRAINT fk_menu_role_role FOREIGN KEY (role_id) REFERENCES role(role_id) ON DELETE CASCADE,
    CONSTRAINT uq_menu_role UNIQUE (menu_id, role_id)
);

CREATE INDEX idx_menu_role_menu ON menu_role(menu_id);
CREATE INDEX idx_menu_role_role ON menu_role(role_id);

-- =====================================================================
-- PERSONAS Y CARNETS
-- =====================================================================

-- card_code es lo que va impreso en el QR del carnet. Reglas que la base de datos garantiza
-- aunque falle la aplicación (los carnets impresos no se pueden corregir):
--   1. Formato AS1-XXXX-XXXX-C con el alfabeto Crockford (CHECK ck_person_card_code_format).
--   2. Único (uq_person_card_code): dos carnets nunca comparten código.
--   3. Inmutable: no se puede cambiar una vez creado (trigger trg_person_card_code_immutable).
--   4. La persona no se puede borrar: su carnet dejaría de servir (trigger trg_person_no_delete).
CREATE TABLE person (
    person_id BIGSERIAL PRIMARY KEY,
    card_code VARCHAR(15) NOT NULL,
    -- Datos de la lista de delegados (Excel), nada más.
    list_number INT,
    region VARCHAR(50),
    district VARCHAR(100),
    church VARCHAR(150),
    paternal_surname VARCHAR(80) NOT NULL,
    maternal_surname VARCHAR(80),
    given_names VARCHAR(120) NOT NULL,
    full_name VARCHAR(200) NOT NULL,      -- "Paterno Materno Nombres"
    position VARCHAR(200),                -- Función
    delegate_type VARCHAR(50),            -- Tipo de delegado
    age SMALLINT,
    gender CHAR(1),
    registration_date TIMESTAMP NOT NULL DEFAULT NOW(),
    app_user_id BIGINT,
    CONSTRAINT uq_person_card_code UNIQUE (card_code),
    CONSTRAINT ck_person_card_code_format
        CHECK (card_code ~ '^AS1-[0-9A-HJKMNP-TV-Z]{4}-[0-9A-HJKMNP-TV-Z]{4}-[0-9A-HJKMNP-TV-Z]$'),
    CONSTRAINT ck_person_paternal_not_blank CHECK (btrim(paternal_surname) <> ''),
    CONSTRAINT ck_person_given_not_blank CHECK (btrim(given_names) <> ''),
    CONSTRAINT ck_person_gender CHECK (gender IS NULL OR gender IN ('M', 'F')),
    CONSTRAINT ck_person_age CHECK (age IS NULL OR age BETWEEN 1 AND 120),
    CONSTRAINT fk_person_app_user FOREIGN KEY (app_user_id) REFERENCES app_user(app_user_id)
);

CREATE INDEX idx_person_full_name ON person (lower(full_name));
CREATE INDEX idx_person_surnames ON person (paternal_surname, maternal_surname, given_names);

CREATE OR REPLACE FUNCTION fn_person_card_code_immutable()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.card_code IS DISTINCT FROM OLD.card_code THEN
        RAISE EXCEPTION 'card_code is permanent (printed on the card) and cannot be changed: %', OLD.card_code;
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_person_card_code_immutable
    BEFORE UPDATE ON person
    FOR EACH ROW EXECUTE FUNCTION fn_person_card_code_immutable();

CREATE OR REPLACE FUNCTION fn_person_no_delete()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'A person cannot be deleted: their printed card (%) must stay valid.', OLD.card_code;
END;
$$;

CREATE TRIGGER trg_person_no_delete
    BEFORE DELETE ON person
    FOR EACH ROW EXECUTE FUNCTION fn_person_no_delete();

-- =====================================================================
-- EVENTOS, SESIONES Y ASISTENCIA
-- =====================================================================

CREATE TABLE event (
    event_id BIGSERIAL PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    description VARCHAR(500),
    start_date DATE NOT NULL,
    end_date DATE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    registration_date TIMESTAMP NOT NULL DEFAULT NOW(),
    app_user_id BIGINT,
    CONSTRAINT ck_event_dates CHECK (end_date IS NULL OR end_date >= start_date),
    CONSTRAINT fk_event_app_user FOREIGN KEY (app_user_id) REFERENCES app_user(app_user_id)
);

CREATE TABLE event_session (
    event_session_id BIGSERIAL PRIMARY KEY,
    event_id BIGINT NOT NULL,
    name VARCHAR(100) NOT NULL,
    session_date DATE NOT NULL,
    is_open BOOLEAN NOT NULL DEFAULT TRUE,
    registration_date TIMESTAMP NOT NULL DEFAULT NOW(),
    app_user_id BIGINT,
    CONSTRAINT fk_event_session_event FOREIGN KEY (event_id) REFERENCES event(event_id),
    CONSTRAINT fk_event_session_app_user FOREIGN KEY (app_user_id) REFERENCES app_user(app_user_id)
);

CREATE INDEX idx_event_session_event ON event_session(event_id);

-- Una persona, una asistencia por sesión: uq_attendance_session_person lo garantiza aunque dos
-- celulares escaneen el mismo carnet en el mismo instante. Sin ON DELETE CASCADE: la asistencia
-- no se pierde por borrar una sesión o un evento (la aplicación tampoco lo permite).
CREATE TABLE attendance (
    attendance_id BIGSERIAL PRIMARY KEY,
    event_session_id BIGINT NOT NULL,
    person_id BIGINT NOT NULL,
    scanned_at TIMESTAMP NOT NULL DEFAULT NOW(),
    app_user_id BIGINT,
    CONSTRAINT fk_attendance_event_session FOREIGN KEY (event_session_id) REFERENCES event_session(event_session_id),
    CONSTRAINT fk_attendance_person FOREIGN KEY (person_id) REFERENCES person(person_id),
    CONSTRAINT fk_attendance_app_user FOREIGN KEY (app_user_id) REFERENCES app_user(app_user_id),
    CONSTRAINT uq_attendance_session_person UNIQUE (event_session_id, person_id)
);

CREATE INDEX idx_attendance_person ON attendance(person_id);
CREATE INDEX idx_attendance_session_scanned ON attendance(event_session_id, scanned_at DESC);
