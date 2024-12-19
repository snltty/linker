using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Diagnostics;

namespace linker.gen
{

    [Generator(LanguageNames.CSharp)]
    public class InterfaceSourceGenerator : IIncrementalGenerator
    {
        private List<GeneratorInfo> generators = new List<GeneratorInfo> {
             new GeneratorInfo{ ClassName="linker.plugins.flow.FlowTypesLoader", InterfaceName="linker.plugins.flow.IFlow" },
             new GeneratorInfo{ ClassName="linker.plugins.relay.client.RelayClientTypesLoader", InterfaceName="linker.messenger.relay.client.transport.IRelayClientTransport" },
             new GeneratorInfo{ ClassName="linker.plugins.relay.server.validator.RelayServerValidatorTypeLoader", InterfaceName="linker.messenger.relay.server.validator.IRelayServerValidator" },
             new GeneratorInfo{ ClassName="linker.plugins.signIn.args.SignInArgsTypesLoader", InterfaceName="linker.messenger.signin.ISignInArgs" },
             new GeneratorInfo{ ClassName="linker.plugins.resolver.ResolverTypesLoader", InterfaceName="linker.plugins.resolver.IResolver" },
             new GeneratorInfo{ ClassName="linker.plugins.tunnel.TunnelExcludeIPTypesLoader",  InterfaceName="linker.messenger.tunnel.ITunnelExcludeIP" },
             new GeneratorInfo{ ClassName="linker.startup.StartupTransfer", InterfaceName="linker.startup.IStartup", Instance=true },
             new GeneratorInfo{ ClassName="linker.plugins.messenger.MessengerResolverTypesLoader", InterfaceName="linker.messenger.IMessenger"},
             new GeneratorInfo{ ClassName="linker.plugins.capi.ApiClientTypesLoader",InterfaceName="linker.plugins.capi.IApiClientController"},
             new GeneratorInfo{ ClassName="linker.plugins.config.ConfigSyncTypesLoader",  InterfaceName="linker.plugins.config.IConfigSync"},
             new GeneratorInfo{ ClassName="linker.plugins.decenter.DecenterTypesLoader",  InterfaceName="linker.plugins.decenter.IDecenter"},
             new GeneratorInfo{ ClassName="linker.plugins.route.RouteExcludeIPTypesLoader", InterfaceName="linker.plugins.route.IRouteExcludeIP" },
        };

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValueProvider<Compilation> compilations = context.CompilationProvider.Select((compilation, cancellationToken) => compilation);

            context.RegisterSourceOutput(compilations, (sourceProductionContext, compilation) =>
            {
               

                foreach (GeneratorInfo info in generators)
                {
                    var iFlowSymbol = compilation.GetTypeByMetadataName(info.InterfaceName);
                    List<string> types = new List<string> { };
                    List<string> classs = new List<string> { };
                    List<string> namespaces = new List<string> { };
                    
                    foreach (var syntaxTree in compilation.SyntaxTrees)
                    {
                        if (syntaxTree == null)
                        {
                            continue;
                        }
                        var root = syntaxTree.GetRoot(sourceProductionContext.CancellationToken);
                        var classDeclarationSyntaxs = root
                            .DescendantNodes(descendIntoTrivia: true)
                            .OfType<ClassDeclarationSyntax>();

                        foreach (var classDeclarationSyntax in classDeclarationSyntaxs)
                        {
                            var model = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
                            var classSymbol = model.GetDeclaredSymbol(classDeclarationSyntax) as INamedTypeSymbol;
                            if (classSymbol.AllInterfaces.Contains(iFlowSymbol))
                            {
                                types.Add($"typeof({classDeclarationSyntax.Identifier.Text})");

                                if (info.Instance)
                                    classs.Add($"new {classDeclarationSyntax.Identifier.Text}()");

                                var namespaceDecl = classDeclarationSyntax.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
                                if (namespaceDecl != null)
                                {
                                    namespaces.Add($"using {namespaceDecl.Name.ToString()};");
                                }
                            }
                        }
                    }
                    

                    /*
                    var referencedAssemblySymbols = compilation.SourceModule.ReferencedAssemblySymbols;
                    foreach (IAssemblySymbol referencedAssemblySymbol in referencedAssemblySymbols)
                    {
                        var allTypeSymbol = GetAllTypeSymbol(referencedAssemblySymbol.GlobalNamespace).SelectMany(c=>c.DeclaringSyntaxReferences).Select(c=>c.SyntaxTree).ToList();
                        foreach (var item in allTypeSymbol)
                        {
                            var root = item.GetRoot(sourceProductionContext.CancellationToken);
                            var classDeclarationSyntaxs = root
                                .DescendantNodes(descendIntoTrivia: true)
                                .OfType<ClassDeclarationSyntax>();

                            foreach (var classDeclarationSyntax in classDeclarationSyntaxs)
                            {
                                var model = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
                                var classSymbol = model.GetDeclaredSymbol(classDeclarationSyntax) as INamedTypeSymbol;
                                if (classSymbol.AllInterfaces.Contains(iFlowSymbol))
                                {
                                    types.Add($"typeof({classDeclarationSyntax.Identifier.Text})");

                                    if (info.Instance)
                                        classs.Add($"new {classDeclarationSyntax.Identifier.Text}()");

                                    var namespaceDecl = classDeclarationSyntax.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
                                    if (namespaceDecl != null)
                                    {
                                        namespaces.Add($"using {namespaceDecl.Name.ToString()};");
                                    }
                                }
                            }
                        }
                    }
                    */

                    var spaces = info.ClassName.Split('.');

                    var source = $@"
                    using System;
                    using System.Collections.Generic;
                    {string.Join("\r\n", namespaces.Distinct())}

                    namespace {string.Join(".", spaces.Take(spaces.Count() - 1))}
                    {{
                        public partial class {spaces.LastOrDefault()}
                        {{
                             public static List<Type> GetSourceGeneratorTypes()
                             {{
                                return new List<Type> {{
                                    {string.Join(",", types)}
                                }};
                             }}
                             public static List<{info.InterfaceName}> GetSourceGeneratorInstances()
                             {{
                                return new List<{info.InterfaceName}> {{
                                    {string.Join(",", classs)}
                                }};
                             }}
                        }}
                    }}";

                    var sourceText = SourceText.From(source, Encoding.UTF8);
                    sourceProductionContext.AddSource($"{info.ClassName}Instances.g.cs", sourceText);
                }
            });
        }
        private static IEnumerable<INamedTypeSymbol> GetAllTypeSymbol(INamespaceSymbol namespaceSymbol)
        {
            var typeMemberList = namespaceSymbol.GetTypeMembers();

            foreach (var typeSymbol in typeMemberList)
            {
                yield return typeSymbol;
            }

            foreach (var namespaceMember in namespaceSymbol.GetNamespaceMembers())
            {
                foreach (var typeSymbol in GetAllTypeSymbol(namespaceMember))
                {
                    yield return typeSymbol;
                }
            }
        }

        public sealed class GeneratorInfo
        {
            public string ClassName { get; set; }
            public string InterfaceName { get; set; }
            public bool Instance { get; set; }
        }
    }
}
