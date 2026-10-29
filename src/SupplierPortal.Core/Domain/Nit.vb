Imports System.Text.RegularExpressions

Namespace Domain

    ''' <summary>Colombian tax id (NIT) with DIAN modulo-11 verification digit.</summary>
    Public NotInheritable Class Nit
        Private Shared ReadOnly Weights As Integer() = {3, 7, 13, 17, 19, 23, 29, 37, 41, 43, 47, 53, 59, 67, 71}

        Private Sub New()
        End Sub

        Public Shared Function CheckDigit(base As String) As Integer
            Dim digits = Regex.Replace(If(base, ""), "\D", "")
            If digits.Length = 0 OrElse digits.Length > Weights.Length Then Throw New ArgumentException("Invalid NIT base.", NameOf(base))
            Dim total = 0
            For i = 0 To digits.Length - 1
                total += (AscW(digits(digits.Length - 1 - i)) - AscW("0"c)) * Weights(i)
            Next
            Dim remainder = total Mod 11
            Return If(remainder < 2, remainder, 11 - remainder)
        End Function

        ''' <summary>
        ''' Accepts what suppliers actually type ("900.123.456-7", "900123456", " 900123456 - 7 ")
        ''' and returns only the digits of the base, or Nothing if it is not a plausible NIT.
        ''' When a verification digit is present it must be correct.
        ''' </summary>
        Public Shared Function NormalizeBase(input As String) As String
            If String.IsNullOrWhiteSpace(input) Then Return Nothing
            Dim m = Regex.Match(input.Trim(), "^([\d.\s]{6,20}?)(?:\s*-\s*(\d))?$")
            If Not m.Success Then Return Nothing
            Dim base = Regex.Replace(m.Groups(1).Value, "\D", "")
            If base.Length < 6 OrElse base.Length > 15 Then Return Nothing
            If m.Groups(2).Success AndAlso CheckDigit(base) <> CInt(m.Groups(2).Value) Then Return Nothing
            Return base
        End Function

        Public Shared Function Format(base As String) As String
            Dim digits = Regex.Replace(If(base, ""), "\D", "")
            Return $"{Long.Parse(digits, Globalization.CultureInfo.InvariantCulture).ToString("N0", Globalization.CultureInfo.GetCultureInfo("es-CO"))}-{CheckDigit(digits)}"
        End Function
    End Class

End Namespace
