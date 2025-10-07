using AltaPagos.Code;
using iTextSharp.text;
using iTextSharp.text.pdf;
using PixelwareApi.File;
using SicerAPI;
using SicerAPI.Utils;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.UI;

namespace AltaPagos
{
    public partial class ImprimirCodigosSicer : Page
    {
        public enum ModoLlamada
        {
            Remesa = 0,
            Comunicacion = 1
        }

        public ModoLlamada Modo
        {
            get
            {
                if (Request["modo"] == null)
                {
                    MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Error, 0, "Error llamada a ImprimirCodigosSicer.aspx, parámetro 'modo' no encontrado");
                    throw new Exception("Error llamada a ImprimirCodigosSicer.aspx, parámetro 'modo' no encontrado");
                }
                else
                {
                    return (ModoLlamada)Convert.ToInt32(Request["modo"]);
                }
            }
        }

        public string CodigoRemesa
        {
            get
            {
                if (Request["codigoRemesa"] == null)
                {
                    MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Error, 0, "Error llamada a ImprimirCodigosSicer.aspx, parámetro 'codigoRemesa' no encontrado");
                    throw new Exception("Error llamada a ImprimirCodigosSicer.aspx, parámetro 'codigoRemesa' no encontrado");
                }
                else
                {
                    return Request["codigoRemesa"].ToString();
                }
            }
        }

        public decimal PwsNumeroComunicacion
        {
            get
            {
                if (Request["pwsNumeroComunicacion"] == null)
                {
                    MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Error, 0, "Error llamada a ImprimirCodigosSicer.aspx, parámetro 'pwsNumeroComunicacion' no encontrado");
                    throw new Exception("Error llamada a ImprimirCodigosSicer.aspx, parámetro 'pwsNumeroComunicacion' no encontrado");
                }
                else
                {
                    return Convert.ToDecimal(Request["pwsNumeroComunicacion"]);
                }
            }
        }

        private int _mumFilStampSICER;
        private int _FilaComienzo;
        private string _path;
        private int _tipoColumna;

        protected void Page_Load(object sender, EventArgs e)
        {
            MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Verbose, 0, "Entramos en ImprimirCodigosSicer");

            if (!IsPostBack)
            {
                if (Modo != ModoLlamada.Remesa && Modo != ModoLlamada.Comunicacion)
                {
                    MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Error, 0, "Modo de entrada '{0}' incorrecto", Convert.ToInt32(Modo));
                    throw new InvalidOperationException(string.Format("Modo de entrada '{0}' incorrecto", Convert.ToInt32(Modo)));
                }

                MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Verbose, 0, "Obtenemos el valor del parámetro NumFilStampSICER");

                Parametros parametros = Parametros.GetParametros("NumFilStampSICER", "SICER");

                if (parametros != null && !string.IsNullOrEmpty(parametros.ValorTX.ToString()))
                {
                    MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Verbose, 0, "Número de filas: {0}", parametros.ValorTX);

                    _mumFilStampSICER = Convert.ToInt32(parametros.ValorTX);

                    for (int i = 0; i < _mumFilStampSICER; i++)
                    {
                        filasBD.Items.Add(new System.Web.UI.WebControls.ListItem((i + 1).ToString(), i.ToString()));
                    }

                    // tipo de columna por defecto
                    parametros = Parametros.GetParametros("ModeStampSICER", "SICER");
                    columnas.SelectedValue = parametros.ValorTX.ToString();
                }
                else
                {
                    throw new InvalidOperationException("Error llamada a ImprimirCodigosSicer.aspx, parámetro 'pwsNumeroComunicacion' no encontrado");
                }
            }
        }

        protected void linkImprimir_Click(object sender, EventArgs e)
        {
            try
            {
                MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Verbose, 0, "Entramos en linkImprimir_Click");

                _tipoColumna = Convert.ToInt32(columnas.SelectedValue);
                _FilaComienzo = _tipoColumna == 1 ? Convert.ToInt32(filasBD.SelectedValue) : Convert.ToInt32(filasEstandar.SelectedValue);

                MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Verbose, 0, "Tipo de columna: {0}, fila de comienzo: {1}", _tipoColumna, _FilaComienzo);

                // Creamos el documento
                Document pdfDocument = new Document(PageSize.A4); // Tamaño A4

                _path = Path.GetTempFileName();
                PdfWriter.GetInstance(pdfDocument, new FileStream(_path, FileMode.Create));

                pdfDocument.Open();

                if (_tipoColumna == 1)
                    GenerarEtiquetasParam(pdfDocument);
                else
                    GenerarEtiquetas(pdfDocument);
            } 
            catch (Exception exc)
            {
                MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Error, 0, "Error en linkImprimir_Click: {0}", exc.Message);
                throw new InvalidOperationException(string.Format("Error en linkImprimir_Click: {0}", exc.Message));
            }
        }


        private void GenerarEtiquetas(Document pdfDocument)
        {
            MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Verbose, 0, "Generamos las etiquetas de 3 en 3");
            // Obtenemos la vista auxiliar para recuperar el segundo sello (tercera columna)
            SchemeNode schemeNodeVista = SchemeNode.LoadNodeById(SessionData.UserFileSystem, ModeloDatos.Instance.IndiceVistaComunicaciones);
            FileView fileViewVista = FileView.LoadFileView(SessionData.UserFileSystem, schemeNodeVista);

            int columns = 3; // Columnas (tres)
            int rows = 17; // Filas (diecisiete)                
            
            // Valores de alto y ancho del código de barras
            float heightScaleFactor = 0.97f;
            float stampA4Width = pdfDocument.PageSize.Width / columns;
            float stampA4Height = (pdfDocument.PageSize.Height / rows) * heightScaleFactor;

            int x = 0;
            int y = _FilaComienzo;

            if (Modo == ModoLlamada.Remesa) // Modo remesa, todas sus comunicaciones
            {
                Remesa remesa = Remesa.LoadRemesa(CodigoRemesa);

                foreach (decimal pwsnumero in remesa.Comunicaciones.Select(com => com.PWSNumero))
                {
                    if (y % rows == 0 && y != 0)
                    {
                        pdfDocument.NewPage();
                    }

                    iTextSharp.text.Image picture = ObtenerImagenSello(Comunicacion.FileComunicacion, pwsnumero);
                    AdjuntarSelloPosicion(x % columns, y % rows, stampA4Width, stampA4Height, picture, ref pdfDocument); x++;
                    AdjuntarSelloPosicion(x % columns, y % rows, stampA4Width, stampA4Height, picture, ref pdfDocument); x++;

                    picture = ObtenerImagenSello(fileViewVista, pwsnumero);
                    AdjuntarSelloPosicion(x % columns, y % rows, stampA4Width, stampA4Height, picture, ref pdfDocument); x++;

                    y++;
                }
            }
            else // Modo comunicacion
            {
                iTextSharp.text.Image picture = ObtenerImagenSello(Comunicacion.FileComunicacion, PwsNumeroComunicacion);
                AdjuntarSelloPosicion(x % columns, y % rows, stampA4Width, stampA4Height, picture, ref pdfDocument); x++;
                AdjuntarSelloPosicion(x % columns, y % rows, stampA4Width, stampA4Height, picture, ref pdfDocument); x++;

                picture = ObtenerImagenSello(fileViewVista, PwsNumeroComunicacion);
                AdjuntarSelloPosicion(x % columns, y % rows, stampA4Width, stampA4Height, picture, ref pdfDocument);
            }

            pdfDocument.Close();

            GenerarDocumento();
        }

        private void GenerarEtiquetasParam(Document pdfDocument)
        {
            MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Verbose, 0, "Generamos las etiquetas de 1 en 1");
            // Número de columnas por fichero
            Parametros parametros = Parametros.GetParametros("NumColStampSICER", "SICER");
            int columns = Convert.ToInt32(parametros.ValorTX); // Columnas 

            parametros = Parametros.GetParametros("NumFilStampSICER", "SICER");
            int rows = Convert.ToInt32(parametros.ValorTX); // Filas

            MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Verbose, 0, "Columnas {0}, filas {1}", columns, rows);

            // Factor corrección altura
            parametros = Parametros.GetParametros("FcFilStampSICER", "SICER");

            MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Verbose, 0, "FcFilStampSICER: {0}", parametros.ValorTX);

            float heightScaleFactor = float.Parse(parametros.ValorTX.ToString(), CultureInfo.InvariantCulture.NumberFormat);

            MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Verbose, 0, "heightScaleFactor: {0}", heightScaleFactor);

            float stampA4Width = pdfDocument.PageSize.Width / columns;

            MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Verbose, 0, "stampA4Width: {0}", stampA4Width);

            float stampA4Height = (pdfDocument.PageSize.Height / rows) * heightScaleFactor;

            MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Verbose, 0, "stampA4Height: {0}", stampA4Height);

            int x = 0;
            int y = _FilaComienzo;


            if (Modo == ModoLlamada.Remesa) // Modo remesa, todas sus comunicaciones
            {
                Remesa remesa = Remesa.LoadRemesa(CodigoRemesa);

                MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Verbose, 0, "Es modo Remesa y son : {0} comunicaciones", remesa.Comunicaciones.Count());

                foreach (decimal pwsnumero in remesa.Comunicaciones.Select(com => com.PWSNumero))
                {
                    if (y % rows == 0 && x % columns == 0 && y != 0)
                    {
                        MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Verbose, 0, "Se avanza a la página siguiente ya que la fila global es : {0} y el modulo de Y - {0} % filas - {1} = {2}", y, rows, (y % rows));
                        pdfDocument.NewPage();
                    }

                    iTextSharp.text.Image picture = ObtenerImagenSello(Comunicacion.FileComunicacion, pwsnumero);
                    AdjuntarSelloPosicion(x % columns, y % rows, stampA4Width, stampA4Height, picture, ref pdfDocument); x++;

                    if (x % columns == 0 && x != 0)
                    {
                        MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Verbose, 0, "Se avanza a la fila siguiente ya que la etiqueta es la numero  : {0} y el modulo de {0} % {1} = {2}", x, columns, (x % columns));
                        y++;
                    }   
                }
            }
            else // Modo comunicacion
            {
                iTextSharp.text.Image picture = ObtenerImagenSello(Comunicacion.FileComunicacion, PwsNumeroComunicacion);
                AdjuntarSelloPosicion(x % columns, y % rows, stampA4Width, stampA4Height, picture, ref pdfDocument);
            }

            pdfDocument.Close();

            GenerarDocumento();

        }

        private void GenerarDocumento()
        {
            // Recuperamos los bytes de contenido
            byte[] bytes = null;

            using (FileStream stream = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                stream.Position = 0;
                BinaryReader reader = new BinaryReader(stream);
                bytes = reader.ReadBytes(Convert.ToInt32(stream.Length));
            }

            // Lo devolvemos como respuesta
            Response.Clear();
            Response.ContentType = "application/pdf";
            Response.AddHeader("Content-Type", "application/pdf");
            Response.AddHeader("Content-Disposition", string.Format("attachment;filename={0}",
                string.Format("Etiquetas_{0}_{1}.pdf", Modo == ModoLlamada.Remesa ? CodigoRemesa : PwsNumeroComunicacion.ToString(),
                DateTime.Now.ToString("dd-MM-yyyy"))));
            Response.Buffer = true;
            Response.BinaryWrite(bytes);
            Response.Flush();
            Response.End();
        }

        private iTextSharp.text.Image ObtenerImagenSello(FileView fileView, decimal pwsNumero)
        {
            MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Information, 0, "Oteniendo sello: indice {0}, numero {1}", fileView.Node.Id, pwsNumero);

            Bitmap stampBitmap = StampFactory.GetStampImage(fileView, pwsNumero);
            return iTextSharp.text.Image.GetInstance(stampBitmap, ImageFormat.Png);
        }

        private void AdjuntarSelloPosicion(int x, int y, float stampA4Width, float stampA4Height, iTextSharp.text.Image picture, ref Document pdfDocument)
        {
            // Factor corrección ancho
            int ancho = _tipoColumna == 1 ? Convert.ToInt32(Parametros.GetParametros("FcColStampSICER", "SICER").ValorTX) : 18;

            Parametros parametrosBordeGrande = Parametros.GetParametros("BordeGrande", "SICER");
            int bordeGrande = Convert.ToInt32(parametrosBordeGrande.ValorTX); // Columnas 

            Parametros parametrosBordeMenor = Parametros.GetParametros("BordeMenor", "SICER");
            int bordeMenor = Convert.ToInt32(parametrosBordeMenor.ValorTX); // Columnas 

            // Números mágicos debido a las medidas de una hoja A4 y a meter 3 x 17 etiquetas de 70 mm x 16,9 mm
            int superiorEdge = Properties.Settings.Default.BordeSuperiorGrande ? bordeGrande : bordeMenor;

            int yPosition = (int)(pdfDocument.Top - superiorEdge - (y * stampA4Height));
            int xPosition = (int)(x * stampA4Width) + ancho;

            picture.SetAbsolutePosition(xPosition, yPosition);
            picture.ScaleToFit(stampA4Width, stampA4Height);

            MyTraceSource.myTraceSource.TraceEvent(TraceEventType.Verbose, 0, "Se añade el tiket siguiente en la posicion x = {0}, y = {1}, pagina = {2}", x, y, pdfDocument.PageNumber);

            pdfDocument.Add(picture);
        }
    }
}