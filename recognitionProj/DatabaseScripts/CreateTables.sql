﻿USE [master]
GO

CREATE DATABASE [RNJI] ON  PRIMARY 
( NAME = N'RNJI', FILENAME = N'E:\recog\gg database\RNJI.mdf'  )
 LOG ON 
( NAME = N'RNJI_log', FILENAME = N'E:\recog\gg database\RNJI_log.ldf' )
USE [RNJI]
GO
CREATE TABLE [dbo].[AcademicInfo](
	[InsID] [int] NOT NULL,
	[InsTypeID] [int] NULL,
	[InsType] [nvarchar](50) NULL,
	[HighEdu_Rec] [int] NULL,
	[QualityDept_Rec] [int] NULL,
	/*[QualityDept_Attach] [nvarchar](50) NULL,*/
	[StudyLangCitizen] [nvarchar](50) NULL,
	[StudyLangInter] [nvarchar](50) NULL,
	[JointClass] [int] NULL,
	[StudySystem] [nvarchar](50) NULL,
	[MinHours] [int] NULL,
	[MaxHours] [int] NULL,
	[ResearchScopus] [nvarchar](50) NULL,
	[ResearchOthers] [nvarchar](50) NULL,
	[Practicing] [int] NULL,
	[StudyAttendance] [int] NULL,
	[StudentsMove] [int] NULL,
	[StudyAttendanceDesc] [nvarchar](50) NULL,
	[StudentsMoveDesc] [nvarchar](50) NULL,
	[DistanceLearning] [int] NULL,
	[MaxHoursDL] [int] NULL,
	[MaxYearsDL] [int] NULL,
	[MaxSemsDL] [int] NULL,
	[Diploma] [int] NULL,
	[DiplomaTest] [int] NULL,
	[HoursPercentage] [int] NULL
	/*[SharedPrograms] [int] NULL*/,
	[EducationType] [nvarchar](50) NULL,
	[AvailableDegrees] [nvarchar](max) NULL,
	[Speciality] [nvarchar](200) NULL,
	--[Faculties] [nvarchar](max) NULL,
	[ARWURank] [int] NULL,
	[THERank] [int] NULL,
	[QSRank] [int] NULL,
	[OtherRank] [nvarchar](50) NULL,
	[NumOfScopusResearches] [int] NULL,
	[ScopusFrom] [int] NULL,
	[ScopusTo] [int] NULL,
	/*note that specializations are in a separate table*/
	--[Infrastructure] [nvarchar](max) NULL,
	[Accepted] [bit] NULL
)
--NEW NEW NEW NEW 
-- Adding new columns to the table for clarivate
ALTER TABLE [dbo].[AcademicInfo]
ADD 
    [NumOfClarivateResearches] [int] NULL,
    [ClarivateFrom] [int] NULL,
    [ClarivateTo] [int] NULL;
GO

ALTER TABLE [dbo].[AcademicInfo]
ADD 
    [LocalARWURank] [int] NULL,
    [LocalTHERank] [int] NULL,
    [LocalQSRank] [int] NULL ;

CREATE TABLE [dbo].[PublicInfo](
	[InsID] [int] NOT NULL,
	[InsName] [nvarchar](100) NULL,
	[Provider] [nvarchar](50) NULL,
	[StartDate] [nvarchar](50) NULL,
	[SDateT] [nvarchar](50) NULL,
	[SDateNT] [nvarchar](50) NULL,
	[SupervisorID] [int] NULL,/*note*/
	[Supervisor] [nvarchar](50) NULL,/*note*/
	[PreName] [nvarchar](50) NULL,
	[PreDegree] [nvarchar](50) NULL,
	[PreMajor] [nvarchar](50) NULL,
	[Postal] [nvarchar](50) NULL,
	[Phone] [nvarchar](50) NULL,
	[Fax] [nvarchar](50) NULL,
	[Email] [nvarchar](50) NULL,
	[Website] [nvarchar](50) NULL,
	[Vision] [nvarchar](100) NULL,
	[Mission] [nvarchar](100) NULL,
	[Goals] [nvarchar](100) NULL,/*Targeted Obj*/
	[InsValues] [nvarchar](100) NULL,
	[LastEditDate] [nvarchar](50) NULL,
	/*continue from here*/
	[EntryDate] [date] NULL,
	[Country] [nvarchar](50) NULL,
	[City] [nvarchar](50) NULL,
	[Address] [nvarchar](50) NULL,
	[CreationDate] [date] NULL,
	[StudentAcceptanceDate] [date] NULL,
	[OtherInfo] [nvarchar](max) NULL,
) 


CREATE TABLE [dbo].[AdmissionRules](
	[InsID] [int] NOT NULL,
	[AdmissionRule] [nvarchar](100) NULL,
	[AdmissionDegree] [nvarchar](50) NULL
) 
CREATE TABLE [dbo].[Attachments](
	[InsID] [int] NOT NULL,
	[AttachID] [int] IDENTITY(1,1) NOT NULL,
	[AttachName] [nvarchar](100) NULL,
	[AttachDesc] [nvarchar](100) NULL,
 CONSTRAINT [PK_Attachments] PRIMARY KEY CLUSTERED 
(
	[AttachID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
)
CREATE TABLE [dbo].[Countries](
	[CountryNO] [int] NOT NULL,
	[CountryNameAR] [nvarchar](50) NULL,
	[CountryNameEN] [nvarchar](50) NULL,
 CONSTRAINT [PK_Countries] PRIMARY KEY CLUSTERED 
(
	[CountryNO] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) 

CREATE TABLE [dbo].[Hospitals](
	[InsID] [int] NOT NULL,
	[HospID] [int] IDENTITY(1,1) NOT NULL,
	[HospType] [nvarchar](50) NULL,
	[HospName] [nvarchar](100) NULL,
	[HospMajor] [nvarchar](100) NULL,
 CONSTRAINT [PK_Hospitals] PRIMARY KEY CLUSTERED 
(
	[HospID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) 

CREATE TABLE [dbo].[Infrastructure](
	[InsID] [int] NOT NULL,
	[Area] [int] NULL,
	[Sites] [int] NULL,
	[Terr] [int] NULL,
	[Halls] [int] NULL,
	[Library] [int] NULL,
	--[Labs] [nvarchar](500) NULL,
	--[LabsAttach] [nvarchar](500) NULL,
	--[Build] [nvarchar](500) NULL
) 
CREATE TABLE [dbo].[Library](
	[InsID] [int] NOT NULL,
	[Area] [int] NULL,
	[Capacity] [int] NULL,
	[ArBooks] [int] NULL,
	[EnBooks] [int] NULL,
	[Papers] [int] NULL,
	[eBooks] [int] NULL,
	[eSubscription] [int] NULL
) 
CREATE TABLE [dbo].[USERS](
	[InsID] [int] IDENTITY(1,1) NOT NULL,
	[InsName] [nvarchar](100) NULL,
	[InsCountry] [nvarchar](50) NULL,
	[Email] [nvarchar](200) NULL,
	[Password] [nvarchar](50) NULL,
	[VerificationCode] [nvarchar](50) NULL,
	[Verified] [int] NULL,
	[Clearance] [int] NOT NULL,
	[Speciality] [nvarchar](50) NULL,
 CONSTRAINT [PK_USERS] PRIMARY KEY CLUSTERED 
(
	[InsID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
)
-- because i modified the email to be bigger, if you still have the old database and you dont want to drop the table, you can use this script to modify the email column
--ALTER TABLE [dbo].[USERS]
--ALTER COLUMN [Email] NVARCHAR(200) NULL;

CREATE TABLE [dbo].[Pictures](
	[InsID] [int] NOT NULL,
	[AttachID] [int] IDENTITY(1,1) NOT NULL,
	[AttachName] [nvarchar](100) NULL,
	[AttachDesc] [nvarchar](100) NULL
) 

CREATE TABLE [dbo].[StudentsAndStuff](
	[InsID] [int] NOT NULL,
	[StudentCitizen] [int] NULL,
	[StudentInter] [int] NULL,
	[StudentJordan] [int] NULL,
	[StudentOverall] [int] NULL,
	[StaffProfessor] [int] NULL,
	[StaffCoProfessor] [int] NULL,
	[StaffAssistantProfessor] [int] NULL,
	[StaffLabSupervisor] [int] NULL,
	[StaffResearcher] [int] NULL,
	[StaffTeacher] [int] NULL,
	[StaffTeacherAssistant] [int] NULL,
	[StaffOthers] [int] NULL
)
-- If the table doesn't exist yet:
CREATE TABLE [dbo].[StudyDuration](
    [InsID] [int] NOT NULL,
    [DiplomaDegreeMIN] [nvarchar](50) NULL,
    [DiplomaMIN] [nvarchar](50) NULL,
    [BSC_DegreeMIN] [nvarchar](50) NULL,
    [BSC_MIN] [nvarchar](50) NULL,
    [HigherDiplomaDegreeMIN] [nvarchar](50) NULL,
    [HigherDiplomaMIN] [nvarchar](50) NULL,
    [MasterDegreeMIN] [nvarchar](50) NULL,
    [MasterMIN] [nvarchar](50) NULL,
    [PhD_DegreeMIN] [nvarchar](50) NULL,
    [PhD_MIN] [nvarchar](50) NULL,
    [SemestersPerYear] INT NULL,         -- New field #1
    [MonthsPerSemester] INT NULL         -- New field #2
);

-- If the table already existed, just do:
-- ALTER TABLE [dbo].[StudyDuration]
--    ADD [SemestersPerYear] INT NULL,
--        [MonthsPerSemester] INT NULL;

CREATE TABLE [dbo].[Specializations](
    [InsID] [int] NOT NULL ,  -- معرف الكيان
    [Type] [nvarchar](100) NOT NULL, -- نوع الكيان
    [NumStu] [int] NOT NULL, -- عدد الطلاب
    [NumProf] [int] NOT NULL, -- عدد المدرسين المتفرغين (Full-Time Teachers)
    --[NumPartTimeProf] [int] NOT NULL, -- عدد المدرسين غير المتفرغين (Part-Time Teachers)
    --[NumProf] [int] NOT NULL, -- عدد الأساتذة (Professors)
    [NumAssociative] [int] NOT NULL, -- عدد الأساتذة المشاركين (Associate Professors)
    [NumAssistant] [int] NOT NULL, -- عدد الأساتذة المساعدين (Assistant Professors)
    [NumProfPractice] [int],
    [NumberLecturers] [int] NOT NULL, -- عدد المدرسين (Lecturers)
    [NumAssisLecturer] [int] NOT NULL, -- عدد المدرسين المساعدين (Assistant Lecturers)
    [NumOtherTeachers] [int] NOT NULL, -- عدد المدرسين الآخرين (Other Teachers)
    --[SpecAttachName] [nvarchar](100) NOT NULL, -- اسم المرفق
    --[SpecAttachDesc] [nvarchar](100) NOT NULL, -- وصف المرفق
	[Color] [int] NOT NULL, -- red for no, orange for partial, green for full
	[Ratio] DECIMAL(5,2) NOT NULL,
	[NumPhdHolders] [int] NOT NULL, -- عدد حملة الدكتوراه
	[PracticalHours] [int] NOT NULL, -- عدد ساعات التدريب العملي
	[TheoreticalHours] [int] NOT NULL, -- عدد ساعات التدريس النظري
	CONSTRAINT UQ_Specializations_InsID_Type UNIQUE (InsID, Type) -- UNIQUE constraint
);
CREATE TABLE [dbo].[ExcelAttach](
[InsID] [int] NOT NULL,
[AttachID] [int] IDENTITY(1,1) NOT NULL,
[AttachName] [nvarchar](100) NULL,
[AttachDesc] [nvarchar](100) NULL,

)
CREATE TABLE [dbo].[AcceptanceRecord]
(
    [InsID]       INT       NOT NULL,
    [RecordNo]    INT       NOT NULL,
    [DateOfRecord]  DATE    NOT NULL,
    [Result]      NVARCHAR(30)  NOT NULL,  -- e.g. "Accepted" or "Rejected"
    [Message]     NVARCHAR(max) NULL       -- JSON or truncated reason
)
ALTER TABLE AcceptanceRecord
ADD RecordID INT PRIMARY KEY IDENTITY(1,1);
-- Adding a nullable binary column named 'finalized'
ALTER TABLE [dbo].[AcceptanceRecord]
ADD [finalized] VARBINARY(MAX) NULL;
ALTER TABLE [dbo].[AcceptanceRecord]
ADD meetingFinalized NVARCHAR(50) NULL,
    decisionOutcome NVARCHAR(30) NULL;
ALTER TABLE [dbo].[AcceptanceRecord]
ALTER COLUMN decisionOutcome NVARCHAR(200) NULL;


CREATE TABLE [dbo].[Supervisor]
(
    [SupervisorID]   INT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name]           NVARCHAR(50) NOT NULL,
    [PhoneNumber]    NVARCHAR(20) NOT NULL,       -- or INT, but phone often better as string
    [Email]          NVARCHAR(100) NOT NULL UNIQUE,
    [Password]       NVARCHAR(100) NOT NULL,
    [Speciality]     NVARCHAR(100) NULL
);
CREATE TABLE [dbo].[SubmissionRequests] (
    [SubmissionID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [InsID] INT NOT NULL,               -- Which institution
    [ApplicantName] NVARCHAR(100) NOT NULL,
    [WorkPlace] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(100) NOT NULL,
    [SubmissionDate] DATETIME NOT NULL,
    [AssignedSupervisorID] INT NULL
);
CREATE TABLE ApplicationQueue (
    QueueID INT IDENTITY(1,1) PRIMARY KEY,
    InsID INT NOT NULL,
    InstitutionName NVARCHAR(255) NOT NULL,
    Specialization NVARCHAR(255) NOT NULL,
    SubmissionDate DATETIME DEFAULT GETDATE(),
    Processed BIT DEFAULT 0 -- 0: Pending, 1: Assigned
);

	CREATE TABLE Meetings (
		MeetingID INT IDENTITY(1,1) PRIMARY KEY,
		MeetingNumber NVARCHAR(20) NOT NULL,  -- Format: No/Year
		CreationDate DATETIME DEFAULT GETDATE(),
		Status NVARCHAR(50) DEFAULT 'Scheduled', -- (Scheduled, Completed)

		-- Institution-related fields
		InsID INT NOT NULL, 
		InstitutionName NVARCHAR(255) NOT NULL, 
		Specialization NVARCHAR(255) NOT NULL, 
		SubmissionDate DATETIME NOT NULL,

		-- Supervisor Assignment
		SupervisorID INT NULL,

		-- Correct Foreign Keys
		FOREIGN KEY (InsID) REFERENCES USERS(InsID) ON DELETE CASCADE,
		FOREIGN KEY (SupervisorID) REFERENCES Supervisor(SupervisorID) ON DELETE SET NULL
	);
	CREATE TABLE Faculty (
    InsID INT NOT NULL,
    FacultyName NVARCHAR(255) NOT NULL, -- Restricted size for indexing performance
    CreateDate DATE NOT NULL,
    LastGraduteDate DATE NOT NULL,
	NumPrograms INT NOT NULL,
	NumPgPrograms INT NOT NULL,
    CONSTRAINT UQ_Faculty_InsID_FacultyName UNIQUE (InsID, FacultyName) -- Ensures uniqueness
);
CREATE TABLE [dbo].[Hospitals](
	[InsID] [int] NOT NULL,
	[HospType] [nvarchar](50) NULL,
	[HospName] [nvarchar](300) NULL,
	[HospMajor] [nvarchar](200) NULL,
	CONSTRAINT UQ_Hospitals_InsID_HospType_HospName_HospMajor  UNIQUE ([InsID], [HospType], [HospName], [HospMajor])
); 