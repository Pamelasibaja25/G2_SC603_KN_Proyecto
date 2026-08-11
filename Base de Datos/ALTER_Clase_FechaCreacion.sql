-- Ejecutar en local y Railway
ALTER TABLE clase
    ADD COLUMN fecha_creacion DATE NOT NULL DEFAULT (CURRENT_DATE)
    AFTER id_rutina;
