using AltaPagos.Code;
using PixelwareApi.File;
using PixelwareApi.File.Records;
using PixelwareApi.File.Records.Searching;
using PixelwareApi.File.Relations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Renci.SshNet;
using PixelwareApi.File.UserActions;
using SicerAPI;
using PixelwareApi.Data;
using PixelwareApi.Common.Utils;
using System.Diagnostics;
using PixelwareApi.File.Documents;

namespace AltaPagos
{
    public partial class SicerNotificaciones : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Index = Request["index"];
                Filter = Request["filter"];
                Sort = Request["sort"];
                CargarConfirmacion();

                EstadoSICER estado = EstadoSICER.ObtenerEstadoSICER();
                if (estado.MaximosErroresSuperados())
                {
                    erroresSicer.Visible = true;
                    LinkButton1.Enabled = false;
                }
                else
                {
                    erroresSicer.Visible = false;
                    LinkButton1.Enabled = true;
                }
            }
        }

        private void CargarConfirmacion()
        {
            try
            {
				System.Diagnostics.Trace.TraceInformation("Se intentan cargar las comunicaciones");

                // Consultamos todos los registros de Comunicaciones
				bool hasOmited = false;
				List<Comunicacion> comunicaciones = Comunicacion.GetComunicaciones(Filter, Sort, out hasOmited);

				System.Diagnostics.Trace.TraceInformation("Se han cargado {0} comunicaciones", comunicaciones.Count);

				if(comunicaciones.Count > 0)
				{
					// Se construye la tabla
					DataTable dataTable = new DataTable();

                    dataTable.Columns.Add("PWSNUMERO", typeof(decimal));
                    dataTable.Columns.Add("FECHAALTAEXP", typeof(string));
					dataTable.Columns.Add("REFERENCIAEXP", typeof(string));
					dataTable.Columns.Add("FECHACOMUNIC", typeof(string));
                    dataTable.Columns.Add("NOMBREINTERPROPAG", typeof(string));
                    dataTable.Columns.Add("NUMDOCIDENTIFINTERPROPAG", typeof(string));
                    dataTable.Columns.Add("NUMJUSTIFINTECO", typeof(string));
					dataTable.Columns.Add("IMPORTETOTAL", typeof(string));
                    dataTable.Columns.Add("APREMIO", typeof(string));
                    dataTable.Columns.Add("MOTIVOEXPEDICION", typeof(string));
                    dataTable.Columns.Add("INCIDENCIA", typeof(string));

					foreach(Comunicacion com in comunicaciones)
					{
						System.Diagnostics.Trace.TraceInformation("Se carga la comunicación con Ref Expediente = {0} en el gridView", com.ReferenciaExp);

                        // Obtenemos el registro padre de la ficha Expedientes
                        SicerAPI.Expediente expediente = com.GetExpedientePadre();
						dataTable.Rows.Add(new object[] { com.PWSNumero,
														  Escape(com.FechaAltaExp.IsNull() ? "" : com.FechaAltaExp.GetDate().ToString("dd/MM/yyyy")),
														  Escape(com.ReferenciaExp),
														  Escape(com.FechaImpresion.IsNull() ? "" : com.FechaImpresion.GetDate().ToString("dd/MM/yyyy")),
                                                          Escape(com.NombreInter),
                                                          Escape(com.NifInter),
                                                          Escape(com.NumJustifInteco),
														  Escape(string.Format("{0:C}", com.ImporteTotal)),
                                                          Escape(DBEnumValueAttribute.Get(com.Apremio)),
                                                          Escape(expediente == null ? "" : expediente.Motivo),
                                                          Escape(ComprobarTextoATruncar(com))
                                                        });
					}

					gridViewComunicaciones.DataSource = dataTable;
					gridViewComunicaciones.DataBind();
					multiView.SetActiveView(viewConfirm);

					if(hasOmited)
					{
						System.Diagnostics.Trace.TraceWarning("Se han omitido algunas comunicaciones. La causa puede ser que no tengan expediente padre asociado o " + 
						                                      "ya hayan sido asociados a otro envío en Sicer");
						labelWarning.Text = Escape("Se han omitido algunas comunicaciones, estas están asignadas a otra remesa y no se pueden asignar a una nueva. " +
                            "A continuación se va a generar la remesa de notificaciones de las siguientes comunicaciones a Sicer, ¿continuar?");
					}
                    else
                    {
                        labelWarning.Text = "Se va a proceder a generar la remesa de notificaciones de las siguientes comunicaciones a Sicer, ¿continuar?";
                    }

					//labelWarning.Text += string.Format(labelWarning.Text, dataTable.Rows.Count);
				}
				else
				{
					if (hasOmited)
					{
						System.Diagnostics.Trace.TraceError("Todos los registros seleccionados han sido omitidos. La causa puede ser que no tengan expediente padre " +
															"asociado o ya hayan sido asociados a otro envío en Sicer");
						MostrarError("Todos los registros seleccionados han sido omitidos. La causa puede ser que no tengan expediente padre " +
						     		 "asociado o ya hayan sido asociados a otro envío en Sicer");
					}
					else
					{
						System.Diagnostics.Trace.TraceError("No hay ningún registro en la selección actual.");
						MostrarError("No hay ningún registro en la selección actual.");
					}
				}
            }
            catch (Exception exc)
            {   
				System.Diagnostics.Trace.TraceError("Excepción: " + exc.ToString());
                MostrarError("Error inesperado en al cargar información sobre las comunicaciones. " + exc.Message);
            }
        }

        private string ComprobarTextoATruncar(Comunicacion comunicacion)
        {
            try
            {
                System.Diagnostics.Trace.TraceInformation("Comprobamos texto a truncar de comunicación {0}", comunicacion.CodComunicacion);
                Code.Expediente expediente = Code.Expediente.ObtenerExpedientePorReferencia(comunicacion.ReferenciaExpediente);

                string result = "";
                RecordProvider recordPtipoIn = new RecordProvider(SessionData.UserFileSystem, Comunicacion.FileTipoIngreso);
                recordPtipoIn.Filter.AddFilter(
                    PixelwareApi.File.Records.Searching.FieldFilterData.CreateFieldFilter(
                        SessionData.UserFileSystem,
                        Comunicacion.FileTipoIngreso.Fields["DESTIPOINGRESO"],
                        PixelwareApi.Data.Filters.FilterOperator.Is,
                        comunicacion.TipoIngreso)
                );

                string remainder = null;

                if (recordPtipoIn.GetTotalCount() > 0 && recordPtipoIn.GetRecords()[0].Values["DESTINATARIO"].ValueFormatString == "Interesado")
                {
                    System.Diagnostics.Trace.TraceInformation("Se cumple el filtro de Tipo Ingreso");
                    PadRightTrim(comunicacion.NombreSolic, 50, ' ', out remainder);
                    if (remainder.Length > 0)
                    {
                        result += string.Format("Se truncarán los siguientes caracteres: '{0}' en el campo 'Nombre y apellidos' de SICER. ", remainder);
                    }

                }
                else
                {
                    System.Diagnostics.Trace.TraceInformation("No se cumple el filtro de Tipo Ingreso");
                    PadRightTrim(comunicacion.NombreInter, 50, ' ', out remainder);
                    if (remainder.Length > 0)
                    {
                        result += string.Format("Se truncarán los siguientes caracteres: '{0}' en el campo 'Nombre y apellidos' de SICER. ", remainder);
                    }
                }

                String lineaDireccion = expediente.TipoViaInter.Substring(0, 2) + ' ' + expediente.NombreViaInter + ' ' + expediente.NumViaInter + ' ' +
                    expediente.EscalInter + ' ' + expediente.PisoInter + ' ' + expediente.PuertaInter;
                PadRightTrim(lineaDireccion, 50, ' ', out remainder);
                if (remainder.Length > 0)
                {
                    result += string.Format("Se truncarán los siguientes caracteres: '{0}' en el campo 'Dirección' de SICER. ", remainder);
                }
                PadRightTrim(expediente.MunicInter, 40, ' ', out remainder);
                if (remainder.Length > 0)
                {
                    result += string.Format("Se truncarán los siguientes caracteres: '{0}' en el campo 'Población' de SICER. ", remainder);
                }
                PadLeftTrim(expediente.CodPosInter, 5, '0', out remainder);
                if (remainder.Length > 0)
                {
                    result += string.Format("Se truncarán los siguientes caracteres: '{0}' en el campo 'Código postal' de SICER. ", remainder);
                }
                return result;
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Error al comprobar textos a truncar. {0}", exc.ToString());
                return "";
            }
        }

        private static string PadLeftTrim(string text, int maxLength, char paddingChar, out string remainder)
        {
            remainder = "";
            string result = (text ?? "").PadLeft(maxLength, paddingChar);
            if (result.Length > maxLength)
            {
                remainder = result.Substring(maxLength);
                result  = result.Substring(0, maxLength);
            }

            return result;
        }

        private static string PadRightTrim(string text, int maxLength, char paddingChar, out string remainder)
        {
            remainder = "";
            string result = text.PadRight(maxLength, paddingChar);
            if (result.Length > maxLength)
            {
                remainder = result.Substring(maxLength);
                result = result.Substring(0, maxLength);
            }

            return result;
        }

        protected void linkContinuar_Click(object sender, EventArgs e)
        {
            multiView.SetActiveView(viewWait);
            ClientScript.RegisterStartupScript(this.GetType(), "generar", ClientScript.GetPostBackEventReference(linkGenerar, ""), true);
        }

        protected void linkGenerar_Click(object sender, EventArgs e)
        {
            DoWork();
        }

        private void DoWork()
        {
            String fichero = String.Empty;
            DateTime fechaDeposito;
            DateTime fechaFichero = DateTime.Now;
            List<AvisoCorreos> avisos = new List<AvisoCorreos>();

            //Comprobamos que las fechas sean correctas
            if (DateTime.TryParseExact(textBoxFechaEnvio.Text, "dd/MM/yyyy", CultureInfo.CurrentCulture.DateTimeFormat, System.Globalization.DateTimeStyles.None, out fechaDeposito))
            {
                int numEnvio = 1;

                try
                {
                    //  Se obtiene la remesa
                    Remesa remesa = Remesa.GenerarNuevaRemesa();
                    remesa.FechaDeposito = fechaDeposito;

					System.Diagnostics.Trace.TraceInformation("Se genera la remesa {0}", remesa.CodRemesa);

                    // Cabecera del fichero y remesa
					System.Diagnostics.Trace.TraceInformation("Se comienza a generar el fichero que se envía a Sicer");
                    fichero += "F" + "N" + ConfigurationManager.AppSettings["codProdSICER"] + ConfigurationManager.AppSettings["codCliSICER"] + ConfigurationManager.AppSettings["codPuntoAdm"] + String.Format("{0:yyyyMMdd}", fechaFichero) + String.Format("{0:HH:mm}", fechaFichero) + new String(' ', 282) + '\n';
                    fichero += "C" + ConfigurationManager.AppSettings["codProdSICER"] + ConfigurationManager.AppSettings["codCliSICER"] + remesa.CodRemesa + fechaFichero.Date.ToString("yyyyMMdd") + fechaDeposito.Date.ToString("yyyyMMdd") + new String(' ', 283) + '\n';

					System.Diagnostics.Trace.TraceInformation("Se obtienen todas las comunicaciones a enviar");
					bool hasOmited = false;
					List<Comunicacion> comunicaciones = Comunicacion.GetComunicaciones(Filter, Sort, out hasOmited);


                    if (comunicaciones.Count == 0)
                    {
						System.Diagnostics.Trace.TraceError("Error: El número de comunicaciones es 0 y no se puede generar la remesa");
                        MostrarError("No hay comunicaciones que enviar");
                    }
                    else
                    {
						System.Diagnostics.Trace.TraceInformation("La remesa generada tiene {0} comunicaciones. Las recorremos", comunicaciones.Count);
                        string dummy = null;

						// Se inicializan las comunicaciones
                        foreach (Comunicacion com in comunicaciones)
                        {
                            Code.Expediente expediente = Code.Expediente.ObtenerExpedientePorReferencia(com.ReferenciaExpediente);
                            
                            // Asociamos la comunicacion a la remesa
                            remesa.AsociarComunicacion(com);
							System.Diagnostics.Trace.TraceInformation("Se asocia la comunicación con referencia expediente = {0} con la remesa {1}", com.ReferenciaExpediente, remesa.CodRemesa);

                            // Inicializacion de los datos
                            // com.CodRemesa = remesa.CodRemesa;
							System.Diagnostics.Trace.TraceInformation("Se inicializan los parámetros del envío Sicer de la comunicacion");
                            com.EstadoEnvio = Comunicacion.EstadoEnvioValores.Abierta;
							com.FechaYHoraSICER = DateTime.Now;
                            com.IDComunicacionSICER = PadLeftTrim(numEnvio.ToString(), 9, '0', out dummy);
							com.SetCodigoEnvio(ConfigurationManager.AppSettings["codProdSICER"], ConfigurationManager.AppSettings["codCliSICER"]);

							System.Diagnostics.Trace.TraceInformation("La comunicación con referencia expediente = {0} cambia su estado por Abierta", com.ReferenciaExpediente);

                            RecordProvider recordPtipoIn = new RecordProvider(SessionData.UserFileSystem, Comunicacion.FileTipoIngreso);
                            recordPtipoIn.Filter.AddFilter(
                                PixelwareApi.File.Records.Searching.FieldFilterData.CreateFieldFilter(
                                    SessionData.UserFileSystem,
                                    Comunicacion.FileTipoIngreso.Fields["DESTIPOINGRESO"],
                                    PixelwareApi.Data.Filters.FilterOperator.Is,
                                    com.TipoIngreso)
                            );


                            String lineaDireccion = expediente.TipoViaInter.Substring(0, 2) + ' ' + expediente.NombreViaInter + ' ' + expediente.NumViaInter + ' ' +
                                expediente.EscalInter + ' ' + expediente.PisoInter + ' ' + expediente.PuertaInter;

                            if (recordPtipoIn.GetTotalCount() > 0 && recordPtipoIn.GetRecords()[0].Values["DESTINATARIO"].ValueFormatString == "Interesado")
                            {
								System.Diagnostics.Trace.TraceInformation("Se cumple el filtro de Tipo Ingreso");
                                fichero += "D" + ConfigurationManager.AppSettings["codProdSICER"] + ConfigurationManager.AppSettings["codCliSICER"] + remesa.CodRemesa + PadLeftTrim(numEnvio.ToString(), 9, '0', out dummy) + PadRightTrim(com.NombreInter, 50, ' ', out dummy) + new String(' ', 50);
                                
                            }
                            else
                            {
								System.Diagnostics.Trace.TraceInformation("No se cumple el filtro de Tipo Ingreso");
                                fichero += "D" + ConfigurationManager.AppSettings["codProdSICER"] + ConfigurationManager.AppSettings["codCliSICER"] + remesa.CodRemesa + PadLeftTrim(numEnvio.ToString(), 9, '0', out dummy) + PadRightTrim(com.NombreSolic, 50, ' ', out dummy) + new String(' ', 50);
                            }
                            fichero += PadRightTrim(lineaDireccion, 50, ' ', out dummy);
                            fichero += PadRightTrim(expediente.MunicInter, 40, ' ', out dummy) + PadRightTrim(expediente.CodPosInter, 5, '0', out dummy) + new String(' ', 95) + '\n';

                            AvisoCorreos aviso = new AvisoCorreos(com.Record.Values["REFERENCIAEXP"].ValueFormatString, com.CodRemesa, com.IDComunicacionSICER, string.Format("Correcto"));
                            aviso.CodigoEnvio = com.CodEnvio;
                            avisos.Add(aviso);
                            numEnvio++;
                        }

						System.Diagnostics.Trace.TraceInformation("Se genera el final del fichero de Sicer");

                        fichero += "c" + ConfigurationManager.AppSettings["codProdSICER"] + ConfigurationManager.AppSettings["codCliSICER"] + remesa.CodRemesa + PadLeftTrim(comunicaciones.Count().ToString(), 9, '0', out dummy) + new String(' ', 290) + '\n';
                        fichero += "f" + ConfigurationManager.AppSettings["codProdSICER"] + ConfigurationManager.AppSettings["codCliSICER"] + "001" + PadLeftTrim(comunicaciones.Count().ToString(), 9, '0', out dummy) + new String(' ', 291);

						System.Diagnostics.Trace.TraceInformation("Se prepara la conexión con el SFTP de Sicer");

                        ConnectionInfo cn = new ConnectionInfo(ConfigurationManager.AppSettings["servidorSFTP"], Convert.ToInt32(ConfigurationManager.AppSettings["puertoSFTP"]),
                            ConfigurationManager.AppSettings["usuarioSFTP"],
                            new PrivateKeyAuthenticationMethod(ConfigurationManager.AppSettings["usuarioSFTP"],
                                new PrivateKeyFile(System.IO.File.OpenRead(ConfigurationManager.AppSettings["rutaClaveSFTP"]))));


                        SftpClient sftp = new SftpClient(cn);
                        sftp.Connect();
						System.Diagnostics.Trace.TraceInformation("Se ha conectado al SFTP con éxito");

                        if (sftp.IsConnected)
                        {
							if (Properties.Settings.Default.UPLOAD_SICER)
							{
								String nombreFichero = "/entrada/" + ConfigurationManager.AppSettings["codProdSICER"] + ConfigurationManager.AppSettings["codCliSICER"];
								nombreFichero += String.Format("{0:yyyyMMdd}", fechaFichero) + "." + String.Format("{0:HHmm}", fechaFichero);
								System.Diagnostics.Trace.TraceInformation("Se sube el fichero al SFTP de Sicer en {0}", nombreFichero);
								sftp.WriteAllText(nombreFichero, fichero, Encoding.GetEncoding(1252));
							}
                            else
                            {
                                MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Information, 0, "El parámetro UPLOAD_SICER es false: No se sube el fichero al SFTP");
                                MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Information, 0, " ======================= FICHERO GENERADO ======================== ");
                                MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Information, 0, "{0}", fichero);
                                MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Information, 0, " ==================== FIN DE FICHERO GENERADO ==================== ");
                            }

                            // Se actualiza la base de datos
                            using (DbTransactionScope trans = SicerAPI.SessionData.UserFileSystem.Database.ComienzaTransaccion())
                            {
                                try
                                {
									System.Diagnostics.Trace.TraceInformation("Se actualiza la base de datos con la remesa creada");
                                    Remesa newRemesa = remesa.CreateDBCascada();

                                    // Adjuntamos el fichero a la remesa creada
                                    string nombreFichero = "Datos_" + ConfigurationManager.AppSettings["codProdSICER"] + "_" + ConfigurationManager.AppSettings["codCliSICER"];
                                    System.Diagnostics.Trace.TraceInformation("Adjuntado documento {0} a la remsea {1}", nombreFichero, newRemesa.CodRemesa);
                                    Document.CreateDocument(SessionData.UserFileSystem, newRemesa.Record, nombreFichero, ".txt", Encoding.GetEncoding(1252).GetBytes(fichero));

                                    trans.Complete();
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Trace.TraceError("Ha ocurrido un error al actualizar la base de datos: {0}", ex);
                                    MostrarError("Ha ocurrido un error al actualizar la base de datos");
                                }
                            }

							System.Diagnostics.Trace.TraceInformation("Se desconecta del SFTP");
                            sftp.Disconnect();

                            multiView.SetActiveView(viewSuccess);
                            literalNumRegistros.Text = string.Format("Se han enviado {0} comunicaciones con éxito de {1}.\n", numEnvio - 1, avisos.Count);
							System.Diagnostics.Trace.TraceInformation("Se han enviado {0} comunicaciones con éxito de {1}", numEnvio - 1, avisos.Count);


                            panelWarnings.Visible = avisos.Count > 0;
                            if (avisos.Count > 0)
                            {
								System.Diagnostics.Trace.TraceInformation("Se crea la tabla informativa de los resultado del envío");
                                foreach (AvisoCorreos aviso in avisos)
                                {
                                    TableCell cellRefExpediente = new TableCell();
                                    cellRefExpediente.Text = aviso.RefExpediente;
                                    TableCell cellCodRemesa = new TableCell();
                                    cellCodRemesa.Text = aviso.CodRemesa;
                                    TableCell cellIdComunicacion = new TableCell();
                                    cellIdComunicacion.Text = aviso.IdComunicacion;
                                    TableCell cellCodigoNT = new TableCell();
                                    cellCodigoNT.Text = aviso.CodigoEnvio;
                                    TableCell cellIncidencia = new TableCell();
                                    cellIncidencia.Text = aviso.Incidencia;
                                    TableRow newRow = new TableRow();
                                    newRow.Cells.AddRange(new TableCell[] { cellRefExpediente, cellCodRemesa, cellIdComunicacion, cellCodigoNT, cellIncidencia });
                                    tableIncidencias.Rows.Add(newRow);
                                }
                            }
                        }
                        else
                        {
                            System.Diagnostics.Trace.TraceError("No se ha podido conectar con el SFTP");
                            MostrarError("No se ha podido conectar con el SFTP");
                        }
                    }
                }
                catch(Exception e)
                {
                    System.Diagnostics.Trace.TraceError("Excepción: " + e);
                    MostrarError("Error inesperado en la generacion de remesa. " + e.Message);
                }
            }
            else
            {
                System.Diagnostics.Trace.TraceError("Fecha inválida");
                MostrarError("Fecha inválida");
            }
        }

        private void MostrarError(string error)
        {
			System.Diagnostics.Trace.TraceError("Se ha producido un error: {0}", error);
            literalError.Text = error;
            multiView.SetActiveView(viewError);
        }

        private static string Escape(string text)
        {
            return text.Trim();
        }

        private string Filter
        {
            get { return (string)ViewState["filter"]; }
            set { ViewState["filter"] = value; }
        }
        private string Sort
        {
            get { return (string)ViewState["sort"]; }
            set { ViewState["sort"] = value; }
        }
        private string Index
        {
            get { return (string)ViewState["index"]; }
            set { ViewState["index"] = value; }
        }
    }
}