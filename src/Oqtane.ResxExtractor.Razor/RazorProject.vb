﻿Imports Oqtane.ResxExtractor.CSharp

Public NotInheritable Class RazorProject
    Inherits CSharpProject

    Public Sub New(path As String)
        MyBase.New(path)
    End Sub

    Public Overrides ReadOnly Property FilesExtension() As String = ".razor"
End Class
