﻿module BlackFox.Stidgen.CsharpGeneration

open BlackFox.Stidgen.Description
open BlackFox.Stidgen.FluentRoslyn
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.CodeAnalysis.Formatting
open System.IO
open System.Text

let private visibilityToKeyword = function
    | Public -> SyntaxKind.PublicKeyword
    | Private -> SyntaxKind.PrivateKeyword
    | Protected -> SyntaxKind.ProtectedKeyword

let private makeValueProperty idType =
    SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(idType.Type.FullName), idType.ValueProperty)
        .AddAccessorListAccessors(
            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                |> withSemicolon,
            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                |> addModifiers [|SyntaxKind.PrivateKeyword|]
                |> withSemicolon
            )
        |> addModifiers [|SyntaxKind.PublicKeyword|]

let private makeClass idType = 
    let visibility = visibilityToKeyword idType.Visibility
    let generatedClass =
        SyntaxFactory.ClassDeclaration(idType.Name)
            |> addModifiers [|SyntaxKind.PartialKeyword; visibility|]
 
    let generatedMethod =
        SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "Test")
            .WithBody(SyntaxFactory.Block())
            |> addModifiers [|SyntaxKind.PublicKeyword; SyntaxKind.StaticKeyword|]
 
    generatedClass.AddMembers(
        generatedMethod,
        idType |> makeValueProperty)

let toCompilationUnit idType = 
    let generatedNamespace =
        SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(idType.Namespace))
            .AddMembers(makeClass idType)

    SyntaxFactory.CompilationUnit()
        .AddUsings("System")
        .AddMembers(generatedNamespace)

let compilationUnitToString compilationUnit =
    let stringBuilder = new StringBuilder()
    let workspace = new AdhocWorkspace()
    let formatted = Formatter.Format(compilationUnit, workspace)
    use writer = new StringWriter(stringBuilder)
    formatted.WriteTo(writer)
    
    writer.ToString()

let idTypeToString idType =
    idType
        |> toCompilationUnit
        |> compilationUnitToString