﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

namespace EntityFrameworkZip.GeneratedCode;

public static class CodeCompiler
{
    /// <summary>
    /// Compiles a chunk of C# code in to a Assembly
    /// </summary>
    /// <param name="code">The C# code</param>
    /// <returns>The assembly</returns>
    public static Assembly Compile(string code)
    {
        try
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var refs = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>();

            var compilation = CSharpCompilation.Create(
                "GeneratedCodeLibrary",
                [syntaxTree],
                refs,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);
            if (!result.Success)
            {
                var errors = string.Join("\n", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
                throw new Exception($"Compile error:\n{errors}");
            }

            ms.Seek(0, SeekOrigin.Begin);
            return Assembly.Load(ms.ToArray());
        }
        catch (Exception ex)
        {
            throw new Exception($"Exception while compiling data model. The generated code:\r\n {code}\r\n\r\n" +
                $"Exception while compiling: \r\n\r\n{ex}", ex);
        }
    }
}
