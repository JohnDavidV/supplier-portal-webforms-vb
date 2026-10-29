Imports System.Security.Cryptography
Imports System.Text

Namespace Security

    ''' <summary>
    ''' Small crypto helpers that behave the same on .NET Framework 4.8 and modern .NET
    ''' (several of the convenient static APIs do not exist on 4.8).
    ''' </summary>
    Public NotInheritable Class Crypto
        Private Sub New()
        End Sub

        Public Shared Function RandomBytes(count As Integer) As Byte()
            Dim buffer(count - 1) As Byte
            Using rng = RandomNumberGenerator.Create()
                rng.GetBytes(buffer)
            End Using
            Return buffer
        End Function

        ''' <summary>Compares two byte arrays in constant time (no early exit on the first difference).</summary>
        Public Shared Function FixedTimeEquals(a As Byte(), b As Byte()) As Boolean
            If a Is Nothing OrElse b Is Nothing OrElse a.Length <> b.Length Then Return False
            Dim diff = 0
            For i = 0 To a.Length - 1
                diff = diff Or (CInt(a(i)) Xor CInt(b(i)))
            Next
            Return diff = 0
        End Function

        Public Shared Function Sha256Hex(value As String) As String
            Using sha = SHA256.Create()
                Return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(If(value, ""))))
            End Using
        End Function

        Public Shared Function HmacSha256(key As Byte(), data As String) As Byte()
            Using mac As New HMACSHA256(key)
                Return mac.ComputeHash(Encoding.UTF8.GetBytes(data))
            End Using
        End Function

        Public Shared Function ToHex(bytes As Byte()) As String
            Dim sb As New StringBuilder(bytes.Length * 2)
            For Each b In bytes
                sb.Append(b.ToString("x2"))
            Next
            Return sb.ToString()
        End Function

        Public Shared Function Base64UrlEncode(bytes As Byte()) As String
            Return Convert.ToBase64String(bytes).TrimEnd("="c).Replace("+"c, "-"c).Replace("/"c, "_"c)
        End Function

        ''' <summary>Returns Nothing for malformed input instead of throwing.</summary>
        Public Shared Function Base64UrlDecode(value As String) As Byte()
            If String.IsNullOrEmpty(value) Then Return Nothing
            Dim s = value.Replace("-"c, "+"c).Replace("_"c, "/"c)
            s = s.PadRight(s.Length + (4 - s.Length Mod 4) Mod 4, "="c)
            Try
                Return Convert.FromBase64String(s)
            Catch ex As FormatException
                Return Nothing
            End Try
        End Function
    End Class

End Namespace
