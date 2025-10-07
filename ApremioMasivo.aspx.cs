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
    public partial class ApremioMasivo : System.Web.UI.Page
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
                    dataTable.Columns.Add("FECHACOMUNIC", typeof(string));
                    dataTable.Columns.Add("ESTADOCOMUNIC", typeof(string));
                    dataTable.Columns.Add("NUMJUSTIFINTECO", typeof(string));
                    dataTable.Columns.Add("ESTADOINTECO", typeof(string));
                    dataTable.Columns.Add("IMPORTETOTAL", typeof(string));
                    dataTable.Columns.Add("FECHAREGSALIDA", typeof(string));

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

                        string fechaAltaExp = recordComunicacion["FECHAALTAEXP"].IsNull() ? "" : Escape(string.Format("{0:dd/MM/yyyy}", recordComunicacion["FECHAALTAEXP"].Value));
                        string referenciaExp = Escape(recordComunicacion["REFERENCIAEXP"].ValueFormatString);
                        string fechaImpresion = recordComunicacion["FECHACOMUNIC"].IsNull() ? "" : Escape(string.Format("{0:dd/MM/yyyy}", recordComunicacion["FECHACOMUNIC"].Value));
                        string estadoComunic = Escape(recordComunicacion["ESTADOCOMUNIC"].ValueFormatString);
                        string numJustifInteco = Escape(recordComunicacion["NUMJUSTIFINTECO"].ValueFormatString);
                        string estadoInteco = Escape(recordComunicacion["ESTADOINTECO"].ValueFormatString);
                        string importeTotal = recordComunicacion["IMPORTETOTAL"].IsNull() ? "" :
                            Escape(string.Format("{0:C}", recordComunicacion["IMPORTETOTAL"].Value));
                        string fechaRegSalida = recordComunicacion["FECHAREGSALIDA"].IsNull() ? "" : Escape(string.Format("{0:dd/MM/yyyy}", recordComunicacion["FECHAREGSALIDA"].Value));

                        dataTable.Rows.Add(new object[] { recordComunicacion.Number, fechaAltaExp, referenciaExp, fechaImpresion, estadoComunic, 
                            numJustifInteco, estadoInteco, importeTotal, fechaRegSalida});
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
                System.Diagnostics.Trace.TraceError("Excepción: " + exc);
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
                long countExito = 0;
                if (count > 0)
                {
                    // Recorremos cada comunicación
                    List<AvisoCorreos> avisos = new List<AvisoCorreos>();
                    RecordReader recordReaderComunicaciones = recordProvider.GetRecordsReader();
                    while (recordReaderComunicaciones.Read())
                    {
                        Record recordComunicacion = (Record)recordReaderComunicaciones.Current;

                        // Obtenemos el registro padre de la ficha Expedientes
                        RelationRecordProvider relationRecordProvider = new RelationRecordProvider(ufs, relExpComunicaciones, recordComunicacion);
                        relationRecordProvider.Type = RelationRecordProviderType.ParentRelatedOnes;
                        List<RelationRecord> relationRecords = relationRecordProvider.GetRelationRecords();
                        if (relationRecords.Count == 0)
                        {
                            // La comunicacion no está asociada a un padre así que pasamos de ella...
                            countExito++;
                            System.Diagnostics.Trace.TraceInformation("No hay comunicaciones padres y se continua con la siguiente iteracion");
                            continue;
                        }

                        string codExpediente = Escape(relationRecords[0].ParentPart["CODEXPEDIENTE"].ValueFormatString);
                        string referenciaExp = Escape(relationRecords[0].ParentPart["REFERENCIAEXP"].ValueFormatString);
                        string codComunicacion = Escape(recordComunicacion["CODCOMUNICACION"].ValueFormatString);

                        try
                        {

                            System.Diagnostics.Trace.TraceInformation("Se va a generar el apremio");
                            GeneracionApremio generacionApremio = new GeneracionApremio(ufs);
                            System.Diagnostics.Trace.TraceInformation("Se va ha generado el apremio");

                            using (DbTransactionScope transaction = ufs.Database.ComienzaTransaccion())
                            {
                                try
                                {
                                    string error = "";
                                    System.Diagnostics.Trace.TraceInformation("Se va a obtener la comunicacion para el apremio");
                                    RelationRecord relationComunicacion = generacionApremio.ObtenerComunicacionParaApremio(relationRecords[0].ParentPart, out error);
                                    if (!string.IsNullOrEmpty(error))
                                    {
                                        avisos.Add(new AvisoCorreos(referenciaExp, codComunicacion, string.Format("Error al obtener comunicación para apremio: {0}", error)));
                                        System.Diagnostics.Trace.TraceError("Error al obtener comunicación para apremio {0}", error);
                                        transaction.Dispose();
                                    }
                                    else
                                    {
                                        // Actualizamos el campo FECHAMASIVOJUST de la comunicación que se ha cerrado
                                        RecordEdition recordEdition = new RecordEdition(ufs);
                                        RecordValuesList values = new RecordValuesList();
                                        values.Add(fileComunicaciones.Fields["FECHAMASIVOJUST"].CreateFieldValue(DateTime.Now.Date));
                                        recordEdition.EditRecord(relationComunicacion.ChildPart, values.GetDifferences(relationComunicacion.ChildPart.Values), new ActionInfo("AltaPagos", "ApremioMasivo", ""));

                                        System.Diagnostics.Trace.TraceInformation("Se va a generar el 069");
                                        generacionApremio.Generar069Apremio(relationComunicacion);
                                        countExito++;
                                        transaction.Complete();
                                    }

                                }
                                catch (Exception exc)
                                {
                                    transaction.Dispose();
                                    System.Diagnostics.Trace.TraceError("Se ha generado una excepción al generar apremio: " + exc);
                                    System.Diagnostics.Trace.TraceError("Se reeleva la excepción");
                                    throw exc;
                                }
                            }
                        } catch (Exception exc) {
                            avisos.Add(new AvisoCorreos(referenciaExp, codComunicacion, string.Format("Error al generar apremio: {0}", exc.Message)));
                            System.Diagnostics.Trace.TraceError("Excepción al generar apremio: " + exc);                           
                        }
                    }

                    mainMultiView.SetActiveView(viewSuccess);
                    literalNumRegistros.Text = string.Format("Se han generado {0} de {1} de apremios con éxito.", countExito, count);

                    lblIncidencias.Visible = countExito != count;
                    panelWarnings.Visible = avisos.Count > 0;
                    if (avisos.Count > 0)
                    {
                        foreach (AvisoCorreos aviso in avisos)
                        {
                            TableCell cellExpediente = new TableCell();
                            cellExpediente.Text = aviso.RefExpediente;
                            TableCell cellComunicacion = new TableCell();
                            cellComunicacion.Text = aviso.CodComunicacion;
                            TableCell cellIncidencia = new TableCell();
                            cellIncidencia.Text = aviso.Incidencia;
                            TableRow newRow = new TableRow();
                            newRow.Cells.AddRange(new TableCell[] { cellExpediente, cellComunicacion, cellIncidencia });
                            tableIncidencias.Rows.Add(newRow);
                        }
                    }
                }
                else
                {
                    MostrarError("No hay ningún registro en la selección actual.");
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Excepción: " + exc);
                MostrarError("Error inesperado en la generación de apremios. " + exc.Message);
            }
        }

        

        //private static string GetValorCampo(Record record, string[] campos)
        //{
        //    foreach (string campo in campos)
        //    {
        //        if (record.File.Fields.IndexOf(campo) >= 0)
        //        {
        //            return record[campo].ValueFormatString;
        //        }
        //    }
        //    return "";
        //}

       
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