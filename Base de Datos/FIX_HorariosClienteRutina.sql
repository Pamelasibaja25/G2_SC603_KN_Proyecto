-- Ejecutar en Railway
USE DB_Orion_Fit;

ALTER TABLE cliente_rutina
    ADD COLUMN horarios VARCHAR(100) NULL
    AFTER estado_asistencia;
