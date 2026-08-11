-- Ejecutar en Railway
ALTER TABLE pago
    ADD COLUMN estado_verificacion VARCHAR(20) NOT NULL DEFAULT 'Verificado'
    AFTER comprobante_pago;

-- Los comprobantes SINPE que ya existan quedan como Pendiente
-- (no se puede saber si ya fueron revisados manualmente, así que
-- se marcan para revisión explícita).
UPDATE pago SET estado_verificacion = 'Pendiente' WHERE metodo_pago = 'SINPE';
