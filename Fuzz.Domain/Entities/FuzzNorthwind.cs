using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fuzz.Domain.Entities;

public class FuzzCategory
{
    [Key]
    public int CategoryID { get; set; }
    [Required]
    [StringLength(15)]
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public byte[]? Picture { get; set; }

    public virtual ICollection<FuzzProduct> Products { get; set; } = new List<FuzzProduct>();
}

public class FuzzCustomer
{
    [Key]
    [StringLength(5)]
    public string CustomerID { get; set; } = string.Empty;
    [Required]
    [StringLength(40)]
    public string CompanyName { get; set; } = string.Empty;
    [StringLength(30)]
    public string? ContactName { get; set; }
    [StringLength(30)]
    public string? ContactTitle { get; set; }
    [StringLength(60)]
    public string? Address { get; set; }
    [StringLength(15)]
    public string? City { get; set; }
    [StringLength(15)]
    public string? Region { get; set; }
    [StringLength(10)]
    public string? PostalCode { get; set; }
    [StringLength(15)]
    public string? Country { get; set; }
    [StringLength(24)]
    public string? Phone { get; set; }
    [StringLength(24)]
    public string? Fax { get; set; }

    public virtual ICollection<FuzzOrder> Orders { get; set; } = new List<FuzzOrder>();
}

public class FuzzEmployee
{
    [Key]
    public int EmployeeID { get; set; }
    [Required]
    [StringLength(20)]
    public string LastName { get; set; } = string.Empty;
    [Required]
    [StringLength(10)]
    public string FirstName { get; set; } = string.Empty;
    [StringLength(30)]
    public string? Title { get; set; }
    [StringLength(25)]
    public string? TitleOfCourtesy { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime? HireDate { get; set; }
    [StringLength(60)]
    public string? Address { get; set; }
    [StringLength(15)]
    public string? City { get; set; }
    [StringLength(15)]
    public string? Region { get; set; }
    [StringLength(10)]
    public string? PostalCode { get; set; }
    [StringLength(15)]
    public string? Country { get; set; }
    [StringLength(24)]
    public string? HomePhone { get; set; }
    [StringLength(4)]
    public string? Extension { get; set; }
    public byte[]? Photo { get; set; }
    public string? Notes { get; set; }
    public int? ReportsTo { get; set; }
    [StringLength(255)]
    public string? PhotoPath { get; set; }

    [ForeignKey("ReportsTo")]
    public virtual FuzzEmployee? Manager { get; set; }
    public virtual ICollection<FuzzEmployee> Subordinates { get; set; } = new List<FuzzEmployee>();
    public virtual ICollection<FuzzOrder> Orders { get; set; } = new List<FuzzOrder>();
}

public class FuzzSupplier
{
    [Key]
    public int SupplierID { get; set; }
    [Required]
    [StringLength(40)]
    public string CompanyName { get; set; } = string.Empty;
    [StringLength(30)]
    public string? ContactName { get; set; }
    [StringLength(30)]
    public string? ContactTitle { get; set; }
    [StringLength(60)]
    public string? Address { get; set; }
    [StringLength(15)]
    public string? City { get; set; }
    [StringLength(15)]
    public string? Region { get; set; }
    [StringLength(10)]
    public string? PostalCode { get; set; }
    [StringLength(15)]
    public string? Country { get; set; }
    [StringLength(24)]
    public string? Phone { get; set; }
    [StringLength(24)]
    public string? Fax { get; set; }
    public string? HomePage { get; set; }

    public virtual ICollection<FuzzProduct> Products { get; set; } = new List<FuzzProduct>();
}

public class FuzzShipper
{
    [Key]
    public int ShipperID { get; set; }
    [Required]
    [StringLength(40)]
    public string CompanyName { get; set; } = string.Empty;
    [StringLength(24)]
    public string? Phone { get; set; }

    public virtual ICollection<FuzzOrder> Orders { get; set; } = new List<FuzzOrder>();
}

public class FuzzProduct
{
    [Key]
    public int ProductID { get; set; }
    [Required]
    [StringLength(40)]
    public string ProductName { get; set; } = string.Empty;
    public int? SupplierID { get; set; }
    public int? CategoryID { get; set; }
    [StringLength(20)]
    public string? QuantityPerUnit { get; set; }
    public decimal? UnitPrice { get; set; } = 0;
    public short? UnitsInStock { get; set; } = 0;
    public short? UnitsOnOrder { get; set; } = 0;
    public short? ReorderLevel { get; set; } = 0;
    public bool Discontinued { get; set; } = false;

    [ForeignKey("CategoryID")]
    public virtual FuzzCategory? Category { get; set; }
    [ForeignKey("SupplierID")]
    public virtual FuzzSupplier? Supplier { get; set; }
    public virtual ICollection<FuzzOrderDetail> OrderDetails { get; set; } = new List<FuzzOrderDetail>();
}

public class FuzzOrder
{
    [Key]
    public int OrderID { get; set; }
    [StringLength(5)]
    public string? CustomerID { get; set; }
    public int? EmployeeID { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? RequiredDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public int? ShipVia { get; set; }
    public decimal? Freight { get; set; } = 0;
    [StringLength(40)]
    public string? ShipName { get; set; }
    [StringLength(60)]
    public string? ShipAddress { get; set; }
    [StringLength(15)]
    public string? ShipCity { get; set; }
    [StringLength(15)]
    public string? ShipRegion { get; set; }
    [StringLength(10)]
    public string? ShipPostalCode { get; set; }
    [StringLength(15)]
    public string? ShipCountry { get; set; }

    [ForeignKey("CustomerID")]
    public virtual FuzzCustomer? Customer { get; set; }
    [ForeignKey("EmployeeID")]
    public virtual FuzzEmployee? Employee { get; set; }
    [ForeignKey("ShipVia")]
    public virtual FuzzShipper? Shipper { get; set; }
    public virtual ICollection<FuzzOrderDetail> OrderDetails { get; set; } = new List<FuzzOrderDetail>();
}

public class FuzzOrderDetail
{
    public int OrderID { get; set; }
    public int ProductID { get; set; }
    public decimal UnitPrice { get; set; } = 0;
    public short Quantity { get; set; } = 1;
    public float Discount { get; set; } = 0;

    [ForeignKey("OrderID")]
    public virtual FuzzOrder Order { get; set; } = null!;
    [ForeignKey("ProductID")]
    public virtual FuzzProduct Product { get; set; } = null!;
}
