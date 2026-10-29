Imports System.Data

Namespace Data

    ''' <summary>
    ''' Minimal ADO.NET helper. Every value travels as a typed parameter - there is no API
    ''' that accepts concatenated SQL with user input, so SQL injection is not possible by
    ''' construction (the classic bug in legacy "sql = "... WHERE nit = '" + txtNit.Text + "'").
    ''' </summary>
    Public NotInheritable Class Db
        Private ReadOnly _open As Func(Of IDbConnection)

        ''' <param name="connectionFactory">Returns a NEW, unopened connection (e.g. Function() New SqlConnection(cs)).</param>
        Public Sub New(connectionFactory As Func(Of IDbConnection))
            _open = connectionFactory
        End Sub

        Public Function Query(Of T)(sql As String, map As Func(Of IDataRecord, T), ParamArray parameters As DbParam()) As List(Of T)
            Dim result As New List(Of T)()
            Using cn = _open()
                Using cmd = Prepare(cn, sql, parameters)
                    cn.Open()
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            result.Add(map(reader))
                        End While
                    End Using
                End Using
            End Using
            Return result
        End Function

        Public Function Execute(sql As String, ParamArray parameters As DbParam()) As Integer
            Using cn = _open()
                Using cmd = Prepare(cn, sql, parameters)
                    cn.Open()
                    Return cmd.ExecuteNonQuery()
                End Using
            End Using
        End Function

        Private Shared Function Prepare(cn As IDbConnection, sql As String, parameters As DbParam()) As IDbCommand
            Dim cmd = cn.CreateCommand()
            cmd.CommandText = sql
            cmd.CommandTimeout = 30
            For Each p In parameters
                Dim dp = cmd.CreateParameter()
                dp.ParameterName = p.Name
                dp.DbType = p.Type
                If p.Size > 0 Then dp.Size = p.Size
                dp.Value = If(p.Value, DBNull.Value)
                cmd.Parameters.Add(dp)
            Next
            Return cmd
        End Function
    End Class

    Public NotInheritable Class DbParam
        Public ReadOnly Name As String
        Public ReadOnly Value As Object
        Public ReadOnly Type As DbType
        Public ReadOnly Size As Integer

        Private Sub New(name As String, value As Object, type As DbType, size As Integer)
            Me.Name = name
            Me.Value = value
            Me.Type = type
            Me.Size = size
        End Sub

        Public Shared Function Text(name As String, value As String, size As Integer) As DbParam
            Return New DbParam(name, value, DbType.String, size)
        End Function

        Public Shared Function Int(name As String, value As Integer) As DbParam
            Return New DbParam(name, value, DbType.Int32, 0)
        End Function

        Public Shared Function Bool(name As String, value As Boolean) As DbParam
            Return New DbParam(name, value, DbType.Boolean, 0)
        End Function
    End Class

End Namespace
