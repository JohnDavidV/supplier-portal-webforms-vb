Imports System.Web

Public Class Global_asax
    Inherits HttpApplication

    Protected Sub Application_Start(sender As Object, e As EventArgs)
        AppServices.Initialize()
    End Sub

    Protected Sub Application_PreSendRequestHeaders(sender As Object, e As EventArgs)
        Dim headers = Response.Headers
        headers.Remove("Server")
        headers.Remove("X-AspNet-Version")
        headers.Remove("X-Powered-By")
        headers("X-Content-Type-Options") = "nosniff"
        headers("X-Frame-Options") = "DENY"
        headers("Referrer-Policy") = "no-referrer"
        headers("Content-Security-Policy") = "default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; frame-ancestors 'none'"
    End Sub

    Protected Sub Application_Error(sender As Object, e As EventArgs)
        ' Log the full exception server-side; users get the friendly customErrors page
        ' without stack traces, connection strings or SQL.
        Dim ex = Server.GetLastError()
        If ex IsNot Nothing Then
            Global.System.Diagnostics.Trace.TraceError("[{0:u}] {1} {2}: {3}", DateTime.UtcNow, Request.HttpMethod, Request.RawUrl, ex)
        End If
    End Sub
End Class
