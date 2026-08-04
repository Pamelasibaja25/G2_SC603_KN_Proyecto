-- =====================================================================
-- MIGRACION: renombrar TODOS los SPs a camelCase real
-- (sp_EditarEquipo -> sp_editarEquipo, etc.)
-- Ejecutar completo en local Y en Railway. Es re-ejecutable (idempotente).
-- =====================================================================

-- 1) Eliminar los SPs viejos (con mayuscula despues de sp_)
DROP PROCEDURE IF EXISTS sp_ActualizarClienteMembresia;
DROP PROCEDURE IF EXISTS sp_ActualizarContraseña;
DROP PROCEDURE IF EXISTS sp_ActualizarEstadosMembresias;
DROP PROCEDURE IF EXISTS sp_AgregarCliente;
DROP PROCEDURE IF EXISTS sp_AgregarClienteMembresia;
DROP PROCEDURE IF EXISTS sp_AgregarEquipo;
DROP PROCEDURE IF EXISTS sp_AgregarMantenimiento;
DROP PROCEDURE IF EXISTS sp_AgregarUsuario;
DROP PROCEDURE IF EXISTS sp_AgregarWOD;
DROP PROCEDURE IF EXISTS sp_CompletarMantenimiento;
DROP PROCEDURE IF EXISTS sp_EditarCliente;
DROP PROCEDURE IF EXISTS sp_EditarEquipo;
DROP PROCEDURE IF EXISTS sp_EditarMantenimiento;
DROP PROCEDURE IF EXISTS sp_EditarUsuario;
DROP PROCEDURE IF EXISTS sp_EliminarCliente;
DROP PROCEDURE IF EXISTS sp_EliminarEquipo;
DROP PROCEDURE IF EXISTS sp_EliminarMantenimiento;
DROP PROCEDURE IF EXISTS sp_ObtenerClientesMembresias;
DROP PROCEDURE IF EXISTS sp_ObtenerClientesResumen;
DROP PROCEDURE IF EXISTS sp_ObtenerEjercicios;
DROP PROCEDURE IF EXISTS sp_ObtenerHistorialMembresia;
DROP PROCEDURE IF EXISTS sp_ObtenerMembresiasProximasVencer;
DROP PROCEDURE IF EXISTS sp_ObtenerUsuarioConNombre;
DROP PROCEDURE IF EXISTS sp_ObtenerUsuariosConNombre;
DROP PROCEDURE IF EXISTS sp_ObtenerWODs;

-- 2) Eliminar por si ya existieran los nuevos (re-ejecucion segura)
DROP PROCEDURE IF EXISTS sp_actualizarClienteMembresia;
DROP PROCEDURE IF EXISTS sp_actualizarContraseña;
DROP PROCEDURE IF EXISTS sp_actualizarEstadosMembresias;
DROP PROCEDURE IF EXISTS sp_agregarCliente;
DROP PROCEDURE IF EXISTS sp_agregarClienteMembresia;
DROP PROCEDURE IF EXISTS sp_agregarEquipo;
DROP PROCEDURE IF EXISTS sp_agregarMantenimiento;
DROP PROCEDURE IF EXISTS sp_agregarUsuario;
DROP PROCEDURE IF EXISTS sp_agregarWOD;
DROP PROCEDURE IF EXISTS sp_completarMantenimiento;
DROP PROCEDURE IF EXISTS sp_editarCliente;
DROP PROCEDURE IF EXISTS sp_editarEquipo;
DROP PROCEDURE IF EXISTS sp_editarMantenimiento;
DROP PROCEDURE IF EXISTS sp_editarUsuario;
DROP PROCEDURE IF EXISTS sp_eliminarCliente;
DROP PROCEDURE IF EXISTS sp_eliminarEquipo;
DROP PROCEDURE IF EXISTS sp_eliminarMantenimiento;
DROP PROCEDURE IF EXISTS sp_obtenerClientesMembresias;
DROP PROCEDURE IF EXISTS sp_obtenerClientesResumen;
DROP PROCEDURE IF EXISTS sp_obtenerEjercicios;
DROP PROCEDURE IF EXISTS sp_obtenerHistorialMembresia;
DROP PROCEDURE IF EXISTS sp_obtenerMembresiasProximasVencer;
DROP PROCEDURE IF EXISTS sp_obtenerUsuarioConNombre;
DROP PROCEDURE IF EXISTS sp_obtenerUsuariosConNombre;
DROP PROCEDURE IF EXISTS sp_obtenerWODs;

-- 3) Crear los SPs con el nombre camelCase correcto
DELIMITER $$

CREATE PROCEDURE sp_agregarWOD(
    IN pIdEntrenador INT,
    IN pNombre       VARCHAR(100),
    IN pObjetivo     VARCHAR(255),
    IN pImagen       VARCHAR(255),
    IN pEjercicios   LONGTEXT
)
BEGIN
    DECLARE nuevaRutinaId INT;
    DECLARE totalEjercicios INT;
    DECLARE indice INT DEFAULT 0;
    DECLARE pIdEjercicio INT;
    DECLARE pSeries INT;
    DECLARE pRepeticiones INT;
    DECLARE pDescanso INT;

    IF pNombre IS NULL OR TRIM(pNombre) = '' THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El nombre del entrenamiento es obligatorio.';
    END IF;

    IF JSON_LENGTH(pEjercicios) = 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Debe incluir al menos un ejercicio en el WOD.';
    END IF;

    INSERT INTO Rutina (id_entrenador, nombre, objetivo, imagen)
    VALUES (pIdEntrenador, pNombre, pObjetivo, pImagen);

    SET nuevaRutinaId = LAST_INSERT_ID();
    SET totalEjercicios = JSON_LENGTH(pEjercicios);

    WHILE indice < totalEjercicios DO
        SET pIdEjercicio  = JSON_UNQUOTE(JSON_EXTRACT(pEjercicios, CONCAT('$[', indice, '].IdEjercicio')));
        SET pSeries       = JSON_UNQUOTE(JSON_EXTRACT(pEjercicios, CONCAT('$[', indice, '].Series')));
        SET pRepeticiones = JSON_UNQUOTE(JSON_EXTRACT(pEjercicios, CONCAT('$[', indice, '].Repeticiones')));
        SET pDescanso     = JSON_UNQUOTE(JSON_EXTRACT(pEjercicios, CONCAT('$[', indice, '].Descanso')));

        INSERT INTO Rutina_Ejercicio (id_rutina, id_reserva, id_ejercicio, series, repeticiones, descanso)
        VALUES (nuevaRutinaId, 1, pIdEjercicio, pSeries, pRepeticiones, pDescanso);

        SET indice = indice + 1;
    END WHILE;
END $$

CREATE PROCEDURE sp_obtenerWODs()
BEGIN
    SELECT
        r.id_rutina      AS IdRutina,
        r.nombre         AS Nombre,
        r.objetivo       AS Objetivo,
        r.imagen         AS Imagen,
        e.id_entrenador  AS IdEntrenador,
        e.nombre         AS NombreEntrenador,
        re.id_rutina_ejercicio AS IdRutinaEjercicio,
        ej.nombre        AS NombreEjercicio,
        re.series        AS Series,
        re.repeticiones  AS Repeticiones,
        re.descanso      AS Descanso
    FROM Rutina r
    INNER JOIN Entrenador e  ON r.id_entrenador = e.id_entrenador
    LEFT JOIN Rutina_Ejercicio re ON r.id_rutina = re.id_rutina
    LEFT JOIN Ejercicio ej   ON re.id_ejercicio = ej.id_ejercicio
    ORDER BY r.id_rutina DESC;
END $$

CREATE PROCEDURE sp_agregarEquipo(
    IN p_nombre VARCHAR(100),
    IN p_estado VARCHAR(30),
    IN p_fecha_compra DATE,
    IN p_costo DECIMAL(10,2)
)
BEGIN
    INSERT INTO equipo (
        nombre,
        estado,
        fecha_compra,
        costo
    )
    VALUES (
        p_nombre,
        p_estado,
        p_fecha_compra,
        p_costo
    );
END $$

CREATE PROCEDURE sp_editarEquipo(
    IN p_id_equipo INT,
    IN p_nombre VARCHAR(100),
    IN p_estado VARCHAR(30),
    IN p_fecha_compra DATE,
    IN p_costo DECIMAL(10,2)
)
BEGIN
    UPDATE equipo
    SET
        nombre = p_nombre,
        estado = p_estado,
        fecha_compra = p_fecha_compra,
        costo = p_costo
    WHERE id_equipo = p_id_equipo;
END $$

CREATE PROCEDURE sp_eliminarEquipo(
    IN p_id_equipo INT
)
BEGIN
    DELETE FROM equipo
    WHERE id_equipo = p_id_equipo;
END $$

CREATE PROCEDURE sp_agregarMantenimiento(
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

CREATE PROCEDURE sp_completarMantenimiento(
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

CREATE PROCEDURE sp_editarMantenimiento(
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

CREATE PROCEDURE sp_eliminarMantenimiento(
    IN p_id_mantenimiento INT
)
BEGIN
    DELETE FROM mantenimiento
    WHERE id_mantenimiento = p_id_mantenimiento;
END $$

CREATE PROCEDURE sp_actualizarContraseña(
    IN pIdUsuario INT,
    IN pPasswordActual VARCHAR(255),
    IN pPasswordNueva VARCHAR(255)
)
BEGIN
    DECLARE vPassword VARCHAR(255);

    -- Obtener la contraseña actual (hash) del usuario
    SELECT contrasena INTO vPassword
    FROM Usuario
    WHERE id_usuario = pIdUsuario;

    -- Validar si coincide el hash
    IF vPassword = SHA2(pPasswordActual,256) THEN
        UPDATE Usuario
        SET contrasena = SHA2(pPasswordNueva, 256)
        WHERE id_usuario = pIdUsuario;
    ELSE
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'La contraseña actual no es correcta';
    END IF;
END $$

CREATE PROCEDURE sp_agregarUsuario(
    IN pNombre VARCHAR(100),
    IN pTelefono VARCHAR(20),
    IN pCorreo VARCHAR(100),
    IN pRol VARCHAR(100),
    IN pUsername VARCHAR(100)
)
BEGIN
	IF EXISTS (SELECT 1 FROM Usuario WHERE username = pUsername) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Revisa el User, ya que se encuentra duplicado';
    ELSE
		INSERT INTO Usuario (username, contrasena, rol)
		VALUES (pUsername, SHA2(pUsername, 256), pRol);
		SET @nuevoUsuarioId = LAST_INSERT_ID();
        
        IF pRol = 'ADMIN' or pRol = 'RECEPTION' THEN
			INSERT INTO Administrador (id_usuario, nombre, telefono, correo)
			VALUES (@nuevoUsuarioId, pNombre, pTelefono, pCorreo);
        
        else
			INSERT INTO Entrenador (id_usuario, nombre, telefono, correo)
			VALUES (@nuevoUsuarioId, pNombre, pTelefono, pCorreo);
        
        END IF;
	END IF;
END $$

CREATE PROCEDURE sp_editarUsuario(
    IN pNombre VARCHAR(100),
    IN pTelefono VARCHAR(20),
    IN pCorreo VARCHAR(100),
    IN pRol VARCHAR(100),
    IN pUsername VARCHAR(100)
)
BEGIN
    DECLARE nuevoUsuarioId INT;

    -- Verificar si existe el usuario
    SELECT id_usuario INTO nuevoUsuarioId
    FROM Usuario
    WHERE username = pUsername;

    IF nuevoUsuarioId IS NULL THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Revisa el User, ya que no se encuentra';
    ELSE
        UPDATE Usuario
        SET rol = pRol
        WHERE id_usuario = nuevoUsuarioId;

        CASE 
        WHEN pRol = 'ADMIN' OR pRol = 'RECEPTION' THEN
            IF EXISTS (SELECT 1 FROM Administrador WHERE id_usuario = nuevoUsuarioId) THEN
                UPDATE Administrador
                SET nombre = pNombre, telefono = pTelefono, correo = pCorreo
                WHERE id_usuario = nuevoUsuarioId;
            ELSE
                INSERT INTO Administrador (id_usuario, nombre, telefono, correo)
                VALUES (nuevoUsuarioId, pNombre, pTelefono, pCorreo);
            END IF;
		WHEN pRol = 'USER' THEN
            IF EXISTS (SELECT 1 FROM cliente WHERE id_usuario = nuevoUsuarioId) THEN
                UPDATE cliente
                SET nombre = pNombre, telefono = pTelefono, correo = pCorreo
                WHERE id_usuario = nuevoUsuarioId;
            ELSE
                INSERT INTO cliente (id_usuario, nombre, telefono, correo)
                VALUES (nuevoUsuarioId, pNombre, pTelefono, pCorreo);
            END IF;
        WHEN pRol = 'TRAINER' tHEN
            IF EXISTS (SELECT 1 FROM Entrenador WHERE id_usuario = nuevoUsuarioId) THEN
                UPDATE Entrenador
                SET nombre = pNombre, telefono = pTelefono, correo = pCorreo
                WHERE id_usuario = nuevoUsuarioId;
            ELSE
                INSERT INTO Entrenador (id_usuario, nombre, telefono, correo)
                VALUES (nuevoUsuarioId, pNombre, pTelefono, pCorreo);
            END IF;
		END CASE;
    END IF;
END $$

CREATE PROCEDURE sp_obtenerUsuarioConNombre(IN pId INT)
BEGIN
    SELECT u.username,
           u.rol,
           CASE 
               WHEN u.rol = 'ADMIN' OR u.rol = 'RECEPTION' THEN a.nombre
               WHEN u.rol = 'USER' THEN c.nombre
               WHEN u.rol = 'TRAINER' THEN e.nombre
               ELSE ''
           END AS nombre,
           CASE 
               WHEN u.rol = 'ADMIN' OR u.rol = 'RECEPTION' THEN a.telefono
               WHEN u.rol = 'USER' THEN c.telefono
               WHEN u.rol = 'TRAINER' THEN e.telefono
               ELSE ''
           END AS telefono,
           CASE 
               WHEN u.rol = 'ADMIN' OR u.rol = 'RECEPTION' THEN a.correo
               WHEN u.rol = 'USER' THEN c.correo
               WHEN u.rol = 'TRAINER' THEN e.correo
               ELSE ''
           END AS correo
    FROM Usuario u
    LEFT JOIN Administrador a ON u.id_usuario = a.id_usuario
    LEFT JOIN Cliente c ON u.id_usuario = c.id_usuario
    LEFT JOIN Entrenador e ON u.id_usuario = e.id_usuario
    WHERE u.id_usuario = pId;
END $$

CREATE PROCEDURE sp_obtenerUsuariosConNombre()
BEGIN
    SELECT u.username,
           u.rol,
           CASE 
               WHEN u.rol = 'ADMIN' or u.rol = 'RECEPTION' THEN a.nombre
               WHEN u.rol = 'USER' THEN c.nombre
               WHEN u.rol = 'TRAINER' THEN e.nombre
               ELSE ''
           END AS nombre,
           CASE 
               WHEN u.rol = 'ADMIN' or u.rol = 'RECEPTION' THEN a.telefono
               WHEN u.rol = 'USER' THEN c.telefono
               WHEN u.rol = 'TRAINER' THEN e.telefono
               ELSE ''
           END AS telefono,
           CASE 
               WHEN u.rol = 'ADMIN' or u.rol = 'RECEPTION' THEN a.correo
               WHEN u.rol = 'USER' THEN c.correo
               WHEN u.rol = 'TRAINER' THEN e.correo
               ELSE ''
           END AS correo
    FROM Usuario u
    LEFT JOIN Administrador a ON u.id_usuario = a.id_usuario
    LEFT JOIN Cliente c ON u.id_usuario = c.id_usuario
    LEFT JOIN Entrenador e ON u.id_usuario = e.id_usuario;
END $$

CREATE PROCEDURE sp_agregarCliente(
    IN pNombre VARCHAR(100),
    IN pCedula VARCHAR(20),
    IN pTelefono VARCHAR(20),
    IN pCorreo VARCHAR(100),
    IN pFechaNacimiento DATE,
    IN pEstado VARCHAR(20)
)
BEGIN
	IF EXISTS (SELECT 1 FROM Usuario WHERE username = pCedula) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Revisa el Cliente, ya que se encuentra duplicado';
    ELSE
		INSERT INTO Usuario (username, contrasena, rol)
		VALUES (pCedula, SHA2(pCedula, 256), 'USER');
		SET @nuevoUsuarioId = LAST_INSERT_ID();
		INSERT INTO cliente (id_usuario, nombre, cedula, telefono, correo, fecha_nacimiento, estado)
		VALUES (@nuevoUsuarioId, pNombre, pCedula, pTelefono, pCorreo, pFechaNacimiento, pEstado);
	END IF;
END $$

CREATE PROCEDURE sp_obtenerClientesResumen()
BEGIN
    SELECT 
        c.*,
        cm.estado AS EstadoMembresia,
        cm.fecha_fin AS Vencimiento
    FROM Cliente c
    LEFT JOIN Cliente_Membresia cm ON c.id_cliente = cm.id_cliente
    LEFT JOIN Membresia m ON cm.id_membresia = m.id_membresia;
END $$

CREATE PROCEDURE sp_actualizarClienteMembresia(
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
    
END $$

CREATE PROCEDURE sp_actualizarEstadosMembresias()
BEGIN
    -- Actualizar a 'Vencida' si la fecha_fin ya pasó y no está suspendida
SET SQL_SAFE_UPDATES = 0;

UPDATE Cliente_Membresia
SET estado = 'Vencida'
WHERE fecha_fin < CURDATE()
  AND estado NOT IN ('Suspendida', 'Vencida');

SET SQL_SAFE_UPDATES = 1;

END $$

CREATE PROCEDURE sp_agregarClienteMembresia(
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
END $$

CREATE PROCEDURE sp_obtenerClientesMembresias()
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
END $$

CREATE PROCEDURE sp_obtenerHistorialMembresia(
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
END $$

CREATE PROCEDURE sp_obtenerMembresiasProximasVencer()
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

END $$

CREATE PROCEDURE sp_editarCliente(
    IN pIdCliente     INT,
    IN pNombre        VARCHAR(100),
    IN pCedula        VARCHAR(20),
    IN pTelefono      VARCHAR(20),
    IN pCorreo        VARCHAR(100),
    IN pFechaNacimiento DATE,
    IN pEstado        VARCHAR(20)
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Cliente WHERE id_cliente = pIdCliente) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El cliente no existe en el sistema.';
    ELSE
        UPDATE Cliente
        SET nombre           = pNombre,
            cedula           = pCedula,
            telefono         = pTelefono,
            correo           = pCorreo,
            fecha_nacimiento = pFechaNacimiento,
            estado           = pEstado
        WHERE id_cliente = pIdCliente;
    END IF;
END $$

CREATE PROCEDURE sp_eliminarCliente(
    IN pIdCliente INT
)
BEGIN
    DECLARE clienteEstado VARCHAR(20);
    DECLARE clienteIdUsuario INT;

    SELECT estado, id_usuario
    INTO clienteEstado, clienteIdUsuario
    FROM Cliente
    WHERE id_cliente = pIdCliente;

    IF clienteEstado IS NULL THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El cliente no existe en el sistema.';
    ELSEIF clienteEstado != 'Inactivo' THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Solo se pueden eliminar clientes con estado Inactivo.';
    ELSE
        DELETE FROM Cliente_Membresia WHERE id_cliente = pIdCliente;
        DELETE FROM Cliente_Rutina    WHERE id_cliente = pIdCliente;
        DELETE FROM Asistencia        WHERE id_cliente = pIdCliente;
        DELETE FROM Cliente           WHERE id_cliente = pIdCliente;
        DELETE FROM Usuario           WHERE id_usuario = clienteIdUsuario;
    END IF;
END $$

CREATE PROCEDURE sp_obtenerEjercicios()
BEGIN
    SELECT
        id_ejercicio   AS IdEjercicio,
        nombre         AS Nombre,
        grupo_muscular AS GrupoMuscular
    FROM Ejercicio
    ORDER BY nombre ASC;
END $$


DELIMITER ;