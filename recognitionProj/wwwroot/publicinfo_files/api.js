// File: publicinfo_files/api.js

document.addEventListener('DOMContentLoaded', async function () {
    const publicInfoForm = document.getElementById('publicInfoForm');

    // 1) Safety check: if the form doesn't exist, exit.
    if (!publicInfoForm) {
        console.error("publicInfoForm not found");
        return;
    }
    // 🔹 Step 1: Retrieve stored email from localStorage
    const storedEmail = localStorage.getItem('UserEmail');

    // 🔹 Step 2: Find the email input field
    const emailField = document.getElementById('Email');

    // 🔹 Step 3: If an email exists in localStorage, set it as the default value
    if (emailField && storedEmail) {
        emailField.value = storedEmail; // Allows overwriting
    }

    // 2) Attempt to GET existing PublicInfo from the server if an InsID is in localStorage.
    //    auth.js already stores: localStorage.setItem("InsID", "123");
    const InsID = localStorage.getItem("InsID");
    if (!InsID) {
        console.warn("No InsID in localStorage; skipping GET /publicinfo/getbyid.");
    } else {
        try {
            const getResponse = await fetch(`/api/publicinfo/getbyid/${InsID}`);
            if (!getResponse.ok) {
                // <--- NEW BLOCK: handle 404 silently
                if (getResponse.status === 404) {
                    console.info("No PublicInfo found. Probably first time. Leaving form blank.");
                } else {
                    const errData = await getResponse.json();
                    console.error("Error fetching PublicInfo:", errData);
                    alert(`Error fetching PublicInfo: ${errData.message || getResponse.status}`);
                }
            } else {
                const resultJson = await getResponse.json();
                console.log("GET /api/publicinfo/getbyid response:", resultJson);


                if (resultJson.success) {
                    // 2b) Populate the form
                    const data = resultJson.data;
                    document.getElementById('InsID').value = data.insID || '';
                    document.getElementById('InsName').value = data.insName || '';
                    document.getElementById('Provider').value = data.provider || '';
                    document.getElementById('StartDate').value = data.startDate || '';
                    document.getElementById('SDateT').value = data.sDateT || '';
                    document.getElementById('SDateNT').value = data.sDateNT || '';
                    //
                    if (data.supervisorID !== null) {
                        document.getElementById('SupervisorID').value = data.supervisorID;
                        document.getElementById('SupervisorID').closest('.form-field').style.display = 'none';
                    }
                    document.getElementById('Supervisor').value = data.supervisor || '';
                    document.getElementById('Supervisor').closest('.form-field').style.display = 'none';

                    document.getElementById('PreName').value = data.preName || '';
                    document.getElementById('PreDegree').value = data.preDegree || '';
                    document.getElementById('PreMajor').value = data.preMajor || '';
                    document.getElementById('Postal').value = data.postal || '';
                    document.getElementById('Phone').value = data.phone || '';
                    document.getElementById('Fax').value = data.fax || '';
                    document.getElementById('Email').value = data.email || '';
                    document.getElementById('Website').value = data.website || '';
                    document.getElementById('Vision').value = data.vision || '';
                    document.getElementById('Mission').value = data.mission || '';
                    document.getElementById('Goals').value = data.goals || '';
                    document.getElementById('InsValues').value = data.insValues || '';
                    document.getElementById('LastEditDate').value = data.lastEditDate || '';
                    document.getElementById('EntryDate').value = data.entryDate || '';
                    document.getElementById('Country').value = data.country || '';
                    document.getElementById('City').value = data.city || '';
                    document.getElementById('Address').value = data.address || '';
                    document.getElementById('CreationDate').value = data.creationDate || '';
                    document.getElementById('StudentAcceptanceDate').value = data.studentAcceptanceDate || '';
                    document.getElementById('OtherInfo').value = data.otherInfo || '';
                } else {
                    console.warn("Server responded success=false:", resultJson);
                    alert(`Server says: ${resultJson.message}`);
                }
            }
        } catch (error) {
            console.error("Network error fetching PublicInfo:", error);
            alert(`Unable to fetch PublicInfo: ${error.message}`);
        }
    }

    // 3) Now handle form submission (POST) to /api/publicinfo/save
    publicInfoForm.addEventListener('submit', async function (event) {
        event.preventDefault(); // Prevent default form submission

        // 3a) Gather all field values
        const insID = parseInt(document.getElementById('InsID').value) || 0;
        const insName = document.getElementById('InsName').value || '';
        const provider = document.getElementById('Provider').value || '';
        const startDate = document.getElementById('StartDate').value || '';
        const sDateT = document.getElementById('SDateT').value || '';
        const sDateNT = document.getElementById('SDateNT').value || '';

        const supervisorIDValue = document.getElementById('SupervisorID').value;
        const supervisorID = supervisorIDValue ? parseInt(supervisorIDValue) : null;
        const supervisor = document.getElementById('Supervisor').value || '';

        const preName = document.getElementById('PreName').value || '';
        const preDegree = document.getElementById('PreDegree').value || '';
        const preMajor = document.getElementById('PreMajor').value || '';

        const postal = document.getElementById('Postal').value || '';
        const phone = document.getElementById('Phone').value || '';
        const fax = document.getElementById('Fax').value || '';
        const email = document.getElementById('Email').value || '';
        const website = document.getElementById('Website').value || '';

        const vision = document.getElementById('Vision').value || '';
        const mission = document.getElementById('Mission').value || '';
        const goals = document.getElementById('Goals').value || '';
        const insValues = document.getElementById('InsValues').value || '';
        const lastEditDate = document.getElementById('LastEditDate').value || '';

        // 3b) DateOnly fields → "yyyy-MM-dd" strings
        const entryDate = document.getElementById('EntryDate').value || '';
        const country = document.getElementById('Country').value || '';
        const city = document.getElementById('City').value || '';
        const address = document.getElementById('Address').value || '';
        const creationDate = document.getElementById('CreationDate').value || '';
        const studentAcceptanceDate = document.getElementById('StudentAcceptanceDate').value || '';
        const otherInfo = document.getElementById('OtherInfo').value || '';

        // 3c) Build the object
        const publicInfo = {
            insID,
            insName,
            provider,
            startDate,
            sDateT,
            sDateNT,
            //supervisorID,
            supervisor: "",  // ✅ Correct syntax
            preName,
            preDegree,
            preMajor,
            postal,
            phone,
            fax,
            email,
            website,
            vision,
            mission,
            goals,
            insValues,
            lastEditDate,
            entryDate,             // "yyyy-MM-dd" or empty
            country,
            city,
            address,
            creationDate,          // "yyyy-MM-dd"
            studentAcceptanceDate, // "yyyy-MM-dd"
            otherInfo
        };

        console.log("publicInfo to be sent:", publicInfo);

        // 3d) POST to /api/publicinfo/save
        try {
            const rawResponse = await fetch('/api/publicinfo/save', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(publicInfo)
            });

            console.log("Raw fetch response:", rawResponse);

            if (!rawResponse.ok) {
                const errorData = await rawResponse.json();
                console.error('Error saving PublicInfo:', errorData);
                alert(`Error: ${errorData.message || 'Unable to save public information.'}`);
                return;
            }

            const result = await rawResponse.json();
            console.log("Parsed JSON response:", result);
            alert(`Success: ${result.message}`);
            window.location.href = "/academicinfo.html";

            // Optionally reset the form or navigate
            // publicInfoForm.reset();
        } catch (error) {
            console.error("Network error or server not reachable:", error);
            alert(`Network error: ${error.message}`);
        }
    });
});
