-- Ejecutar en Railway (y en local). Re-ejecutable.
-- Nombres camelCase: deben coincidir exactamente con los CALL de UsuariosController.
-- sp_agregarUsuario ahora recibe la contraseña ya hasheada desde la aplicación (PBKDF2).

USE DB_Orion_Fit;

DROP PROCEDURE IF EXISTS sp_obtenerUsuariosConNombre;
DROP PROCEDURE IF EXISTS sp_obtenerUsuarioConNombre;
DROP PROCEDURE IF EXISTS sp_agregarUsuario;
DROP PROCEDURE IF EXISTS sp_editarUsuario;

DELIMITER $$

CREATE PROCEDURE sp_obtenerUsuariosConNombre()
BEGIN
    SELECT u.username,
           u.rol,
           COALESCE(a.nombre, e.nombre, c.nombre, '')   AS nombre,
           COALESCE(a.telefono, e.telefono, c.telefono, '') AS telefono,
           COALESCE(a.correo, e.correo, c.correo, '')   AS correo
    FROM usuario u
    LEFT JOIN administrador a ON a.id_usuario = u.id_usuario
    LEFT JOIN entrenador e    ON e.id_usuario = u.id_usuario
    LEFT JOIN cliente c       ON c.id_usuario = u.id_usuario;
END $$

CREATE PROCEDURE sp_obtenerUsuarioConNombre(IN pId INT)
BEGIN
    SELECT u.username,
           u.rol,
           COALESCE(a.nombre, e.nombre, c.nombre, '')   AS nombre,
           COALESCE(a.telefono, e.telefono, c.telefono, '') AS telefono,
           COALESCE(a.correo, e.correo, c.correo, '')   AS correo
    FROM usuario u
    LEFT JOIN administrador a ON a.id_usuario = u.id_usuario
    LEFT JOIN entrenador e    ON e.id_usuario = u.id_usuario
    LEFT JOIN cliente c       ON c.id_usuario = u.id_usuario
    WHERE u.id_usuario = pId;
END $$

CREATE PROCEDURE sp_agregarUsuario(
    IN pNombre VARCHAR(100),
    IN pTelefono VARCHAR(20),
    IN pCorreo VARCHAR(100),
    IN pRol VARCHAR(100),
    IN pUsername VARCHAR(100),
    IN pContrasena VARCHAR(255)
)
BEGIN
    DECLARE nuevoUsuarioId INT;

    -- Usuario duplicado, ignorando guiones, puntos y mayúsculas (ej: 1-234 == 1234)
    IF EXISTS (
        SELECT 1 FROM usuario
        WHERE LOWER(REGEXP_REPLACE(username, '[^A-Za-z0-9]', ''))
            = LOWER(REGEXP_REPLACE(pUsername, '[^A-Za-z0-9]', ''))
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Ya existe un usuario con ese nombre de usuario.';
    END IF;

    IF EXISTS (SELECT 1 FROM administrador WHERE LOWER(correo) = LOWER(pCorreo))
        OR EXISTS (SELECT 1 FROM entrenador WHERE LOWER(correo) = LOWER(pCorreo))
        OR EXISTS (SELECT 1 FROM cliente WHERE LOWER(correo) = LOWER(pCorreo)) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Ya existe un usuario registrado con ese correo.';
    END IF;

    INSERT INTO usuario (username, contrasena, rol)
    VALUES (pUsername, pContrasena, pRol);
    SET nuevoUsuarioId = LAST_INSERT_ID();

    IF pRol LIKE '%ADMIN%' OR pRol LIKE '%RECEPTION%' THEN
        INSERT INTO administrador (id_usuario, nombre, telefono, correo)
        VALUES (nuevoUsuarioId, pNombre, pTelefono, pCorreo);
    END IF;

    IF pRol LIKE '%TRAINER%' THEN
        INSERT INTO entrenador (id_usuario, nombre, telefono, correo)
        VALUES (nuevoUsuarioId, pNombre, pTelefono, pCorreo);
    END IF;

    IF pRol LIKE '%USER%' AND pRol NOT LIKE '%RECEPTION%' THEN
        INSERT INTO cliente (id_usuario, nombre, telefono, correo)
        VALUES (nuevoUsuarioId, pNombre, pTelefono, pCorreo);
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
    DECLARE idUsuario INT;

    SELECT id_usuario INTO idUsuario
    FROM usuario
    WHERE username = pUsername;

    IF idUsuario IS NULL THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Revisa el user, ya que no se encuentra';
    ELSE
        UPDATE usuario SET rol = pRol WHERE id_usuario = idUsuario;

        -- Los datos de contacto se actualizan en TODAS las tablas de rol que ya tenga la cuenta
        UPDATE administrador SET nombre = pNombre, telefono = pTelefono, correo = pCorreo WHERE id_usuario = idUsuario;
        UPDATE entrenador    SET nombre = pNombre, telefono = pTelefono, correo = pCorreo WHERE id_usuario = idUsuario;
        UPDATE cliente       SET nombre = pNombre, telefono = pTelefono, correo = pCorreo WHERE id_usuario = idUsuario;

        -- Y se crean las tablas de los roles nuevos que todavía no tengan registro
        IF (pRol LIKE '%ADMIN%' OR pRol LIKE '%RECEPTION%')
            AND NOT EXISTS (SELECT 1 FROM administrador WHERE id_usuario = idUsuario) THEN
            INSERT INTO administrador (id_usuario, nombre, telefono, correo)
            VALUES (idUsuario, pNombre, pTelefono, pCorreo);
        END IF;

        IF pRol LIKE '%TRAINER%'
            AND NOT EXISTS (SELECT 1 FROM entrenador WHERE id_usuario = idUsuario) THEN
            INSERT INTO entrenador (id_usuario, nombre, telefono, correo)
            VALUES (idUsuario, pNombre, pTelefono, pCorreo);
        END IF;

        IF pRol LIKE '%USER%' AND pRol NOT LIKE '%RECEPTION%'
            AND NOT EXISTS (SELECT 1 FROM cliente WHERE id_usuario = idUsuario) THEN
            INSERT INTO cliente (id_usuario, nombre, telefono, correo)
            VALUES (idUsuario, pNombre, pTelefono, pCorreo);
        END IF;
    END IF;
END $$

DELIMITER ;
