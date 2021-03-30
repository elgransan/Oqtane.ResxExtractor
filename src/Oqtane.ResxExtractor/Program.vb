Imports System.Globalization
Imports System.IO
Imports Oqtane.ResxExtractor.Core
Imports Oqtane.ResxExtractor.Core.Extraction
Imports Oqtane.ResxExtractor.Core.Generation
Imports Oqtane.ResxExtractor.Razor

Module Program
    Private Const DefaultCulture As String = "en"

    Sub Main(args As String())
        If args.Length < 2 OrElse args.Length > 3 Then
            PrintHelp()

            Return
        End If

        Dim sourcePath As String = args(0)
        Dim destinationPath As String = args(1)
        Dim culture As String = DefaultCulture

        If args.Length = 3 Then
            Dim tokens() As String = args(2).Split("=")
            If tokens.Length = 2 AndAlso tokens(0) = "-c" Then
                culture = tokens(1)
            Else
                PrintHelp()

                Return
            End If
        End If

        Dim cultureInfo As CultureInfo = CultureInfo.GetCultureInfo(culture)
        Dim cultureFolderName As String = $"{cultureInfo.DisplayName.Split(" ")(0)} ({cultureInfo.Name})"

        destinationPath = Path.Combine(destinationPath, cultureFolderName)

        If Directory.Exists(sourcePath) Then
            Dim localizedStringCollection As New LocalizedStringCollection()
            Dim projectPaths = Directory.EnumerateFiles(sourcePath, $"*.csproj", SearchOption.AllDirectories)

            For Each projectPath In projectPaths
                Dim projects As New List(Of IProject) From {
                    New RazorProject(projectPath)
                }
                Dim localizedStringExtractor As New LocalizedStringExtractor(projects)
                Dim localizedStringOccurences As IEnumerable(Of LocalizedStringOccurence) = localizedStringExtractor.ExtractAsync().GetAwaiter().GetResult()
                localizedStringCollection.AddRange(localizedStringOccurences)

                PrintProjectStats(projectPath, localizedStringOccurences.Count())
            Next

            If localizedStringCollection.Count() > 0 Then
                Dim results = localizedStringCollection.SelectMany(Function(s) s.Locations).GroupBy(Function(l) l.File.Path)
                For Each result In results
                    Dim filePath As String = result.Key.Substring(sourcePath.Length + 1)
                    Dim resourcesPath As String = Path.Combine(destinationPath, filePath.Replace(".razor", ".resx"))
                    Try
                        Dim resxWriter As New ResxWriter(resourcesPath)
                        For Each location In result
                            Dim localizedString As LocalizedString = localizedStringCollection.Where(Function(s) s.Locations.Contains(location)).Single()
                            resxWriter.AddResource(localizedString.Name, localizedString.Value)
                        Next

                        resxWriter.Generate()
                    Catch ex As Exception
                        Console.WriteLine($"Error in '{result.Key}'")
                    End Try
                Next
            End If
        Else
            Console.WriteLine("The folder doesn't contains any project(s).")
        End If
    End Sub

    Private Sub PrintHelp()
        Console.WriteLine("Usage: oqtane-extractor <SOURCE_PATH> <DESTINATION_PATH> - ")
        Console.WriteLine()
        Console.WriteLine("Arguments:")
        Console.WriteLine("  <SOURCE_PATH>        The path to the source directory, that contains all projects to be scanned.")
        Console.WriteLine("  <DESTINATION_PATH>   The path to a directory where RESX files will be generated.")
        Console.WriteLine("Options:")
        Console.WriteLine("  -c                   The two letter code for the language that you target.")
    End Sub

    Private Sub PrintProjectStats(ByVal projectPath As String, ByVal localizedStringsCount As Integer)
        Dim defaultConsoleColor = Console.ForegroundColor
        Console.Write($"{Path.GetFileName(projectPath)}: Found ")
        Console.ForegroundColor = If(localizedStringsCount = 0, ConsoleColor.Red, ConsoleColor.Green)
        Console.Write(localizedStringsCount)
        Console.ForegroundColor = defaultConsoleColor
        Console.Write(" strings.")
        Console.WriteLine()
    End Sub
End Module
