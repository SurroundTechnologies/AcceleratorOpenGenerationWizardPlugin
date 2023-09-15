using System;
using A4DN.CF.SchemaEntities;
using A4DN.CF.WizardShared;
using A4DN.Core.BOS.Base;
using A4DN.Core.BOS.FrameworkEntity;
using A4DN.Core.BOS.NPOI.Excel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GenerationWizardPlugin
{
    public class GoldCrossWizardDefaults : AB_IGenerationWizardDefault
    {
        internal enum Mode { InitialSetup, ColumnsChanged };

        // Generation Wizard Shared Data
        internal AB_GenerationWizardShared _WizardShared;

        private Dictionary<string, string> _FileKeys = new Dictionary<string, string>();
        private List<string> _UnusedFiled = new List<string>();

        #region Generic Code

        /// <summary>
        /// Accelerator Method - initialize.
        /// </summary>
        /// <param name="generationWizardShared">The generation wizard shared.</param>
        public virtual void am_Initialize(AB_GenerationWizardShared generationWizardShared)
        {
            try
            {
                // Load file Keys
                var npoiExcelWorkbook = new AB_NPOIExcel().am_OpenWorkbook("C:\\SurroundClientSystems\\GoldCross\\Generation Resources\\WizardPlugin\\DB Keys.xlsx");
                var npoiExcelSheet = npoiExcelWorkbook?.GetSheetAt(0);
                if (npoiExcelSheet != null)
                {
                    for (int row = 1; row <= npoiExcelSheet.LastRowNum; row++)
                    {
                        if (npoiExcelSheet.GetRow(row) != null) //null is when the row only contains empty cells
                        {
                            var file = npoiExcelSheet.GetRow(row).GetCell(0)?.StringCellValue;
                            var keys = npoiExcelSheet.GetRow(row).GetCell(1)?.StringCellValue;

                            if (!_FileKeys.ContainsKey(file))
                            {
                                _FileKeys.Add(file, keys);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }


            try
            {
                // Load Unused Files
                var npoiExcelWorkbook2 = new AB_NPOIExcel().am_OpenWorkbook("C:\\SurroundClientSystems\\GoldCross\\Generation Resources\\WizardPlugin\\UnusedFiles.xlsx");
                var npoiExcelSheet2 = npoiExcelWorkbook2?.GetSheetAt(0);
                if (npoiExcelSheet2 != null)
                {
                    for (int row = 1; row <= npoiExcelSheet2.LastRowNum; row++)
                    {
                        if (npoiExcelSheet2.GetRow(row) != null) //null is when the row only contains empty cells
                        {
                            var file = npoiExcelSheet2.GetRow(row).GetCell(1)?.StringCellValue;

                            if (!string.IsNullOrWhiteSpace(file) && !_UnusedFiled.Contains(file))
                            {
                                _UnusedFiled.Add(file);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
         

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
            if (moduleEntity?.AllColumns.Count(s => s.IsKey == true) > 0)
            {
                // Has Keys
                return false;
            }

            // No Keys
          
            // If only one column, then make it the key
            if (moduleEntity?.AllColumns.Count == 1)
            {
                moduleEntity.AllColumns.ForEach(s => s.IsKey = true);

                return false;
            }

            // no keys Columns  - look up in excel document
            if (_FileKeys != null && _FileKeys.TryGetValue(moduleEntity.FileName, out string keys))
            {
                var keyList = keys.Split(',');
                if (keyList.Any())
                {
                    foreach (var key in keyList)
                    {
                        var col = moduleEntity.AllColumns.FirstOrDefault(c => c.Name?.ToUpper() == key?.Trim().ToUpper());
                        if (col != null)
                        {
                            col.IsKey = true;
                            col.IsExplorerBarField = true;
                            col.IsSearchColumnEntryEnabled = false;
                        }
                    }

                    return false;
                }
            }

                
            #region Example Code

            //foreach (var modView in moduleEntity.GenerationModuleExplorers.FirstOrDefault().GenerationModuleExplorerViews)
            //{
            //    if (modView.ViewName == moduleEntity.FileName)
            //    {
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

            #endregion Example Code

            return true;
        }

        /// <summary>
        /// The method is called Before the module is added to the module manager. You can return false if you don't want the module to be added.
        /// </summary>
        /// <param name="moduleEntity">The module entity.</param>
        /// <returns><c>true</c> if you want to add the module to the module manager, <c>false</c> otherwise.</returns>
        public virtual bool am_AllowAddModule(AB_GenerationModuleEntity moduleEntity)
        {
            return _UnusedFiled != null && !_UnusedFiled.Contains(moduleEntity.FileName);
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

            // Remove old Logical Files from the Views. We only want to use the SQL Indexes.
            foreach (var view in moduleEntity.GenerationModuleExplorers[0].GenerationModuleExplorerViews.OrderBy(x => x.ViewName).Where(w => w.Type.ToUpper() == "LOGICAL"))
            {
                _WizardShared.am_RemoveViewFromModule(view, moduleEntity);
            }
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

        #region Rules

        /// <summary>
        /// Set Module level Rules before the Column Rules are Processed
        /// </summary>
        internal virtual void SetModuleRulesBeforeColumnRules(Mode mode, AB_GenerationModuleEntity moduleEntity)
        {
            switch (mode)
            {
                case Mode.InitialSetup:

                    // Set Module Name.
                    moduleEntity.ModuleName = moduleEntity.ModuleName.Replace(" - ", " ");

                    // These 2 lines were commented out because the Gold Cross Modernized DB has been defined with all the correct case. This cleanup is not necessary 
                    //if (moduleEntity.ModuleName.Contains("  "))
                    //{
                    //    moduleEntity.ModuleName = moduleEntity.ModuleName.Substring(0, moduleEntity.ModuleName.IndexOf("  "));
                    //}
                    // moduleEntity.ModuleName = ToTitleCase(FormatModuleName(moduleEntity.ModuleName));
                    // moduleEntity.ModuleName = moduleEntity.ModuleName.Replace("File", "").Replace("Table", "").Replace("Master", "").Replace("Nemsis", "NEMSIS").Replace("Cpt", "CPT").Replace("Ytd", "YTD").Replace("\"", "").Replace("  ", " ").Replace("+", "").Replace("#", "Nbr").Replace("-", "").TrimEnd('.').TrimEnd('?').Trim();

                    //RVI Document Management
                    switch (moduleEntity.FileName.ToString())
                    {
                        case "RVABREP":
                            moduleEntity.ModuleName = "RVI Documents";
                            break;
                        case "RVAAREP":
                            moduleEntity.ModuleName = "RVI Document System Descriptions";
                            break;
                    }


                    //Correct Module Name
                    switch (moduleEntity.ModuleName.ToString())
                    {
                        case "YTD Payments By Detail":
                            moduleEntity.ModuleName = "YTD Payments By Entry Code Detail";
                            break;
                        case "Holiday Table":
                            moduleEntity.ModuleName = "Holidays";
                            break;
                        case "Cross Corporation Patient":
                            moduleEntity.ModuleName = "Corporation Patients";
                            break;
                    }

                    //Set Module Name Plural
                    switch (moduleEntity.ModuleName.ToString())
                    {
                        case "Account":
                        case "Patient":
                        case "CAD Interface":
                        case "Trip":
                        case "Trip Detail":
                        case "Month-End Detail":
                        case "Miscellaneous Insurance Group":
                        case "Miscellaneous Insurance EMS Provider ID":
                        case "Paid Claims Claim":
                        case "Paid Claims Line Detail":
                        case "Description Override":
                        case "Payment Batch Detail":
                        case "Billed Charges Run Number":
                        case "Billed Charges Detail":
                        case "Billed Charges Exception":
                        case "Collection Letters Detail":
                        case "Patient Portal Trip Balance":
                        case "EMS Provider":
                        case "Ambulance":
                        case "Crew Member":
                        case "Clinic":
                        case "Department":
                        case "Entry Code":
                        case "Entry Code Price":
                        case "Entry Code Contract Price":
                        case "Entry Code Expected Reimbursement":
                        case "Holiday":
                            moduleEntity.ModuleName = moduleEntity.ModuleName + "s";
                            break;
                        case "Dispatch":
                        case "Miscellaneous Insurance Address":
                        case "Payment Batch":
                            moduleEntity.ModuleName = moduleEntity.ModuleName + "es";
                            break;
                    }

                    // Set the Module Description to Module Name
                    moduleEntity.ModuleDescription = moduleEntity.ModuleName;

                    //Set the Module Image
                    switch (moduleEntity.ModuleName.ToString())
                    {
                        case "Account":
                        case "Accounts":
                        case "Accounts GC":
                            moduleEntity.Image = "Briefcase";
                            break;
                        case "Account Short Comments":
                            moduleEntity.Image = "Remark";
                            break;
                        case "Account Comments":
                            moduleEntity.Image = "Remark";
                            break;
                        case "Dispatch":
                            moduleEntity.Image = "Ambulance";
                            break;
                        case "Trip Insurance":
                            moduleEntity.Image = "Insurance";
                            break;
                        case "Trip Month-End Status":
                            moduleEntity.Image = "Event";
                            break;
                        case "Trip Narrative":
                            moduleEntity.Image = "CustomerComments";
                            break;
                        case "Miscellaneous Insurance Address":
                            moduleEntity.Image = "AddressLabel";
                            break;
                        case "Paid Claims Check Totals":
                            moduleEntity.Image = "BlankCheck";
                            break;
                        case "Miscellaneous Insurance Address Comments":
                            moduleEntity.Image = "Remark";
                            break;
                        case "System Configuration":
                            moduleEntity.Image = "ModuleSystemOptions";
                            break;
                        case "Data Dictionary":
                            moduleEntity.Image = "SystemTables";
                            break;
                        case "EMS Provider":
                            moduleEntity.Image = "Doctor";
                            break;
                        case "Clinic":
                            moduleEntity.Image = "Hospital";
                            break;
                        case "Department":
                            moduleEntity.Image = "Organization";
                            break;
                        case "Next Number Configurations":
                            moduleEntity.Image = "NextIDNumber";
                            break;
                        case "Holiday":
                            moduleEntity.Image = "Holiday";
                            break;
                        case "Patient Portal Trip Balances":
                            moduleEntity.Image = "PortalUser";
                            break;
                        default:
                            // Set to the Module name without spaces.
                            moduleEntity.Image = moduleEntity.ModuleName.Replace(" ", "");
                            break;
                    }
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
                    // viewColumnEntity.ColumnDescription = ToTitleCase(viewColumnEntity.ColumnDescription);
                    // Fix Common Title Case Mistakes
                    // This line were commented out because the Gold Cross DB has been defined with all the correct case. This cleanup is not necessary 
                    // viewColumnEntity.ColumnDescription = viewColumnEntity.ColumnDescription.Replace("Id", "ID").Replace("Pdf", "PDF").Replace("P.O.", "PO").Replace("Po Number", "PO Number").Replace("Nemsis", "NEMSIS").Replace("Pcr", "PCR").Replace("Cpt", "CPT").Replace("Ytd", "YTD");

                    // Property Name Remove Illegal Characters
                    viewColumnEntity.EntityPropertyName = AB_GenerationWizardShared.am_RemoveIllegalCharacters(viewColumnEntity.ColumnDescription);

                    // RVI Document Management
                    switch (viewColumnEntity.EntityPropertyName.ToUpper())
                    {
                        // RVABREP
                        case "ABAACD":
                            viewColumnEntity.EntityPropertyName = "SystemCode";
                            viewColumnEntity.ColumnDescription = "System Code";
                            break;
                        case "ABAANB":
                            viewColumnEntity.EntityPropertyName = "SystemTransactionNumber";
                            viewColumnEntity.ColumnDescription = "System Transaction Number";
                            break;
                        case "ABABCD":
                            viewColumnEntity.EntityPropertyName = "SystemIndex1";
                            viewColumnEntity.ColumnDescription = "System Index 1";
                            break;
                        case "ABACCD":
                            viewColumnEntity.EntityPropertyName = "SystemIndex2";
                            viewColumnEntity.ColumnDescription = "System Index 2";
                            break;
                        case "ABADCD":
                            viewColumnEntity.EntityPropertyName = "SystemIndex3";
                            viewColumnEntity.ColumnDescription = "System Index 3";
                            break;
                        case "ABAECD":
                            viewColumnEntity.EntityPropertyName = "SystemIndex4";
                            viewColumnEntity.ColumnDescription = "System Index 4";
                            break;
                        case "ABAFCD":
                            viewColumnEntity.EntityPropertyName = "SystemIndex5";
                            viewColumnEntity.ColumnDescription = "System Index 5";
                            break;
                        case "ABAGCD":
                            viewColumnEntity.EntityPropertyName = "SystemIndex6";
                            viewColumnEntity.ColumnDescription = "System Index 6";
                            break;
                        case "ABAHCD":
                            viewColumnEntity.EntityPropertyName = "SystemIndex7";
                            viewColumnEntity.ColumnDescription = "System Index 7";
                            break;
                        case "ABABST":
                            viewColumnEntity.EntityPropertyName = "ImageType";
                            viewColumnEntity.ColumnDescription = "Image Type";
                            break;
                        case "ABAICD":
                            viewColumnEntity.EntityPropertyName = "ImagePath";
                            viewColumnEntity.ColumnDescription = "Image Path";
                            break;
                        case "ABAJCD":
                            viewColumnEntity.EntityPropertyName = "ImageFileName";
                            viewColumnEntity.ColumnDescription = "Image File Name";
                            break;
                        case "ABAADT":
                            viewColumnEntity.EntityPropertyName = "ImageCreationDate";
                            viewColumnEntity.ColumnDescription = "Image Creation Date";
                            break;
                        case "ABABDT":
                            viewColumnEntity.EntityPropertyName = "ImageLastViewDate";
                            viewColumnEntity.ColumnDescription = "Image Last View Date";
                            break;
                        case "ABABUN":
                            viewColumnEntity.EntityPropertyName = "TotalPages";
                            viewColumnEntity.ColumnDescription = "Total Pages";
                            break;
                        case "ABACST":
                            viewColumnEntity.EntityPropertyName = "SMDeleteCode";
                            viewColumnEntity.ColumnDescription = "SM Delete Code";
                            break;
                        // RVAAREP
                        case "AAAACD":
                            viewColumnEntity.EntityPropertyName = "SystemCode";
                            viewColumnEntity.ColumnDescription = "System Code";
                            break;
                        case "AAAATX":
                            viewColumnEntity.EntityPropertyName = "IndexDescription1";
                            viewColumnEntity.ColumnDescription = "Index Description 1";
                            break;
                        case "AAABTX":
                            viewColumnEntity.EntityPropertyName = "SystemCodeDescription";
                            viewColumnEntity.ColumnDescription = "System Code Description";
                            break;
                        case "AAACTX":
                            viewColumnEntity.EntityPropertyName = "IndexDescription2";
                            viewColumnEntity.ColumnDescription = "Index Description 2";
                            break;
                        case "AAADTX":
                            viewColumnEntity.EntityPropertyName = "IndexDescription3";
                            viewColumnEntity.ColumnDescription = "Index Description 3";
                            break;
                        case "AAAETX":
                            viewColumnEntity.EntityPropertyName = "IndexDescription4";
                            viewColumnEntity.ColumnDescription = "Index Description 4";
                            break;
                        case "AAAFTX":
                            viewColumnEntity.EntityPropertyName = "IndexDescription5";
                            viewColumnEntity.ColumnDescription = "Index Description 5";
                            break;
                        case "AAAGTX":
                            viewColumnEntity.EntityPropertyName = "IndexDescription6";
                            viewColumnEntity.ColumnDescription = "Index Description 6";
                            break;
                        case "AAAHTX":
                            viewColumnEntity.EntityPropertyName = "IndexDescription7";
                            viewColumnEntity.ColumnDescription = "Index Description 7";
                            break;
                        case "AAATX":
                            viewColumnEntity.EntityPropertyName = "IndexDescription";
                            viewColumnEntity.ColumnDescription = "Index Description ";
                            break;
                        case "AAAAST":
                            viewColumnEntity.EntityPropertyName = "SMDeleteCode";
                            viewColumnEntity.ColumnDescription = "SM Delete Code";
                            break;
                    }

                    // Set Url Field Visualization
                    if (viewColumnEntity.Name == "URL")
                    {
                        viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_ImageUrl;
                    }

                    // Set Email Field Visualization
                    if (viewColumnEntity.Name.Contains("Email"))
                    {
                        viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_EmailAddress;
                    }

                    //// Apply Currency Format String to 9.2 Decimals with Amount in the name
                    if ((viewColumnEntity.Name.Contains("Amount") || viewColumnEntity.Name.Contains("Balance") || viewColumnEntity.Name.Contains("Price") || viewColumnEntity.Name.Contains("Charges") || viewColumnEntity.Name.Contains("Dollars Billed") || viewColumnEntity.Name.Contains("Payment") || viewColumnEntity.Name.Contains("Std Amt") || viewColumnEntity.Name.Contains("Adj Minus") || viewColumnEntity.Name.Contains("Adj Plus") || viewColumnEntity.Name.Contains("Baseline Delinquent") || viewColumnEntity.Name.Contains("Baseline Current")) && viewColumnEntity.PropertyType == AB_PropertyTypes.Decimal && (viewColumnEntity.FieldLength == 7 || viewColumnEntity.FieldLength == 9 || viewColumnEntity.FieldLength == 11) && viewColumnEntity.FieldDecimals == 2)
                    {
                        viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_Currency;
                        viewColumnEntity.StringFormat = "c";
                    }



                    //// Set Numeric(8) date fields as Property Type of DateTime and Field Visualization of Date
                    if ((viewColumnEntity.Name.Contains("Date")) && (viewColumnEntity.Type == "DECIMAL(8.0)" || viewColumnEntity.Type == "NUMERIC(8.0)" || viewColumnEntity.Type == "DATE(4)"))
                    {
                        viewColumnEntity.PropertyType = AB_PropertyTypes.DateTime;
                        viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_DatePicker;
                        if ((viewColumnEntity.Type == "NUMERIC(8.0)") || (viewColumnEntity.Type == "DECIMAL(8.0)"))
                        {
                            viewColumnEntity.AdditionalDataMapParameters = "databaseFieldType: AB_EntityFieldType.Decimal";
                        }
                    }
                    // Set Numeric(6) Time fields as Property Type of TimeSpan and Field Visualization of Time
                    if ((viewColumnEntity.Name.Contains("Time")) && (viewColumnEntity.Type == "NUMERIC(8.0)" || viewColumnEntity.Type == "NUMERIC(6.0)" || viewColumnEntity.Type == "DECIMAL(6.0)" || viewColumnEntity.Type == "DECIMAL(4.0)"))
                    {
                        viewColumnEntity.PropertyType = AB_PropertyTypes.TimeSpan;
                        viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_TimePicker;
                        viewColumnEntity.AdditionalDataMapParameters = "databaseFieldType: AB_EntityFieldType.Decimal";
                    }

                    #region Example Code

                    //// Set Numeric(8) date fields as Property Type of DateTime and Field Visualization of Date
                    //if (viewColumnEntity.ColumnDescription.EndsWith("Date") && viewColumnEntity.Type == "NUMERIC(8.0)")
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

                    // Convert 3 date fields to 1

                    var virtualFieldsToAdd = new List<Tuple<string, int, int, int, int>>();
                    foreach (var dateYear in moduleEntity.AllColumns.Where(c => c.EntityPropertyName.EndsWith("Year") && c.Type == "NUMERIC(4.0)"))
                    {
                        var fieldDesc = dateYear.EntityPropertyName.Replace("Year", "");

                        var dateMonth = moduleEntity.AllColumns.Where(c => c.EntityPropertyName.EndsWith($"{fieldDesc}Month") && c.Type == "NUMERIC(2.0)").FirstOrDefault();
                        var dateDay = moduleEntity.AllColumns.Where(c => c.EntityPropertyName.EndsWith($"{fieldDesc}Day") && c.Type == "NUMERIC(2.0)").FirstOrDefault();
                        if (dateYear != null && dateMonth != null && dateDay != null)
                        {
                            var dateFieldName = dateYear.EntityPropertyName.Replace("Year", "");
                            var datefieldDetailFieldDisplaySequence = dateYear.DetailFieldDisplaySequence;
                            var datefieldAuditStampDisplaySequence = dateYear.AuditStampDisplaySequence;
                            var datefieldTitleFieldDisplaySequence = dateYear.TitleFieldDiplaySequence;
                            var datefieldContentWindowDisplaySequence = dateYear.ContentWindowDisplaySequence;
                            virtualFieldsToAdd.Add(Tuple.Create(dateFieldName, datefieldDetailFieldDisplaySequence, datefieldAuditStampDisplaySequence, datefieldTitleFieldDisplaySequence, datefieldContentWindowDisplaySequence));
                        }
                    }
                    if (virtualFieldsToAdd.Any())
                    {
                        var listEntrySequence = 0;
                        foreach (var listEntry in virtualFieldsToAdd)
                        {
                            var dateFieldName = listEntry.Item1;
                            var datefieldDetailFieldDisplaySequence = listEntry.Item2 + listEntrySequence;
                            var datefieldAuditStampDisplaySequence = listEntry.Item3 + listEntrySequence;
                            var datefieldTitleFieldDisplaySequence = listEntry.Item4 + listEntrySequence;
                            var dateFieldContentWindowDisplaySequence = listEntry.Item5 + listEntrySequence;
                            var virtualField = AddUndefinedVirtualFieldForDate(moduleEntity, dateFieldName);

                            if (virtualField != null)
                            {
                                if (virtualField.EntityPropertyName.Contains("RecordEntryDate") || virtualField.EntityPropertyName.Contains("RecordChangeDate"))
                                {
                                    virtualField.IsDetailField = false;
                                    virtualField.IsAuditStampField = true;
                                }
                                else
                                {
                                    virtualField.IsDetailField = true;
                                }
                                //virtualField.DisplaySequence = dateFieldSequence;
                                virtualField.DetailFieldDisplaySequence = datefieldDetailFieldDisplaySequence;
                                virtualField.AuditStampDisplaySequence = datefieldAuditStampDisplaySequence;
                                virtualField.TitleFieldDiplaySequence = datefieldTitleFieldDisplaySequence;
                                virtualField.ContentWindowDisplaySequence = dateFieldContentWindowDisplaySequence;
                                listEntrySequence++;
                            }
                        }
                    }


                    #region Example Code

                    //// Add View Standards
                    //AddViewByViewFields(moduleEntity, moduleEntity.FileName + "AV1", new List<string> { "CFNAME", "CLNAME" });
                    //AddViewByViewFields(moduleEntity, moduleEntity.FileName + "AV2", new List<string> { "CLNAME", "CFNAME" });
                    //if (moduleEntity.FileName == "COMPANY")
                    //{
                    //    AddViewByViewFields(moduleEntity, ToTitleCase(moduleEntity.FileName) + "Name", new List<string> { "YNAME", "YCOMP" });
                    //}

                    // Need to set Drop Downs for Catalogs, Brands, Sections, Subsections, Catalog Print Sequence, Media, ... See views above

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
            {"RecordEntryDate", AB_AuditStampTypes.CreateDate},
            {"RecordEntryTime", AB_AuditStampTypes.CreateTime},
            {"RecordEntryUserID", AB_AuditStampTypes.CreateUser},
            {"RecordEntryProgram", AB_AuditStampTypes.Undefined},
            {"RecordChangeDate", AB_AuditStampTypes.LastChangeDate},
            {"RecordChangeTime", AB_AuditStampTypes.LastChangeTime},
            {"RecordChangeUserID", AB_AuditStampTypes.LastChangeUser},
            {"RecordChangeProgram", AB_AuditStampTypes.Undefined},
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
            //"ID"
        };

        internal virtual List<string> DetailTitleField2
        {
            get { return _detailTitleField2; }
            set { _detailTitleField2 = value; }
        }

        private List<string> _detailTitleField2 = new List<string>
        {
             "Name", "Description"
        };

        /// <summary>
        /// Logic to determine if the field is a Title Field
        /// </summary>
        internal virtual bool IsDetailTitleField(AB_GenerationViewColumnEntity vce)
        {
            return (DetailTitleField.Any(x => vce.ViewField.ToUpper().EndsWith(x.ToUpper()))) || (DetailTitleField2.Any(x => vce.ColumnDescription.ToUpper().Contains(x.ToUpper()))) || (vce.IsKey);
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

        #region Relationships

        private void _AddDatabaseRelationships(ObservableCollection<AB_SchemaRelationshipEntity> relationships)
        {
            if (relationships == null)
            {
                relationships = new ObservableCollection<AB_SchemaRelationshipEntity>();
            }

            try
            {
                //  0 System Child File
                //  1 System Parent File
                //  2 System Child Keys
                //  3 System Parent Keys
                //  4 Cardinality
                //  5 Child Schema
                //  6 Child File
                //  7 Parent Schema
                //  8 Parent File
                //  9 Child Keys
                // 10 Parent Keys
                // 11 Constraint Phase


                var npoiExcelWorkbook = new AB_NPOIExcel().am_OpenWorkbook("C:\\SurroundClientSystems\\GoldCross\\Generation Resources\\WizardPlugin\\DB Relationships.xlsx");
                var npoiExcelSheet = npoiExcelWorkbook?.GetSheetAt(0);
                if (npoiExcelSheet != null)
                {
                    for (int row = 2; row <= npoiExcelSheet.LastRowNum; row++)
                    {
                        if (npoiExcelSheet.GetRow(row) != null) //null is when the row only contains empty cells
                        {
                            // System Names
                            // var childFile = npoiExcelSheet.GetRow(row).GetCell(0).StringCellValue;
                            // var parentFile = npoiExcelSheet.GetRow(row).GetCell(1).StringCellValue;
                            // var childKeys = npoiExcelSheet.GetRow(row).GetCell(2).StringCellValue;
                            // var parentKeys = npoiExcelSheet.GetRow(row).GetCell(3).StringCellValue;

                            // SQL Names
                            var childSchema = npoiExcelSheet.GetRow(row).GetCell(5).StringCellValue;
                            var childFile = npoiExcelSheet.GetRow(row).GetCell(6).StringCellValue;
                            var parentSchema = npoiExcelSheet.GetRow(row).GetCell(7).StringCellValue;
                            var parentFile = npoiExcelSheet.GetRow(row).GetCell(8).StringCellValue;
                            var childKeys = npoiExcelSheet.GetRow(row).GetCell(9).StringCellValue;
                            var parentKeys = npoiExcelSheet.GetRow(row).GetCell(10).StringCellValue;
                            var cardinality = npoiExcelSheet.GetRow(row).GetCell(4).StringCellValue;
                            var constraintPhase = npoiExcelSheet.GetRow(row).GetCell(11).CellType == NPOI.SS.UserModel.CellType.Numeric
                                ? npoiExcelSheet.GetRow(row).GetCell(11).NumericCellValue.ToString()
                                : npoiExcelSheet.GetRow(row).GetCell(11).StringCellValue;
                            var childKeyList = childKeys.Split(',');
                            var parentKeyList = parentKeys.Split(',');
                            if (!string.IsNullOrWhiteSpace(childFile) && !string.IsNullOrWhiteSpace(parentFile) &&
                                childKeyList.Count() > 0)
                            {
                                for (var i = 0; i < childKeyList.Count(); i++)
                                {
                                    var childKey = childKeyList[i];
                                    var parentKey = parentKeyList[i];
                                    // Cardinality is child : parent
                                    if (constraintPhase != "1" && constraintPhase != "2" && constraintPhase != "3" && constraintPhase != "4"
                                        && constraintPhase != "Invalid" && !childKeys.Contains("XX") && !parentKeys.Contains("XX"))
                                    {
                                        relationships.Add(_AddRelationship(parentSchema.Trim(), parentFile.Trim(), parentKey.Trim(),
                                                                           childSchema.Trim(), childFile.Trim(), childKey.Trim(),
                                                                           SchemaRelationshipType.OneToMany));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

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
                    //Not needed for Gold Cross
                    //gvc.ColumnDescription = ToTitleCase(gvc.ColumnDescription);
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
                    moduleEntity.AllColumns.OrderBy(x => x.ContentWindowDisplaySequence).ToList());
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
                    vce.ContentWindowDisplaySequence = contentWindowDispSeq;
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
                ViewField = AB_GenerationWizardShared.am_RemoveIllegalCharacters(columnDescription),
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

        internal AB_GenerationViewColumnEntity AddUndefinedVirtualFieldForDate(AB_GenerationModuleEntity moduleEntity, string columnDescription)
        {
            var virtualField = new AB_GenerationViewColumnEntity()
            {
                EntityPropertyName = AB_GenerationWizardShared.am_RemoveIllegalCharacters(columnDescription),
                ColumnDescription = columnDescription,
                IsVirtual = true,
                VirtualFieldType = AB_VirtualFieldTypes.Undefined,
                PropertyType = AB_PropertyTypes.DateTime,
                FieldVisualization = AB_FieldVisualizations.AB_DatePicker,
                DatabasePropertyType = AB_PropertyTypes.DateTime,
                Type = AB_PropertyTypes.DateTime.ToString().ToUpper(),
                ViewField = AB_GenerationWizardShared.am_RemoveIllegalCharacters(columnDescription),
            };

            _WizardShared.am_AddVirtualColumnToModule(moduleEntity, virtualField);

            return moduleEntity.AllColumns.Where(c => (c.EntityPropertyName == virtualField.EntityPropertyName)).FirstOrDefault();
        }

        #endregion Helper Methods
    }
}