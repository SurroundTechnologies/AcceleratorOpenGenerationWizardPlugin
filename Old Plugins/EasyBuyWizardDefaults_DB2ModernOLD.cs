﻿using A4DN.Core.BOS.Base;
using A4DN.Core.BOS.FrameworkEntity;
using System.Collections.Generic;
using System.Linq;

namespace GenerationWizardPlugin
{
    public class EasyBuyWizardDefaults_DB2ModernOLD : WizardDefaults
    {
        internal override void SetModuleRulesBeforeColumnRules(WizardDefaults.Mode mode, AB_GenerationModuleEntity moduleEntity)
        {
            AuditStamps = new Dictionary<string, AB_AuditStampTypes>()
                {
                    {"CreatedAt", AB_AuditStampTypes.CreateDate},
                    {"CreatedBy", AB_AuditStampTypes.CreateUser},
                    {"CreatedWith", AB_AuditStampTypes.Undefined},
                    {"LastModifiedAt", AB_AuditStampTypes.LastChangeDate},
                    {"LastModifiedBy", AB_AuditStampTypes.LastChangeUser},
                    {"LastModifiedWith", AB_AuditStampTypes.Undefined},
                };

            DetailTitleField = new List<string>
                {
                    "Name",
                    "LastName",
                    "FirstName"
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

                    // This is done to eliminate conflicts with having the Internal ID referenced multiple times in the Module for each join.
                    if (viewColumnEntity.EntityPropertyName.ToUpper() == "INTERNALID")
                    {
                        viewColumnEntity.EntityPropertyName = moduleEntity.ModuleName.Replace(" ", "") + "InternalID";
                        viewColumnEntity.ColumnDescription = moduleEntity.ModuleName + " Internal ID";
                    }


                    // Set TimeStamp, TIMESTMP, Type fields to DateTime
                    if (viewColumnEntity.Type.Contains("TIMESTMP"))
                    {
                        //viewColumnEntity.PropertyType = AB_PropertyTypes.DateTime;
                        viewColumnEntity.FieldVisualization = AB_FieldVisualizations.AB_DateTimePickerWithLabel;
                    }

                    // Uncheck Warehouse ID and Sales Person ID Fields from Content Window, Search Explorer Bar and Detail
                    if (viewColumnEntity.ViewField == "WarhouseInternalID" || viewColumnEntity.ViewField == "SalesPersonInternalID")
                    {
                        viewColumnEntity.IsContentWindowField = false;
                        viewColumnEntity.IsExplorerBarField = false;
                        viewColumnEntity.IsDetailField = false;
                        viewColumnEntity.IsAuditStampField = false;
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

                    // Set Any Fields that are not audit stamps but end in "Date" or "Time" as Detail Title Fields
                    if ((viewColumnEntity.ViewField.ToUpper().EndsWith("Date") || viewColumnEntity.ViewField.ToUpper().EndsWith("Time")) && !IsAuditTabField(viewColumnEntity))
                    {
                        viewColumnEntity.IsTitleField = true;
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
                    if (moduleEntity.GenerationModuleExplorers[0] != null && moduleEntity.GenerationModuleExplorers[0].GenerationModuleExplorerViews != null)
                    {
                        foreach (var view in moduleEntity.GenerationModuleExplorers[0].GenerationModuleExplorerViews.OrderBy(x => x.ViewName))
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
            const string defaultViewSuffix = "Name";

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