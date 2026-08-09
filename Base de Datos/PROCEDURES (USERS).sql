USE DB_Orion_Fit;
drop procedure if exists sp_obtenerusuariosconnombre;
drop procedure if exists sp_agregarusuario;
drop procedure if exists sp_editarusuario;
drop procedure if exists sp_obtenerusuarioconnombre;
drop procedure if exists sp_actualizarcontraseña;

delimiter $$

create procedure sp_obtenerusuariosconnombre()
begin
    select u.username,
           u.rol,
           case 
               when u.rol = 'admin' or u.rol = 'reception' then a.nombre
               when u.rol = 'user' then c.nombre
               when u.rol = 'trainer' then e.nombre
               else ''
           end as nombre,
           case 
               when u.rol = 'admin' or u.rol = 'reception' then a.telefono
               when u.rol = 'user' then c.telefono
               when u.rol = 'trainer' then e.telefono
               else ''
           end as telefono,
           case 
               when u.rol = 'admin' or u.rol = 'reception' then a.correo
               when u.rol = 'user' then c.correo
               when u.rol = 'trainer' then e.correo
               else ''
           end as correo
    from usuario u
    left join administrador a on u.id_usuario = a.id_usuario
    left join cliente c on u.id_usuario = c.id_usuario
    left join entrenador e on u.id_usuario = e.id_usuario;
end $$

delimiter ;

delimiter $$
create procedure sp_agregarusuario(
    in pnombre varchar(100),
    in ptelefono varchar(20),
    in pcorreo varchar(100),
    in prol varchar(100),
    in pusername varchar(100)
)
begin
	if exists (select 1 from usuario where username = pusername) then
        signal sqlstate '45000'
            set message_text = 'revisa el user, ya que se encuentra duplicado';
    else
		insert into usuario (username, contrasena, rol)
		values (pusername, sha2(pusername, 256), prol);
		set @nuevousuarioid = last_insert_id();
        
        if prol = 'admin' or prol = 'reception' then
			insert into administrador (id_usuario, nombre, telefono, correo)
			values (@nuevousuarioid, pnombre, ptelefono, pcorreo);
        
        else
			insert into entrenador (id_usuario, nombre, telefono, correo)
			values (@nuevousuarioid, pnombre, ptelefono, pcorreo);
        
        end if;
	end if;
end $$
delimiter ;

delimiter $$

create procedure sp_editarusuario(
    in pnombre varchar(100),
    in ptelefono varchar(20),
    in pcorreo varchar(100),
    in prol varchar(100),
    in pusername varchar(100)
)
begin
    declare nuevousuarioid int;

    -- verificar si existe el usuario
    select id_usuario into nuevousuarioid
    from usuario
    where username = pusername;

    if nuevousuarioid is null then
        signal sqlstate '45000'
            set message_text = 'revisa el user, ya que no se encuentra';
    else
        update usuario
        set rol = prol
        where id_usuario = nuevousuarioid;

        case 
        when prol = 'admin' or prol = 'reception' then
            if exists (select 1 from administrador where id_usuario = nuevousuarioid) then
                update administrador
                set nombre = pnombre, telefono = ptelefono, correo = pcorreo
                where id_usuario = nuevousuarioid;
            else
                insert into administrador (id_usuario, nombre, telefono, correo)
                values (nuevousuarioid, pnombre, ptelefono, pcorreo);
            end if;
		when prol = 'user' then
            if exists (select 1 from cliente where id_usuario = nuevousuarioid) then
                update cliente
                set nombre = pnombre, telefono = ptelefono, correo = pcorreo
                where id_usuario = nuevousuarioid;
            else
                insert into cliente (id_usuario, nombre, telefono, correo)
                values (nuevousuarioid, pnombre, ptelefono, pcorreo);
            end if;
        when prol = 'trainer' then
            if exists (select 1 from entrenador where id_usuario = nuevousuarioid) then
                update entrenador
                set nombre = pnombre, telefono = ptelefono, correo = pcorreo
                where id_usuario = nuevousuarioid;
            else
                insert into entrenador (id_usuario, nombre, telefono, correo)
                values (nuevousuarioid, pnombre, ptelefono, pcorreo);
            end if;
		end case;
    end if;
end $$

delimiter ;

delimiter $$

create procedure sp_obtenerusuarioconnombre(in pid int)
begin
    select u.username,
           u.rol,
           case 
               when u.rol = 'admin' or u.rol = 'reception' then a.nombre
               when u.rol = 'user' then c.nombre
               when u.rol = 'trainer' then e.nombre
               else ''
           end as nombre,
           case 
               when u.rol = 'admin' or u.rol = 'reception' then a.telefono
               when u.rol = 'user' then c.telefono
               when u.rol = 'trainer' then e.telefono
               else ''
           end as telefono,
           case 
               when u.rol = 'admin' or u.rol = 'reception' then a.correo
               when u.rol = 'user' then c.correo
               when u.rol = 'trainer' then e.correo
               else ''
           end as correo
    from usuario u
    left join administrador a on u.id_usuario = a.id_usuario
    left join cliente c on u.id_usuario = c.id_usuario
    left join entrenador e on u.id_usuario = e.id_usuario
    where u.id_usuario = pid;
end$$

delimiter ;

delimiter $$

create procedure sp_actualizarcontraseña(
    in pidusuario int,
    in ppasswordactual varchar(255),
    in ppasswordnueva varchar(255)
)
begin
    declare vpassword varchar(255);

    -- obtener la contraseña actual (hash) del usuario
    select contrasena into vpassword
    from usuario
    where id_usuario = pidusuario;

    -- validar si coincide el hash
    if vpassword = sha2(ppasswordactual,256) then
        update usuario
        set contrasena = sha2(ppasswordnueva, 256)
        where id_usuario = pidusuario;
    else
        signal sqlstate '45000'
            set message_text = 'la contraseña actual no es correcta';
    end if;
end$$

delimiter ;
