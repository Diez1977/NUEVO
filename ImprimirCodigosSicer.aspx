<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ImprimirCodigosSicer.aspx.cs" Inherits="AltaPagos.ImprimirCodigosSicer" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
    <head runat="server">
        <title>Imprimir códigos Sicer</title>

        <meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>

        <link href="styles/Global.css" rel="stylesheet" type="text/css" />
        <link type="text/css" href="styles/ui.jqgrid.css"  rel="stylesheet"/>
        <link href="Styles/jquery-ui-1.8.4.css" rel="stylesheet" />
        <link href="Styles/msg/jquery.msg.css" rel="stylesheet" type="text/css" />
        <link href="styles/Informe.css" rel="stylesheet" type="text/css" />
        <link href="styles/GridRegistros.css" rel="stylesheet" type="text/css" />

        <script type="text/javascript" src="Scripts/jquery-1.5.2.min.js"></script>
        <script src="Scripts/msg/jquery.center.min.js" type="text/javascript"></script>
        <script src="Scripts/msg/jquery.msg.js" type="text/javascript"></script>
        <script type="text/javascript" src="Scripts/jquery-ui-1.8.4.min.js"></script>
        <script type="text/javascript">
            $(document).ready(function () {
                if ($("#columnas").val()) {
                    cambiarModo($("#columnas").val());
                }

                $("#columnas").change(function () {
                    var valor = $(this).val();
                    if (valor) {
                        cambiarModo(valor);
                    } else {
                        $("#divFilas").css("display", "none");
                    }
                }); 
            });

            function cambiarModo(valor) {
                $("#divFilas").css("display", "block");

                $("#filasEstandar").val("");
                $("#filasBD").val("");

                if (valor === "1") {
                    $("#filasEstandar").css("display", "none");
                    $("#filasBD").css("display", "block");
                } else if (valor === "3") {
                    $("#filasEstandar").css("display", "block");
                    $("#filasBD").css("display", "none");
                }
            }

            function onBtnImprimir() {
                if ($("#columnas").val() === "1" && $("#filasBD").val() == "") {
                    return false;
                } else if ($("#columnas").val() === "3" && $("#filasEstandar").val() == "") {
                    return false;
                }

                return true;
            }

        </script>
    </head>
    <body>
        <form id="form1" runat="server">
            <div id="cabecera">
                A continuación, se procederá a generar las etiquetas de las comunicaciones asociadas a esta remesa.
            </div>
            <br />

            <div id="divColumnas" style="margin-bottom:10px;">
                <div class="subtitle info"><asp:Label ID="labelConfirm" runat="server" Text="Seleccione el número de etiquetas a generar por comunicación"></asp:Label></div>

                <asp:DropDownList runat="server" ID="columnas" AppendDataBoundItems="true" style="margin-left:38px;margin-top:5px;">
                    <asp:ListItem Text="" Value="" />
                    <asp:ListItem Text="1" Value="1" />
                    <asp:ListItem Text="3" Value="3" />
                </asp:DropDownList>
            </div>

            <div id="divFilas" style="margin-bottom:10px;display: none;">    
                <div class="subtitle info"><asp:Label ID="label1" runat="server" Text="Además, marque en qué fila de la hoja de etiquetas desea comenzar a imprimir"></asp:Label></div>
                
                <asp:DropDownList runat="server" ID="filasEstandar" AppendDataBoundItems="true" style="margin-left:38px;margin-top:5px;display: none;">
                    <asp:ListItem Text="" Value="" />
                    <asp:ListItem Text="1" Value="0" />
                    <asp:ListItem Text="2" Value="1" />
                    <asp:ListItem Text="3" Value="2" />
                    <asp:ListItem Text="4" Value="3" />
                    <asp:ListItem Text="5" Value="4" />
                    <asp:ListItem Text="6" Value="5" />
                    <asp:ListItem Text="7" Value="6" />
                    <asp:ListItem Text="8" Value="7" />
                    <asp:ListItem Text="9" Value="8" />
                    <asp:ListItem Text="10" Value="9" />
                    <asp:ListItem Text="11" Value="10" />
                    <asp:ListItem Text="12" Value="11" />
                    <asp:ListItem Text="13" Value="12" />
                    <asp:ListItem Text="14" Value="13" />
                    <asp:ListItem Text="15" Value="14" />
                    <asp:ListItem Text="16" Value="15" />
                    <asp:ListItem Text="17" Value="16" />
                </asp:DropDownList>

                <asp:DropDownList runat="server" ID="filasBD" AppendDataBoundItems="true" style="margin-left:38px;margin-top:5px;display: none;">
                    <asp:ListItem Text="" Value="" />
                </asp:DropDownList>
            </div>

            <div style="padding-left:38px;">
                <asp:LinkButton ID="LinkButtonImprimir" runat="server" Text="Imprimir" CssClass="BotonActivo" OnClientClick="return onBtnImprimir();" onclick="linkImprimir_Click"></asp:LinkButton>
            </div>
        </form>
    </body>
</html>
