<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ContraerMasivo.aspx.cs" Inherits="AltaPagos.ContraerMasivo" %>
<%@ Register TagPrefix="PixelwareApi" Namespace="PixelwareApi.Web" Assembly="PixelwareApi.Web" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Contraer masivo</title>
    <link href="styles/Global.css" rel="stylesheet" type="text/css" />
    <link href="styles/Informe.css" rel="stylesheet" type="text/css" />
    <link href="styles/GridRegistros.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div id="cabecera">
            Contracción masiva de comunicaciones
        </div>
        <asp:MultiView ID="mainMultiView" runat="server">
        <asp:View ID="viewConfirm" runat="server">
                <div class="subtitle info"><asp:Label ID="labelConfirm" runat="server" Text="Se va a proceder a contraer masivamente las siguientes {0} comunicaciones. ¿Desea continuar con la operación?"></asp:Label></div>
                <div style="padding-left:38px;padding-bottom:20px;">
                    <asp:LinkButton ID="LinkButton1" runat="server" Text="Continuar" CssClass="BotonActivo" onclick="linkContinuar_Click" ></asp:LinkButton>
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
                            <asp:BoundField DataField="FECHAREGSALIDA" HeaderText="Fec. reg. salida">
                            </asp:BoundField>
                        </Columns>
                    </PixelwareApi:ExtendedGridView>
                </div>
            </asp:View>
            <asp:View ID="viewWait" runat="server">
                <div class="title progress">Contracción masiva de comunicaciones en progreso. Por favor, espere...</div>
                <asp:LinkButton ID="linkGenerar" runat="server" Text="Generar fichero" 
                    onclick="linkGenerar_Click" style="display:none" ></asp:LinkButton>
            </asp:View>
            <asp:View ID="viewSuccess" runat="server">
                <div class="title success">Se han generado todas las contracciones correctamente.</div>
                <div class="subtitle"><asp:Literal ID="literalNumRegistros" runat="server"></asp:Literal></div>
                <asp:Panel ID="panelWarnings" runat="server" Visible="false">
                    <div class="subtitle">Durante la generación de las contracciones se encontraron las siguientes incidencias:</div>
                    <asp:Table ID="tableIncidencias" runat="server" CellPadding="0" CellSpacing="0">
                        <asp:TableHeaderRow>
                            <asp:TableHeaderCell CssClass="cell1">Ref. del Expediente</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell2">Cod. Comunicación</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell3">Incidencia</asp:TableHeaderCell>
                        </asp:TableHeaderRow>
                    </asp:Table>
                </asp:Panel>
            </asp:View>
            <asp:View ID="viewError" runat="server">
                <div class="title error">No se han podido generar la contracción de las comunicaciones.</div>
                <div class="subtitle"><asp:Literal ID="literalError" runat="server"></asp:Literal></div>
            </asp:View>
        </asp:MultiView>
    </div>
    </form>
</body>
</html>
