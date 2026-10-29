Imports System.Globalization
Imports System.Web
Imports SupplierPortal.Core.Domain

''' <summary>
''' Public withholding certificate behind a signed, expiring link: no login needed, but the
''' NIT, year and expiry cannot be changed without invalidating the HMAC signature.
''' </summary>
Public Class CertificateHandler
    Implements IHttpHandler

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return True
        End Get
    End Property

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        Dim q = context.Request.QueryString
        Dim nitBase = If(q("nit"), "")
        Dim year, exp As Long
        Dim services = AppServices.Current

        If Not Long.TryParse(q("year"), year) OrElse Not Long.TryParse(q("exp"), exp) _
           OrElse Not services.Links.IsValid("withholding:" & year, nitBase, exp, q("sig")) Then
            context.Response.StatusCode = 403
            context.Response.ContentType = "text/plain; charset=utf-8"
            context.Response.Write("El enlace no es válido o ya expiró.")
            Return
        End If

        Dim supplier = services.Repository.Find(nitBase)
        Dim summary = services.Repository.YearlySummary(nitBase).FirstOrDefault(Function(s) s.Year = year)
        If supplier Is Nothing OrElse summary Is Nothing Then
            context.Response.StatusCode = 404
            Return
        End If

        Dim co = CultureInfo.GetCultureInfo("es-CO")
        Dim enc = Function(s As String) HttpUtility.HtmlEncode(s)
        context.Response.ContentType = "text/html; charset=utf-8"
        context.Response.Cache.SetCacheability(HttpCacheability.Private)
        context.Response.Write(
$"<!DOCTYPE html><html lang=""es""><head><meta charset=""utf-8""><title>Certificado de retención {year}</title>
<link href=""Content/site.css"" rel=""stylesheet""></head><body class=""certificate""><main class=""card"">
<h1>Certificado de retención en la fuente – {year}</h1>
<p>Demo Corp S.A.S. certifica que durante el año gravable {year} practicó retención en la fuente a:</p>
<table class=""grid""><tr><th>Proveedor</th><td>{enc(supplier.Name)}</td></tr>
<tr><th>NIT</th><td>{enc(Nit.Format(supplier.NitBase))}</td></tr>
<tr><th>Pagos</th><td>{summary.Payments}</td></tr>
<tr><th>Valor bruto pagado</th><td class=""num"">{summary.Gross.ToString("C0", co)}</td></tr>
<tr><th>Retención practicada</th><td class=""num"">{summary.Withholding.ToString("C0", co)}</td></tr></table>
<p class=""muted"">Documento generado el {Date.Now.ToString("d 'de' MMMM 'de' yyyy", co)}. Enlace válido hasta {DateTimeOffset.FromUnixTimeSeconds(exp).ToLocalTime().ToString("g", co)}.</p>
</main></body></html>")
    End Sub
End Class
