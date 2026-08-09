-- =====================================================================
-- TODOS los ALTER/CREATE TABLE nuevos generados durante la refactorizacion
-- (Modulos 2, 3 y 4). Ejecutar en local y en Railway.
-- El script de renombrado de SPs (MIGRACION_Rename_SPs_CamelCase.sql)
-- se corre APARTE.
-- =====================================================================
USE DB_Orion_Fit;
-- Modulo 2 (WOD): estado de asistencia Aceptar/No asistir
ALTER TABLE cliente_rutina
    ADD COLUMN estado_asistencia VARCHAR(20) NOT NULL DEFAULT 'PENDIENTE'
    AFTER fecha_asignacion;

-- Modulo 3 (Reservas): clases visibles para el cliente solo si se crearon ayer
ALTER TABLE clase
    ADD COLUMN fecha_creacion DATE NOT NULL DEFAULT (CURRENT_DATE)
    AFTER id_rutina;

-- Modulo 4 (Pagos/SINPE)
ALTER TABLE pago
    ADD COLUMN comprobante_pago VARCHAR(255) NULL
    AFTER descripcion;

CREATE TABLE IF NOT EXISTS configuracion_sinpe (
    id_configuracion INT AUTO_INCREMENT PRIMARY KEY,
    imagen_qr VARCHAR(255) NOT NULL,
    actualizado_en DATETIME NOT NULL
);
