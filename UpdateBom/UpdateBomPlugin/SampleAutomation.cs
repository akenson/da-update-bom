/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using Inventor;
using Autodesk.Forge.DesignAutomation.Inventor.Utils;

using Newtonsoft.Json;

using File = System.IO.File;
using Path = System.IO.Path;
using Directory = System.IO.Directory;
using Newtonsoft.Json.Linq;

namespace UpdateBomPlugin
{
    [ComVisible(true)]
    public class SampleAutomation
    {
        private readonly InventorServer inventorApplication;

        public SampleAutomation(InventorServer inventorApp)
        {
            inventorApplication = inventorApp;
        }

        public void Run(Document placeholder)
        {
            // LogTrace("Run called with {0}", doc.DisplayName);

            LogTrace("Running Update BOM...");
            string currDir = Directory.GetCurrentDirectory();


            // For local debugging
            //string inputPath = System.IO.Path.Combine(currDir, @"../../inputFiles", "params.json");
            //Dictionary<string, string> options = JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText(inputPath));

            Dictionary<string, string> options = JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText("inputParams.json"));
            string inputFile = options["inputFile"];
            string projectFile = options["projectFile"];

            string assemblyPath = Path.GetFullPath(Path.Combine(currDir, inputFile));
            string fullProjectPath = Path.GetFullPath(Path.Combine(currDir, projectFile));

            Console.WriteLine("fullProjectPath = " + fullProjectPath);

            DesignProject dp = inventorApplication.DesignProjectManager.DesignProjects.AddExisting(fullProjectPath);
            dp.Activate();

            Console.WriteLine("assemblyPath = " + assemblyPath);
            Document doc = inventorApplication.Documents.Open(assemblyPath);

            using (new HeartBeat())
            {
                GetBom(doc);
            }
        }

        public void RunWithArguments(Document doc, NameValueMap map)
        {
            LogTrace("RunWithArguments not implemented");
        }

        public void GetBom(Document doc)
        {
            try
            {
                if (doc.DocumentType == DocumentTypeEnum.kPartDocumentObject)
                {
                    LogTrace("Part Document");
                    // Parts don't have a BOM
                    LogTrace("Writing out empty bomRows.json");
                    File.WriteAllText("bomRows.json", "No BOM");
                }
                else if (doc.DocumentType == DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    LogTrace("Assembly Document");
                    AssemblyComponentDefinition assemblyComponentDef = ((AssemblyDocument)doc).ComponentDefinition;
                    BOM bom = assemblyComponentDef.BOM;
                    BOMViews bomViews = bom.BOMViews;
                    BOMView structureView = bomViews["Structured"];

                    JArray bomRows = new JArray();
                    GetBomRowProperties(structureView.BOMRows, bomRows);

                    LogTrace("Writing out bomRows.json");
                    File.WriteAllText("bomRows.json", bomRows.ToString());

                }
                else
                {
                    LogTrace("Unknown Document");
                    // unsupported doc type, throw exception
                    throw new Exception("Unsupported document type: " + doc.DocumentType.ToString());
                }

                ////BOM bom = componentDef.BOM;
                //BOMViews bomViews = bom.BOMViews;
                //BOMView structureView = bomViews["Structured"];

                //JArray bomRows = new JArray();
                //GetBomRowProperties(structureView.BOMRows, bomRows);

                //LogTrace("Writing out bomRows.json");
                //File.WriteAllText("bomRows.json", bomRows.ToString());

            }
            catch (Exception e)
            {
                LogError("Bom failed: " + e.ToString());
            }
        
        }

        public void GetBomRowProperties(BOMRowsEnumerator rows, JArray bomRows)
        {
            const string TRACKING = "Design Tracking Properties";
            foreach (BOMRow row in rows)
            {
                ComponentDefinition componentDef = row.ComponentDefinitions[1];

                // Assumes not virtual component (if so add conditional for that here)
                Property partNum = componentDef.Document.PropertySets[TRACKING]["Part Number"];
                Property descr = componentDef.Document.PropertySets[TRACKING]["Description"];
                Property material = componentDef.Document.PropertySets[TRACKING]["Material"];

                JObject bomRow = new JObject(
                    new JProperty("row_number", row.ItemNumber),
                    new JProperty("part_number", partNum.Value),
                    new JProperty("quantity", row.ItemQuantity),
                    new JProperty("description", descr.Value),
                    new JProperty("material", material.Value)
                    );

                bomRows.Add(bomRow);

                // iterate through child rows
                if (row.ChildRows != null)
                {
                    GetBomRowProperties(row.ChildRows, bomRows);
                }
            }
        }

        static void DirPrint(string sDir)
        {
            try
            {
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                        LogTrace("file: " + f);
                    }
                    DirPrint(d);
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }

        #region Logging utilities

        /// <summary>
        /// Log message with 'trace' log level.
        /// </summary>
        private static void LogTrace(string format, params object[] args)
        {
            Trace.TraceInformation(format, args);
        }

        /// <summary>
        /// Log message with 'trace' log level.
        /// </summary>
        private static void LogTrace(string message)
        {
            Trace.TraceInformation(message);
        }

        /// <summary>
        /// Log message with 'error' log level.
        /// </summary>
        private static void LogError(string format, params object[] args)
        {
            Trace.TraceError(format, args);
        }

        /// <summary>
        /// Log message with 'error' log level.
        /// </summary>
        private static void LogError(string message)
        {
            Trace.TraceError(message);
        }

        #endregion
    }
}