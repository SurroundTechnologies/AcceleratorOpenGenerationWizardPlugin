using A4DN.CF.SchemaEntities;
using A4DN.CF.WizardShared;
using A4DN.Core.BOS.Base;
using A4DN.Core.BOS.FrameworkEntity;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GenerationWizardPlugin
{
    public class WizardDefaultsTemplate : AB_IGenerationWizardDefault
    {
        internal enum Mode { InitialSetup, ColumnsChanged };

        // Generation Wizard Shared Data
        internal AB_GenerationWizardShared _WizardShared;

        #region Generic Code

        public virtual void am_Initialize(AB_GenerationWizardShared generationWizardShared)
        {
            // Relationships are pulled from the database access routes. If no relationships are defined on the database, you can define the relationships in the _AddDatabaseRelationships method.
            _AddDatabaseRelationships(generationWizardShared.ap_DatabaseRelationships);

            _WizardShared = generationWizardShared;
        }

        /// <summary>
        /// Prompts for keys if none specified.
        /// </summary>
        /// <remarks>This method is called when no keys are found on the physical file. Returning True will prompt the user to select the keys. You can set the keys in this method and then return false to not show the prompt</remarks>
        /// <param name="moduleEntity">The module entity.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public virtual bool am_PromptForKeysIfNoneSpecified(AB_GenerationModuleEntity moduleEntity)
        {
            #region Example Code
            //// Use Keys from L1 view if it exists
            //foreach (var modView in moduleEntity.GenerationModuleExplorers.FirstOrDefault().GenerationModuleExplorerViews)
            //{
            //    if (modView.ViewName == moduleEntity.FileName + "L1" || modView.ViewName == moduleEntity.FileName + "L01")
            //    {
            //        modView.ViewName = moduleEntity.FileName;

            //        // Set As Default View
            //        _WizardShared.am_SetDefaultView(modView, moduleEntity);

            //        foreach (var viewCol in modView.GenerationViewColumns)
            //        {
            //            var col = moduleEntity.AllColumns.Where(c => c.Name == viewCol.ViewField).FirstOrDefault();
            //            col.IsKey = true;

            //        }

            //        return false;
            //    }
            //}

            #endregion


            return true;
        }

        /// <summary>
        /// The method is called Before the module is added to the module manager. You can return false if you don't want the module to be added.
        /// </summary>
        /// <param name="moduleEntity">The module entity.</param>
        /// <returns><c>true</c> if you want to add the module to the module manager, <c>false</c> otherwise.</returns>
        public virtual bool am_AllowAddModule(AB_GenerationModuleEntity moduleEntity)
        {
            if (moduleEntity.FileName == "sysdiagrams")
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// am_SetDefaultForModule: Set Default for Module is called when a file is added to the Module Manager.
        /// </summary>
        /// <param name="moduleEntity">Module Entity that is being added to the Module Manager</param>
        /// <returns></returns>
        public virtual void am_SetDefaultForModule(AB_GenerationModuleEntity moduleEntity)
        {
            if (moduleEntity == null)
                return;

            if (moduleEntity.AllColumns == null) return;
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
        /// am_ViewColumnsAddedToModule: View Columns Added to Module is called when a column is added or removed
        /// </summary>
        /// <param name="moduleEntity">Module Entity that contains the added or removed column</param>
        /// <returns></returns>
        public virtual void am_ViewColumnsAddedToModule(AB_GenerationModuleEntity moduleEntity)
        {
            if (moduleEntity == null)
                return;

            if (moduleEntity.AllColumns == null) return;
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
        /// am_BeforeAddJoinColumnToModule: This method is called before the join column is added to the module.
        /// </summary>
        /// <param name="moduleEntity">The module entity.</param>
        /// <param name="joinField">The join field.</param>
        public virtual void am_BeforeAddJoinColumnToModule(AB_GenerationModuleEntity moduleEntity, AB_GenerationViewColumnEntity joinField)
        {

        }

        /// <summary>
        /// am_AllModulesCompletedLoading: This method is called after all modules completed being added to the module manager
        /// </summary>
        /// <param name="generationModuleCollection">The generation module collection.</param>
        public virtual void am_AllModulesCompletedLoading(ObservableCollection<AB_GenerationModuleEntity> generationModuleCollection)
        {

        }

        #endregion Generic Code

        // Most custom development will be done in the Rules region.
        // You can insert custom logic before the column rules, for the column rules, and after the column rules.
        #region Rules

        /// <summary>
        /// Set Module level Rules before the Column Rules are Processed
        /// </summary>
        internal virtual void SetModuleRulesBeforeColumnRules(Mode mode, AB_GenerationModuleEntity moduleEntity)
        {
            switch (mode)
            {
                case Mode.InitialSetup:
                    // Set Module Name.  This will also set the image name
                    moduleEntity.ModuleName = FormatModuleName(moduleEntity.ModuleName).Replace("File", "").Replace("Table", "").Replace("Master", "").Trim();

                    // Set the Module Description to Module Name
                    moduleEntity.ModuleDescription = moduleEntity.ModuleName;

                    // Module has Auto Generated Keys
                    moduleEntity.FileHasAutoGeneratedKey = false;

                    break;

                case Mode.ColumnsChanged:
                    break;
            }
        }

        /// <summary>
        /// Set Column Rules
        /// </summary>
        internal virtual void SetColumnRules(Mode mode, AB_GenerationModuleEntity moduleEntity, AB_GenerationViewColumnEntity viewColumnEntity)
        {
            switch (mode)
            {
                case Mode.InitialSetup:

                    // Set Column Description to have Title Case
                    viewColumnEntity.ColumnDescription = ToTitleCase(viewColumnEntity.ColumnDescription);

                    // Property Name is Uppercase - Convert to titlecase
                    viewColumnEntity.EntityPropertyName = AB_GenerationWizardShared.am_RemoveIllegalCharacters(viewColumnEntity.ColumnDescription);

                    #region Example Code

                    //// Set Numeric(8) date fields as Property Type of DateTime and Field Visualization of Date
                    //if (viewColumnEntity.ColumnDescription.EndsWith("Date") && viewColumnEntity.Type == "NUMERIC(8)")
                    //{
                    //    viewColumnEntity.PropertyType = AB_PropertyTypes.DateTime;
                    //    viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_DatePicker;
                    //    viewColumnEntity.AdditionalDataMapParameters = "databaseFieldType: AB_EntityFieldType.Decimal";
                    //}

                    //// Apply Currency Format String to Decimals
                    //if (viewColumnEntity.PropertyType == AB_PropertyTypes.Decimal && viewColumnEntity.FieldDecimals != 0)
                    //{
                    //    viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_Currency;
                    //    viewColumnEntity.StringFormat = "c";
                    //}

                    //// Apply Phone Format String
                    //if (viewColumnEntity.ColumnDescription.Contains("Phone"))
                    //{
                    //    viewColumnEntity.PropertyType = AB_PropertyTypes.Decimal;
                    //    viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_PhoneNumber;
                    //    viewColumnEntity.StringFormat = "(###) ###-####";
                    //}

                    //// Set Active Char(1) Property Type of Boolean and Field Visualization of CheckBox
                    //if (viewColumnEntity.ColumnDescription.Contains("Active") && viewColumnEntity.Type == "CHAR(1)")
                    //{
                    //    viewColumnEntity.PropertyType = AB_PropertyTypes.Boolean;
                    //    viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_CheckBoxWithLabel;
                    //}

                    //// Apply Email Field Visualization
                    //if (viewColumnEntity.ColumnDescription.Contains("Email"))
                    //{
                    //    viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_EmailAddress;
                    //}

                    //if (viewColumnEntity.IsKey && moduleEntity.FileHasAutoGeneratedKey == true)
                    //{
                    //    viewColumnEntity.Visible = false;
                    //}

                    #endregion Example Code

                    viewColumnEntity.IsTitleField = IsDetailTitleField(viewColumnEntity);

                    if (IsIdentityField(viewColumnEntity))
                    {
                        viewColumnEntity.IsIdentity = true;
                        viewColumnEntity.IsRequiredField = false;
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
                    }
                    viewColumnEntity.ShowInExtendedView = false;

                    if (viewColumnEntity.IsKey)
                    {
                        viewColumnEntity.IsRequiredField = true;
                    }

                    if (!viewColumnEntity.IsTitleField)
                    {
                        //If not a title field, then hide on small devices
                        viewColumnEntity.WebMarkupTHDataAttributes = "data-hide=\"phone,tablet\"";
                    }

                    break;

                case Mode.ColumnsChanged:

                    break;
            }
        }

        /// <summary>
        /// Set Module level Rules after the Column Rules have been processed
        /// </summary>
        internal virtual void SetModuleRulesAfterColumnRules(Mode mode, AB_GenerationModuleEntity moduleEntity)
        {
            switch (mode)
            {
                case Mode.InitialSetup:

                    SetViewDescriptionsToColumnNames(moduleEntity);

                    #region Example Code

                    //// Add View Standards
                    //AddViewByViewFields(moduleEntity, moduleEntity.FileName + "AV1", new List<string> { "CFNAME", "CLNAME" });
                    //AddViewByViewFields(moduleEntity, moduleEntity.FileName + "AV2", new List<string> { "CLNAME", "CFNAME" });

                    //// Add Joins
                    //if (_WizardShared.ap_Modules != null)
                    //{
                    //    foreach (var module in _WizardShared.ap_Modules)
                    //    {
                    //        if (module.AllColumns != null)
                    //        {
                    //            // Add Image Path join if CUSTNO and PRODNO Exist and are Keys
                    //            if (module.AllColumns.Where(c => (c.Name == "CUSTNO" || c.Name == "PRODNO") && c.IsKey).Count() > 0)
                    //            {
                    //                var joinFields = new Dictionary<string, string>();
                    //                joinFields.Add("PATH", "Image");
                    //                AddJoinFields(module, "CSIMGP", joinFields);

                    //                var joinField = module.AllColumns.Where(c => (c.Name == "CSIMGP.PATH")).FirstOrDefault();
                    //                if (joinField != null)
                    //                {
                    //                    if (module.FileName == "CSCSTP")
                    //                    {
                    //                        joinField.WebMarkupTDInnerHTML = "'<img src= ' + data + ' height=\"85\" width=\"64\"/>'";
                    //                    }
                    //                    else
                    //                    {
                    //                        joinField.WebMarkupTDInnerHTML = "'<img src= ' + data + ' height=\"64\" width=\"64\"/>'";
                    //                    }

                    //                    joinField.FieldVisualization = AB_FieldVisualizations.AB_ImageUrl;
                    //                    joinField.IsDetailField = true;

                    //                    MoveFieldsInContentWindow(module);
                    //                }

                    //            }

                    //            // Add First Name and Last Name joins if file contains CUSTNO and it is not the key
                    //            if (module.AllColumns.Where(c => (c.Name == "CUSTNO" || c.Name == "CUST#") && !c.IsKey).Count() > 0)
                    //            {
                    //                var joinFields = new Dictionary<string, string>();
                    //                joinFields.Add("CFNAME", "Customer First Name");
                    //                joinFields.Add("CLNAME", "Customer Last Name");
                    //                AddJoinFields(module, "CSCSTP", joinFields);

                    //                var joinField = module.AllColumns.Where(c => (c.Name == "CSCSTP.CFNAME")).FirstOrDefault();
                    //                if (joinField != null)
                    //                {
                    //                    joinField.IsExplorerBarField = true;
                    //                }

                    //                joinField = module.AllColumns.Where(c => (c.Name == "CSCSTP.CLNAME")).FirstOrDefault();
                    //                if (joinField != null)
                    //                {
                    //                    joinField.IsExplorerBarField = true;
                    //                }

                    //            }

                    //            // Add Product joins if file contains PRODNO and file has more than 1 key
                    //            if (module.AllColumns.Where(c => (c.Name == "PRODNO") && c.IsKey).Count() > 0)
                    //            {
                    //                if (module.AllColumns.Where(c => c.IsKey).Count() > 1)
                    //                {
                    //                    var joinFields = new Dictionary<string, string>();
                    //                    joinFields.Add("PRODTYPE", "Product Type");
                    //                    joinFields.Add("DESCRP", "Product Description");
                    //                    joinFields.Add("SELLPR", "Product Selling Price");
                    //                    AddJoinFields(module, "CSINVP", joinFields);

                    //                    var joinField = module.AllColumns.Where(c => (c.Name == "CSINVP.DESCRP")).FirstOrDefault();
                    //                    if (joinField != null)
                    //                    {
                    //                        joinField.IsExplorerBarField = true;
                    //                    }
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                    //// Add Full name Virtual
                    //var firstName = moduleEntity.AllColumns.Where(c => c.EntityPropertyName == "FirstName" && !c.IsJoinedField).FirstOrDefault();
                    //var lastName = moduleEntity.AllColumns.Where(c => c.EntityPropertyName == "LastName" && !c.IsJoinedField).FirstOrDefault();
                    //if (firstName != null && lastName != null)
                    //{
                    //    var virtualField = AddConcatenationVirtualField(moduleEntity, "Full Name", new List<AB_GenerationViewColumnEntity> { lastName, firstName }, " ,");

                    //    if (virtualField != null)
                    //    {
                    //        virtualField.IsDetailField = false;
                    //    }
                    //}

                    //// Add Full Address Virtual
                    //var streetAddress = moduleEntity.AllColumns.Where(c => c.EntityPropertyName == "StreetAddress").FirstOrDefault();
                    //var city = moduleEntity.AllColumns.Where(c => c.EntityPropertyName == "City").FirstOrDefault();
                    //var state = moduleEntity.AllColumns.Where(c => c.EntityPropertyName == "State").FirstOrDefault();
                    //var zip4 = moduleEntity.AllColumns.Where(c => c.EntityPropertyName == "Zip4").FirstOrDefault();
                    //if (streetAddress != null && city != null && state != null && zip4 != null)
                    //{
                    //    var virtualField = AddConcatenationVirtualField(moduleEntity, "Full Address", new List<AB_GenerationViewColumnEntity> { streetAddress, city, state, zip4 }, " ");

                    //    if (virtualField != null)
                    //    {
                    //        virtualField.IsDetailField = false;
                    //    }
                    //}

                    ////Remove all views that begin with "IX"
                    //foreach (var view in moduleEntity.GenerationModuleExplorers[0].GenerationModuleExplorerViews.OrderBy(x => x.ViewName).Where(w => w.ViewName.StartsWith("IX")))
                    //{
                    //    _WizardShared.am_RemoveViewFromModule(view, moduleEntity);
                    //}

                    #endregion Example Code

                    break;

                case Mode.ColumnsChanged:

                    break;
            }
        }

        #endregion Rules

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
            // you can also use the undefined Audit Stamp
            //{"AuditStampName", AB_AuditStampTypes.Undefined},
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
             "ID",
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
            // "IsDeleted"
        };

        /// <summary>
        /// Logic to determine if the field is a Delete Flag Field
        /// </summary>
        internal virtual bool IsDeleteFlagField(AB_GenerationViewColumnEntity vce)
        {
            return DeleteFlagField.Any(x => vce.ViewField.ToUpper().Contains(x.ToUpper()));
        }

        #endregion Soft Delete Fields

        #endregion Default Standards

        // Here you can relationships if they are not already being added via database constraints
        #region Relationships

        private void _AddDatabaseRelationships(ObservableCollection<AB_SchemaRelationshipEntity> relationships)
        {
            if (relationships == null)
            {
                relationships = new ObservableCollection<AB_SchemaRelationshipEntity>();
            }

            // Example
            //relationships.Add(_AddRelationship("CSCSTP", "CUSTNO", "CSORDP", "CUST#", SchemaRelationshipType.OneToMany));
        }

        private AB_SchemaRelationshipEntity _AddRelationship(string primaryTable, string primaryKeyColumn, string foreignTable, string foreignKeyColumn, SchemaRelationshipType relationshipType)
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

        // In additon to the below helper methods, you can also use the methods defined in _WizardShared to further manipulate the generation
        #region Helper Methods

        /// <summary>
        /// Format Module Name
        /// </summary>
        /// <param name="s">Name to be formated</param>
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
        /// <param name="s">Name to be formated</param>
        /// <returns></returns>

        internal static string ToTitleCase(string s)
        {
            var cultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            return cultureInfo.TextInfo.ToTitleCase(s.ToLower());
        }

        /// <summary>
        /// Adds the view by view fields.
        /// </summary>
        /// <param name="moduleEntity">The module entity.</param>
        /// <param name="viewName">Name of the view.</param>
        /// <param name="viewFields">The view fields.</param>
        /// <returns>AB_GenerationModuleExplorerViewEntity.</returns>
        internal AB_GenerationModuleExplorerViewEntity AddViewByViewFields(AB_GenerationModuleEntity moduleEntity, string viewName, List<string> viewFields, bool defaultView = false)
        {
            var viewColumns = new ObservableCollection<AB_GenerationViewColumnEntity>();

            foreach (var viewField in viewFields)
            {
                if (moduleEntity.AllColumns.Any(x => x.ViewField.ToUpper().Equals(viewField.ToUpper())))
                {
                    var nameColumn = moduleEntity.AllColumns.FirstOrDefault(x => x.ViewField.ToUpper().Equals(viewField.ToUpper()));
                    if (nameColumn != null)
                    {
                        viewColumns.Add(nameColumn);
                    }
                }
            }

            if (viewColumns.Count() > 0)
            {
                var viewDescription = "";
                foreach (var gvc in viewColumns)
                {
                    viewDescription = viewDescription == ""
                        ? gvc.ColumnDescription
                        : viewDescription + ", " + gvc.ColumnDescription;
                }

                var keyColumns = moduleEntity.AllColumns.Where(x => x.IsKey);
                foreach (var vc in keyColumns.Where(vc => !viewColumns.Contains(vc)))
                {
                    viewColumns.Add(vc);
                }

                var viewToAdd = _WizardShared.am_CreateDefaultView(viewName, viewDescription, viewColumns);
                if (viewToAdd == null)
                {
                    MessageBox.Show("Error adding View: " + viewName + " to module: " + moduleEntity.ModuleName);
                    return null;
                }
                _WizardShared.am_AddViewToModule(viewToAdd, moduleEntity);

                if (defaultView)
                {
                    _WizardShared.am_SetDefaultView(viewToAdd, moduleEntity);

                    _WizardShared.am_SetFirstViewColumnAsFirstContentWindowColumn(moduleEntity);
                }

                return viewToAdd;
            }

            return null;
        }

        internal void SetViewDescriptionsToColumnNames(AB_GenerationModuleEntity moduleEntity)
        {
            foreach (var mev in moduleEntity.GenerationModuleExplorers.SelectMany(me => me.GenerationModuleExplorerViews))
            {
                mev.ViewDescription = "";
                foreach (var gvc in mev.GenerationViewColumns)
                {
                    gvc.ColumnDescription = ToTitleCase(gvc.ColumnDescription);
                    mev.ViewDescription = mev.ViewDescription == ""
                        ? gvc.ColumnDescription
                        : mev.ViewDescription + ", " + gvc.ColumnDescription;
                }
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
                    moduleEntity.AllColumns.OrderBy(x => x.ContentWindowDiplaySequence).ToList());
            var itemsToMove = new ObservableCollection<AB_GenerationViewColumnEntity>();

            foreach (var vce in contentWindowItems)
            {
                // Move Join Fields To Top
                if (vce.IsJoinedField)
                {
                    itemsToMove.Add(vce);
                }
            }

            if (itemsToMove.Count > 0)
            {
                CollectionHelperMethods.MoveItemsTop(itemsToMove, contentWindowItems);

                for (int i = 0; i < moduleEntity.AllColumns.Where(k => k.IsKey).Count(); i++)
                {
                    CollectionHelperMethods.MoveItemsDown(itemsToMove, contentWindowItems);
                }

                // Resequence by 5
                int contentWindowDispSeq = 5;
                foreach (AB_GenerationViewColumnEntity vce in contentWindowItems)
                {
                    vce.ContentWindowDiplaySequence = contentWindowDispSeq;
                    contentWindowDispSeq += 5;
                }
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

        internal void AddJoinFields(AB_GenerationModuleEntity moduleEntity, string joinedFileName, Dictionary<string, string> joinFields)
        {
            foreach (var genRel in moduleEntity.GenerationRelationships)
            {
                if (moduleEntity == genRel.ParentModule && genRel.ChildTableName == joinedFileName)
                {
                    var joinFieldsList = new List<AB_GenerationViewColumnEntity>();
                    foreach (var field in joinFields)
                    {
                        if (moduleEntity.AllColumns.Where(x => x.IsJoinedField && x.JoinedFileName == joinedFileName && x.Name == field.Key).Count() == 0)
                        {
                            var joinField = genRel.EligibleChildFields.Where(x => x.Name == field.Key).FirstOrDefault();
                            if (joinField != null)
                            {
                                var joinFieldCopy = joinField.am_CreateDeepCopy<AB_GenerationViewColumnEntity>();
                                joinFieldCopy.JoinEntityPropertyName = AB_GenerationWizardShared.am_RemoveIllegalCharacters(field.Value);
                                joinFieldCopy.JoinFieldDescription = field.Value;

                                joinFieldsList.Add(joinFieldCopy);
                            }
                        }
                    }

                    if (joinFieldsList.Count() > 0)
                    {
                        _WizardShared.am_AddJoinedColumnsToModule(moduleEntity, joinedFileName, joinFieldsList, genRel);
                    }
                }

                if (moduleEntity == genRel.ChildModule && genRel.ParentTableName == joinedFileName)
                {
                    var joinFieldsList = new List<AB_GenerationViewColumnEntity>();
                    foreach (var field in joinFields)
                    {
                        if (moduleEntity.AllColumns.Where(x => x.IsJoinedField && x.JoinedFileName == joinedFileName && x.Name == field.Key).Count() == 0)
                        {
                            var joinField = genRel.EligibleParentFields.Where(x => x.Name == field.Key).FirstOrDefault();

                            if (joinField != null)
                            {
                                var joinFieldCopy = joinField.am_CreateDeepCopy<AB_GenerationViewColumnEntity>();
                                joinFieldCopy.JoinEntityPropertyName = AB_GenerationWizardShared.am_RemoveIllegalCharacters(field.Value);
                                joinFieldCopy.JoinFieldDescription = field.Value;

                                joinFieldsList.Add(joinFieldCopy);
                            }
                        }
                    }

                    if (joinFieldsList.Count() > 0)
                    {
                        _WizardShared.am_AddJoinedColumnsToModule(moduleEntity, joinedFileName, joinFieldsList, genRel);
                    }
                }
            }
        }

        internal AB_GenerationViewColumnEntity AddConcatenationVirtualField(AB_GenerationModuleEntity moduleEntity, string columnDescription, List<AB_GenerationViewColumnEntity> fields, string virtualConcatenationSeperator)
        {
            var virtualField = new AB_GenerationViewColumnEntity()
            {
                EntityPropertyName = AB_GenerationWizardShared.am_RemoveIllegalCharacters(columnDescription),
                ColumnDescription = columnDescription,
                IsVirtual = true,
                VirtualFieldType = AB_VirtualFieldTypes.Concatenation,
                PropertyType = AB_PropertyTypes.String,
                FieldVisualization = AB_FieldVisualizations.AB_FieldWithLabel,
                DatabasePropertyType = AB_PropertyTypes.String,
                Type = AB_PropertyTypes.String.ToString().ToUpper(),
                ViewField = AB_GenerationWizardShared.am_RemoveIllegalCharacters(columnDescription).ToUpper(),
                VirtualCalculationFields = new ObservableCollection<AB_CalculationVirtualEntity>(),
                VirtualConcatenationSeperator = virtualConcatenationSeperator,
            };

            foreach (var field in fields)
            {
                virtualField.VirtualCalculationFields.Add(new AB_CalculationVirtualEntity() { VirtualProperty = field, VirtualPropertyType = AB_VirtualPropertyType.Field });
            }

            _WizardShared.am_AddVirtualColumnToModule(moduleEntity, virtualField);

            return moduleEntity.AllColumns.Where(c => (c.EntityPropertyName == virtualField.EntityPropertyName)).FirstOrDefault();
        }

        #endregion Helper Methods
    }
}