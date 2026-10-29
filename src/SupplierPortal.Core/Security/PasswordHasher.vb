Imports System.Security.Cryptography

Namespace Security

    Public Enum PasswordCheck
        Failed
        Success
        ''' <summary>Correct password stored with an outdated scheme: re-hash it now.</summary>
        SuccessRehashNeeded
    End Enum

    ''' <summary>
    ''' Versioned password storage with transparent upgrades:
    '''   pbkdf2$&lt;iterations&gt;$&lt;salt&gt;$&lt;hash&gt;   current (PBKDF2-HMAC-SHA256, random salt)
    '''   legacy$&lt;sha256 hex&gt;                    unsalted SHA-256 left by the previous portal
    ''' Legacy values are accepted once and replaced with the current scheme on that login.
    ''' </summary>
    Public NotInheritable Class PasswordHasher
        Public Const CurrentPrefix As String = "pbkdf2"
        Public Const LegacyPrefix As String = "legacy"
        Private Const SaltSize As Integer = 16
        Private Const HashSize As Integer = 32

        Private ReadOnly _iterations As Integer

        Public Sub New(Optional iterations As Integer = 310000)
            If iterations < 10000 Then Throw New ArgumentOutOfRangeException(NameOf(iterations))
            _iterations = iterations
        End Sub

        Public Function Hash(password As String) As String
            If String.IsNullOrEmpty(password) Then Throw New ArgumentException("Password is required.", NameOf(password))
            Dim salt = Crypto.RandomBytes(SaltSize)
            Dim derived = Derive(password, salt, _iterations, HashSize)
            Return String.Join("$", CurrentPrefix, _iterations.ToString(Globalization.CultureInfo.InvariantCulture),
                               Convert.ToBase64String(salt), Convert.ToBase64String(derived))
        End Function

        Public Function Verify(stored As String, password As String) As PasswordCheck
            If String.IsNullOrEmpty(stored) OrElse password Is Nothing Then Return PasswordCheck.Failed
            Dim parts = stored.Split("$"c)

            Select Case parts(0)
                Case CurrentPrefix
                    Dim iterations As Integer
                    If parts.Length <> 4 OrElse Not Integer.TryParse(parts(1), iterations) OrElse iterations < 1 Then Return PasswordCheck.Failed
                    Dim salt, expected As Byte()
                    Try
                        salt = Convert.FromBase64String(parts(2))
                        expected = Convert.FromBase64String(parts(3))
                    Catch ex As FormatException
                        Return PasswordCheck.Failed
                    End Try
                    If Not Crypto.FixedTimeEquals(Derive(password, salt, iterations, expected.Length), expected) Then Return PasswordCheck.Failed
                    Return If(iterations < _iterations, PasswordCheck.SuccessRehashNeeded, PasswordCheck.Success)

                Case LegacyPrefix
                    If parts.Length <> 2 Then Return PasswordCheck.Failed
                    Dim actual = Text.Encoding.ASCII.GetBytes(Crypto.Sha256Hex(password))
                    Dim legacy = Text.Encoding.ASCII.GetBytes(parts(1).ToLowerInvariant())
                    Return If(Crypto.FixedTimeEquals(actual, legacy), PasswordCheck.SuccessRehashNeeded, PasswordCheck.Failed)

                Case Else
                    Return PasswordCheck.Failed  ' unknown format: fail closed
            End Select
        End Function

        Private Shared Function Derive(password As String, salt As Byte(), iterations As Integer, size As Integer) As Byte()
            Using kdf As New Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256)
                Return kdf.GetBytes(size)
            End Using
        End Function
    End Class

End Namespace
