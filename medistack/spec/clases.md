# Clases C#

*Usuario*: UsuarioId, RolId, credenciales (user/pass), datos personales (nombre, apellido, dni, fecha de nacimiento), contacto y estado (activo/bloqueado), FechaRegistro,  UltimoAcceso.

*Paciente*: IdUsuario, obraSocialId, activo.

*Profesional*: IdUsuario, matricula, activo.

*Especialidad*: id, nombre, activo.

*ProfesionalEspecialidad*: vincula médico y especialidad. Campos: valorConsulta, esquemaTipo y esquemaValor (para liquidar honorarios después).

*HorarioAtencion*: profesionalId, diaSemana, horaInicio, horaFin.

*ObraSocial*: id, nombre, codigoCUIT, activo.

*CoberturaEspecialidad*: obraSocialId, especialidadId, porcentajeCobertura.

*Convenio*: profesionalEspecialidadId, obraSocialId, activo.

*Turno*: id, pacienteId, profesionalId, especialidadId, fecha, estado, motivo, datos de la seña (monto y estado).

*Cobro*: turnoId, montoBase, montoObraSocial, copago, señaDescontada, medioPago, vuelto.

*CierreCaja*: fecha, administrativoId, totales (efectivo, tarjeta, transferencia, general), cantidadCobros, efectivoContado, diferencia.

*LiquidacionHonorarios*: profesionalId, periodoDesde, periodoHasta, totalLiquidado.

*InteraccionIA*: usuarioId, canal (Texto/Voz), consulta, funcionEjecutada, respuesta, gastoTokens y fecha. La registra `AgenteIAPacienteNegocio` en cada consulta al asistente y se audita en Reportes.

*Egreso*: id, fecha, concepto, monto, categoria (Insumos/Viaticos/Mantenimiento/Otros), comprobante y administrativoId. Gasto en efectivo del cajón; `CierreCaja` guarda `TotalEgresos` y calcula `Diferencia = EfectivoContado − (TotalEfectivo − TotalEgresos)`.

*Rol*: RolId, Nombre, Descripcion. 