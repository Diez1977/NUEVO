<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SicerNotificaciones.aspx.cs" Inherits="AltaPagos.SicerNotificaciones" %>
<%@ Register TagPrefix="PixelwareApi" Namespace="PixelwareApi.Web" Assembly="PixelwareApi.Web" %>

<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head" runat="server">
    <title>Generador de remesas para notificaciones a Sicer</title>
    <link href="styles/Global.css" rel="stylesheet" type="text/css" />
    <link href="styles/Informe.css" rel="stylesheet" type="text/css" />
    <link href="styles/GridRegistros.css" rel="stylesheet" type="text/css" />
    <link type="text/css" href="Styles/jquery-ui-1.8.4.css" rel="stylesheet" />

    <script src="scripts/jquery-1.5.2.min.js" type="text/javascript"></script>
    <script src="scripts/jquery-ui-1.8.4.min.js" type="text/javascript"></script>
    <script src="scripts/jquery.datePicker.js" type="text/javascript"></script>
    <script src="scripts/customDatePicker.js" type="text/javascript"></script>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div id="cabecera">
            Generación de remesa de notificaciones para Sicer
        </div>
        <br />
        <asp:MultiView ID="multiView" runat="server">
            <asp:View ID="viewConfirm" runat="server">
                <div>
                    <asp:Label ID="labelWarning" CssClass="subtitle" runat="server" Text="Se va a proceder a generar la remesa de notificaciones de las siguientes comunicaciones a Sicer, ¿continuar?"></asp:Label>
                </div>
                <br />
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
                        <asp:BoundField DataField="NOMBREINTERPROPAG" HeaderText="Nombre y apellidos">
                        </asp:BoundField>
                        <asp:BoundField DataField="NUMDOCIDENTIFINTERPROPAG" HeaderText="NIF">
                        </asp:BoundField>
                        <asp:BoundField DataField="NUMJUSTIFINTECO" HeaderText="Núm. justif. INTECO">
                        </asp:BoundField>
                        <asp:BoundField DataField="IMPORTETOTAL" HeaderText="Importe total">
                        </asp:BoundField>
                        <asp:BoundField DataField="APREMIO" HeaderText="Apremio">
                        </asp:BoundField>
                        <asp:BoundField DataField="MOTIVOEXPEDICION" HeaderText="Motivo expedición">
                        </asp:BoundField>
                        <asp:BoundField DataField="INCIDENCIA" HeaderText="Incidencia">
                        </asp:BoundField>
                    </Columns>
                </PixelwareApi:ExtendedGridView>
                <div class="subtitle info">
                    <br />
                    <asp:Label ID="labelFechaEnvio" runat="server" Text="Introduzca la fecha de envío"></asp:Label>
                    <br />
                    <asp:TextBox ID="textBoxFechaEnvio" runat="server" Text=""></asp:TextBox>
                    <br />
                    <br />
                </div>
                <div style="padding-left:38px;padding-bottom:20px;">
                    <div style="padding-bottom:10px;">
                        <span id="erroresSicer" runat="server" class="title error">No es posible enviar la remesa ya que la conexión con el servicio SICER/sFTP Sicer está caída. Vuelva a intentarlo en unos minutos y si el problema persiste consulte con su administrador.</span>
                    </div>
                    <asp:LinkButton ID="LinkButton1" runat="server" Text="Continuar" CssClass="BotonActivo" onclick="linkContinuar_Click" ></asp:LinkButton>
                    <a class="BotonActivo" href="javascript:window.close()">Cancelar</a>
                </div>
            </asp:View>

            <asp:View ID="viewWait" runat="server">
                <div class="title progress">Generando remesa. Por favor, espere...</div>
                <asp:LinkButton ID="linkGenerar" runat="server" Text="" style="display:none" onclick="linkGenerar_Click" ></asp:LinkButton>
            </asp:View>

            <asp:View ID="viewSuccess" runat="server">
                <div class="title success">Se ha generado la remesa correctamente.</div>
                <div class="subtitle"><asp:Literal ID="literalNumRegistros" runat="server"></asp:Literal></div>
                <asp:Panel ID="panelWarnings" runat="server" Visible="false">
                    <div class="subtitle">El estado de la generación de la remesa es el siguiente:</div>
                    <asp:Table ID="tableIncidencias" runat="server" CellPadding="0" CellSpacing="0">
                        <asp:TableHeaderRow>
                            <asp:TableHeaderCell CssClass="cell1">Ref. Expediente</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell2">Cod. Remesa Creada</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell1">Id. Com. en Remesa</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell1">Código NT</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell2">Resultado</asp:TableHeaderCell>
                        </asp:TableHeaderRow>
                    </asp:Table>
                </asp:Panel>
            </asp:View>
            <asp:View ID="viewError" runat="server">
                <div class="title error">No se ha podido generar la remesa.</div>
                <div class="subtitle"><asp:Literal ID="literalError" runat="server"></asp:Literal></div>
            </asp:View>
        </asp:MultiView>
    </div>
    </form>
</body>
</html>
