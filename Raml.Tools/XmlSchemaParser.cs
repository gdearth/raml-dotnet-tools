﻿using Microsoft.CSharp;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using Raml.Common;

namespace Raml.Tools
{
    public class XmlSchemaParser
    {
        public ApiObject Parse(string key, string schema, IDictionary<string, ApiObject> objects, string targetNamespace)
        {
            var codeNamespace = Process(schema, targetNamespace + ".Models");
            
            var code = GenerateCode(codeNamespace);
            
            if(HasDuplicatedObjects(objects, codeNamespace))
                return null;

            if(codeNamespace.Types.Count == 0)
                return null;

            return new ApiObject { Name = NetNamingMapper.GetObjectName(key), GeneratedCode = code };
        }


        public static CodeNamespace Process(string xsdSchema, string targetNamespace)
        {
            // Load the XmlSchema and its collection.
            XmlSchema xsd;
            using (var fs = new StringReader(xsdSchema))
            {
                xsd = XmlSchema.Read(fs, null);
                xsd.Compile(null);
            }
            XmlSchemas schemas = new XmlSchemas();
            schemas.Add(xsd);
            // Create the importer for these schemas.
            XmlSchemaImporter importer = new XmlSchemaImporter(schemas);
            // System.CodeDom namespace for the XmlCodeExporter to put classes in.
            CodeNamespace ns = new CodeNamespace(targetNamespace);
            XmlCodeExporter exporter = new XmlCodeExporter(ns);
            // Iterate schema top-level elements and export code for each.
            foreach (XmlSchemaElement element in xsd.Elements.Values)
            {
                // Import the mapping first.
                XmlTypeMapping mapping = importer.ImportTypeMapping(
                  element.QualifiedName);
                // Export the code finally.
                exporter.ExportTypeMapping(mapping);
            }
            return ns;
        }


        private static string GenerateCode(CodeNamespace codeNamespace)
        {
            var codeProvider = new CSharpCodeProvider();

            string code;
            using (var writer = new StringWriter())
            {
                codeProvider.GenerateCodeFromNamespace(codeNamespace, writer, new CodeGeneratorOptions());
                code = writer.GetStringBuilder().ToString();
            }
            return code;
        }

        private static bool HasDuplicatedObjects(IDictionary<string, ApiObject> objects, CodeNamespace codeNamespace)
        {
            foreach (CodeTypeDeclaration typeDeclaration in codeNamespace.Types)
            {
                var obj = new ApiObject {Name = typeDeclaration.Name};

                if (objects.ContainsKey(obj.Name) || objects.Any(o => o.Value.Name == obj.Name))
                    return true;
                
                objects.Add(obj.Name, obj);
            }
            return false;
        }

    }
}