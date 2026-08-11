-- Ejecutar en local y Railway
ALTER TABLE pago
    ADD COLUMN comprobante_pago VARCHAR(255) NULL
    AFTER descripcion;

CREATE TABLE IF NOT EXISTS configuracion_sinpe (
    id_configuracion INT AUTO_INCREMENT PRIMARY KEY,
    imagen_qr VARCHAR(255) NOT NULL,
    actualizado_en DATETIME NOT NULL
);
