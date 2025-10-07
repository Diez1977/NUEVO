<%@ Page Language="C#" AutoEventWireup="True" CodeBehind="AltaPagos.aspx.cs" Inherits="AltaPagos.AltaPagos" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">
<head>
<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
<title>Ventana Pagos</title>
<link type="text/css" href="styles/Site.css"  rel="stylesheet"/>
<link type="text/css" href="styles/ui.jqgrid.css"  rel="stylesheet"/>
<link type="text/css" href="Styles/jquery-ui-1.8.4.css" rel="stylesheet" />
<link type="text/css" href="styles/datePicker.css" rel="stylesheet" /> 
<link type="text/css" href="styles/jquery.alerts.css" rel="stylesheet" /> 
<style>
html, body {
    margin: 0;
    padding: 0;
    font-size: 85%;
}
</style>
<script src="scripts/Custom.js" type="text/javascript"></script>
<script src="scripts/jquery-1.5.2.min.js" type="text/javascript"></script>
<script src="scripts/grid.locale-es.js" type="text/javascript"></script>
<script src="scripts/jquery.jqGrid.min.js" type="text/javascript"></script>
<script src="scripts/jquery.datePicker.js" type="text/javascript"></script>
<script src="scripts/jquery-ui-1.8.4.min.js" type="text/javascript"></script>
<script src="scripts/date.js" type="text/javascript"></script>
<script src="scripts/json2.js" type="text/javascript"></script>
<script src="scripts/jquery.alerts.js" type="text/javascript"></script>
<script src="scripts/Formularios.js" type="text/javascript"></script>
<script src="scripts/AltaPagos.js" type="text/javascript"></script>

</head>
<body>
<div id='tableDesplDiv' class='tableDesplDiv'>    

    <input type="hidden" id="PIXEL_INFO_TIPOINGRESOPROPAG" name="PIXEL_INFO_TIPOINGRESOPROPAG" value=""/> 
    <input type="hidden" id="PIXEL_INFO_CONCEPTOPAGO" name="PIXEL_INFO_CONCEPTOPAGO" value=""/>     
    <input type="hidden" id="PIXEL_INFO_CONCPRESUP1" name="PIXEL_INFO_CONCPRESUP1" value=""/>     
    <input type="hidden" id="PIXEL_INFO_CONCPRESUP2" name="PIXEL_INFO_CONCPRESUP2" value=""/>     
    <input type="hidden" id="PIXEL_INFO_CONCPRESUP3" name="PIXEL_INFO_CONCPRESUP3" value=""/>     
    <input type="hidden" id="PIXEL_INFO_CONCPRESUP4" name="PIXEL_INFO_CONCPRESUP4" value=""/>     
    <input type="hidden" id="PIXEL_INFO_NUMCUENTABANCARIA" name="PIXEL_INFO_NUMCUENTABANCARIA" value=""/>
    <input type="hidden" id="NUM_ROW_CP" name="NUM_ROW_CP" value=""/>         
    <input type="hidden" id="NUM_ROW_EB" name="NUM_ROW_EB" value=""/>         
    <input type="hidden" id="NUM_ROW_PROPAGAR" name="NUM_ROW_PROPAGAR" value=""/>     
    <input type="hidden" id="NUM_ROW_NUEVO" name="NUM_ROW_NUEVO" value=""/>     
    <input type="hidden" id="VALUE_PROPAGAR" name="VALUE_PROPAGAR" value=""/>     
    <input type="hidden" id="UPDATE_CAMPOS_CONCEPTOPAGO" name="UPDATE_CAMPOS_CONCEPTOPAGO" value=""/>     
    <input type="hidden" id="UPDATE_EBANCARIA" name="UPDATE_EBANCARIA" value=""/>         
    <input type="hidden" id="URL" name="URL" value=""/>     

    <div id='Cabecera' class='Cabecera'>    
        <div id='CabeceraLeft' class='CabeceraLeft'><img src="Images/IMG_Cliente.gif" alt=""/></div>
        <div id='CabeceraRight' class='CabeceraRight'>            
        </div>
    </div>
    <div id='Grid' class='Grid'>
        <table id="grid"></table>
        <div id="pager"></div>

    </div>    
    <div id='buttonValidar' class='bValidar'>
        <input type="button" value="Validar Pagos" onclick="salvarestado(0,0)" />
    </div>
</div>

</body>
</html>
