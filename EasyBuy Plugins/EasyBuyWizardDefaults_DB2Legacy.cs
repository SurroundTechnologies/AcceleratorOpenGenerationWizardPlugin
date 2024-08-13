using A4DN.CF.WizardShared;
using A4DN.Core.BOS.Base;
using A4DN.Core.BOS.FrameworkEntity;
using GenerationWizardPlugin.EBHelpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;

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
            // Relationships are pulled from the database access routes. If no relationships are defined on the database, you can define the relationships in the GetDatabaseRelationships method.
            generationWizardShared.ap_DatabaseRelationships = EasyBuyHelpers.GetDatabaseRelationships(DBBrand.IBMiDB2, "EASYBUYDEM", "EASYBUYDEV");

            EasyBuyHelpers.InitializeExcelConstants(DBType.Legacy);
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

            // Set Generation defaults for each Column
            foreach (var viewColumnEntity in moduleEntity.AllColumns)
                SetColumnRules(Mode.InitialSetup, moduleEntity, viewColumnEntity);
            
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
        { }

        #endregion

        #region Rules

        internal void SetModuleRulesBeforeColumnRules(Mode mode, AB_GenerationModuleEntity moduleEntity)
        {
            switch (mode)
            {
                case Mode.InitialSetup:
                    // Set Module Name. This will also set the image name
                    moduleEntity.ModuleName = A4DNPluginHelpers.FormatModuleName(moduleEntity.FileDescription);

                    // Set the Module Description to Module Name
                    moduleEntity.ModuleDescription = moduleEntity.ModuleName;

                    // Module has Auto Generated Keys
                    moduleEntity.FileHasAutoGeneratedKey = true;
                    break;

                case Mode.ColumnsChanged:
                    break;
            }
        }

        internal void SetColumnRules(Mode mode, AB_GenerationModuleEntity moduleEntity, AB_GenerationViewColumnEntity viewColumnEntity)
        {
			var currentTable = (EasyBuyTable)Enum.Parse(typeof(EasyBuyTable), moduleEntity.TableDescription.Replace(" ", ""));

			switch (mode)
            {
                case Mode.InitialSetup:

					// Set Exclude From Content Fields
					var excludeFromContentFields = EasyBuyHelpers.GetDefinedFieldsForTable(currentTable, A4DNFieldType.ExcludeFromContent);
					viewColumnEntity.IsContentWindowField = !viewColumnEntity.IsFieldMatch(excludeFromContentFields);

					// Set Exclude From Search Fields
					var excludeFromSearchFields = EasyBuyHelpers.GetDefinedFieldsForTable(currentTable, A4DNFieldType.ExcludeFromSearch);
					viewColumnEntity.IsExplorerBarField = !viewColumnEntity.IsFieldMatch(excludeFromSearchFields);

					// Set Exclude From Detail Fields
					var excludeFromDetailFields = EasyBuyHelpers.GetDefinedFieldsForTable(currentTable, A4DNFieldType.ExcludeFromDetail);
					viewColumnEntity.IsDetailField = !viewColumnEntity.IsFieldMatch(excludeFromDetailFields);

					// Set Hidden Fields
					var hiddenFields = EasyBuyHelpers.GetDefinedFieldsForTable(currentTable, A4DNFieldType.Hidden);
					viewColumnEntity.Visible = !viewColumnEntity.IsFieldMatch(hiddenFields);

					// Set Identity Fields
					var identityFields = EasyBuyHelpers.GetDefinedFieldsForTable(currentTable, A4DNFieldType.Identity);
                    if (viewColumnEntity.IsFieldMatch(identityFields) && viewColumnEntity.IsKey)
                    {
						viewColumnEntity.IsIdentity = false;
						viewColumnEntity.IsRequiredField = false;
						viewColumnEntity.IsAutoIncrementedInCode = true;
					}

					// Set Audit Fields
					var auditFields = EasyBuyHelpers.GetDefinedFieldsForTable(currentTable, A4DNFieldType.Audit);
					if (viewColumnEntity.IsFieldMatch(auditFields))
					{
						viewColumnEntity.IsDetailField = false;
						viewColumnEntity.IsAuditStampField = true;
						viewColumnEntity.AuditStampFieldType = EasyBuyHelpers.AuditStampType(viewColumnEntity.Name, DBType.Legacy);
					}

					// Set Required Fields
					var requiredFields = EasyBuyHelpers.GetDefinedFieldsForTable(currentTable, A4DNFieldType.Required);
					viewColumnEntity.IsRequiredField = viewColumnEntity.IsFieldMatch(requiredFields);

					// Set Title Fields
					var titleFields = EasyBuyHelpers.GetDefinedFieldsForTable(currentTable, A4DNFieldType.Title);
					viewColumnEntity.IsTitleField = viewColumnEntity.IsFieldMatch(titleFields);
					if (!viewColumnEntity.IsTitleField) //If not a title field, then hide on small devices
						viewColumnEntity.WebMarkupTHDataAttributes = "data-hide=\"phone,tablet\"";

					// Set Currency Fields
					var currencyFields = EasyBuyHelpers.GetDefinedFieldsForTable(currentTable, A4DNFieldType.Currency);
					if (viewColumnEntity.IsFieldMatch(currencyFields))
						viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_Currency;

					// Set Percent Fields
					var percentFields = EasyBuyHelpers.GetDefinedFieldsForTable(currentTable, A4DNFieldType.Percent);
					if (viewColumnEntity.IsFieldMatch(percentFields))
						viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_Percent;

					// Set Extended Search Fields
					var extendedSearchFields = EasyBuyHelpers.GetDefinedFieldsForTable(currentTable, A4DNFieldType.ExtendedSearch);
                    if (viewColumnEntity.IsFieldMatch(extendedSearchFields) || viewColumnEntity.IsAuditStampField || (viewColumnEntity.IsKey && viewColumnEntity.IsIdentity))
                    {
						viewColumnEntity.IsExplorerBarField = true;
						viewColumnEntity.IsExtendedSearchField = true;
						viewColumnEntity.IsContentWindowField = true;
						viewColumnEntity.ShowInExtendedView = true;
					}
					else
					{
						viewColumnEntity.IsExtendedSearchField = false;
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
					var dateFields = EasyBuyHelpers.GetDefinedFieldsForTable(currentTable, A4DNFieldType.Date);
					if (viewColumnEntity.IsFieldMatch(dateFields) && (viewColumnEntity.Type == "NUMERIC(8.0)" || viewColumnEntity.Type == "DATE(4)"))
                    {
                        viewColumnEntity.AdditionalDataMapParameters = "databaseFieldType: AB_EntityFieldType.Date";
                        viewColumnEntity.PropertyType = AB_PropertyTypes.DateTime;
						viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_DatePicker;

                        if ((viewColumnEntity.Type == "NUMERIC(8.0)"))
                        {
                            viewColumnEntity.AdditionalDataMapParameters = "databaseFieldType: AB_EntityFieldType.Decimal";
                        }
                    }

					// Set Numeric(6) Time fields as Property Type of TimeSpan and Field Visualization of Time
					var timeFields = EasyBuyHelpers.GetDefinedFieldsForTable(currentTable, A4DNFieldType.Time);
					if (viewColumnEntity.IsFieldMatch(timeFields) && (viewColumnEntity.Type == "NUMERIC(6.0)"))
                    {
                        viewColumnEntity.PropertyType = AB_PropertyTypes.TimeSpan;
                        viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_TimePicker;
                        viewColumnEntity.AdditionalDataMapParameters = "databaseFieldType: AB_EntityFieldType.Decimal";
                    }

					// If the field has not already been set to a Title Field, set any Date or Time fields that are not Audit Stamps to be added to the title
					if (!viewColumnEntity.IsTitleField)
						viewColumnEntity.IsTitleField = (viewColumnEntity.IsFieldMatch(dateFields) || viewColumnEntity.IsFieldMatch(timeFields)) && !viewColumnEntity.IsFieldMatch(auditFields);

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
                    WizardShared.AddViewByViewField(moduleEntity, "Name");

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

            A4DNPluginHelpers.MoveFieldsInContentWindow(moduleEntity);

            // Uncheck internal Key for Title if other non-key fields are checked and the key is an Identity or AutoGenerated Field
            if (moduleEntity.AllColumns.Count(p => p.IsTitleField && !p.IsKey) > 0)
            {
                moduleEntity.AllColumns.Where(t => t.IsKey && (t.IsIdentity || t.IsAutoIncrementedInCode)).ForEach(u => u.IsTitleField = false);
            }
        }

		#endregion

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
    }
}