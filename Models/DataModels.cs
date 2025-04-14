using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccreditationSystem.Models
{
    // School Model
    public class School
    {
        public School()
        {
            // Initialize string properties
            Name = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
            Address = string.Empty;
            Type = string.Empty;
            Status = string.Empty;
            Description = string.Empty;

            // Initialize collections
            Facilities = new List<Facility>();
            Inspections = new List<Inspection>();
            Documents = new List<Document>();
            AccreditationHistory = new List<AccreditationHistory>();
        }

        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string Type { get; set; }

        [Required]
        public string Status { get; set; }

        public DateTime EstablishedDate { get; set; }

        public string Description { get; set; }

        public DateTime LastUpdated { get; set; }

        // Navigation properties
        public List<Facility> Facilities { get; set; }
        public List<Inspection> Inspections { get; set; }
        public List<Document> Documents { get; set; }
        public List<AccreditationHistory> AccreditationHistory { get; set; }
    }

    // Facility Model
    public class Facility
    {
        public Facility()
        {
            Name = string.Empty;
            Type = string.Empty;
            Equipment = string.Empty;
            Condition = string.Empty;
            School = new School();
        }

        public int Id { get; set; }

        [Required]
        public int SchoolId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Type { get; set; }

        public int Capacity { get; set; }

        public int Size { get; set; }

        public string Equipment { get; set; }

        [Required]
        public string Condition { get; set; }

        // Navigation properties
        public School School { get; set; }
    }

    // Equipment Model
    public class Equipment
    {
        public Equipment()
        {
            Name = string.Empty;
            EquipmentId = string.Empty;
            Category = string.Empty;
            Location = string.Empty;
            Status = string.Empty;
            Condition = string.Empty;
            Description = string.Empty;
            Manufacturer = string.Empty;
            Model = string.Empty;
            SerialNumber = string.Empty;
            ImageUrl = string.Empty;
        }

        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string EquipmentId { get; set; }

        [Required]
        public string Category { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public DateTime PurchaseDate { get; set; }

        public decimal PurchasePrice { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public string Condition { get; set; }

        public string Description { get; set; }

        public string Manufacturer { get; set; }

        public string Model { get; set; }

        public string SerialNumber { get; set; }

        public DateTime? WarrantyExpiry { get; set; }

        public string ImageUrl { get; set; }
    }

    // Application Model
    public class Application
    {
        public Application()
        {
            Email = string.Empty;
            Service = string.Empty;
            NationalId = string.Empty;
            Phone = string.Empty;
            Status = string.Empty;
            ReviewedBy = string.Empty;
            Comments = string.Empty;
        }

        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(50)]
        public string Service { get; set; }

        [Required]
        [StringLength(20)]
        public string NationalId { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }

        [Required]
        public string Status { get; set; }

        public DateTime SubmissionDate { get; set; }

        public DateTime? ReviewDate { get; set; }

        public string ReviewedBy { get; set; }

        public string Comments { get; set; }
    }

    // Inspection Model
    public class Inspection
    {
        public Inspection()
        {
            Inspector = string.Empty;
            Type = string.Empty;
            Result = string.Empty;
            Comments = string.Empty;
            School = new School();
        }

        public int Id { get; set; }

        [Required]
        public int SchoolId { get; set; }

        [Required]
        public DateTime InspectionDate { get; set; }

        [Required]
        [StringLength(100)]
        public string Inspector { get; set; }

        [Required]
        public string Type { get; set; }

        [Required]
        public string Result { get; set; }

        public string Comments { get; set; }

        // Navigation properties
        public School School { get; set; }
    }

    // Document Model
    public class Document
    {
        public Document()
        {
            Name = string.Empty;
            Type = string.Empty;
            FilePath = string.Empty;
            FileType = string.Empty;
            Status = string.Empty;
            School = new School();
        }

        public int Id { get; set; }

        [Required]
        public int SchoolId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Type { get; set; }

        [Required]
        public string FilePath { get; set; }

        public string FileType { get; set; }

        public long FileSize { get; set; }

        public DateTime UploadDate { get; set; }

        [Required]
        public string Status { get; set; }

        // Navigation properties
        public School School { get; set; }
    }

    // Accreditation History Model
    public class AccreditationHistory
    {
        public AccreditationHistory()
        {
            AccreditationType = string.Empty;
            Program = string.Empty;
            IssuedBy = string.Empty;
            CertificateNumber = string.Empty;
            School = new School();
        }

        public int Id { get; set; }

        [Required]
        public int SchoolId { get; set; }

        [Required]
        public string AccreditationType { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public string Program { get; set; }

        public string IssuedBy { get; set; }

        public string CertificateNumber { get; set; }

        // Navigation properties
        public School School { get; set; }
    }
}