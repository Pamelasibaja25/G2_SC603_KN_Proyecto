ALTER TABLE `mantenimiento`
    ADD COLUMN `tipo` ENUM('Preventivo','Correctivo','Calibracion','Limpieza')
    NOT NULL DEFAULT 'Preventivo'
    AFTER `id_equipo`;
    
    -- =====================================================================
-- Stored Procedures para la tabla `mantenimiento`.
-- Mismo estilo y convenciones que SP-Equipos.sql (prefijo p_ en
-- parametros, un bloque DELIMITER por procedimiento).
-- =====================================================================

DELIMITER $$

CREATE PROCEDURE SP_AgregarMantenimiento(
    IN p_id_equipo INT,
    IN p_tipo VARCHAR(30),
    IN p_fecha DATE,
    IN p_descripcion VARCHAR(255),
    IN p_costo DECIMAL(10,2),
    IN p_estado VARCHAR(30)
)
BEGIN
    INSERT INTO mantenimiento (
        id_equipo,
        tipo,
        fecha,
        descripcion,
        costo,
        estado
    )
    VALUES (
        p_id_equipo,
        p_tipo,
        p_fecha,
        p_descripcion,
        p_costo,
        p_estado
    );
END $$

DELIMITER ;

DELIMITER $$

CREATE PROCEDURE SP_EditarMantenimiento(
    IN p_id_mantenimiento INT,
    IN p_id_equipo INT,
    IN p_tipo VARCHAR(30),
    IN p_fecha DATE,
    IN p_descripcion VARCHAR(255),
    IN p_costo DECIMAL(10,2),
    IN p_estado VARCHAR(30)
)
BEGIN
    UPDATE mantenimiento
    SET
        id_equipo = p_id_equipo,
        tipo = p_tipo,
        fecha = p_fecha,
        descripcion = p_descripcion,
        costo = p_costo,
        estado = p_estado
    WHERE id_mantenimiento = p_id_mantenimiento;
END $$

DELIMITER ;

DELIMITER $$

CREATE PROCEDURE SP_EliminarMantenimiento(
    IN p_id_mantenimiento INT
)
BEGIN
    DELETE FROM mantenimiento
    WHERE id_mantenimiento = p_id_mantenimiento;
END $$

DELIMITER ;

-- HU #39: cierra un mantenimiento programado, marcandolo como Completado
-- y actualizando fecha/tipo/costo/descripcion con los datos reales.
DELIMITER $$

CREATE PROCEDURE SP_CompletarMantenimiento(
    IN p_id_mantenimiento INT,
    IN p_tipo VARCHAR(30),
    IN p_fecha DATE,
    IN p_descripcion VARCHAR(255),
    IN p_costo DECIMAL(10,2)
)
BEGIN
    UPDATE mantenimiento
    SET
        tipo = p_tipo,
        fecha = p_fecha,
        descripcion = p_descripcion,
        costo = p_costo,
        estado = 'Completado'
    WHERE id_mantenimiento = p_id_mantenimiento;
END $$

DELIMITER ;