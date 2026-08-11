-- Ejecutar manualmente en la BD de Railway (MySQL)
ALTER TABLE cliente_rutina
    ADD COLUMN estado_asistencia VARCHAR(20) NOT NULL DEFAULT 'PENDIENTE'
    AFTER fecha_asignacion;
