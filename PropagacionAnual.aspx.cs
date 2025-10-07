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
using Vivienda;
using System.Globalization;

namespace AltaPagos
{
    public partial class PropagacionAnual : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            HideIncidencias();

            if (!IsPostBack)
            {
                index = Request["index"];
                filter = Request["filter"];
                sort = Request["sort"];
                
                HideIncidencias();
                CargarConfirmacion();
            }
        }

        private void CargarConfirmacion()
        {
            try
            {
                // Cargamos los expedientes
                ExpedienteCollection expedientes = ExpedienteCollection.LoadExpededientes(filter, sort);
                if (expedientes.Count > 0)
                {
                    // Expedientes concretos que se van a procesar
                    gridViewExpedientes.DataSource = expedientes.Expedientes;
                    gridViewExpedientes.DataBind();

                    // Se rellena el resumen de los tipos de los expedientes
                    gridViewTipoExpedientes.DataSource = expedientes.Counters.ToList();
                    gridViewTipoExpedientes.DataBind();

                    // Se comprueban los pagos y se muestran los errores
                    CheckExpedientes(expedientes);

                    mainMultiView.SetActiveView(viewConfirm);
                    labelConfirm.Text = string.Format(labelConfirm.Text, expedientes.Count);
                }
                else
                {
                    MostrarError("No hay ningún registro en la selección actual.");
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Excepción: " + exc);
                MostrarError("Error inesperado al cargar información sobre los expedientes. " + exc.Message);
            }
        }

        private void HideIncidencias()
        {
            WarningDiv.Visible = false;
        }

        private void CheckExpedientes(ExpedienteCollection expedientes)
        {
            IncidenciasDiv.Visible = false;
                 
            var badExpedientes = new List<Expediente>();
            bool result = expedientes.CheckPagosExpediente(badExpedientes, true);
            if (badExpedientes.Count > 0)
            {
                WarningDiv.Visible = true;
                WarningText.Text = @"Se ha detectado que los siguientes expedientes contienen varios pagos que son de años diferentes.";

                gridViewErrorsExpediente.DataSource = badExpedientes;
                gridViewErrorsExpediente.DataBind();
                IncidenciasDiv.Visible = true;

                if (!result && badExpedientes.Count > 1)
                {
                    WarningText.Text += @"<br>Se ha detectado que existen varios pagos que no pertenecen al mismo año entre diferentes expedientes.";
                }
            }
            else if (!result)
            {
                // Como badExpedientes.Count == 0 los pagos dentro de un mismo expediente están bien, sin embargo, si comparamos entre
                // diferentes expedientes son de diferentes años.
                // Ejemplo: EXPEDIENTE1: PAGO1 (01/03/2003) PAGO1 (01/04/2003) PAGO3 (01/05/2003)
                //          EXPEDIENTE2: PAGO1 (01/10/2010) PAGO1 (01/11/2010) 

                // Si el anterior era cierto entonces este error no se muestra.
                WarningDiv.Visible = true;
                WarningText.Text = @"Se han detectado que existen varios pagos que no pertenecen al mismo año entre diferentes expedientes.";
            }
        }

        protected void linkContinuar_Click(object sender, EventArgs e)
        {
            // Se cachean los valores del formulario
            try
            {
                Motivo = tbPropagarMotivo.Text;
                saveFechaLiquidacion();

                // Se continua procesando la petición
                mainMultiView.SetActiveView(viewWait);
                ClientScript.RegisterStartupScript(this.GetType(), "generar", ClientScript.GetPostBackEventReference(linkGenerar, ""), true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Excepción: " + ex);
                MostrarError(string.Format("Error: {0}", ex.Message));
            }
        }

        private void saveFechaLiquidacion()
        {
            // Lo hacemos así porque guardamos un datetime y en el textbox hay un string. Antes de guardarlo lo comprobamos
            // Usamos Request porque al acceder por el textbox de ASPX, la propiedad Text está vacía.
            DateTime fecha;
            if (DateTime.TryParseExact(Request["tbFechaLiquidacion"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            {
                FechaLiquidacion = fecha;
            }
            else
            {
                throw new Exception("La fecha introducida es incorrecta.");
            }
        }


        protected void linkGenerar_Click(object sender, EventArgs e)
        {
            DoWork();
        }


        private void DoWork()
        {
            string refExpViejo = null;
            List<AvisoCorreos> avisos = new List<AvisoCorreos>();

            try
            {
                RecordFilterStringSerializer serializer = new RecordFilterStringSerializer(userFileSystem);
                RecordFilter recordFilter = serializer.DeserializeFromString(filter, fileExpedientes);

                // Consultamos todos los registros de Expedientes
                RecordProvider recordProviderExp = new RecordProvider(userFileSystem, fileExpedientes);
                recordProviderExp.Filter = recordFilter;
                long count = recordProviderExp.GetTotalCount();
                if (count > 0)
                {
                    int total = 0;
                    bool hasErrors = false; // Flag global que indica si hay errores en algún expediente

                    // Recorremos cada expediente
                    RecordReader recordReaderExpedientes = recordProviderExp.GetRecordsReader();
                    
                    while (recordReaderExpedientes.Read())
                    {
                        Record oldRecordExpediente = (Record)recordReaderExpedientes.Current;
                        refExpViejo = Escape(oldRecordExpediente.Values["REFERENCIAEXP"].ValueFormatString);

                        using (DbTransactionScope transaction = userFileSystem.Database.ComienzaTransaccion())
                        {
                            string refExpedNuevo;
                            Record newRecordExpediente = CreateExpediente(oldRecordExpediente, out refExpedNuevo);
                            CreatePagos(oldRecordExpediente, newRecordExpediente);

                            bool estado = false;

                            //Generacion de la Liquidacion
                            RelationRecord relRecordExp = null;
                            try
                            {
                                GeneracionImportes generacionImportes = new GeneracionImportes(userFileSystem);
                                InfoLiquidacion info = generacionImportes.GenerarImportesExpediente(newRecordExpediente);
                                relRecordExp = info.RecordComunicacion;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.TraceError(string.Format("Error al crear la liquidación: {0}", ex));
                                MostrarError("Error creando Liquidacion: {0} " + ex.Message);
                                avisos.Add(new AvisoCorreos(refExpViejo, refExpedNuevo, string.Format("Error creando Liquidacion")));
                                estado = true;
                                System.Diagnostics.Trace.TraceError(string.Format("Se ejecuta el dispose de la transacción"));
                                transaction.Dispose();
                            }

                            //Generacion del Oficio
                            if (!estado)
                            {
                                try
                                {
                                    GeneracionOficio generacionOficio = new GeneracionOficio(userFileSystem);
                                    generacionOficio.Generar(relRecordExp);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Trace.TraceError(string.Format("Error al crear el oficio: {0}", ex));
                                    MostrarError("Error creando Oficio: {0} " + ex.Message);
                                    avisos.Add(new AvisoCorreos(refExpViejo, refExpedNuevo, string.Format("Error creando Oficio")));
                                    estado = true;
                                    System.Diagnostics.Trace.TraceError(string.Format("Se ejecuta el dispose de la transacción"));
                                    transaction.Dispose();
                                }
                            }

                            /*
                            //Generacion del 069
                            if (!estado)
                            {
                                try
                                {
                                    GeneracionJustificante generacionJustificante = new GeneracionJustificante(userFileSystem);
                                    generacionJustificante.GenerarDescargarJustificante(newRecordExpediente, relRecordExp.ChildPart, true);
                                }
                                catch (Exception ex)
                                {
                                    MostrarError("Error creando Justificante: {0} " + ex.Message);
                                    avisos.Add(new AvisoCorreos(refExpViejo, refExpedNuevo, string.Format("Error creando Justificante")));
                                    estado = true;
                                    transaction.Dispose();
                                }
                            }
                            */

                            //Si no hay ningun error se marca como correcta la propagacion
                            if (!estado)
                            {
                                total++;
                                AvisoCorreos aviso = new AvisoCorreos(refExpViejo, refExpedNuevo, string.Format("Creado correctamente"));
                                aviso.RefExpNuevo = refExpedNuevo;
                                avisos.Add(aviso);
                                transaction.Complete();
                            }

                            // Si estado=true => existen errores
                            hasErrors |= estado;
                        }
                    }

                    mainMultiView.SetActiveView(viewSuccess);
                    if (count > 1)
                    {
                        literalNumRegistros.Text = string.Format("Se han propagado {0} de {1} expedientes con éxito.\n", total, count);
                    }
                    CreateResults(avisos, hasErrors);
                }
                else
                {
                    MostrarError("No hay ningún registro en la selección actual.");
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Excepción: " + exc.ToString());
                MostrarError("Error inesperado en la propagacion de expedientes. " + exc.Message);
            }
        }

        private void CreateResults(List<AvisoCorreos> avisos, bool hasErrors)
        {
            DivWarningResult.Visible = hasErrors;
            panelWarnings.Visible = avisos.Count > 0;
            if (avisos.Count > 0)
            {
                foreach (AvisoCorreos aviso in avisos)
                {
                    TableCell cellExpViejo = new TableCell();
                    cellExpViejo.Text = aviso.RefExpediente;
                    TableCell cellExpNuevo = new TableCell();
                    cellExpNuevo.Text = aviso.CodComunicacion;
                    TableCell cellIncidencia = new TableCell();
                    cellIncidencia.Text = aviso.Incidencia;
                    TableRow newRow = new TableRow();
                    newRow.Cells.AddRange(new TableCell[] { cellExpViejo, cellExpNuevo, cellIncidencia });
                    tableIncidencias.Rows.Add(newRow);
                }
            }
        }

        private int GetNumPagos(Record oldRecordExpediente)
        {
            RelationRecordProvider relacionProvider = new RelationRecordProvider(userFileSystem, relationExpendientePagos, oldRecordExpediente);
            int numPagos = 0;
            using (RelationRecordReader relacionReader = relacionProvider.GetRelationRecordsReader())
            {
                relacionReader.Open();
                while (relacionReader.Read())
                {
                    numPagos++;
                }
            }
            return numPagos;
        }

        private void CreatePagos(Record recordExpediente, Record newRecordExp)
        {
            //Buscamos las relaciones entre el Expediente y sus Pagos
            RelationRecordProvider relacionProvider = new RelationRecordProvider(userFileSystem, relationExpendientePagos, recordExpediente);

            using (RelationRecordReader relacionReader = relacionProvider.GetRelationRecordsReader())
            {
                RecordEdition recordEditionPagos = new RecordEdition(userFileSystem);
                RecordValuesList newValuesPagos = recordEditionPagos.PrepareNewRecordData(filePagos);

                // Copiamos los pagos correspondientes al Expediente actual y los asignamos al nuevo Expediente. 
                // Añadimos 1 año a cada pago
                relacionReader.Open();
                while (relacionReader.Read())
                {
                    // Se crea una copia del pago
                    RelationRecord relacionPagosRecord = (RelationRecord)relacionReader.Current;
                    Record recordPago = relacionPagosRecord.ChildPart;
                    foreach (Field field in filePagos.Fields)
                    {
                        newValuesPagos[field.Name].Value = recordPago.Values[field.Name].Value;
                    }

                    // Sobreescribimos los valores
                    DateTime nuevaFecha = recordPago["FECHAPAGO"].IsNull() ? DateTime.MinValue : Convert.ToDateTime(recordPago["FECHAPAGO"].Value);
                    if (recordPago["FECHAPAGO"].IsNull())
                    {
                        newValuesPagos["FECHAPAGO"].Value = null;
                    }
                    else
                    {
                        newValuesPagos["FECHAPAGO"].Value = nuevaFecha.AddYears(1);
                    }

                    newValuesPagos["CODCOMUNIC"].Value = "";
                    newValuesPagos["CODEXPED"].Value = newRecordExp["codexpediente"].Value;

                    // Creamos el registro y lo asociamos al expediente creado
                    var actionInfo = new ActionInfo("AltaPagos", "GeneracionCopiaExpediente", "PruebaPagos");
                    Record newRecordPagos = recordEditionPagos.CreateRecord(filePagos, newValuesPagos, actionInfo);
                    System.Diagnostics.Trace.TraceInformation("Creacion de pago copia {0}", newRecordPagos.Number);
                    RelationRecord.CreateRelationRecord(userFileSystem, relationExpendientePagos, newRecordExp, newRecordPagos);
                }
            }
        }

        private Record CreateExpediente(Record recordExpediente, out string refExpedNuevo)
        {
            RecordEdition recordEditionExp = new RecordEdition(userFileSystem);
            RecordValuesList newValuesExp = recordEditionExp.PrepareNewRecordData(fileExpedientes);

            List<string> excepciones = new List<string>(new string[]{
                    "NUMJUSTIFINTECO",
                    "ESTADOINTECO",
                    "MENSAJEERRORINTECO",
                    "ENVIADO",
                    "FECHAREGSALIDA",
                    "NUMREGSALIDA",
                    "IMPORTETOTAL",
                    "FECHAACEPTCERTIF",
                    "FECHAVENCIMIENTO",
                    "FECHAENVIOBOE",
                    "FECHAPUBLICBOE",
                    "DOMICILIOFISCAL",
                    "HAYALEGACIONES",
                    "ALEGACIONES",
                    "SUSPENDIDO"
                });

            // Copiamos todos los valores
            foreach (Field field in fileExpedientes.Fields)
            {
                if (!excepciones.Contains(field.Name))
                {
                    newValuesExp[field.Name].Value = recordExpediente.Values[field.Name].Value;
                }
            }

            // Campos que recogemos del formulario
            newValuesExp["FECHALIQUID"].Value = FechaLiquidacion;
            newValuesExp["MOTIVOEXPEDICION"].Value = Motivo;
            newValuesExp["ESTADO"].Value = "PteImporte";

            // Vaciamos los que no nos interesan
            newValuesExp["REFERENCIAEXP"].Value = null; // Se sobreescribe con un control de acciones
            
            DateTime fechaActual = DateTime.Now; // lo guardamos antes por posible condición de carrera (improbable pero en este proyecto pasa de todo...)
            newValuesExp["FECHAALTA"].Value = new DateTime(fechaActual.Year, fechaActual.Month, fechaActual.Day);


            Record newRecordExp = recordEditionExp.CreateRecord(fileExpedientes, newValuesExp, new ActionInfo("AltaPagos", "GeneracionCopiaExpediente", "PruebaExpedientes"));
            System.Diagnostics.Trace.TraceInformation("Creacion de expediente copia {0}", newRecordExp.Number);

            // Obtenemos la referencia (hay que repetir la consulta en base de datos porque este numero se crea
            // mediante un control de acciones)
            Expediente expediente = new Expediente(newRecordExp);
            refExpedNuevo = Escape(expediente.ReferenciaExp);
            
            return newRecordExp;
        }

        private DateTime FechaLiquidacion
        {
            get { return (DateTime)ViewState["FechaLiquidacion"]; }
            set { ViewState["FechaLiquidacion"] = value; }
        }

        private string Motivo
        {
            get { return (string)ViewState["ProgagarMotivo"]; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new Exception("El motivo está vacío");
                }

                ViewState["ProgagarMotivo"] = value;
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

        private string filter
        {
            get { return (string)ViewState["filter"]; }
            set { ViewState["filter"] = value; }
        }
        private string sort
        {
            get { return (string)ViewState["sort"]; }
            set { ViewState["sort"] = value; }
        }
        private string index
        {
            get { return (string)ViewState["index"]; }
            set { ViewState["index"] = value; }
        }

        private FileView fileExpedientes
        {
            get { return ConexiónAlmacen.ObtenerFileViewExpedientes(); }
        }

        private FileView filePagos
        {
            get { return ConexiónAlmacen.ObtenerFileViewPagos(); }
        }

        private UserFileSystem userFileSystem
        {
            get { return ConexiónAlmacen.ObtenerUserFileSystem(); }
        }

        private Relation relationExpendientePagos
        {
            get { return ConexiónAlmacen.ObtenerRelationExpPagos(); }
        }
    }
}