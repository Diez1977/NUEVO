using AltaPagos.Code;
using PixelwareApi.Data;
using PixelwareApi.File;
using PixelwareApi.File.Documents;
using PixelwareApi.File.Records;
using Renci.SshNet;
using SicerAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AltaPagos
{
    public partial class ComprobarEstados : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargarConfirmacion();
            }
        }

        private void CargarConfirmacion()
        {
			try
			{
				DoWorkFicheroRemesa();
			}
			catch(Exception exception)
			{
                MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Error, 0, "Se ha detectado el siguiente error: {0}", exception);
			}
        }

        private void DoWorkFicheroRemesa()
        {
            for(int i = 1; i <= 17; i++)
            {
                ListItem listItem = new ListItem(i.ToString(), i.ToString());
                filaImpresion.Items.Add(listItem);
            }

            UserFileSystem userFileSystem = ConexiónAlmacen.ObtenerUserFileSystem();
            decimal indiceRemesa = decimal.Parse(ConfigurationManager.AppSettings["indiceRemesa"]);
            FileView fileViewRemesa = SchemeNode.LoadNodeById(userFileSystem, indiceRemesa).GetFile();
            RecordProvider recordPRemesa = new RecordProvider(userFileSystem, fileViewRemesa);

            long count = recordPRemesa.GetTotalCount();
            if (count > 0)
            {
                MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Information, 0, "Se muestran los estados de {0} remesas", count);

                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("PWSNUMERO", typeof(decimal));
                dataTable.Columns.Add("CODREMESA", typeof(string));
                dataTable.Columns.Add("ESTADO");
                dataTable.Columns.Add("LINKBUTTON");

                List<String> codRemesas = new List<String>();
                List<String> estadosRemesas = new List<String>();
                using (RecordReader recordReader = recordPRemesa.GetRecordsReader())
                {
                    while (recordReader.Read())
                    {
                        Record record = (Record)recordReader.Current;

                        codRemesas.Add(Escape(record["CODREMESA"].ValueFormatString));
                        estadosRemesas.Add(Escape(record["ESTADO"].ValueFormatString));

                        dataTable.Rows.Add(new object[] { record.Number, codRemesas.Last()});
                    }
                }

                gridViewRemesas.DataSource = dataTable;
                gridViewRemesas.DataBind();

                int i = 0;
                foreach (String estadoRemesa in estadosRemesas)
                {
                    if (estadoRemesa == "Abierta")
                    {
                        GridViewRow row = gridViewRemesas.Rows[i];

                        HyperLink hyperLink = row.FindControl("link") as HyperLink;
                        hyperLink.Text = estadoRemesa;
                        hyperLink.NavigateUrl = "javascript:showWindow('DetallesRemesa.aspx?codRemesa=" + codRemesas[i] + "')";

                        Label labelDocumento = row.FindControl("documentoLbl") as Label;
                        labelDocumento.Text = string.Format("<a id='btnDescargar_{0}' href='ImprimirCodigosSicer.aspx?modo=0&codigoRemesa={0}' " +
                            "class='BotonActivo btDownload' onclick='javascript: abrirPanelConfirmacion(\"{0}\"); return false;' " +
                            "title='Generación y descarga de etiquetas'>Generar etiquetas</a>", codRemesas[i]);
                        
                        // Se comprueba el estado de todas las comunicaciones
                        Remesa remesaProcesada = Remesa.LoadRemesa(codRemesas[i]);
                        bool comunicacionesAceptadas = true;
                        foreach(Comunicacion comunicacion in remesaProcesada.Comunicaciones)
                        {
                            comunicacionesAceptadas &= comunicacion.EstadoEnvio == Comunicacion.EstadoEnvioValores.Aceptada;
                            if (!comunicacionesAceptadas)
                            {
                                // Robustez: Solo se pueden imprimir etiquetas de aquellas remesas con todas sus comunicaciones aceptadas
                                labelDocumento.Text = "Algunas comunicaciones de esta remesa están en un Estado de Envío SICER final";
                                break;
                            }
                        }
                    }
                    else if (estadoRemesa == "Errores")
                    {
                        GridViewRow row = gridViewRemesas.Rows[i];

                        HyperLink hl = row.FindControl("link") as HyperLink;
                        hl.Text = estadoRemesa;
                        hl.ToolTip = "Ir al detalle de la remesa";
                        hl.NavigateUrl = "javascript:showWindow('ErroresRemesa.aspx?codRemesa=" + codRemesas[i] + "')";

                        LinkButton linkButton = row.FindControl("lbReenvioRemesa") as LinkButton;
                        linkButton.Visible = true;
                        linkButton.CommandArgument = codRemesas[i];

                        LinkButton linkButton2 = row.FindControl("lbEtiquetasRemesa") as LinkButton;
                        linkButton2.Visible = false;
                    }
                    else
                    {
                        GridViewRow row = gridViewRemesas.Rows[i];

                        HyperLink hyperLink = row.FindControl("link") as HyperLink;
                        hyperLink.Text = estadoRemesa;
                    }

                    i++;
                }

                multiView.SetActiveView(viewFichero);
            }
            else
            {
                MostrarError("No hay ningún registro en la selección actual.");
            }
        }

        protected void linkButtonReenviarRemesa_Click(object sender, EventArgs e)
        {
            multiView.SetActiveView(viewWait);
            LinkButton button = (LinkButton)sender;
            linkRemesa.CommandArgument = button.CommandArgument;
            ScriptManager.RegisterStartupScript(this, this.GetType(), "generar", ClientScript.GetPostBackEventReference(linkRemesa, ""), true);
        }

        protected void linkRemesa_Click(object sender, EventArgs e)
        {
            LinkButton button = (LinkButton)sender;
            DoWorkReenvioRemesa(button.CommandArgument);
        }

        private void DoWorkReenvioRemesa(string codRemesa)
        {
            String fichero = String.Empty;
            DateTime fechaFichero = DateTime.Now;

            try
            {
                MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Information, 0, "Se va a reenviar la remesa {0}", codRemesa);

                //  Se obtiene la remesa
                Remesa remesa = Remesa.LoadRemesa(codRemesa);

                // Cabecera del fichero y remesa
                fichero += "F" + "N" + ConfigurationManager.AppSettings["codProdSICER"] + ConfigurationManager.AppSettings["codCliSICER"] + ConfigurationManager.AppSettings["codPuntoAdm"] + String.Format("{0:yyyyMMdd}", fechaFichero) + String.Format("{0:HH:mm}", fechaFichero) + new String(' ', 282) + '\n';
                fichero += "C" + ConfigurationManager.AppSettings["codProdSICER"] + ConfigurationManager.AppSettings["codCliSICER"] + remesa.CodRemesa + fechaFichero.Date.ToString("yyyyMMdd") + remesa.FechaDeposito.Date.ToString("yyyyMMdd") + new String(' ', 283) + '\n';


                // Se inicializan las comunicaciones
                MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Information, 0, "La remesa tiene {0} comunicaciones asociadas", remesa.Comunicaciones.Count);

                int numEnvio = 1;
                foreach (Comunicacion com in remesa.Comunicaciones)
                {
                    Code.Expediente expediente = Code.Expediente.ObtenerExpedientePorReferencia(com.ReferenciaExpediente);

                    // Inicializacion de los datos
                    MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Information, 0, "Se actualiza el estado a Abierto y la hora de envío de la comunicación {0}", com.CodComunicacion);

                    com.EstadoEnvio = Comunicacion.EstadoEnvioValores.Abierta;
					com.FechaYHoraSICER = DateTime.Now;

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
                        MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Information, 0, "Se cumple el filtro de Tipo Ingreso");

                        fichero += "D" + ConfigurationManager.AppSettings["codProdSICER"] + ConfigurationManager.AppSettings["codCliSICER"] + remesa.CodRemesa + numEnvio.ToString().PadLeft(9, '0') + com.NombreInter.PadRight(50, ' ') + new String(' ', 50);
                        fichero += lineaDireccion.PadRight(50, ' ');
                        fichero += expediente.MunicInter.PadRight(40, ' ') + expediente.CodPosInter + new String(' ', 95) + '\n';
                    }
                    else
                    {
                        MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Information, 0, "No se cumple el filtro de Tipo Ingreso");

                        fichero += "D" + ConfigurationManager.AppSettings["codProdSICER"] + ConfigurationManager.AppSettings["codCliSICER"] + remesa.CodRemesa + numEnvio.ToString().PadLeft(9, '0') + com.NombreSolic.PadRight(50, ' ') + new String(' ', 50);
                        fichero += lineaDireccion.PadRight(50, ' ');
                        fichero += expediente.MunicInter.PadRight(40, ' ') + expediente.CodPosInter + new String(' ', 95) + '\n';
                    }

                    numEnvio++;
                }

                fichero += "c" + ConfigurationManager.AppSettings["codProdSICER"] + ConfigurationManager.AppSettings["codCliSICER"] + remesa.CodRemesa + remesa.Comunicaciones.Count.ToString().PadLeft(9, '0') + new String(' ', 290) + '\n';
                fichero += "f" + ConfigurationManager.AppSettings["codProdSICER"] + ConfigurationManager.AppSettings["codCliSICER"] + "001" + remesa.Comunicaciones.Count.ToString().PadLeft(9, '0') + new String(' ', 291);

				ConnectionInfo cn = new ConnectionInfo(ConfigurationManager.AppSettings["servidorSFTP"], 
                    Convert.ToInt32(ConfigurationManager.AppSettings["puertoSFTP"]),
					ConfigurationManager.AppSettings["usuarioSFTP"],
					new PrivateKeyAuthenticationMethod(ConfigurationManager.AppSettings["usuarioSFTP"],
					new PrivateKeyFile(System.IO.File.OpenRead(ConfigurationManager.AppSettings["rutaClaveSFTP"]))));

                SftpClient sftp = new SftpClient(cn);
                sftp.Connect();

                MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Information, 0, "Se ha conectado al SFTP con éxito");

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
                    using (DbTransactionScope transaction = SicerAPI.SessionData.UserFileSystem.Database.ComienzaTransaccion())
                    {
                        try
                        {
                            System.Diagnostics.Trace.TraceInformation("Se actualiza la base de datos con la remesa creada");
                            remesa.Estado = Remesa.EstadoValores.Bloqueada;
                            remesa.ActualizarEstadoCascada();

                            // Adjuntamos el fichero a la remesa creada
                            byte[] bytes = new byte[fichero.Length * sizeof(char)];
                            System.Buffer.BlockCopy(fichero.ToCharArray(), 0, bytes, 0, bytes.Length);

                            string nombreFichero = "Datos_" + ConfigurationManager.AppSettings["codProdSICER"] + "_" + ConfigurationManager.AppSettings["codCliSICER"];
                            System.Diagnostics.Trace.TraceInformation("Adjuntado documento {0} a la remsea {1}", nombreFichero, remesa.CodRemesa);
                            Document.CreateDocument(SessionData.UserFileSystem, remesa.Record, nombreFichero, ".txt", bytes);

                            transaction.Complete();
                        }
                        catch (Exception exception)
                        {
                            MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Error, 0, "Ha ocurrido un error al actualizar la base de datos: {0}", exception);
                            MostrarError("Ha ocurrido un error al actualizar la base de datos");
                        }
                    }

                    MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Information, 0, "Se desconecta del SFTP");
                    sftp.Disconnect();
                }
                else
                {
                    MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Error, 0, "No se ha podido conectar con el SFTP");
                    MostrarError("No se ha podido conectar con el SFTP");
                }
            }
            catch (Exception exception)
            {
                MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Error, 0, "Excepción: " + exception.ToString());
                MostrarError("Error inesperado en la generacion de remesa. " + exception.Message);
            }
            
            multiView.SetActiveView(viewSuccess);
        }

        private void MostrarError(string error)
        {
			System.Diagnostics.Trace.TraceError(error);
            literalError.Text = error;
            multiView.SetActiveView(viewError);
        }

        private static string Escape(string text)
        {
            return text.Trim();
        }

    }
}