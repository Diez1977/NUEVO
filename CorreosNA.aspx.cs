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

namespace AltaPagos
{
    public partial class CorreosNA : System.Web.UI.Page
    {
     
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Index = Request["index"];
                Filter = Request["filter"];
                Sort = Request["sort"];
                mainMultiView.SetActiveView(viewWait);
                ClientScript.RegisterStartupScript(this.GetType(), "cargaDatos",
                    ClientScript.GetPostBackEventReference(linkGenerar, ""), true);
            }
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
                // Primero cargamos todos los tipoingresos para más adelante determinar si se usarán campos de INTERESADO o de SOLICITANTE
                FileView fileTipoIngreso = ConexiónAlmacen.ObtenerFileViewTipoIngreso();
                List<string> fieldsTipoIngreso = new List<string>(new string[] { "CODTIPOINGRESO", "DESTINATARIO" });
                Dictionary<string, bool> dictTipoIngresoSolicitante = new Dictionary<string, bool>();
                PartialDataRecordProvider tipoIngresoProvider = new PartialDataRecordProvider(ufs, fileTipoIngreso, delegate(Field aux) { return fieldsTipoIngreso.Contains(aux.Name); });
                RecordReader tipoIngresosReader = tipoIngresoProvider.GetRecordsReader();
                while (tipoIngresosReader.Read())
                {
                    Record recordTipoIngreso = (Record) tipoIngresosReader.Current;
                    string codTipoIngreso = recordTipoIngreso["CODTIPOINGRESO"].ValueFormatString;
                    bool solicitante = recordTipoIngreso["DESTINATARIO"].ValueFormatString == "Solicitante";
                    if (!dictTipoIngresoSolicitante.ContainsKey(codTipoIngreso)) {
                        dictTipoIngresoSolicitante.Add(codTipoIngreso, solicitante);
                    }
                }

                FileView fileComunicaciones = ConexiónAlmacen.ObtenerFileViewComunicaciones();
                RecordFilterStringSerializer serializer = new RecordFilterStringSerializer(ufs);
                RecordFilter recordFilter = serializer.DeserializeFromString(Filter, fileComunicaciones);
                List<string> fieldsExpediente = new List<string>(new string[] { "CODEXPEDIENTE", "REFERENCIAEXP", "CODTIPOINGRESO", 
                    "NOMBREINTERAUX", "APELLIDO1INTERAUX", "APELLIDO2INTERAUX", 
                    "TIPOVIAINTER", "NOMBREVIAINTER", "NUMVIAINTER", "BLOQUEINTER", "ESCALINTER", "PISOINTER", "PUERTAINTER", 
                    "CODPOSINTER", "MUNICINTER", "PROVINTER", 
                    "NOMBRESOLICAUX", "APELLIDO1SOLICAUX", "APELLIDO2SOLICAUX", 
                    "TIPOVIASOLIC", "NOMBREVIASOLIC", "NUMVIASOLIC", "BLOQUESOLIC", "ESCALSOLIC", "PISOSOLIC", "PUERTASOLIC", 
                    "CODPOSSOLIC", "MUNICSOLIC", "PROVSOLIC"
                });
                List<string> fieldsComunicacion = new List<string>(new string[] { "CODCOMUNICACION" });

                Relation relExpComunicaciones = ConexiónAlmacen.ObtenerRelationExpComunicaciones();

                // Consultamos todos los registros de Comunicaciones, ponemos un fieldfilter vacío porque en realidad no vamos a leer campos
                // de esta ficha sino de la ficha padre
                PartialDataRecordProvider recordProvider = new PartialDataRecordProvider(ufs, fileComunicaciones, delegate(Field aux) { return fieldsComunicacion.Contains(aux.Name); });
                recordProvider.Filter = recordFilter;
                recordProvider.AddSortExpression(Sort);
                long count = recordProvider.GetTotalCount();
                if (count > 0)
                {
                    // Inicializamos el fichero de texto
                    List<AvisoCorreos> avisos = new List<AvisoCorreos>();
                    long numLinea = 1;
                    TempFile = System.IO.Path.GetTempFileName();
                    long length = 0;
                    using (FileStream tempStream = new FileStream(TempFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        StreamWriter writer = new StreamWriter(tempStream, Encoding.GetEncoding(1252)); // ANSI


                        List<Record> recordsComunicaciones = recordProvider.GetRecords();

                        // Recorremos cada comunicación
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
                            string referenciaexp = Escape(recordExpediente["REFERENCIAEXP"].ValueFormatString);
                            string codComunicacion = Escape(recordComunicacion["CODCOMUNICACION"].ValueFormatString);
                            string codTipoIngreso = recordExpediente["CODTIPOINGRESO"].ValueFormatString;

                            string nombre, apellido1, apellido2, tipoVia, nombreVia, numVia, escalera, piso, puerta,
                                codigoPostal, localidad, provincia, bloque;
                            if (!dictTipoIngresoSolicitante.ContainsKey(codTipoIngreso) ||
                                dictTipoIngresoSolicitante[codTipoIngreso])
                            {
                                nombre = Escape(recordExpediente["NOMBRESOLICAUX"].ValueFormatString);
                                apellido1 = Escape(recordExpediente["APELLIDO1SOLICAUX"].ValueFormatString);
                                apellido2 = Escape(recordExpediente["APELLIDO2SOLICAUX"].ValueFormatString);
                                tipoVia = Escape(recordExpediente["TIPOVIASOLIC"].ValueFormatString);
                                nombreVia = Escape(recordExpediente["NOMBREVIASOLIC"].ValueFormatString);
                                numVia = Escape(recordExpediente["NUMVIASOLIC"].ValueFormatString);
                                escalera = Escape(recordExpediente["ESCALSOLIC"].ValueFormatString);
                                piso = Escape(recordExpediente["PISOSOLIC"].ValueFormatString);
                                puerta = Escape(recordExpediente["PUERTASOLIC"].ValueFormatString);
                                codigoPostal = Escape(recordExpediente["CODPOSSOLIC"].ValueFormatString);
                                localidad = Escape(recordExpediente["MUNICSOLIC"].ValueFormatString);
                                provincia = Escape(recordExpediente["PROVSOLIC"].ValueFormatString);
                                bloque = Escape(recordExpediente["BLOQUESOLIC"].ValueFormatString);
                            }
                            else
                            {
                                nombre = Escape(recordExpediente["NOMBREINTERAUX"].ValueFormatString);
                                apellido1 = Escape(recordExpediente["APELLIDO1INTERAUX"].ValueFormatString);
                                apellido2 = Escape(recordExpediente["APELLIDO2INTERAUX"].ValueFormatString);
                                tipoVia = Escape(recordExpediente["TIPOVIAINTER"].ValueFormatString);
                                nombreVia = Escape(recordExpediente["NOMBREVIAINTER"].ValueFormatString);
                                numVia = Escape(recordExpediente["NUMVIAINTER"].ValueFormatString);
                                escalera = Escape(recordExpediente["ESCALINTER"].ValueFormatString);
                                piso = Escape(recordExpediente["PISOINTER"].ValueFormatString);
                                puerta = Escape(recordExpediente["PUERTAINTER"].ValueFormatString);
                                codigoPostal = Escape(recordExpediente["CODPOSINTER"].ValueFormatString);
                                localidad = Escape(recordExpediente["MUNICINTER"].ValueFormatString);
                                provincia = Escape(recordExpediente["PROVINTER"].ValueFormatString);
                                bloque = Escape(recordExpediente["BLOQUEINTER"].ValueFormatString);
                            }
                            if (!string.IsNullOrEmpty(bloque))
                            {
                                bloque = "BL " + bloque;
                            }
                            if (!string.IsNullOrEmpty(escalera))
                            {
                                escalera = "ESC " + escalera;
                            }

                            // Controlamos los tamaños máximos para la generación del formato de correos
                            string textoTruncado = "";
                            if (!CheckSize(nombre, 38, out nombre, out textoTruncado))
                            {
                                avisos.Add(new AvisoCorreos(referenciaexp, codComunicacion, string.Format("Se han truncado los siguientes caracteres: '{0}' del campo Nombre (línea {1} del fichero)", textoTruncado, numLinea)));
                            }

                            string apellidos = "";
                            apellidos = ConcatenateSpace(apellidos, apellido1);
                            apellidos = ConcatenateSpace(apellidos, apellido2);
                            if (!CheckSize(apellidos, 40, out apellidos, out textoTruncado))
                            {
                                avisos.Add(new AvisoCorreos(referenciaexp, codComunicacion, string.Format("Se han truncado los siguientes caracteres: '{0}' del campo Apellidos (línea {1} del fichero)", textoTruncado, numLinea)));
                            }

                            string direccion = "";
                            Match match = Regex.Match(tipoVia, @"(?'abrv'\w*) - \w*", RegexOptions.None);
                            if (match.Success)
                            {
                                tipoVia = match.Groups["abrv"].Value;
                            }
                            direccion = ConcatenateSpace(direccion, tipoVia);
                            direccion = ConcatenateSpace(direccion, nombreVia);
                            direccion = ConcatenateSpace(direccion, numVia);
                            direccion = ConcatenateSpace(direccion, bloque);
                            direccion = ConcatenateSpace(direccion, escalera);
                            direccion = ConcatenateSpace(direccion, piso);
                            direccion = ConcatenateSpace(direccion, puerta);
                            if (!CheckSize(direccion, 40, out direccion, out textoTruncado))
                            {
                                avisos.Add(new AvisoCorreos(referenciaexp, codComunicacion, string.Format("Se han truncado los siguientes caracteres: '{0}' del campo Dirección (línea {1} del fichero)", textoTruncado, numLinea)));
                            }

                            
                            if (!CheckSize(codigoPostal, 10, out codigoPostal, out textoTruncado))
                            {
                                avisos.Add(new AvisoCorreos(referenciaexp, codComunicacion, string.Format("Se han truncado los siguientes caracteres: '{0}' del campo Código Postal (línea {1} del fichero)", textoTruncado, numLinea)));
                            }

                            
                            if (!CheckSize(localidad, 40, out localidad, out textoTruncado))
                            {
                                avisos.Add(new AvisoCorreos(referenciaexp, codComunicacion, string.Format("Se han truncado los siguientes caracteres: '{0}' del campo Localidad (línea {1} del fichero)", textoTruncado, numLinea)));
                            }


                            if (!CheckSize(provincia, 30, out provincia, out textoTruncado))
                            {
                                avisos.Add(new AvisoCorreos(referenciaexp, codComunicacion, string.Format("Se han truncado los siguientes caracteres: '{0}' del campo Provincia (línea {1} del fichero)", textoTruncado, numLinea)));
                            }

                            string codExpedienteTruncated;
                            if (!CheckSize(referenciaexp, 30, out codExpedienteTruncated, out textoTruncado))
                            {
                                avisos.Add(new AvisoCorreos(referenciaexp, codComunicacion, string.Format("Se han truncado los siguientes caracteres: '{0}' del campo Número de Expediente (línea {1} del fichero)", textoTruncado, numLinea)));
                            }

                            string linea = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}", nombre, apellidos, "", direccion, codigoPostal, localidad, "", "ES", codExpedienteTruncated, "3");
                            writer.WriteLine(linea);
                            numLinea++;
                        }
                        writer.Flush();
                        length = tempStream.Length;
                    }

                    mainMultiView.SetActiveView(viewSuccess);
                    literalNumRegistros.Text = string.Format("Se han procesado e incluido en el fichero de texto {0} registros.", count);
                    linkDownload.Text = string.Format("Para descargar el fichero de texto generado ({0}), pulse aquí", ToKBString(length));

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
                System.Diagnostics.Trace.TraceError("Excepción: " + exc.ToString());
                MostrarError("Error inesperado en la generación del fichero. " + exc.Message);
            }
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
            }   else {
                newText = text;
                remainder = "";
                return true;
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

        private static string Escape(string text)
        {
            return text.Trim().Replace("\t", "");
        }

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
            Response.ContentType = "text/plain";
            Response.AddHeader("Content-Disposition",
                "attachment; filename=\"Correos-NA.txt\"");
            Response.TransmitFile(TempFile);
            Response.End();
        }

        private void MostrarError(string error)
        {
            literalError.Text = error;
            mainMultiView.SetActiveView(viewError);
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