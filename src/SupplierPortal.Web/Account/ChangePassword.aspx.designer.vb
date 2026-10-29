Option Strict On
Option Explicit On

Partial Public Class ChangePassword
    Protected WithEvents ForcedNotice As Global.System.Web.UI.WebControls.PlaceHolder
    Protected WithEvents FormPanel As Global.System.Web.UI.WebControls.Panel
    Protected WithEvents CurrentBox As Global.System.Web.UI.WebControls.TextBox
    Protected WithEvents NewBox As Global.System.Web.UI.WebControls.TextBox
    Protected WithEvents ConfirmBox As Global.System.Web.UI.WebControls.TextBox
    Protected WithEvents ConfirmMatches As Global.System.Web.UI.WebControls.CompareValidator
    Protected WithEvents Errors As Global.System.Web.UI.WebControls.BulletedList
    Protected WithEvents SaveButton As Global.System.Web.UI.WebControls.Button
    Protected WithEvents Done As Global.System.Web.UI.WebControls.Label
End Class
