using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AccreditationSchoolSystem.Pages
{
    public class HomePageModel : PageModel
    {
        private readonly ILogger<HomePageModel> _logger;

        public HomePageModel(ILogger<HomePageModel> logger)
        {
            _logger = logger;

            // Initialize data
            InitializeData();
        }

        #region Form Properties

        [BindProperty]
        [Display(Name = "Tracking ID")]
        public string TrackingId { get; set; }

        [BindProperty]
        [Display(Name = "Status")]
        public string StatusFilter { get; set; }

        [BindProperty]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [BindProperty]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(7);

        [BindProperty]
        [Display(Name = "Application Type")]
        public int? ApplicationTypeId { get; set; }

        [BindProperty]
        [Display(Name = "Include Expired")]
        public bool IncludeExpired { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email Address")]
        public string SubscriberEmail { get; set; }

        [BindProperty]
        [Display(Name = "City")]
        public string SubscriberCity { get; set; }

        [BindProperty]
        [Display(Name = "Consent")]
        [Required(ErrorMessage = "You must agree to receive updates")]
        public bool SubscriberConsent { get; set; }

        #endregion

        #region Data Collections

        public List<SelectListItem> StatusOptions { get; set; }
        public List<SelectListItem> ApplicationTypes { get; set; }
        public List<QuickLinkModel> QuickLinks { get; set; }
        public List<ServiceModel> PlanningServices { get; set; }
        public List<EducationOptionModel> EducationOptions { get; set; }
        public List<AwardModel> Awards { get; set; }
        public List<FooterColumnModel> FooterColumns { get; set; }
        public List<FooterLinkModel> FooterBottomLinks { get; set; }

        #endregion

        public void OnGet()
        {
            // Page is already initialized in constructor
        }

        public IActionResult OnPostTrackApplication()
        {
            if (!ModelState.IsValid)
            {
                InitializeData();
                return Page();
            }

            // Process the application tracking
            _logger.LogInformation($"Tracking application: {TrackingId}, Status Filter: {StatusFilter}");

            // In a real app, you would search for the application in your database

            // For demonstration, redirect to a results page
            return RedirectToPage("/Application/TrackingResults", new { id = TrackingId, status = StatusFilter });
        }

        public IActionResult OnPostSubscribe()
        {
            // Only validate the newsletter subscription fields
            ModelState.ClearValidationState(nameof(TrackingId));
            ModelState.ClearValidationState(nameof(StatusFilter));
            ModelState.MarkFieldValid(nameof(TrackingId));
            ModelState.MarkFieldValid(nameof(StatusFilter));

            if (!ModelState.IsValid)
            {
                InitializeData();
                return Page();
            }

            // Process newsletter subscription
            _logger.LogInformation($"New subscriber: {SubscriberEmail}, City: {SubscriberCity}");

            // In a real app, you would add the subscriber to your database or email service

            // Add a success message
            TempData["SuccessMessage"] = "Thank you for subscribing! You'll receive our updates soon.";

            return RedirectToPage();
        }

        private void InitializeData()
        {
            // Initialize dropdown options
            StatusOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "pending", Text = "Pending" },
                new SelectListItem { Value = "approved", Text = "Approved" },
                new SelectListItem { Value = "rejected", Text = "Rejected" },
                new SelectListItem { Value = "review", Text = "Under Review" }
            };

            ApplicationTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "New School Registration" },
                new SelectListItem { Value = "2", Text = "Renewal" },
                new SelectListItem { Value = "3", Text = "TVET Program" },
                new SelectListItem { Value = "4", Text = "Primary Level" },
                new SelectListItem { Value = "5", Text = "Secondary Level" }
            };

            // Initialize quick links
            QuickLinks = new List<QuickLinkModel>
            {
                new QuickLinkModel
                {
                    Title = "Apply for Accreditation",
                    IconClass = "fas fa-file-alt",
                    Url = "NewSchoolRegistration"
                },
                new QuickLinkModel
                {
                    Title = "Registered Schools",
                    IconClass = "fas fa-school",
                    Url = "AccreditedSchool"
                },
                new QuickLinkModel
                {
                    Title = "Check Application Status",
                    IconClass = "fas fa-clipboard-check",
                    Url = "TrackApplication"
                },
                new QuickLinkModel
                {
                    Title = "Certificate Verification",
                    IconClass = "fas fa-id-card",
                    Url = "DownloadCertificate"
                }
            };

            // Initialize planning services
            PlanningServices = new List<ServiceModel>
            {
                new ServiceModel
                {
                    Title = "Track Application",
                    Description = "Start your journey in education by registering a new school. Get guidance on requirements and standards for establishing an educational institution.",
                    ImageUrl = "/images/track-application.jpg",
                    Url = "TrackApplication",
                    ButtonText = "Track Now"
                },
                new ServiceModel
                {
                    Title = "Accredited Schools",
                    Description = "Start your journey in education by registering a new school. Get guidance on requirements and standards for establishing an educational institution.",
                    ImageUrl = "/images/accredited-schools.jpg",
                    Url = "AccreditedSchool",
                    ButtonText = "View Schools"
                },
                new ServiceModel
                {
                    Title = "New School Registration",
                    Description = "Start your journey in education by registering a new school. Get guidance on requirements and standards for establishing an educational institution.",
                    ImageUrl = "/images/new-school.jpg",
                    Url = "NewSchoolRegistration",
                    ButtonText = "Register New School"
                },
                new ServiceModel
                {
                    Title = "TVET Programs",
                    Description = "Start your journey in education by registering a new school. Get guidance on requirements and standards for establishing an educational institution.",
                    ImageUrl = "/images/submit-claim.jpg",
                    Url = "TVETTrades",
                    ButtonText = "Apply Now"
                }
            };

            // Initialize education options
            EducationOptions = new List<EducationOptionModel>
            {
                new EducationOptionModel
                {
                    Title = "TVET Trades",
                    DateRange = "13 Apr 2025 - 18 Apr 2025",
                    PriceInfo = "Certification from USD 970",
                    ImageUrl = "/images/tvet-trades.jpg"
                },
                new EducationOptionModel
                {
                    Title = "Primary Level",
                    DateRange = "04 May 2025 - 09 May 2025",
                    PriceInfo = "Certification from USD 994",
                    ImageUrl = "/images/primary-level.JPG"
                },
                new EducationOptionModel
                {
                    Title = "Secondary Level",
                    DateRange = "11 Apr 2025 - 04 May 2025",
                    PriceInfo = "Certification from USD 1023",
                    ImageUrl = "/images/secondary-level.jpg"
                }
            };

            // Initialize awards
            Awards = new List<AwardModel>
            {
                new AwardModel
                {
                    Title = "Top Accreditation Agency",
                    LogoUrl = "/images/award1.jpg"
                },
                new AwardModel
                {
                    Title = "Excellence in Education",
                    LogoUrl = "/images/award2.jpg"
                },
                new AwardModel
                {
                    Title = "Quality Recognition",
                    LogoUrl = "/images/award3.jpg"
                },
                new AwardModel
                {
                    Title = "Best Accreditation in Africa",
                    LogoUrl = "/images/award4.jpg"
                }
            };

            // Initialize footer columns
            FooterColumns = new List<FooterColumnModel>
            {
                new FooterColumnModel
                {
                    Title = "Accreditation System",
                    Links = new List<FooterLinkModel>
                    {
                        new FooterLinkModel { Text = "About us", Url = "/About" },
                        new FooterLinkModel { Text = "Careers", Url = "/Careers" },
                        new FooterLinkModel { Text = "Press releases", Url = "/Press" },
                        new FooterLinkModel { Text = "Sponsorship", Url = "/Sponsorship" },
                        new FooterLinkModel { Text = "Annual reports", Url = "/Reports" },
                        new FooterLinkModel { Text = "Environmental sustainability", Url = "/Sustainability" }
                    }
                },
                new FooterColumnModel
                {
                    Title = "Group companies",
                    Links = new List<FooterLinkModel>
                    {
                        new FooterLinkModel { Text = "International Partnerships", Url = "/Partners/International" },
                        new FooterLinkModel { Text = "Campus Facilities", Url = "/Facilities" },
                        new FooterLinkModel { Text = "Executive Education", Url = "/Education/Executive" },
                        new FooterLinkModel { Text = "Certification Programs", Url = "/Programs/Certification" },
                        new FooterLinkModel { Text = "Internal Media Services", Url = "/Services/Media" },
                        new FooterLinkModel { Text = "Quality Assurance", Url = "/QualityAssurance" }
                    }
                },
                new FooterColumnModel
                {
                    Title = "Services",
                    Links = new List<FooterLinkModel>
                    {
                        new FooterLinkModel { Text = "School Registration", Url = "/Schools/Register" },
                        new FooterLinkModel { Text = "TVET Programs", Url = "/Programs/TVET" },
                        new FooterLinkModel { Text = "Accreditation meetings", Url = "/Meetings" },
                        new FooterLinkModel { Text = "Advertise with us", Url = "/Advertise" },
                        new FooterLinkModel { Text = "Quality Control", Url = "/Quality" },
                        new FooterLinkModel { Text = "Compliance Reviews", Url = "/Compliance" }
                    }
                },
                new FooterColumnModel
                {
                    Title = "Partners",
                    Links = new List<FooterLinkModel>
                    {
                        new FooterLinkModel { Text = "Ministry of Education", Url = "/Partners/Ministry" },
                        new FooterLinkModel { Text = "Global Standards Registry", Url = "/Partners/Standards" },
                        new FooterLinkModel { Text = "Trade partners", Url = "/Partners/Trade" },
                        new FooterLinkModel { Text = "Government Agencies", Url = "/Partners/Government" },
                        new FooterLinkModel { Text = "International Boards", Url = "/Partners/International" }
                    }
                },
                new FooterColumnModel
                {
                    Title = "Help",
                    Links = new List<FooterLinkModel>
                    {
                        new FooterLinkModel { Text = "Contact us", Url = "/Contact" },
                        new FooterLinkModel { Text = "Browse FAQs", Url = "/FAQ" },
                        new FooterLinkModel { Text = "Application alerts", Url = "/Alerts" }
                    }
                }
            };

            // Initialize footer bottom links
            FooterBottomLinks = new List<FooterLinkModel>
            {
                new FooterLinkModel { Text = "Cookie policy", Url = "/Cookies" },
                new FooterLinkModel { Text = "Legal", Url = "/Legal" },
                new FooterLinkModel { Text = "Privacy", Url = "/Privacy" },
                new FooterLinkModel { Text = "Accessibility", Url = "/Accessibility" },
                new FooterLinkModel { Text = "Terms and conditions", Url = "/Terms" },
                new FooterLinkModel { Text = "Sitemap", Url = "/Sitemap" },
                new FooterLinkModel { Text = "Cookie Consent", Url = "/CookieConsent" }
            };
        }
    }

    #region Models

    public class QuickLinkModel
    {
        public string Title { get; set; }
        public string IconClass { get; set; }
        public string Url { get; set; }
    }

    public class ServiceModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Url { get; set; }
        public string ButtonText { get; set; }
    }

    public class EducationOptionModel
    {
        public string Title { get; set; }
        public string DateRange { get; set; }
        public string PriceInfo { get; set; }
        public string ImageUrl { get; set; }
    }

    public class AwardModel
    {
        public string Title { get; set; }
        public string LogoUrl { get; set; }
    }

    public class FooterColumnModel
    {
        public string Title { get; set; }
        public List<FooterLinkModel> Links { get; set; }
    }

    public class FooterLinkModel
    {
        public string Text { get; set; }
        public string Url { get; set; }
    }

    #endregion
}