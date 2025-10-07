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
    public partial class PropagacionExpedientes : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                index = Request["index"];
                filter = Request["filter"];
                sort = Request["sort"];
                
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

        protected void linkContinuar_Click(object sender, EventArgs e)
        {
            // Se cachean los valores del formulario
            try
            {
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


        protected void linkGenerar_Click(object sender, EventArgs e)
        {
            DoWork();
        }


        private void DoWork()
        {
            List<AvisoCorreos> avisos = new List<AvisoCorreos>();

            // Obtenemos los expedientes nuevamente
            using (DbTransactionScope transaction = userFileSystem.Database.ComienzaTransaccion())
            {
                try
                {
                    ExpedienteCollection expedientes = ExpedienteCollection.LoadExpededientes(filter, sort);
                    foreach (Expediente expediente in expedientes)
                    {
                        Expediente newExpediente = expediente.PropagateExpediente();
                        avisos.Add(new AvisoCorreos(Escape(expediente.ReferenciaExp), Escape(newExpediente.ReferenciaExp), string.Format("Propagado correctamente")));
                    }

                    // Se acepta la transación
                    transaction.Complete();

                    // Se cambia la vista a la de éxito
                    mainMultiView.SetActiveView(viewSuccess);
                    literalNumRegistros.Text = string.Format("Se han propagado con éxito los {0} expedientes.\n", expedientes.Count);
                    CreateResults(avisos);
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("Excepción: " + ex);
                    MostrarError(string.Format("Error al propagar: {0}", ex.Message));
                    transaction.Dispose();
                }
            }
        }

        private void CreateResults(List<AvisoCorreos> avisos)
        {
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

        private UserFileSystem userFileSystem
        {
            get {  return ConexiónAlmacen.ObtenerUserFileSystem(); }
        }
    }
}