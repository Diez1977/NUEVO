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
using System.Data;
using AltaPagos.IntegrationServiceRegistroES;

namespace AltaPagos
{
    public partial class EnvioRegistroSalida : System.Web.UI.Page
    {
     
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Index = Request["index"];
                Filter = Request["filter"];
                Sort = Request["sort"];
                NumeroExpediente = Request["numeroExpediente"];
                NumeroComunicacion = Request["numeroComunicacion"];
                if (!string.IsNullOrEmpty(NumeroComunicacion) && !string.IsNullOrEmpty(NumeroExpediente))
                {
                    mainMultiView.SetActiveView(viewWait);
                    ClientScript.RegisterStartupScript(this.GetType(), "cargaDatos",
                        ClientScript.GetPostBackEventReference(linkGenerar, ""), true);
                }
                else
                {
                    CargarConfirmacion();
                }
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
            if (!string.IsNullOrEmpty(NumeroComunicacion) && !string.IsNullOrEmpty(NumeroExpediente))
            {
                EnviarARegistroUnaComunicacion();
            }
            else
            {
                EnviarARegistro();
            }
        }

        private void EnviarARegistro()
        {

            try
            {
                UserFileSystem ufs = ConexiónAlmacen.ObtenerUserFileSystem();
                // Primero cargamos todos los tipoingresos para más adelante determinar si se usarán campos de INTERESADO o de SOLICITANTE
                FileView fileTipoIngreso = ConexiónAlmacen.ObtenerFileViewTipoIngreso();
                List<string> fieldsTipoIngreso = new List<string>(new string[] { "CODTIPOINGRESO", "DESTINATARIO", "TIPOTRANSPORTE", "DESTIPOINGRESO", "RESPONSABLE" });
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
                List<string> fieldsExpediente = new List<string>(new string[] { "CODEXPEDIENTE", "REFERENCIAEXP", "CODTIPOINGRESO",
                    "NOMBREINTER", "NUMDOCIDENTIFINTER",
                    "TIPOVIAINTER", "NOMBREVIAINTER", "NUMVIAINTER", "ESCALINTER", "PISOINTER", "PUERTAINTER", 
                    "CODPOSINTER", "MUNICINTER", "PROVINTER", 
                    "NOMBRESOLIC", "NUMDOCIDENTIFSOLIC",
                    "TIPOVIASOLIC", "NOMBREVIASOLIC", "NUMVIASOLIC", "ESCALSOLIC", "PISOSOLIC", "PUERTASOLIC", 
                    "CODPOSSOLIC", "MUNICSOLIC", "PROVSOLIC"
                });

                List<string> fieldsComunicacion = new List<string>(new string[] { "CODCOMUNICACION", "CODEXPEDIENTE", "REFERENCIAEXP",
                    //"NOMBREINTERPROPAG", "NUMDOCIDENTIFINTERPROPAG",
                    //"TIPOVIAVIVPROPAG", "NOMBREVIAVIVPROPAG", "NUMVIAVIVPROPAG", " ESCALVIVPROPAG", "PISOVIVPROPAG", "PUERTAVIVPROPAG", 
                    //"CODPOSVIVPROPAG", "MUNICVIVPROPAG", "PROVVIVPROPAG", 
                    "GRUPOPROPAG", "TIPOINGRESOPROPAG" });

                Relation relExpComunicaciones = ConexiónAlmacen.ObtenerRelationExpComunicaciones();

                // Consultamos todos los registros de Comunicaciones, ponemos un fieldfilter vacío porque en realidad no vamos a leer campos
                // de esta ficha sino de la ficha padre
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
                    dataTable.Columns.Add("RESULTADO", typeof(string));

                    // Recorremos cada comunicación
                    int countExito = 0;
                    int countTotal = 0;
                    RecordReader recordReaderComunicaciones = recordProvider.GetRecordsReader();
                    while (recordReaderComunicaciones.Read())
                    {
                        List<string> avisos = new List<string>();
                        Record recordComunicacion = (Record)recordReaderComunicaciones.Current;

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


                        // Sacamos campos clave del expediente y la comunicación
                        recordExpediente = relationRecords[0].ParentPart;

                        bool envioCorrecto = EnviarARegistro(ufs, fileComunicaciones, recordComunicacion, recordExpediente, dictTipoIngresoSolicitante, out avisos);
                        if (envioCorrecto)
                        {
                            countExito++;
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
                        string resultado = envioCorrecto ? "Correcto": string.Join(".\n", avisos);
                        resultado = HttpUtility.HtmlEncode(resultado).Replace("\n", "<br/>");

                        dataTable.Rows.Add(new object[] { recordComunicacion.Number, fechaAltaExp, referenciaExp, fechaImpresion, estadoComunic,
                            numJustifInteco, estadoInteco, importeTotal, fechaRegSalida, resultado});
                        countTotal++;
                    }

                    mainMultiView.SetActiveView(viewSuccess);
                    literalNumRegistros.Text = string.Format("Se han enviado {0} comunicaciones con éxito de {1}.", countExito, countTotal);


                    gridViewResultado.DataSource = dataTable;
                    gridViewResultado.DataBind();
                }
                else
                {
                    MostrarError("No hay ningún registro en la selección actual.");
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Excepción: " + exc.ToString());
                MostrarError("Error inesperado en la generación del fichero. " + exc.Message);
            }
        }

        private void EnviarARegistroUnaComunicacion()
        {
            
            try
            {
                UserFileSystem ufs = ConexiónAlmacen.ObtenerUserFileSystem();
                // Primero cargamos todos los tipoingresos para más adelante determinar si se usarán campos de INTERESADO o de SOLICITANTE
                FileView fileTipoIngreso = ConexiónAlmacen.ObtenerFileViewTipoIngreso();
                List<string> fieldsTipoIngreso = new List<string>(new string[] { "CODTIPOINGRESO", "DESTINATARIO", "TIPOTRANSPORTE", "DESTIPOINGRESO", "RESPONSABLE" });
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
                Record recordComunicacion = Record.LoadRecord(ufs, fileComunicaciones, Convert.ToDecimal(NumeroComunicacion));
                
                // Obtenemos el registro padre de la ficha Expedientes
                Relation relExpComunicaciones = ConexiónAlmacen.ObtenerRelationExpComunicaciones();
                Record recordExpediente = Record.LoadRecord(ufs, relExpComunicaciones.ParentFile, Convert.ToDecimal(NumeroExpediente));

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
                dataTable.Columns.Add("RESULTADO", typeof(string));

                int countExito = 0;
                List<string> avisos = new List<string>();
                bool envioCorrecto = EnviarARegistro(ufs, fileComunicaciones, recordComunicacion, recordExpediente, dictTipoIngresoSolicitante, out avisos);
                if (envioCorrecto)
                {
                    countExito++;
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
                string resultado = envioCorrecto ? "Correcto" : string.Join(".\n", avisos);

                dataTable.Rows.Add(new object[] { recordComunicacion.Number, fechaAltaExp, referenciaExp, fechaImpresion, estadoComunic,
                            numJustifInteco, estadoInteco, importeTotal, fechaRegSalida, resultado});

                mainMultiView.SetActiveView(viewSuccess);
                literalNumRegistros.Text = string.Format("Se han enviado al Registro de Unidad de Salida {0} comunicaciones con éxito.", countExito);
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Excepción: " + exc.ToString());
                MostrarError("Error inesperado en la generación del fichero. " + exc.Message);
            }
        }

        private static bool EnviarARegistro(UserFileSystem ufs, FileView fileComunicaciones, Record recordComunicacion, Record recordExpediente, Dictionary<string, RecordValuesList> dictTipoIngresoSolicitante, out List<string> avisos)
        {
            avisos = new List<string>();

            var integrationRegistroES = ConexiónAlmacenRegistroES.ObtenerIntegration();
            
            string codExpediente = Escape(recordExpediente["CODEXPEDIENTE"].ValueFormatString);
            string referenciaExp = Escape(recordExpediente["REFERENCIAEXP"].ValueFormatString);
            string codComunicacion = Escape(recordComunicacion["CODCOMUNICACION"].ValueFormatString);
            string codTipoIngreso = recordExpediente["CODTIPOINGRESO"].ValueFormatString;

            bool solicitante = false;
            string tipoTransporte = "";
            string desTipoIngreso = "";
            string responsable = "";
            if (dictTipoIngresoSolicitante.ContainsKey(codTipoIngreso))
            {
                solicitante = "Solicitante".Equals(dictTipoIngresoSolicitante[codTipoIngreso]["DESTINATARIO"].ValueFormatString);
                tipoTransporte = dictTipoIngresoSolicitante[codTipoIngreso]["TIPOTRANSPORTE"].ValueFormatString;
                desTipoIngreso = dictTipoIngresoSolicitante[codTipoIngreso]["DESTIPOINGRESO"].ValueFormatString;
                responsable = dictTipoIngresoSolicitante[codTipoIngreso]["RESPONSABLE"].ValueFormatString;
            }

            var fieldDataList = new List<field_data>();
            
            // Vamos rellenando los campos del nuevo registro de salida
            fieldDataList.Add(new field_data() { field_name = "LSDESTINATARIO", data = new data_text() { value = "Libre" } });
            fieldDataList.Add(new field_data() { field_name = "BOREGSAL", data = new data_bool() { value = true } });
            
            string nombre, numidentif, tipoVia, nombreVia, numVia, escalera, piso, puerta,
                    codigoPostal, localidad, provincia;

            if (solicitante)
            {
                numidentif = Escape(recordExpediente["NUMDOCIDENTIFSOLIC"].ValueFormatString);
                nombre = Escape(recordExpediente["NOMBRESOLIC"].ValueFormatString);
                tipoVia = Escape(recordExpediente["TIPOVIASOLIC"].ValueFormatString);
                nombreVia = Escape(recordExpediente["NOMBREVIASOLIC"].ValueFormatString);
                numVia = Escape(recordExpediente["NUMVIASOLIC"].ValueFormatString);
                escalera = Escape(recordExpediente["ESCALSOLIC"].ValueFormatString);
                piso = Escape(recordExpediente["PISOSOLIC"].ValueFormatString);
                puerta = Escape(recordExpediente["PUERTASOLIC"].ValueFormatString);
                codigoPostal = Escape(recordExpediente["CODPOSSOLIC"].ValueFormatString);
                localidad = Escape(recordExpediente["MUNICSOLIC"].ValueFormatString);
                provincia = Escape(recordExpediente["PROVSOLIC"].ValueFormatString);
            }
            else
            {
                numidentif = Escape(recordExpediente["NUMDOCIDENTIFINTER"].ValueFormatString);
                nombre = Escape(recordExpediente["NOMBREINTER"].ValueFormatString);
                tipoVia = Escape(recordExpediente["TIPOVIAINTER"].ValueFormatString);
                nombreVia = Escape(recordExpediente["NOMBREVIAINTER"].ValueFormatString);
                numVia = Escape(recordExpediente["NUMVIAINTER"].ValueFormatString);
                escalera = Escape(recordExpediente["ESCALINTER"].ValueFormatString);
                piso = Escape(recordExpediente["PISOINTER"].ValueFormatString);
                puerta = Escape(recordExpediente["PUERTAINTER"].ValueFormatString);
                codigoPostal = Escape(recordExpediente["CODPOSINTER"].ValueFormatString);
                localidad = Escape(recordExpediente["MUNICINTER"].ValueFormatString);
                provincia = Escape(recordExpediente["PROVINTER"].ValueFormatString);
            }

            // Controlamos los tamaños máximos para la generación del formato de correos
            string textoTruncado = "";
            if (!CheckSize(nombre, 150, out nombre, out textoTruncado))
            {
                avisos.Add(string.Format("Se han truncado los siguientes caracteres: '{0}' del campo Nombre", textoTruncado));
            }
            fieldDataList.Add(new field_data() { field_name = "TXDSTNAME", data = new data_text() { value = nombre } });
            
            if (!CheckSize(numidentif, 20, out numidentif, out textoTruncado))
            {
                avisos.Add(string.Format("Se han truncado los siguientes caracteres: '{0}' del campo NIF", textoTruncado));
            }
            fieldDataList.Add(new field_data() { field_name = "TXDSTID", data = new data_text() { value = numidentif } });

            string direccion = "";
            Match match = Regex.Match(tipoVia, @"(?'abrv'\w*) - \w*", RegexOptions.None);
            if (match.Success)
            {
                tipoVia = match.Groups["abrv"].Value;
            }
            direccion = ConcatenateSpace(direccion, tipoVia);
            direccion = ConcatenateSpace(direccion, nombreVia);
            direccion = ConcatenateSpace(direccion, numVia);
            direccion = ConcatenateSpace(direccion, escalera);
            direccion = ConcatenateSpace(direccion, piso);
            direccion = ConcatenateSpace(direccion, puerta);
            if (!CheckSize(direccion, 200, out direccion, out textoTruncado))
            {
                avisos.Add(string.Format("Se han truncado los siguientes caracteres: '{0}' del campo Dirección", textoTruncado));
            }
            fieldDataList.Add(new field_data() { field_name = "TXDSTDIREC", data = new data_text() { value = direccion } });

            if (!CheckSize(codigoPostal, 8, out codigoPostal, out textoTruncado))
            {
                avisos.Add(string.Format("Se han truncado los siguientes caracteres: '{0}' del campo Código Postal", textoTruncado));
            }
            fieldDataList.Add(new field_data() { field_name = "TXDSTCP", data = new data_text() { value = codigoPostal } });
            
            if (!CheckSize(localidad, 80, out localidad, out textoTruncado))
            {
                avisos.Add(string.Format("Se han truncado los siguientes caracteres: '{0}' del campo Localidad", textoTruncado));
            }
            fieldDataList.Add(new field_data() { field_name = "TXDSTMUN", data = new data_text() { value = localidad } });

            if (!CheckSize(provincia, 80, out provincia, out textoTruncado))
            {
                avisos.Add(string.Format("Se han truncado los siguientes caracteres: '{0}' del campo Provincia", textoTruncado));
            }
            fieldDataList.Add(new field_data() { field_name = "TXDSTPROV", data = new data_text() { value = provincia } });

            fieldDataList.Add(new field_data() { field_name = "TXDSTPAIS", data = new data_text() { value = "ESPAÑA" } });
            fieldDataList.Add(new field_data() { field_name = "TXTRANSP", data = new data_text() { value = tipoTransporte } });
            fieldDataList.Add(new field_data() { field_name = "RESPONSABLE", data = new data_text() { value = responsable } });
            fieldDataList.Add(new field_data() { field_name = "TXIDEXTERNO", data = new data_text() { value = recordComunicacion.Number.ToString() } });
            fieldDataList.Add(new field_data() { field_name = "BOPAPEL", data = new data_bool() { value = true } });
            fieldDataList.Add(new field_data() { field_name = "TXTIPODOCU", data = new data_text() { value = "carta" } });
            fieldDataList.Add(new field_data() { field_name = "TXREMITENTE", data = new data_text() { value = "S.G. Política y Ayudas a la Vivienda" } });
            fieldDataList.Add(new field_data() { field_name = "TXCREADOR", data = new data_text() { value = "" } });
            fieldDataList.Add(new field_data() { field_name = "TXASUNTO", data = new data_text() { value = string.Format("REQUERIMIENTO DEVOLUCIÓN DE INGRESOS. {0}", desTipoIngreso) } });

            string grupoPropag = recordComunicacion["GRUPOPROPAG"].ValueFormatString;
            if (!CheckSize(grupoPropag, 100, out grupoPropag, out textoTruncado))
            {
                avisos.Add(string.Format("Se han truncado los siguientes caracteres: '{0}' del campo Grupo Expediente", textoTruncado));
            }
            fieldDataList.Add(new field_data() { field_name = "TXTEMA", data = new data_text() { value = grupoPropag } });

            string referenciaExpTruncada;
            if (!CheckSize(referenciaExp, 25, out referenciaExpTruncada, out textoTruncado))
            {
                avisos.Add(string.Format("Se han truncado los siguientes caracteres: '{0}' del campo Referencia Expediente", textoTruncado));
            }
            fieldDataList.Add(new field_data() { field_name = "TXNEXP", data = new data_text() { value = referenciaExpTruncada } });


            string tipoIngresoPropag = recordComunicacion["TIPOINGRESOPROPAG"].ValueFormatString;
            if (!CheckSize(tipoIngresoPropag, 150, out tipoIngresoPropag, out textoTruncado))
            {
                avisos.Add(string.Format("Se han truncado los siguientes caracteres: '{0}' del campo TipoIngreso", textoTruncado));
            }
            fieldDataList.Add(new field_data() { field_name = "TXRGASUNTO", data = new data_text() { value = tipoIngresoPropag } });
            //recordValuesRegistroSalida["TXESTADO"].Value = "ENVIAR";
            fieldDataList.Add(new field_data() { field_name = "LSNOTIF", data = new data_text() { value = "Notificación postal" } });
            fieldDataList.Add(new field_data() { field_name = "TXCODIGO", data = new data_text() { value = "SGPAV" } });

            System.Diagnostics.Trace.TraceInformation("Documentos adjuntos al expediente: " + recordExpediente.Documents.Count);

            bool exito = false;
            try
            {
                var fileId = new file_id() { index = decimal.Parse(ConfigurationManager.AppSettings["indiceRegistroSalida"]) };
                var actionInfo = new action_info() { reason = "Envío de comunicación", observations = "Envío de comunicación" };
                record record = integrationRegistroES.CreateRecord(fileId, fieldDataList.ToArray(), null, actionInfo);

                actionInfo = new action_info() { reason = "Envío documento de comunicación", observations = "Envío documento de comunicación" };

                foreach (var pixelDocument in recordExpediente.Documents)
                {
                    integrationRegistroES.CreateRecordDocument(record.record_id, pixelDocument.FileName, pixelDocument.GetContent(), actionInfo);
                }

                exito = true;
            }
            catch (Exception exc)
            {
                avisos.Add(string.Format("Error al enviar comunicación: {0}", exc.Message));
                System.Diagnostics.Trace.TraceError("Excepción al crear registro de salida: " + exc.ToString());
            }

            if (exito)
            {
                try
                {
                    // Cargo la comunicación entera de nuevo proque el edit record lanza controles de acciones que necesitan que todos los campos estén rellenos
                    Record recordComunicacion2 = Record.LoadRecord(ufs, fileComunicaciones, recordComunicacion.Number);
                    RecordEdition recordEditionIngresos = new RecordEdition(ufs);
                    RecordValuesList camposEditarComunicacion = new RecordValuesList();
                    camposEditarComunicacion.Add(fileComunicaciones.Fields["ESTADOCOMUNIC"].CreateFieldValue("PteRegistro"));
                    camposEditarComunicacion.Add(fileComunicaciones.Fields["FECHAMASIVORSU"].CreateFieldValue(DateTime.Now.Date));
                    recordEditionIngresos.EditRecord(recordComunicacion2, camposEditarComunicacion.GetDifferences(recordComunicacion2.Values),
                        new ActionInfo("DevIngresos", "Envío de comunicación a registro", "Envío de comunicación a registro"));

                    Record recordExpediente2 = Record.LoadRecord(ufs, recordExpediente.File, recordExpediente.Number);
                    RecordValuesList camposEditarExpediente = new RecordValuesList();
                    camposEditarExpediente.Add(recordExpediente.File.Fields["ESTADO"].CreateFieldValue("PteRegistro"));
                    recordEditionIngresos.EditRecord(recordExpediente2, camposEditarExpediente.GetDifferences(recordExpediente2.Values),
                        new ActionInfo("DevIngresos", "Envío de comunicación a registro", "Envío de comunicación a registro"));
                    
                }
                catch (Exception exc)
                {
                    avisos.Add(string.Format("Error al actualizar estado de la comunicación: {0}", exc.Message));
                    System.Diagnostics.Trace.TraceError("Excepción al actualizar estado de comunicación: " + exc.ToString());
                }
            }
            return exito;
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
        private string NumeroExpediente
        {
            get { return (string)ViewState["numeroExpediente"]; }
            set { ViewState["numeroExpediente"] = value; }
        }
        private string NumeroComunicacion
        {
            get { return (string)ViewState["numeroComunicacion"]; }
            set { ViewState["numeroComunicacion"] = value; }
        }
        private string TempFile
        {
            get { return (string) ViewState["TempFile"]; }
            set { ViewState["TempFile"] = value; }
        }
    }
}