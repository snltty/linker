using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace linker.gen
{

    [Generator(LanguageNames.CSharp)]
    public class InterfaceSourceGenerator : IIncrementalGenerator
    {
        private List<GeneratorInfo> generators = new List<GeneratorInfo> {
             new GeneratorInfo{ ClassName="linker.plugins.flow.FlowTypesLoader", InterfaceName="linker.plugins.flow.IFlow" },
             new GeneratorInfo{ ClassName="linker.plugins.relay.client.RelayTypesLoader", InterfaceName="linker.plugins.relay.client.transport.ITransport" },
             new GeneratorInfo{ ClassName="linker.plugins.relay.server.validator.RelayValidatorTypeLoader", InterfaceName="linker.plugins.relay.server.validator.IRelayValidator" },
             new GeneratorInfo{ ClassName="linker.plugins.signIn.args.SignInArgsTypesLoader", InterfaceName="linker.plugins.signIn.args.ISignInArgs" },
             new GeneratorInfo{ ClassName="linker.plugins.resolver.ResolverTypesLoader", InterfaceName="linker.plugins.resolver.IResolver" },
             new GeneratorInfo{ ClassName="linker.plugins.tunnel.excludeip.TunnelExcludeIPTypesLoader",  InterfaceName="linker.plugins.tunnel.excludeip.ITunnelExcludeIP" },
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
                            .DescendantNodes(descendIntoTrivia: false)
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

                    var spaces = info.ClassName.Split('.');

                    var source = $@"
                    using System;
                    using System.Collections.Generic;
                    {string.Join("\r\n", namespaces)}

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


        public sealed class GeneratorInfo
        {
            public string ClassName { get; set; }
            public string InterfaceName { get; set; }
            public bool Instance { get; set; }
        }
    }
}
