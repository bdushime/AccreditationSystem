using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccreditationSystem.Pages.Admin
{
    public class TVETTradesModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }

        public List<TVETProgram> Programs { get; set; }
        public SelectList Categories { get; set; }
        public List<CategoryInfo> CategoryList { get; set; }

        public void OnGet()
        {
            // Initialize category list with counts and icons
            CategoryList = new List<CategoryInfo>
            {
                new CategoryInfo { Key = "Information Technology", Value = 15, Icon = "fas fa-laptop-code" },
                new CategoryInfo { Key = "Healthcare", Value = 12, Icon = "fas fa-heartbeat" },
                new CategoryInfo { Key = "Construction", Value = 10, Icon = "fas fa-hard-hat" },
                new CategoryInfo { Key = "Hospitality", Value = 8, Icon = "fas fa-utensils" },
                new CategoryInfo { Key = "Automotive", Value = 7, Icon = "fas fa-car" },
                new CategoryInfo { Key = "Manufacturing", Value = 9, Icon = "fas fa-industry" },
                new CategoryInfo { Key = "Agriculture", Value = 6, Icon = "fas fa-seedling" },
                new CategoryInfo { Key = "Creative Arts", Value = 8, Icon = "fas fa-paint-brush" }
            };

            // Create dropdown list of categories
            Categories = new SelectList(CategoryList.Select(c => c.Key).ToList());

            // Generate sample programs data (In a real app, this would come from a database)
            var allPrograms = GetSamplePrograms();

            // Apply filters if provided
            var filteredPrograms = allPrograms;
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                filteredPrograms = filteredPrograms.Where(p =>
                    p.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrEmpty(Category))
            {
                filteredPrograms = filteredPrograms.Where(p =>
                    p.Category.Equals(Category, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Calculate pagination
            TotalPages = (int)Math.Ceiling(filteredPrograms.Count / (double)PageSize);
            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

            // Get the current page of programs
            Programs = filteredPrograms
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        private List<TVETProgram> GetSamplePrograms()
        {
            // This would typically come from a database
            return new List<TVETProgram>
            {
                new TVETProgram
                {
                    Id = 1,
                    Name = "Web Development",
                    Category = "Information Technology",
                    Duration = "12 months",
                    Level = "Certificate III",
                    Certification = "National Certificate in Web Development",
                    Description = "This program equips students with skills in HTML, CSS, JavaScript, and modern web frameworks. Students learn to build responsive websites and web applications.",
                    EntryRequirements = "High school diploma or equivalent. Basic computer literacy required.",
                    CareerOpportunities = new List<string> { "Web Developer", "Front-end Developer", "UI Developer", "Freelance Web Designer" }
                },
                new TVETProgram
                {
                    Id = 2,
                    Name = "Nursing Assistant",
                    Category = "Healthcare",
                    Duration = "6 months",
                    Level = "Certificate II",
                    Certification = "Certificate in Nursing Assistance",
                    Description = "This program prepares students for careers in healthcare, focusing on patient care, medical terminology, and basic clinical procedures.",
                    EntryRequirements = "High school diploma. No prior healthcare experience required.",
                    CareerOpportunities = new List<string> { "Nursing Assistant", "Patient Care Technician", "Home Health Aide" }
                },
                new TVETProgram
                {
                    Id = 3,
                    Name = "Electrical Installation",
                    Category = "Construction",
                    Duration = "18 months",
                    Level = "Certificate IV",
                    Certification = "National Certificate in Electrical Installation",
                    Description = "Students learn about electrical systems, wiring, safety procedures, and building codes. Hands-on training with electrical equipment and tools is provided.",
                    EntryRequirements = "High school diploma with mathematics. Basic technical aptitude recommended.",
                    CareerOpportunities = new List<string> { "Electrician", "Electrical Technician", "Maintenance Electrician", "Solar Installer" }
                },
                new TVETProgram
                {
                    Id = 4,
                    Name = "Culinary Arts",
                    Category = "Hospitality",
                    Duration = "12 months",
                    Level = "Certificate III",
                    Certification = "Diploma in Culinary Arts",
                    Description = "This program covers food preparation, cooking techniques, menu planning, and kitchen management. Students gain hands-on experience in professional kitchens.",
                    EntryRequirements = "High school diploma. No prior cooking experience required.",
                    CareerOpportunities = new List<string> { "Chef", "Sous Chef", "Pastry Chef", "Caterer" }
                },
                new TVETProgram
                {
                    Id = 5,
                    Name = "Automotive Repair",
                    Category = "Automotive",
                    Duration = "24 months",
                    Level = "Certificate III",
                    Certification = "National Certificate in Automotive Technology",
                    Description = "Students learn to diagnose, repair, and maintain vehicles. Training includes engine repair, brake systems, electrical systems, and computerized diagnostics.",
                    EntryRequirements = "High school diploma. Basic mechanical aptitude recommended.",
                    CareerOpportunities = new List<string> { "Automotive Technician", "Mechanic", "Service Advisor", "Parts Specialist" }
                },
                new TVETProgram
                {
                    Id = 6,
                    Name = "CNC Machining",
                    Category = "Manufacturing",
                    Duration = "12 months",
                    Level = "Certificate III",
                    Certification = "Certificate in CNC Machining",
                    Description = "This program teaches programming and operation of computer numerical control machines. Students learn blueprint reading, precision measurement, and quality control.",
                    EntryRequirements = "High school diploma with mathematics. Technical aptitude recommended.",
                    CareerOpportunities = new List<string> { "CNC Machinist", "CNC Programmer", "Manufacturing Technician" }
                },
                new TVETProgram
                {
                    Id = 7,
                    Name = "Network Administration",
                    Category = "Information Technology",
                    Duration = "18 months",
                    Level = "Certificate IV",
                    Certification = "Advanced Certificate in Network Administration",
                    Description = "This program covers network design, implementation, security, and troubleshooting. Students work with industry-standard hardware and software.",
                    EntryRequirements = "High school diploma. Basic computer literacy required.",
                    CareerOpportunities = new List<string> { "Network Administrator", "Systems Administrator", "IT Support Specialist", "Network Engineer" }
                },
                new TVETProgram
                {
                    Id = 8,
                    Name = "Graphic Design",
                    Category = "Creative Arts",
                    Duration = "12 months",
                    Level = "Certificate III",
                    Certification = "Certificate in Graphic Design",
                    Description = "Students learn principles of design, color theory, typography, and industry-standard software like Adobe Creative Suite.",
                    EntryRequirements = "High school diploma. Portfolio review may be required.",
                    CareerOpportunities = new List<string> { "Graphic Designer", "Production Artist", "Web Designer", "Multimedia Artist" }
                },
                new TVETProgram
                {
                    Id = 9,
                    Name = "Medical Office Administration",
                    Category = "Healthcare",
                    Duration = "9 months",
                    Level = "Certificate II",
                    Certification = "Certificate in Medical Office Administration",
                    Description = "This program covers medical terminology, office procedures, electronic health records, billing, and insurance processing.",
                    EntryRequirements = "High school diploma. Basic computer skills required.",
                    CareerOpportunities = new List<string> { "Medical Secretary", "Medical Records Clerk", "Healthcare Administrator", "Medical Biller" }
                },
                new TVETProgram
                {
                    Id = 10,
                    Name = "Welding Technology",
                    Category = "Manufacturing",
                    Duration = "12 months",
                    Level = "Certificate III",
                    Certification = "Certificate in Welding Technology",
                    Description = "Students learn various welding techniques including MIG, TIG, and stick welding. Training includes blueprint reading, metallurgy, and safety procedures.",
                    EntryRequirements = "High school diploma. Mechanical aptitude recommended.",
                    CareerOpportunities = new List<string> { "Welder", "Fabricator", "Welding Inspector", "Pipe Welder" }
                }
            };
        }
    }

    public class TVETProgram
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Duration { get; set; }
        public string Level { get; set; }
        public string Certification { get; set; }
        public string Description { get; set; }
        public string EntryRequirements { get; set; }
        public List<string> CareerOpportunities { get; set; }
    }

    public class CategoryInfo
    {
        public string Key { get; set; }
        public int Value { get; set; }
        public string Icon { get; set; }
    }
}
