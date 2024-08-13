using A4DN.CF.SchemaEntities;
using A4DN.Core.BOS.Base;
using A4DN.Core.BOS.NPOI.Excel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Forms;

namespace GenerationWizardPlugin.EBHelpers
{
	public enum EasyBuyTable
	{
		Customer,
		ShippingAddress,
		Product,
		Order,
		OrderItem
	}

	public enum DBType
	{
		Legacy,
		Modern
	}

	public enum DBBrand
	{
		MSSQL,
		IBMiDB2
	}

	public static class EasyBuyHelpers
	{
		public static List<ModuleConstants> EasyBuyModuleConstants = new List<ModuleConstants>();

		public static string ConstantsWorkbookPath = "C:\\Surround\\AcceleratorOpenGenerationWizardPlugin\\EasyBuyConstants\\";
		public static string LegacyConstantsWorkbook = "EasyBuyConstants_Legacy.xlsx";
		public static string ModernConstantsWorkbook = "EasyBuyConstants_Modern.xlsx";


		public static void InitializeExcelConstants(DBType dbType)
		{
			XSSFWorkbook constantsWorkbook = new XSSFWorkbook(); // must have a value assigned here
			bool workbookFound = false;
			string constantsWorkbookToUse = "";

			switch (dbType)
			{
				case DBType.Legacy:
					constantsWorkbookToUse = LegacyConstantsWorkbook;
					break;
				case DBType.Modern:
					constantsWorkbookToUse = ModernConstantsWorkbook;
					break;
			}

			while (!workbookFound)
			{
				try
				{
					constantsWorkbook = new AB_NPOIExcel().am_OpenWorkbook(ConstantsWorkbookPath + constantsWorkbookToUse) as XSSFWorkbook;
					workbookFound = true;
				}
				catch (Exception e)
				{
					var messageBox = MessageBox.Show(e.Message + 
													 Environment.NewLine + Environment.NewLine + 
													 "Would you like to close the workbook and proceed?", "Error Opening Constants Workbook", MessageBoxButtons.OKCancel);

					if (messageBox == DialogResult.OK)
					{
						if (NPOIHelpers.KillExcelProcess(constantsWorkbookToUse))
						{
							continue;
						}
						else
						{
							var messageBox2 = MessageBox.Show(e.Message + 
															  Environment.NewLine + Environment.NewLine + 
															  "Failed to automatically close the workbook, please close it manually before proceeding", "Error Opening Constants Workbook", MessageBoxButtons.OK);
						}
					}
					else
					{
						var unstableMessageBox = MessageBox.Show("No Constants workbook found, plugin will be unstable. Please fix the error and restart Generation Wizard.", "Error Opening Constants Workbook", MessageBoxButtons.OK);
						break;
					}
				}
			}

			if (workbookFound)
			{
				try
				{
					EasyBuyModuleConstants.AddModuleConstants(constantsWorkbook);
				}
				catch (Exception e)
				{
					var messageBox = MessageBox.Show(e.Message, "Plugin Error", MessageBoxButtons.OK);
				}
			}
		}

		public static List<DefinedField> GetDefinedFieldsForTable(EasyBuyTable table, A4DNFieldType fieldType)
		{
			List<DefinedField> fields = new List<DefinedField>();

			// Add the Shared Constants fields to all modules' defined fields
			fields.AddRange(EasyBuyModuleConstants.FirstOrDefault(x => x.ModuleName == "Shared Constants").DefinedFields.Where(x => x.FieldType == fieldType));

			switch (table)
			{
				case EasyBuyTable.Customer:
					fields.AddRange(EasyBuyModuleConstants.FirstOrDefault(x => x.ModuleName == "Customer").DefinedFields.Where(x => x.FieldType == fieldType));
					break;

				case EasyBuyTable.ShippingAddress:
					fields.AddRange(EasyBuyModuleConstants.FirstOrDefault(x => x.ModuleName == "Shipping Address").DefinedFields.Where(x => x.FieldType == fieldType));
					break;

				case EasyBuyTable.Product:
					fields.AddRange(EasyBuyModuleConstants.FirstOrDefault(x => x.ModuleName == "Product").DefinedFields.Where(x => x.FieldType == fieldType));
					break;

				case EasyBuyTable.Order:
					fields.AddRange(EasyBuyModuleConstants.FirstOrDefault(x => x.ModuleName == "Order").DefinedFields.Where(x => x.FieldType == fieldType));
					break;

				case EasyBuyTable.OrderItem:
					fields.AddRange(EasyBuyModuleConstants.FirstOrDefault(x => x.ModuleName == "Order Item").DefinedFields.Where(x => x.FieldType == fieldType));
					break;

				default:
					return null;
			}

			return fields;
		}

		#region Audit Stamps

		public static Dictionary<string, AB_AuditStampTypes> LegacyAuditStampTypes = new Dictionary<string, AB_AuditStampTypes>
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

		public static Dictionary<string, AB_AuditStampTypes> ModernAuditStampTypes = new Dictionary<string, AB_AuditStampTypes>
		{
			{"CreatedAt", AB_AuditStampTypes.CreateDate},
			{"CreatedBy", AB_AuditStampTypes.CreateUser},
			{"CreatedWith", AB_AuditStampTypes.Undefined},
			{"LastModifiedAt", AB_AuditStampTypes.LastChangeDate},
			{"LastModifiedBy", AB_AuditStampTypes.LastChangeUser},
			{"LastModifiedWith", AB_AuditStampTypes.Undefined},
		};

		/// <summary>
		/// Logic to determine the Audit Stamp Type
		/// </summary>
		public static AB_AuditStampTypes AuditStampType(string name, DBType dbType)
		{
			bool isAudit;

			switch (dbType)
			{
				case DBType.Legacy:
					isAudit = LegacyAuditStampTypes.Any(x => name.ToUpper().Contains(x.Key.ToUpper()));
					return isAudit ? LegacyAuditStampTypes.Where(aud => name.ToUpper().Contains(aud.Key.ToUpper())).Select(aud => aud.Value).FirstOrDefault() : AB_AuditStampTypes.Undefined;

				case DBType.Modern:
					isAudit = ModernAuditStampTypes.Any(x => name.ToUpper().Contains(x.Key.ToUpper()));
					return isAudit ? ModernAuditStampTypes.Where(aud => name.ToUpper().Contains(aud.Key.ToUpper())).Select(aud => aud.Value).FirstOrDefault() : AB_AuditStampTypes.Undefined;

				default:
					break;
			}

			return AB_AuditStampTypes.Undefined;
		}

		#endregion

		#region Relationships

		public static ObservableCollection<AB_SchemaRelationshipEntity> GetDatabaseRelationships(DBBrand dbBrand, params string[] schemas)
		{
			var relationships = new ObservableCollection<AB_SchemaRelationshipEntity>();

			switch (dbBrand)
			{
				case DBBrand.IBMiDB2:
					foreach (var schema in schemas)
					{
						// Customers have One to Many Orders
						relationships.Add(_AddRelationship(schema, "YD1C", "YD1CIID", schema, "YD1O", "YD1O1CID", SchemaRelationshipType.OneToMany));
						// Customers have One to Many Shipping Addresses
						relationships.Add(_AddRelationship(schema, "YD1C", "YD1CIID", schema, "YD1S", "YD1S1CID", SchemaRelationshipType.OneToMany));
						// Customers have Sub Customers
						relationships.Add(_AddRelationship(schema, "YD1C", "YD1CIID", schema, "YD1C", "YD1CPTID", SchemaRelationshipType.OneToMany));
						// Orders have One to Many Order Items
						relationships.Add(_AddRelationship(schema, "YD1O", "YD1OIID", schema, "YD1I", "YD1I1OID", SchemaRelationshipType.OneToMany));
						// Products have One to Many Order Items
						relationships.Add(_AddRelationship(schema, "YD1P", "YD1PIID", schema, "YD1I", "YD1I1PID", SchemaRelationshipType.OneToMany));
						// Shipping Addresses have One to Many Order
						relationships.Add(_AddRelationship(schema, "YD1S", "YD1SIID", schema, "YD1O", "YD1O1SID", SchemaRelationshipType.OneToMany));
					}
					break;

				case DBBrand.MSSQL:
					foreach (var schema in schemas)
					{
						// Customers have One to Many Orders
						relationships.Add(_AddRelationship(schema, "[dbo].[YD1C]", "YD1CIID", schema, "[dbo].[YD1O]", "YD1O1CID", SchemaRelationshipType.OneToMany));
						// Customers have One to Many Shipping Addresses
						relationships.Add(_AddRelationship(schema, "[dbo].[YD1C]", "YD1CIID", schema, "[dbo].[YD1S]", "YD1S1CID", SchemaRelationshipType.OneToMany));
						// Customers have Sub Customers
						relationships.Add(_AddRelationship(schema, "[dbo].[YD1C]", "YD1CIID", schema, "[dbo].[YD1C]", "YD1CPTID", SchemaRelationshipType.OneToMany));
						// Orders have One to Many Order Items
						relationships.Add(_AddRelationship(schema, "[dbo].[YD1O]", "YD1OIID", schema, "[dbo].[YD1I]", "YD1I1OID", SchemaRelationshipType.OneToMany));
						// Products have One to Many Order Items
						relationships.Add(_AddRelationship(schema, "[dbo].[YD1P]", "YD1PIID", schema, "[dbo].[YD1I]", "YD1I1PID", SchemaRelationshipType.OneToMany));
						// Shipping Addresses have One to Many Order
						relationships.Add(_AddRelationship(schema, "[dbo].[YD1S]", "YD1SIID", schema, "[dbo].[YD1O]", "YD1O1SID", SchemaRelationshipType.OneToMany));
					}
					break;

				default: break;
			}
			

			return relationships;
		}

		public static AB_SchemaRelationshipEntity _AddRelationship(string primarySchema, string primaryTable, string primaryKeyColumn, string foreignSchema, string foreignTable, string foreignKeyColumn, SchemaRelationshipType relationshipType)
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

		#endregion

	}
}
