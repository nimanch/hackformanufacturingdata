using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.DigitalTwins.Parser;

namespace MiaB.model.dtdl2cdm
{
    class DTDLParser
    {
        public IReadOnlyDictionary<Dtmi, DTEntityInfo> DTDLObjectModel { get; set; }

        public List<DTInterfaceInfo> DTDLInterfaces { get; set; }

        public DTDLParser(string root, string ext)
        {
            Log.Ok("Simple DTDL Validator");

            DirectoryInfo dinfo = null;
            try
            {
                dinfo = new DirectoryInfo(root);
            }
            catch (Exception e)
            {
                Log.Error($"Error accessing the target directory '{root}': \n{e.Message}");
                return;
            }
            Log.Alert($"Validating *.{ext} files in folder '{dinfo.FullName}'.");
            if (dinfo.Exists == false)
            {
                Log.Error($"Specified directory '{root}' does not exist: Exiting...");
                return;
            }
            else
            {
                SearchOption searchOpt = SearchOption.AllDirectories;
                var files = dinfo.EnumerateFiles($"*.{ext}", searchOpt);
                if (files.Count() == 0)
                {
                    Log.Alert("No matching files found. Exiting.");
                    return;
                }
                Dictionary<FileInfo, string> modelDict = new Dictionary<FileInfo, string>();
                int count = 0;
                string lastFile = "<none>";
                try
                {
                    foreach (FileInfo fi in files)
                    {
                        StreamReader r = new StreamReader(fi.FullName);
                        string dtdl = r.ReadToEnd(); r.Close();
                        modelDict.Add(fi, dtdl);
                        lastFile = fi.FullName;
                        count++;
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Could not read files. \nLast file read: {lastFile}\nError: \n{e.Message}");
                    return;
                }
                Log.Ok($"Read {count} files from specified directory");
                int errJson = 0;
                foreach (FileInfo fi in modelDict.Keys)
                {
                    modelDict.TryGetValue(fi, out string dtdl);
                    try
                    {
                        JsonDocument.Parse(dtdl);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Invalid json found in file {fi.FullName}.\nJson parser error \n{e.Message}");
                        errJson++;
                    }
                }
                if (errJson > 0)
                {
                    Log.Error($"\nFound  {errJson} Json parsing errors");
                    return;
                }
                Log.Ok($"Validated JSON for all files - now validating DTDL");
                List<string> modelList = modelDict.Values.ToList<string>();
                ModelParser parser = new ModelParser();
                parser.DtmiResolver = Resolver;
                try
                {
                    Stopwatch s = Stopwatch.StartNew();
                    DTDLObjectModel = parser.ParseAsync(modelList).GetAwaiter().GetResult();
                    s.Stop();
                    Log.Out("");
                    Log.Ok($"**********************************************");
                    Log.Ok($"** Validated all files - Your DTDL is valid **");
                    Log.Ok($"**********************************************");
                    Log.Out($"Found a total of {DTDLObjectModel.Keys.Count()} entities");
                    Log.Out($"Parsing took {s.ElapsedMilliseconds}ms");

                    DTDLInterfaces = new List<DTInterfaceInfo>();
                    IEnumerable<DTInterfaceInfo> ifenum = from entity in DTDLObjectModel.Values
                                                          where entity.EntityKind == DTEntityKind.Interface
                                                          select entity as DTInterfaceInfo;
                    DTDLInterfaces.AddRange(ifenum);

                }
                catch (ParsingException pe)
                {
                    Log.Error($"*** Error parsing models");
                    int derrcount = 1;
                    foreach (ParsingError err in pe.Errors)
                    {
                        Log.Error($"Error {derrcount}:");
                        Log.Error($"{err.Message}");
                        Log.Error($"Primary ID: {err.PrimaryID}");
                        Log.Error($"Secondary ID: {err.SecondaryID}");
                        Log.Error($"Property: {err.Property}\n");
                        derrcount++;
                    }
                    return;
                }
                catch (ResolutionException rex)
                {
                    Log.Error("Could not resolve required references");
                }
            }
        }

        async Task<IEnumerable<string>> Resolver(IReadOnlyCollection<Dtmi> dtmis)
        {
            Log.Error($"*** Error parsing models. Missing:");
            foreach (Dtmi d in dtmis)
            {
                Log.Error($"  {d}");
            }
            return null;
        }
    }
}
