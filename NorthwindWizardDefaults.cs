using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using A4DN.Core.BOS.FrameworkEntity;
using A4DN.CF.WizardShared;
using System.Collections.ObjectModel;
using A4DN.Core.BOS.Base;
using System.Windows.Forms;

namespace GenerationWizardPlugin
{
    public class NorthwindWizardDefaults : AB_IGenerationWizardDefault
    {

        internal enum Mode { InitialSetup, ColumnsChanged };

        // Generation Wizard Shared Data
        internal readonly AB_GenerationWizardShared WizardShared = new AB_GenerationWizardShared();

        /// <summary>
        /// Accelerator Method - initialize.
        /// </summary>
        /// <param name="generationWizardShared">The generation wizard shared.</param>
        public virtual void am_Initialize(AB_GenerationWizardShared generationWizardShared)
        {

        }

        /// <summary>
        /// Accelerator Method - prompt for keys if none specified.
        /// </summary>
        /// <param name="ModuleEntity">The module entity.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public virtual bool am_PromptForKeysIfNoneSpecified(AB_GenerationModuleEntity ModuleEntity)
        {
            return true;
        }

        /// <summary>
        /// The method is called Before the module is added to the module manager. You can return false if you don't want the module to be added.
        /// </summary>
        /// <param name="ModuleEntity">The module entity.</param>
        /// <returns><c>true</c> if you want to add the module to the module manager, <c>false</c> otherwise.</returns>
        public virtual bool am_AllowAddModule(AB_GenerationModuleEntity ModuleEntity)
        {
            if (ModuleEntity.FileName == "sysdiagrams")
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
                    moduleEntity.ModuleName = FormatModuleName(moduleEntity.FileName);

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

                    var KeyCount = moduleEntity.AllColumns.Where(itm => itm.IsKey).Count();
                    if (KeyCount == 1)
                    {
                        // Set Identity Field
                        if (IsIdentityField(viewColumnEntity))
                        {

                            if (viewColumnEntity.Type == "int(10)" && viewColumnEntity.Name != "RegionID")
                            {
                                viewColumnEntity.IsIdentity = true;
                                moduleEntity.FileHasAutoGeneratedKey = true;
                                viewColumnEntity.IsRequiredField = false;
                            }
                        }
                    }

                    // Set Field Visualization of Date to only display date and not the time.
                    if (viewColumnEntity.Name.EndsWith("Date"))
                    {       
                        viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_DatePicker;
                    }

                    if (viewColumnEntity.Type.StartsWith("ntext"))
                    {
                        viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_MultilineFieldWithLabel;
                    }

                    if (viewColumnEntity.Type.StartsWith("money"))
                    {
                        viewColumnEntity.StringFormat = "c";
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

                    //if (IsExtendedSearchField(viewColumnEntity))
                    //{
                    //    viewColumnEntity.IsExplorerBarField = true;
                    //    viewColumnEntity.IsExtendedSearchField = true;
                    //}
                    //else
                    //{
                        viewColumnEntity.IsExplorerBarField = true;
                        viewColumnEntity.IsExtendedSearchField = false;
                    //}

                    //if (IsExtendedContentWindowField(viewColumnEntity))
                    //{
                        //viewColumnEntity.IsContentWindowField = true;
                        //viewColumnEntity.ShowInExtendedView = true;
                    //}
                    //else
                    //{
                        viewColumnEntity.ShowInExtendedView = false;
                    //}

                    if (viewColumnEntity.IsKey)
                    {
                        viewColumnEntity.IsContentWindowField = true;
                        viewColumnEntity.ShowInExtendedView = false;
                        viewColumnEntity.Visible = true;
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
                                viewColumnEntity.ContentWindowDisplaySequence =
                                    vce.ContentWindowDisplaySequence += 1;
                            }

                        }
                    }

                    if (viewColumnEntity.IsKey)
                    {
                     
                        viewColumnEntity.Visible = true;
                    }

                    break;
            }

            viewColumnEntity.IsTitleField = IsDetailTitleField(viewColumnEntity);

            if (!viewColumnEntity.IsTitleField)
            {
                //If not a title field, then hide on small devices
                viewColumnEntity.WebMarkupTHDataAttributes = "data-hide=\"phone,tablet\"";

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

                    var moduleName = "";
                    if (moduleEntity.ModuleName.EndsWith("ies"))
                    {
                        moduleName = moduleEntity.ModuleName.Replace("ies", "y");
                    }
                    else if (moduleEntity.ModuleName.EndsWith("s"))
                    {
                        moduleName = moduleEntity.ModuleName.Remove(moduleEntity.ModuleName.Length - 1, 1);
                    }


                    if (moduleName == "Order")
                    {
                        break;
                    }

                    // Add View By Name if there is a name field and Set to Default
                    if (AddViewByViewField(moduleEntity, "Name"))
                    {
                        // Set the Drop Down Label to Module Name + Field Name/Desc
                        foreach (var dd in moduleEntity.DropDowns)
                        {
                            foreach (var ddViewFields in dd.DropDownViewFields.Where(ddViewFields => ddViewFields.ColumnDescription.ToUpper().EndsWith("NAME")))
                            {
                                if (!ddViewFields.ColumnDescription.Contains(moduleName))
                                {
                                    dd.DropDownLabel = moduleName + " " + ddViewFields.ColumnDescription;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (AddViewByViewField(moduleEntity, "Description"))
                        {
                            // Set the Drop Down Label to Module Name + Field Name/Desc
                            foreach (var dd in moduleEntity.DropDowns)
                            {
                                foreach (var ddViewFields in dd.DropDownViewFields.Where(ddViewFields => ddViewFields.ColumnDescription.ToUpper().EndsWith("DESCRIPTION")))
                                {
                                    if (!ddViewFields.ColumnDescription.Contains(moduleName))
                                    {
                                        dd.DropDownLabel = moduleName + " " + ddViewFields.ColumnDescription;
                                        break;
                                    }
                                }
                            }
                        }
                    }


                    break;

                case Mode.ColumnsChanged:

                    break;

            }

            MoveFieldsInContentWindow(moduleEntity);

            // Uncheck internal Key for Title if other non-key fields are checked and the key is an Identity or AutoGenerated Field
            //if (moduleEntity.AllColumns.Count(p => p.IsTitleField && !p.IsKey) > 0)
            //{
            //    moduleEntity.AllColumns.Where(t => t.IsKey && (t.IsIdentity || t.IsAutoIncrementedInCode)).ForEach(u => u.IsTitleField = false);
            //}

        }

        #endregion // Rules

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

        #endregion //Identity Fields

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
                {"CreateDate", AB_AuditStampTypes.CreateDate},
                {"CreateTime", AB_AuditStampTypes.CreateTime},
                {"CreateUser", AB_AuditStampTypes.CreateUser},
                {"LastChangeDate", AB_AuditStampTypes.LastChangeDate},
                {"LastChangeTime", AB_AuditStampTypes.LastChangeTime},
                {"LastChangeUser", AB_AuditStampTypes.LastChangeUser},
                {"rowguid", AB_AuditStampTypes.Undefined},
                {"ModifiedDate", AB_AuditStampTypes.LastChangeDate},
                // you can also use the undefined Audit Stamp
                //{"AuditStampName", AB_AuditStampTypes.Undefined},
            };

        /// <summary>
        /// Logic to determine if the field should go on the Audit Stamp Tab
        /// </summary>
        internal virtual bool IsAuditTabField(AB_GenerationViewColumnEntity vce)
        {
            return AuditStamps.Any(x => vce.Name.ToUpper().Contains(x.Key.ToUpper())) || IsDeleteFlagField(vce);
        }

        /// <summary>
        /// Logic to determine the Audit Stamp Type
        /// </summary>
        internal virtual AB_AuditStampTypes AuditStampType(string name)
        {
            var isAudit = _auditStamps.Any(x => name.ToUpper().Contains(x.Key.ToUpper()));
            return isAudit ? _auditStamps.Where(aud => name.ToUpper().Contains(aud.Key.ToUpper())).Select(aud => aud.Value).FirstOrDefault() : AB_AuditStampTypes.Undefined;
        }

        #endregion //Audit Stamp Fields

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

        #endregion //Content Window Extended Fields

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

        #endregion //Search Explorer Bar Extended Fields

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
               "OrderDate",
               "[dbo].[Customers].CompanyName",
               "CustomerCompanyName",
               "[dbo].[Products].ProductName",
               "ProductName",
               "CompanyName",
              "CustomerDescription",
              "TerritoryDescription",
              "RegionDescription",
              "LastName",
              "FirstName",
              "CategoryName"

            };


        /// <summary>
        /// Logic to determine if the field is a Title Field
        /// </summary>
        internal virtual bool IsDetailTitleField(AB_GenerationViewColumnEntity vce)
        {
            return DetailTitleField.Any(x => vce.ViewField.ToUpper().Equals(x.ToUpper())) || (vce.IsKey);
        }

        #endregion //Detail Title Fields

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

        #endregion //Soft Delete Fields

        #endregion //Default Standards

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
        /// <param name="viewField">The view field.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal bool AddViewByViewField(AB_GenerationModuleEntity moduleEntity, string viewField)
        {
            if (moduleEntity.AllColumns.Any(x => x.ViewField.ToUpper().EndsWith(viewField.ToUpper())))
            {
                var viewColumns = new ObservableCollection<AB_GenerationViewColumnEntity>();
                var nameColumn = moduleEntity.AllColumns.FirstOrDefault(x => x.ViewField.ToUpper().EndsWith(viewField.ToUpper()));
                if (nameColumn != null)
                {
                    viewColumns.Add(nameColumn);
                }
                var keyColumns = moduleEntity.AllColumns.Where(x => x.IsKey);
                foreach (var vc in keyColumns.Where(vc => !viewColumns.Contains(vc)))
                {
                    viewColumns.Add(vc);
                }

                var viewToAdd = WizardShared.am_CreateDefaultView(nameColumn.Description, nameColumn.Description, viewColumns);
                if (viewToAdd == null)
                {
                    MessageBox.Show("Error adding View: By " + viewField + " to module: " + moduleEntity.ModuleName);
                    return false;
                }
                WizardShared.am_AddViewToModule(viewToAdd, moduleEntity);

                WizardShared.am_SetDefaultView(viewToAdd, moduleEntity);

                WizardShared.am_SetFirstViewColumnAsFirstContentWindowColumn(moduleEntity);

                return true;
            }

            return false;
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
                //// Move Join Fields To Top
                //else if (vce.IsJoinedField)
                //{
                //    itemsToMoveTop.Add(vce);
                //}
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

        #endregion
    }
}
