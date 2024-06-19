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
    public class AdventureWorksWizardDefaults : WizardDefaults
    {

        internal override void SetColumnRules(Mode mode, AB_GenerationModuleEntity moduleEntity, AB_GenerationViewColumnEntity viewColumnEntity)
        {
            base.SetColumnRules(mode, moduleEntity, viewColumnEntity);

            switch (mode)
            {
                case Mode.InitialSetup:

                    // This is done to eliminate conflicts with having the Business Entity ID referenced multiple times in a file
                    if (viewColumnEntity.EntityPropertyName.ToUpper() == "BUSINESSENTITYID")
                    {

                        viewColumnEntity.EntityPropertyName = moduleEntity.ModuleName.Replace(" ", "") + "BusinessEntityID";
                        viewColumnEntity.ColumnDescription = moduleEntity.ModuleName + " Business Entity ID";
                    }


                    break;

                case Mode.ColumnsChanged:

                    break;

            }

        }

    }
}
