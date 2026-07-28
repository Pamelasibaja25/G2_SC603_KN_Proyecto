USE DB_Orion_Fit;
DROP PROCEDURE IF EXISTS sp_ObtenerClientesMembresias;
DROP PROCEDURE IF EXISTS sp_ActualizarEstadosMembresias;
DROP PROCEDURE IF EXISTS sp_AgregarClienteMembresia;
DROP PROCEDURE IF EXISTS sp_ActualizarClienteMembresia;
DROP PROCEDURE IF EXISTS sp_ObtenerHistorialMembresia;
DROP PROCEDURE IF EXISTS sp_ObtenerMembresiasProximasVencer;

/*EVENTS*/
CREATE EVENT IF NOT EXISTS EventoActualizarEstados
ON SCHEDULE EVERY 1 DAY
STARTS CURRENT_TIMESTAMP
DO
    CALL sp_ActualizarEstadosMembresias();


/*PROCEDURES*/
DELIMITER $$

CREATE PROCEDURE sp_ObtenerClientesMembresias()
BEGIN
    SELECT 
        c.nombre AS Cliente,
        m.nombre AS TipoPlan,
        cm.fecha_inicio AS FechaInicio,
        cm.fecha_fin AS FechaFin,
        m.precio AS Precio,
        cm.estado AS Estado,
       c.id_cliente as IdCliente,
        m.id_membresia as IdMembresia
    FROM Cliente_Membresia cm
    INNER JOIN Cliente c ON cm.id_cliente = c.id_cliente
    INNER JOIN Membresia m ON cm.id_membresia = m.id_membresia;
END$$

DELIMITER ;

DELIMITER $$

CREATE PROCEDURE sp_ActualizarEstadosMembresias()
BEGIN
    -- Actualizar a 'Vencida' si la fecha_fin ya pasó y no está suspendida
SET SQL_SAFE_UPDATES = 0;

UPDATE Cliente_Membresia
SET estado = 'Vencida'
WHERE fecha_fin < CURDATE()
  AND estado NOT IN ('Suspendida', 'Vencida');

SET SQL_SAFE_UPDATES = 1;

END$$

DELIMITER ;

DELIMITER $$

CREATE PROCEDURE sp_AgregarClienteMembresia(
    IN p_id_cliente INT,
    IN p_id_membresia INT,
    IN p_fecha_inicio DATE,
    IN p_fecha_fin DATE,
    IN p_estado VARCHAR(20)
)
BEGIN
    -- Verificar si el cliente ya tiene una membresía 
    IF NOT EXISTS (
        SELECT 1
        FROM Cliente_Membresia
        WHERE id_cliente = p_id_cliente
    ) THEN
        -- Insertar nueva membresía
        INSERT INTO Cliente_Membresia (
            id_cliente, id_membresia, fecha_inicio, fecha_fin, estado
        ) VALUES (
            p_id_cliente, p_id_membresia, p_fecha_inicio, p_fecha_fin, p_estado
        );
    ELSE
        -- Opcional: lanzar un error o mensaje
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El cliente ya tiene una membresía activa';
    END IF;
END$$

DELIMITER ;

DELIMITER $$

CREATE PROCEDURE sp_ActualizarClienteMembresia(
    IN p_id_cliente INT,
    IN p_id_membresia INT,
    IN p_fecha_inicio DATE,
    IN p_fecha_fin DATE,
    IN p_estado VARCHAR(20)
)
BEGIN
    -- Guardar la información actual en el historial
    INSERT INTO historial_membresias (
        id_cliente,
        id_membresia,
        fecha_inicio,
        fecha_fin
    )
    SELECT
        id_cliente,
        id_membresia,
        fecha_inicio,
        fecha_fin
    FROM Cliente_Membresia
    WHERE id_cliente = p_id_cliente;
    
    -- Actualiza la membresía del cliente si existe
    UPDATE Cliente_Membresia
    SET id_membresia = p_id_membresia,
        fecha_inicio = p_fecha_inicio,
        fecha_fin = p_fecha_fin,
        estado = p_estado
    WHERE id_cliente = p_id_cliente;
    
END$$

DELIMITER ;

DELIMITER $$

CREATE PROCEDURE sp_ObtenerHistorialMembresia(
    IN p_id_cliente INT
)
BEGIN
    SELECT
        h.id_historial,
        h.id_cliente,
        c.nombre AS Cliente,
        h.id_membresia,
        m.nombre AS Membresia,
        h.fecha_inicio,
        h.fecha_fin
    FROM historial_membresias h
    INNER JOIN Cliente c
        ON h.id_cliente = c.id_cliente
    INNER JOIN Membresia m
        ON h.id_membresia = m.id_membresia
    WHERE h.id_cliente = p_id_cliente
    ORDER BY h.id_historial DESC;
END$$

DELIMITER ;
DELIMITER $$

CREATE PROCEDURE sp_ObtenerMembresiasProximasVencer()
BEGIN

    SELECT
        cm.id_cliente AS IdCliente,
        c.nombre AS Cliente,
        m.nombre AS Membresia,
        cm.fecha_fin AS FechaFin,
        DATEDIFF(cm.fecha_fin, CURDATE()) AS DiasRestantes
    FROM Cliente_Membresia cm
        INNER JOIN Cliente c
            ON c.id_cliente = cm.id_cliente
        INNER JOIN Membresia m
            ON m.id_membresia = cm.id_membresia
    WHERE cm.estado = 'Activa'
        AND cm.fecha_fin BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL 15 DAY)
    ORDER BY cm.fecha_fin ASC;

END$$

DELIMITER ;


