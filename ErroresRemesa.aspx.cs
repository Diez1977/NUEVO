using AltaPagos.Code;
using PixelwareApi.File;
using PixelwareApi.File.Records;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AltaPagos
{
    public partial class ErroresRemesa : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
			try
			{
				UserFileSystem ufs = ConexiónAlmacen.ObtenerUserFileSystem();
				FileView fileComunicaciones = ConexiónAlmacen.ObtenerFileViewComunicaciones();
				RecordProvider recordPRemesa = new RecordProvider(ufs, fileComunicaciones);
				recordPRemesa.Filter.AddFilter(PixelwareApi.File.Records.Searching.FieldFilterData.CreateFieldFilter(ufs,
					fileComunicaciones.Fields["CODREMESA"],
					PixelwareApi.Data.Filters.FilterOperator.Is,
					Request.QueryString["codRemesa"]));
				recordPRemesa.Filter.AddFilter(PixelwareApi.File.Records.Searching.FieldFilterData.CreateFieldFilter(ufs,
					fileComunicaciones.Fields["ESTADOENVIO"],
					PixelwareApi.Data.Filters.FilterOperator.Is,
					"Errores"));

				long count = recordPRemesa.GetTotalCount();
				if (count > 0)
				{
					System.Diagnostics.Trace.TraceInformation("La remesa {0} tiene {1} comunicación(es) con errores", Request.QueryString["codRemesa"], count);

					DataTable dataTable = new DataTable();
					dataTable.Columns.Add("PWSNUMERO", typeof(decimal));
					dataTable.Columns.Add("REFERENCIAEXP", typeof(string));
					dataTable.Columns.Add("CODCOMUNICACION", typeof(string));
					dataTable.Columns.Add("IDERRORSICER", typeof(string));
					dataTable.Columns.Add("DESCERRORSICER", typeof(string));


					using (RecordReader recordReader = recordPRemesa.GetRecordsReader())
					{
						while (recordReader.Read())
						{
							Record record = (Record)recordReader.Current;

							string referenciaExp = Escape(record["REFERENCIAEXP"].ValueFormatString);
							string codComunicacion = Escape(record["CODCOMUNICACION"].ValueFormatString);
							string idErrorSicer = Escape(record["IDERRORSICER"].ValueFormatString);
							string descErrorSicer = Escape(record["DESCERRORSICER"].ValueFormatString);

							dataTable.Rows.Add(new object[] { record.Number, referenciaExp, codComunicacion, idErrorSicer, descErrorSicer });
						}
					}

					gridViewRemesas.DataSource = dataTable;
					gridViewRemesas.DataBind();
				}
				else
				{
					System.Diagnostics.Trace.TraceError("Se intenta mostrar los errores de la remesa {0} pero ésta no tiene ninguno", Request.QueryString["codRemesa"]);
				}
			}
			catch(Exception ex)
			{
				System.Diagnostics.Trace.TraceError("Se ha producido un error al obtener los errores de la remesa: {0}", ex);
			}
        }

        private static string Escape(string text)
        {
            return text.Trim();
        }
    }
}