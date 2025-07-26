namespace InternProject.Models
{

    public class ApplicationForm
    {
        public int Id { get; set; }
        public string applicantUnitName { get; set; }
        public string appliedProjectName { get; set; }
        public string appliedTypeName { get; set; }
        public string participantTypeName { get; set; }
        public string applicationPeriodName { get; set; }
        public string applicationStateName { get; set; }
        public string projectName { get; set; }
        public int applicantUnit { get; set; }
        public int appliedProject { get; set; }
        public int appliedType { get; set; }
        public int participantType { get; set; }
        public int applicationPeriod { get; set; }
        public DateTime? applicationDate { get; set; }
        public int applicationState { get; set; }
        public DateTime? stateDate { get; set; }
        public string grantAmount { get; set; }
    }


}
