//FILE: academicinfo_files/api.js

document.addEventListener('DOMContentLoaded', async function () {
    const academicInfoForm = document.getElementById('academicInfoForm');
    if (!academicInfoForm) {
        console.error("No #academicInfoForm found.");
        return;
    }

    // 1) Attempt to GET existing record (if InsID is in localStorage)
    const storedInsID = localStorage.getItem("InsID");
    if (!storedInsID) {
        console.warn("No InsID in localStorage, skipping GET from /api/academicinfo/getbyid");
    } else {
        try {
            const getResponse = await fetch(`/api/academicinfo/getbyid/${storedInsID}`);
            if (!getResponse.ok) {
                // <--- NEW BLOCK: handle 404 silently
                if (getResponse.status === 404) {
                    console.info("No AcademicInfo found in DB. This is normal for first time entries.");
                    // No error or alert. Just keep the form blank.
                } else {
                    const errData = await getResponse.json();
                    console.error("Error fetching AcademicInfo:", errData);
                    alert(`Error fetching AcademicInfo: ${errData.message || getResponse.status}`);
                }
            } else {
                const resultJson = await getResponse.json();
                console.log("GET /api/academicinfo/getbyid response:", resultJson);

                if (resultJson.success) {
                    const data = resultJson.data;
                    // Populate form fields
                    document.getElementById('InsID').value = data.insID || '0';
                    if (data.insTypeID != null) {
                        document.getElementById('InsTypeID').value = data.insTypeID;
                    }
                    document.getElementById('InsType').value = data.insType || '';
                    document.getElementById('HighEdu_Rec').value = data.highEdu_Rec || '';
                    document.getElementById('QualityDept_Rec').value = data.qualityDept_Rec || '';
                    document.getElementById('StudyLangCitizen').value = data.studyLangCitizen || '';
                    document.getElementById('StudyLangInter').value = data.studyLangInter || '';
                    if (data.jointClass != null) {
                        document.getElementById('JointClass').value = data.jointClass;
                    }
                    // For studySystem checkboxes, if data.studySystem = "Yearly Program,ECTS", then check them
                    const systemArray = data.studySystem ? data.studySystem.split(',') : [];
                    ['YearlyProgram', 'SemesterProgram', 'CreditHours', 'ECTS'].forEach(id => {
                        document.getElementById(id).checked = systemArray.includes(document.getElementById(id).value);
                    });

                    document.getElementById('MinHours').value = data.minHours || '';
                    document.getElementById('MaxHours').value = data.maxHours || '';
                    //document.getElementById('ResearchScopus').value = data.researchScopus || '';
                    //document.getElementById('ResearchOthers').value = data.researchOthers || '';

                    if (data.practicing != null) {
                        document.getElementById('Practicing').value = data.practicing;
                    }
                    if (data.studyAttendance != null) {
                        document.getElementById('StudyAttendance').value = data.studyAttendance;
                    }
                    if (data.studentsMove != null) {
                        document.getElementById('StudentsMove').value = data.studentsMove;
                    }
                    document.getElementById('StudyAttendanceDesc').value = data.studyAttendanceDesc || '';
                    document.getElementById('StudentsMoveDesc').value = data.studentsMoveDesc || '';
                    if (data.distanceLearning != null) {
                        document.getElementById('DistanceLearning').value = data.distanceLearning;
                    }
                    document.getElementById('MaxHoursDL').value = data.maxHoursDL || '';
                    document.getElementById('MaxYearsDL').value = data.maxYearsDL || '';
                    document.getElementById('MaxSemsDL').value = data.maxSemsDL || '';
                    if (data.diploma != null) {
                        document.getElementById('Diploma').value = data.diploma;
                    }
                    document.getElementById('DiplomaTest').value = data.diplomaTest || '';
                    document.getElementById('HoursPercentage').value = data.hoursPercentage || '';
                    document.getElementById('EducationType').value = data.educationType || '';

                    // AvailableDegrees checkboxes
                    //const degArray = data.availableDegrees ? data.availableDegrees.split(',') : [];
                    //['DiplomaChk', 'HigherDiplomaChk', 'BSCChk', 'MasterChk', 'PhDChk'].forEach(id => {
                    //    document.getElementById(id).checked = degArray.includes(document.getElementById(id).value);
                    //});
                    // ✅ Speciality checkboxes (New Section)
                    if (data.speciality) {
                        const specialityArray = data.speciality.split(','); // Convert string to array
                        document.querySelectorAll('.speciality-checkbox').forEach(checkbox => {
                            if (specialityArray.includes(checkbox.value)) {
                                checkbox.checked = true; // Check relevant boxes
                            }
                        });
                    }

                    document.getElementById('ARWURank').value = data.arwuRank || 0;
                    document.getElementById('THERank').value = data.theRank || 0;
                    document.getElementById('QSRank').value = data.qsRank || 0;

                    document.getElementById('LocalARWURank').value = data.localARWURank || 0;
                    document.getElementById('LocalTHERank').value = data.localTHERank || 0;
                    document.getElementById('LocalQSRank').value = data.localQSRank || 0;


                    document.getElementById('OtherRank').value = data.otherRank || '';
                    document.getElementById('NumOfScopusResearches').value = data.numOfScopusResearches || 0;

                    // ScopusFrom / ScopusTo are ints, e.g. 20230415, so to fill input type="date", parse carefully (optional).
                    // If stored as "20231110", you can reformat to "2023-11-10". Example:
                    if (data.scopusFrom > 0) {
                        // e.g. 20231110 -> "2023-11-10"
                        let sf = data.scopusFrom.toString();
                        if (sf.length === 8) {
                            sf = sf.slice(0, 4) + '-' + sf.slice(4, 6) + '-' + sf.slice(6);
                            document.getElementById('ScopusFrom').value = sf;
                        }
                    }
                    if (data.scopusTo > 0) {
                        let st = data.scopusTo.toString();
                        if (st.length === 8) {
                            st = st.slice(0, 4) + '-' + st.slice(4, 6) + '-' + st.slice(6);
                            document.getElementById('ScopusTo').value = st;
                        }
                    }
                    // New: Populate Clarivate info
                    document.getElementById('NumOfClarivateResearches').value = data.numOfClarivateResearches || 0;
                    if (data.clarivateFrom > 0) {
                        let cf = data.clarivateFrom.toString();
                        if (cf.length === 8) {
                            cf = cf.slice(0, 4) + '-' + cf.slice(4, 6) + '-' + cf.slice(6);
                            document.getElementById('ClarivateFrom').value = cf;
                        }
                    }
                    if (data.clarivateTo > 0) {
                        let ct = data.clarivateTo.toString();
                        if (ct.length === 8) {
                            ct = ct.slice(0, 4) + '-' + ct.slice(4, 6) + '-' + ct.slice(6);
                            document.getElementById('ClarivateTo').value = ct;
                        }
                    }
                  
                    if (data.accepted != null) {
                        document.getElementById('Accepted').checked = (data.accepted === 1);
                    }
                }
            }
        } catch (error) {
            console.error("Network error fetching AcademicInfo:", error);
            alert(`Unable to fetch AcademicInfo: ${error.message}`);
        }
    }

    // 2) Submit -> POST to /api/academicinfo/save
    academicInfoForm.addEventListener('submit', async function (event) {
        event.preventDefault();

        // Gather form fields
        const insID = parseInt(document.getElementById('InsID').value) || 0;
        const insTypeID = parseInt(document.getElementById('InsTypeID').value) || null;
        const insType = document.getElementById('InsType').value || '';
        const highEdu_Rec = parseInt(document.getElementById('HighEdu_Rec').value) || null;
        const qualityDept_Rec = parseInt(document.getElementById('QualityDept_Rec').value) || null;
        const studyLangCitizen = document.getElementById('StudyLangCitizen').value || '';
        const studyLangInter = document.getElementById('StudyLangInter').value || '';
        const jointClass = parseInt(document.getElementById('JointClass').value) || null;

        // studySystem from checkboxes
        const studySystemArray = Array.from(document.querySelectorAll('input[name="StudySystem"]:checked')).map(cb => cb.value);
        const studySystem = studySystemArray.join(',');

        const minHours = parseInt(document.getElementById('MinHours').value) || null;
        const maxHours = parseInt(document.getElementById('MaxHours').value) || null;
        //const researchScopus = document.getElementById('ResearchScopus').value || '';
        //const researchOthers = document.getElementById('ResearchOthers').value || '';
        const practicing = parseInt(document.getElementById('Practicing').value) || null;
        const studyAttendance = parseInt(document.getElementById('StudyAttendance').value) || null;
        const studentsMove = parseInt(document.getElementById('StudentsMove').value) || null;
        const studyAttendanceDesc = document.getElementById('StudyAttendanceDesc').value || '';
        const studentsMoveDesc = document.getElementById('StudentsMoveDesc').value || '';
        const distanceLearning = parseInt(document.getElementById('DistanceLearning').value) || null;
        const maxHoursDL = parseInt(document.getElementById('MaxHoursDL').value) || null;
        const maxYearsDL = parseInt(document.getElementById('MaxYearsDL').value) || null;
        const maxSemsDL = parseInt(document.getElementById('MaxSemsDL').value) || null;
        const diploma = parseInt(document.getElementById('Diploma').value) || null;
        const diplomaTest = parseInt(document.getElementById('DiplomaTest').value) || null;
        const hoursPercentage = parseInt(document.getElementById('HoursPercentage').value) || null;
        const educationType = document.getElementById('EducationType').value || '';

        // availableDegrees from checkboxes
        const degArray = Array.from(document.querySelectorAll('input[name="AvailableDegrees"]:checked')).map(cb => cb.value);
        const availableDegrees = degArray.join(',');
        const specialityValues = Array.from(document.querySelectorAll('.speciality-checkbox:checked')).map(input => input.value);
        const speciality = specialityValues.join(',');
        const arwuRank = parseInt(document.getElementById('ARWURank').value) || 0;
        const theRank = parseInt(document.getElementById('THERank').value) || 0;
        const qsRank = parseInt(document.getElementById('QSRank').value) || 0;
        const localArwuRank = parseInt(document.getElementById('LocalARWURank').value) || 0;
        const localTheRank = parseInt(document.getElementById('LocalTHERank').value) || 0;
        const localQsRank = parseInt(document.getElementById('LocalQSRank').value) || 0;
        const otherRank = document.getElementById('OtherRank').value || '';
        const numOfScopusResearches = parseInt(document.getElementById('NumOfScopusResearches').value) || 0;

        // scopusFrom / scopusTo are date inputs but DB expects int YYYYMMDD
        // So if user picks "2023-11-10", transform to "20231110" (int)
        function dateInputToInt(id) {
            const val = document.getElementById(id).value; // e.g. "2023-11-10"
            if (!val) return 0;
            // "2023-11-10" -> "20231110"
            return parseInt(val.replace(/-/g, ''));
        }
        const scopusFrom = dateInputToInt('ScopusFrom');
        const scopusTo = dateInputToInt('ScopusTo');

       
        // accepted is a checkbox => convert to int (1 or 0)
        const accepted = document.getElementById('Accepted').checked ? 1 : 0;

        // Build the object
        // New: Gather Clarivate data
        const numOfClarivateResearches = parseInt(document.getElementById('NumOfClarivateResearches').value) || 0;
        const clarivateFrom = dateInputToInt('ClarivateFrom');
        const clarivateTo = dateInputToInt('ClarivateTo');

        const academicInfo = {
            insID,
            insTypeID,
            insType,
            highEdu_Rec,
            qualityDept_Rec,
            studyLangCitizen,
            studyLangInter,
            jointClass,
            studySystem,
            minHours,
            maxHours,
            practicing,
            studyAttendance,
            studentsMove,
            studyAttendanceDesc,
            studentsMoveDesc,
            distanceLearning,
            maxHoursDL,
            maxYearsDL,
            maxSemsDL,
            diploma,
            diplomaTest,
            hoursPercentage,
            educationType,
            availableDegrees,
            speciality,
            arwuRank,
            theRank,
            qsRank,
            localArwuRank,
            localTheRank,
            localQsRank,
            otherRank,
            numOfScopusResearches,
            scopusFrom,
            scopusTo,
            numOfClarivateResearches, // new
            clarivateFrom,            // new
            clarivateTo,              // new
            accepted
        };

        console.log("academicInfo to be sent:", academicInfo);

        try {
            const rawResponse = await fetch('/api/academicinfo/save', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(academicInfo)
            });

            console.log("Raw fetch response:", rawResponse);

            if (!rawResponse.ok) {
                const errorData = await rawResponse.json();
                console.error('Error saving AcademicInfo:', errorData);
                alert(`Error: ${errorData.message || 'Unable to save academic information.'}`);
                return;
            }

            const result = await rawResponse.json();
            console.log("Parsed JSON response:", result);
            alert(`Success: ${result.message}`);
            window.location.href = "/Mspec3.html";

            // academicInfoForm.reset(); // If you want to clear the form
        } catch (error) {
            console.error("Network error or server not reachable:", error);
            alert(`Network error: ${error.message}`);
        }
    });
});

// Helper to sync hidden <input> #InsType with the text version of #InsTypeID
function updateHiddenField() {
    try {
        const dropdown = document.getElementById('InsTypeID');
        const hiddenField = document.getElementById('InsType');
        hiddenField.value = dropdown.options[dropdown.selectedIndex].text;
    } catch (err) {
        console.error("Error in updateHiddenField:", err);
    }
}
