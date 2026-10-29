Namespace Security

    ''' <summary>
    ''' Tamper-proof, expiring links for documents a supplier can forward (e.g. a withholding
    ''' certificate for their accountant). HMAC-SHA256 over purpose + resource + expiry.
    ''' </summary>
    Public NotInheritable Class SignedLink
        Private ReadOnly _key As Byte()
        Private ReadOnly _clock As Func(Of DateTimeOffset)

        Public Sub New(key As Byte(), Optional clock As Func(Of DateTimeOffset) = Nothing)
            If key Is Nothing OrElse key.Length < 32 Then Throw New ArgumentException("Use a key of at least 256 bits.", NameOf(key))
            _key = key
            _clock = If(clock, Function() DateTimeOffset.UtcNow)
        End Sub

        Public Function Sign(purpose As String, resourceId As String, validFor As TimeSpan) As SignedToken
            Dim expires = _clock().Add(validFor).ToUnixTimeSeconds()
            Return New SignedToken(expires, Compute(purpose, resourceId, expires))
        End Function

        Public Function IsValid(purpose As String, resourceId As String, expires As Long, signature As String) As Boolean
            If _clock().ToUnixTimeSeconds() > expires Then Return False
            Dim given = Crypto.Base64UrlDecode(signature)
            If given Is Nothing Then Return False
            Return Crypto.FixedTimeEquals(given, Crypto.Base64UrlDecode(Compute(purpose, resourceId, expires)))
        End Function

        Private Function Compute(purpose As String, resourceId As String, expires As Long) As String
            ' Length-prefixed fields: ("a|b","c") and ("a","b|c") can never collide.
            Dim payload = $"{purpose.Length}:{purpose}|{resourceId.Length}:{resourceId}|{expires}"
            Return Crypto.Base64UrlEncode(Crypto.HmacSha256(_key, payload))
        End Function
    End Class

    Public Structure SignedToken
        Public ReadOnly Expires As Long
        Public ReadOnly Signature As String

        Public Sub New(expires As Long, signature As String)
            Me.Expires = expires
            Me.Signature = signature
        End Sub
    End Structure

End Namespace
