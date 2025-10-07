<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PropagacionExpedientes.aspx.cs" Inherits="AltaPagos.PropagacionExpedientes" %>
<%@ Register TagPrefix="PixelwareApi" Namespace="PixelwareApi.Web" Assembly="PixelwareApi.Web" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>Propagación de expedientes</title>

    <link href="styles/Global.css" rel="stylesheet" type="text/css" />
    <link href="styles/Informe.css" rel="stylesheet" type="text/css" />
    <link href="styles/GridRegistros.css" rel="stylesheet" type="text/css" />

    <script type="text/javascript" src="Scripts/jquery-1.5.2.min.js"></script>
    <link href="styles/Propagacion.css" rel="stylesheet" type="text/css" />

</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div id="cabecera">
            Propagación de expedientes
        </div>
        <asp:MultiView ID="mainMultiView" runat="server">
            <asp:View ID="viewConfirm" runat="server">
                <div class="ConfirmPropagacionDiv">
                    <asp:Label CssClass="marginTextOperacionPropagar" ID="labelConfirm" runat="server" Text="Se va a proceder a propagar los expedientes. Para confirmar la operación pulse en continuar."></asp:Label>
                    <div class="propagarDivButtonsForm">
                        <asp:LinkButton ID="BtnContinuar" runat="server" Text="Continuar" CssClass="BotonActivo" OnClientClick="return onBtnContinuar();" onclick="linkContinuar_Click" ></asp:LinkButton>
                        <a class="BotonActivo" href="javascript:window.close()">Cancelar</a>
                    </div>
                </div>
                

                <div runat="server" id="ResumenExpedientesDiv">
                    <p class="marginTextProgagacion">Resumen de los expedientes seleccionados según el tipo de ingreso:</p>
                    <PixelwareApi:ExtendedGridView ID="gridViewTipoExpedientes" runat="server" 
                        AutoGenerateColumns="False" DataKeyNames="Type" 
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
                            <AlwaysShowHeader Enable="false" ShowEmptyRow="true" EmptyMessage="&lt;ML&gt;No se ha definido ningún expediente .&lt;/ML&gt;" />
                        </ExtendedProperties>
                        <Columns>
                            <asp:BoundField DataField="Type" HeaderText="Tipo de expediente">
                            </asp:BoundField>
                            <asp:BoundField DataField="Counter"  ItemStyle-CssClass="expedientsCount" HeaderStyle-CssClass="headerNoWrap"  HeaderText="Número de expedientes">
                            </asp:BoundField>
                        </Columns>
                    </PixelwareApi:ExtendedGridView>
                </div>

                <div class="PropagacionAllExpedientesDiv">
                    <p class="marginTextProgagacion">Los expedientes seleccionados son:</p>
                    <PixelwareApi:ExtendedGridView ID="gridViewExpedientes" runat="server" 
                        AutoGenerateColumns="False" DataKeyNames="PWSNumero" 
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
                            <AlwaysShowHeader Enable="false" ShowEmptyRow="true" EmptyMessage="&lt;ML&gt;No se ha definido ningún expediente .&lt;/ML&gt;" />
                        </ExtendedProperties>
                        <Columns>
                            <asp:TemplateField HeaderText="Fecha de alta">
                                <ItemTemplate>
                                    <%# string.Format("{0:dd/MM/yyyy}", Eval("FechaAlta")) %>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="ReferenciaExp" HeaderText="Referencia">
                            </asp:BoundField>
                            <asp:BoundField DataField="NumDocIdentifInter" HeaderText="NIF">
                            </asp:BoundField>
                            <asp:BoundField DataField="NombreInter" HeaderText="Nombre inter.">
                            </asp:BoundField>
                            <asp:TemplateField HeaderText="Fecha de liquidación">
                                <ItemTemplate>
                                    <%# string.Format("{0:dd/MM/yyyy}", Eval("FechaLiquid")) %>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="ImporteTotal" HeaderText="Importe total">
                            </asp:BoundField>
                        </Columns>
                    </PixelwareApi:ExtendedGridView>
                </div>
            </asp:View>
            <asp:View ID="viewWait" runat="server">
                <div class="title progress">Propagación anual de expedientes en progreso. Por favor, espere...</div>
                <asp:LinkButton ID="linkGenerar" runat="server" Text="" style="display:none"
                        onclick="linkGenerar_Click" ></asp:LinkButton>
            </asp:View>
            <asp:View ID="viewSuccess" runat="server">
                <div class="title success">Resultado de la propagación</div>

                <div class="subtitle"><asp:Literal ID="literalNumRegistros" runat="server"></asp:Literal></div>
                <asp:Panel ID="panelWarnings" CssClass="resultPropagacionGrid" runat="server" Visible="false">
                    <div class="subtitle">Detalles de la propagación: </div>
                    <asp:Table ID="tableIncidencias" runat="server" CellPadding="0" CellSpacing="0">
                        <asp:TableHeaderRow>
                            <asp:TableHeaderCell CssClass="cell1">Referencia Expediente Antiguo</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell2">Referencia Expediente Nuevo</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell2">Resultado</asp:TableHeaderCell>
                        </asp:TableHeaderRow>
                    </asp:Table>
                </asp:Panel>
            </asp:View>
            <asp:View ID="viewError" runat="server">
                <div class="title error">No se han podido propagar los expedientes.</div>
                <div class="subtitle"><asp:Literal ID="literalError" runat="server"></asp:Literal></div>
            </asp:View>
        </asp:MultiView>
    </div>
    </form>
</body>
</html>
