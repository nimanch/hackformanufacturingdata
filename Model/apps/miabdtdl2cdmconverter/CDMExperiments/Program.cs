using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CommonDataModel.ObjectModel.Cdm;
using Microsoft.CommonDataModel.ObjectModel.Enums;
using Microsoft.CommonDataModel.ObjectModel.Storage;
using Microsoft.CommonDataModel.ObjectModel.Utilities;
using Microsoft.Azure.DigitalTwins.Parser;
using System.Linq;
using System.Collections.Generic;

namespace MiaB.model.dtdl2cdm
{
    class Program
    {
        
        private const string cdm = "cdm";
        private const string FoundationJsonPath = "cdm:/foundations.cdm.json";

        // Configure storage adapters to point at the target local manifest location and at the fake public standards
        // TODO: These values should be changed based on your environment (better to take these from configurations)
        private const string local = "local";
        private const string dtdlRoot = @"E:\Hack2020\DtdlToCdmConverter\Models";
        private const string pathToCDMWork = @"E:\Hack2020\DtdlToCdmConverter\CDM\";
        private const string pathToCDMSampleData = @"E:\Hack2020\DtdlToCdmConverter\CDM\Samples\";
        private const string outputFolderName = "Hackathon2020";

        // Hard coded values for ADLS
        private const string adls = "adls";
        private const string adlsHostName = "gen2hackstore.dfs.core.windows.net";
        // Remember ADLS allows only lowercase letters, numbers and hyphens.This root path should be available in ADLS - i.e. needs to be created manually
        private const string adlsRoot = "/cdm/hack2020";
        // SECURITY RISK - this should be saved in AKV and retrived from there. Hardcodign for quick hacking
        private const string adlsAccessKey = "xnv4EoFbGh53d5n2669F5CniZYRnY/EfDQSz6vStu22m4m/pJlq9zn0nfI8UsQvvtixM/kIoxC4xpinHSYV7ZQ==";
        
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Hello CDM!");
                // Make a corpus, the corpus is the collection of all documents and folders created or discovered while navigating objects and paths
                var cdmCorpus = new CdmCorpusDefinition();

                Console.WriteLine("Configure storage adapters");

                cdmCorpus.Storage.Mount(local, new LocalAdapter(pathToCDMWork + outputFolderName));
                cdmCorpus.Storage.DefaultNamespace = local; // local is our default. so any paths that start out navigating without a device tag will assume local

                // Fake cdm, normaly use the github adapter
                // Mount it as the 'cdm' device, not the default so must use "cdm:/folder" to get there
                cdmCorpus.Storage.Mount(cdm, new LocalAdapter(pathToCDMSampleData + "example-public-standards"));

                // Mount ADLS adapter
                cdmCorpus.Storage.Mount(
                                    adls,
                                    new ADLSAdapter(hostname:adlsHostName,
                                                    root: adlsRoot,
                                                    sharedKey:adlsAccessKey)
                                    );

                await SaveCDMDocuments(local, cdmCorpus);
                await SaveCDMDocuments(adls, cdmCorpus);

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        private static async Task SaveCDMDocuments(string nameSpace, CdmCorpusDefinition cdmCorpus)
        {
            Console.WriteLine("Make placeholder manifest");
            // Make the temp manifest and add it to the root of the local documents in the corpus
            CdmManifestDefinition manifestAbstract = cdmCorpus.MakeObject<CdmManifestDefinition>(CdmObjectType.ManifestDef, "tempAbstract");

            // Add the temp manifest to the root of the local documents in the corpus.
            var localRoot = cdmCorpus.Storage.FetchRootFolder(nameSpace);
            localRoot.Documents.Add(manifestAbstract);

            // Create two entities from scratch, and add some attributes, traits, properties, and relationships in between
            Console.WriteLine("Create net new entities");

            DTDLParser parser = new DTDLParser(dtdlRoot, "json");
            foreach (DTInterfaceInfo info in parser.DTDLInterfaces)
            {
                CreateCustomEntity(cdmCorpus, manifestAbstract, localRoot, info);
            }

            // Create the resolved version of everything in the root folder too
            Console.WriteLine("Resolve the placeholder");
            var manifestResolved = await manifestAbstract.CreateResolvedManifestAsync("default", null);

            // Add an import to the foundations doc so the traits about partitons will resolve nicely
            manifestResolved.Imports.Add("cdm:/foundations.cdm.json");

            Console.WriteLine($"Save the folders and partition data documents to {nameSpace} storage.");
            await SavePartitionDocuments(nameSpace, cdmCorpus, manifestResolved);

            Console.WriteLine($"Save the manifest, entity and resolved documents to {nameSpace} storage.");
            // Save all other files (resolved, manifest, entity etc.)
            await manifestResolved.SaveAsAsync($"{manifestResolved.ManifestName}.manifest.cdm.json", true);
        }

        private static async Task SavePartitionDocuments(string nameSpace, CdmCorpusDefinition cdmCorpus, CdmManifestDefinition manifestResolved)
        {
            try
            {
                foreach (CdmEntityDeclarationDefinition eDef in manifestResolved.Entities)
                {
                    // Get the entity being pointed at
                    var localEDef = eDef;
                    var entDef = await cdmCorpus.FetchObjectAsync<CdmEntityDefinition>(localEDef.EntityPath, manifestResolved);
                    // Make a fake partition, just to demo that
                    var part = cdmCorpus.MakeObject<CdmDataPartitionDefinition>(CdmObjectType.DataPartitionDef, $"{entDef.EntityName}-data-description");
                    localEDef.DataPartitions.Add(part);
                    part.Explanation = "not real data, just for demo";

                    // Define the location of the partition, relative to the manifest
                    var location = $"{nameSpace}:/{entDef.EntityName}/partition-data.csv";
                    part.Location = cdmCorpus.Storage.CreateRelativeCorpusPath(location, manifestResolved);

                    // Add trait to partition for csv params
                    var csvTrait = part.ExhibitsTraits.Add("is.partition.format.CSV", false);
                    csvTrait.Arguments.Add("columnHeaders", "true");
                    csvTrait.Arguments.Add("delimiter", ",");

                    // Make a fake file with nothing but header for columns
                    string header = "";
                    foreach (CdmTypeAttributeDefinition att in entDef.Attributes)
                    {
                        if (header != "")
                            header += ",";
                        header += att.Name;
                    }

                    if (nameSpace.Equals(local, StringComparison.OrdinalIgnoreCase))
                    {
                        string directoryPath = cdmCorpus.Storage.CorpusPathToAdapterPath($"{nameSpace}:/{ entDef.EntityName}");
                        Directory.CreateDirectory(directoryPath);
                        // Get the actual location of the partition file from the corpus
                        string partPath = cdmCorpus.Storage.CorpusPathToAdapterPath(location);
                        File.WriteAllText(partPath, header);
                    }
                    else
                    {
                        var adapter = cdmCorpus.Storage.FetchAdapter(nameSpace);
                        await adapter.WriteAsync(location, header);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write($"Error while saving CDM Document. Details: {ex.ToString()}");
            }
        }

        /// <summary>
        /// Create an type attribute definition instance with provided data type.
        /// </summary>
        /// <param name="cdmCorpus"> The CDM corpus. </param>
        /// <param name="attributeName"> The directives to use while getting the resolved entities. </param>
        /// <param name="purpose"> The manifest to be resolved. </param>
        /// <param name="dataType"> The data type.</param>
        /// <returns> The instance of type attribute definition. </returns>
        private static CdmTypeAttributeDefinition CreateEntityAttributeWithPurposeAndDataType(CdmCorpusDefinition cdmCorpus, string attributeName, string purpose, string dataType)
        {
            var entityAttribute = CreateEntityAttributeWithPurpose(cdmCorpus, attributeName, purpose);
            entityAttribute.DataType = cdmCorpus.MakeRef<CdmDataTypeReference>(CdmObjectType.DataTypeRef, dataType, true);
            return entityAttribute;
        }

        /// <summary>
        /// Create an type attribute definition instance with provided purpose.
        /// </summary>
        /// <param name="cdmCorpus"> The CDM corpus. </param>
        /// <param name="attributeName"> The directives to use while getting the resolved entities. </param>
        /// <param name="purpose"> The manifest to be resolved. </param>
        /// <returns> The instance of type attribute definition. </returns>
        private static CdmTypeAttributeDefinition CreateEntityAttributeWithPurpose(CdmCorpusDefinition cdmCorpus, string attributeName, string purpose)
        {
            var entityAttribute = cdmCorpus.MakeObject<CdmTypeAttributeDefinition>(CdmObjectType.TypeAttributeDef, attributeName, false);
            entityAttribute.Purpose = cdmCorpus.MakeRef<CdmPurposeReference>(CdmObjectType.PurposeRef, purpose, true);
            return entityAttribute;
        }

        /// <summary>
        /// Create a relationship linking by creating an eneity attribute definition instance with a trait. 
        /// This allows you to add a resolution guidance to customize your data.
        /// </summary>
        /// <param name="cdmCorpus"> The CDM corpus. </param>
        /// <param name="associatedEntityName"> The name of the associated entity. </param>
        /// <param name="foreignKeyName"> The name of the foreign key. </param>
        /// <param name="attributeExplanation"> The explanation of the attribute.</param>
        /// <returns> The instatnce of entity attribute definition. </returns>
        private static CdmEntityAttributeDefinition CreateAttributeForRelationshipBetweenTwoEntities(
            CdmCorpusDefinition cdmCorpus,
            string associatedEntityName,
            string foreignKeyName,
            string attributeExplanation)
        {
            // Define a relationship by creating an entity attribute
            var entityAttributeDef = cdmCorpus.MakeObject<CdmEntityAttributeDefinition>(CdmObjectType.EntityAttributeDef, foreignKeyName);
            entityAttributeDef.Explanation = attributeExplanation;
            // Creating an entity reference for the associated entity 
            CdmEntityReference associatedEntityRef = cdmCorpus.MakeRef<CdmEntityReference>(CdmObjectType.EntityRef, associatedEntityName, false);

            // Creating a "is.identifiedBy" trait for entity reference
            CdmTraitReference traitReference = cdmCorpus.MakeObject<CdmTraitReference>(CdmObjectType.TraitRef, "is.identifiedBy", false);
            traitReference.Arguments.Add(null, $"{associatedEntityName}/(resolvedAttributes)/{associatedEntityName}Id");

            // Add the trait to the attribute's entity reference
            associatedEntityRef.AppliedTraits.Add(traitReference);
            entityAttributeDef.Entity = associatedEntityRef;

            // Add resolution guidance
            var attributeResolution = cdmCorpus.MakeObject<CdmAttributeResolutionGuidance>(CdmObjectType.AttributeResolutionGuidanceDef);
            attributeResolution.entityByReference = attributeResolution.makeEntityByReference();
            attributeResolution.entityByReference.allowReference = true;
            attributeResolution.renameFormat = "{m}";
            var entityAttribute = CreateEntityAttributeWithPurposeAndDataType(cdmCorpus, $"{foreignKeyName}Id", "identifiedBy", "entityId");
            attributeResolution.entityByReference.foreignKeyAttribute = entityAttribute as CdmTypeAttributeDefinition;
            entityAttributeDef.ResolutionGuidance = attributeResolution;

            return entityAttributeDef;
        }

        /// <summary>
        /// Create a relationship linking with an attribute an eneity attribute definition instance without a trait.
        /// </summary>
        /// <param name="cdmCorpus"> The CDM corpus. </param>
        /// <param name="associatedEntityName"> The name of . </param>
        /// <param name="foreignKeyName"> The name of the foreign key. </param>
        /// <param name="attributeExplanation"> The explanation of the attribute.</param>
        /// <returns> The instatnce of entity attribute definition. </returns>
        private static CdmEntityAttributeDefinition CreateSimpleAttributeForRelationshipBetweenTwoEntities(
            CdmCorpusDefinition cdmCorpus,
            string associatedEntityName,
            string foreignKeyName,
            string attributeExplanation)
        {
            // Define a relationship by creating an entity attribute
            var entityAttributeDef = cdmCorpus.MakeObject<CdmEntityAttributeDefinition>(CdmObjectType.EntityAttributeDef, foreignKeyName);
            entityAttributeDef.Explanation = attributeExplanation;

            // Creating an entity reference for the associated entity - simple name reference
            entityAttributeDef.Entity = cdmCorpus.MakeRef<CdmEntityReference>(CdmObjectType.EntityRef, associatedEntityName, true);

            // Add resolution guidance - enable reference
            var attributeResolution = cdmCorpus.MakeObject<CdmAttributeResolutionGuidance>(CdmObjectType.AttributeResolutionGuidanceDef);
            attributeResolution.entityByReference = attributeResolution.makeEntityByReference();
            attributeResolution.entityByReference.allowReference = true;
            entityAttributeDef.ResolutionGuidance = attributeResolution;

            return entityAttributeDef;
        }

        private static void CreateCustomEntity(CdmCorpusDefinition cdmCorpus, CdmManifestDefinition manifestAbstract, 
                                               CdmFolderDefinition localRoot, DTInterfaceInfo info)
        {
            string EntityName = info.Id.ToString();
            string convertedEntityName = EntityName.Replace(':', '_');
            convertedEntityName = convertedEntityName.Replace(';', '-');

            // Create an entity - CustomAccount which has a relationship with the entity CustomPerson
            // Create the entity definition instance
            var entity = cdmCorpus.MakeObject<CdmEntityDefinition>(CdmObjectType.EntityDef, convertedEntityName, false);
            // Add type attributes to the entity instance
            var entityAttributeId = CreateEntityAttributeWithPurposeAndDataType(cdmCorpus, $"$dtId", "identifiedBy", "entityId");
            entity.Attributes.Add(entityAttributeId);
            //var entityAttributeName = CreateEntityAttributeWithPurposeAndDataType(cdmCorpus, $"${convertedEntityName}Name", "hasA", "name");
            //entity.Attributes.Add(entityAttributeName);

            var timestamp = CreateEntityAttributeWithPurposeAndDataType(cdmCorpus, "$timestamp", "hasA", "dateTime");
            entity.Attributes.Add(timestamp);

            // Add properties to the entity instance
            entity.DisplayName = info.DisplayName.FirstOrDefault().Value;
            entity.Version = "0.0.1";
            entity.Description = info.Description.FirstOrDefault().Value;
            // Create the document which contains the entity
            var entityDoc = cdmCorpus.MakeObject<CdmDocumentDefinition>(CdmObjectType.DocumentDef, $"{convertedEntityName}.cdm.json", false);
            // Add an import to the foundations doc so the traits about partitons will resolve nicely
            entityDoc.Imports.Add(FoundationJsonPath);
            entityDoc.Definitions.Add(entity);

            foreach (KeyValuePair<string, DTContentInfo> kvp in info.Contents)
            {
                if (kvp.Value.EntityKind == DTEntityKind.Property)
                {
                    DTPropertyInfo pi = kvp.Value as DTPropertyInfo;
                    Dtmi def = DefiningModel(pi.Name, info);
                    if (def == info.Id)
                    {

                        Log.Out($"{info.Id}: Adding locally defined property {pi.Name}");

                        string type = "";
                        if (pi.Schema != null)
                        {
                            switch (pi.Schema.EntityKind)
                            {
                                case DTEntityKind.String: type = "string"; break;
                                case DTEntityKind.Float: type = "float"; break;
                                case DTEntityKind.Double: type = "double"; break;
                                case DTEntityKind.Boolean: type = "boolean"; break;
                                case DTEntityKind.Integer: type = "integer"; break;
                                default: break;
                            }
                        }
                        if (type != "")
                        {
                            var prop = CreateEntityAttributeWithPurposeAndDataType(cdmCorpus, pi.Name, "hasA", type);
                            entity.Attributes.Add(prop);
                        }
                    }
                    else
                    {
                        Log.Alert($"{info.Id}: Ignored property {pi.Name} because it is defined in \n{def}");
                    }
                }
            }

            

            // Handle inheritance 
            if (info.Extends.Count > 0)
            {
                foreach (DTInterfaceInfo parent in info.Extends)
                {
                    string pEntityName = parent.Id.ToString();
                    string pConvertedEntityName = pEntityName.Replace(':', '_');
                    pConvertedEntityName = pConvertedEntityName.Replace(';', '-');
                    entity.ExtendsEntity = cdmCorpus.MakeObject<CdmEntityReference>(CdmObjectType.EntityRef, pConvertedEntityName, true);
                    entityDoc.Imports.Add($"{pConvertedEntityName}.cdm.json");
                }
            }

            // Handle references
            foreach (KeyValuePair<string, DTContentInfo> kvp in info.Contents)
            {
                if (kvp.Value.EntityKind == DTEntityKind.Relationship)
                {
                    DTRelationshipInfo ri = kvp.Value as DTRelationshipInfo;
                    Dtmi def = DefiningModel(ri.Name, info);
                    if (def == info.Id)
                    {
                        string pEntityName = string.Format("{0}_{1}",def.AbsoluteUri.Substring(0,def.AbsoluteUri.IndexOf(";")), ri.Name.ToString());
                        string pConvertedEntityName = pEntityName.Replace(':', '_');
                        pConvertedEntityName = pConvertedEntityName.Replace(';', '-');
                        Log.Out($"{info.Id}: Adding locally defined relationship {ri.Name}");
                        var attributeExplanation = $"{ri.Name}: {ri.Description.Values.FirstOrDefault()}";
                        var t = kvp.Value;
                        CreateRelatedCustomEntity(cdmCorpus, manifestAbstract, localRoot, ri.Properties,pConvertedEntityName,ri.Name);
                        // You can all CreateSimpleAttributeForRelationshipBetweenTwoEntities() instead, but CreateAttributeForRelationshipBetweenTwoEntities() can show 
                        // more details of how to use resolution guidance to customize your data
                        var refAttribute = CreateAttributeForRelationshipBetweenTwoEntities(cdmCorpus, convertedEntityName, pConvertedEntityName, attributeExplanation);
                        entity.Attributes.Add(refAttribute);
                        // Add an import to the foundations doc so the traits about partitons will resolve nicely
                        entityDoc.Imports.Add(FoundationJsonPath);
                        // the CustomAccount entity has a relationship with the CustomPerson entity, this relationship is defined from its attribute with traits, 
                        // the import to the entity reference CustomPerson's doc is required
                        entityDoc.Imports.Add($"{pConvertedEntityName}.cdm.json");
                    }
                    else
                    {
                        Log.Alert($"{info.Id}: Ignored property {ri.Name} because it is defined in \n{def}");
                    }
                }
            }

            // Add the document to the root of the local documents in the corpus
            localRoot.Documents.Add(entityDoc, entityDoc.Name);
            // Add the entity to the manifest
            manifestAbstract.Entities.Add(entity);
        }
private static void CreateRelatedCustomEntity(CdmCorpusDefinition cdmCorpus, CdmManifestDefinition manifestAbstract, 
                                               CdmFolderDefinition localRoot, List<DTPropertyInfo> info,string entityName,string displayName)
        {
        
            string convertedEntityName = entityName;

            // Create an entity - CustomAccount which has a relationship with the entity CustomPerson
            // Create the entity definition instance
            var entity = cdmCorpus.MakeObject<CdmEntityDefinition>(CdmObjectType.EntityDef, convertedEntityName, false);
            // Add type attributes to the entity instance
            var entityAttributeId = CreateEntityAttributeWithPurposeAndDataType(cdmCorpus, $"$dtId", "identifiedBy", "entityId");
            entity.Attributes.Add(entityAttributeId);
            // Add properties to the entity instance
            entity.DisplayName = displayName;
            entity.Version = "0.0.1";
          //  entity.Description = info.Description.FirstOrDefault().Value;
            
            // Create the document which contains the entity
            var entityDoc = cdmCorpus.MakeObject<CdmDocumentDefinition>(CdmObjectType.DocumentDef, $"{convertedEntityName}.cdm.json", false);
            // Add an import to the foundations doc so the traits about partitons will resolve nicely
            entityDoc.Imports.Add(FoundationJsonPath);
            entityDoc.Definitions.Add(entity);

            foreach (DTPropertyInfo prop in info)
            {
                if (prop.EntityKind == DTEntityKind.Property)
                {
                    Log.Out($"{convertedEntityName}: Adding locally defined property {prop.Name}");

                        string type = "";
                        if (prop.Schema != null)
                        {
                            switch (prop.Schema.EntityKind)
                            {
                                case DTEntityKind.String: type = "string"; break;
                                case DTEntityKind.Float: type = "float"; break;
                                case DTEntityKind.Double: type = "double"; break;
                                case DTEntityKind.Boolean: type = "boolean"; break;
                                case DTEntityKind.Integer: type = "integer"; break;
                                default: break;
                            }
                        }
                        if (type != "")
                        {
                            var pro = CreateEntityAttributeWithPurposeAndDataType(cdmCorpus, prop.Name, "hasA", type);
                            entity.Attributes.Add(pro);
                        }
                    
                }
            }

            // Add the document to the root of the local documents in the corpus
            localRoot.Documents.Add(entityDoc, entityDoc.Name);
            
            // Add the entity to the manifest
            manifestAbstract.Entities.Add(entity);
        }
        private static Dtmi DefiningModel(string item, DTInterfaceInfo ifInfo)
        {
            // must be depth first
            Dtmi result = null;
            foreach (DTInterfaceInfo parent in ifInfo.Extends)
            {
                result = DefiningModel(item, parent);
                if (result != null)
                    return result;
            }
            // Only check if defined locally after all super classes have been checked
            foreach (DTContentInfo ci in ifInfo.Contents.Values)
            {
                if (ci.Name == item)
                    return ifInfo.Id;
            }
            // This should really never happen
            return null;
        }
    }
}
