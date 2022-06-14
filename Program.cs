using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ProgramAnalyzer
{
    class Program
    {
        private static string text2 = @"var x = 1;";
        private static string text = @"void Method()
{
    var x = 1;
    var y = x + 123; 
    var z = y + 132;
    var w = 2, p = 123;
    
    }";
        private static string prefix = "/Users/raviraghavan/Projects/MongoDB_Analyzer/ProgramAnalyzer/SampleCodeFiles";
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");
            String fileName = prefix + "/testClass.cs";
            //analyzePrograms(fileName, true);
            //analyzeTextCode(text);

            MongoClient mongoClient = new MongoClient("mongodb+srv://mflixAppUser:mflixAppPwd@sandbox.wbado.mongodb.net/?retryWrites=true&w=majority&maxPoolSize=50&wtimeoutMS=2500");

            IMongoDatabase database = mongoClient.GetDatabase("sample_mflix");
            IMongoCollection<BsonDocument> movies_collection = database.GetCollection<BsonDocument>("movies");


            //Builders Queries

            //Query #1
            var finder = Builders<BsonDocument>.Filter.Where(x => x["year"] < 1920);
            var reviews = movies_collection.Find<BsonDocument>(finder).ToList();
            foreach(var review in reviews)
            {
                //Console.WriteLine("Review: " + review);
            }


            var secondQuery = Builders<BsonDocument>.Filter.Eq(x => x["type"], "movie");
            var projection = Builders<BsonDocument>.Projection.Include(m => m["year"]);


            var resultFromQuery = movies_collection.Find(secondQuery).Project(projection);


            //Aggregation Pipeline
            var output = movies_collection.Aggregate()
                          .Match(m => m["type"] == "movie")
                          .Group(x => x["year"], g => new { Result = g.Select(x => x["year"]).Count() });


            //Console.WriteLine(output);

            //LINQ Queries
            //var query = movies_collection.AsQueryable()
            //.Select(p => new { p["type"], p["year"] });


            analyzeTextCode(text);


        }

        static void analyzePrograms(String fileName, bool file)
        {
            string testScript = File.ReadAllText(fileName);
            if (!file) { testScript = text; }
            //Find the "using" statements
            SyntaxTree AST = CSharpSyntaxTree.ParseText(testScript);

            CompilationUnitSyntax root = AST.GetCompilationUnitRoot();
            var compilation = CSharpCompilation.Create("HelloWorld")
            .AddReferences(MetadataReference.CreateFromFile(
                typeof(string).Assembly.Location))
            .AddSyntaxTrees(AST);


            SemanticModel model = compilation.GetSemanticModel(AST);


            SyntaxList<UsingDirectiveSyntax> usingDirectives = root.Usings;
            SyntaxNode syntaxRoot = root.SyntaxTree.GetRoot();
            //Console.WriteLine($"{syntaxRoot.Kind()}");
            SymbolInfo rootSymbolInfo = model.GetSymbolInfo(root);
            Console.WriteLine(rootSymbolInfo.Symbol);
            foreach (UsingDirectiveSyntax usingDirective in usingDirectives)
            {
                NameSyntax name = usingDirective.Name;
                Console.WriteLine($"This Statement is using {name.ToString()}");

                SymbolInfo nameInfo = model.GetSymbolInfo(name);
                var nameSymbol = nameInfo.Symbol;
                if (nameSymbol == null)
                {
                    continue;
                }
                var systemSymbol = (INamespaceSymbol)nameInfo.Symbol;
                //Console.WriteLine($"This Namespace is {systemSymbol.ToString()}");
                //foreach (INamespaceSymbol ns in systemSymbol.GetNamespaceMembers())
                //{
                //    Console.WriteLine(ns);
                //}

            }

            Console.WriteLine($"The root is {root.Kind()}");


            foreach (MemberDeclarationSyntax mds in root.Members)
            {
                SyntaxKind memberType = mds.Kind();

                if (memberType == SyntaxKind.NamespaceDeclaration)
                {
                    NamespaceDeclarationSyntax NDS = (NamespaceDeclarationSyntax)(mds);
                    Console.WriteLine($"This member is {mds.Kind()} and the name of this namespace is {NDS.Name}");
                    analyzeNamespaces(NDS, model);
                }
                else if (memberType == SyntaxKind.ClassDeclaration)
                {
                    ClassDeclarationSyntax CDS = (ClassDeclarationSyntax)(mds);
                    Console.WriteLine($"This member is {mds.Kind()} and the name of this Class is {CDS.Identifier}");
                    analyzeClasses(CDS, model);
                }
                else if(memberType == SyntaxKind.MethodDeclaration)
                {
                    MethodDeclarationSyntax MDS = (MethodDeclarationSyntax)(mds);
                    Console.WriteLine($"This member is {mds.Kind()} and the name of this Method is {MDS.Identifier}");
                    analyzeMethods(MDS, model);
                }
                else if(memberType == SyntaxKind.GlobalStatement)
                {
                    Console.WriteLine($"This member is {mds.Kind()}");

                    //int descendantNodes = mds.DescendantNodes().OfType<MethodDeclarationSyntax>().Count();
                    //Console.WriteLine($"{descendantNodes} is this number");
                    //try
                    //{
                    //    MethodDeclarationSyntax method = (MethodDeclarationSyntax)(mds);
                    //}
                    //catch(Exception exe)
                    //{
                    //    Console.WriteLine("ERROR");
                    //}
                    analyzeTextCode(testScript);
                }
                else
                {
                    Console.WriteLine($"This member is {mds.Kind()}");
                }
            }
        }
        
        static void analyzeNamespaces(NamespaceDeclarationSyntax parameter, SemanticModel model)
        {
            foreach (MemberDeclarationSyntax mds in parameter.Members)
            {
                SyntaxKind memberType = mds.Kind();
                if (memberType == SyntaxKind.NamespaceDeclaration)
                {
                    NamespaceDeclarationSyntax NDS = (NamespaceDeclarationSyntax)(mds);
                    Console.WriteLine($"This member is {mds.Kind()} and the name of this namespace is {NDS.Name}");
                    analyzeNamespaces(NDS, model);
                }
                else if (memberType == SyntaxKind.ClassDeclaration)
                {
                    ClassDeclarationSyntax CDS = (ClassDeclarationSyntax)(mds);
                    Console.WriteLine($"This member is {mds.Kind()} and the name of this Class is {CDS.Identifier}");
                    analyzeClasses(CDS, model);
                }

            }
        }

        static void analyzeClasses(ClassDeclarationSyntax parameter, SemanticModel model)
        {
            foreach (MemberDeclarationSyntax mds in parameter.Members)
            {
                SyntaxKind memberType = mds.Kind();
                if (memberType == SyntaxKind.NamespaceDeclaration)
                {
                    NamespaceDeclarationSyntax NDS = (NamespaceDeclarationSyntax)(mds);
                    Console.WriteLine($"This member is {mds.Kind()} and the name of this namespace is {NDS.Name}");
                    analyzeNamespaces(NDS, model);
                }
                else if (memberType == SyntaxKind.ClassDeclaration)
                {
                    ClassDeclarationSyntax CDS = (ClassDeclarationSyntax)(mds);
                    Console.WriteLine($"This member is {mds.Kind()} and the name of this Class is {CDS.Identifier}");
                    analyzeClasses(CDS, model);
                }
                else if (memberType == SyntaxKind.MethodDeclaration)
                {
                    MethodDeclarationSyntax MDS = (MethodDeclarationSyntax)(mds);
                    Console.WriteLine($"This member is {mds.Kind()} and the name of this Method is {MDS.Identifier}");
                    analyzeMethods(MDS, model);
                }
                else if (memberType == SyntaxKind.ConstructorDeclaration)
                {
                    ConstructorDeclarationSyntax constructor = (ConstructorDeclarationSyntax)(mds);
                    Console.WriteLine($"This member is {mds.Kind()} and the name of this Constructor is {constructor.Identifier}");
                    analyzeConstructor(constructor, model);
                }
                else if(memberType == SyntaxKind.FieldDeclaration)
                {
                    FieldDeclarationSyntax fds = (FieldDeclarationSyntax)(mds);
                    foreach(var variable in fds.Declaration.Variables)
                    {
                        var fieldSymbol = (IFieldSymbol)model.GetDeclaredSymbol(variable);
                        if(fieldSymbol == null) { continue; }
                        Console.WriteLine($"There is a field with name {fieldSymbol.Name} found in class {parameter.Identifier}");
                        if (fieldSymbol.IsConst)
                        {
                            Console.WriteLine($"Value of this Field is: {fieldSymbol.ConstantValue}");
                        }
                    }
                    //SymbolInfo symbolInfo = model.GetSymbolInfo(fds);
                    //IFieldSymbol fieldSymbol = (IFieldSymbol)symbolInfo.Symbol;
                    //if (fieldSymbol == null) { continue; }
                    //Console.WriteLine($"Field found in class {parameter.Identifier}");
                    //Console.WriteLine($"Value of this Field is: {fieldSymbol.ConstantValue}");
                }

            }
        }

        static void analyzeMethods(MethodDeclarationSyntax parameter, SemanticModel model)
        {
            SymbolInfo symbolInfo = model.GetSymbolInfo(parameter);

            Console.WriteLine("---------------------------------------------");
            Console.WriteLine($"This Method is called {parameter.Identifier}");
            //Console.WriteLine($"Is this Method Static: {symbolInfo.Symbol.IsStatic}");
            IEnumerable<SyntaxNode> descendentNodes = parameter.DescendantNodes();
            IEnumerable<VariableDeclarationSyntax> variableDeclarations = descendentNodes.OfType<VariableDeclarationSyntax>();
            IEnumerable<AssignmentExpressionSyntax> assignments = descendentNodes.OfType<AssignmentExpressionSyntax>();

            Console.WriteLine("============Variable Declarations==========");
            foreach (VariableDeclarationSyntax variableDeclaration in variableDeclarations)
            {
                VariableDeclarationSyntax declaration = variableDeclaration;
                SeparatedSyntaxList<VariableDeclaratorSyntax> list = declaration.Variables;
                IEnumerable<AssignmentExpressionSyntax> innerAssignments = variableDeclaration.DescendantNodes().OfType<AssignmentExpressionSyntax>();
                foreach (var variable in list)
                {
                    Console.WriteLine("Declared Variable: " + variable.Identifier);
                    var text = list.ElementAt(0).ToString();
                    var assignmentTree = CSharpSyntaxTree.ParseText(text);

                    var nestedRoot = assignmentTree.GetRoot();

                    IEnumerable<SyntaxNode> nestedDescendantNodes = nestedRoot.DescendantNodes();

                    IEnumerable<AssignmentExpressionSyntax> nestedAssignments = nestedDescendantNodes.OfType<AssignmentExpressionSyntax>();
                    foreach (var nestedAssignment in nestedAssignments)
                    {
                        Console.WriteLine($"{nestedAssignment.Left} is set equal to {nestedAssignment.Right}");
                    }
                }
                Console.WriteLine("-------");
            }
            Console.WriteLine("===========Other Variable Assignments ===========");
            foreach (var variableAssignment in assignments)
            {
                //AssignmentExpressionSyntax assignment = variableAssignment;
                Console.WriteLine($"{variableAssignment.Left} is set equal to {variableAssignment.Right}");
            }
            Console.WriteLine("---------------------------------------------");

        }

        static void analyzeConstructor(ConstructorDeclarationSyntax parameter, SemanticModel sm)
        {
            Console.WriteLine($"This is a Constructor with name {parameter.Identifier}");
        }

        static void analyzeTextCode(String textCode)
        {
            SyntaxTree AST = CSharpSyntaxTree.ParseText(textCode);

            CompilationUnitSyntax root = AST.GetCompilationUnitRoot();
            var compilation = CSharpCompilation.Create("HelloWorld")
            .AddReferences(MetadataReference.CreateFromFile(
                typeof(string).Assembly.Location))
            .AddSyntaxTrees(AST);
            IEnumerable<SyntaxNode> descendentNodes = root.DescendantNodes();
            IEnumerable<VariableDeclarationSyntax> variableDeclarations = descendentNodes.OfType<VariableDeclarationSyntax>();
            IEnumerable<AssignmentExpressionSyntax> assignments = descendentNodes.OfType<AssignmentExpressionSyntax>();

            SemanticModel model = compilation.GetSemanticModel(AST);

            Dictionary<string, string> map = new Dictionary<string, string>();
            //Console.WriteLine("============Variable Declarations==========");
            foreach (VariableDeclarationSyntax variableDeclaration in variableDeclarations)
            {
                VariableDeclarationSyntax declaration = variableDeclaration;
                SeparatedSyntaxList<VariableDeclaratorSyntax> list = declaration.Variables;
                IEnumerable<AssignmentExpressionSyntax> innerAssignments = variableDeclaration.DescendantNodes().OfType<AssignmentExpressionSyntax>();
                foreach (var variable in list)
                {
                    Console.WriteLine("Declared Variable: " + variable.Identifier);
                    var text = list.ElementAt(0).ToString();
                    var assignmentTree = CSharpSyntaxTree.ParseText(text);

                    var nestedRoot = assignmentTree.GetRoot();

                    IEnumerable<SyntaxNode> nestedDescendantNodes = nestedRoot.DescendantNodes();

                    IEnumerable<AssignmentExpressionSyntax> nestedAssignments = nestedDescendantNodes.OfType<AssignmentExpressionSyntax>();
                    foreach (var nestedAssignment in nestedAssignments)
                    {
                        map[nestedAssignment.Left.ToString()] = nestedAssignment.Right.ToString();

                        //Console.WriteLine($"{nestedAssignment.Left} is set equal to {nestedAssignment.Right}");
                    }
                }
                //Console.WriteLine("-------");
            }
            //Assignment Printouts
            foreach (KeyValuePair<string, string> entry in map)
            {
                // do something with entry.Value or entry.Key
                Console.WriteLine($"{entry.Key} : {entry.Value}");
            }

            if (assignments.Count() > 0)
            {
                Console.WriteLine("===========Other Variable Assignments ===========");
                foreach (var variableAssignment in assignments)
                {
                    //AssignmentExpressionSyntax assignment = variableAssignment;
                    Console.WriteLine($"{variableAssignment.Left} is set equal to {variableAssignment.Right}");
                }
                Console.WriteLine("---------------------------------------------");
            }
        }
    }
}

