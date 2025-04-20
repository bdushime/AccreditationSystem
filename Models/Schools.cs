namespace AccreditationSystem.Models
{
    public class Schools
    {
        public int id { get; set; }
        public string school_name { get; set; }
        public string school_email { get; set; }
        public string school_phone { get; set; }
        public string school_website { get; set; }
        public int? year_established { get; set; }
        public string school_type { get; set; }
        public string education_levels { get; set; }

        public int number_of_classrooms { get; set; }
        public decimal average_classroom_size { get; set; }
        public int average_students_per_classroom { get; set; }
        public string classroom_equipment_level { get; set; }
        public string specialized_facilities { get; set; }
        public int total_student_enrollment { get; set; }
        public int number_of_teaching_staff { get; set; }

        public string address_line_1 { get; set; }
        public string address_line_2 { get; set; }
        public string city { get; set; }
        public string state_province { get; set; }

        public string status { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
    }
}
