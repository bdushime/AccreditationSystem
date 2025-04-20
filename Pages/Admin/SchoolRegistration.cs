using System;
using System.ComponentModel.DataAnnotations;

namespace ResponsiveEducationPortal.Models
{
    public class SchoolRegistration
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "School name is required")]
        [StringLength(100, ErrorMessage = "School name cannot exceed 100 characters")]
        [Display(Name = "School Name")]
        public string SchoolName { get; set; }

        [Required(ErrorMessage = "School type is required")]
        [Display(Name = "School Type")]
        public string SchoolType { get; set; }

        [Required(ErrorMessage = "Founding year is required")]
        [Range(1800, 2050, ErrorMessage = "Please enter a valid founding year")]
        [Display(Name = "Year Founded")]
        public int FoundingYear { get; set; }

        [Display(Name = "Registration Number")]
        public string RegistrationNumber { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; }

        [Url(ErrorMessage = "Please enter a valid website URL")]
        [Display(Name = "Website")]
        public string Website { get; set; }

        [Required(ErrorMessage = "Contact person name is required")]
        [Display(Name = "Contact Person")]
        public string ContactPersonName { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Required(ErrorMessage = "City is required")]
        [Display(Name = "City")]
        public string City { get; set; }

        [Required(ErrorMessage = "State/Province is required")]
        [Display(Name = "State/Province")]
        public string State { get; set; }

        [Required(ErrorMessage = "Country is required")]
        [Display(Name = "Country")]
        public string Country { get; set; }

        [Required(ErrorMessage = "Postal/ZIP code is required")]
        [Display(Name = "Postal/ZIP Code")]
        public string PostalCode { get; set; }

        // Application Status
        [Display(Name = "Application Status")]
        public string ApplicationStatus { get; set; } = "Draft";

        // Timestamps
        [Display(Name = "Date Created")]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}