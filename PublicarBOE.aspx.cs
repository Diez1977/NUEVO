using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Web.UI.WebControls;
using AltaPagos.Code;
using PixelwareApi.File;
using PixelwareApi.File.Records;
using PixelwareApi.File.Relations;
using PixelwareApi.File.UserActions;
using PixelwareApi.File.Records.Searching;

namespace AltaPagos
{
    public partial class PublicarBOE : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Index = Request["index"];
                Filter = Request["filter"];
                Sort = Request["sort"];
                LoadFilter();
                mainMultiView.SetActiveView(viewContinuar);
            }
        }

        protected void linkContinuar_Click(object sender, EventArgs e)
        {
            DateTime fechaPublicacion = DateTime.MinValue;
            try
            {
                //Comprobamos que las fechas sean correctas
                if (DateTime.TryParseExact(textBoxFechaPublicacion.Text, "dd/MM/yyyy", CultureInfo.CurrentCulture.DateTimeFormat, System.Globalization.DateTimeStyles.None, out fechaPublicacion))
                {
                    // Todo correcto, ponemos el view de espera y lanzamos el post para publicar en BOE
                    mainMultiView.SetActiveView(viewEspera);
                    ClientScript.RegisterStartupScript(this.GetType(), "generar", ClientScript.GetPostBackEventReference(linkActualizar, ""), true);
                }
                else
                {
                    if (fechaPublicacion.Date == DateTime.MinValue)
                        MostrarError("Fecha de publicación incorrecta.");
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Excepción: " + ex.ToString());
                MostrarError("Error inesperado en al cargar información sobre las comunicaciones. " + ex.Message);
            }
        }

        protected void linkActualizar_Click(object sender, EventArgs e)
        {
            DoWork();
        }

        protected void linkVolver_Click(object sender, EventArgs e)
        {
            mainMultiView.SetActiveView(viewContinuar);
        }

        private bool LoadFilter()
        {
            UserFileSystem ufs = ConexiónAlmacen.ObtenerUserFileSystem();

            FileView fileComunicaciones = ConexiónAlmacen.ObtenerFileViewComunicaciones();
            Relation relExpComunicaciones = ConexiónAlmacen.ObtenerRelationExpComunicaciones();

            RecordFilterStringSerializer serializer = new RecordFilterStringSerializer(ufs);
            RecordFilter recordFilter = serializer.DeserializeFromString(Filter, fileComunicaciones);

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
                dataTable.Columns.Add("FECHAENVIOBOE", typeof(string));
                

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
                    string importeTotal = recordComunicacion["IMPORTETOTAL"].IsNull() ? "" : Escape(string.Format("{0:C}", recordComunicacion["IMPORTETOTAL"].Value));
                    string fechaEnvioBOE = recordComunicacion["FECHAENVIOBOE"].IsNull() ? "" : Escape(string.Format("{0:dd/MM/yyyy}", recordComunicacion["FECHAENVIOBOE"].Value));

                    dataTable.Rows.Add(new object[] { recordComunicacion.Number, fechaAltaExp, referenciaExp, fechaImpresion, estadoComunic, 
                                numJustifInteco, estadoInteco, importeTotal, fechaEnvioBOE});
                }
                gridViewComunicaciones.DataSource = dataTable;
                gridViewComunicaciones.DataBind();
                mainMultiView.SetActiveView(viewContinuar);
                labelContinuar.Text = string.Format(labelContinuar.Text, dataTable.Rows.Count);
                return true;
            }
            else
            {
                MostrarError("No hay ningún registro en la selección actual.");
                return false;
            }
        }

        private void DoWork()
        {
            try
            {
                UserFileSystem ufs = ConexiónAlmacen.ObtenerUserFileSystem();

                FileView fileComunicaciones = ConexiónAlmacen.ObtenerFileViewComunicaciones();
                FileView fileExpedientes = ConexiónAlmacen.ObtenerFileViewExpedientes();
                Relation relExpComunicaciones = ConexiónAlmacen.ObtenerRelationExpComunicaciones();

                RecordFilterStringSerializer serializer = new RecordFilterStringSerializer(ufs);
                RecordFilter recordFilter = serializer.DeserializeFromString(Filter, fileComunicaciones);

                // Consultamos todos los registros de Comunicaciones
                RecordProvider recordProvider = new RecordProvider(ufs, fileComunicaciones);
                recordProvider.Filter = recordFilter;
                recordProvider.AddSortExpression(Sort);

                
                long count = recordProvider.GetTotalCount();
                if(count > 0)
                {
                    int total = 0;
                    // Recorremos cada comunicación
                    List<AvisoCorreos> avisos = new List<AvisoCorreos>();
                    RecordReader recordReaderComunicaciones = recordProvider.GetRecordsReader();
                    while(recordReaderComunicaciones.Read())
                    {
                        Record recordComunicacion = (Record)recordReaderComunicaciones.Current;
                        

                        // Obtenemos el registro padre de la ficha Expedientes
                        RelationRecordProvider relationRecordProvider = new RelationRecordProvider(ufs, relExpComunicaciones, recordComunicacion);
                        relationRecordProvider.Type = RelationRecordProviderType.ParentRelatedOnes;
                        List<RelationRecord> relationRecords = relationRecordProvider.GetRelationRecords();
                        if(relationRecords.Count == 0)
                        {
                            // La comunicacion no está asociada a un padre así que pasamos de ella...
                            continue;
                        }

                        string fechaAltaExp = null, referenciaExp = null, fechaImpresion = null, estadoComunic = null, 
                            numJustifInteco = null, estadoInteco = null, importeTotal = null, fechaEnvioBOE = null,
                            fechaPublicacionBOE = null, codigoEnvio = null;

                        try
                        {
                            //Editamos los campos de las comunicaciones
                            RecordEdition recordEdition = new RecordEdition(ufs);
                            RecordValuesList editValues = new RecordValuesList();

                            DateTime fechaPublicacion = DateTime.ParseExact(textBoxFechaPublicacion.Text, "dd/MM/yyyy",
                                CultureInfo.CurrentCulture.DateTimeFormat);

                            editValues.Add(fileComunicaciones.Fields["FECHAPUBLICBOE"].CreateFieldValue(fechaPublicacion));
                            
                            DateTime fechaAceptacion = fechaPublicacion.AddDays(15);
                            editValues.Add(fileComunicaciones.Fields["FECHAACEPTCERTIF"].CreateFieldValue(fechaAceptacion));

                            if(recordComunicacion.Values["APREMIO"].ValueFormatString == "Si") 
                                editValues.Add(fileComunicaciones.Fields["ESTADOCOMUNIC"].CreateFieldValue("Contraer"));
                            else if(recordComunicacion.Values["APREMIO"].ValueFormatString == "No")
                                editValues.Add(fileComunicaciones.Fields["ESTADOCOMUNIC"].CreateFieldValue("Alegando"));

                            recordEdition.EditRecord(recordComunicacion, editValues.GetDifferences(recordComunicacion.Values), new ActionInfo("PublicarBOE", "ModificarValores", ""));
                            System.Diagnostics.Trace.TraceInformation("Comunicacion actualizada");

                            // Actualizamos también el campo estado del expediente padre
                            editValues = new RecordValuesList();
                            if (recordComunicacion.Values["APREMIO"].ValueFormatString == "Si")
                                editValues.Add(fileExpedientes.Fields["ESTADO"].CreateFieldValue("Contraer"));
                            else if (recordComunicacion.Values["APREMIO"].ValueFormatString == "No")
                                editValues.Add(fileExpedientes.Fields["ESTADO"].CreateFieldValue("Alegando"));
                            editValues.Add(fileExpedientes.Fields["FECHAPUBLICBOE"].CreateFieldValue(fechaPublicacion));
                            editValues.Add(fileExpedientes.Fields["FECHAACEPTCERTIF"].CreateFieldValue(fechaAceptacion));

                            recordEdition.EditRecord(relationRecords[0].ParentPart, editValues.GetDifferences(relationRecords[0].ParentPart.Values), new ActionInfo("PublicarBOE", "ModificarValores", ""));
                            System.Diagnostics.Trace.TraceInformation("Esto del expediente actualizado");

                            //Obtenemos los resultados
                            fechaAltaExp = recordComunicacion["FECHAALTAEXP"].IsNull() ? "" : Escape(string.Format("{0:dd/MM/yyyy}", recordComunicacion["FECHAALTAEXP"].Value));
                            referenciaExp = Escape(recordComunicacion["REFERENCIAEXP"].ValueFormatString);
                            fechaImpresion = recordComunicacion["FECHACOMUNIC"].IsNull() ? "" : Escape(string.Format("{0:dd/MM/yyyy}", recordComunicacion["FECHACOMUNIC"].Value));
                            estadoComunic = Escape(recordComunicacion["ESTADOCOMUNIC"].ValueFormatString);
                            numJustifInteco = Escape(recordComunicacion["NUMJUSTIFINTECO"].ValueFormatString);
                            estadoInteco = Escape(recordComunicacion["ESTADOINTECO"].ValueFormatString);
                            importeTotal = recordComunicacion["IMPORTETOTAL"].IsNull() ? "" : Escape(string.Format("{0:C}", recordComunicacion["IMPORTETOTAL"].Value));
                            fechaEnvioBOE = recordComunicacion["FECHAENVIOBOE"].IsNull() ? "" : Escape(string.Format("{0:dd/MM/yyyy}", recordComunicacion["FECHAENVIOBOE"].Value));
                            fechaPublicacionBOE = recordComunicacion["FECHAPUBLICBOE"].IsNull() ? "" : Escape(string.Format("{0:dd/MM/yyyy}", recordComunicacion["FECHAPUBLICBOE"].Value));
                            codigoEnvio = recordComunicacion["CODENVIO"].IsNull() ? "" : Escape(recordComunicacion["CODENVIO"].ValueFormatString);

                            avisos.Add(new AvisoCorreos(fechaAltaExp, referenciaExp, fechaImpresion, estadoComunic, numJustifInteco,
                                estadoInteco, importeTotal, fechaEnvioBOE, fechaPublicacionBOE, codigoEnvio, string.Format("Creado correctamente")));
                            total++;
                        }
                        catch(Exception ex)
                        {
                            avisos.Add(new AvisoCorreos(fechaAltaExp, referenciaExp, fechaImpresion, estadoComunic, numJustifInteco,
                                estadoInteco, importeTotal, fechaEnvioBOE, fechaPublicacionBOE, codigoEnvio, string.Format("Error actualizando")));
                            System.Diagnostics.Trace.TraceError("Excepción al actualizar: " + ex.ToString()); 
                        }
                    }
                    
                    mainMultiView.SetActiveView(viewExito);
                    literalNumRegistros.Text = string.Format("Se han actualizado {0} de {1} comunicaciones con éxito.",total, count);
                    System.Diagnostics.Trace.TraceInformation("Operacion completa");

                    //Mostramos los resultados
                    panelWarnings.Visible = avisos.Count > 0;
                    if(avisos.Count > 0)
                    {
                        foreach(AvisoCorreos aviso in avisos)
                        {
                            TableCell cellFechaAltaExpediente = new TableCell();
                            cellFechaAltaExpediente.Text = aviso.FechaAltaExpediente;
                            TableCell cellReferenciaExp = new TableCell();
                            cellReferenciaExp.Text = aviso.RefExpediente;
                            TableCell cellFechaImpresion = new TableCell();
                            cellFechaImpresion.Text = aviso.FechaImpresion;
                            TableCell cellEstadoComunicacion = new TableCell();
                            cellEstadoComunicacion.Text = aviso.EstadoComunicacion;
                            TableCell cellNumJustifInteco = new TableCell();
                            cellNumJustifInteco.Text = aviso.NumJustificanteInteco;
                            TableCell cellEstadoInteco = new TableCell();
                            cellEstadoInteco.Text = aviso.EstadoInteco;
                            TableCell cellImporteTotal = new TableCell();
                            cellImporteTotal.Text = aviso.ImporteTotal;
                            TableCell cellEnvioBOE = new TableCell();
                            cellEnvioBOE.Text = aviso.FechaEnvioBOE;
                            TableCell cellPublicacionBOE = new TableCell();
                            cellPublicacionBOE.Text = aviso.FechaPublicacionBOE;
                            TableCell cellIncidencia = new TableCell();
                            cellIncidencia.Text = aviso.Incidencia;
                            
                            TableRow newRow = new TableRow();
                            newRow.Cells.AddRange(new TableCell[] { cellFechaAltaExpediente, cellReferenciaExp,
                                cellFechaImpresion, cellEstadoComunicacion, cellNumJustifInteco, cellEstadoInteco, cellImporteTotal, 
                                cellEnvioBOE, cellPublicacionBOE, cellIncidencia });
                            tableIncidencias.Rows.Add(newRow);
                        }
                    }
                }
                else
                {
                    MostrarError("No hay ningún registro en la selección actual.");
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Excepción: " + ex.ToString());
                MostrarError("Error inesperado en la actualización de comunicaciones. " + ex.Message);
            }

        }

        private void MostrarError(string error)
        {
            literalError.Text = error;
            mainMultiView.SetActiveView(viewError);
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