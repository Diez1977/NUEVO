using AltaPagos.Code;
using PixelwareApi.Common.Utils;
using PixelwareApi.File;
using PixelwareApi.File.Records;
using SicerAPI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AltaPagos
{
    public partial class DetallesRemesa : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                for (int i = 1; i <= 17; i++)
                {
                    ListItem listItem = new ListItem(i.ToString(), i.ToString());
                    filaImpresion.Items.Add(listItem);
                }

                Remesa remesa = Remesa.LoadRemesa(Request.QueryString["codRemesa"]);
                bool remesaAceptada = remesa.Estado == Remesa.EstadoValores.Abierta;

			    // TODO: Sustituir todo este codigo por uno usando la API de SICER
                MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Information, 0, "Se obtienen las comunicaciones para la remesa {0}", Request.QueryString["codRemesa"]);

			    UserFileSystem userFileSystem = ConexiónAlmacen.ObtenerUserFileSystem();
                FileView fileViewComunicaciones = ConexiónAlmacen.ObtenerFileViewComunicaciones();
                RecordProvider recordProvider = new RecordProvider(userFileSystem, fileViewComunicaciones);
                recordProvider.Filter.AddFilter(PixelwareApi.File.Records.Searching.FieldFilterData.CreateFieldFilter(userFileSystem,
                    fileViewComunicaciones.Fields["CODREMESA"],
                    PixelwareApi.Data.Filters.FilterOperator.Is,
                    Request.QueryString["codRemesa"]));

                long count = recordProvider.GetTotalCount();
                if (count > 0)
                {
				    MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Information, 0,"Se han encontrado {0} comunicaciones", count);

                    DataTable dataTable = new DataTable();
                    dataTable.Columns.Add("PWSNUMERO", typeof(decimal));
                    dataTable.Columns.Add("REFERENCIAEXP", typeof(string));
                    dataTable.Columns.Add("NUMJUSTIFINTECO", typeof(string));
                    dataTable.Columns.Add("CODENVIO", typeof(string));
                    dataTable.Columns.Add("NUMDOCIDENTIFINTERPROPAG", typeof(string));
                    dataTable.Columns.Add("FECHAACTUALIZACIONSICER", typeof(string));

            	    dataTable.Columns.Add("ESTADOENVIO", typeof(string));
                    dataTable.Columns.Add("LINKBUTTON");

                    List<string> pwsNumeros = new List<string>();
				    List<bool> canPrintEtiqueta = new List<bool>();
                    using (RecordReader recordReader = recordProvider.GetRecordsReader())
                    {
                        while (recordReader.Read())
                        {
                            Record record = (Record)recordReader.Current;

                            string idComunicacionTraza = record["IDENVIOSICER"].IsNull() ? "<NULL>" : record["IDENVIOSICER"].ValueFormatString;
                            string estadoComunicacionTraza = record["ESTADOENVIO"].IsNull() ? "<NULL>" : record["ESTADOENVIO"].ValueFormatString;

						    MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Information, 0,"Se procesa la comunicacion {0} : tiene el estado {1}", idComunicacionTraza, estadoComunicacionTraza);

                            string referenciaExp = Escape(record["REFERENCIAEXP"].ToStringFormat());
                            string estadoEnvio = record["ESTADOENVIO"].IsNull() ? "NO ASIGNADO" : Escape(record["ESTADOENVIO"].ValueFormatString);
                            string estadoEnvioAux = record["ESTADOENVIO"].IsNull() ? "" : Escape(record["ESTADOENVIO"].ValueFormatString);
                            string codigoIntenco = record["NUMJUSTIFINTECO"].IsNull() ? "" : Escape(record["NUMJUSTIFINTECO"].ValueFormatString);
                            string codigoEnvio = record["CODENVIO"].IsNull() ? "" : Escape(record["CODENVIO"].ValueFormatString);
                            string nif = record["NUMDOCIDENTIFINTERPROPAG"].IsNull() ? "" : Escape(record["NUMDOCIDENTIFINTERPROPAG"].ValueFormatString);

                            string fechaString = string.Empty;
                            if(!record["FECHAACTUALIZACIONSICER"].IsNull())
                            {
                                DateTime fecha = Convert.ToDateTime(record["FECHAACTUALIZACIONSICER"].Value);
                                fechaString = fecha.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                            }

                            pwsNumeros.Add(Convert.ToString(record.Number));
                            canPrintEtiqueta.Add(estadoEnvioAux == DBEnumValueAttribute.Get(Comunicacion.EstadoEnvioValores.Aceptada));

                            dataTable.Rows.Add(new object[] { record.Number, referenciaExp, codigoIntenco, codigoEnvio, nif, fechaString, estadoEnvio });
                        }
                    }

                    gridViewRemesas.DataSource = dataTable;
                    gridViewRemesas.DataBind();

                    int i = 0;
                    foreach (GridViewRow row in gridViewRemesas.Rows)
                    {
                        Label labelDocumento = row.FindControl("documentoLbl") as Label;

                        if (canPrintEtiqueta[i] & remesaAceptada)
                        {
                            labelDocumento.Text = string.Format("<a id='btnDescargar_{0}' href='ImprimirCodigosSicer.aspx?modo=1&pwsNumeroComunicacion={0}' " +
                                "class='BotonActivo btDownload' onclick='javascript: abrirPanelConfirmacion(\"{0}\"); return false;' " +
                                "title='Generación y descarga de etiqueta'>Generar etiqueta</a>", pwsNumeros[i]);
                        }
                        else
                        {
                            labelDocumento.Text = "La comunicación se encuentra en un Estado de Envío SICER final";
                        }

                        i++;
                    }

				    multiView.SetActiveView(viewRemesa);
                }
			    else
			    {
                    MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Warning, 0, "La remesa {0} no tiene comunicaciones asociadas", Request.QueryString["codRemesa"]);
				    MostrarError(string.Format("La remesa {0} no tiene comunicaciones asociadas", Request.QueryString["codRemesa"]));
			    }
            }
			catch (Exception exception)
			{
                MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Error, 0, "Exception: {0}", exception.ToString());
				MostrarError(string.Format("{0}", exception.Message));
			}
        }

        private static string Escape(string text)
        {
            return text.Trim();
        }

		private void MostrarError(string error)
		{
			System.Diagnostics.Trace.TraceError(error);
			literalError.Text = Escape(error);
			multiView.SetActiveView(viewError);
		}

	}
}