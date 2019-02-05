﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OData2Poco.Api;
using OData2Poco.Extensions;
using OData2Poco.InfraStructure.FileSystem;
using OData2Poco.InfraStructure.Logging;

namespace OData2Poco.CommandLine
{
    /// <summary>
    ///     Command Pattern to manage all options of commandline
    /// </summary>
    internal class CsCommand : IPocoCommand
    {
        public readonly Options ArgOptions;
        public PocoSetting PocoSettingOptions;
        public string Code { get; private set; }
        private readonly ColoredConsole _logger = ColoredConsole.Default;
        public O2P O2PGen { get; set; }
        public List<string> Errors; //model generation errors
        private IPocoFileSystem _fileSystem;

        public CsCommand(Options options, IPocoFileSystem fileSystem)
        {
            if (fileSystem == null)
                _fileSystem = new NullFileSystem();
            else
            {
                _fileSystem = fileSystem;
            }

            Errors = new List<string>();

            ArgOptions = options;
            O2PGen = new O2P(config =>
            {
                config.AddNavigation = options.Navigation;
                config.AddNullableDataType = options.AddNullableDataType;
                config.AddEager = options.Eager;
                config.Inherit = string.IsNullOrWhiteSpace(options.Inherit) ? null : options.Inherit;
                config.NamespacePrefix = string.IsNullOrEmpty(options.Namespace) ? string.Empty : options.Namespace;
                config.NameCase = options.NameCase.ToCaseEnum();
                config.Attributes = options.Attributes?.ToList();

                config.AddKeyAttribute = options.Key;
                config.AddTableAttribute = options.Table;
                config.AddRequiredAttribute = options.Required;
                config.AddJsonAttribute = options.AddJsonAttribute;

            });
            PocoSettingOptions = O2PGen.Setting;
        }



        public async Task Execute()
        {

            ShowOptions();
            Console.WriteLine();
            if (ArgOptions.Validate() < 0)
            {
                ArgOptions.Errors.ForEach(x =>
                {
                    _logger.Error(x);
                });
                return;
            }

            //show warning
            ArgOptions.Errors.ForEach(x =>
                {
                    _logger.Warn(x);
                });


            _logger.Info($"Start processing url: {ArgOptions.Url}");
            //show result
            await GenerateCodeCommandAsync();
            ServiceInfo();

            SaveMetaDataCommand();
            ShowHeaderCommand();
            ListPocoCommand();
            VerboseCommand();
            ShowErrors();

        }
        public void ShowOptions(Options option)
        {
            //format option as: -n Navigation= True
            _logger.Normal("************* CommandLine options***********");
            var list = CommandLineUtility.GetOptions(option);
            list.ForEach(x => _logger.Normal(x));
            _logger.Normal("********************************************");
        }
        public void ShowErrors()
        {
            if (Errors.Count == 0) return;
            _logger.Error("--------- Errors--------");
            Errors.ForEach(x =>
            {
                _logger.Error(x);
            });
        }
        public void ServiceInfo()
        {
            _logger.Normal($"{new string('-', 15)}Service Information {new string('-', 15)}");
            _logger.Info($"OData Service Url: {ArgOptions.Url} ");
            _logger.Info($"OData Service Version: {O2PGen.MetaDataVersion} ");
            _logger.Info($"Number of Entities: {O2PGen.ClassList.Count}");
            _logger.Normal(new string('-', 50));
            _logger.Sucess("Success creation of the Poco Model");
        }

        public void ShowOptions()
        {
            ShowOptions(ArgOptions);
        }


        #region Utility



        private void SaveToFile(string fileName, string text)
        {
            _fileSystem.SaveToFile(fileName, text);
        }

        #endregion

        #region commands

        private void ListPocoCommand()
        {
            //---------list -l 
            if (!ArgOptions.ListPoco) return;

            Console.WriteLine();
            _logger.Info($"POCO classes (count: {O2PGen.ClassList.Count}");
            _logger.Normal(new string('=', 20));
            var items = O2PGen.ClassList.OrderBy(m => m.Name).ToList();
            items.ForEach(m =>
            {
                var index = items.IndexOf(m);
                var remoteUrl = string.IsNullOrEmpty(m.EntitySetName) ? "" : ArgOptions.Url + @"/" + m.EntitySetName;
                //v1.5
                _logger.Normal($"{index + 1}: {m.Name} {remoteUrl}");
            });
        }

        private void VerboseCommand()
        {
            //---------verbose -v
            if (!ArgOptions.Verbose) return;

            Console.WriteLine();
            if (!string.IsNullOrEmpty(Code))
            {
                _logger.Normal("---------------Code Generated--------------------------------");
                _logger.Normal(Code);
            }
            else
            {
                _logger.Error("Code not generated");
            }
        }

        private void ShowHeaderCommand()
        {
            //------------ header -h for http media only not file--------------------
            if (ArgOptions.Header && ArgOptions.Url.StartsWith("http"))
            {
                Console.WriteLine();
                _logger.Normal("HTTP Header");
                _logger.Normal(new string('=', 15));
                O2PGen.ServiceHeader.ToList().ForEach(m => { _logger.Normal($" {m.Key}: {m.Value}"); });
            }
        }

        private async Task GenerateCodeCommandAsync()
        {

            if (ArgOptions.Url.StartsWith("http"))
            {
                Code = await O2PGen.GenerateAsync(new Uri(ArgOptions.Url), ArgOptions.User, ArgOptions.Password);
            }
            else
            {
                var xml = File.ReadAllText(ArgOptions.Url);
                Code = O2PGen.Generate(xml);
            }


            if (ArgOptions.Lang == "cs")
            {
                _logger.Info("Saving generated CSharp code to file : " + ArgOptions.CodeFilename);
                SaveToFile(ArgOptions.CodeFilename, Code);
                _logger.Confirm("CSharp code  is generated Successfully.");
            }
            else if (ArgOptions.Lang == "vb")
            {
                //vb.net
                Code = await VbCodeConvertor.CodeConvert(Code); //convert to vb.net
                if (!string.IsNullOrEmpty(Code))
                {
                    var filename = Path.ChangeExtension(ArgOptions.CodeFilename, ".vb");
                    _logger.Normal("Saving generated VB.NET code to file : " + ArgOptions.CodeFilename);
                    SaveToFile(filename, Code);
                    _logger.Confirm("VB.NET code  is generated Successfully.");
                }
                else
                {
                    _logger.Warn("Vb Service Converter isn't available.");
                }
            }
            else
            {
                _logger.Warn($"Lang option: '{ArgOptions.Lang}' isn't valid. Only cs or vb are accepted \r\n No code is generated");
                Code = "";
            }

        }

        private void SaveMetaDataCommand()
        {
            //---------metafile -m
            if (string.IsNullOrEmpty(ArgOptions.MetaFilename)) return;

            _logger.Normal("");
            _logger.Info($"Saving Metadata to file : {ArgOptions.MetaFilename}");
            var metaData = O2PGen.MetaDataAsString.FormatXml();
            SaveToFile(ArgOptions.MetaFilename, metaData);
        }

        #endregion
    }
}