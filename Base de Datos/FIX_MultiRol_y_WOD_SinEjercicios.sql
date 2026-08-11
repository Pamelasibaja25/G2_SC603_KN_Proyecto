-- Ejecutar en Railway. Reemplaza sp_agregarwod: ya no exige al menos
-- un ejercicio (los WOD ahora se publican solo con imagen).
DROP PROCEDURE IF EXISTS sp_agregarwod;

DELIMITER $$

CREATE PROCEDURE sp_agregarwod(
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

    INSERT INTO rutina (id_entrenador, nombre, objetivo, imagen)
    VALUES (pIdEntrenador, pNombre, pObjetivo, pImagen);

    SET nuevaRutinaId = LAST_INSERT_ID();
    SET totalEjercicios = IFNULL(JSON_LENGTH(pEjercicios), 0);

    WHILE indice < totalEjercicios DO
        SET pIdEjercicio  = JSON_UNQUOTE(JSON_EXTRACT(pEjercicios, CONCAT('$[', indice, '].IdEjercicio')));
        SET pSeries       = JSON_UNQUOTE(JSON_EXTRACT(pEjercicios, CONCAT('$[', indice, '].Series')));
        SET pRepeticiones = JSON_UNQUOTE(JSON_EXTRACT(pEjercicios, CONCAT('$[', indice, '].Repeticiones')));
        SET pDescanso     = JSON_UNQUOTE(JSON_EXTRACT(pEjercicios, CONCAT('$[', indice, '].Descanso')));

        INSERT INTO rutina_ejercicio (id_rutina, id_reserva, id_ejercicio, series, repeticiones, descanso)
        VALUES (nuevaRutinaId, 1, pIdEjercicio, pSeries, pRepeticiones, pDescanso);

        SET indice = indice + 1;
    END WHILE;
END $$

DELIMITER ;

DROP PROCEDURE IF EXISTS sp_agregarusuario;
DROP PROCEDURE IF EXISTS sp_editarusuario;
DROP PROCEDURE IF EXISTS sp_obtenerusuarioconnombre;
DROP PROCEDURE IF EXISTS sp_obtenerusuariosconnombre;

DELIMITER $$

CREATE PROCEDURE sp_agregarusuario(
    IN pNombre VARCHAR(100),
    IN pTelefono VARCHAR(20),
    IN pCorreo VARCHAR(100),
    IN pRol VARCHAR(100),
    IN pUsername VARCHAR(100)
)
BEGIN
    DECLARE nuevoUsuarioId INT;

    IF EXISTS (SELECT 1 FROM usuario WHERE username = pUsername) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Revisa el user, ya que se encuentra duplicado';
    ELSE
        INSERT INTO usuario (username, contrasena, rol)
        VALUES (pUsername, SHA2(pUsername, 256), pRol);
        SET nuevoUsuarioId = LAST_INSERT_ID();

        -- Una persona puede tener varios roles a la vez (CSV: "ADMIN,TRAINER").
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
    END IF;
END $$

CREATE PROCEDURE sp_editarusuario(
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

        IF pRol LIKE '%ADMIN%' OR pRol LIKE '%RECEPTION%' THEN
            IF EXISTS (SELECT 1 FROM administrador WHERE id_usuario = idUsuario) THEN
                UPDATE administrador
                SET nombre = pNombre, telefono = pTelefono, correo = pCorreo
                WHERE id_usuario = idUsuario;
            ELSE
                INSERT INTO administrador (id_usuario, nombre, telefono, correo)
                VALUES (idUsuario, pNombre, pTelefono, pCorreo);
            END IF;
        END IF;

        IF pRol LIKE '%TRAINER%' THEN
            IF EXISTS (SELECT 1 FROM entrenador WHERE id_usuario = idUsuario) THEN
                UPDATE entrenador
                SET nombre = pNombre, telefono = pTelefono, correo = pCorreo
                WHERE id_usuario = idUsuario;
            ELSE
                INSERT INTO entrenador (id_usuario, nombre, telefono, correo)
                VALUES (idUsuario, pNombre, pTelefono, pCorreo);
            END IF;
        END IF;

        IF pRol LIKE '%USER%' AND pRol NOT LIKE '%RECEPTION%' THEN
            IF EXISTS (SELECT 1 FROM cliente WHERE id_usuario = idUsuario) THEN
                UPDATE cliente
                SET nombre = pNombre, telefono = pTelefono, correo = pCorreo
                WHERE id_usuario = idUsuario;
            ELSE
                INSERT INTO cliente (id_usuario, nombre, telefono, correo)
                VALUES (idUsuario, pNombre, pTelefono, pCorreo);
            END IF;
        END IF;
    END IF;
END $$

CREATE PROCEDURE sp_obtenerusuarioconnombre(IN pId INT)
BEGIN
    SELECT u.username,
           u.rol,
           COALESCE(a.nombre, e.nombre, c.nombre, '')   AS nombre,
           COALESCE(a.telefono, e.telefono, c.telefono, '') AS telefono,
           COALESCE(a.correo, e.correo, c.correo, '')   AS correo
    FROM usuario u
    LEFT JOIN administrador a ON u.id_usuario = a.id_usuario
    LEFT JOIN entrenador e ON u.id_usuario = e.id_usuario
    LEFT JOIN cliente c ON u.id_usuario = c.id_usuario
    WHERE u.id_usuario = pId;
END $$

CREATE PROCEDURE sp_obtenerusuariosconnombre()
BEGIN
    SELECT u.username,
           u.rol,
           COALESCE(a.nombre, e.nombre, c.nombre, '')   AS nombre,
           COALESCE(a.telefono, e.telefono, c.telefono, '') AS telefono,
           COALESCE(a.correo, e.correo, c.correo, '')   AS correo
    FROM usuario u
    LEFT JOIN administrador a ON u.id_usuario = a.id_usuario
    LEFT JOIN entrenador e ON u.id_usuario = e.id_usuario
    LEFT JOIN cliente c ON u.id_usuario = c.id_usuario;
END $$

DELIMITER ;
