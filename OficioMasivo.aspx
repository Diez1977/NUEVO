<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="OficioMasivo.aspx.cs" Inherits="AltaPagos.OficioMasivo" EnableEventValidation="false"  %>
<%@ Register TagPrefix="PixelwareApi" Namespace="PixelwareApi.Web" Assembly="PixelwareApi.Web" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Oficio masivo</title>
    <link href="styles/Global.css" rel="stylesheet" type="text/css" />
    <link href="styles/Informe.css" rel="stylesheet" type="text/css" />
    <link href="styles/GridRegistros.css" rel="stylesheet" type="text/css" />
    <style type="text/css">
        table { width: auto }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div id="cabecera">
            Generación masiva de oficios
        </div>
        <asp:MultiView ID="mainMultiView" runat="server">
            <asp:View ID="viewConfirm" runat="server">
                <div class="subtitle info"><asp:Label ID="labelConfirm" runat="server" Text="Se va a proceder a generar de forma masiva el oficio de las {0} comunicaciones siguientes. ¿Desea continuar con la operación?"></asp:Label></div>
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
                                <ItemStyle CssClass="GridNoWrap" />
                            </asp:BoundField>
                            <asp:BoundField DataField="FECHALIQUID" HeaderText="Fec. liquidación">
                            </asp:BoundField>
                            <asp:BoundField DataField="NOMBREINTERPROPAG" HeaderText="Nombre y apellidos">
                            </asp:BoundField>
                            <asp:BoundField DataField="NUMDOCIDENTIFINTERPROPAG" HeaderText="NIF">
                            </asp:BoundField>
                            <asp:BoundField DataField="MOTIVOEXPEDICION" HeaderText="Motivo expedición">
                            </asp:BoundField>
                            <asp:BoundField DataField="IMPORTETOTAL" HeaderText="Importe total">
                                <ItemStyle CssClass="GridNoWrap" />
                            </asp:BoundField>
                            <asp:BoundField DataField="MUNICVIVPROPAG" HeaderText="Municipio">
                            </asp:BoundField>
                        </Columns>
                    </PixelwareApi:ExtendedGridView>
                </div>
            </asp:View>
            <asp:View ID="viewWait" runat="server">
                <div class="title progress">Generación masiva de oficios en progreso. Por favor, espere...</div>
                <asp:LinkButton ID="linkGenerar" runat="server" Text="" style="display:none"
                        onclick="linkGenerar_Click" ></asp:LinkButton>
            </asp:View>
            <asp:View ID="viewSuccess" runat="server">
                <div class="title success">Se han generado todos los oficios correctamente.</div>
                <div class="subtitle"><asp:Literal ID="literalNumRegistros" runat="server"></asp:Literal></div>
                <asp:Panel ID="panelWarnings" runat="server" Visible="false">
                    <div class="subtitle" id="subtitleLabel" runat="server">Durante la generación de los oficios se encontraron las siguientes incidencias:</div>
                    <PixelwareApi:ExtendedGridView ID="gridResultado" runat="server" 
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
                                <ItemStyle CssClass="GridNoWrap" />
                            </asp:BoundField>
                            <asp:BoundField DataField="FECHALIQUID" HeaderText="Fec. liquidación">
                            </asp:BoundField>
                            <asp:BoundField DataField="NOMBREINTERPROPAG" HeaderText="Nombre y apellidos">
                            </asp:BoundField>
                            <asp:BoundField DataField="NUMDOCIDENTIFINTERPROPAG" HeaderText="NIF">
                            </asp:BoundField>
                            <asp:BoundField DataField="MOTIVOEXPEDICION" HeaderText="Motivo expedición">
                            </asp:BoundField>
                            <asp:BoundField DataField="IMPORTETOTAL" HeaderText="Importe total">
                                <ItemStyle CssClass="GridNoWrap" />
                            </asp:BoundField>
                            <asp:BoundField DataField="MUNICVIVPROPAG" HeaderText="Municipio">
                            </asp:BoundField>
                            <asp:BoundField DataField="ESTADOPROPAG" HeaderText="Estado">
                            </asp:BoundField>
                            <asp:BoundField DataField="RESULTADO" HeaderText="Resultado de la operación">
                            </asp:BoundField>
                        </Columns>
                    </PixelwareApi:ExtendedGridView>
                </asp:Panel>
            </asp:View>
            <asp:View ID="viewError" runat="server">
                <div class="title error">No se han podido generar los oficios.</div>
                <div class="subtitle"><asp:Literal ID="literalError" runat="server"></asp:Literal></div>
            </asp:View>
        </asp:MultiView>
    </div>
    </form>
</body>
</html>
