<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="DetallesRemesa.aspx.cs" Inherits="AltaPagos.DetallesRemesa" %>
<%@ Register TagPrefix="PixelwareApi" Namespace="PixelwareApi.Web" Assembly="PixelwareApi.Web" %>

<!DOCTYPE HTML>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head" runat="server">
<meta http-equiv="Content-Type" content="text/html"/>
    <title>Detalle de comunicaciones de remesa</title>
    <link href="styles/Global.css" rel="stylesheet" type="text/css" />
    <link type="text/css" href="styles/ui.jqgrid.css"  rel="stylesheet"/>
    <link type="text/css" href="Styles/jquery-ui-1.8.4.css" rel="stylesheet" />
    <link href="Scripts/msg/jquery.msg.css" rel="stylesheet" type="text/css" />
    <link href="styles/Informe.css" rel="stylesheet" type="text/css" />
    <link href="styles/GridRegistros.css" rel="stylesheet" type="text/css" />

    <script type="text/javascript" src="Scripts/jquery-1.5.2.min.js"></script>
    <script src="Scripts/msg/jquery.center.min.js" type="text/javascript"></script>
    <script src="Scripts/msg/jquery.msg.js" type="text/javascript"></script>
    <script type="text/javascript" src="Scripts/jquery-ui-1.8.4.min.js"></script>

    <script type="text/javascript">
        $(function () {
            $("#divConfirmacion").dialog({
                autoOpen: false,
                height: 330,
                width: 500,
                modal: true
            });

            $("#divConfirmacion").parent().appendTo($("form:first"));
            $("#divConfirmacion").parent().css('z-index', 1000);
        });

        function abrirPanelConfirmacion(value) {
            $("#filaImpresion").val("1");

            $("#divConfirmacion").dialog({
                buttons: {
                    "Generar": function () {
	                        $(this).dialog("close");

	                        $("#descargarEtiquetas").attr('href', 'ImprimirCodigosSicer.aspx?modo=1&pwsNumeroComunicacion=' + value + '&filaComienzo=' + $("#filaImpresion option:selected").val());
	                        $("#descargarEtiquetas").click(function () {
	                            return true;
	                        });
	                        $("#descargarEtiquetas")[0].click();
                    },
                    "Cancelar": function () {
	                        $(this).dialog("close");
	                    }
	                }
            });

            $("#divConfirmacion").dialog("open");
        }

    </script>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div id="cabecera">
            Detalle de las comunicaciones de una remesa
        </div>
        <br />
        <asp:MultiView ID="multiView" runat="server">
            <asp:View ID="viewRemesa" runat="server">
                <div>
                    <asp:Label ID="labelConfirm" CssClass="subtitle" runat="server" Text="A continuación se muestra el estado y los detalles de las comunicaciones asociadas a la remesa."></asp:Label>
                </div>
                <br />
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
                        <AlwaysShowHeader Enable="false" ShowEmptyRow="true" EmptyMessage="&lt;ML&gt;No se ha definido ninguna comunicación.&lt;/ML&gt;" />
                    </ExtendedProperties>
                    <Columns>
                        <asp:BoundField DataField="REFERENCIAEXP" HeaderText="Referencia Expediente">
                        </asp:BoundField>
                        <asp:BoundField DataField="NUMJUSTIFINTECO" HeaderText="Nº Docum">
                        </asp:BoundField>
                        <asp:BoundField DataField="CODENVIO" HeaderText="Código envío">
                        </asp:BoundField>
                        <asp:BoundField DataField="NUMDOCIDENTIFINTERPROPAG" HeaderText="NIF interesado">
                        </asp:BoundField>
                        <asp:BoundField DataField="FECHAACTUALIZACIONSICER" HeaderText="Fecha de actualización">
                        </asp:BoundField>
                        <asp:BoundField DataField="ESTADOENVIO" HeaderText="Estado">
                        </asp:BoundField>
                        <asp:TemplateField HeaderText="Acción" ItemStyle-HorizontalAlign="Center">
                            <ItemTemplate>
                               <asp:Label ID="documentoLbl" runat="server"></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </PixelwareApi:ExtendedGridView>
            </asp:View>
            <asp:View ID="viewWait" runat="server">
                <div class="title progress">Realizando operación. Por favor, espere...</div>
            </asp:View>
            <asp:View ID="viewSuccess" runat="server">
                <div class="title success">Impresión correcta. Se volverá a la página anterior en breve.</div>
            </asp:View>
            <asp:View ID="viewError" runat="server">
                <div class="title error">Error al mostrar la remesa</div>
                <div class="subtitle"><asp:Literal ID="literalError" runat="server"></asp:Literal></div>
            </asp:View>
        </asp:MultiView>
        <div id="divConfirmacion" runat="server" title="Confirmación de generación de etiqueta" style="display:none">   
            <p>A continuación se procedera a generar el PDF con la etiqueta de la comunicación elegida.</p>
            <p>Seleccione en que fila de la hoja de etiquetas desea realizar la impresión:</p>  
            <asp:DropDownList ID="filaImpresion" runat="server">
                
            </asp:DropDownList>
            <a id="descargarEtiquetas"></a>
        </div>
    </div>
    </form>
</body>
</html>
