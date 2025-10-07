using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Diagnostics;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Configuration;
using PixelwareApi.Data;
using PixelwareApi.File;
using PixelwareApi.File.Records;
using PixelwareApi.File.Relations;
using PixelwareApi.Web;
using PixelwareApi.File.UserActions;
using System.Web.Services;
using AltaPagos.Code;


namespace AltaPagos
{
    public partial class PageServer : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        [WebMethod]
        public static void SaveGrid(List<Code.Row> rows)
        {
            System.Diagnostics.Trace.WriteLine("SaveGrid: Parámetros correctos");
            UserFileSystem ufs = Code.ConexiónAlmacen.ObtenerUserFileSystem();

            System.Diagnostics.Trace.WriteLine("SaveGrid: UserFileSystem correcto");

            foreach (Code.Row row in rows)
            {
                using (DbTransactionScope trans = ufs.Database.ComienzaTransaccion())
                {
                    try
                    {
                        PixelwareApi.File.UserActions.RecordEdition recordEdition = new PixelwareApi.File.UserActions.RecordEdition(ufs);
                        Record childRecord = Record.LoadRecord(ufs, ConexiónAlmacen.ObtenerFileViewPagos(), row.childnumber);
                        System.Diagnostics.Trace.WriteLine("SaveGrid: Registro cargado correctamente");
                        FileView filePagos = ConexiónAlmacen.ObtenerFileViewPagos();
                        //RecordValuesList recordValues = childRecord.Values;
                        RecordValuesList recordValues = new RecordValuesList();
                        //Valores del registro  
                        if (row.fechapago != null && row.fechapago != "")
                        {
                            recordValues.Add(filePagos.Fields["FECHAPAGO"].CreateFieldValue(row.fechapago));
                            System.Diagnostics.Trace.WriteLine(string.Format("SaveGrid: ----- FECHAPAGO: {0}", row.fechapago));
                        }                          
                        else
                            throw new Exception("No se puede guardar el pago sin el campo FECHAPAGO");

                        row.ppalpago = row.ppalpago.Replace(".", ",");

                        decimal ppaldecimal = Decimal.Parse(row.ppalpago);
                        recordValues.Add(filePagos.Fields["PPALPAGO"].CreateFieldValue(ppaldecimal));
                        System.Diagnostics.Trace.WriteLine(string.Format("SaveGrid: ----- PPALPAGO: {0}", ppaldecimal));

                        row.interespago = row.interespago.Replace(".", ",");

                        recordValues.Add(filePagos.Fields["INTERESPAGO"].CreateFieldValue(row.interespago));
                        System.Diagnostics.Trace.WriteLine(string.Format("SaveGrid: ----- INTERESPAGO: {0}", row.interespago));

                        if (row.ConceptoPago != null && row.ConceptoPago != "")
                        {
                            recordValues.Add(filePagos.Fields["CONCEPTOPAGO"].CreateFieldValue(row.ConceptoPago));
                            System.Diagnostics.Trace.WriteLine(string.Format("SaveGrid: ----- CONCEPTOPAGO: {0}", ppaldecimal));
                        }
                        else
                            throw new Exception("No se puede guardar el pago sin el campo CONCEPTOPAGO");

                        recordValues.Add(filePagos.Fields["CONCPRESUP1"].CreateFieldValue(row.cpresupuesto1));
                        System.Diagnostics.Trace.WriteLine(string.Format("SaveGrid: ----- CONCPRESUP1: {0}", row.cpresupuesto1));
                        recordValues.Add(filePagos.Fields["CONCPRESUP2"].CreateFieldValue(row.cpresupuesto2));
                        System.Diagnostics.Trace.WriteLine(string.Format("SaveGrid: ----- CONCPRESUP2: {0}", row.cpresupuesto2));
                        recordValues.Add(filePagos.Fields["CONCPRESUP3"].CreateFieldValue(row.cpresupuesto3));
                        System.Diagnostics.Trace.WriteLine(string.Format("SaveGrid: ----- CONCPRESUP3: {0}", row.cpresupuesto3));
                        recordValues.Add(filePagos.Fields["CONCPRESUP4"].CreateFieldValue(row.cpresupuesto4));
                        System.Diagnostics.Trace.WriteLine(string.Format("SaveGrid: ----- CONCPRESUP4: {0}", row.cpresupuesto4));

                        recordValues.Add(filePagos.Fields["CODEXPED"].CreateFieldValue(row.codexp));

                        recordValues.Add(filePagos.Fields["NUMCUENTABANCARIA"].CreateFieldValue(row.EntidadBancaria));
                        System.Diagnostics.Trace.WriteLine(string.Format("SaveGrid: ----- NUMCUENTABANCARIA: {0}", row.EntidadBancaria));
                        recordValues.Add(filePagos.Fields["TIPOINGRESOPROPAG"].CreateFieldValue(row.codtipoingreso));
                        System.Diagnostics.Trace.WriteLine(string.Format("SaveGrid: ----- TIPOINGRESOPROPAG: {0}", row.codtipoingreso));
                        ActionInfo action = new ActionInfo("SaveGrid", "Salvar Pago", "");
                        recordEdition.EditRecord(childRecord, recordValues.GetDifferences(childRecord.Values), action);
                        //recordEdition.EditRecord(childRecord, recordValues, action);
                        System.Diagnostics.Trace.WriteLine("SaveGrid: Registro salvado correctamente");
                        trans.Complete();
                    }
                    catch (Exception exc)
                    {
                        System.Diagnostics.Trace.WriteLine("SaveGrid: Exception-> " + exc.ToString());
                        trans.Dispose();
                        throw exc;
                    }
                }


            }
        }
    }
}