using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PixelwareApi.File;
using PixelwareApi.File.Records.Searching;
using System.Configuration;
using PixelwareApi.File.Records;
using System.Text;
using System.IO;
using AltaPagos.Code;
using PixelwareApi.File.Relations;
using System.Text.RegularExpressions;
using NPOI.HSSF.UserModel;
using NPOI.HPSF;
using NPOI.SS.UserModel;
using PixelwareApi.File.UserActions;
using Vivienda.Operaciones;
using PixelwareApi.Data;
using System.Data;

namespace AltaPagos
{
    public partial class JustificanteMasivo : System.Web.UI.Page
    {
     
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Index = Request["index"];
                Filter = Request["filter"];
                Sort = Request["sort"];
                CargarConfirmacion();
            }
        }

        private void CargarConfirmacion()
        {
            try
            {
                UserFileSystem ufs = ConexiónAlmacen.ObtenerUserFileSystem();

                FileView fileComunicaciones = ConexiónAlmacen.ObtenerFileViewComunicaciones();
                RecordFilterStringSerializer serializer = new RecordFilterStringSerializer(ufs);
                RecordFilter recordFilter = serializer.DeserializeFromString(Filter, fileComunicaciones);
                Relation relExpComunicaciones = ConexiónAlmacen.ObtenerRelationExpComunicaciones();

                // Consultamos todos los registros de Comunicaciones
                RecordProvider recordProvider = new RecordProvider(ufs, fileComunicaciones);
                recordProvider.Filter = recordFilter;
                recordProvider.AddSortExpression(Sort);
                long count = recordProvider.GetTotalCount();
                if (count > 0)
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Columns.Add("PWSNUMERO", typeof(decimal));
                    dataTable.Columns.Add("FECHAALTAEXP", typeof(string));
                    dataTable.Columns.Add("REFERENCIAEXP", typeof(string));
                    dataTable.Columns.Add("FECHALIQUID", typeof(string));
                    dataTable.Columns.Add("NOMBREINTERPROPAG", typeof(string));
                    dataTable.Columns.Add("NUMDOCIDENTIFINTERPROPAG", typeof(string));
                    dataTable.Columns.Add("MOTIVOEXPEDICION", typeof(string));
                    dataTable.Columns.Add("IMPORTETOTAL", typeof(string));
                    dataTable.Columns.Add("MUNICVIVPROPAG", typeof(string));

                    // Recorremos cada comunicación
                    List<AvisoCorreos> avisos = new List<AvisoCorreos>();
                    RecordReader recordReaderComunicaciones = recordProvider.GetRecordsReader();
                    while (recordReaderComunicaciones.Read())
                    {
                        Record recordComunicacion = (Record)recordReaderComunicaciones.Current;

                        // Obtenemos el registro padre de la ficha Expedientes
                        RelationRecordProvider relationRecordProvider = new RelationRecordProvider(ufs, relExpComunicaciones, recordComunicacion);
                        relationRecordProvider.Type = RelationRecordProviderType.ParentRelatedOnes;
                        long countAux = relationRecordProvider.GetTotalCount();
                        if (countAux == 0)
                        {
                            // La comunicacion no está asociada a un padre así que pasamos de ella...
                            continue;
                        }
                        Record recordExpediente = relationRecordProvider.GetRelationRecords()[0].ParentPart;

                        string fechaAltaExp = recordComunicacion["FECHAALTAEXP"].IsNull() ? "" : Escape(string.Format("{0:dd/MM/yyyy}", recordComunicacion["FECHAALTAEXP"].Value));
                        string referenciaExp = Escape(recordComunicacion["REFERENCIAEXP"].ValueFormatString);
                        string fechaLiquidacion = recordComunicacion["FECHACOMUNIC"].IsNull() ? "" : Escape(string.Format("{0:dd/MM/yyyy}", recordComunicacion["FECHACOMUNIC"].Value));
                        string nombreApellidos = Escape(recordComunicacion["NOMBREINTERPROPAG"].ValueFormatString);
                        string nif = Escape(recordComunicacion["NUMDOCIDENTIFINTERPROPAG"].ValueFormatString);
                        string motivo = Escape(recordExpediente["MOTIVOEXPEDICION"].ValueFormatString);
                        string importeTotal = recordComunicacion["IMPORTETOTAL"].IsNull() ? "" :
                            Escape(string.Format("{0:C}", recordComunicacion["IMPORTETOTAL"].Value));
                        string municipio = Escape(recordComunicacion["MUNICVIVPROPAG"].ValueFormatString);

                        dataTable.Rows.Add(new object[] { recordComunicacion.Number, fechaAltaExp, referenciaExp, fechaLiquidacion, nombreApellidos, 
                            nif, motivo, importeTotal, municipio});
                    }
                    gridViewComunicaciones.DataSource = dataTable;
                    gridViewComunicaciones.DataBind();
                    mainMultiView.SetActiveView(viewConfirm);
                    labelConfirm.Text = string.Format(labelConfirm.Text, dataTable.Rows.Count);
                }
                else
                {
                    MostrarError("No hay ningún registro en la selección actual.");
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Excepción: " + exc.ToString());
                MostrarError("Error inesperado en al cargar información sobre las comunicaciones. " + exc.Message);
            }
        }

        protected void linkContinuar_Click(object sender, EventArgs e)
        {
            mainMultiView.SetActiveView(viewWait);
            ClientScript.RegisterStartupScript(this.GetType(), "generar", ClientScript.GetPostBackEventReference(linkGenerar, ""), true);
        }

        protected void linkGenerar_Click(object sender, EventArgs e)
        {
            DoWork();
        }

        private void DoWork()
        {
            try
            {
                System.Diagnostics.Trace.TraceInformation("Inicio del proceso de generación de modelo 069 masivo");
                
                UserFileSystem ufs = ConexiónAlmacen.ObtenerUserFileSystem();
               
                FileView fileComunicaciones = ConexiónAlmacen.ObtenerFileViewComunicaciones();
                RecordFilterStringSerializer serializer = new RecordFilterStringSerializer(ufs);
                RecordFilter recordFilter = serializer.DeserializeFromString(Filter, fileComunicaciones);
                Relation relExpComunicaciones = ConexiónAlmacen.ObtenerRelationExpComunicaciones();

                // Consultamos todos los registros de Comunicaciones
                RecordProvider recordProvider = new RecordProvider(ufs, fileComunicaciones);
                recordProvider.Filter = recordFilter;
                recordProvider.AddSortExpression(Sort);
                long count = recordProvider.GetTotalCount();
                long success = 0;
                System.Diagnostics.Trace.TraceInformation("Procesando {0} comunicaciones", count);
                if (count > 0)
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Columns.Add("PWSNUMERO", typeof(decimal));
                    dataTable.Columns.Add("FECHAALTAEXP", typeof(string));
                    dataTable.Columns.Add("REFERENCIAEXP", typeof(string));
                    dataTable.Columns.Add("FECHALIQUID", typeof(string));
                    dataTable.Columns.Add("NOMBREINTERPROPAG", typeof(string));
                    dataTable.Columns.Add("NUMDOCIDENTIFINTERPROPAG", typeof(string));
                    dataTable.Columns.Add("MOTIVOEXPEDICION", typeof(string));
                    dataTable.Columns.Add("IMPORTETOTAL", typeof(string));
                    dataTable.Columns.Add("MUNICVIVPROPAG", typeof(string));
                    dataTable.Columns.Add("ESTADOPROPAG", typeof(string));
                    dataTable.Columns.Add("RESULTADO", typeof(string));

                    // Recorremos cada comunicación
                    List<AvisoCorreos> avisos = new List<AvisoCorreos>();
                    RecordReader recordReaderComunicaciones = recordProvider.GetRecordsReader();
                    while (recordReaderComunicaciones.Read())
                    {
                        Record recordComunicacion = (Record)recordReaderComunicaciones.Current;
                        System.Diagnostics.Trace.TraceInformation("Procesando comunicación pwsnumero={0}", recordComunicacion.Number);

                        // Obtenemos el registro padre de la ficha Expedientes
                        RelationRecordProvider relationRecordProvider = new RelationRecordProvider(ufs, relExpComunicaciones, recordComunicacion);
                        relationRecordProvider.Type = RelationRecordProviderType.ParentRelatedOnes;
                        List<RelationRecord> relationRecords = relationRecordProvider.GetRelationRecords();
                        if (relationRecords.Count == 0)
                        {
                            // La comunicacion no está asociada a un padre así que pasamos de ella...
                            continue;
                        }

                        Record recordExpediente = relationRecords[0].ParentPart;
                        System.Diagnostics.Trace.TraceInformation("Procesando expediente pwsnumero={0}", recordExpediente.Number);
                        string codExpediente = Escape(relationRecords[0].ParentPart["CODEXPEDIENTE"].ValueFormatString);
                        string referenciaExp = Escape(relationRecords[0].ParentPart["REFERENCIAEXP"].ValueFormatString);
                        string codComunicacion = Escape(recordComunicacion["CODCOMUNICACION"].ValueFormatString);
                        string estado = "";
                        string resultado = "";

                        try
                        {
                            GeneracionJustificante generacionJustificante = new GeneracionJustificante(ufs);
                            generacionJustificante.GenerarDescargarJustificante(recordExpediente, recordComunicacion, true);
                            System.Diagnostics.Trace.TraceInformation("Modelo 069 generado con éxito");
                            resultado = "Modelo 069 generado con éxito";
                            success++;
                        } catch (Exception exc) {
                            avisos.Add(new AvisoCorreos(referenciaExp, codComunicacion, string.Format("Error al generar modelo 069: {0}", exc.Message)));
                            resultado = string.Format("Error al generar modelo 069: {0}", exc.Message);
                            System.Diagnostics.Trace.TraceError("Excepción al generar modelo 069: " + exc.ToString());
                        }
                        finally
                        {
                            try
                            {
                                Record recordComunicacion2 = Record.LoadRecord(ufs, fileComunicaciones, recordComunicacion.Number);
                                estado = recordComunicacion2["ESTADOPROPAG"].ValueFormatString;
                            }
                            catch (Exception exc)
                            {
                                System.Diagnostics.Trace.TraceError("Excepción al leer estado final de la comunicación: " + exc.ToString());
                            }

                            // Recargamos el expediente y la comunicacion por los controles de acciones
                            recordExpediente = Record.LoadRecord(ufs, ConexiónAlmacen.ObtenerFileViewExpedientes(), recordExpediente.Number);
                            recordComunicacion = Record.LoadRecord(ufs, ConexiónAlmacen.ObtenerFileViewComunicaciones(), recordComunicacion.Number); 

                            string fechaAltaExp = recordComunicacion["FECHAALTAEXP"].IsNull() ? "" : Escape(string.Format("{0:dd/MM/yyyy}", recordComunicacion["FECHAALTAEXP"].Value));
                            string fechaLiquidacion = recordComunicacion["FECHACOMUNIC"].IsNull() ? "" : Escape(string.Format("{0:dd/MM/yyyy}", recordComunicacion["FECHACOMUNIC"].Value));
                            string nombreApellidos = Escape(recordComunicacion["NOMBREINTERPROPAG"].ValueFormatString);
                            string nif = Escape(recordComunicacion["NUMDOCIDENTIFINTERPROPAG"].ValueFormatString);
                            string motivo = Escape(recordExpediente["MOTIVOEXPEDICION"].ValueFormatString);
                            string importeTotal = recordComunicacion["IMPORTETOTAL"].IsNull() ? "" :
                                Escape(string.Format("{0:C}", recordComunicacion["IMPORTETOTAL"].Value));
                            string municipio = Escape(recordComunicacion["MUNICVIVPROPAG"].ValueFormatString);

                            dataTable.Rows.Add(new object[] { recordComunicacion.Number, fechaAltaExp, referenciaExp, fechaLiquidacion, nombreApellidos, 
                            nif, motivo, importeTotal, municipio, estado, resultado});
                        }
                    }

                    mainMultiView.SetActiveView(viewSuccess);
                    literalNumRegistros.Text = string.Format("Se han generado {0} de {1} modelos 069 con éxito.", success, count);

                    panelWarnings.Visible = true;
                    subtitleLabel.Visible = success != count;

                    //if (avisos.Count > 0)
                    //{
                        gridResultado.DataSource = dataTable;
                        gridResultado.DataBind();
                    //}
                }
                else
                {
                    MostrarError("No hay ningún registro en la selección actual.");
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Excepción: " + exc.ToString());
                MostrarError("Error inesperado en la generación de modelo 069. " + exc.Message);
            }
        }

        
        private void MostrarError(string error)
        {
            literalError.Text = error;
            mainMultiView.SetActiveView(viewError);
        }

        private static string ConcatenateSpace(string text, string textToAdd)
        {
            string result = text;
            if (textToAdd.Length > 0)
            {
                if (text.Length > 0)
                {
                    result += " ";
                }
                result += textToAdd;
            }
            return result;
        }

        private static bool CheckSize(string text, int maxLength, out string newText, out string remainder)
        {
            if (text.Length > maxLength)
            {
                newText = text.Substring(0, maxLength);
                remainder = text.Substring(maxLength);
                return false;
            }
            else
            {
                newText = text;
                remainder = "";
                return true;
            }
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