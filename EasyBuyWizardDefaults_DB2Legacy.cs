using A4DN.CF.SchemaEntities;
using A4DN.Core.BOS.Base;
using A4DN.Core.BOS.FrameworkEntity;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using A4DN.CF.WizardShared;
using System.Text;
using System.Windows.Forms;

namespace GenerationWizardPlugin
{
    public class EasyBuyWizardDefaults_DB2Legacy : AB_IGenerationWizardDefault
    {
        internal enum Mode { InitialSetup, ColumnsChanged };

        // Generation Wizard Shared Data
        internal readonly AB_GenerationWizardShared WizardShared = new AB_GenerationWizardShared();

        #region Required Methods

        /// <summary>
        /// Accelerator Method <c>am_Initialize</c>: initialize.
        /// </summary>
        /// <param name="generationWizardShared">The generation wizard shared.</param>
        public void am_Initialize(AB_GenerationWizardShared generationWizardShared)
        {
            // Relationships are pulled from the database access routes. If no relationships are defined on the database, you can define the relationships in the _GetDatabaseRelationships method.
            generationWizardShared.ap_DatabaseRelationships = _GetDatabaseRelationships();
        }

        /// <summary>
        /// Accelerator Method <c>am_PromptForKeysIfNoneSpecified</c>: Prompts for keys if none specified on the File/Table.
        /// </summary>
        /// <remarks>This method is called when no keys are found on the table or physical file. Returning True will prompt the user to select the keys. You can set the keys in this method and then return false to not show the prompt</remarks>
        /// <param name="moduleEntity">The module entity.</param>
        /// <returns><c>true</c> if you want to prompt the user to specify the keys, <c>false</c> if you can specify the keys in this method.</returns>
        public bool am_PromptForKeysIfNoneSpecified(AB_GenerationModuleEntity moduleEntity)
        {
            return true;
        }

        /// <summary>
        /// Accelerator Method <c>am_AllowAddModule</c>: intervene with allowing a particular module to be added
        /// </summary>
        /// <remarks>This method is called Before the module is added to the module manager. You can return false if you don't want the module to be added.</remarks>
        /// <param name="moduleEntity">The module entity.</param>
        /// <returns><c>true</c> if you want to add the module to the module manager, <c>false</c> otherwise.</returns>
        public bool am_AllowAddModule(AB_GenerationModuleEntity moduleEntity)
        {
            switch (moduleEntity.FileName)
            {
                //Add a case for every module that shouldn't be allowed
                case "sysdiagrams":
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Accelerator Method <c>am_SetDefaultForModule</c>: Set Default for Module is called when a file is added to the Module Manager.
        /// </summary>
        /// <param name="moduleEntity">Module Entity that is being added to the Module Manager</param>
        /// <returns></returns>
        public void am_SetDefaultForModule(AB_GenerationModuleEntity moduleEntity)
        {
            if (moduleEntity == null || moduleEntity.AllColumns == null) return;

            // Set Module Level Rules before Column Rules
            SetModuleRulesBeforeColumnRules(Mode.InitialSetup, moduleEntity);

            foreach (var viewColumnEntity in moduleEntity.AllColumns)
            {
                // Set Generation defaults for each Column
                SetColumnRules(Mode.InitialSetup, moduleEntity, viewColumnEntity);
            }

            // Set Module Level Rules after Column Rules
            SetModuleRulesAfterColumnRules(Mode.InitialSetup, moduleEntity);
        }

        /// <summary>
        /// Accelerator Method <c>am_ViewColumnsAddedToModule</c>: View Columns Added to Module is called when a column is added or removed
        /// </summary>
        /// <param name="moduleEntity">Module Entity that contains the added or removed column</param>
        /// <returns></returns>
        public void am_ViewColumnsAddedToModule(AB_GenerationModuleEntity moduleEntity)
        {
            if (moduleEntity == null || moduleEntity.AllColumns == null) return;

            // Set Module Level Rules before Column Rules
            SetModuleRulesBeforeColumnRules(Mode.ColumnsChanged, moduleEntity);

            foreach (var viewColumnEntity in moduleEntity.AllColumns)
            {
                // Set Generation defaults for each Column
                SetColumnRules(Mode.ColumnsChanged, moduleEntity, viewColumnEntity);
            }

            // Set Module Level Rules after Column Rules
            SetModuleRulesAfterColumnRules(Mode.ColumnsChanged, moduleEntity);
        }

        /// <summary>
        /// Accelerator Method <c>am_BeforeAddJoinColumnToModule</c>: This method is called before the join column is added to the module.
        /// </summary>
        /// <param name="moduleEntity">The module entity.</param>
        /// <param name="joinField">The join field.</param>
        public void am_BeforeAddJoinColumnToModule(AB_GenerationModuleEntity moduleEntity, AB_GenerationViewColumnEntity joinField)
        {
            // Use the foreign key name minus the "id". Concat with "Name" if that exists in join field description, otherwise use full joinfield description
            var cd = joinField.JoinRelationship.KeyMaps.FirstOrDefault().SelectedForeignKeyViewColumn.ColumnDescription;
            if (cd.ToUpper().EndsWith("INTERNAL ID")) { cd = cd.Remove(cd.Length - 11).TrimEnd(); }

            // Gets the foreign key name and removes "Internal ID" from the end
            var ep = joinField.JoinRelationship.KeyMaps.FirstOrDefault().SelectedForeignKeyViewColumn.EntityPropertyName;
            if (ep.ToUpper().EndsWith("INTERNALID")) { ep = ep.Remove(ep.Length - 10).TrimEnd(); }

            if (joinField.JoinFieldDescription.ToUpper().Contains("NAME"))
            {
                joinField.ColumnDescription = string.Format("{0} {1}", cd, "Name");
                joinField.EntityPropertyName = string.Format("{0}{1}", ep, "Name");
            }
            else
            {
                joinField.ColumnDescription = string.Format("{0} {1}", cd, joinField.JoinFieldDescription);
                joinField.EntityPropertyName = string.Format("{0}{1}", ep, joinField.JoinEntityPropertyName);
            }
        }

        /// <summary>
        /// Accelerator Method <c>am_AllModulesCompletedLoading</c>: This method is called after all modules completed being added to the module manager
        /// </summary>
        /// <param name="generationModuleCollection">The generation module collection.</param>
        public void am_AllModulesCompletedLoading(ObservableCollection<AB_GenerationModuleEntity> generationModuleCollection)
        {

        }

        #endregion

        #region Rules

        internal void SetModuleRulesBeforeColumnRules(Mode mode, AB_GenerationModuleEntity moduleEntity)
        {
            switch (mode)
            {
                case Mode.InitialSetup:
                    // Set Module Name.  This will also set the image name
                    moduleEntity.ModuleName = FormatModuleName(moduleEntity.FileDescription);

                    // Set the Module Description to Module Name
                    moduleEntity.ModuleDescription = moduleEntity.ModuleName;

                    // Module has Auto Generated Keys
                    moduleEntity.FileHasAutoGeneratedKey = true;

                    moduleEntity.DetailLabelPosition = AB_GenerationModuleEntity.AB_LabelPosition.Left;
                    break;

                case Mode.ColumnsChanged:
                    break;
            }
        }

        internal void SetColumnRules(Mode mode, AB_GenerationModuleEntity moduleEntity, AB_GenerationViewColumnEntity viewColumnEntity)
        {
            switch (mode)
            {
                case Mode.InitialSetup:
                    // SetAdd Attribute [AB_AutoIncremented] to Identity Field
                    if (IsIdentityField(viewColumnEntity))
                    {
                        viewColumnEntity.IsIdentity = false;
                        viewColumnEntity.IsRequiredField = false;
                        if (viewColumnEntity.Type == "NUMERIC(8.0)")
                        {
                            viewColumnEntity.IsAutoIncrementedInCode = true;
                        }
                    }

                    if (IsAuditTabField(viewColumnEntity))
                    {
                        viewColumnEntity.IsDetailField = false;
                        viewColumnEntity.IsAuditStampField = true;
                        viewColumnEntity.AuditStampFieldType = AuditStampType(viewColumnEntity.Name);
                    }

                    if (IsDeleteFlagField(viewColumnEntity))
                    {
                        viewColumnEntity.IsDeleteFlagField = true;
                    }

                    if (IsExtendedSearchField(viewColumnEntity))
                    {
                        viewColumnEntity.IsExplorerBarField = true;
                        viewColumnEntity.IsExtendedSearchField = true;
                    }
                    else
                    {
                        viewColumnEntity.IsExplorerBarField = true;
                        viewColumnEntity.IsExtendedSearchField = false;
                    }

                    if (IsExtendedContentWindowField(viewColumnEntity))
                    {
                        viewColumnEntity.IsContentWindowField = true;
                        viewColumnEntity.ShowInExtendedView = true;
                    }
                    else
                    {
                        viewColumnEntity.ShowInExtendedView = false;
                    }

                    if (viewColumnEntity.IsKey)
                    {
                        viewColumnEntity.IsContentWindowField = true;
                        viewColumnEntity.ShowInExtendedView = false;
                        viewColumnEntity.Visible = false;
                    }

                    // This is done to eliminate conflicts with having the Internal ID referenced multiple times in a file
                    if (viewColumnEntity.EntityPropertyName.ToUpper() == "INTERNALID")
                    {
                        viewColumnEntity.EntityPropertyName = moduleEntity.ModuleName.Replace(" ", "") + "InternalID";
                        viewColumnEntity.ColumnDescription = moduleEntity.ModuleName + " Internal ID";
                    }

                    // Set Numeric(8) date fields as Property Type of DateTime and Field Visualization of Date
                    if ((viewColumnEntity.Name.Substring(viewColumnEntity.Name.Length - 2) == "DT") && ((viewColumnEntity.Type == "NUMERIC(8.0)") || viewColumnEntity.Type == "DATE(4)"))
                    {
                        viewColumnEntity.PropertyType = AB_PropertyTypes.DateTime;
                        viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_DatePicker;

                        if ((viewColumnEntity.Type == "NUMERIC(8.0)"))
                        {
                            viewColumnEntity.AdditionalDataMapParameters = "databaseFieldType: AB_EntityFieldType.Decimal";
                        }
                    }

                    // Set Numeric(6) Time fields as Property Type of TimeSpan and Field Visualization of Time
                    if ((viewColumnEntity.Name.Substring(viewColumnEntity.Name.Length - 2) == "TM") && (viewColumnEntity.Type == "NUMERIC(6.0)"))
                    {
                        viewColumnEntity.PropertyType = AB_PropertyTypes.TimeSpan;
                        viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_TimePicker;
                        viewColumnEntity.AdditionalDataMapParameters = "databaseFieldType: AB_EntityFieldType.Decimal";
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

                    viewColumnEntity.IsTitleField = IsDetailTitleField(viewColumnEntity);
                    if (!viewColumnEntity.IsTitleField)
                    {
                        //If not a title field, then hide on small devices
                        viewColumnEntity.WebMarkupTHDataAttributes = "data-hide=\"phone,tablet\"";
                    }

                    if (IsCurrencyField(viewColumnEntity))
                    {
                        viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_Currency;
                    }

                    break;

                case Mode.ColumnsChanged:
                    if (viewColumnEntity.IsVirtual)
                    {
                        //Do Not Check Virtuals on the Detail Tab
                        viewColumnEntity.IsDetailField = false;

                        // Uncheck Real Fields Content Window that make up the Virtual Field
                        if (viewColumnEntity.VirtualFieldType == AB_VirtualFieldTypes.Concatenation)
                        {
                            foreach (var vce in from vcf in viewColumnEntity.VirtualCalculationFields where vcf.VirtualPropertyType == AB_VirtualPropertyType.Field from vce in moduleEntity.AllColumns where vce.EntityPropertyName == vcf.VirtualPropertyName select vce)
                            {
                                vce.Visible = false;
                                viewColumnEntity.ContentWindowDisplaySequence = vce.ContentWindowDisplaySequence += 1;
                            }
                        }
                    }

                    //We have to run this again here since CodeFactory overrides this setting
                    if (viewColumnEntity.ViewField.EndsWith("M1") && viewColumnEntity.FieldLength == 100)
                    {
                        viewColumnEntity.IsContentWindowField = false;
                        viewColumnEntity.IsExplorerBarField = false;
                    }
                    break;
            }
        }

        internal void SetModuleRulesAfterColumnRules(Mode mode, AB_GenerationModuleEntity moduleEntity)
        {
            switch (mode)
            {
                case Mode.InitialSetup:

                    // Set the Default View
                    _defaultViewSet = false;
                    if (moduleEntity.GenerationModuleExplorers[0] != null && moduleEntity.GenerationModuleExplorers[0].GenerationModuleExplorerViews != null)
                    {
                        foreach (var view in moduleEntity.GenerationModuleExplorers[0].GenerationModuleExplorerViews.OrderBy(x => x.ViewName))
                        {
                            _SetDefaultView(view, moduleEntity);
                        }
                    }

                    // Add View By Name if there is a name field and Set to Default
                    AddViewByViewField(moduleEntity, "Name");

                    // Set the Drop Down Label to Module Name + Field Name/Desc
                    foreach (var dd in moduleEntity.DropDowns)
                    {
                        foreach (var ddViewFields in dd.DropDownViewFields.Where(ddViewFields => ddViewFields.ColumnDescription.ToUpper().Equals("NAME")))
                        {
                            dd.DropDownLabel = moduleEntity.ModuleName + " " + ddViewFields.ColumnDescription;

                            break;
                        }
                    }

                    break;

                case Mode.ColumnsChanged:
                    break;
            }

            MoveFieldsInContentWindow(moduleEntity);

            // Uncheck internal Key for Title if other non-key fields are checked and the key is an Identity or AutoGenerated Field
            if (moduleEntity.AllColumns.Count(p => p.IsTitleField && !p.IsKey) > 0)
            {
                moduleEntity.AllColumns.Where(t => t.IsKey && (t.IsIdentity || t.IsAutoIncrementedInCode)).ForEach(u => u.IsTitleField = false);
            }
        }

        private bool _defaultViewSet;

        /// <summary>
        /// Set Default View
        /// </summary>
        /// <param name="viewEntity">View Entity</param>
        /// <param name="moduleEntity">Module Entity</param>
        /// <returns></returns>
        private void _SetDefaultView(AB_GenerationModuleExplorerViewEntity viewEntity, AB_GenerationModuleEntity moduleEntity)
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

        #endregion

        #region Relationships

        private ObservableCollection<AB_SchemaRelationshipEntity> _GetDatabaseRelationships()
        {
            var relationships = new ObservableCollection<AB_SchemaRelationshipEntity>();

            // Customers have One to Many Orders
            relationships.Add(_AddRelationship("EASYBUYDEM", "YD1C", "YD1CIID", "EASYBUYDEM", "YD1O", "YD1O1CID", SchemaRelationshipType.OneToMany));
            // Customers have One to Many Shipping Addresses
            relationships.Add(_AddRelationship("EASYBUYDEM", "YD1C", "YD1CIID", "EASYBUYDEM", "YD1S", "YD1S1CID", SchemaRelationshipType.OneToMany));
            // Customers have Sub Customers
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
                PrimarySchema = primarySchema,
                PrimaryTable = primaryTable,
                PrimaryKeyColumn = primaryKeyColumn,
                ForeignSchema = foreignSchema,
                ForeignTable = foreignTable,
                ForeignKeyColumn = foreignKeyColumn,
                RelationshipType = relationshipType,
            };
        }

        #endregion Relationships

        #region Default Standards

        #region Identity Fields

        /// <summary>
        /// Add all Identity Field standards to the collection
        /// </summary>
        internal virtual List<string> IdentityField
        {
            get { return _identityField; }
            set { _identityField = value; }
        }

        private List<string> _identityField = new List<string>
            {
                "ID"
            };

        /// <summary>
        /// Logic to determine if the field is an Identity Field
        /// </summary>
        internal virtual bool IsIdentityField(AB_GenerationViewColumnEntity vce)
        {
            return _identityField.Any(x => vce.ViewField.ToUpper().EndsWith(x.ToUpper())) &&
                   ((vce.IsKey));
        }

        #endregion Identity Fields

        #region Audit Stamp Fields

        /// <summary>
        /// Add all Audit Stamp standards to the collection and assign an Audit Stamp Type
        /// </summary>
        internal virtual Dictionary<string, AB_AuditStampTypes> AuditStamps
        {
            get { return _auditStamps; }
            set { _auditStamps = value; }
        }

        private Dictionary<string, AB_AuditStampTypes> _auditStamps = new Dictionary<string, AB_AuditStampTypes>
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

        /// <summary>
        /// Logic to determine if the field should go on the Audit Stamp Tab
        /// </summary>
        internal virtual bool IsAuditTabField(AB_GenerationViewColumnEntity vce)
        {
            return AuditStamps.Any(x => vce.Name.ToUpper().Contains(x.Key.ToUpper())) || ((vce.IsKey) && (vce.IsIdentity)) || IsDeleteFlagField(vce);
        }

        /// <summary>
        /// Logic to determine the Audit Stamp Type
        /// </summary>
        internal virtual AB_AuditStampTypes AuditStampType(string name)
        {
            var isAudit = _auditStamps.Any(x => name.ToUpper().Contains(x.Key.ToUpper()));
            return isAudit ? _auditStamps.Where(aud => name.ToUpper().Contains(aud.Key.ToUpper())).Select(aud => aud.Value).FirstOrDefault() : AB_AuditStampTypes.Undefined;
        }

        #endregion Audit Stamp Fields

        #region Content Window Extended Fields

        /// <summary>
        ///  Add all Content Window Extended Field standards to the collection
        /// </summary>
        internal virtual List<string> ExtendedContentWindowField
        {
            get { return _extendedContentWindowField; }
            set { _extendedContentWindowField = value; }
        }

        private List<string> _extendedContentWindowField = new List<string>
            {
                "PTID",
                "PTRL"
            };

        /// <summary>
        /// Logic to determine if the field is an Extended Content Window Field
        /// </summary>
        internal virtual bool IsExtendedContentWindowField(AB_GenerationViewColumnEntity vce)
        {
            return ExtendedContentWindowField.Any(x => vce.ViewField.ToUpper().EndsWith(x.ToUpper())) || vce.IsAuditStampField ||
                                  ((vce.IsKey) && (vce.IsIdentity));
        }

        #endregion Content Window Extended Fields

        #region Search Explorer Bar Extended Fields

        /// <summary>
        /// Add all Search Explorer Bar Extended Field standards to the collection
        /// </summary>
        internal virtual List<string> ExtendedSearchField
        {
            get { return _extendedSearchField; }
            set { _extendedSearchField = value; }
        }

        private List<string> _extendedSearchField = new List<string>
        {
            "PTID",
            "PTRL"
        };

        /// <summary>
        /// Logic to determine if the field is an Extended Search Field
        /// </summary>
        internal virtual bool IsExtendedSearchField(AB_GenerationViewColumnEntity vce)
        {
            return ExtendedSearchField.Any(x => vce.ViewField.ToUpper().EndsWith(x.ToUpper())) || (vce.IsAuditStampField) ||
                   ((vce.IsKey) && (vce.IsIdentity));
        }

        #endregion Search Explorer Bar Extended Fields

        #region Detail Title Fields

        /// <summary>
        ///  Add all Detail Title Field standards to the collection
        /// </summary>
        internal virtual List<string> DetailTitleField
        {
            get { return _detailTitleField; }
            set { _detailTitleField = value; }
        }

        private List<string> _detailTitleField = new List<string>
            {
                "NM",
                "NLN",
                "NFN"
            };

        /// <summary>
        /// Logic to determine if the field is a Title Field
        /// </summary>
        internal virtual bool IsDetailTitleField(AB_GenerationViewColumnEntity vce)
        {
            return DetailTitleField.Any(x => vce.ViewField.ToUpper().EndsWith(x.ToUpper())) || (vce.IsKey);
        }

        #endregion Detail Title Fields

        #region Soft Delete Fields

        /// <summary>
        /// Add all Soft Delete Field standards to the collection
        /// </summary>
        internal virtual List<string> DeleteFlagField
        {
            get { return _deleteFlagField; }
            set { _deleteFlagField = value; }
        }

        private List<string> _deleteFlagField = new List<string>
            {
                "IsDeleted"
            };

        /// <summary>
        /// Logic to determine if the field is a Delete Flag Field
        /// </summary>
        internal virtual bool IsDeleteFlagField(AB_GenerationViewColumnEntity vce)
        {
            return DeleteFlagField.Any(x => vce.ViewField.ToUpper().Contains(x.ToUpper()));
        }

		#endregion Soft Delete Fields

		#region Other

		private bool IsCurrencyField(AB_GenerationViewColumnEntity column)
		{
			List<string> keywords = new List<string>() { "cost", "price" };

			return column.PropertyType == AB_PropertyTypes.Decimal &&
				   keywords.Any(x => column.NewEntityPropertyName.ToLower().Contains(x.ToLower()));
		}

		#endregion

		#endregion Default Standards

		#region Helper Methods

		/// <summary>
		/// Format Module Name
		/// </summary>
		/// <param name="s">Name to be formateed</param>
		/// <returns></returns>
		internal static string FormatModuleName(string s)
        {
            // Replace any Keywords
            s = StringBuilderReplaceKeywords(new StringBuilder(s, s.Length * 2));

            // Put a space before each Capital Letter
            var s1 = s;
            s = string.Join(
                string.Empty,
                s.Select((x, i) => (
                    char.IsUpper(x) && i > 0 &&
                    (char.IsLower(s1[i - 1]) || (i < s1.Count() - 1 && char.IsLower(s1[i + 1])))
                    )
                    ? " " + x
                    : x.ToString()));
            // Limit to 60 Characters
            if (s.Length > 60)
            {
                s = s.Substring(0, 60);
            }

            s = s.Replace("  ", " ");

            return s;
        }

        /// <summary>
        /// Title Case
        /// </summary>
        /// <param name="s">Name to be formateed</param>
        /// <returns></returns>

        internal static string ToTitleCase(string s)
        {
            var cultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            return cultureInfo.TextInfo.ToTitleCase(s.ToLower());
        }

        /// <summary>
        /// Add a View by Name if a column exists that contains Name and set the View as the Default
        /// </summary>
        /// <param name="moduleEntity">Module Entity</param>
        /// <returns></returns>
        internal void AddViewByViewField(AB_GenerationModuleEntity moduleEntity, string viewField)
        {
            if (moduleEntity.AllColumns.Any(x => x.ViewField.ToUpper().Equals(viewField.ToUpper())))
            {
                var viewColumns = new ObservableCollection<AB_GenerationViewColumnEntity>();
                var nameColumn = moduleEntity.AllColumns.FirstOrDefault(x => x.ViewField.ToUpper().Equals(viewField.ToUpper()));
                if (nameColumn != null)
                {
                    viewColumns.Add(nameColumn);
                }
                var keyColumns = moduleEntity.AllColumns.Where(x => x.IsKey);
                foreach (var vc in keyColumns.Where(vc => !viewColumns.Contains(vc)))
                {
                    viewColumns.Add(vc);
                }

                var viewToAdd = WizardShared.am_CreateDefaultView(viewField, viewField, viewColumns);
                if (viewToAdd == null)
                {
                    MessageBox.Show("Error adding View: By " + viewField + " to module: " + moduleEntity.ModuleName);
                    return;
                }
                WizardShared.am_AddViewToModule(viewToAdd, moduleEntity);

                WizardShared.am_SetDefaultView(viewToAdd, moduleEntity);

                WizardShared.am_SetFirstViewColumnAsFirstContentWindowColumn(moduleEntity);
            }
        }


        /// <summary>
        /// Move fields around in Content Window.
        /// </summary>
        /// <param name="moduleEntity">Module Entity</param>
        /// <returns></returns>
        internal void MoveFieldsInContentWindow(AB_GenerationModuleEntity moduleEntity)
        {
            var contentWindowItems =
                new ObservableCollection<AB_GenerationViewColumnEntity>(
                    moduleEntity.AllColumns.OrderBy(x => x.ContentWindowDisplaySequence).ToList());
            var itemsToMoveBottom = new ObservableCollection<AB_GenerationViewColumnEntity>();
            var itemsToMoveTop = new ObservableCollection<AB_GenerationViewColumnEntity>();

            foreach (var vce in contentWindowItems)
            {
                // Move Extended View Items to Bottom
                if (vce.ShowInExtendedView == true)
                {
                    itemsToMoveBottom.Add(vce);
                }
                // Move Join Fields To Top
                else if (vce.IsJoinedField)
                {
                    itemsToMoveTop.Add(vce);
                }
            }

            CollectionHelperMethods.MoveItemsTop(itemsToMoveTop, contentWindowItems);
            CollectionHelperMethods.MoveItemsBottom(itemsToMoveBottom, contentWindowItems);

            // Resequence by 5
            int contentWindowDispSeq = 5;
            foreach (AB_GenerationViewColumnEntity vce in contentWindowItems)
            {
                vce.ContentWindowDisplaySequence = contentWindowDispSeq;
                contentWindowDispSeq += 5;
            }
        }

        /// <summary>
        /// String Builder Replace Keywords
        /// </summary>
        /// <param name="data">Data to Replace Keywords</param>
        /// <param name="moduleEntity">Module Entity</param>
        /// <returns></returns>
        internal static string StringBuilderReplaceKeywords(StringBuilder data)
        {
            var values = new Dictionary<string, string>
            {
                // {"BusinessEntity", "Person"}
            };

            // Replace only if not exactly equal
            foreach (string k in values.Keys.Where(k => data.ToString() != k))
            {
                data.Replace(k, values[k]);
            }

            return data.ToString();
        }

        #endregion Helper Methods
    }
}