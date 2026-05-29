USE DB_Orion_Fit;
DROP PROCEDURE IF EXISTS sp_ObtenerUsuariosConNombre;
DELIMITER $$

CREATE PROCEDURE sp_ObtenerUsuariosConNombre()
BEGIN
    SELECT u.username,
           u.rol,
           CASE 
               WHEN u.rol = 'ADMIN' or u.rol = 'RECEPTION' THEN a.nombre
               WHEN u.rol = 'USER' THEN c.nombre
               WHEN u.rol = 'TRAINER' THEN e.nombre
               ELSE ''
           END AS nombre
    FROM Usuario u
    LEFT JOIN Administrador a ON u.id_usuario = a.id_usuario
    LEFT JOIN Cliente c ON u.id_usuario = c.id_usuario
    LEFT JOIN Entrenador e ON u.id_usuario = e.id_usuario;
END $$

DELIMITER ;
