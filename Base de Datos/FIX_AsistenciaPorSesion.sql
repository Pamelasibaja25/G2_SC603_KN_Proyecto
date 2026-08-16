USE DB_Orion_Fit;

ALTER TABLE asistencia
    ADD COLUMN id_cliente_rutina INT NULL
    AFTER id_cliente;

ALTER TABLE asistencia
    ADD CONSTRAINT fk_asistencia_cliente_rutina
    FOREIGN KEY (id_cliente_rutina) REFERENCES cliente_rutina(id_cliente_rutina)
    ON DELETE SET NULL;
