<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CorreosNA.aspx.cs" Inherits="AltaPagos.CorreosNA" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Exportar a Correos NA</title>
    <link href="styles/Correos.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div id="cabecera">
            Generación de fichero de texto para envío a Correos
        </div>
        <asp:MultiView ID="mainMultiView" runat="server">
            <asp:View ID="viewWait" runat="server">
                <div class="title progress">Generando fichero. Por favor, espere...</div>
                <asp:LinkButton ID="linkGenerar" runat="server" Text="Generar fichero" 
                    onclick="linkGenerar_Click" style="display:none" ></asp:LinkButton>
            </asp:View>
            <asp:View ID="viewSuccess" runat="server">
                <div class="subtitle success">Se ha generado correctamente el fichero de texto.</div>
                <div class="subtitle"><asp:Literal ID="literalNumRegistros" runat="server"></asp:Literal></div>
                <asp:Panel ID="panelWarnings" runat="server">
                    <div class="subtitle">Durante la generación del fichero se encontraron las siguientes incidencias:</div>
                    <asp:Table ID="tableIncidencias" runat="server" CellPadding="0" CellSpacing="0">
                        <asp:TableHeaderRow>
                            <asp:TableHeaderCell CssClass="cell1">Ref. del Expediente</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell2">Cod. Comunicación</asp:TableHeaderCell>
                            <asp:TableHeaderCell CssClass="cell3">Incidencia</asp:TableHeaderCell>
                        </asp:TableHeaderRow>
                    </asp:Table>
                </asp:Panel>
                <asp:LinkButton ID="linkDownload" runat="server" Text="Para descargar el fichero de texto generado, pulse aquí" 
                onclick="linkDownload_Click"></asp:LinkButton>
            </asp:View>
            <asp:View ID="viewError" runat="server">
                <div class="title error">No se ha podido completar la generación del fichero de texto.</div>
                <div class="subtitle"><asp:Literal ID="literalError" runat="server"></asp:Literal></div>
            </asp:View>
        </asp:MultiView>
    </div>
    </form>
</body>
</html>
