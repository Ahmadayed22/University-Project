using System.Text.Json.Serialization;

namespace recognitionProj
{
    // Remove or rename the parameters in the constructor so each parameter 
    // matches the property name (case-insensitive).
    // The simplest approach is to either remove the full-args constructor 
    // or ensure each parameter's name exactly matches the corresponding property.

    public class AcademicInfo
    {
        public int InsID { get; set; }
        public int? InsTypeID { get; set; }
        public string InsType { get; set; }
        public int? HighEdu_Rec { get; set; }
        public int? QualityDept_Rec { get; set; }
        public string StudyLangCitizen { get; set; }
        public string StudyLangInter { get; set; }
        public int? JointClass { get; set; }
        public string StudySystem { get; set; }
        public int? MinHours { get; set; }
        public int? MaxHours { get; set; }
        public string? ResearchScopus { get; set; } // remains unused from the form
        public string? ResearchOthers { get; set; } // remains unused from the form
        public int? Practicing { get; set; }
        public int? StudyAttendance { get; set; }
        public int? StudentsMove { get; set; }
        public string StudyAttendanceDesc { get; set; }
        public string StudentsMoveDesc { get; set; }
        public int? DistanceLearning { get; set; }
        public int? MaxHoursDL { get; set; }
        public int? MaxYearsDL { get; set; }
        public int? MaxSemsDL { get; set; }
        public int? Diploma { get; set; }
        public int? DiplomaTest { get; set; }
        public int? HoursPercentage { get; set; }
        public string EducationType { get; set; }
        public string? AvailableDegrees { get; set; }
        public string Speciality { get; set; }
        public int ARWURank { get; set; }
        public int THERank { get; set; }
        public int QSRank { get; set; }
        public string OtherRank { get; set; }
        public int? NumOfScopusResearches { get; set; }
        public int? ScopusFrom { get; set; }
        public int? ScopusTo { get; set; }
        // NEW Clarivate fields:
        public int? NumOfClarivateResearches { get; set; }
        public int? ClarivateFrom { get; set; }
        public int? ClarivateTo { get; set; }
        public int? Accepted { get; set; }
        public int? LocalARWURank { get; set; }
        public int? LocalTHERank { get; set; }
        public int? LocalQSRank { get; set; }


        // Option B: If you insist on a full constructor, match parameter names
        // exactly to the property names (case-insensitive). Example:
        [JsonConstructor]
        public AcademicInfo(
            int InsID,
            int? InsTypeID,
            string InsType,
            int? HighEdu_Rec,
            int? QualityDept_Rec,
            string StudyLangCitizen,
            string StudyLangInter,
            int? JointClass,
            string StudySystem,
            int? MinHours,
            int? MaxHours,
            string ResearchScopus,
            string ResearchOthers,
            int? Practicing,
            int? StudyAttendance,
            int? StudentsMove,
            string StudyAttendanceDesc,
            string StudentsMoveDesc,
            int? DistanceLearning,
            int? MaxHoursDL,
            int? MaxYearsDL,
            int? MaxSemsDL,
            int? Diploma,
            int? DiplomaTest,
            int? HoursPercentage,
            string EducationType,
            string AvailableDegrees,
            string Speciality,
            int ARWURank,
            int THERank,
            int QSRank,
            string OtherRank,
            int? NumOfScopusResearches,
            int? ScopusFrom,
            int? ScopusTo,
            int? NumOfClarivateResearches,  
        int? ClarivateFrom,            
        int? ClarivateTo,               
            int? Accepted ,
            int? LocalARWURank, //new
            int? LocalTHERank, //new
            int? LocalQSRank //new
        )
        {
            this.InsID = InsID;
            this.InsTypeID = InsTypeID;
            this.InsType = InsType;
            this.HighEdu_Rec = HighEdu_Rec;
            this.QualityDept_Rec = QualityDept_Rec;
            this.StudyLangCitizen = StudyLangCitizen;
            this.StudyLangInter = StudyLangInter;
            this.JointClass = JointClass;
            this.StudySystem = StudySystem;
            this.MinHours = MinHours;
            this.MaxHours = MaxHours;
            this.ResearchScopus = ResearchScopus;
            this.ResearchOthers = ResearchOthers;
            this.Practicing = Practicing;
            this.StudyAttendance = StudyAttendance;
            this.StudentsMove = StudentsMove;
            this.StudyAttendanceDesc = StudyAttendanceDesc;
            this.StudentsMoveDesc = StudentsMoveDesc;
            this.DistanceLearning = DistanceLearning;
            this.MaxHoursDL = MaxHoursDL;
            this.MaxYearsDL = MaxYearsDL;
            this.MaxSemsDL = MaxSemsDL;
            this.Diploma = Diploma;
            this.DiplomaTest = DiplomaTest;
            this.HoursPercentage = HoursPercentage;
            this.EducationType = EducationType;
            this.AvailableDegrees = AvailableDegrees;
            this.Speciality = Speciality;
            this.ARWURank = ARWURank;
            this.THERank = THERank;
            this.QSRank = QSRank;
            this.OtherRank = OtherRank;
            this.NumOfScopusResearches = NumOfScopusResearches;
            this.ScopusFrom = ScopusFrom;
            this.ScopusTo = ScopusTo;
            this.NumOfClarivateResearches = NumOfClarivateResearches;  
            this.ClarivateFrom = ClarivateFrom;                        
            this.ClarivateTo = ClarivateTo;                            
            this.Accepted = Accepted;
            this.LocalARWURank= LocalARWURank; //new
            this.LocalTHERank= LocalTHERank; //new
            this.LocalQSRank= LocalQSRank; //new
        }
    }
}
