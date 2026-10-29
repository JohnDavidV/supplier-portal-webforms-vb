Imports System.Collections.Concurrent

Namespace Security

    ''' <summary>
    ''' Per-account lockout with exponential backoff (30 s, 60 s, 120 s... up to 15 min)
    ''' after a number of free attempts. A successful login resets the counter.
    ''' </summary>
    Public NotInheritable Class LoginThrottle
        Private Class Entry
            Public Failures As Integer
            Public LockedUntil As DateTimeOffset
        End Class

        Private ReadOnly _entries As New ConcurrentDictionary(Of String, Entry)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _clock As Func(Of DateTimeOffset)
        Private ReadOnly _freeAttempts As Integer
        Private ReadOnly _baseLock As TimeSpan
        Private ReadOnly _maxLock As TimeSpan

        Public Sub New(Optional freeAttempts As Integer = 5, Optional clock As Func(Of DateTimeOffset) = Nothing,
                       Optional baseLockSeconds As Integer = 30, Optional maxLockMinutes As Integer = 15)
            _freeAttempts = freeAttempts
            _clock = If(clock, Function() DateTimeOffset.UtcNow)
            _baseLock = TimeSpan.FromSeconds(baseLockSeconds)
            _maxLock = TimeSpan.FromMinutes(maxLockMinutes)
        End Sub

        ''' <summary>Remaining lock time, or Nothing when a login attempt is allowed.</summary>
        Public Function LockedFor(account As String) As TimeSpan?
            Dim e As Entry = Nothing
            If Not _entries.TryGetValue(Normalize(account), e) Then Return Nothing
            Dim remaining = e.LockedUntil - _clock()
            Return If(remaining > TimeSpan.Zero, remaining, CType(Nothing, TimeSpan?))
        End Function

        Public Sub RegisterFailure(account As String)
            Dim e = _entries.GetOrAdd(Normalize(account), Function(k) New Entry())
            SyncLock e
                e.Failures += 1
                If e.Failures >= _freeAttempts Then
                    Dim exponent = Math.Min(e.Failures - _freeAttempts, 10)
                    Dim ticks = Math.Min(_baseLock.Ticks * (1L << exponent), _maxLock.Ticks)
                    e.LockedUntil = _clock().Add(TimeSpan.FromTicks(ticks))
                End If
            End SyncLock
        End Sub

        Public Sub RegisterSuccess(account As String)
            Dim removed As Entry = Nothing
            _entries.TryRemove(Normalize(account), removed)
        End Sub

        Private Shared Function Normalize(account As String) As String
            Return If(account, "").Trim()
        End Function
    End Class

End Namespace
