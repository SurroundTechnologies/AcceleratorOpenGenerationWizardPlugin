using A4DN.CF.SchemaEntities;
using A4DN.Core.BOS.Base;
using A4DN.Core.BOS.FrameworkEntity;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using A4DN.CF.WizardShared;

namespace GenerationWizardPlugin
{
    public class EasyBuyWizardDefaults_MSSQLLegacy : WizardDefaults
    {
        public override void am_Initialize(AB_GenerationWizardShared generationWizardShared)
        {
            // Relationships are pulled from the database access routes. If no relationships are defined on the database, you can define the relationships in the _GetDatabaseRelationships method.
            generationWizardShared.ap_DatabaseRelationships = _GetDatabaseRelationships();

            base.am_Initialize(generationWizardShared);
        }

        internal override void SetModuleRulesBeforeColumnRules(WizardDefaults.Mode mode, AB_GenerationModuleEntity moduleEntity)
        {
            AuditStamps = new Dictionary<string, AB_AuditStampTypes>()
                {
                    {"CRDT", AB_AuditStampTypes.CreateDate},
                    {"CRTM", AB_AuditStampTypes.CreateTime},
                    {"CRUS", AB_AuditStampTypes.CreateUser},
                    {"CRJB", AB_AuditStampTypes.Undefined},
                    {"CRJN", AB_AuditStampTypes.Undefined},
                    {"LCDT", AB_AuditStampTypes.LastChangeDate},
                    {"LCTM", AB_AuditStampTypes.LastChangeTime},
                    {"LCUS", AB_AuditStampTypes.LastChangeUser},
                    {"LCJB", AB_AuditStampTypes.Undefined},
                    {"LCJN", AB_AuditStampTypes.Undefined},
                };

            ExtendedContentWindowField = new List<string>
                {
                    "PTID",
                    "PTRL"
                };

            ExtendedSearchField = new List<string>
                {
                    "PTID",
                    "PTRL"
                };

            DetailTitleField = new List<string>
                {
                    "NM",
                    "NLN",
                    "NFN"
                };

            base.SetModuleRulesBeforeColumnRules(mode, moduleEntity);

            switch (mode)
            {
                case Mode.InitialSetup:
                    // Set Module Name.  This will also set the image name
                    moduleEntity.ModuleName = FormatModuleName(moduleEntity.FileDescription);

                    break;

                case Mode.ColumnsChanged:
                    break;
            }
        }

        internal override void SetColumnRules(Mode mode, AB_GenerationModuleEntity moduleEntity, AB_GenerationViewColumnEntity viewColumnEntity)
        {
            base.SetColumnRules(mode, moduleEntity, viewColumnEntity);

            switch (mode)
            {
                case Mode.InitialSetup:

                    // This is done to eliminate conflicts with having the Internal ID referenced multiple times in a file
                    if (viewColumnEntity.EntityPropertyName.ToUpper() == "INTERNALID")
                    {
                        viewColumnEntity.EntityPropertyName = moduleEntity.ModuleName.Replace(" ", "") + "InternalID";
                        viewColumnEntity.ColumnDescription = moduleEntity.ModuleName + " Internal ID";
                    }

                    if (viewColumnEntity.Type == "numeric")
                    {
                        viewColumnEntity.PropertyType = AB_PropertyTypes.Integer;
                    }

                    // Set Numeric(8) date fields as Property Type of DateTime and Field Visualization of Date
                    if ((viewColumnEntity.Name.Substring(viewColumnEntity.Name.Length - 2) == "DT") && ((viewColumnEntity.Type == "NUMERIC(8)") || viewColumnEntity.Type == "DATE(4)" || viewColumnEntity.Type == "numeric"))
                    {
                        viewColumnEntity.PropertyType = AB_PropertyTypes.DateTime;
                        viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_DatePicker;

                        if ((viewColumnEntity.Type == "NUMERIC(8)" || viewColumnEntity.Type == "numeric"))
                        {
                            viewColumnEntity.AdditionalDataMapParameters = "databaseFieldType: AB_EntityFieldType.Decimal";
                        }
                    }

                    // Set Numeric(6) Time fields as Property Type of TimeSpan and Field Visualization of Time
                    if ((viewColumnEntity.Name.Substring(viewColumnEntity.Name.Length - 2) == "TM") && (viewColumnEntity.Type == "NUMERIC(6)" || viewColumnEntity.Type == "numeric"))
                    {
                        viewColumnEntity.PropertyType = AB_PropertyTypes.TimeSpan;
                        viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_TimePicker;
                        viewColumnEntity.AdditionalDataMapParameters = "databaseFieldType: AB_EntityFieldType.Decimal";
                    }

                    // Set Identity Field
                    if (IsIdentityField(viewColumnEntity))
                    {
                        if (viewColumnEntity.Type == "int")
                        {
                            viewColumnEntity.IsIdentity = true;
                        }
                    }

                    // Uncheck Memo Field from Content Window and Search Explorer Bar
                    if (viewColumnEntity.ViewField.EndsWith("M1") && viewColumnEntity.FieldLength == 100)
                    {
                        viewColumnEntity.IsContentWindowField = false;
                        viewColumnEntity.IsExplorerBarField = false;
                    }

                    // Uncheck Image Path from Search Explorer Bar
                    if (viewColumnEntity.ViewField.EndsWith("IMPT") && viewColumnEntity.FieldLength == 256)
                    {
                        viewColumnEntity.IsExplorerBarField = false;
                    }

                    // Uncheck Warehouse ID and Sales Person ID Fields from Content Window, Search Explorer Bar and Detail
                    if (viewColumnEntity.ViewField.EndsWith("YD1O1WID") || viewColumnEntity.ViewField.EndsWith("YD1O1AID"))
                    {
                        viewColumnEntity.IsContentWindowField = false;
                        viewColumnEntity.IsExplorerBarField = false;
                        viewColumnEntity.IsDetailField = false;
                        viewColumnEntity.IsAuditStampField = false;
                    }

                    // Set Nick Name Field to Visible False in Content Window
                    if (viewColumnEntity.ViewField.EndsWith("NN") && viewColumnEntity.FieldLength == 50)
                    {
                        viewColumnEntity.Visible = false;
                    }

                    // Set Fields in Title Required
                    if (viewColumnEntity.IsTitleField && !viewColumnEntity.IsIdentity)
                    {
                        viewColumnEntity.IsRequiredField = true;
                    }
                    else
                    {
                        viewColumnEntity.IsRequiredField = false;
                    }

                    // Set Any Fields that are not audit stamps but end in "DT" or "TM" as Detail Title Fields
                    if ((viewColumnEntity.ViewField.ToUpper().EndsWith("DT") || viewColumnEntity.ViewField.ToUpper().EndsWith("TM")) && !IsAuditTabField(viewColumnEntity))
                    {
                        viewColumnEntity.IsTitleField = true;
                    }

                    var requiredField = new List<string>
                {
                    "NM","CNLN","CNFN","A1","A2","A3","HPC","LPC","HCY","LCY","TL","EM","OID","PID","IQT","IID","CID","ODT","SID","ST","PONO","CD","LSPR"
                };
                    // Set Required Fields
                    if (requiredField.Any(x => viewColumnEntity.ViewField.ToUpper().EndsWith(x.ToUpper())))
                    {
                        viewColumnEntity.IsRequiredField = true;
                    }

                    break;

                case Mode.ColumnsChanged:

                    break;
            }

            if (!viewColumnEntity.IsTitleField)
            {
                //If not a title field, then hide on small devices
                viewColumnEntity.WebMarkupTHDataAttributes = "data-hide=\"phone,tablet\"";
            }
        }

        internal override void SetModuleRulesAfterColumnRules(WizardDefaults.Mode mode, AB_GenerationModuleEntity moduleEntity)
        {
            switch (mode)
            {
                case Mode.InitialSetup:

                    // Set the Default View
                    _defaultViewSet = false;
                    if (moduleEntity.GenerationModuleExplorers[0] != null &&
                        moduleEntity.GenerationModuleExplorers[0].GenerationModuleExplorerViews != null)
                    {
                        foreach (
                            var view in moduleEntity.GenerationModuleExplorers[0].GenerationModuleExplorerViews.OrderBy(x => x.ViewName)
                            )
                        {
                            _SetDefaultView(view, moduleEntity);
                        }
                    }

                    break;

                case Mode.ColumnsChanged:
                    break;
            }

            base.SetModuleRulesAfterColumnRules(mode, moduleEntity);
        }

        private bool _defaultViewSet;

        /// <summary>
        /// Set Default View
        /// </summary>
        /// <param name="viewEntity">View Entity</param>
        /// <param name="moduleEntity">Module Entity</param>
        /// <returns></returns>
        private void _SetDefaultView(AB_GenerationModuleExplorerViewEntity viewEntity,
                                      AB_GenerationModuleEntity moduleEntity)
        {
            const string defaultViewSuffix = "LF1";

            if (viewEntity.ViewName.ToUpper().EndsWith(defaultViewSuffix.ToUpper()) && viewEntity.Description.ToUpper().Contains("NAME"))
            {
                if (!_defaultViewSet)
                {
                    WizardShared.am_SetDefaultView(viewEntity, moduleEntity);
                    _defaultViewSet = true;
                }
            }
        }

        #region Relationships

        private ObservableCollection<AB_SchemaRelationshipEntity> _GetDatabaseRelationships()
        {
            var relationships = new ObservableCollection<AB_SchemaRelationshipEntity>();

            // Customers have One to Many Orders
            relationships.Add(_AddRelationship("EASYBUYDEM", "YD1C", "YD1CIID", "EASYBUYDEM", "YD1O", "YD1O1CID", SchemaRelationshipType.OneToMany));
            // Customers have One to Many Shipping Addresses
            relationships.Add(_AddRelationship("EASYBUYDEM", "YD1C", "YD1CIID", "EASYBUYDEM", "YD1S", "YD1S1CID", SchemaRelationshipType.OneToMany));
            // Customers have Customer
            relationships.Add(_AddRelationship("EASYBUYDEM", "YD1C", "YD1CIID", "EASYBUYDEM", "YD1C", "YD1CPTID", SchemaRelationshipType.OneToMany));
            // Orders have One to Many Order Items
            relationships.Add(_AddRelationship("EASYBUYDEM", "YD1O", "YD1OIID", "EASYBUYDEM", "YD1I", "YD1I1OID", SchemaRelationshipType.OneToMany));
            // Products have One to Many Order Items
            relationships.Add(_AddRelationship("EASYBUYDEM", "YD1P", "YD1PIID", "EASYBUYDEM", "YD1I", "YD1I1PID", SchemaRelationshipType.OneToMany));
            // Shipping Addresses have One to Many Order
            relationships.Add(_AddRelationship("EASYBUYDEM", "YD1S", "YD1SIID", "EASYBUYDEM", "YD1O", "YD1O1SID", SchemaRelationshipType.OneToMany));

            // Customers have One to Many Orders
            relationships.Add(_AddRelationship("EASYBUYDEV", "YD1C", "YD1CIID", "EASYBUYDEV", "YD1O", "YD1O1CID", SchemaRelationshipType.OneToMany));
            // Customers have One to Many Shipping Addresses
            relationships.Add(_AddRelationship("EASYBUYDEV", "YD1C", "YD1CIID", "EASYBUYDEV", "YD1S", "YD1S1CID", SchemaRelationshipType.OneToMany));
            // Customers have Customer
            relationships.Add(_AddRelationship("EASYBUYDEV", "YD1C", "YD1CIID", "EASYBUYDEV", "YD1C", "YD1CPTID", SchemaRelationshipType.OneToMany));
            // Orders have One to Many Order Items
            relationships.Add(_AddRelationship("EASYBUYDEV", "YD1O", "YD1OIID", "EASYBUYDEV", "YD1I", "YD1I1OID", SchemaRelationshipType.OneToMany));
            // Products have One to Many Order Items
            relationships.Add(_AddRelationship("EASYBUYDEV", "YD1P", "YD1PIID", "EASYBUYDEV", "YD1I", "YD1I1PID", SchemaRelationshipType.OneToMany));
            // Shipping Addresses have One to Many Order
            relationships.Add(_AddRelationship("EASYBUYDEV", "YD1S", "YD1SIID", "EASYBUYDEV", "YD1O", "YD1O1SID", SchemaRelationshipType.OneToMany));

            return relationships;
        }

        private AB_SchemaRelationshipEntity _AddRelationship(string primarySchema, string primaryTable, string primaryKeyColumn, string foreignSchema, string foreignTable, string foreignKeyColumn, SchemaRelationshipType relationshipType)
        {
            return new AB_SchemaRelationshipEntity()
            {
                PrimaryTable = primaryTable,
                PrimaryKeyColumn = primaryKeyColumn,
                ForeignTable = foreignTable,
                ForeignKeyColumn = foreignKeyColumn,
                RelationshipType = relationshipType,
            };
        }

        #endregion Relationships
    }
}