﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition.Hosting
Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Snippets
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Snippets
    Public Class SnippetCompletionProviderTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function SnippetCompletion() As Task
            Dim markup = "a?$$"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic, extraParts:={GetType(MockSnippetInfoService)})
            Using testState
                testState.SendTabToCompletion()

                Assert.Equal(testState.GetDocumentText(), "a")

                Await testState.WaitForAsynchronousOperationsAsync()
                Assert.Equal(testState.CurrentCompletionPresenterSession.SelectedItem.DisplayText, "Shortcut")

                Dim document = testState.Workspace.CurrentSolution.Projects.First().Documents.First()
                Dim service = CompletionService.GetService(document)
                Dim itemDescription = Await service.GetDescriptionAsync(document, testState.CurrentCompletionPresenterSession.SelectedItem)
                Assert.True(itemDescription.Text.StartsWith("Description"))

                testState.SendTabToCompletion()

                Assert.True(testState.SnippetExpansionClient.TryInsertExpansionCalled)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TracksChangeSpanCorrectly() As Task
            Dim markup = "a?$$"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic, extraParts:={GetType(MockSnippetInfoService)})
            Using testState
                testState.SendTabToCompletion()
                Await testState.WaitForAsynchronousOperationsAsync()
                Assert.Equal(testState.CurrentCompletionPresenterSession.SelectedItem.DisplayText, "Shortcut")

                testState.SendBackspace()
                Await testState.WaitForAsynchronousOperationsAsync()
                Assert.Equal(testState.CurrentCompletionPresenterSession.SelectedItem.DisplayText, "Shortcut")

                testState.SendTabToCompletion()

                Await testState.WaitForAsynchronousOperationsAsync()
                Assert.True(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal(testState.GetDocumentText(), "Shortcut")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function SnippetListOnlyIfTextBeforeQuestionMark() As Task
            Dim markup = <File>
Class C
    ?$$
End Class</File>.Value

            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic, extraParts:={GetType(MockSnippetInfoService)})
            Using testState
                testState.SendTabToCompletion()
                Await testState.WaitForAsynchronousOperationsAsync()
                Assert.Null(testState.CurrentCompletionPresenterSession)
            End Using
        End Function
    End Class

    <ExportLanguageService(GetType(ISnippetInfoService), LanguageNames.VisualBasic), [Shared]>
    Friend Class MockSnippetInfoService
        Implements ISnippetInfoService

        Public Function GetSnippetsAsync_NonBlocking() As IEnumerable(Of SnippetInfo) Implements ISnippetInfoService.GetSnippetsIfAvailable
            Return SpecializedCollections.SingletonEnumerable(New SnippetInfo("Shortcut", "Title", "Description", "Path"))
        End Function

        Public Function ShouldFormatSnippet(snippetInfo As SnippetInfo) As Boolean Implements ISnippetInfoService.ShouldFormatSnippet
            Return False
        End Function

        Public Function SnippetShortcutExists_NonBlocking(shortcut As String) As Boolean Implements ISnippetInfoService.SnippetShortcutExists_NonBlocking
            Return shortcut = "Shortcut"
        End Function
    End Class
End Namespace

