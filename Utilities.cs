using A4DN.CF.WizardShared;
using A4DN.Core.BOS.FrameworkEntity;
using Microsoft.Office.Interop.Excel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace GenerationWizardPlugin
{
	public class DefinedField
	{
		public DefinedField(string fieldName, A4DNFieldType fieldType, MatchOnType matchOnType)
		{
			FieldName = fieldName;
			FieldType = fieldType;
			MatchOnType = matchOnType;
		}

		public string FieldName { get; set; }
		public A4DNFieldType FieldType { get; set; }
		public MatchOnType MatchOnType { get; set; }
	}

	public class ModuleConstants
	{
		public ModuleConstants(string moduleName) => ModuleName = moduleName;

		public string ModuleName { get; set; }

		public List<DefinedField> DefinedFields { get; set; } = new List<DefinedField>();
	}

	public enum A4DNFieldType // These should match the values in the Field Types table your Constants Excel workbook (spaces will be ignored)
	{
		Identity,
		Audit,
		Title,
		NoCopy,
		Required,
		ExtendedSearch,
		ExcludeFromContent,
		ExcludeFromSearch,
		ExcludeFromDetail,
		Hidden,
		Currency,
		Percent,
		Date,
		Time,
		DateTime
	}

	public enum MatchOnType
	{
		EndsWith,
		StartsWith,
		Contains,
		Equals,
	}

	public static class A4DNPluginHelpers
	{
		public static bool IsFieldMatch(this AB_GenerationViewColumnEntity viewColumnEntity, List<DefinedField> matchFields)
		{
			var viewField = viewColumnEntity.ViewField.ToUpper();

			return matchFields.Any(x =>
				(x.MatchOnType == MatchOnType.Equals && viewField.Equals(x.FieldName.ToUpper())) ||
				(x.MatchOnType == MatchOnType.EndsWith && viewField.EndsWith(x.FieldName.ToUpper())) ||
				(x.MatchOnType == MatchOnType.StartsWith && viewField.StartsWith(x.FieldName.ToUpper())) ||
				(x.MatchOnType == MatchOnType.Contains && viewField.Contains(x.FieldName.ToUpper()))
			);
		}

		public static string ToTitleCase(string s)
		{
			var cultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
			return cultureInfo.TextInfo.ToTitleCase(s.ToLower());
		}

		public static void AddViewByViewField(this AB_GenerationWizardShared wizardShared, AB_GenerationModuleEntity moduleEntity, string viewField)
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

				var viewToAdd = wizardShared.am_CreateDefaultView(viewField, viewField, viewColumns);
				if (viewToAdd == null)
				{
					MessageBox.Show("Error adding View: By " + viewField + " to module: " + moduleEntity.ModuleName);
					return;
				}
				wizardShared.am_AddViewToModule(viewToAdd, moduleEntity);

				wizardShared.am_SetDefaultView(viewToAdd, moduleEntity);

				wizardShared.am_SetFirstViewColumnAsFirstContentWindowColumn(moduleEntity);
			}
		}

		public static void MoveFieldsInContentWindow(AB_GenerationModuleEntity moduleEntity)
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

		public static string FormatModuleName(string s)
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

		public static string StringBuilderReplaceKeywords(StringBuilder data)
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
	}

	public static class NPOIHelpers
	{
		public static List<XSSFTable> GetTables(this ISheet sheet)
		{
			List<XSSFTable> tables = new List<XSSFTable>();

			if (sheet is XSSFSheet xssfSheet)
			{
				foreach (var table in xssfSheet.GetTables())
				{
					tables.Add(table);
				}
			}

			return tables;
		}

		public static List<ISheet> GetViableSheets(this IWorkbook constantsWorkbook)
		{
			List<ISheet> viableSheets = new List<ISheet>();
			for (int i = 0; i < constantsWorkbook.NumberOfSheets; i++)
			{
				ISheet sheet = constantsWorkbook.GetSheetAt(i);
				List<string> sheetNamesToExclude = new List<string>() { "Types" };
				if (!sheetNamesToExclude.Contains(sheet.SheetName))
					viableSheets.Add(sheet);
			}

			return viableSheets;
		}

		public static void AddModuleConstants(this List<ModuleConstants> moduleConstantsList, XSSFWorkbook workbook)
		{
			moduleConstantsList.Clear();

			foreach (var sheet in workbook.GetViableSheets())
			{
				// If ModuleConstants class has already been added, use that one, otherwise make a new one
				ModuleConstants moduleConstants = moduleConstantsList.FirstOrDefault(x => x.ModuleName == sheet.SheetName);
				if (moduleConstants == null) moduleConstants = new ModuleConstants(sheet.SheetName);

				foreach (var table in sheet.GetTables())
				{
					CellReference startCell = table.StartCellReference;
					int headerRowNum = table.GetXSSFSheet().GetRow(startCell.Row).RowNum + 1;

					int fieldColumnIndex = table.FindColumnIndex("Field") + 1;
					int fieldTypeColumnIndex = table.FindColumnIndex("Field Type") + 1;
					int matchOnColumnIndex = table.FindColumnIndex("Match On") + 1;

					for (int rowIndex = headerRowNum; rowIndex <= table.RowCount; rowIndex++)
					{
						IRow row = sheet.GetRow(rowIndex);
						if (row == null) continue;

						var fieldName = row.GetCell(fieldColumnIndex)?.ToString();
						if (string.IsNullOrWhiteSpace(fieldName)) continue; // If there is no field name, ignore the row
						var fieldType = (A4DNFieldType)Enum.Parse(typeof(A4DNFieldType), row.GetCell(fieldTypeColumnIndex)?.ToString().Replace(" ", ""));
						var matchOn = (MatchOnType)Enum.Parse(typeof(MatchOnType), row.GetCell(matchOnColumnIndex)?.ToString().Replace(" ", ""));

						moduleConstants.DefinedFields.Add(new DefinedField(fieldName, fieldType, matchOn));
					}
				}

				moduleConstantsList.Add(moduleConstants);
			}
		}

		public static bool KillExcelProcess(string workbookFileName)
		{
			try
			{
				Microsoft.Office.Interop.Excel.Application excelApp = (Microsoft.Office.Interop.Excel.Application)Marshal.GetActiveObject("Excel.Application");

				foreach (Workbook workbook in excelApp.Workbooks)
				{
					if (workbook.FullName.Contains(workbookFileName))
					{
						workbook.Close();
						return true;
					}
				}
					
				return false;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error killing Excel process: {ex.Message}");
				return false;
			}
		}
	}
}
