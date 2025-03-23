// File: formHandler.js

document.addEventListener("DOMContentLoaded", () => {

    // 0) If we have a 'StoredInsID', override the normal 'InsID' first
    const storedInsID = localStorage.getItem("StoredInsID");
    if (storedInsID) {
        console.log(`Found StoredInsID = ${storedInsID}; overwriting InsID...`);
        localStorage.setItem("InsID", storedInsID);
    }

    function populateFormFields() {
        const insID = localStorage.getItem('InsID');
        const insName = localStorage.getItem('InsName');
        const insCountry = localStorage.getItem('InsCountry');
        // (If needed) const insSpeciality = localStorage.getItem('Speciality');

        if (insID) document.getElementById('InsID').value = insID;
        if (insName) document.getElementById('InsName').value = insName;
        if (insCountry) document.getElementById('Country').value = insCountry;
    }

    function checkAuthentication() {
        const insID = localStorage.getItem('InsID');
        const supervisorID = localStorage.getItem('SupervisorID');

        if (supervisorID) {
            console.log("Supervisor user is authenticated.");
            return;
        }

        if (insID) {
            const requiredKeys = ['InsID', 'InsName', 'Clearance', 'Verified'];
            const missingKeys = requiredKeys.filter(key => !localStorage.getItem(key));

            if (missingKeys.length > 0) {
                console.warn('Missing required keys in localStorage for institution user:', missingKeys);
                alert('Session expired or invalid data. Redirecting to login page.');
                window.location.href = '/login.html';
                return;
            }
            console.log("Institution user is authenticated.");
            return;
        }

        console.warn("Neither InsID nor SupervisorID found in localStorage. Redirecting...");
        alert('Session expired or invalid data. Redirecting to login page.');
        window.location.href = '/login.html';
    }

    // Logout link logic
    const logoutLink = document.getElementById("lnkLogout");
    if (logoutLink) {
        logoutLink.addEventListener("click", function (e) {
            e.preventDefault();
            console.log("Logging out...");
            localStorage.clear();
            window.location.href = "/login.html";
        });
    }

    // Finally, run checks & populate fields
    checkAuthentication();
    populateFormFields();
});