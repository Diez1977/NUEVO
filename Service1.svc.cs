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
using AltaPagos.Code;

namespace AltaPagos
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    [ServiceContract(Namespace = "")]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class Service1
    {
        // To use HTTP GET, add [WebGet] attribute. (Default ResponseFormat is WebMessageFormat.Json)
        // To create an operation that returns XML,
        //     add [WebGet(ResponseFormat=WebMessageFormat.Xml)],
        //     and include the following line in the operation body:
        //         WebOperationContext.Current.OutgoingResponse.ContentType = "text/xml";
        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        public Code.Pagos GetPagos(string sidx, string sord, int page, int rows)
        {
            Code.Pagos pagos = new Code.Pagos();

            decimal pwsnumero = 0;
            string field = null;
            
            if (!decimal.TryParse(HttpContext.Current.Request["pwsnumero"], out pwsnumero))
                throw new Exception("Param pwsnumero not valid");

            Trace.WriteLine("GetPagos: Parámetros correctos");

            UserFileSystem ufs = Code.ConexiónAlmacen.ObtenerUserFileSystem();

            Trace.WriteLine("GetPagos: UserFileSystem correcto");

            Record parentRecord = Record.LoadRecord(ufs, ConexiónAlmacen.ObtenerFileViewExpedientes(), pwsnumero);

            Trace.WriteLine("GetPagos: Se carga el registro padre con numero " + parentRecord.Number.ToString());

            RelationRecordProvider relationRecordProv = new RelationRecordProvider(ufs, ConexiónAlmacen.ObtenerRelationExpPagos(), parentRecord);

            Trace.WriteLine("GetPagos: Se carga la relacion " + relationRecordProv.Relation.ToString());
            
            pagos.page = page;

            if (relationRecordProv.GetTotalCount() > 0)
            {
                pagos.total = (int)Math.Ceiling((decimal)relationRecordProv.GetTotalCount() / (decimal)rows);
            }
            else
            {
                pagos.total = 0;
            }

            if (page > pagos.total) page = pagos.total;

            relationRecordProv.StartRecord = (ulong)(rows * page - rows);
  
            FileView fw = ConexiónAlmacen.ObtenerFileViewPagos();
            
            if(sidx == "ConceptoPago")
                field = "CONCEPTOPAGO";
            else if(sidx == "FechaPago")
                field = "FECHAPAGO";
            else if(sidx == "Ppal")
                field = "PPALPAGO";
            else if(sidx == "Interes")
                field = "INTERESPAGO";
            else if(sidx == "EntidadBancaria")
                field = "NUMCUENTABANCARIA";
            else field = null;
            
            if(!string.IsNullOrEmpty(field)){
                if(sord=="desc")
                    relationRecordProv.Order.Add(new PixelwareApi.File.Records.Searching.FieldOrder(fw.Fields[field], PixelwareApi.File.Records.Searching.FieldOrderDirection.Descending));
                else if(sord == "asc")
                    relationRecordProv.Order.Add(new PixelwareApi.File.Records.Searching.FieldOrder(fw.Fields[field], PixelwareApi.File.Records.Searching.FieldOrderDirection.Ascending));
            }

            //esto es lo unico que se quedaria
            List<RelationRecord> listRelationRecord = relationRecordProv.GetRelationRecords();

            pagos.records = listRelationRecord.Count;

            /**/
            Trace.WriteLine("GetPagos: Número de pagos: " + listRelationRecord.Count.ToString());           

            if (listRelationRecord.Count > 0)
            {
                Trace.WriteLine("GetPagos: Tiene pagos asociados");

                foreach (RelationRecord record in listRelationRecord){               

                    Trace.WriteLine("GetPagos: Procesando pago con numero " + record.ChildPart.Number.ToString());
                    Code.Pago pago = new Code.Pago();
                    pago.cell.Add("");
                    pago.cell.Add(record.ChildPart["CONCEPTOPAGO"].Value == null ? "" : record.ChildPart["CONCEPTOPAGO"].Value.ToString());
                    pago.cell.Add(record.ChildPart["FECHAPAGO"].Value == null ? "" : ((DateTime)record.ChildPart["FECHAPAGO"].Value).ToShortDateString());
                    pago.cell.Add(record.ChildPart["PPALPAGO"].Value == null ? "" : record.ChildPart["PPALPAGO"].Value.ToString().Replace(",","."));
                    pago.cell.Add(record.ChildPart["INTERESPAGO"].Value == null ? "" : record.ChildPart["INTERESPAGO"].Value.ToString().Replace(",","."));
                    pago.cell.Add("");
                    pago.cell.Add(record.ChildPart["NUMCUENTABANCARIA"].Value == null ? "" : record.ChildPart["NUMCUENTABANCARIA"].Value.ToString());
                    //Periodicidad
                    pago.cell.Add("");
                    pago.cell.Add("");
                    pago.cell.Add("");
                    //Indice de la ficha incrustados                    
                    pago.cell.Add(record.ChildPart.File.Node.Id.ToString());
                    //Campo incrustado en la ficha
                    pago.cell.Add("INCRUSCONCEPTOPAGO");
                    pago.cell.Add("SELENTIDADBRIA");
                    //Str de la navegacion
                    Navigation navigation = Navigation.LoadNavigation(ufs, record.ChildPart.File.Node.Id, record.ChildPart.Number);
                    pago.cell.Add(navigation.ToString());
                    //numero padre
                    pago.cell.Add(pwsnumero.ToString());
                    //numero hijo
                    pago.cell.Add(record.ChildPart.Number.ToString());
                    //Codigo tipo ingreso
                    pago.cell.Add(record.ChildPart["TIPOINGRESOPROPAG"].Value == null ? "" : record.ChildPart["TIPOINGRESOPROPAG"].Value.ToString());
                    //Conceptos Presupuestarios
                    pago.cell.Add(record.ChildPart["CONCPRESUP1"].Value == null ? "" : record.ChildPart["CONCPRESUP1"].Value.ToString());
                    pago.cell.Add(record.ChildPart["CONCPRESUP2"].Value == null ? "" : record.ChildPart["CONCPRESUP2"].Value.ToString());
                    pago.cell.Add(record.ChildPart["CONCPRESUP3"].Value == null ? "" : record.ChildPart["CONCPRESUP3"].Value.ToString());
                    pago.cell.Add(record.ChildPart["CONCPRESUP4"].Value == null ? "" : record.ChildPart["CONCPRESUP4"].Value.ToString());
                    //nuevo
                    pago.cell.Add("0");
                    pago.cell.Add(ObtenerUrl());
                    pago.cell.Add("");
                    //Codigo Expediente
                    pago.cell.Add(record.ChildPart["CODEXPED"].Value == null ? "" : record.ChildPart["CODEXPED"].Value.ToString());
                    pagos.rows.Add(pago);
                }
            }
            else { 
                // Creamos una fila "vacia"
                Trace.WriteLine("GetPagos: Se crea un pago nuevo");
                Code.Pago pago = new Code.Pago();
                pago.cell.Add("");
                // Concepto de Pago vacio
                pago.cell.Add("");
                //Fecha de Pago del expediente
                pago.cell.Add("");
                //Pago principal vacio
                pago.cell.Add("0");
                // Interes pago vacio
                pago.cell.Add("0");
                //Boton entidad bancaria vacio
                pago.cell.Add("");
                //Entidad Bancaria vacio
                pago.cell.Add(parentRecord.Values["ENTIDADFINANCIERA"].Value == null ? "" : parentRecord.Values["ENTIDADFINANCIERA"].Value.ToString());
                // Periodicidad vacio
                pago.cell.Add(ConfigurationManager.AppSettings["ValueopcionPeriodicidadCero"].ToString() + ":" + ConfigurationManager.AppSettings["LabelopcionPeriodicidadCero"].ToString());
                // boton nuevo vacio (se instancia en el grid)
                pago.cell.Add("");
                // boton eliminar vacio (se instancia en el grid) 
                pago.cell.Add("");
                // Los siguientes son campos ocultos en el grid, necesarios para dar de alta pagos.
                //Indice de la ficha incrustados
                //Indice del hijo
                pago.cell.Add(fw.Node.Id.ToString());
                //Campo incrustado en la ficha
                pago.cell.Add("INCRUSCONCEPTOPAGO");
                pago.cell.Add("SELENTIDADBRIA");
                //Str de la navegacion vacia (no tenemos el numero del hijo)                
                pago.cell.Add("");
                //numero padre
                pago.cell.Add(pwsnumero.ToString());
                //numero hijo vacio (aun no esta creado)
                pago.cell.Add("");
                //Codigo tipo ingreso propagado del padre
                pago.cell.Add(parentRecord.Values["TIPOINGRESO"].Value == null ? "" : parentRecord.Values["TIPOINGRESO"].Value.ToString());
                //Conceptos Presupuestarios
                pago.cell.Add("");
                pago.cell.Add("");
                pago.cell.Add("");
                pago.cell.Add("");
                //nuevo
                pago.cell.Add("1");
                pago.cell.Add(ObtenerUrl());
                pago.cell.Add("");
                pago.cell.Add(parentRecord.Values["CODEXPEDIENTE"].Value == null ? "" : parentRecord.Values["CODEXPEDIENTE"].Value.ToString());
                pagos.rows.Add(pago);
            }
            Trace.WriteLine("GetPagos: Finalizado OK");

            return pagos;
        }
    
        // To use HTTP GET, add [WebGet] attribute. (Default ResponseFormat is WebMessageFormat.Json)
        // To create an operation that returns XML,
        //     add [WebGet(ResponseFormat=WebMessageFormat.Xml)],
        //     and include the following line in the operation body:
        //         WebOperationContext.Current.OutgoingResponse.ContentType = "text/xml";
        [OperationContract]
        [WebGet]
        public void DeletePago()
        {
            decimal childNumber = 0, parentNumber = 0;

            if (!decimal.TryParse(HttpContext.Current.Request["childNumber"], out childNumber))
                throw new Exception("Param childNumber not valid");
            if (!decimal.TryParse(HttpContext.Current.Request["pwsnumero"], out parentNumber))
                throw new Exception("Param pwsnumero not valid");

            Trace.WriteLine("DeletePago: Parámetros correctos");
            UserFileSystem ufs = Code.ConexiónAlmacen.ObtenerUserFileSystem();

            Trace.WriteLine("DeletePago: UserFileSystem correcto");

            using (DbTransactionScope trans = ufs.Database.ComienzaTransaccion())
            {
                try
                {
                    Record childRecord = Record.LoadRecord(ufs, ConexiónAlmacen.ObtenerFileViewPagos(), childNumber);
                    Record parentRecord = Record.LoadRecord(ufs, ConexiónAlmacen.ObtenerFileViewExpedientes(), parentNumber);

                    Navigation nav = new Navigation(ufs, parentRecord);
                    RelationRecordProvider relationProvider = new RelationRecordProvider(ufs, ConexiónAlmacen.ObtenerRelationExpPagos(), parentRecord);
                    List<RelationRecord> listRelationRecord = relationProvider.GetRelationRecords();

                    RelationRecord relRec = listRelationRecord.Single(aux => aux.ChildPart.Number == childNumber);
                    nav.AddAssociatedRecord(relRec);

                    PixelwareApi.File.UserActions.RecordDeletion.RecordDeletion recordDeletion =
                        new PixelwareApi.File.UserActions.RecordDeletion.RecordDeletion(ufs, nav);
                    ActionInfo action = new ActionInfo("AltaPagos", "Eliminar Pago", "");
                    recordDeletion.DeleteSimple(action);

                    trans.Complete();
                }
                catch (Exception exc)
                {
                    Trace.WriteLine("DeletePago: Exception ->" + exc.ToString());
                    trans.Dispose();
                    throw exc;
                }
            }
            
        }

        // To use HTTP GET, add [WebGet] attribute. (Default ResponseFormat is WebMessageFormat.Json)
        // To create an operation that returns XML,
        //     add [WebGet(ResponseFormat=WebMessageFormat.Xml)],
        //     and include the following line in the operation body:
        //         WebOperationContext.Current.OutgoingResponse.ContentType = "text/xml";
        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        public Pago PropagarPago()
        {
            decimal pwsnumero = 0, ret = 0, ppal = 0, interespago = 0, periodicidad = 0;
            string ppalParam, interesPagoParam;
            Pago pago;

            if (!decimal.TryParse(HttpContext.Current.Request["pwsnumero"], out pwsnumero))
                throw new Exception("Param pwsnumero not valid");


            ppalParam = HttpContext.Current.Request["ppalpago"].Replace(".", ",");
            if (!decimal.TryParse(ppalParam, out ppal))
                throw new Exception("Param ppal not valid");

            interesPagoParam = HttpContext.Current.Request["interespago"].Replace(".", ",");
            if (!decimal.TryParse(interesPagoParam, out interespago))
                throw new Exception("Param interespago not valid");

            Trace.WriteLine("PropagarPagos: Parámetros correctos");

            UserFileSystem ufs = Code.ConexiónAlmacen.ObtenerUserFileSystem();

            Trace.WriteLine("PropagarPagos: UserFileSystem correcto");

            using (DbTransactionScope trans = ufs.Database.ComienzaTransaccion())
            {
                try
                {
                    Record parentRecord = Record.LoadRecord(ufs, ConexiónAlmacen.ObtenerFileViewExpedientes(), pwsnumero);
                    Trace.WriteLine("PropagarPago: Registro padre correcto");
                    FileView fw = ConexiónAlmacen.ObtenerFileViewPagos();

                    PixelwareApi.File.UserActions.RecordEdition recordEdition = new PixelwareApi.File.UserActions.RecordEdition(ufs);
                    RecordValuesList recordValues = recordEdition.PrepareNewRecordData(fw);

                    //Valores del registro
                    recordValues["CONCEPTOPAGO"] = new TextRecordValue(fw.Fields["CONCEPTOPAGO"], HttpContext.Current.Request["conceptopago"]);
                    recordValues["CONCPRESUP1"] = new TextRecordValue(fw.Fields["CONCPRESUP1"], HttpContext.Current.Request["cpresupuesto1"]);
                    recordValues["CONCPRESUP2"] = new TextRecordValue(fw.Fields["CONCPRESUP2"], HttpContext.Current.Request["cpresupuesto2"]);
                    recordValues["CONCPRESUP3"] = new TextRecordValue(fw.Fields["CONCPRESUP3"], HttpContext.Current.Request["cpresupuesto3"]);
                    recordValues["CONCPRESUP4"] = new TextRecordValue(fw.Fields["CONCPRESUP4"], HttpContext.Current.Request["cpresupuesto4"]);

                    if (HttpContext.Current.Request["Periodicidad"] != null && HttpContext.Current.Request["Periodicidad"] != "") {
                        if (!decimal.TryParse(HttpContext.Current.Request["Periodicidad"], out periodicidad))
                            periodicidad = 0;
                    }

                    DateTime fecha = Convert.ToDateTime(HttpContext.Current.Request["fechapago"]);
                    fecha = fecha.AddMonths((int)periodicidad);
                    recordValues["FECHAPAGO"] = new DateRecordValue(fw.Fields["FECHAPAGO"], fecha.ToString());                    
                    recordValues["PPALPAGO"] = new RealRecordValue(fw.Fields["PPALPAGO"], ppal);
                    recordValues["INTERESPAGO"] = new RealRecordValue(fw.Fields["INTERESPAGO"], interespago);
                    recordValues["NUMCUENTABANCARIA"] = new TextRecordValue(fw.Fields["NUMCUENTABANCARIA"], HttpContext.Current.Request["entidadbancaria"]);
                    
                    //Valores que el nuevo pago hereda del padre.
                    recordValues["ESTADOPROPAG"].Value = parentRecord.Values["ESTADO"].Value;
                    recordValues["TIPOINGRESOPROPAG"].Value = parentRecord.Values["TIPOINGRESO"].Value;
                    recordValues["NOMBREINTERPROPAG"].Value = parentRecord.Values["NOMBREINTER"].Value;
                    recordValues["NOMBRESOLICPROPAG"].Value = parentRecord.Values["NOMBRESOLIC"].Value;
                    recordValues["TIPOVIAVIVPROPAG"].Value = parentRecord.Values["TIPOVIAVIV"].Value;
                    recordValues["NOMBREVIAVIVPROPAG"].Value = parentRecord.Values["NOMBREVIAVIV"].Value;
                    recordValues["NUMVIAVIVPROPAG"].Value = parentRecord.Values["NUMVIAVIV"].Value;
                    recordValues["MUNICVIVPROPAG"].Value = parentRecord.Values["MUNICVIV"].Value;
                    recordValues["CODPOSVIVPROPAG"].Value = parentRecord.Values["CODPOSVIV"].Value;
                    recordValues["PROVVIVPROPAG"].Value = parentRecord.Values["PROVVIV"].Value;
                    recordValues["PISOVIVPROPAG"].Value = parentRecord.Values["PISOVIV"].Value;
                    recordValues["GRUPOPROPAG"].Value = parentRecord.Values["GRUPO"].Value;
                    recordValues["PUERTAVIVPROPAG"].Value = parentRecord.Values["PUERTAVIV"].Value;
                    recordValues["ESCALVIVPROPAG"].Value = parentRecord.Values["ESCALVIV"].Value;
                    recordValues["CODEXPED"].Value = parentRecord.Values["CODEXPEDIENTE"].Value;

                    ActionInfo action = new ActionInfo("AltaPagos", "Nuevo Pago", "");
                    Trace.WriteLine("PropagarPago: valores correctos");

                    Record childRecord = recordEdition.CreateRecord(fw, recordValues, action);
                    Trace.WriteLine("PropagarPago: Registro hijo creado correctamente");
                    RelationRecord.CreateRelationRecord(ufs, ConexiónAlmacen.ObtenerRelationExpPagos(), parentRecord, childRecord);
                    Trace.WriteLine("PropagarPago: Relación creada correctamente");
                    ret = childRecord.Number;
                    trans.Complete();
                    string date="";
                    if (fecha.Day <= 9)
                        date = "0" + fecha.Day;
                    else
                        date = fecha.Day.ToString();

                    if (fecha.Month<=9)
                        date+= "/0"+fecha.Month+"/"+fecha.Year;
                    else
                         date+="/"+fecha.Month+"/"+fecha.Year;

                    pago = new Pago(ret, pwsnumero, recordValues["CONCEPTOPAGO"].Value.ToString(), date, ppal,
                        recordValues["INTERESPAGO"].Value.ToString(), recordValues["NUMCUENTABANCARIA"].Value.ToString(), recordValues["TIPOINGRESOPROPAG"].Value.ToString(),
                        periodicidad, recordValues["CONCPRESUP1"].Value.ToString(), recordValues["CONCPRESUP2"].Value.ToString(), recordValues["CONCPRESUP3"].Value.ToString(),
                        recordValues["CONCPRESUP4"].Value.ToString(), recordValues["CODEXPED"].Value.ToString(), ObtenerUrl(), fw.Node.Id, fw.Node.Id.ToString() + "," + ret);
                }
                catch (Exception exc)
                {
                    Trace.WriteLine("PropagarPago: Exception ->" + exc.ToString());
                    trans.Dispose();
                    throw exc;
                }
            }

            return pago;
        }

        // To use HTTP GET, add [WebGet] attribute. (Default ResponseFormat is WebMessageFormat.Json)
        // To create an operation that returns XML,
        //     add [WebGet(ResponseFormat=WebMessageFormat.Xml)],
        //     and include the following line in the operation body:
        //         WebOperationContext.Current.OutgoingResponse.ContentType = "text/xml";
        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        public decimal  NuevoPago()
        {
            decimal pwsnumero = 0, ret = 0, ppal = 0, interespago = 0;            


            if (!decimal.TryParse(HttpContext.Current.Request["pwsnumero"], out pwsnumero))
                throw new Exception("Param pwsnumero not valid");

            if (HttpContext.Current.Request["ppalpago"] != "" && HttpContext.Current.Request["ppalpago"].ToString() != null)
            {
                string ppalParam = HttpContext.Current.Request["ppalpago"].Replace(".", ",");
                if (!decimal.TryParse(ppalParam, out ppal))
                    throw new Exception("Param ppal not valid");
            }
            else ppal = 0;

            if (HttpContext.Current.Request["interespago"] != "" && HttpContext.Current.Request["interespago"].ToString() != null)
            {
                string interesPagoParam = HttpContext.Current.Request["interespago"].Replace(".", ",");
                if (!decimal.TryParse(interesPagoParam, out interespago))
                    throw new Exception("Param interespago not valid");
            }
            else interespago = 0;

            
            Trace.WriteLine("NuevoPago: Parámetros correctos");

            UserFileSystem ufs = Code.ConexiónAlmacen.ObtenerUserFileSystem();

            Trace.WriteLine("NuevoPago: UserFileSystem correcto");

            using (DbTransactionScope trans = ufs.Database.ComienzaTransaccion())
            {
                try
                {
                    Record parentRecord = Record.LoadRecord(ufs, ConexiónAlmacen.ObtenerFileViewExpedientes(), pwsnumero);
                    Trace.WriteLine("NuevoPago: Registro padre correcto");
                    FileView fw = ConexiónAlmacen.ObtenerFileViewPagos();

                    PixelwareApi.File.UserActions.RecordEdition recordEdition = new PixelwareApi.File.UserActions.RecordEdition(ufs);
                    RecordValuesList recordValues = recordEdition.PrepareNewRecordData(fw);

                    //Valores del registro
                    if (HttpContext.Current.Request["conceptopago"] != null && HttpContext.Current.Request["conceptopago"] != "")
                        recordValues["CONCEPTOPAGO"] = new TextRecordValue(fw.Fields["CONCEPTOPAGO"], HttpContext.Current.Request["conceptopago"]);
                    else
                        throw new Exception("No se puede guardar el pago sin el campo CONCEPTOPAGO");

                    if (HttpContext.Current.Request["fechapago"] != null && HttpContext.Current.Request["fechapago"] != "")
                        recordValues["FECHAPAGO"] = new DateRecordValue(fw.Fields["FECHAPAGO"], HttpContext.Current.Request["fechapago"]);
                    else
                        throw new Exception("No se puede guardar el pago sin el campo FECHAPAGO");

                    recordValues["PPALPAGO"] = new RealRecordValue(fw.Fields["PPALPAGO"], ppal);
                    recordValues["INTERESPAGO"] = new RealRecordValue(fw.Fields["INTERESPAGO"], interespago);
                    if (HttpContext.Current.Request["entidadbancaria"] != null && HttpContext.Current.Request["entidadbancaria"] !="")
                        recordValues["NUMCUENTABANCARIA"] = new TextRecordValue(fw.Fields["NUMCUENTABANCARIA"], HttpContext.Current.Request["entidadbancaria"]);
                    else
                        throw new Exception("No se puede guardar el pago sin el campo NUMCUENTABANCARIA");
                    
                    //Valores que el nuevo pago hereda del padre.
                    recordValues["ESTADOPROPAG"].Value = parentRecord.Values["ESTADO"].Value;
                    recordValues["TIPOINGRESOPROPAG"].Value = parentRecord.Values["TIPOINGRESO"].Value;
                    recordValues["NOMBREINTERPROPAG"].Value = parentRecord.Values["NOMBREINTER"].Value;
                    recordValues["NOMBRESOLICPROPAG"].Value = parentRecord.Values["NOMBRESOLIC"].Value;
                    recordValues["TIPOVIAVIVPROPAG"].Value = parentRecord.Values["TIPOVIAVIV"].Value;
                    recordValues["NOMBREVIAVIVPROPAG"].Value = parentRecord.Values["NOMBREVIAVIV"].Value;
                    recordValues["NUMVIAVIVPROPAG"].Value = parentRecord.Values["NUMVIAVIV"].Value;
                    recordValues["MUNICVIVPROPAG"].Value = parentRecord.Values["MUNICVIV"].Value;
                    recordValues["CODPOSVIVPROPAG"].Value = parentRecord.Values["CODPOSVIV"].Value;
                    recordValues["PROVVIVPROPAG"].Value = parentRecord.Values["PROVVIV"].Value;
                    recordValues["PISOVIVPROPAG"].Value = parentRecord.Values["PISOVIV"].Value;
                    recordValues["GRUPOPROPAG"].Value = parentRecord.Values["GRUPO"].Value;
                    recordValues["PUERTAVIVPROPAG"].Value = parentRecord.Values["PUERTAVIV"].Value;
                    recordValues["ESCALVIVPROPAG"].Value = parentRecord.Values["ESCALVIV"].Value;
                    recordValues["CODEXPED"].Value = parentRecord.Values["CODEXPEDIENTE"].Value;

                    recordValues["CONCPRESUP1"] = new TextRecordValue(fw.Fields["CONCPRESUP1"], HttpContext.Current.Request["cpresupuesto1"]);
                    recordValues["CONCPRESUP2"] = new TextRecordValue(fw.Fields["CONCPRESUP2"], HttpContext.Current.Request["cpresupuesto2"]);
                    recordValues["CONCPRESUP3"] = new TextRecordValue(fw.Fields["CONCPRESUP3"], HttpContext.Current.Request["cpresupuesto3"]);
                    recordValues["CONCPRESUP4"] = new TextRecordValue(fw.Fields["CONCPRESUP4"], HttpContext.Current.Request["cpresupuesto4"]);

                    ActionInfo action = new ActionInfo("AltaPagos", "Nuevo Pago", "");
                    Trace.WriteLine("NuevoPago: valores correctos");

                    Record childRecord = recordEdition.CreateRecord(fw, recordValues, action);
                    Trace.WriteLine("NuevoPago: Registro hijo creado correctamente");
                    RelationRecord.CreateRelationRecord(ufs, ConexiónAlmacen.ObtenerRelationExpPagos(), parentRecord, childRecord);
                    Trace.WriteLine("NuevoPago: Relación creada correctamente");
                    ret = childRecord.Number;
                    trans.Complete();
                }
                catch (Exception exc)
                {
                    Trace.WriteLine("NuevoPago: Exception ->" + exc.ToString());
                    trans.Dispose();
                    throw exc;
                }
            }
           
            return ret;
        }

        public string ObtenerUrl()        
        {
            Trace.WriteLine("ObtenerUrl: Procesando");
            return (ConfigurationManager.AppSettings["urlSearch"].ToString());
        }

        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        public string ObtenerOpciones()
        {
            Trace.WriteLine("ObtenerOpciones: Procesando");
            return ConfigurationManager.AppSettings["ValueopcionPeriodicidadCero"].ToString() + ":" + ConfigurationManager.AppSettings["LabelopcionPeriodicidadCero"].ToString()
             + ";" + ConfigurationManager.AppSettings["ValueopcionPeriodicidadUno"].ToString() + ":" + ConfigurationManager.AppSettings["LabelopcionPeriodicidadUno"].ToString() + ";" +
             ConfigurationManager.AppSettings["ValueopcionPeriodicidadDos"].ToString() + ":" + ConfigurationManager.AppSettings["LabelopcionPeriodicidadDos"].ToString() + ";" +
             ConfigurationManager.AppSettings["ValueopcionPeriodicidadTres"].ToString() + ":" + ConfigurationManager.AppSettings["LabelopcionPeriodicidadTres"].ToString() + ";" +
             ConfigurationManager.AppSettings["ValueopcionPeriodicidadCuatro"].ToString() + ":" + ConfigurationManager.AppSettings["LabelopcionPeriodicidadCuatro"].ToString();
        }
    }
    [Serializable]
    public class Pago{

        public Pago(decimal childnumber, decimal parentnumber, string ConceptoPago, string FechaPago, decimal Ppal, string  Interes,
            string EntidadBancaria, string tipoingreso, decimal Periodicidad, string ConceptoPresupuestario1,
            string ConceptoPresupuestario2, string ConceptoPresupuestario3, string ConceptoPresupuestario4, string codexp,
            string urlSearch, decimal indice, string navegacion)
        {
            this.childnumber= childnumber;// msg,// ret
            this.parentnumber= parentnumber;//: row["parentnumber"], //indExpedientes
            this.ConceptoPago= ConceptoPago;//: row["ConceptoPago"], //recordValues["CONCEPTOPAGO"]
            this.FechaPago = FechaPago;//: fecha, //fecha.AddMonths((int)periodicidad).ToString()
            this.Ppal= Ppal;//: ppal// 
            this.Interes= Interes;//: interespago,// recordValues["INTERESPAGO"]
            this.EntidadBancaria= EntidadBancaria;//: recordValues["NUMCUENTABANCARIA"]
            this.tipoingreso= tipoingreso;//recordValues["TIPOINGRESOPROPAG"].Value , 
            this.Periodicidad =Periodicidad;//: row["Periodicidad"],//periodicidad 
            this.ConceptoPresupuestario1 = ConceptoPresupuestario1;//: row["ConceptoPresupuestario1"], //recordValues["CONCPRESUP1"] 
            this.ConceptoPresupuestario2=ConceptoPresupuestario2;//: row["ConceptoPresupuestario2"], //recordValues["CONCPRESUP2"] 
            this.ConceptoPresupuestario3=ConceptoPresupuestario3;//: row["ConceptoPresupuestario3"], //recordValues["CONCPRESUP3"] 
            this.ConceptoPresupuestario4=ConceptoPresupuestario4;//: row["ConceptoPresupuestario4"], //recordValues["CONCPRESUP4"] 
            this.codexp= codexp;//: row["codexp"], // recordValues["CODEXPED"].Value
            this.urlSearch=urlSearch;//: row["urlSearch"], //ObtenerUrl()
            this.indice=indice;//: row["indice"],//indPagos
            this.navegacion =navegacion;//: row["indice"] + ',' +  msg,//indPagos , ret
            
        }
        decimal childnumber;// msg,// ret
        decimal parentnumber;//: row["parentnumber"], //indExpedientes
        string ConceptoPago;//: row["ConceptoPago"], //recordValues["CONCEPTOPAGO"]
        string FechaPago;//: fecha, //fecha.AddMonths((int)periodicidad).ToString()
        decimal Ppal;//: ppal// 
        string Interes;//: interespago,// recordValues["INTERESPAGO"]
        string EntidadBancaria;//: recordValues["NUMCUENTABANCARIA"]
        string tipoingreso;//recordValues["TIPOINGRESOPROPAG"].Value , 
        decimal Periodicidad;//: row["Periodicidad"],//periodicidad 
        string ConceptoPresupuestario1;//: row["ConceptoPresupuestario1"], //recordValues["CONCPRESUP1"] 
        string ConceptoPresupuestario2;//: row["ConceptoPresupuestario2"], //recordValues["CONCPRESUP2"] 
        string ConceptoPresupuestario3;//: row["ConceptoPresupuestario3"], //recordValues["CONCPRESUP3"] 
        string ConceptoPresupuestario4;//: row["ConceptoPresupuestario4"], //recordValues["CONCPRESUP4"] 
        string codexp;//: row["codexp"], // recordValues["CODEXPED"].Value
        string urlSearch;//: row["urlSearch"], //ObtenerUrl()
        decimal indice;//: row["indice"],//indPagos
        string navegacion;//: row["indice"] + ',' +  msg,//indPagos , ret
    }
}
