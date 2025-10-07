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
    public partial class ImportesMasivo : System.Web.UI.Page
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

                FileView fileExpedientes = ConexiónAlmacen.ObtenerFileViewExpedientes();
                RecordFilterStringSerializer serializer = new RecordFilterStringSerializer(ufs);
                RecordFilter recordFilter = serializer.DeserializeFromString(Filter, fileExpedientes);
                Relation relExpComunicaciones = ConexiónAlmacen.ObtenerRelationExpComunicaciones();

                // Consultamos todos los registros de Comunicaciones
                RecordProvider recordProvider = new RecordProvider(ufs, fileExpedientes);
                recordProvider.Filter = recordFilter;
                recordProvider.AddSortExpression(Sort);
                long count = recordProvider.GetTotalCount();
                if (count > 0)
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Columns.Add("PWSNUMERO", typeof(decimal));
                    dataTable.Columns.Add("FECHAALTA", typeof(string));
                    dataTable.Columns.Add("REFERENCIAEXP", typeof(string));
                    dataTable.Columns.Add("FECHALIQUID", typeof(string));
                    dataTable.Columns.Add("NOMBREINTER", typeof(string));
                    dataTable.Columns.Add("NUMDOCIDENTIFINTER", typeof(string));
                    dataTable.Columns.Add("MOTIVOEXPEDICION", typeof(string));
                    dataTable.Columns.Add("MUNICINTER", typeof(string));

                    // Recorremos cada expediente
                    List<AvisoCorreos> avisos = new List<AvisoCorreos>();
                    RecordReader recordReaderExpedientes = recordProvider.GetRecordsReader();
                    while (recordReaderExpedientes.Read())
                    {
                        Record recordExpediente = (Record)recordReaderExpedientes.Current;

                        string fechaAltaExp = recordExpediente["FECHAALTA"].IsNull() ? "" : Escape(string.Format("{0:dd/MM/yyyy}", recordExpediente["FECHAALTA"].Value));
                        string referenciaExp = Escape(recordExpediente["REFERENCIAEXP"].ValueFormatString);
                        string fechaLiquidacion = recordExpediente["FECHALIQUID"].IsNull() ? "" : Escape(string.Format("{0:dd/MM/yyyy}", recordExpediente["FECHALIQUID"].Value));
                        string nombreApellidos = Escape(recordExpediente["NOMBREINTER"].ValueFormatString);
                        string nif = Escape(recordExpediente["NUMDOCIDENTIFINTER"].ValueFormatString);
                        string motivo = Escape(recordExpediente["MOTIVOEXPEDICION"].ValueFormatString);
                        string municipio = Escape(recordExpediente["MUNICINTER"].ValueFormatString);

                        dataTable.Rows.Add(new object[] { recordExpediente.Number, fechaAltaExp, referenciaExp, fechaLiquidacion, nombreApellidos, 
                            nif, motivo, municipio});
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
                MostrarError("Error inesperado en al cargar información sobre los expedientes. " + exc.Message);
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
                System.Diagnostics.Trace.TraceInformation("Inicio del proceso de generación de liquidaciones masivo");
                
                UserFileSystem ufs = ConexiónAlmacen.ObtenerUserFileSystem();
               
                FileView fileExpedientes = ConexiónAlmacen.ObtenerFileViewExpedientes();
                RecordFilterStringSerializer serializer = new RecordFilterStringSerializer(ufs);
                RecordFilter recordFilter = serializer.DeserializeFromString(Filter, fileExpedientes);
                Relation relExpComunicaciones = ConexiónAlmacen.ObtenerRelationExpComunicaciones();

                // Consultamos todos los registros de Expedientes
                RecordProvider recordProvider = new RecordProvider(ufs, fileExpedientes);
                recordProvider.Filter = recordFilter;
                recordProvider.AddSortExpression(Sort);
                long count = recordProvider.GetTotalCount();
                long success = 0;
                System.Diagnostics.Trace.TraceInformation("Procesando {0} expedientes", count);
                if (count > 0)
                {
                    // Recorremos cada expediente
                    DataTable dataTable = new DataTable();
                    dataTable.Columns.Add("PWSNUMERO", typeof(decimal));
                    dataTable.Columns.Add("FECHAALTA", typeof(string));
                    dataTable.Columns.Add("REFERENCIAEXP", typeof(string));
                    dataTable.Columns.Add("FECHALIQUID", typeof(string));
                    dataTable.Columns.Add("NOMBREINTER", typeof(string));
                    dataTable.Columns.Add("NUMDOCIDENTIFINTER", typeof(string));
                    dataTable.Columns.Add("MOTIVOEXPEDICION", typeof(string));
                    dataTable.Columns.Add("IMPORTETOTAL", typeof(string));
                    dataTable.Columns.Add("MUNICINTER", typeof(string));
                    dataTable.Columns.Add("ESTADOPROPAG", typeof(string));
                    dataTable.Columns.Add("RESULTADO", typeof(string));

                    List<AvisoCorreos> avisos = new List<AvisoCorreos>();
                    RecordReader recordReaderExpedientes = recordProvider.GetRecordsReader();
                    while (recordReaderExpedientes.Read())
                    {
                        Record recordExpediente = (Record)recordReaderExpedientes.Current;
                        decimal pwsnumeroExpediente = recordExpediente.Number;
                        System.Diagnostics.Trace.TraceInformation("Procesando expediente pwsnumero={0}", recordExpediente.Number);

                        string codComunicacion = "";
                        string estado = "";
                        string resultado = "";
                        string referenciaExp = Escape(recordExpediente["REFERENCIAEXP"].ValueFormatString);

                        try
                        {
                            GeneracionImportes generacionImportes = new GeneracionImportes(ufs);
                            Vivienda.InfoLiquidacion infoLiquidacion = generacionImportes.GenerarImportesExpediente(recordExpediente);
                            
                            codComunicacion = Escape(infoLiquidacion.RecordComunicacion.ChildPart["CODCOMUNICACION"].ValueFormatString);
                            
                            System.Diagnostics.Trace.TraceInformation("Liquidación generada con éxito");
                            
                            resultado = "Liquidación generada con éxito";
                            // estado = infoLiquidacion.RecordComunicacion.ChildPart["ESTADOPROPAG"].ValueFormatString;
                            Record comunicacionActualizada = Record.LoadRecord(ufs, ConexiónAlmacen.ObtenerFileViewComunicaciones(), infoLiquidacion.RecordComunicacion.ChildPart.Number);
                            estado = Escape(comunicacionActualizada["ESTADOPROPAG"].ToStringFormat());

                            success++;
                        } catch (Exception exc) {
                            avisos.Add(new AvisoCorreos(referenciaExp, codComunicacion, string.Format("Error al generar liquidación: {0}", exc.Message)));
                            resultado = string.Format("Excepción al generar liquidación: {0}", exc.Message);
                            System.Diagnostics.Trace.TraceError("Excepción al generar liquidación: " + exc.ToString());
                        }
                        finally
                        {
                            // Recargamos el record de expediente debido a un control de acciones.
                            recordExpediente = Record.LoadRecord(ufs, fileExpedientes, pwsnumeroExpediente);

                            string codExpediente = Escape(recordExpediente["CODEXPEDIENTE"].ValueFormatString);
                            string fechaAltaExp = recordExpediente["FECHAALTA"].IsNull() ? "" : Escape(string.Format("{0:dd/MM/yyyy}", recordExpediente["FECHAALTA"].Value));
                            string fechaLiquidacion = recordExpediente["FECHALIQUID"].IsNull() ? "" : Escape(string.Format("{0:dd/MM/yyyy}", recordExpediente["FECHALIQUID"].Value));
                            string nombreApellidos = Escape(recordExpediente["NOMBREINTER"].ValueFormatString);
                            string nif = Escape(recordExpediente["NUMDOCIDENTIFINTER"].ValueFormatString);
                            string motivo = Escape(recordExpediente["MOTIVOEXPEDICION"].ValueFormatString);
                            string importeTotal = recordExpediente["IMPORTETOTAL"].IsNull() ? "" :
                                Escape(string.Format("{0:C}", recordExpediente["IMPORTETOTAL"].Value));
                            string municipio = Escape(recordExpediente["MUNICINTER"].ValueFormatString);

                            dataTable.Rows.Add(new object[] { recordExpediente.Number, fechaAltaExp, referenciaExp, fechaLiquidacion, nombreApellidos, 
                            nif, motivo, importeTotal, municipio, estado, resultado});
                        }
                    }

                    mainMultiView.SetActiveView(viewSuccess);
                    literalNumRegistros.Text = string.Format("Se han generado {0} de {1} liquidaciones con éxito.", success, count);
                    subtitleText.Visible = success != count;

                    panelWarnings.Visible = true;
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
                MostrarError("Error inesperado en la generación de liquidaciones. " + exc.Message);
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