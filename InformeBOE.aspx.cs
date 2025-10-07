using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PixelwareApi.File;
using PixelwareApi.Data;
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
using System.Data;

namespace AltaPagos
{
    public partial class InformeBOE : System.Web.UI.Page
    {
     
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Index = Request["index"];
                Filter = HttpUtility.UrlDecode(Request["filter"]);
                Sort = Request["sort"];
                CargarConfirmacion();
            }
        }

        private void CargarConfirmacion()
        {
            try
            {
                UserFileSystem ufs = ConexiónAlmacen.ObtenerUserFileSystem();

                System.Diagnostics.Trace.TraceInformation("Cargamos record filter.");
                FileView fileComunicaciones = ConexiónAlmacen.ObtenerFileViewComunicaciones();
                RecordFilterStringSerializer serializer = new RecordFilterStringSerializer(ufs);
                RecordFilter recordFilter = serializer.DeserializeFromString(Filter, fileComunicaciones);
                System.Diagnostics.Trace.TraceInformation("RecordFilter: {0}", recordFilter.GetSQLFilter());
                Relation relExpComunicaciones = ConexiónAlmacen.ObtenerRelationExpComunicaciones();
                System.Diagnostics.Trace.TraceInformation("Relación de comunicaciones cargada: {0}", relExpComunicaciones.RelationId);

                // Consultamos todos los registros de Comunicaciones
                System.Diagnostics.Trace.TraceInformation("Consultamos las comunicaciones");
                RecordProvider recordProvider = new RecordProvider(ufs, fileComunicaciones);
                recordProvider.Filter = recordFilter;
                recordProvider.AddSortExpression(Sort);
                long count = recordProvider.GetTotalCount();
                System.Diagnostics.Trace.TraceInformation("Count: {0}", count);
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
                        System.Diagnostics.Trace.TraceInformation("Buscamos el registro padre de expediente");
                        RelationRecordProvider relationRecordProvider = new RelationRecordProvider(ufs, relExpComunicaciones, recordComunicacion);
                        relationRecordProvider.Type = RelationRecordProviderType.ParentRelatedOnes;
                        long countAux = relationRecordProvider.GetTotalCount();
                        if (countAux == 0)
                        {
                            // La comunicacion no está asociada a un padre así que pasamos de ella...
                            System.Diagnostics.Trace.TraceInformation("No hay padre");
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
                    System.Diagnostics.Trace.TraceInformation("Binding datasource");
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
            GenerarFichero();
        }

        private void GenerarFichero()
        {
            
            try
            {
                UserFileSystem ufs = ConexiónAlmacen.ObtenerUserFileSystem();

                // Comienza la transacción
                using (DbTransactionScope transaction = ufs.Database.ComienzaTransaccion())
                {

                    // Primero cargamos todos los tipoingresos para más adelante determinar si se usarán campos de INTERESADO o de SOLICITANTE
                    FileView fileTipoIngreso = ConexiónAlmacen.ObtenerFileViewTipoIngreso();
                    List<string> fieldsTipoIngreso = new List<string>(new string[] { "CODTIPOINGRESO", "DESTINATARIO", "GRUPO" });
                    Dictionary<string, RecordValuesList> dictTipoIngresoSolicitante = new Dictionary<string, RecordValuesList>();
                    PartialDataRecordProvider tipoIngresoProvider = new PartialDataRecordProvider(ufs, fileTipoIngreso, delegate(Field aux) { return fieldsTipoIngreso.Contains(aux.Name); });
                    RecordReader tipoIngresosReader = tipoIngresoProvider.GetRecordsReader();
                    while (tipoIngresosReader.Read())
                    {
                        Record recordTipoIngreso = (Record)tipoIngresosReader.Current;
                        string codTipoIngreso = recordTipoIngreso["CODTIPOINGRESO"].ValueFormatString;
                        if (!dictTipoIngresoSolicitante.ContainsKey(codTipoIngreso))
                        {
                            dictTipoIngresoSolicitante.Add(codTipoIngreso, recordTipoIngreso.Values);
                        }
                    }

                    FileView fileComunicaciones = ConexiónAlmacen.ObtenerFileViewComunicaciones();
                    RecordFilterStringSerializer serializer = new RecordFilterStringSerializer(ufs);
                    RecordFilter recordFilter = serializer.DeserializeFromString(Filter, fileComunicaciones);
                    List<string> fieldsExpediente = new List<string>(new string[] { "CODEXPEDIENTE", "CODTIPOINGRESO", 
                        "REFERENCIAEXP", 
                        "NUMDOCIDENTIFINTER", "NOMBREINTERAUX", "APELLIDO1INTERAUX", "APELLIDO2INTERAUX", 
                        "NUMDOCIDENTIFSOLIC", "NOMBRESOLICAUX", "APELLIDO1SOLICAUX", "APELLIDO2SOLICAUX"
                    });
                    List<string> fieldsComunicacion = new List<string>(new string[] { "CODCOMUNICACION", "APREMIOPROPAG", "APREMIO" });

                    Relation relExpComunicaciones = ConexiónAlmacen.ObtenerRelationExpComunicaciones();

                    // Consultamos todos los registros de Comunicaciones, ponemos un fieldfilter vacío porque en realidad no vamos a leer campos
                    // de esta ficha sino de la ficha padre
                    PartialDataRecordProvider recordProvider = new PartialDataRecordProvider(ufs, fileComunicaciones, delegate(Field aux) { return fieldsComunicacion.Contains(aux.Name); });
                    recordProvider.Filter = recordFilter;
                    recordProvider.AddSortExpression(Sort);
                    long count = recordProvider.GetTotalCount();
                    if (count > 0)
                    {
                        // Inicializamos el fichero Excel
                        TempFile = System.IO.Path.GetTempFileName();

                        using (FileStream templateFile = new FileStream(Server.MapPath(@"~/Templates/InformeBOE.xlt"), FileMode.Open, FileAccess.Read))
                        {
                            HSSFWorkbook workbook = new HSSFWorkbook(templateFile);

                            //create a entry of DocumentSummaryInformation
                            DocumentSummaryInformation dsi = PropertySetFactory.CreateDocumentSummaryInformation();
                            dsi.Company = "Pixelware";
                            workbook.DocumentSummaryInformation = dsi;

                            //create a entry of SummaryInformation
                            SummaryInformation si = PropertySetFactory.CreateSummaryInformation();
                            si.Subject = "Informe BOE";
                            workbook.SummaryInformation = si;

                            ISheet sheet1 = workbook.GetSheet("Informe");

                            // Recorremos cada comunicación
                            int numLinea = 1;
                            List<Record> recordsComunicaciones = recordProvider.GetRecords();
                            foreach (Record recordComunicacion in recordsComunicaciones)
                            {
                                // Obtenemos el registro padre de la ficha Expedientes
                                Record recordExpediente = null;
                                RelationRecordProvider relationRecordProvider = new RelationRecordProvider(ufs, relExpComunicaciones, recordComunicacion);
                                relationRecordProvider.Type = RelationRecordProviderType.ParentRelatedOnes;
                                List<RelationRecord> relationRecords = relationRecordProvider.GetRecordsWithPartialFieldValues(delegate(Field aux)
                                {
                                    return fieldsExpediente.Contains(aux.Name);
                                });
                                if (relationRecords.Count == 0)
                                {
                                    // La comunicacion no está asociada a un padre así que pasamos de ella...
                                    continue;
                                }

                                // Sacamos los datos del expediente
                                recordExpediente = relationRecords[0].ParentPart;

                                string codExpediente = Escape(recordExpediente["CODEXPEDIENTE"].ValueFormatString);
                                string referenciaExp = Escape(recordExpediente["REFERENCIAEXP"].ValueFormatString);
                                string codComunicacion = Escape(recordComunicacion["CODCOMUNICACION"].ValueFormatString);
                                string codTipoIngreso = recordExpediente["CODTIPOINGRESO"].ValueFormatString;

                                string nombre, apellido1, apellido2, numidentif;
                                bool solicitante = false;
                                if (dictTipoIngresoSolicitante.ContainsKey(codTipoIngreso))
                                {
                                    solicitante = "Solicitante".Equals(dictTipoIngresoSolicitante[codTipoIngreso]["DESTINATARIO"].ValueFormatString);
                                }
                                if (solicitante)
                                {
                                    numidentif = Escape(recordExpediente["NUMDOCIDENTIFSOLIC"].ValueFormatString);
                                    nombre = Escape(recordExpediente["NOMBRESOLICAUX"].ValueFormatString);
                                    apellido1 = Escape(recordExpediente["APELLIDO1SOLICAUX"].ValueFormatString);
                                    apellido2 = Escape(recordExpediente["APELLIDO2SOLICAUX"].ValueFormatString);
                                }
                                else
                                {
                                    numidentif = Escape(recordExpediente["NUMDOCIDENTIFINTER"].ValueFormatString);
                                    nombre = Escape(recordExpediente["NOMBREINTERAUX"].ValueFormatString);
                                    apellido1 = Escape(recordExpediente["APELLIDO1INTERAUX"].ValueFormatString);
                                    apellido2 = Escape(recordExpediente["APELLIDO2INTERAUX"].ValueFormatString);
                                }
                                IRow newrow = sheet1.CreateRow(numLinea);
                                newrow.CreateCell(0, CellType.STRING).SetCellValue(numidentif);

                                string nombreTotal = "";
                                nombreTotal = ConcatenateSpace(nombreTotal, nombre);
                                nombreTotal = ConcatenateSpace(nombreTotal, apellido1);
                                nombreTotal = ConcatenateSpace(nombreTotal, apellido2);
                                newrow.CreateCell(1, CellType.STRING).SetCellValue(nombreTotal);

                                string apremio = Escape(recordComunicacion["APREMIO"].ValueFormatString);
                                string procedimiento = "";
                                if (apremio.ToUpper().Equals("NO"))
                                {
                                    procedimiento = "NOTIFICACIÓN PROPUESTA DE LIQUIDACIÓN {0}";
                                }
                                else
                                {
                                    procedimiento = "NOTIFICACIÓN DE LIQUIDACIÓN {0}";
                                }
                                string grupo = "";
                                if (dictTipoIngresoSolicitante.ContainsKey(codTipoIngreso))
                                {
                                    grupo = dictTipoIngresoSolicitante[codTipoIngreso]["GRUPO"].ValueFormatString;
                                }
                                procedimiento = string.Format(procedimiento, grupo);
                                newrow.CreateCell(2, CellType.STRING).SetCellValue(procedimiento);

                                newrow.CreateCell(3, CellType.STRING).SetCellValue(referenciaExp);

                                newrow.CreateCell(4, CellType.STRING).SetCellValue("S.G. Política y Ayudas a la Vivienda");
                                newrow.CreateCell(5, CellType.STRING).SetCellValue("Secretaría General de Vivienda");

                                numLinea++;

                                // Editamos la comunicación
                                try
                                {
                                    Record recordComunicacion2 = Record.LoadRecord(ufs, fileComunicaciones, recordComunicacion.Number);
                                    RecordEdition recordEditionIngresos = new RecordEdition(ufs);
                                    RecordValuesList camposEditarComunicacion = new RecordValuesList();
                                    camposEditarComunicacion.Add(fileComunicaciones.Fields["ESTADOCOMUNIC"].CreateFieldValue("NotifBOE"));
                                    camposEditarComunicacion.Add(fileComunicaciones.Fields["FECHAENVIOBOE"].CreateFieldValue(DateTime.Now.Date));
                                    camposEditarComunicacion.Add(fileComunicaciones.Fields["FECHAMASIVOBOE"].CreateFieldValue(DateTime.Now.Date));
                                    recordEditionIngresos.EditRecord(recordComunicacion2, camposEditarComunicacion.GetDifferences(recordComunicacion2.Values),
                                        new ActionInfo("DevIngresos", "Envío de comunicación a BOE", "Envío de comunicación a BOE"));

                                    System.Diagnostics.Trace.TraceError("Editando expediente pwsnumero: {0}", recordExpediente.Number);
                                    Record recordExpediente2 = Record.LoadRecord(ufs, recordExpediente.File, recordExpediente.Number);
                                    RecordValuesList camposEditarExpediente = new RecordValuesList();
                                    camposEditarExpediente.Add(recordExpediente.File.Fields["ESTADO"].CreateFieldValue("NotifBOE"));
                                    camposEditarExpediente.Add(recordExpediente.File.Fields["FECHAENVIOBOE"].CreateFieldValue(DateTime.Now.Date));
                                    recordEditionIngresos.EditRecord(recordExpediente2, camposEditarExpediente.GetDifferences(recordExpediente2.Values),
                                        new ActionInfo("DevIngresos", "Envío de comunicación a registro", "Envío de comunicación a registro"));

                                }
                                catch (Exception exc)
                                {
                                    System.Diagnostics.Trace.TraceError("Error al editar la comunicación: " + exc.ToString());
                                }
                            }

                            // Guargamos el excel a temporal
                            using (FileStream outputFile = new FileStream(TempFile, FileMode.Open, FileAccess.ReadWrite))
                            {
                                workbook.Write(outputFile);
                                outputFile.Close();
                            }
                        }
                        FileInfo fileInfo = new FileInfo(TempFile);
                        long length = fileInfo.Length;

                        mainMultiView.SetActiveView(viewSuccess);
                        literalNumRegistros.Text = string.Format("Se han procesado e incluido en el fichero Excel {0} registros.", count);
                        linkDownload.Text = string.Format("Para descargar el fichero Excel generado ({0}), pulse aquí", ToKBString(length));
                    }
                    else
                    {
                        MostrarError("No hay ningún registro en la selección actual.");
                    }

                    //Si todo ha ido correctamente
                    transaction.Complete();
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Excepción: " + exc.ToString());
                MostrarError("Error inesperado en la generación del fichero. " + exc.Message);
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

       
        private static string ToKBString(long length)
        {
            if (length < 1024)
            {
                return string.Format("{0:n} Bytes", length);
            }
            else
            {
                return string.Format("{0:n0} KBytes", Math.Floor((double)(length / 1024)));
            }
        }

        protected void linkDownload_Click(object sender, EventArgs e)
        {
            Response.Clear();
            Response.ContentType = "application/vnd.ms-excel";
            Response.AddHeader("Content-Disposition",
                "attachment; filename=\"InformeBOE.xls\"");
            Response.TransmitFile(TempFile);
            Response.End();
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
        private string TempFile
        {
            get { return (string) ViewState["TempFile"]; }
            set { ViewState["TempFile"] = value; }
        }
    }
}