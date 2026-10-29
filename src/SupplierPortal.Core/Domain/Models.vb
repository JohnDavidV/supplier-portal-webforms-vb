Namespace Domain

    Public Class Supplier
        Public Property NitBase As String
        Public Property Name As String
        Public Property Email As String
        Public Property PasswordHash As String
        Public Property MustChangePassword As Boolean
        Public Property IsActive As Boolean = True
    End Class

    Public Class Payment
        Public Property Id As Integer
        Public Property NitBase As String
        Public Property PaidOn As Date
        Public Property InvoiceNumber As String
        Public Property GrossAmount As Decimal
        Public Property Withholding As Decimal
        Public ReadOnly Property NetAmount As Decimal
            Get
                Return GrossAmount - Withholding
            End Get
        End Property
    End Class

    Public Class PaymentSummary
        Public Property Year As Integer
        Public Property Payments As Integer
        Public Property Gross As Decimal
        Public Property Withholding As Decimal
    End Class

End Namespace
