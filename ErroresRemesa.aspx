<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ErroresRemesa.aspx.cs" Inherits="AltaPagos.ErroresRemesa" %>
<%@ Register TagPrefix="PixelwareApi" Namespace="PixelwareApi.Web" Assembly="PixelwareApi.Web" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head" runat="server">
<meta http-equiv="Content-Type" content="text/html"/>
    <title>Detalle de errores de remesa</title>
    <link href="styles/Global.css" rel="stylesheet" type="text/css" />
    <link href="styles/Informe.css" rel="stylesheet" type="text/css" />
    <link href="styles/GridRegistros.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <div id="cabecera">
                <ML>Detalle de errores de remesa</ML>
            </div>
            <div class="subtitle info">
                <asp:Label ID="labelFichero" runat="server" Text="<ML>Detalle de errores de remesa</ML>"></asp:Label>
                <PixelwareApi:ExtendedGridView ID="gridViewRemesas" runat="server" 
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
                        <AlwaysShowHeader Enable="false" ShowEmptyRow="true" EmptyMessage="&lt;ML&gt;No se ha definido ningúna comunicación .&lt;/ML&gt;" />
                    </ExtendedProperties>
                    <Columns>
                        <asp:BoundField DataField="REFERENCIAEXP" HeaderText="<ML>Referencia Exp.</ML>">
                        </asp:BoundField>
                        <asp:BoundField DataField="CODCOMUNICACION" HeaderText="<ML>Cód. Comunicación</ML>">
                        </asp:BoundField>
                        <asp:BoundField DataField="IDERRORSICER" HeaderText="<ML>Id error SICER</ML>">
                        </asp:BoundField>
                        <asp:BoundField DataField="DESCERRORSICER" HeaderText="<ML>Descrip. error SICER</ML>">
                        </asp:BoundField>
                    </Columns>
                </PixelwareApi:ExtendedGridView>
            </div>
        </div>
    </form>
</body>
</html>
