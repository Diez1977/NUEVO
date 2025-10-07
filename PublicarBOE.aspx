<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PublicarBOE.aspx.cs" Inherits="AltaPagos.PublicarBOE" EnableEventValidation="false"  %>
<%@ Register TagPrefix="PixelwareApi" Namespace="PixelwareApi.Web" Assembly="PixelwareApi.Web" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>Actualizacion masiva de comunicaciones pendientes de publicar en BOE (se filtrará por la fecha de envío)</title>
    <link href="styles/Global.css" rel="stylesheet" type="text/css" />
    <link href="styles/Informe.css" rel="stylesheet" type="text/css" />
    <link href="styles/GridRegistros.css" rel="stylesheet" type="text/css" />
    <link type="text/css" href="Styles/jquery-ui-1.8.4.css" rel="stylesheet" />
    <link href="styles/PublicarBOE.css" rel="stylesheet" />

    <script src="scripts/jquery-1.5.2.min.js" type="text/javascript"></script>
    <script src="scripts/jquery-ui-1.8.4.min.js" type="text/javascript"></script>
    <script src="scripts/jquery.datePicker.js" type="text/javascript"></script>
    <script src="scripts/customDatePicker.js" type="text/javascript"></script>
    
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div id="cabecera">
            Actualizacion masiva de comunicaciones pendientes de publicar en BOE 
        </div>
        <asp:MultiView ID="mainMultiView" runat="server">
            <asp:View ID="viewContinuar" runat="server">
                <div class="subtitle info"><asp:Label ID="labelContinuar" runat="server" Text="Se va a proceder a actualizar masivamente las {0} comunicaciones pendientes de publicar en BOE. ¿Desea continuar con la operación?"></asp:Label></div>
                <div class="FormFechaPublicacion">
                    <asp:Label ID="labelFechaPublicacion" runat="server" Text="Introduzca la fecha de publicación en BOE:"></asp:Label>
                    <br />
                    <asp:TextBox ID="textBoxFechaPublicacion" runat="server" Text=""></asp:TextBox>
                    <br />
                </div>
                <div style="padding-left:38px;padding-bottom:20px;">
	                <asp:LinkButton ID="linkContinuar" runat="server" Text="Continuar" CssClass="BotonActivo" onclick="linkContinuar_Click" ></asp:LinkButton>
	                <a class="BotonActivo" href="javascript:window.close()">Cancelar</a>
                </div>
                <div>
	                <PixelwareApi:ExtendedGridView ID="gridViewComunicaciones" runat="server" 
		                AutoGenerateColumns="False" DataKeyNames="PWSNUMERO" 
		                AllowPaging="False" AllowSorting="False" PageSize="10" 
		                CssClass="GridRegistros" EnableSortingAndPagingCallbacks="false" >
		                <HeaderStyle CssClass="Grid_Head RowHandCursor" />
			                <SelectedRowStyle CssClass="Grid_SelectedRow RowHandCursor" />
			                <RowStyle CssClass="Grid_Result RowHandCursor" />
			                <AlternatingRowStyle CssClass="Grid_AlternatingRow RowHandCursor" />
			                <ExtendedProperties AddServerIds="true">
                            <ClientRowSelection OnClientClick="onRowSelection" Enable="false" />
                            <CheckBoxSelection Enable="false" EnableCheckAllBox="false" ColumnPosition="0" PersistSelection="false" />
                            <MultiColumnSorting Enable="false" ShowImages="true" SortAscImageUrl="Images/sortascending.gif" SortDescImageUrl="Images/sortdescending.gif"  />
			                <FilterRow Enable="false" CssClass="GridFilterRow" />
			                <AlwaysShowHeader Enable="false" ShowEmptyRow="true" EmptyMessage="&lt;ML&gt;No se ha definido ninguna comunicación.&lt;/ML&gt;" />
		                    </ExtendedProperties>
		                <Columns>                            
			                <asp:BoundField DataField="FECHAALTAEXP" HeaderText="Fecha alta exp">
			                </asp:BoundField>
                            <asp:BoundField DataField="REFERENCIAEXP" HeaderText="Referencia exp">
                            </asp:BoundField>
                            <asp:BoundField DataField="FECHACOMUNIC" HeaderText="Fec. impresión">
                            </asp:BoundField>
                            <asp:BoundField DataField="ESTADOCOMUNIC" HeaderText="Estado comunic.">
                            </asp:BoundField>
                            <asp:BoundField DataField="NUMJUSTIFINTECO" HeaderText="Núm. justif. INTECO">
                            </asp:BoundField>
                            <asp:BoundField DataField="ESTADOINTECO" HeaderText="Estado INTECO">
                            </asp:BoundField>
                            <asp:BoundField DataField="IMPORTETOTAL" HeaderText="Importe total">
                            </asp:BoundField>
                            <asp:BoundField DataField="FECHAENVIOBOE" HeaderText="Fecha envío BOE">
                            </asp:BoundField>
		                </Columns>
	                </PixelwareApi:ExtendedGridView>
                </div>
            </asp:View>
            <asp:View ID="viewEspera" runat="server">
                <div class="title progress">Actualización masiva de comunicaciones en progreso. Por favor, espere...</div>
	            <asp:LinkButton ID="linkActualizar" runat="server" Text="" style="display:none" onclick="linkActualizar_Click" ></asp:LinkButton>
            </asp:View>
            <asp:View ID="viewExito" runat="server">
                <div class="title success">Se han generado todas las actualizaciones correctamente.</div>
	            <div class="subtitle"><asp:Literal ID="literalNumRegistros" runat="server"></asp:Literal></div>
	            <asp:Panel ID="panelWarnings" runat="server" Visible="false">
		            <div class="subtitle">Resumen de las operaciones:</div>
		            <asp:Table ID="tableIncidencias" runat="server" CellPadding="0" CellSpacing="0">
			            <asp:TableHeaderRow>
				            <asp:TableHeaderCell CssClass="cell2">Fecha alta exp.</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell1">Ref. expediente</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell2">Fecha comunicación</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell1">Estado comunicación</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell2">Núm. justif. Inteco</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell1">Estado Inteco</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell2">Importe Total</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell2">Fecha envío BOE</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell2">Fecha publicación BOE</asp:TableHeaderCell>
				            <asp:TableHeaderCell CssClass="cell2">Resultado</asp:TableHeaderCell>
			            </asp:TableHeaderRow>
		            </asp:Table>
	            </asp:Panel>
            </asp:View>
            <asp:View ID="viewError" runat="server">
                <div class="title error">No se han podido actualizar las comunicaciones.</div>
	            <div class="subtitle info"><asp:Literal ID="literalError" runat="server"></asp:Literal></div>
                <br />
                <div style="text-align:center;"><asp:LinkButton ID="linkVolver" runat="server" Text="Volver" CssClass="BotonActivo" onclick="linkVolver_Click" ></asp:LinkButton></div>
            </asp:View>
        </asp:MultiView>
    </div>
    </form>
</body>
</html>