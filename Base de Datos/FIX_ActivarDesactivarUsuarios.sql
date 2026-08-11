-- Ejecutar en Railway
ALTER TABLE usuario
    ADD COLUMN activo TINYINT(1) NOT NULL DEFAULT 1
    AFTER rol;
