USE DB_Orion_Fit;
drop procedure if exists sp_obtenerclientesresumen;
drop procedure if exists sp_agregarcliente;

delimiter $$
create procedure sp_obtenerclientesresumen()
begin
    select 
        c.*,
        cm.estado as estadomembresia,
        cm.fecha_fin as vencimiento
    from cliente c
    left join cliente_membresia cm on c.id_cliente = cm.id_cliente
    left join membresia m on cm.id_membresia = m.id_membresia;
end$$
delimiter ;

delimiter $$
create procedure sp_agregarcliente(
    in pnombre varchar(100),
    in pcedula varchar(20),
    in ptelefono varchar(20),
    in pcorreo varchar(100),
    in pfechanacimiento date,
    in pestado varchar(20)
)
begin
	if exists (select 1 from usuario where username = pcedula) then
        signal sqlstate '45000'
            set message_text = 'revisa el cliente, ya que se encuentra duplicado';
    else
		insert into usuario (username, contrasena, rol)
		values (pcedula, sha2(pcedula, 256), 'user');
		set @nuevousuarioid = last_insert_id();
		insert into cliente (id_usuario, nombre, cedula, telefono, correo, fecha_nacimiento, estado)
		values (@nuevousuarioid, pnombre, pcedula, ptelefono, pcorreo, pfechanacimiento, pestado);
	end if;
end $$
delimiter ;
