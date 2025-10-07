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

namespace AltaPagos
{
    public partial class AltaPagos : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //decimal pwsnumero = 0, indExpedientes = 0;

            //if (!decimal.TryParse(HttpContext.Current.Request["pwsnumero"], out pwsnumero))
            //    throw new Exception("Param pwsnumero not valid");


            //indExpedientes = decimal.Parse(ConfigurationManager.AppSettings["indiceExpedientes"]);

            //UserFileSystem ufs = AltaPagos.Code.ConexiónAlmacen.ObtenerUserFileSystem();        

            //Record parentRecord = Record.LoadRecord(ufs, FileView.LoadFileView(ufs,
            //    SchemeNode.LoadNodeById(ufs, indExpedientes)), pwsnumero);

            //tituloLabel.Text = "Alta de pagos asociados al Expediente \"" + parentRecord.Values["REFERENCIAEXP"].ToString() + "\"" ;
        }
    }
}
