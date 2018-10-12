﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using OData2Poco.CustAttributes;
using OData2Poco.TextTransform;

namespace OData2Poco
{
    /// <summary>
    ///     Generate c# code
    ///     PocoClassGeneratorCs(IPocoGenerator pocoGen, PocoSetting setting = null)
    ///     called from MetaDataReader class
    /// </summary>
    internal class PocoClassGeneratorCs : IPocoClassGenerator
    {
        private static IPocoGenerator _pocoGen;
        public IDictionary<string, ClassTemplate> PocoModel { get;  set; } //= new Dictionary<string, ClassTemplate>();
        public string PocoModelAsJson
        {
            get
            {
                return JsonConvert.SerializeObject(PocoModel, Formatting.Indented);
            }
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="pocoGen"></param>
        /// <param name="setting"></param>
        public PocoClassGeneratorCs(IPocoGenerator pocoGen, PocoSetting setting = null)
        {
            PocoSetting = setting ?? new PocoSetting();
            AttributeFactory.Default.Init(PocoSetting);
            _pocoGen = pocoGen;
            PocoModel = new Dictionary<string, ClassTemplate>();
            Template = new FluentCsTextTemplate();
            var list = _pocoGen.GeneratePocoList(); //generate all classes from model
            if (list != null)
                foreach (var item in list)
                {
                    PocoModel[item.Name] = item;
                }

            //Console.WriteLine("PocoClassGeneratorCs constructor key: {0}", PocoSetting.AddKeyAttribute);
            CodeText = null;
        }

        private static string CodeText { get; set; }
        public FluentCsTextTemplate Template { get; private set; }
        public PocoSetting PocoSetting { get; set; }

        /// <summary>
        ///     Indexer
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ClassTemplate this[string key]
        {
            get { return PocoModel[key]; }
        }

        //container for all classes
        public List<ClassTemplate> ClassList
        {
            get { return PocoModel.Select(kvp => kvp.Value).ToList(); }
        }

    
        /// <summary>
        ///     Generate C# code for all POCO classes in the model
        /// </summary>
        /// <returns></returns>
        public string GeneratePoco()
        {
            Template.WriteLine(GetHeader()); //header of the file (using xxx;....)

            //var json = JsonConvert.SerializeObject(_classDictionary, Formatting.Indented); //data for test cases
            //File.WriteAllText("northmodel.json",json);

            foreach (var item in PocoModel)
            {
                Template.WriteLine(ClassToString(item.Value)); //c# code of the class
                //Console.WriteLine(ClassToString(item.Value));
            }
            Template.EndNamespace();
            return Template.ToString();
        }

        private string GetHeader()
        {
            var comment = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated using  OData2Poco Class library.
//     Service Url: {0}
//     MetaData Version: {1}
//     Generated On: {2}
// </auto-generated>
//------------------------------------------------------------------------------
";
            //Use a user supplied namespace prefix combined with the schema namepace or just the schema namespace
            var namespc = _pocoGen.MetaData.SchemaNamespace;
            if (!string.IsNullOrWhiteSpace(PocoSetting.NamespacePrefix))
            {
                namespc = (PocoSetting.NamespacePrefix + "." + _pocoGen.MetaData.SchemaNamespace).Replace("..", ".");
                namespc = namespc.TrimEnd('.');
            }


            //Ensure the <auto-generated> tag is at the start of the file, and enclose all usings in a namespace
            var h = new FluentCsTextTemplate();
            h.WriteLine(comment, _pocoGen.MetaData.ServiceUrl, _pocoGen.MetaData.MetaDataVersion,
                DateTimeOffset.Now.ToString("s"))
                .StartNamespace(namespc);
            //.UsingNamespace("System")
            //.UsingNamespace("System.Collections.Generic");

            var assemplyManager = new AssemplyManager(PocoSetting, PocoModel);
            var asemplyList = assemplyManager.AssemplyReference;
            foreach (var entry in asemplyList)
            {
                h.UsingNamespace(entry);
            }

            return h.ToString();
        }


        /// <summary>
        ///     Generte C# code for a given  Entity using FluentCsTextTemplate
        /// </summary>
        /// <param name="ent"> Class  to generate code</param>
        /// <param name="includeNamespace"></param>
        /// <returns></returns>
        internal string ClassToString(ClassTemplate ent, bool includeNamespace = false)
        {
            var csTemplate = new FluentCsTextTemplate();
            if (includeNamespace) csTemplate.WriteLine(GetHeader());


            ////for enum
            if (ent.IsEnum)
            {
                var elements = string.Join(",\r\n ", ent.EnumElements.ToArray());
                //var enumString = string.Format("public enum {0} {{ {1} }}", ent.Name, elements);
                var enumString = $"\tpublic enum {ent.Name}\r\n\t {{\r\n {elements} \r\n\t}}";
                return enumString;
            }


            //v 2.2
            //foreach (var item in ent.GetAttributes(PocoSetting))
            foreach (var item in ent.GetAllAttributes()) //not depend on pocosetting
            {
                csTemplate.PushIndent("\t").WriteLine(item).PopIndent();
            }
            var baseClass = ent.BaseType != null && PocoSetting.UseInheritance ? ent.BaseType : PocoSetting.Inherit;

            csTemplate.StartClass(ent.Name, baseClass);
            //   csTemplate.StartClass(ent.Name, PocoSetting.Inherit, partial:true); //delayed to a future release to avoid change of most test cases
            foreach (var p in ent.Properties)
            {
                var pp = new PropertyGenerator(p, PocoSetting);

                //@@@ v1.0.0-rc3
                // navigation properties
                //v1.4 skip
                //if (p.IsNavigate) continue;

                //v1.5
                if (p.IsNavigate)
                {
                    //Console.WriteLine("navigation entity {0}  prop: {1}",ent.Name, p.PropName);
                    if (!PocoSetting.AddNavigation && !PocoSetting.AddEager) continue;
                }


                foreach (var item in pp.GetAllAttributes())
                {
                    csTemplate.WriteLine(item);
                }
                csTemplate.WriteLine(pp.Declaration);
                //Console.WriteLine(pp.ToString());
            }
            csTemplate.EndClass();
            if (includeNamespace) csTemplate.EndNamespace(); //"}" for namespace
            CodeText = csTemplate.ToString();
            return CodeText;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(CodeText)) CodeText = GeneratePoco();
            return CodeText;
        }
    }
}