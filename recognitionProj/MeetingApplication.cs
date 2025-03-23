public class MeetingApplication
{
    public int MeetingID { get; set; }
    public string CreationDate { get; set; }
    public string MeetingNumber { get; set; }
    public int InsID { get; set; }
    public string InstitutionName { get; set; }
    public string Specialization { get; set; }
    public string SubmissionDate { get; set; }
    public int SupervisorID { get; set; }
    public string SupervisorName { get; set; }
}
public class MeetingInfo
{
    public string MeetingNumber { get; set; }
    public DateTime CreationDate { get; set; }
}
