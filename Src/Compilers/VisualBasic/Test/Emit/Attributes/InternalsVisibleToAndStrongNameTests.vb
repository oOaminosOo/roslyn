﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection.Metadata
Imports System.Reflection.PortableExecutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities

Public Class InternalsVisibleToAndStrongNameTests
    Inherits BasicTestBase

#Region "Helpers"

    Sub New()
        SigningTestHelpers.InstallKey()
    End Sub

    Private Shared ReadOnly KeyPairFile As String = SigningTestHelpers.KeyPairFile
    Private Shared ReadOnly PublicKeyFile As String = SigningTestHelpers.PublicKeyFile
    Private Shared ReadOnly PublicKey As ImmutableArray(Of Byte) = SigningTestHelpers.PublicKey
    Private Shared ReadOnly DefaultProvider As DesktopStrongNameProvider = New SigningTestHelpers.VirtualizedStrongNameProvider(ImmutableArray.Create(Of String)())

    Private Shared Function GetProviderWithPath(keyFilePath As String) As DesktopStrongNameProvider
        Return New SigningTestHelpers.VirtualizedStrongNameProvider(ImmutableArray.Create(keyFilePath))
    End Function

#End Region

#Region "Naming Tests"

    <Fact>
    Public Sub PubKeyFromKeyFileAttribute()
        Dim x = KeyPairFile
        Dim s = "<Assembly: System.Reflection.AssemblyKeyFile(""" & x & """)>" & vbCrLf &
                "Public Class C" & vbCrLf &
                "End Class"

        Dim g = Guid.NewGuid()
        Dim other = VisualBasicCompilation.Create(
            g.ToString(),
            {VisualBasicSyntaxTree.ParseText(s)},
            {MscorlibRef},
            TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        other.VerifyDiagnostics()
        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, other.Assembly.Identity.PublicKey))
    End Sub

    <Fact>
    Public Sub PubKeyFromKeyFileAttribute_AssemblyKeyFileResolver()
        Dim keyFileDir = Path.GetDirectoryName(KeyPairFile)
        Dim keyFileName = Path.GetFileName(KeyPairFile)

        Dim s = "<Assembly: System.Reflection.AssemblyKeyFile(""" & keyFileName & """)>" & vbCrLf &
                "Public Class C" & vbCrLf &
                "End Class"

        Dim syntaxTree = ParseAndVerify(s)

        ' verify failure with default assembly key file resolver
        Dim comp = CreateCompilationWithMscorlib({syntaxTree}, compOptions:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))
        comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_PublicKeyFileFailure).WithArguments(keyFileName, "File not found."))

        Assert.True(comp.Assembly.Identity.PublicKey.IsEmpty)

        ' verify success with custom assembly key file resolver with keyFileDir added to search paths
        comp = VisualBasicCompilation.Create(
            GetUniqueName(),
            {syntaxTree},
            {MscorlibRef},
            TestOptions.ReleaseDll.WithStrongNameProvider(GetProviderWithPath(keyFileDir)))

        comp.VerifyDiagnostics()
        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, comp.Assembly.Identity.PublicKey))
    End Sub

    <Fact>
    Public Sub PubKeyFromKeyFileAttribute_AssemblyKeyFileResolver_RelativeToCurrentParent()
        Dim keyFileDir = Path.GetDirectoryName(KeyPairFile)
        Dim keyFileName = Path.GetFileName(KeyPairFile)

        Dim s = "<Assembly: System.Reflection.AssemblyKeyFile(""..\" & keyFileName & """)>" & vbCrLf &
                "Public Class C" & vbCrLf &
                "End Class"

        Dim syntaxTree = ParseAndVerify(s)

        ' verify failure with default assembly key file resolver
        Dim comp As Compilation = CreateCompilationWithMscorlib({syntaxTree}, compOptions:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))
        comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_PublicKeyFileFailure).WithArguments("..\" & keyFileName, "File not found."))

        Assert.True(comp.Assembly.Identity.PublicKey.IsEmpty)

        ' verify success with custom assembly key file resolver with keyFileDir\TempSubDir added to search paths
        comp = VisualBasicCompilation.Create(
            GetUniqueName(),
            references:={MscorlibRef},
            syntaxTrees:={syntaxTree},
            options:=TestOptions.ReleaseDll.WithStrongNameProvider(GetProviderWithPath(PathUtilities.CombineAbsoluteAndRelativePaths(keyFileDir, "TempSubDir\"))))

        comp.VerifyDiagnostics()
        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, comp.Assembly.Identity.PublicKey))
    End Sub

    <Fact>
    Public Sub PubKeyFromKeyContainerAttribute()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        other.VerifyDiagnostics()
        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, other.Assembly.Identity.PublicKey))
    End Sub

    <Fact>
    Public Sub PubKeyFromKeyFileOptions()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.ReleaseDll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider))

        other.VerifyDiagnostics()
        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, other.Assembly.Identity.PublicKey))
    End Sub

    <Fact>
    Public Sub PubKeyFromKeyFileOptions_ReferenceResolver()
        Dim keyFileDir = Path.GetDirectoryName(KeyPairFile)
        Dim keyFileName = Path.GetFileName(KeyPairFile)

        Dim source = <![CDATA[
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
        Dim references = {MscorlibRef}
        Dim syntaxTrees = {ParseAndVerify(source)}

        ' verify failure with default resolver
        Dim comp = VisualBasicCompilation.Create(
            GetUniqueName(),
            references:=references,
            syntaxTrees:=syntaxTrees,
            options:=TestOptions.ReleaseDll.WithCryptoKeyFile(keyFileName).WithStrongNameProvider(DefaultProvider))

        comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_PublicKeyFileFailure).WithArguments(keyFileName, "File not found."))

        Assert.True(comp.Assembly.Identity.PublicKey.IsEmpty)

        ' verify success with custom assembly key file resolver with keyFileDir added to search paths
        comp = VisualBasicCompilation.Create(
            GetUniqueName(),
            references:=references,
            syntaxTrees:=syntaxTrees,
            options:=TestOptions.ReleaseDll.WithCryptoKeyFile(keyFileName).WithStrongNameProvider(GetProviderWithPath(keyFileDir)))

        comp.VerifyDiagnostics()
        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, comp.Assembly.Identity.PublicKey))
    End Sub

    <Fact>
    Public Sub PubKeyFromKeyFileOptionsJustPublicKey()
        Dim s =
            <compilation>
                <file name="Clavelle.vb"><![CDATA[
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
                </file>
            </compilation>
        Dim other = CreateCompilationWithMscorlib(s, options:=TestOptions.ReleaseDll.WithCryptoKeyFile(PublicKeyFile).WithDelaySign(True).WithStrongNameProvider(DefaultProvider))

        Assert.Empty(other.GetDiagnostics())
        Assert.True(ByteSequenceComparer.Instance.Equals(TestResources.SymbolsTests.General.snPublicKey.AsImmutableOrNull(), other.Assembly.Identity.PublicKey))
    End Sub

    <Fact>
    Public Sub PubKeyFromKeyFileOptionsJustPublicKey_ReferenceResolver()
        Dim publicKeyFileDir = Path.GetDirectoryName(PublicKeyFile)
        Dim publicKeyFileName = Path.GetFileName(PublicKeyFile)

        Dim source = <![CDATA[
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>

        Dim references = {MscorlibRef}
        Dim syntaxTrees = {ParseAndVerify(source)}

        ' verify failure with default resolver
        Dim comp = VisualBasicCompilation.Create(
            GetUniqueName(),
            references:=references,
            syntaxTrees:=syntaxTrees,
            options:=TestOptions.ReleaseDll.WithCryptoKeyFile(publicKeyFileName).WithDelaySign(True).WithStrongNameProvider(DefaultProvider))

        ' error CS7027: Error extracting public key from file 'PublicKeyFile.snk' -- File not found.
        ' warning CS7033: Delay signing was specified and requires a public key, but no public key was specified
        comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_PublicKeyFileFailure).WithArguments(publicKeyFileName, "File not found."),
            Diagnostic(ERRID.WRN_DelaySignButNoKey))
        Assert.True(comp.Assembly.Identity.PublicKey.IsEmpty)

        ' verify success with custom assembly key file resolver with publicKeyFileDir added to search paths
        comp = VisualBasicCompilation.Create(
            GetUniqueName(),
            references:=references,
            syntaxTrees:=syntaxTrees,
            options:=TestOptions.ReleaseDll.WithCryptoKeyFile(publicKeyFileName).WithDelaySign(True).WithStrongNameProvider(GetProviderWithPath(publicKeyFileDir)))

        comp.VerifyDiagnostics()
        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, comp.Assembly.Identity.PublicKey))
    End Sub

    <Fact>
    Public Sub PubKeyFileNotFoundOptions()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.ReleaseExe.WithCryptoKeyFile("foo").WithStrongNameProvider(DefaultProvider))

        CompilationUtils.AssertTheseDeclarationDiagnostics(other,
            <errors>
BC36980: Error extracting public key from file 'foo': File not found.                    
                </errors>)
        Assert.True(other.Assembly.Identity.PublicKey.IsEmpty)
    End Sub


    <Fact>
    Public Sub KeyFileAttributeEmpty()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyKeyFile("")>
Public Class C
 Friend Sub Foo()
    End Sub
End Class
]]>
    </file>
</compilation>, TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        other.VerifyDiagnostics()
        Assert.True(other.Assembly.Identity.PublicKey.IsEmpty)
    End Sub

    <Fact>
    Public Sub KeyContainerEmpty()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyKeyName("")>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        other.VerifyDiagnostics()
        Assert.True(other.Assembly.Identity.PublicKey.IsEmpty)
    End Sub

    <Fact>
    Public Sub PubKeyFileBogusOptions()
        Dim tmp = Temp.CreateFile().WriteAllBytes(New Byte() {1, 2, 3, 4})
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file>
        <![CDATA[
Public Class C
Friend Sub Foo()
End Sub
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.ReleaseDll.WithCryptoKeyFile(tmp.Path).WithStrongNameProvider(New DesktopStrongNameProvider()))

        other.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_PublicKeyFileFailure).WithArguments(tmp.Path, "Value does not fall within the expected range."))

        Assert.True(other.Assembly.Identity.PublicKey.IsEmpty)
    End Sub

    <Fact>
    Public Sub PubKeyContainerBogusOptions()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithCryptoKeyContainer("foo").WithStrongNameProvider(DefaultProvider))

        CompilationUtils.AssertTheseDeclarationDiagnostics(other,
            <errors>
BC36981: Error extracting public key from container 'foo': Keyset does not exist (Exception from HRESULT: 0x80090016)                    
                </errors>)
        Assert.True(other.Assembly.Identity.PublicKey.IsEmpty)
    End Sub
#End Region

#Region "IVT Access checking"
    <Fact>
    Public Sub IVTBasicCompilation()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="HasIVTToCompilation">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WantsIVTAccess")>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        other.VerifyDiagnostics()

        Dim c As VisualBasicCompilation = CreateCompilationWithMscorlibAndReferences(
<compilation name="WantsIVTAccessButCantHave">
    <file name="a.vb"><![CDATA[
Public Class A
    Friend Class B
        Protected Sub New(o As C)
          o.Foo()
        End Sub
    End Class
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(other)}, options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        'compilation should not succeed, and internals should not be imported.
        c.GetDiagnostics()

        CompilationUtils.AssertTheseDiagnostics(c, <error>
BC30390: 'C.Friend Sub Foo()' is not accessible in this context because it is 'Friend'.
          o.Foo()
          ~~~~~
</error>)

        Dim c2 As VisualBasicCompilation = CreateCompilationWithMscorlibAndReferences(
<compilation name="WantsIVTAccess">
    <file name="a.vb"><![CDATA[
Public Class A
    Friend Class B
        Protected Sub New(o As C)
          o.Foo()
        End Sub
    End Class
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(other)}, options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        c2.VerifyDiagnostics()
    End Sub

    <Fact>
    Public Sub IVTBasicMetadata()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="HasIVTToCompilation">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WantsIVTAccess")>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        Dim otherImage = other.EmitToArray()

        Dim c As VisualBasicCompilation = CreateCompilationWithMscorlibAndReferences(
<compilation name="WantsIVTAccessButCantHave">
    <file name="a.vb"><![CDATA[
Public Class A
    Friend Class B
        Protected Sub New(o As C)
          o.Foo()
        End Sub
    End Class
End Class
]]>
    </file>
</compilation>, {MetadataReference.CreateFromImage(otherImage)}, TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        'compilation should not succeed, and internals should not be imported.
        c.GetDiagnostics()

        'gives "is not a member" error because internals were not imported because no IVT was found
        'on HasIVTToCompilation that referred to WantsIVTAccessButCantHave
        CompilationUtils.AssertTheseDiagnostics(c, <error>
BC30456: 'Foo' is not a member of 'C'.
          o.Foo()
          ~~~~~
</error>)

        Dim c2 As VisualBasicCompilation = CreateCompilationWithMscorlibAndReferences(
<compilation name="WantsIVTAccess">
    <file name="a.vb"><![CDATA[
Public Class A
    Friend Class B
        Protected Sub New(o As C)
          o.Foo()
        End Sub
    End Class
End Class
]]>
    </file>
</compilation>, {MetadataReference.CreateFromImage(otherImage)}, TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        c2.VerifyDiagnostics()
    End Sub

    <Fact>
    Public Sub SignModuleKeyContainerBogus()
        Dim c1 As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="WantsIVTAccess">
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyKeyName("bogus")>
Public Class A
End Class
]]>
    </file>
</compilation>, TestOptions.ReleaseModule.WithStrongNameProvider(DefaultProvider))

        'shouldn't have an error. The attribute's contents are checked when the module is added.
        Dim reference = c1.EmitToImageReference()

        Dim c2 As VisualBasicCompilation = CreateCompilationWithMscorlibAndReferences(
(<compilation name="WantsIVTAccess">
     <file name="a.vb"><![CDATA[
Public Class C
End Class
]]>
     </file>
 </compilation>), {reference}, TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        c2.VerifyDiagnostics(Diagnostic(ERRID.ERR_PublicKeyContainerFailure).WithArguments("bogus", "Keyset does not exist (Exception from HRESULT: 0x80090016)"))
    End Sub

    <Fact>
    Public Sub SignModuleKeyFileBogus()
        Dim c1 As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="WantsIVTAccess">
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyKeyFile("bogus")>
Public Class A
End Class
]]>
    </file>
</compilation>, TestOptions.ReleaseModule.WithStrongNameProvider(DefaultProvider))

        'shouldn't have an error. The attribute's contents are checked when the module is added.
        Dim reference = c1.EmitToImageReference()

        Dim c2 As VisualBasicCompilation = CreateCompilationWithMscorlibAndReferences(
(<compilation name="WantsIVTAccess">
     <file name="a.vb"><![CDATA[
Public Class C
End Class
]]>
     </file>
 </compilation>), {reference}, TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        c2.VerifyDiagnostics(Diagnostic(ERRID.ERR_PublicKeyFileFailure).WithArguments("bogus", "File not found."))
    End Sub

    <Fact>
    Public Sub IVTSigned()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="Paul">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>
Friend Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithCryptoKeyFile(KeyPairFile).WithDelaySign(True).WithStrongNameProvider(DefaultProvider))

        other.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlibAndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Public Class A
    Private Sub New(o As C)
        o.Foo()
    End Sub
End Class
]]>
    </file>
</compilation>,
{New VisualBasicCompilationReference(other)}, TestOptions.ReleaseDll.WithCryptoKeyContainer("roslynTestContainer").WithStrongNameProvider(DefaultProvider))

        Assert.DoesNotThrow(Function() requestor.Assembly.Identity)
        requestor.VerifyDiagnostics()
    End Sub

    <Fact>
    Public Sub IVTErrorNotBothSigned()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="Paul">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>
Friend Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        other.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlibAndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Public Class A
    Private Sub New(o As C)
        o.Foo()
    End Sub
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(other)}, TestOptions.ReleaseDll.WithCryptoKeyFile(KeyPairFile).WithDelaySign(True).WithStrongNameProvider(DefaultProvider))

        Assert.DoesNotThrow(Function() requestor.Assembly.Identity)
        'gives "is not accessible" error because internals were imported because IVT was found
        CompilationUtils.AssertTheseDiagnostics(requestor, <error>BC30389: 'C' is not accessible in this context because it is 'Friend'.
    Private Sub New(o As C)
                         ~
</error>)

    End Sub

    <Fact>
    Public Sub IVTDeferredSuccess()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="Paul">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>
Friend Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithCryptoKeyFile(KeyPairFile).WithDelaySign(True).WithStrongNameProvider(DefaultProvider))
        other.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlibAndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Imports MyC=C 'causes optimistic granting
<Assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>
Public Class A
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(other)}, options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        Assert.DoesNotThrow(Function() requestor.Assembly.Identity)
        Assert.True(DirectCast(other.Assembly, IAssemblySymbol).GivesAccessTo(requestor.Assembly))
        requestor.AssertNoDiagnostics()
    End Sub

    <Fact>
    Public Sub IVTDeferredFailSignMismatch()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="Paul">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>
Friend Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        other.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlibAndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Imports MyC=C
<Assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>
Public Class A
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(other)}, options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        Assert.DoesNotThrow(Function() requestor.Assembly.Identity)
        CompilationUtils.AssertTheseDiagnostics(requestor,
            <error>BC36958: Friend access was granted by 'Paul, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null', but the strong name signing state of the output assembly does not match that of the granting assembly.</error>)
    End Sub

    <Fact>
    Public Sub IVTDeferredFailKeyMismatch()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="Paul">
    <file name="a.vb"><![CDATA[
'key is wrong in the first digit
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("John, PublicKey=10240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>
Friend Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithCryptoKeyContainer("roslynTestContainer").WithStrongNameProvider(DefaultProvider))

        other.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlibAndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Imports MyC=C
<Assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>
Public Class A
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(other)}, options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        Assert.DoesNotThrow(Function() requestor.Assembly.Identity)
        CompilationUtils.AssertTheseDiagnostics(requestor, <errors>BC36957: Friend access was granted by 'Paul, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2', but the public key of the output assembly does not match that specified by the attribute in the granting assembly.</errors>)

    End Sub


    <Fact>
    Public Sub IVTSuccessThroughIAssembly()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="Paul">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>
Friend Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithCryptoKeyFile(KeyPairFile).WithDelaySign(True).WithStrongNameProvider(DefaultProvider))

        other.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlibAndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Imports MyC=C 'causes optimistic granting
<Assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>
Public Class A
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(other)}, options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        Assert.True(DirectCast(other.Assembly, IAssemblySymbol).GivesAccessTo(requestor.Assembly))
    End Sub

    <Fact>
    Public Sub IVTFailSignMismatchThroughIAssembly()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="Paul">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>
Friend Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        other.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlibAndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Imports MyC=C
<Assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>
Public Class A
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(other)}, options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        Assert.False(DirectCast(other.Assembly, IAssemblySymbol).GivesAccessTo(requestor.Assembly))
    End Sub

    <WorkItem(820450, "DevDiv")>
    <Fact>
    Public Sub IVTGivesAccessToUsingDifferentKeys()
        Dim giver As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="Paul">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>
Namespace ClassLibrary
    Friend Class FriendClass
     Public Sub Foo()
     End Sub
    End Class
end Namespace
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithCryptoKeyFile(SigningTestHelpers.KeyPairFile2).WithStrongNameProvider(DefaultProvider))

        giver.VerifyDiagnostics()

        Dim requestor As VisualBasicCompilation = CreateCompilationWithMscorlibAndReferences(
<compilation name="John">
    <file name="a.vb"><![CDATA[
Public Class ClassWithFriendMethod
    Friend Sub Test(A as ClassLibrary.FriendClass)
    End Sub
End Class
]]>
    </file>
</compilation>, {New VisualBasicCompilationReference(giver)}, options:=TestOptions.ReleaseDll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider))

        Assert.True(DirectCast(giver.Assembly, IAssemblySymbol).GivesAccessTo(requestor.Assembly))
        Assert.Empty(requestor.GetDiagnostics())
    End Sub
#End Region

#Region "IVT instantiations"
    <Fact>
    Public Sub IVTHasCulture()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="Sam">
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
<Assembly: InternalsVisibleTo("WantsIVTAccess, Culture=neutral")>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        Dim expectedErrors = <error><![CDATA[
BC31534: Friend assembly reference 'WantsIVTAccess, Culture=neutral' is invalid. InternalsVisibleTo declarations cannot have a version, culture, public key token, or processor architecture specified.
<Assembly: InternalsVisibleTo("WantsIVTAccess, Culture=neutral")>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></error>
        CompilationUtils.AssertTheseDeclarationDiagnostics(other, expectedErrors)
    End Sub

    <Fact>
    Public Sub IVTNoKey()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="Sam">
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
<Assembly: InternalsVisibleTo("WantsIVTAccess")>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider))

        Dim expectedErrors = <error><![CDATA[
BC31535: Friend assembly reference 'WantsIVTAccess' is invalid. Strong-name signed assemblies must specify a public key in their InternalsVisibleTo declarations.
<Assembly: InternalsVisibleTo("WantsIVTAccess")>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></error>
        CompilationUtils.AssertTheseDeclarationDiagnostics(other, expectedErrors)
    End Sub
#End Region

#Region "Signing"
    <Fact>
    Public Sub SignIt()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="Sam">
    <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.ReleaseDll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider))

        Dim peHeaders = New PEHeaders(other.EmitToStream())
        Assert.Equal(CorFlags.StrongNameSigned, peHeaders.CorHeader.Flags And CorFlags.StrongNameSigned)
    End Sub

    <Fact>
    Public Sub SignItWithOnlyPublicKey()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="Sam">
    <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.ReleaseDll.WithCryptoKeyFile(PublicKeyFile).WithStrongNameProvider(DefaultProvider))

        Using outStrm = New MemoryStream()
            Dim emitResult = other.Emit(outStrm)

            CompilationUtils.AssertTheseDiagnostics(emitResult.Diagnostics,
<errors>
BC36961: Key file '<%= PublicKeyFile %>' is missing the private key needed for signing.
</errors>)
        End Using

        other = other.WithOptions(TestOptions.ReleaseModule.WithCryptoKeyFile(PublicKeyFile))

        Dim assembly As VisualBasicCompilation = CreateCompilationWithMscorlibAndReferences(
<compilation name="Sam2">
    <file name="a.vb">
    </file>
</compilation>,
        {other.EmitToImageReference()},
        options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        Using outStrm = New MemoryStream()
            Dim emitResult = assembly.Emit(outStrm)

            CompilationUtils.AssertTheseDiagnostics(emitResult.Diagnostics,
<errors>
BC36961: Key file '<%= PublicKeyFile %>' is missing the private key needed for signing.
</errors>)
        End Using
    End Sub

    <Fact>
    Public Sub DelaySignItWithOnlyPublicKey()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="Sam">
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyDelaySign(True)>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.ReleaseDll.WithCryptoKeyFile(PublicKeyFile).WithStrongNameProvider(DefaultProvider))

        CompileAndVerify(other)
    End Sub

    <Fact>
    Public Sub DelaySignButNoKey()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyDelaySign(True)>
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        Dim outStrm = New MemoryStream()
        Dim emitResult = other.Emit(outStrm)
        ' Dev11: vbc : warning BC40010: Possible problem detected while building assembly 'VBTestD': Delay signing was requested, but no key was given
        '              warning BC41008: Use command-line option '/delaysign' or appropriate project settings instead of 'System.Reflection.AssemblyDelaySignAttribute'.
        CompilationUtils.AssertTheseDiagnostics(emitResult.Diagnostics, <errors>BC40060: Delay signing was specified and requires a public key, but no public key was specified.</errors>)
        Assert.True(emitResult.Success)
    End Sub

    <Fact>
    Public Sub SignInMemory()
        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider))

        Dim outStrm = New MemoryStream()
        Dim emitResult = other.Emit(outStrm)
        Assert.True(emitResult.Success)
    End Sub

    <WorkItem(545720, "DevDiv")>
    <WorkItem(530050, "DevDiv")>
    <Fact>
    Public Sub InvalidAssemblyName()

        Dim il = <![CDATA[
.assembly extern mscorlib { }
.assembly asm1
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 09 2F 5C 3A 2A 3F 27 3C 3E 7C 00 00 ) // .../\:*?'<>|..
}

.class private auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
]]>

        Dim vb = <compilation>
                     <file name="a.vb"><![CDATA[
Public Class Derived
    Inherits Base
End Class
]]>
                     </file>
                 </compilation>

        Dim ilRef = CompileIL(il.Value, appendDefaultHeader:=False)

        Dim comp = CreateCompilationWithMscorlibAndReferences(vb, {ilRef}, TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        ' NOTE: dev10 reports ERR_FriendAssemblyNameInvalid, but Roslyn won't (DevDiv #15099).
        comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_InaccessibleSymbol2, "Base").WithArguments("Base", "Friend"))
    End Sub

    <Fact>
    Public Sub DelaySignWithAssemblySignatureKey()
        '//Note that this SignatureKey is some random one that I found in the devdiv build.
        '//It is not related to the other keys we use in these tests.

        '//In the native compiler, when the AssemblySignatureKey attribute is present, and
        '//the binary is configured for delay signing, the contents of the assemblySignatureKey attribute
        '//(rather than the contents of the keyfile or container) are used to compute the size needed to 
        '//reserve in the binary for its signature. Signing using this key is only supported via sn.exe

        Dim other = CreateCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyDelaySign(True)>
<Assembly: System.Reflection.AssemblySignatureKey("002400000c800000140100000602000000240000525341310008000001000100613399aff18ef1a2c2514a273a42d9042b72321f1757102df9ebada69923e2738406c21e5b801552ab8d200a65a235e001ac9adc25f2d811eb09496a4c6a59d4619589c69f5baf0c4179a47311d92555cd006acc8b5959f2bd6e10e360c34537a1d266da8085856583c85d81da7f3ec01ed9564c58d93d713cd0172c8e23a10f0239b80c96b07736f5d8b022542a4e74251a5f432824318b3539a5a087f8e53d2f135f9ca47f3bb2e10aff0af0849504fb7cea3ff192dc8de0edad64c68efde34c56d302ad55fd6e80f302d5efcdeae953658d3452561b5f36c542efdbdd9f888538d374cef106acf7d93a4445c3c73cd911f0571aaf3d54da12b11ddec375b3", "a5a866e1ee186f807668209f3b11236ace5e21f117803a3143abb126dd035d7d2f876b6938aaf2ee3414d5420d753621400db44a49c486ce134300a2106adb6bdb433590fef8ad5c43cba82290dc49530effd86523d9483c00f458af46890036b0e2c61d077d7fbac467a506eba29e467a87198b053c749aa2a4d2840c784e6d")>
Public Class C
 Friend Sub Foo()
    End Sub
End Class
]]>
    </file>
</compilation>, {MscorlibRef_v4_0_30316_17626}, TestOptions.ReleaseDll.WithDelaySign(True).WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider))

        ' confirm header has expected SN signature size
        Dim peHeaders = New PEHeaders(other.EmitToStream())
        Assert.Equal(256, peHeaders.CorHeader.StrongNameSignatureDirectory.Size)
    End Sub

    ''' <summary>
    ''' Won't fix (easy to be tested here)
    ''' </summary>
    <Fact(), WorkItem(529953, "DevDiv"), WorkItem(530112, "DevDiv")>
    Public Sub DeclareAssemblyKeyNameAndFile_BC41008()

        Dim src = "<Assembly: System.Reflection.AssemblyKeyName(""Key1"")>" & vbCrLf &
                "<Assembly: System.Reflection.AssemblyKeyFile(""" & KeyPairFile & """)>" & vbCrLf &
              "Public Class C" & vbCrLf &
              "End Class"

        Dim tree = ParseAndVerify(src)
        Dim comp = CreateCompilationWithMscorlib({tree}, compOptions:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        ' Native Compiler:
        'warning BC41008: Use command-line option '/keycontainer' or appropriate project settings instead of 'System.Reflection.AssemblyKeyNameAttribute() '.
        ' <Assembly: System.Reflection.AssemblyKeyName("Key1")>
        '            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        'warning BC41008: Use command-line option '/keyfile' or appropriate project settings instead of 'System.Reflection.AssemblyKeyFileAttribute() '.
        '<Assembly: System.Reflection.AssemblyKeyFile("Key2.snk")>
        '  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        comp.VerifyDiagnostics()
        '   Diagnostic(ERRID.WRN_UseSwitchInsteadOfAttribute, "System.Reflection.AssemblyKeyName(""Key1""").WithArguments("/keycontainer"),
        '   Diagnostic(ERRID.WRN_UseSwitchInsteadOfAttribute, "System.Reflection.AssemblyKeyFile(""Key2.snk""").WithArguments("/keyfile"))

        Dim outStrm = New MemoryStream()
        Dim emitResult = comp.Emit(outStrm)
        Assert.True(emitResult.Success)

    End Sub

    Private Sub ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(
        moduleContents As Stream,
        expectedModuleAttr As AttributeDescription
    )
        ' a module doesn't get signed for real. It should have either a keyfile or keycontainer attribute
        ' parked on a typeRef named 'AssemblyAttributesGoHere.' When the module is added to an assembly, the
        ' resulting assembly is signed with the key referred to by the aforementioned attribute.

        Dim success As EmitResult
        Dim tempFile = Temp.CreateFile()
        moduleContents.Position = 0

        Using metadata = ModuleMetadata.CreateFromStream(moduleContents)
            Dim flags = metadata.Module.PEReaderOpt.PEHeaders.CorHeader.Flags
            ' confirm file does not claim to be signed
            Assert.Equal(0, CInt(flags And CorFlags.StrongNameSigned))
            Dim token As Handle = metadata.Module.GetTypeRef(metadata.Module.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHere")
            Assert.False(token.IsNil)   ' could the magic type ref be located? If not then the attribute's not there.
            Dim attrInfos = metadata.Module.FindTargetAttributes(token, expectedModuleAttr)
            Assert.Equal(1, attrInfos.Count())

            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Public Class Z
End Class
]]>
    </file>
</compilation>

            ' now that the module checks out, ensure that adding it to a compilation outputing a dll
            ' results in a signed assembly.
            Dim assemblyComp = CreateCompilationWithMscorlibAndReferences(source, {metadata.GetReference()}, TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

            Using finalStrm = tempFile.Open()
                success = assemblyComp.Emit(finalStrm)
            End Using
        End Using

        success.Diagnostics.Verify()

        Assert.True(success.Success)
        AssertFileIsSigned(tempFile)
    End Sub

    Private Shared Sub AssertFileIsSigned(file As TempFile)
        ' TODO should check to see that the output was actually signed
        Using peStream = New FileStream(file.Path, FileMode.Open)
            Dim flags = New PEHeaders(peStream).CorHeader.Flags
            Assert.Equal(CorFlags.StrongNameSigned, flags And CorFlags.StrongNameSigned)
        End Using
    End Sub

    <Fact>
    Public Sub SignModuleKeyFileAttr()
        Dim x = KeyPairFile

        Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[<]]>Assembly: System.Reflection.AssemblyKeyFile("<%= x %>")>

Public Class C
End Class
    </file>
</compilation>

        Dim other = CreateCompilationWithMscorlib(source, TestOptions.ReleaseModule.WithStrongNameProvider(DefaultProvider))

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(other.EmitToStream(), AttributeDescription.AssemblyKeyFileAttribute)
    End Sub

    <Fact>
    Public Sub SignModuleKeyContainerAttr()
        Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[<]]>Assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>

Public Class C
End Class
    </file>
</compilation>

        Dim other = CreateCompilationWithMscorlib(source, TestOptions.ReleaseModule.WithStrongNameProvider(DefaultProvider))

        Dim outStrm = New MemoryStream()
        Dim success = other.Emit(outStrm)
        Assert.True(success.Success)

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(outStrm, AttributeDescription.AssemblyKeyNameAttribute)
    End Sub

    <WorkItem(531195, "DevDiv")>
    <Fact>
    Public Sub SignModuleKeyContainerCmdLine()
        Dim source =
<compilation>
    <file name="a.vb">
Public Class C
End Class
    </file>
</compilation>

        Dim other = CreateCompilationWithMscorlib(source, TestOptions.ReleaseModule.WithCryptoKeyContainer("roslynTestContainer").WithStrongNameProvider(DefaultProvider))

        Dim outStrm = New MemoryStream()
        Dim success = other.Emit(outStrm)
        Assert.True(success.Success)

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(outStrm, AttributeDescription.AssemblyKeyNameAttribute)
    End Sub

    <WorkItem(531195, "DevDiv")>
    <Fact>
    Public Sub SignModuleKeyContainerCmdLine_1()
        Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyKeyName("roslynTestContainer")>

Public Class C
End Class
    ]]></file>
</compilation>

        Dim other = CreateCompilationWithMscorlib(source, TestOptions.ReleaseModule.WithCryptoKeyContainer("roslynTestContainer").WithStrongNameProvider(DefaultProvider))

        Dim outStrm = New MemoryStream()
        Dim success = other.Emit(outStrm)
        Assert.True(success.Success)

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(outStrm, AttributeDescription.AssemblyKeyNameAttribute)
    End Sub

    <WorkItem(531195, "DevDiv")>
    <Fact>
    Public Sub SignModuleKeyContainerCmdLine_2()
        Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyKeyName("bogus")>

Public Class C
End Class
    ]]></file>
</compilation>

        Dim other = CreateCompilationWithMscorlib(source, TestOptions.ReleaseModule.WithCryptoKeyContainer("roslynTestContainer").WithStrongNameProvider(DefaultProvider))

        AssertTheseDiagnostics(other,
<expected>
BC37207: Attribute 'System.Reflection.AssemblyKeyNameAttribute' given in a source file conflicts with option 'CryptoKeyContainer'.
</expected>)
    End Sub

    <WorkItem(531195, "DevDiv")>
    <Fact>
    Public Sub SignModuleKeyFileCmdLine()
        Dim source =
<compilation>
    <file name="a.vb">
Public Class C
End Class
    </file>
</compilation>

        Dim other = CreateCompilationWithMscorlib(source, TestOptions.ReleaseModule.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider))

        Dim outStrm = New MemoryStream()
        Dim success = other.Emit(outStrm)
        Assert.True(success.Success)

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(outStrm, AttributeDescription.AssemblyKeyFileAttribute)
    End Sub

    <WorkItem(531195, "DevDiv")>
    <Fact>
    Public Sub SignModuleKeyFileCmdLine_1()
        Dim x = KeyPairFile
        Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[<]]>assembly: System.Reflection.AssemblyKeyFile("<%= x %>")>        

Public Class C
End Class
    </file>
</compilation>

        Dim other = CreateCompilationWithMscorlib(source, TestOptions.ReleaseModule.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider))

        Dim outStrm = New MemoryStream()
        Dim success = other.Emit(outStrm)
        Assert.True(success.Success)

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(outStrm, AttributeDescription.AssemblyKeyFileAttribute)
    End Sub

    <Fact>
    Public Sub SignModuleKeyFileCmdLine_2()
        Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[<]]>assembly: System.Reflection.AssemblyKeyFile("bogus")>        

Public Class C
End Class
    </file>
</compilation>

        Dim other = CreateCompilationWithMscorlib(source, TestOptions.ReleaseModule.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider))

        AssertTheseDiagnostics(other,
<expected>
BC37207: Attribute 'System.Reflection.AssemblyKeyFileAttribute' given in a source file conflicts with option 'CryptoKeyFile'.
</expected>)
    End Sub

    <Fact> <WorkItem(529779, "DevDiv")>
    Public Sub Bug529779_1()

        Dim unsigned As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C1
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Foo()
    Dim x as New System.Guid()
    System.Console.WriteLine(x)
 End Sub
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.ReleaseDll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider))

        CompileAndVerify(other.WithReferences({other.References(0), New VisualBasicCompilationReference(unsigned)})).VerifyDiagnostics()

        CompileAndVerify(other.WithReferences({other.References(0), MetadataReference.CreateFromImage(unsigned.EmitToArray)})).VerifyDiagnostics()
    End Sub

    <Fact> <WorkItem(529779, "DevDiv")>
    Public Sub Bug529779_2()

        Dim unsigned As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation name="Unsigned">
    <file name="a.vb"><![CDATA[
Public Class C1
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Foo()
    Dim x as New C1()
    System.Console.WriteLine(x)
 End Sub
End Class
]]>
    </file>
</compilation>,
        options:=TestOptions.ReleaseDll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider))

        Dim comps = {other.WithReferences({other.References(0), New VisualBasicCompilationReference(unsigned)}),
                     other.WithReferences({other.References(0), MetadataReference.CreateFromImage(unsigned.EmitToArray)})}

        For Each comp In comps
            Dim outStrm = New MemoryStream()
            Dim emitResult = comp.Emit(outStrm)

            ' Dev12 reports an error
            Assert.True(emitResult.Success)

            AssertTheseDiagnostics(emitResult.Diagnostics,
<expected>
BC41997: Referenced assembly 'Unsigned, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' does not have a strong name.
</expected>)
        Next
    End Sub

    <Fact>
    Public Sub AssemblySignatureKeyAttribute_1()
        Dim other As VisualBasicCompilation = CreateCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblySignatureKeyAttribute(
"00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
"bc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819")>

Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, {MscorlibRef_v4_0_30316_17626},
        options:=TestOptions.ReleaseDll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider))

        Dim peHeaders = New PEHeaders(other.EmitToStream())
        Assert.Equal(CorFlags.StrongNameSigned, peHeaders.CorHeader.Flags And CorFlags.StrongNameSigned)
    End Sub

    <Fact>
    Public Sub AssemblySignatureKeyAttribute_2()
        Dim other As VisualBasicCompilation = CreateCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblySignatureKeyAttribute(
"xxx 00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
"bc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819")>

Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, {MscorlibRef_v4_0_30316_17626},
        options:=TestOptions.ReleaseDll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider))

        Dim outStrm = New MemoryStream()
        Dim emitResult = other.Emit(outStrm)

        Assert.False(emitResult.Success)

        AssertTheseDiagnostics(emitResult.Diagnostics,
<expected>
BC37209: Invalid signature public key specified in AssemblySignatureKeyAttribute.
"xxx 00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
    End Sub

    <Fact>
    Public Sub AssemblySignatureKeyAttribute_3()
        Dim other As VisualBasicCompilation = CreateCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblySignatureKeyAttribute(
"00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
"FFFFbc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819")>

Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, {MscorlibRef_v4_0_30316_17626},
        options:=TestOptions.ReleaseDll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider))

        Dim outStrm = New MemoryStream()
        Dim emitResult = other.Emit(outStrm)

        Assert.False(emitResult.Success)

        AssertTheseDiagnostics(emitResult.Diagnostics,
<expected>
BC36980: Error extracting public key from file '<%= KeyPairFile %>': Invalid countersignature specified in AssemblySignatureKeyAttribute. (Exception from HRESULT: 0x80131423)
</expected>)
    End Sub

    <Fact>
    Public Sub AssemblySignatureKeyAttribute_4()
        Dim other As VisualBasicCompilation = CreateCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblySignatureKeyAttribute(
"xxx 00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
"bc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819")>

Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, {MscorlibRef_v4_0_30316_17626},
        options:=TestOptions.ReleaseDll.WithCryptoKeyFile(PublicKeyFile).WithDelaySign(True).WithStrongNameProvider(DefaultProvider))

        Dim outStrm = New MemoryStream()
        Dim emitResult = other.Emit(outStrm)

        Assert.False(emitResult.Success)

        AssertTheseDiagnostics(emitResult.Diagnostics,
<expected>
BC37209: Invalid signature public key specified in AssemblySignatureKeyAttribute.
"xxx 00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
    End Sub

    <Fact>
    Public Sub AssemblySignatureKeyAttribute_5()
        Dim other As VisualBasicCompilation = CreateCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblySignatureKeyAttribute(
"00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
"FFFFbc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819")>

Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, {MscorlibRef_v4_0_30316_17626},
        options:=TestOptions.ReleaseDll.WithCryptoKeyFile(PublicKeyFile).WithDelaySign(True).WithStrongNameProvider(DefaultProvider))

        CompileAndVerify(other)
    End Sub

    <Fact>
    Public Sub AssemblySignatureKeyAttribute_6()
        Dim other As VisualBasicCompilation = CreateCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblySignatureKeyAttribute(
Nothing,
"bc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819")>

Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, {MscorlibRef_v4_0_30316_17626},
        options:=TestOptions.ReleaseDll.WithCryptoKeyFile(PublicKeyFile).WithDelaySign(True).WithStrongNameProvider(DefaultProvider))

        Dim outStrm = New MemoryStream()
        Dim emitResult = other.Emit(outStrm)

        Assert.False(emitResult.Success)

        AssertTheseDiagnostics(emitResult.Diagnostics,
<expected>
BC37209: Invalid signature public key specified in AssemblySignatureKeyAttribute.
Nothing,
~~~~~~~
</expected>)
    End Sub

    <Fact>
    Public Sub AssemblySignatureKeyAttribute_7()
        Dim other As VisualBasicCompilation = CreateCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblySignatureKeyAttribute(
"00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
Nothing)>

Public Class C
 Friend Sub Foo()
 End Sub
End Class
]]>
    </file>
</compilation>, {MscorlibRef_v4_0_30316_17626},
        options:=TestOptions.ReleaseDll.WithCryptoKeyFile(PublicKeyFile).WithDelaySign(True).WithStrongNameProvider(DefaultProvider))

        CompileAndVerify(other)
    End Sub

#End Region

    <Fact, WorkItem(769840, "DevDiv")>
    Public Sub Bug769840()
        Dim ca = CreateCompilationWithMscorlib(
<compilation name="Bug769840_A">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Bug769840_B, PublicKey=0024000004800000940000000602000000240000525341310004000001000100458a131798af87d9e33088a3ab1c6101cbd462760f023d4f41d97f691033649e60b42001e94f4d79386b5e087b0a044c54b7afce151b3ad19b33b332b83087e3b8b022f45b5e4ff9b9a1077b0572ff0679ce38f884c7bd3d9b4090e4a7ee086b7dd292dc20f81a3b1b8a0b67ee77023131e59831c709c81d11c6856669974cc4")>

Friend Class A
	Public Value As Integer = 3
End Class
]]></file>
</compilation>, options:=TestOptions.ReleaseDll.WithStrongNameProvider(DefaultProvider))

        CompileAndVerify(ca)

        Dim cb = CreateCompilationWithMscorlibAndReferences(
<compilation name="Bug769840_B">
    <file name="a.vb"><![CDATA[
Friend Class B
    Public Function GetA() As A
        Return New A()
    End Function
End Class
]]></file>
</compilation>, {New VisualBasicCompilationReference(ca)}, options:=TestOptions.ReleaseModule.WithStrongNameProvider(DefaultProvider))

        CompileAndVerify(cb, verify:=False).Diagnostics.Verify()
    End Sub

End Class
