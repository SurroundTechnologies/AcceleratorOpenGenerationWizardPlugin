using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerationWizardPlugin.Constants
{
	public enum EasyBuyTable
	{
		Customer,
		ShippingAddress,
		Product,
		Order,
		OrderItem
	}

	public static class EasyBuyHelpers
	{
		public static List<string> GetRequiredFieldsForTable(EasyBuyTable table, bool getSystemDescription = false)
		{
			switch (table)
			{
				case EasyBuyTable.Customer:
					return getSystemDescription ? SystemDescriptionConstants.CustomerRequiredFields : SystemNameConstants.CustomerRequiredFields;

				case EasyBuyTable.ShippingAddress:
					return getSystemDescription ? SystemDescriptionConstants.ShippingAddressRequiredFields : SystemNameConstants.ShippingAddressRequiredFields;

				case EasyBuyTable.Product:
					return getSystemDescription ? SystemDescriptionConstants.ProductRequiredFields : SystemNameConstants.ProductRequiredFields;

				case EasyBuyTable.Order:
					return getSystemDescription ? SystemDescriptionConstants.OrderRequiredFields : SystemNameConstants.OrderRequiredFields;

				case EasyBuyTable.OrderItem:
					return getSystemDescription ? SystemDescriptionConstants.OrderItemRequiredFields : SystemNameConstants.OrderItemRequiredFields;

				default:
					return null;
			}
		}
	}

	public static class SystemNameConstants // Use with DB2Legacy, MSSQLLegacy
	{
		#region Required Fields

		// Required fields that are keys do not need to be included in the required fields list as they are made required with the [AB_Key] attribute

		// Customer Required Fields: Name, LegalName, ContactFirstName, ContactLastName, BillingAddress1, BillingAddress2, BillingAddress3, BillingPostalCode, BillingCountry
		public static readonly List<string> CustomerRequiredFields = new List<string>() { "YD1CNM", "YD1CNMLG", "YD1CCNFN", "YD1CCNLN", "YD1CBLA1", "YD1CBLA2", "YD1CBLA3", "YD1CBLPC", "YD1CBLCY" };

		// Shipping Address Required Fields: CustomerInternalID, Name, ContactFirstName, ContactLastName, Address1, Address2, Address3, PostalCode, Country, Telephone
		public static readonly List<string> ShippingAddressRequiredFields = new List<string>() { "YD1S1CID", "YD1SNM", "YD1SCNFN", "YD1SCNLN", "YD1SSHA1", "YD1SSHA2", "YD1SSHA3", "YD1SSHPC", "YD1SSHCY", "YD1STL" };

		// Product Required Fields: Name, Code, ListPrice
		public static readonly List<string> ProductRequiredFields = new List<string>() { "YD1PNM", "YD1PCD", "YD1PLSPR" };

		// Order Required Fields: CustomerInternalID, ShippingAddressInternalID, WarehouseName, OrderDate, OrderTime, PurchaseOrderNumber, Status, SalesPersonName
		public static readonly List<string> OrderRequiredFields = new List<string>() { "YD1O1CID", "YD1O1SID", "YD1O1WNM", "YD1ODT", "YD1OTM", "YD1OPONO", "YD1OST", "YD1O1ANM" };

		// Order Item Required Fields: OrderInternalID, ProductInternalID, Quantity
		public static readonly List<string> OrderItemRequiredFields = new List<string>() { "YD1I1OID", "YD1I1PID", "YD1IQT" };

		#endregion
	}

	public static class SystemDescriptionConstants // Use with DB2Modern, MSSQLModern
	{
		#region Required Fields

		// Required fields that are keys do not need to be included in the required fields list as they are made required with the [AB_Key] attribute

		// Customer Required Fields: Name, LegalName, ContactFirstName, ContactLastName, BillingAddress1, BillingAddress2, BillingAddress3, BillingPostalCode, BillingCountry
		public static readonly List<string> CustomerRequiredFields = new List<string>() { "Name", "LegalName", "ContactFirstName", "ContactLastName", "BillingAddress1", "BillingAddress2", "BillingAddress3", "BillingPostalCode", "BillingCountry" };

		// Shipping Address Required Fields: CustomerInternalID, Name, ContactFirstName, ContactLastName, Address1, Address2, Address3, PostalCode, Country, Telephone
		public static readonly List<string> ShippingAddressRequiredFields = new List<string>() { "CustomerInternalID", "Name", "ContactFirstName", "ContactLastName", "Address1", "Address2", "Address3", "PostalCode", "Country", "Telephone" };

		// Product Required Fields: Name, Code, ListPrice
		public static readonly List<string> ProductRequiredFields = new List<string>() { "Name", "Code", "ListPrice" };

		// Order Required Fields: CustomerInternalID, ShippingAddressInternalID, WarehouseName, OrderDateTime, PurchaseOrderNumber, Status, SalesPersonName
		public static readonly List<string> OrderRequiredFields = new List<string>() { "CustomerInternalID", "ShippingAddressInternalID", "WarehouseName", "OrderDateTime", "PurchaseOrderNumber", "Status", "SalesPersonName" };

		// Order Item Required Fields: OrderInternalID, ProductInternalID, Quantity
		public static readonly List<string> OrderItemRequiredFields = new List<string>() { "OrderInternalID", "ProductInternalID", "Quantity" };

		#endregion
	}





}
